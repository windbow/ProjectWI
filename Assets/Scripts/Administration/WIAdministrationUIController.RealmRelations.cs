using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 각 진영의 공개 정보와 AI 운영 성향을 정세 창에 표시합니다.
        private void OpenFactionOverviewModal()
        {
            VisualElement panel = CreateModal("대륙 진영 정세");
            foreach (WIFactionDefinition faction in database.Factions)
            {
                int castleCount = state.Castles.FindAll(castle => castle.FactionId == faction.Id).Count;
                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                string economy = faction.PlayerFaction && factionState != null
                    ? $" · 금화 {factionState.Gold:N0} · 마나 {factionState.ManaCrystal:N0} · 영향력 {factionState.Influence:N0}"
                    : string.Empty;
                string status = factionState?.Eliminated == true ? $" · 멸망(제 {factionState.EliminatedTurn}턴)" : string.Empty;
                panel.Add(new Label($"{faction.DisplayName.Get(database.UseEnglish)} · 영토 {castleCount}성 · 성향 {GetAIStrategyDisplayName(faction.AIStrategy)}{status}{economy}"));
                foreach (WIStartingRelationshipDefinition relationship in database.StartingRelationships)
                {
                    if (relationship.FactionId != faction.Id) continue;
                    WIHeroDefinition first = database.GetHero(relationship.FirstHeroId);
                    WIHeroDefinition second = database.GetHero(relationship.SecondHeroId);
                    if (first == null || second == null) continue;
                    string level = relationship.Level == WIRelationshipLevel.Conflict ? "갈등" : "친애";
                    panel.Add(new Label($"  └ 주요 관계 · {first.DisplayName.Get(database.UseEnglish)} ↔ {second.DisplayName.Get(database.UseEnglish)} · {level}\n     {relationship.Context.Get(database.UseEnglish)}"));
                }
            }
        }

        // 플레이어 진영과 다른 진영의 현재 외교 상태를 목록으로 표시합니다.
        private void OpenDiplomacyModal()
        {
            VisualElement panel = CreateModal("외교");
            panel.Add(new Label("복잡한 점수 대신 현재 관계와 실행 가능한 명령만 표시합니다."));
            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.Id == state.PlayerFactionId)
                {
                    continue;
                }
                if (state.GetFactionState(faction.Id)?.Eliminated == true) continue;

                WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(state.PlayerFactionId, faction.Id);
                Button factionButton = new Button(() => OpenDiplomacyTargetModal(faction));
                factionButton.text = $"{faction.DisplayName.Get(database.UseEnglish)} · {GetDiplomaticStatusDisplayName(relation.Status)}";
                panel.Add(factionButton);
            }
        }

        // 조사·방첩·유언비어·인재 이간 중 실행할 첩보를 선택합니다.
        private void OpenSchemeModal()
        {
            VisualElement panel = CreateModal("첩보");
            panel.Add(new Label("지력이 높은 인물을 보내십시오. 적 성의 질서와 방첩이 성공률을 낮춥니다."));
            WIFactionRuntimeState faction = state.GetFactionState(state.PlayerFactionId);
            panel.Add(new Label($"보유 영향력 {faction.Influence:N0}"));
            foreach (WISchemeMissionState mission in state.SchemeMissions.Where(item => item.InitiatorFactionId == state.PlayerFactionId))
            {
                WISchemeDefinition activeScheme = database.GetScheme(mission.SchemeId);
                WIHeroDefinition activeAgent = database.GetHero(mission.AgentHeroId);
                WICastleDefinition activeTarget = database.GetCastle(mission.TargetCastleId);
                panel.Add(new Label($"진행 중 · {activeScheme?.DisplayName.Get(database.UseEnglish)} · " +
                    $"{activeAgent?.DisplayName.Get(database.UseEnglish)} → {activeTarget?.DisplayName.Get(database.UseEnglish)} · {mission.RemainingMonths}개월"));
            }
            foreach (WISchemeDefinition scheme in database.SchemeDefinitions)
            {
                Button button = new Button(() => OpenSchemeAgentModal(scheme));
                button.text = $"{scheme.DisplayName.Get(database.UseEnglish)} · 영향력 {scheme.InfluenceCost} · " +
                              $"기본 성공 {scheme.BaseSuccessChance}% · 기본 발각 {scheme.BaseDetectionChance}%\n" +
                              scheme.Description.Get(database.UseEnglish);
                button.SetEnabled(faction.Influence >= scheme.InfluenceCost);
                panel.Add(button);
            }
        }

        // 선택한 첩보를 맡길 플레이어 진영의 유휴 인물을 표시합니다.
        private void OpenSchemeAgentModal(WISchemeDefinition scheme)
        {
            VisualElement panel = CreateModal($"{scheme.DisplayName.Get(database.UseEnglish)} · 담당 인물");
            bool hasCandidate = false;
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == state.PlayerFactionId))
            {
                foreach (string heroId in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false))
                {
                    WIHeroDefinition hero = database.GetHero(heroId);
                    if (hero == null)
                    {
                        continue;
                    }

                    hasCandidate = true;
                    Button button = new Button(() => OpenSchemeTargetCastleModal(scheme, hero));
                    button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 지력 {hero.Intelligence}";
                    panel.Add(button);
                }
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 첩보 종류에 맞는 아군 또는 적 성을 대상 목록으로 표시합니다.
        private void OpenSchemeTargetCastleModal(WISchemeDefinition scheme, WIHeroDefinition agent)
        {
            VisualElement panel = CreateModal($"{scheme.DisplayName.Get(database.UseEnglish)} · 대상 성");
            bool ownTarget = scheme.SchemeType == WISchemeType.Counterintelligence;
            foreach (WICastleRuntimeState castleState in state.Castles.Where(item =>
                         ownTarget ? item.FactionId == state.PlayerFactionId : item.FactionId != state.PlayerFactionId))
            {
                WICastleDefinition castle = database.GetCastle(castleState.CastleId);
                Button button = new Button(() =>
                {
                    if (scheme.SchemeType == WISchemeType.Alienation)
                    {
                        OpenSchemeTargetHeroModal(scheme, agent, castleState);
                    }
                    else
                    {
                        ExecuteScheme(scheme, agent, castleState, null);
                    }
                });
                WISchemeIntelState intel = state.SchemeIntel.Find(item =>
                    item.ObserverFactionId == state.PlayerFactionId && item.TargetCastleId == castleState.CastleId);
                string defense = ownTarget ? $"방첩 {castleState.CounterintelligenceMonths}개월"
                    : intel == null ? "정보 미확보" : $"조사 정보 {intel.RemainingMonths}개월";
                button.text = $"{castle.DisplayName.Get(database.UseEnglish)} · {defense}";
                bool canRevealCalculation = ownTarget || intel != null;
                if (canRevealCalculation)
                {
                    int successChance = WISchemeSystem.CalculateSuccessChance(scheme, agent, castleState);
                    int counterPenalty = castleState.CounterintelligenceMonths > 0 ? 20 : 0;
                    button.tooltip = $"성공 {successChance}% = 기본 {scheme.BaseSuccessChance}% + 담당 지력 {agent.Intelligence}/2 - 질서 {castleState.Stability}/2 - 방첩 {counterPenalty}%\n발각은 기본 확률 + 질서/4 + 방첩 - 담당 지력/3으로 별도 판정합니다.";
                }
                else
                {
                    button.tooltip = $"정보 미확보 · 기본 성공 {scheme.BaseSuccessChance}%만 확인할 수 있습니다. 조사 성공 후 질서·방첩을 포함한 최종 확률이 공개됩니다.";
                }
                panel.Add(button);
            }
        }

        // 인재 이간의 대상 성에 주둔한 적 인물을 표시합니다.
        private void OpenSchemeTargetHeroModal(WISchemeDefinition scheme, WIHeroDefinition agent, WICastleRuntimeState castle)
        {
            VisualElement panel = CreateModal("인재 이간 · 대상 인물");
            if (WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, castle) == false)
            {
                panel.Add(new Label("주둔 인물 정보가 없습니다. 먼저 해당 성의 조사를 성공시키십시오."));
                return;
            }
            foreach (string heroId in castle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (hero == null || character == null)
                {
                    continue;
                }

                Button button = new Button(() => ExecuteScheme(scheme, agent, castle, hero));
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 충성 {GetLoyaltyDisplayName(character.LoyaltyState)}";
                panel.Add(button);
            }
        }

        // 첩보 담당 인물을 한 달 임무에 배정하고 다음 턴 판정을 예약합니다.
        private void ExecuteScheme(WISchemeDefinition scheme, WIHeroDefinition agent, WICastleRuntimeState targetCastle, WIHeroDefinition targetHero)
        {
            bool scheduled = WISchemeSystem.TrySchedule(database, state, scheme.Id, state.PlayerFactionId,
                agent.Id, targetCastle.CastleId, targetHero?.Id, UnityEngine.Random.Range(0, 100), out string message);
            RefreshAll();
            ShowMessage(scheduled ? message + "\n담당 인물은 결과가 나올 때까지 다른 임무에 배정할 수 없습니다." : message);
        }

        // 충성 상태를 첩보 UI용 한국어로 변환합니다.
        private string GetLoyaltyDisplayName(WILoyaltyState loyalty)
        {
            switch (loyalty)
            {
                case WILoyaltyState.Unsettled: return "동요";
                case WILoyaltyState.Danger: return "위험";
                default: return "안정";
            }
        }

        // 선택 진영의 관계 설명과 현재 가능한 외교 명령을 표시합니다.
        private void OpenDiplomacyTargetModal(WIFactionDefinition targetFaction)
        {
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(state.PlayerFactionId, targetFaction.Id);
            VisualElement panel = CreateModal($"외교 · {targetFaction.DisplayName.Get(database.UseEnglish)}");
            panel.Add(new Label(GetDiplomaticDescription(relation)));

            List<WICharacterRuntimeState> ourPrisoners = state.Characters.Where(item => item.Captured &&
                item.CapturedFromFactionId == state.PlayerFactionId && item.CaptorFactionId == targetFaction.Id).ToList();
            List<WICharacterRuntimeState> theirPrisoners = state.Characters.Where(item => item.Captured &&
                item.CapturedFromFactionId == targetFaction.Id && item.CaptorFactionId == state.PlayerFactionId).ToList();
            foreach (WICharacterRuntimeState prisoner in ourPrisoners)
            {
                WIHeroDefinition hero = database.GetHero(prisoner.HeroId);
                Button ransom = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.RansomPrisoner(database, state, state.PlayerFactionId, prisoner.HeroId),
                    targetFaction));
                ransom.text = $"포로 몸값 · {hero?.DisplayName.Get(database.UseEnglish) ?? prisoner.HeroId} · " +
                              $"금화 {database.PrisonerRansomGold}";
                ransom.SetEnabled(state.GetPlayerFactionState().Gold >= database.PrisonerRansomGold);
                panel.Add(ransom);
            }
            if (ourPrisoners.Count > 0 && theirPrisoners.Count > 0)
            {
                WICharacterRuntimeState ourPrisoner = ourPrisoners[0];
                WICharacterRuntimeState theirPrisoner = theirPrisoners[0];
                Button exchange = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.ExchangePrisoners(state, state.PlayerFactionId, targetFaction.Id,
                        ourPrisoner.HeroId, theirPrisoner.HeroId), targetFaction));
                exchange.text = $"포로 맞교환 · {database.GetHero(ourPrisoner.HeroId)?.DisplayName.Get(database.UseEnglish)} ↔ " +
                                database.GetHero(theirPrisoner.HeroId)?.DisplayName.Get(database.UseEnglish);
                panel.Add(exchange);
            }

            if (relation.Status == WIDiplomaticStatus.War || relation.Status == WIDiplomaticStatus.Neutral)
            {
                Button improve = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.ImproveDiplomaticRelations(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                improve.text = relation.Status == WIDiplomaticStatus.War
                    ? $"휴전 교섭 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} · 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}"
                    : $"친선 사절 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} · 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}";
                improve.tooltip = $"확정 명령 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} + 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}를 소비해 관계를 한 단계 개선합니다.";
                panel.Add(improve);
            }
            else if (relation.Status == WIDiplomaticStatus.Friendly)
            {
                Button pact = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.SignNonAggression(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                pact.text = $"불가침 협정 · 영향력 {WIAdministrationTurnSystem.NonAggressionInfluenceCost}";
                pact.tooltip = $"확정 명령 · 우호 관계에서 영향력 {WIAdministrationTurnSystem.NonAggressionInfluenceCost}를 소비합니다.";
                panel.Add(pact);
            }
            else if (relation.Status == WIDiplomaticStatus.NonAggression)
            {
                Button alliance = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.FormAlliance(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                alliance.text = $"동맹 체결 · 영향력 {WIAdministrationTurnSystem.AllianceInfluenceCost}";
                alliance.tooltip = $"확정 명령 · 불가침 관계에서 영향력 {WIAdministrationTurnSystem.AllianceInfluenceCost}를 소비합니다.";
                panel.Add(alliance);
            }
            else if (relation.Status == WIDiplomaticStatus.Alliance)
            {
                Button aid = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.RequestAllianceAid(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                aid.text = relation.AidCooldownMonths > 0
                    ? $"금화 원조 · {relation.AidCooldownMonths}개월 후 재요청"
                    : $"금화 원조 요청 · +{WIAdministrationTurnSystem.AllianceAidGold}";
                aid.SetEnabled(relation.AidCooldownMonths <= 0);
                aid.tooltip = relation.AidCooldownMonths > 0
                    ? $"사용 불가 · 재요청 대기 {relation.AidCooldownMonths}개월"
                    : $"확정 명령 · 동맹으로부터 금화 {WIAdministrationTurnSystem.AllianceAidGold}를 받고 재사용 대기시간이 적용됩니다.";
                panel.Add(aid);

                if (relation.JointAttackMonthsRemaining > 0)
                {
                    panel.Add(new Label($"공동 공격 진행 · {database.GetCastle(relation.JointAttackTargetCastleId)?.DisplayName.Get(database.UseEnglish)} · " +
                                        $"{relation.JointAttackMonthsRemaining}개월 남음"));
                }
                foreach (WICastleRuntimeState target in state.Castles.Where(item =>
                             item.FactionId != state.PlayerFactionId && item.FactionId != targetFaction.Id &&
                             WIAdministrationTurnSystem.AreFactionsAtWar(state, state.PlayerFactionId, item.FactionId) &&
                             WIAdministrationTurnSystem.AreFactionsAtWar(state, targetFaction.Id, item.FactionId))
                         .GroupBy(item => item.FactionId).Select(group => group.First()))
                {
                    Button jointAttack = new Button(() => ExecuteDiplomaticCommand(
                        WIAdministrationTurnSystem.ProposeJointAttack(database, state, state.PlayerFactionId,
                            targetFaction.Id, target.CastleId), targetFaction));
                    jointAttack.text = $"공동 공격 제안 · {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)} · " +
                                       $"영향력 {database.JointAttackInfluenceCost} · {database.JointAttackDurationMonths}개월";
                    panel.Add(jointAttack);
                }
            }

            if (relation.Status != WIDiplomaticStatus.War)
            {
                Button war = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.DeclareWar(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                war.text = $"선전포고 · 영향력 {WIAdministrationTurnSystem.DeclareWarInfluenceCost}";
                war.tooltip = $"위험한 확정 명령 · 영향력 {WIAdministrationTurnSystem.DeclareWarInfluenceCost}를 소비하고 즉시 전쟁 상태가 됩니다.";
                panel.Add(war);
            }
        }

        // 외교 명령 결과를 반영하고 같은 대상의 최신 관계 화면을 다시 엽니다.
        private void ExecuteDiplomaticCommand(bool succeeded, WIFactionDefinition targetFaction)
        {
            RefreshAll();
            if (succeeded == false)
            {
                ShowMessage("외교 명령 조건이나 자원이 부족합니다.");
                return;
            }

            OpenDiplomacyTargetModal(targetFaction);
        }

        // 외교 상태를 UI용 한국어 이름으로 변환합니다.
        private string GetDiplomaticStatusDisplayName(WIDiplomaticStatus status)
        {
            switch (status)
            {
                case WIDiplomaticStatus.War: return "전쟁";
                case WIDiplomaticStatus.Friendly: return "우호";
                case WIDiplomaticStatus.NonAggression: return "불가침";
                case WIDiplomaticStatus.Alliance: return "동맹";
                default: return "중립";
            }
        }

        // 현재 관계와 다음 행동의 의미를 짧은 문장으로 설명합니다.
        private string GetDiplomaticDescription(WIDiplomaticRelationState relation)
        {
            switch (relation.Status)
            {
                case WIDiplomaticStatus.War: return "교전 중입니다. 적 성으로 원정할 수 있으며 휴전 교섭을 시도할 수 있습니다.";
                case WIDiplomaticStatus.Friendly: return "사절 교류가 안정되었습니다. 불가침 협정을 제안할 수 있습니다.";
                case WIDiplomaticStatus.NonAggression: return "서로 공격하지 않기로 합의했습니다. 동맹 체결을 제안할 수 있습니다.";
                case WIDiplomaticStatus.Alliance: return $"동맹 관계입니다. 금화 원조 재요청 대기 {relation.AidCooldownMonths}개월.";
                default: return "공식 협정이 없는 중립 관계입니다. 친선 사절을 파견할 수 있습니다.";
            }
        }

        // AI 성향을 정세 화면용 한국어 이름으로 변환합니다.
        private string GetAIStrategyDisplayName(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Development: return "개발";
                case WIAIStrategy.Defense: return "수비";
                case WIAIStrategy.Aggressive: return "공세";
                case WIAIStrategy.Scheme: return "모략";
                default: return "부국";
            }
        }

    }
}
