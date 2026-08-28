using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        public const int ImproveRelationsGoldCost = 100;
        public const int ImproveRelationsInfluenceCost = 15;
        public const int NonAggressionInfluenceCost = 25;
        public const int AllianceInfluenceCost = 40;
        public const int DeclareWarInfluenceCost = 10;
        public const int AllianceAidGold = 200;

        // 지정한 성이 현재 플레이어가 직접 명령할 수 있는 소유 영지인지 확인합니다.
        public static bool CanPlayerManageCastle(WIAdministrationState state, WICastleRuntimeState castle)
        {
            return state != null && castle != null && castle.FactionId == state.PlayerFactionId;
        }

        // 플레이어가 참가해야 하는 전투가 남아 있어 다음 턴을 진행할 수 없는지 확인합니다.
        public static bool HasUnresolvedPlayerBattles(WIAdministrationState state)
        {
            return state?.BattleSessions != null && state.BattleSessions.Any(session =>
                session.PlayerInvolved && session.Status != WIBattleSessionStatus.Resolved);
        }

        // 같은 성에 있는 인물 쌍의 관계 단계에 맞는 미발생 사건을 한 건 생성합니다.
        public static void CreateRelationshipEventCandidates(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            if (state.PendingRelationshipEvents.Count > 0)
            {
                return;
            }

            foreach (WIRelationshipState relationship in state.Relationships)
            {
                WICastleRuntimeState castle = state.Castles.Find(item =>
                    item.FactionId == state.PlayerFactionId &&
                    item.HeroIds.Contains(relationship.FirstHeroId) &&
                    item.HeroIds.Contains(relationship.SecondHeroId));
                if (castle == null)
                {
                    continue;
                }

                WIRelationshipEventDefinition definition = database.RelationshipEventDefinitions
                    .FirstOrDefault(item => item.RequiredLevel == relationship.Level);
                if (definition == null)
                {
                    continue;
                }

                string key = GetRelationshipEventKey(definition.Id, relationship.FirstHeroId, relationship.SecondHeroId);
                if (state.CompletedRelationshipEventKeys.Contains(key))
                {
                    continue;
                }

                state.PendingRelationshipEvents.Add(new WIPendingRelationshipEvent
                {
                    EventId = definition.Id,
                    FirstHeroId = relationship.FirstHeroId,
                    SecondHeroId = relationship.SecondHeroId
                });
                summary.News.Add($"관계 사건 발생 · {definition.Title.Get(database.UseEnglish)}");
                return;
            }
        }

        // 관계 사건 선택 결과를 관계·공훈·피로에 적용하고 중복 발생을 막습니다.
        public static bool ResolveRelationshipEvent(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIPendingRelationshipEvent pendingEvent,
            int choiceIndex,
            WITurnSummary summary)
        {
            WIRelationshipEventDefinition definition = pendingEvent == null
                ? null
                : database.GetRelationshipEvent(pendingEvent.EventId);
            if (definition == null || choiceIndex < 0 || choiceIndex >= definition.Choices.Count ||
                state.PendingRelationshipEvents.Contains(pendingEvent) == false)
            {
                return false;
            }

            WIRelationshipEventChoiceDefinition choice = definition.Choices[choiceIndex];
            WIRelationshipState relationship = state.GetOrCreateRelationship(pendingEvent.FirstHeroId, pendingEvent.SecondHeroId);
            relationship.Level = (WIRelationshipLevel)Mathf.Clamp(
                (int)relationship.Level + choice.RelationshipShift,
                (int)WIRelationshipLevel.Conflict,
                (int)WIRelationshipLevel.Fondness);

            foreach (string heroId in new[] { pendingEvent.FirstHeroId, pendingEvent.SecondHeroId })
            {
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (character == null) continue;
                character.Merit = Mathf.Max(0, character.Merit + choice.MeritDelta);
                character.Fatigue = Mathf.Clamp(character.Fatigue + choice.FatigueDelta, 0, 100);
            }

            string key = GetRelationshipEventKey(definition.Id, pendingEvent.FirstHeroId, pendingEvent.SecondHeroId);
            if (state.CompletedRelationshipEventKeys.Contains(key) == false)
            {
                state.CompletedRelationshipEventKeys.Add(key);
            }
            state.PendingRelationshipEvents.Remove(pendingEvent);
            summary?.News.Add($"관계 사건 해결 · {definition.Title.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}");
            return true;
        }

        // 사업 선택 사건의 결과를 성 수치에 적용하고 대기 목록에서 제거합니다.
        public static bool ResolveProjectEvent(
            WIAdministrationState state,
            WIPendingProjectEvent pendingEvent,
            int gain,
            bool boldRisk,
            WITurnSummary summary = null)
        {
            if (state == null || pendingEvent == null || gain <= 0 ||
                state.PendingProjectEvents.Contains(pendingEvent) == false)
            {
                return false;
            }

            WICastleRuntimeState castle = state.GetCastle(pendingEvent.CastleId);
            if (castle == null || castle.FactionId != state.PlayerFactionId)
            {
                return false;
            }

            ApplyProjectEventStat(castle, pendingEvent.ProjectType, gain);
            if (pendingEvent.ProjectType == WICastleProjectType.Recruitment)
            {
                WICharacterRuntimeState character = state.GetCharacter(pendingEvent.HeroId);
                if (character != null)
                {
                    character.Reputation += gain;
                }
            }
            if (boldRisk)
            {
                castle.Stability = Mathf.Clamp(castle.Stability - 2, 0, 100);
            }

            state.PendingProjectEvents.Remove(pendingEvent);
            summary?.News.Add($"사업 사건 해결 · {pendingEvent.Title} · 성과 +{gain}");
            return true;
        }

        // 사업 종류에 맞는 성 능력치를 증가시킵니다.
        private static void ApplyProjectEventStat(
            WICastleRuntimeState castle,
            WICastleProjectType projectType,
            int gain)
        {
            if (projectType == WICastleProjectType.Prosperity)
            {
                castle.Prosperity = Mathf.Clamp(castle.Prosperity + gain, 0, 100);
            }
            else if (projectType == WICastleProjectType.Technology)
            {
                castle.Technology = Mathf.Clamp(castle.Technology + gain, 0, 100);
            }
            else if (projectType == WICastleProjectType.Stability)
            {
                castle.Stability = Mathf.Clamp(castle.Stability + gain, 0, 100);
            }
            else if (projectType == WICastleProjectType.Fortification)
            {
                castle.Defense = Mathf.Clamp(castle.Defense + gain, 0, 100);
            }
        }

        // 인물 순서와 무관한 관계 사건 완료 키를 만듭니다.
        private static string GetRelationshipEventKey(string eventId, string firstHeroId, string secondHeroId)
        {
            return string.CompareOrdinal(firstHeroId, secondHeroId) <= 0
                ? $"{eventId}:{firstHeroId}:{secondHeroId}"
                : $"{eventId}:{secondHeroId}:{firstHeroId}";
        }

        // 지정 진영이 다음 달에 받을 성별 기본 수입과 연구 보너스 합계를 계산합니다.
        public static WITurnSummary GetFactionMonthlyIncome(WIAdministrationDatabaseSO database,
            WIAdministrationState state, string factionId)
        {
            WITurnSummary total = new WITurnSummary();
            WIFactionRuntimeState faction = state.GetFactionState(factionId);
            if (faction == null)
            {
                return total;
            }

            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == factionId))
            {
                WITurnSummary income = new WITurnSummary();
                AddCastleIncome(database.GetCastle(castle.CastleId), castle, income);
                ApplyResearchIncomeBonus(database, faction, income);
                total.GoldGained += income.GoldGained;
                total.ManaGained += income.ManaGained;
                total.InfluenceGained += income.InfluenceGained;
            }
            return total;
        }

        // 친선 비용을 지불해 전쟁을 끝내거나 중립 관계를 우호로 개선합니다.
        public static bool ImproveDiplomaticRelations(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            if (initiator == null || relation == null || initiator.Gold < ImproveRelationsGoldCost ||
                initiator.Influence < ImproveRelationsInfluenceCost ||
                relation.Status != WIDiplomaticStatus.War && relation.Status != WIDiplomaticStatus.Neutral)
            {
                return false;
            }

            initiator.Gold -= ImproveRelationsGoldCost;
            initiator.Influence -= ImproveRelationsInfluenceCost;
            relation.Status = relation.Status == WIDiplomaticStatus.War
                ? WIDiplomaticStatus.Neutral
                : WIDiplomaticStatus.Friendly;
            return true;
        }

        // 우호 진영과 영향력을 사용해 불가침 협정을 체결합니다.
        public static bool SignNonAggression(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            if (initiator == null || relation == null || relation.Status != WIDiplomaticStatus.Friendly ||
                initiator.Influence < NonAggressionInfluenceCost)
            {
                return false;
            }

            initiator.Influence -= NonAggressionInfluenceCost;
            relation.Status = WIDiplomaticStatus.NonAggression;
            return true;
        }

        // 불가침 진영과 영향력을 사용해 동맹을 체결합니다.
        public static bool FormAlliance(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            if (initiator == null || relation == null || relation.Status != WIDiplomaticStatus.NonAggression ||
                initiator.Influence < AllianceInfluenceCost)
            {
                return false;
            }

            initiator.Influence -= AllianceInfluenceCost;
            relation.Status = WIDiplomaticStatus.Alliance;
            relation.AidCooldownMonths = 0;
            return true;
        }

        // 영향력을 지불하고 협정을 파기해 대상 진영에 선전포고합니다.
        public static bool DeclareWar(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            if (initiator == null || relation == null || relation.Status == WIDiplomaticStatus.War ||
                initiator.Influence < DeclareWarInfluenceCost)
            {
                return false;
            }

            initiator.Influence -= DeclareWarInfluenceCost;
            relation.Status = WIDiplomaticStatus.War;
            relation.AidCooldownMonths = 0;
            return true;
        }

        // 동맹국에서 금화 원조를 받고 6개월 재요청 대기 시간을 설정합니다.
        public static bool RequestAllianceAid(WIAdministrationState state, string requesterFactionId, string allyFactionId)
        {
            WIFactionRuntimeState requester = state.GetFactionState(requesterFactionId);
            WIFactionRuntimeState ally = state.GetFactionState(allyFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(requesterFactionId, allyFactionId);
            if (requester == null || ally == null || relation == null || relation.Status != WIDiplomaticStatus.Alliance ||
                relation.AidCooldownMonths > 0 || ally.Gold < AllianceAidGold)
            {
                return false;
            }

            ally.Gold -= AllianceAidGold;
            requester.Gold += AllianceAidGold;
            relation.AidCooldownMonths = 6;
            return true;
        }

        // 원소속 진영이 몸값을 지불해 상대 진영이 억류한 포로 한 명을 즉시 귀환시킵니다.
        public static bool RansomPrisoner(WIAdministrationDatabaseSO database, WIAdministrationState state,
            string requesterFactionId, string prisonerHeroId)
        {
            WICharacterRuntimeState prisoner = state.GetCharacter(prisonerHeroId);
            WIFactionRuntimeState requester = state.GetFactionState(requesterFactionId);
            WIFactionRuntimeState captor = state.GetFactionState(prisoner?.CaptorFactionId);
            WIResourceType resourceType = prisoner != null && prisoner.RansomRequested
                ? prisoner.RansomResourceType : WIResourceType.Gold;
            int amount = prisoner != null && prisoner.RansomRequested
                ? Mathf.Max(1, prisoner.RansomAmount) : database.PrisonerRansomGold;
            if (prisoner == null || requester == null || captor == null || prisoner.Captured == false ||
                prisoner.CapturedFromFactionId != requesterFactionId ||
                GetFactionResource(requester, resourceType) < amount)
            {
                return false;
            }

            AddFactionResource(requester, resourceType, -amount);
            AddFactionResource(captor, resourceType, amount);
            return ReleaseCapturedCharacter(state, prisoner);
        }

        // 진영의 지정 자원 보유량을 반환합니다.
        private static int GetFactionResource(WIFactionRuntimeState faction, WIResourceType resourceType)
        {
            if (resourceType == WIResourceType.ManaCrystal)
            {
                return faction.ManaCrystal;
            }

            if (resourceType == WIResourceType.Influence)
            {
                return faction.Influence;
            }

            return faction.Gold;
        }

        // 진영의 지정 자원을 증감합니다.
        private static void AddFactionResource(WIFactionRuntimeState faction, WIResourceType resourceType, int amount)
        {
            if (resourceType == WIResourceType.ManaCrystal)
            {
                faction.ManaCrystal += amount;
                return;
            }

            if (resourceType == WIResourceType.Influence)
            {
                faction.Influence += amount;
                return;
            }

            faction.Gold += amount;
        }

        // 서로 상대 진영이 억류한 두 포로를 비용 없이 맞교환해 각 원소속 성으로 귀환시킵니다.
        public static bool ExchangePrisoners(WIAdministrationState state, string firstFactionId, string secondFactionId,
            string firstPrisonerHeroId, string secondPrisonerHeroId)
        {
            WICharacterRuntimeState first = state.GetCharacter(firstPrisonerHeroId);
            WICharacterRuntimeState second = state.GetCharacter(secondPrisonerHeroId);
            if (first == null || second == null || first.Captured == false || second.Captured == false ||
                first.CapturedFromFactionId != firstFactionId || first.CaptorFactionId != secondFactionId ||
                second.CapturedFromFactionId != secondFactionId || second.CaptorFactionId != firstFactionId)
            {
                return false;
            }

            return ReleaseCapturedCharacter(state, first) && ReleaseCapturedCharacter(state, second);
        }

        // 동맹 양측이 모두 교전 중인 제3진영의 성을 제한 기간 공동 공격 목표로 지정합니다.
        public static bool ProposeJointAttack(WIAdministrationDatabaseSO database, WIAdministrationState state,
            string proposerFactionId, string allyFactionId, string targetCastleId)
        {
            WIFactionRuntimeState proposer = state.GetFactionState(proposerFactionId);
            WIDiplomaticRelationState alliance = state.GetOrCreateDiplomaticRelation(proposerFactionId, allyFactionId);
            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            if (proposer == null || alliance == null || target == null || alliance.Status != WIDiplomaticStatus.Alliance ||
                target.FactionId == proposerFactionId || target.FactionId == allyFactionId ||
                AreFactionsAtWar(state, proposerFactionId, target.FactionId) == false ||
                AreFactionsAtWar(state, allyFactionId, target.FactionId) == false ||
                proposer.Influence < database.JointAttackInfluenceCost)
            {
                return false;
            }

            proposer.Influence -= database.JointAttackInfluenceCost;
            alliance.JointAttackTargetCastleId = targetCastleId;
            alliance.JointAttackMonthsRemaining = database.JointAttackDurationMonths;
            return true;
        }

        // 두 진영이 현재 전쟁 상태인지 확인합니다.
        public static bool AreFactionsAtWar(WIAdministrationState state, string firstFactionId, string secondFactionId)
        {
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(firstFactionId, secondFactionId);
            return relation != null && relation.Status == WIDiplomaticStatus.War;
        }

        // 영토가 사라진 진영을 한 번만 멸망 처리하고 잔존 행동·전투단·인물·외교 약속을 정리합니다.
        public static void ResolveFactionEliminations(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIFactionRuntimeState faction in state.Factions.Where(item => item.Eliminated == false).ToList())
            {
                if (state.Castles.Any(castle => castle.FactionId == faction.FactionId)) continue;
                faction.Eliminated = true;
                faction.EliminatedTurn = state.Turn;
                faction.ActiveResearchId = string.Empty;
                faction.ResearcherHeroId = string.Empty;
                faction.ResearchRemainingMonths = 0;

                HashSet<string> removedCharacterIds = new HashSet<string>(state.Armies
                    .Where(army => army.FactionId == faction.FactionId)
                    .SelectMany(army => army.Members).Select(member => member.HeroId));
                foreach (WICharacterRuntimeState prisoner in state.Characters.Where(character =>
                             character.Captured && character.CapturedFromFactionId == faction.FactionId))
                {
                    removedCharacterIds.Add(prisoner.HeroId);
                }
                foreach (WICharacterRuntimeState releasedPrisoner in state.Characters.Where(character =>
                             character.Captured && character.CaptorFactionId == faction.FactionId).ToList())
                {
                    ReleaseCapturedCharacter(state, releasedPrisoner);
                }
                state.Armies.RemoveAll(army => army.FactionId == faction.FactionId);
                state.BattleSessions.RemoveAll(session => session.AttackerFactionId == faction.FactionId ||
                                                         session.DefenderFactionId == faction.FactionId);
                state.SchemeMissions.RemoveAll(mission => mission.InitiatorFactionId == faction.FactionId);
                foreach (WIDiplomaticRelationState relation in state.DiplomaticRelations.Where(relation =>
                             relation.FirstFactionId == faction.FactionId || relation.SecondFactionId == faction.FactionId))
                {
                    relation.JointAttackTargetCastleId = string.Empty;
                    relation.JointAttackMonthsRemaining = 0;
                    relation.AidCooldownMonths = 0;
                }
                foreach (string heroId in removedCharacterIds)
                {
                    foreach (WICastleRuntimeState castle in state.Castles) castle.HeroIds.Remove(heroId);
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    if (character == null) continue;
                    character.Recruited = false;
                    character.Captured = false;
                    character.CaptorFactionId = string.Empty;
                    character.CapturedFromFactionId = string.Empty;
                    character.CapturedMonthsRemaining = 0;
                    character.Activity = WICharacterActivityType.None;
                }

                WIFactionDefinition definition = database.GetFaction(faction.FactionId);
                WIFactionEliminationNarrativeDefinition narrative = database.GetFactionEliminationNarrative(faction.FactionId);
                string title = narrative?.Title.Get(database.UseEnglish) ?? "진영 멸망";
                string description = narrative?.Description.Get(database.UseEnglish) ?? "모든 영토를 상실했습니다.";
                summary?.News.Add($"진영 멸망 · {definition?.DisplayName.Get(database.UseEnglish) ?? faction.FactionId} · " +
                                  $"{title} · 제 {state.Turn}턴 · {description}");
            }
        }

        // 한 턴의 AI 판단 근거를 진영·분야별 한 건으로 제한해 월간 보고에 추가합니다.
        public static void AddAIReasonReport(WITurnSummary summary, string factionId, string factionName,
            string category, string reason)
        {
            if (summary == null) return;
            summary.AIReasonReports = summary.AIReasonReports ?? new List<string>();
            if (summary.AIReasonReports.Count >= 12) return;
            string prefix = $"[AI 판단] {factionId} · {category} ·";
            if (summary.AIReasonReports.Any(item => item.StartsWith(prefix))) return;
            summary.AIReasonReports.Add($"{prefix} {factionName} · {reason}");
        }

        // 난이도의 AI 후보 범위 안에서 턴과 고정 소금값으로 재현 가능한 선택 순위를 반환합니다.
        public static int GetAICandidateIndex(WIAdministrationDatabaseSO database, WIAdministrationState state,
            int candidateCount, int salt)
        {
            if (candidateCount <= 1) return 0;
            int window = Mathf.Clamp(database.GetDifficulty(state.Difficulty)?.AICandidateWindow ?? 2, 1, candidateCount);
            return Mathf.Abs(state.Turn + salt) % window;
        }

        // 현재 난이도가 AI에게 허용하는 상위 후보 범위를 반환합니다.
        public static int GetAICandidateWindow(WIAdministrationDatabaseSO database, WIAdministrationState state,
            int candidateCount)
        {
            return Mathf.Clamp(database.GetDifficulty(state.Difficulty)?.AICandidateWindow ?? 2, 1, Mathf.Max(1, candidateCount));
        }

        // 외교 원조 대기 시간을 줄이고 AI 진영의 제한적인 관계 개선을 처리합니다.
        private static void ResolveDiplomaticTurn(WIAdministrationDatabaseSO database, WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WIDiplomaticRelationState relation in state.DiplomaticRelations)
            {
                relation.AidCooldownMonths = Mathf.Max(0, relation.AidCooldownMonths - 1);
                relation.JointAttackMonthsRemaining = Mathf.Max(0, relation.JointAttackMonthsRemaining - 1);
                if (relation.JointAttackMonthsRemaining == 0) relation.JointAttackTargetCastleId = string.Empty;
            }

            if (state.Turn % 4 != 0)
            {
                return;
            }

            foreach (WIFactionDefinition faction in database.Factions.Where(item => item.PlayerFaction == false))
            {
                if (faction.AIStrategy == WIAIStrategy.Aggressive)
                {
                    continue;
                }

                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                if (factionState == null || factionState.Eliminated) continue;
                WIDiplomaticRelationState relation = state.DiplomaticRelations.FirstOrDefault(item =>
                    (item.FirstFactionId == faction.Id || item.SecondFactionId == faction.Id) &&
                    item.Status == WIDiplomaticStatus.Neutral);
                if (relation == null || factionState.Influence < 10)
                {
                    continue;
                }

                factionState.Influence -= 10;
                relation.Status = WIDiplomaticStatus.Friendly;
                string counterpartId = relation.FirstFactionId == faction.Id ? relation.SecondFactionId : relation.FirstFactionId;
                summary.News.Add($"대륙 정세 · {faction.DisplayName.Get(database.UseEnglish)}와 {database.GetFaction(counterpartId).DisplayName.Get(database.UseEnglish)}의 관계가 우호로 개선됨");
                AddAIReasonReport(summary, faction.Id, faction.DisplayName.Get(database.UseEnglish), "외교",
                    $"중립 관계를 개선해 고립을 완화 · 영향력 10 사용 · 대상 {database.GetFaction(counterpartId).DisplayName.Get(database.UseEnglish)}");
                break;
            }
        }

        // 같은 진영의 인접 성으로 인물의 한 달 이동을 시작합니다.
        public static bool StartCharacterTransfer(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string heroId,
            string targetCastleId)
        {
            WICastleRuntimeState origin = state.Castles.FirstOrDefault(castle => castle.HeroIds.Contains(heroId));
            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            int reservedSlots = state.CharacterTransfers.Count(transfer => transfer.TargetCastleId == targetCastleId);
            if (origin == null || target == null || origin.CastleId == targetCastleId ||
                origin.FactionId != target.FactionId ||
                origin.AdjacentCastleIds.Contains(targetCastleId) == false ||
                target.HeroIds.Count + reservedSlots >= target.GetHeroSlotCount() ||
                state.IsCharacterBusy(heroId) || origin.GovernorHeroId == heroId)
            {
                return false;
            }

            origin.HeroIds.Remove(heroId);
            state.CharacterTransfers.Add(new WICharacterTransferState
            {
                HeroId = heroId,
                OriginCastleId = origin.CastleId,
                TargetCastleId = targetCastleId,
                RemainingMonths = 1
            });
            return true;
        }

        // 성에 주둔한 유휴 인물을 영지관로 임명하거나 기존 영지관을 교체합니다.
        public static bool AssignGovernor(WIAdministrationState state, string castleId, string heroId)
        {
            WICastleRuntimeState castle = state.GetCastle(castleId);
            if (castle == null || castle.HeroIds.Contains(heroId) == false ||
                state.IsCharacterBusy(heroId) || IsAdministrationCapable(state, heroId) == false)
            {
                return false;
            }

            foreach (WICastleRuntimeState other in state.Castles.Where(item => item.GovernorHeroId == heroId))
            {
                other.GovernorHeroId = string.Empty;
                other.DelegatedToGovernor = false;
            }
            castle.GovernorHeroId = heroId;
            return true;
        }

        // 태생 영웅 또는 승격한 인물만 내정 업무를 맡을 수 있는지 확인합니다.
        public static bool IsAdministrationCapable(WIAdministrationState state, string heroId)
        {
            WICharacterRuntimeState character = state?.GetCharacter(heroId);
            return character != null &&
                   (character.BaseGrade == WICharacterGrade.Hero || character.PromotedToHero);
        }

        // 개인 활동은 내정 인력인 태생 영웅 또는 승격 영웅만 수행할 수 있습니다.
        public static bool CanPerformCharacterActivity(
            WIAdministrationState state,
            string heroId,
            WICharacterActivityType activity)
        {
            return IsAdministrationCapable(state, heroId);
        }

        // 이동 시간이 끝난 인물을 목적지 성에 배치합니다.
        private static void ResolveCharacterTransfers(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterTransferState transfer in state.CharacterTransfers.ToList())
            {
                transfer.RemainingMonths -= 1;
                if (transfer.RemainingMonths > 0)
                {
                    continue;
                }

                WICastleRuntimeState target = state.GetCastle(transfer.TargetCastleId);
                if (target != null && target.HeroIds.Count < target.GetHeroSlotCount())
                {
                    target.HeroIds.Add(transfer.HeroId);
                    string heroName = database.GetHero(transfer.HeroId).DisplayName.Get(database.UseEnglish);
                    string castleName = database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish);
                    summary.News.Add($"{heroName} 이동 완료 · {castleName}");
                }
                else
                {
                    state.GetCastle(transfer.OriginCastleId)?.HeroIds.Add(transfer.HeroId);
                    summary.News.Add($"인물 이동 취소 · 목적지 주둔 슬롯 부족");
                }
                state.CharacterTransfers.Remove(transfer);
            }
        }

        // 성에 지정된 월간 중점 사업을 계산하고 관련 성 수치를 반영합니다.
        private static void ResolveCastleProject(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WITurnSummary summary)
        {
            WICastleProjectState project = castleState.ActiveProject;
            if (project == null)
            {
                return;
            }

            WIHeroDefinition manager = database.GetHero(project.ManagerHeroId);
            WIFactionRuntimeState factionState = state.GetFactionState(castleState.FactionId);
            WIFactionPolicy policy = factionState == null ? WIFactionPolicy.Prosperity : factionState.Policy;
            int gain = GetExpectedProjectGain(database, project.ProjectType, manager, project.Investment) +
                       GetFactionPolicyBonus(database, policy, project.ProjectType) +
                       GetResearchEffectBonus(database, factionState, WIResearchEffectType.ProjectGain) +
                       GetTitleProjectBonus(database, state, manager) +
                       GetLegacyBonus(castleState, project.ProjectType) +
                       GetCastleSpecialtyProjectBonus(database.GetCastle(castleState.CastleId), project.ProjectType);

            ApplyProjectGain(castleState, project.ProjectType, gain);
            project.RemainingMonths -= 1;
            if (project.RemainingMonths > 0)
            {
                return;
            }

            ApplyTraitUniqueEffects(database, state, castleState, project, manager, summary);

            // 확장 사업이 완료되면 성 규모를 한 단계 올립니다.
            if (project.ProjectType == WICastleProjectType.Expansion &&
                castleState.CastleSize < WICastleSize.Large)
            {
                castleState.CastleSize += 1;
                castleState.PendingSpecialFacilityChoice = true;
            }

            if (project.Delegated)
            {
                string castleName = database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish);
                summary.DelegationReports.Add($"[위임 결과] {castleName} · {project.ProjectType} · 예상 +{project.ExpectedGain} / 실제 +{gain} · 비용 {project.GoldCost}G");
            }

            WICastleDefinition castle = database.GetCastle(castleState.CastleId);
            string managerName = manager == null
                ? database.GetText("UI_NO_MANAGER")
                : manager.DisplayName.Get(database.UseEnglish);
            string resultText = project.ProjectType == WICastleProjectType.Expansion
                ? $"{castleState.CastleSize} 규모 확장 완료 · 특화 시설 선택 가능"
                : $"{project.ProjectType} +{gain}";
            WIFactionDefinition faction = database.GetFaction(castleState.FactionId);
            if (faction != null && faction.PlayerFaction)
            {
                summary.News.Add($"{castle.DisplayName.Get(database.UseEnglish)} · {resultText} · {managerName}");
                CreateProjectEventAndLegacyChoice(database, state, castleState, project, manager, gain, summary);
            }
            else
            {
                summary.AIProjectsCompleted += 1;
                if (summary.AIProjectFactionIds.Contains(faction.Id) == false)
                {
                    summary.AIProjectFactionIds.Add(faction.Id);
                }
                if (IsAdjacentToPlayer(database, state, castleState))
                {
                    summary.News.Add($"AI 동향 · {castle.DisplayName.Get(database.UseEnglish)}에서 {project.ProjectType} 사업 완료");
                }
            }
            castleState.ActiveProject = null;
        }

        // 사업과 일치하는 담당 인물 특기의 고유 결과를 적용하고 월간 보고 문구를 추가합니다.
        public static void ApplyTraitUniqueEffects(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WICastleProjectState project,
            WIHeroDefinition manager,
            WITurnSummary summary)
        {
            if (manager == null || castleState == null || project == null)
            {
                return;
            }

            WIFactionRuntimeState faction = state.GetFactionState(castleState.FactionId);
            foreach (WITraitType traitType in manager.Traits)
            {
                WITraitDefinition trait = database.GetTrait(traitType);
                if (trait == null || trait.ProjectTypes.Contains(project.ProjectType) == false)
                {
                    continue;
                }

                int value = Mathf.Max(1, trait.UniqueEffectValue);
                string result = string.Empty;
                switch (traitType)
                {
                    case WITraitType.Agronomist:
                        foreach (WIArmyState army in state.Armies.Where(item =>
                                     item.FactionId == castleState.FactionId && item.CurrentCastleId == castleState.CastleId))
                        {
                            army.Supply = WISupplyState.Sufficient;
                        }
                        result = "주둔 전투단 보급 충분";
                        break;
                    case WITraitType.Merchant:
                        if (faction != null && faction.FactionId == state.PlayerFactionId) summary.GoldGained += value;
                        else if (faction != null) faction.Gold += value;
                        result = $"추가 금화 +{value}";
                        break;
                    case WITraitType.Architect:
                        int cost = GetProjectCost(database, project.ProjectType, project.Investment);
                        int refund = Mathf.Max(1, cost * value / 100);
                        if (faction != null && faction.FactionId == state.PlayerFactionId) summary.GoldGained += refund;
                        else if (faction != null) faction.Gold += refund;
                        result = $"공사비 절감 {refund}G";
                        break;
                    case WITraitType.Constable:
                        castleState.CounterintelligenceMonths = Mathf.Max(castleState.CounterintelligenceMonths, value);
                        result = $"방첩 강화 {value}개월";
                        break;
                    case WITraitType.Scholar:
                        if (faction != null && string.IsNullOrEmpty(faction.ActiveResearchId) == false)
                        {
                            faction.ResearchRemainingMonths = Mathf.Max(0, faction.ResearchRemainingMonths - value);
                            result = $"연구 기간 {value}개월 단축";
                        }
                        else
                        {
                            if (faction != null && faction.FactionId == state.PlayerFactionId) summary.ManaGained += value;
                            else if (faction != null) faction.ManaCrystal += value;
                            result = $"연구 마나 +{value}";
                        }
                        break;
                    case WITraitType.Negotiator:
                        if (faction != null && faction.FactionId == state.PlayerFactionId) summary.InfluenceGained += value;
                        else if (faction != null) faction.Influence += value;
                        result = $"교섭 영향력 +{value}";
                        break;
                    case WITraitType.Doctor:
                        foreach (string heroId in castleState.HeroIds)
                        {
                            WICharacterRuntimeState character = state.GetCharacter(heroId);
                            if (character == null) continue;
                            character.InjuryMonths = Mathf.Max(0, character.InjuryMonths - value);
                            character.Fatigue = Mathf.Max(0, character.Fatigue - value * 10);
                        }
                        result = $"주둔 인물 부상 {value}개월·피로 {value * 10} 회복";
                        break;
                    case WITraitType.Instructor:
                        foreach (WIArmyState army in state.Armies.Where(item =>
                                     item.FactionId == castleState.FactionId && item.CurrentCastleId == castleState.CastleId))
                        {
                            army.CohesionExperience += value;
                            UpdateArmyProficiency(army);
                        }
                        result = $"주둔 전투단 숙련 경험 +{value}";
                        break;
                }

                if (string.IsNullOrEmpty(result) == false && faction != null && faction.FactionId == state.PlayerFactionId)
                {
                    summary.News.Add($"특기 발동 · {trait.DisplayName.Get(database.UseEnglish)} · {result}");
                }
            }
        }

        // 주둔 인물을 대장으로 지정해 새 전투단을 생성합니다.
        public static WIArmyState CreateArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            string commanderHeroId)
        {
            if (castle == null || castle.HeroIds.Contains(commanderHeroId) == false || state.IsCharacterBusy(commanderHeroId))
            {
                return null;
            }

            WIArmyState army = new WIArmyState
            {
                ArmyId = $"army_{state.NextArmyNumber}",
                DisplayName = $"제 {state.NextArmyNumber} 전투단",
                FactionId = castle.FactionId,
                CurrentCastleId = castle.CastleId
            };
            army.Members.Add(new WIArmyMemberState { HeroId = commanderHeroId, Role = WIUnitRole.Commander });
            state.NextArmyNumber += 1;
            state.Armies.Add(army);
            return army;
        }

        // 같은 성의 대기 인물을 지정된 역할로 전투단에 추가합니다.
        public static bool AddArmyMember(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            string heroId,
            WIUnitRole role)
        {
            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0 || castle == null ||
                castle.HeroIds.Contains(heroId) == false || state.IsCharacterBusy(heroId))
            {
                return false;
            }

            if (army.Members.Count >= GetRecommendedArmySize(database, army))
            {
                return false;
            }

            army.Members.Add(new WIArmyMemberState { HeroId = heroId, Role = role });
            return true;
        }

        // 대장이 아닌 전투단원을 제외하고 편성 변경에 따른 합동 경험을 감소시킵니다.
        public static bool RemoveArmyMember(
            WIAdministrationState state,
            WIArmyState army,
            string heroId)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0 || army.JointTrainingScheduled)
            {
                return false;
            }

            WIArmyMemberState member = army.Members.Find(item => item.HeroId == heroId);
            if (member == null || member.Role == WIUnitRole.Commander)
            {
                return false;
            }

            army.Members.Remove(member);
            army.CohesionExperience = Mathf.Max(0, army.CohesionExperience - 20);
            UpdateArmyProficiency(army);
            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (castle != null && castle.HeroIds.Contains(heroId) == false)
            {
                castle.HeroIds.Add(heroId);
            }

            return true;
        }

        // 주둔 중인 전투단을 해산하고 모든 구성원을 현재 성으로 복귀시킵니다.
        public static bool DisbandArmy(WIAdministrationState state, WIArmyState army)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0)
            {
                return false;
            }

            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (castle == null)
            {
                return false;
            }

            foreach (WIArmyMemberState member in army.Members)
            {
                if (castle.HeroIds.Contains(member.HeroId) == false)
                {
                    castle.HeroIds.Add(member.HeroId);
                }
            }

            state.Armies.Remove(army);
            return true;
        }

        // 주둔 중인 전투단에 다음 달 합동 훈련을 예약합니다.
        public static bool ScheduleJointTraining(WIArmyState army)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0 || army.JointTrainingScheduled)
            {
                return false;
            }

            army.JointTrainingScheduled = true;
            return true;
        }

        // 대장의 통솔에 따른 현재 전투단의 권장 인원 상한을 반환합니다.
        public static int GetRecommendedArmySize(WIAdministrationDatabaseSO database, WIArmyState army)
        {
            WIArmyMemberState commander = army.Members.Find(member => member.Role == WIUnitRole.Commander);
            WIHeroDefinition hero = commander == null ? null : database.GetHero(commander.HeroId);
            return hero == null ? 1 : Mathf.Clamp(3 + hero.Leadership / 20, 4, 8);
        }

        // 인접 성을 목표로 월간 이동을 시작합니다.
        public static bool BeginArmyMarch(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            string targetCastleId)
        {
            if (army == null || army.IsMoving || army.AwaitingBattle || army.ReorganizationMonths > 0)
            {
                return false;
            }

            WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
            if (origin == null || origin.AdjacentCastleIds.Contains(targetCastleId) == false)
            {
                return false;
            }

            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            WIFactionRuntimeState factionState = state.GetFactionState(army.FactionId);
            if (target == null || factionState == null)
            {
                return false;
            }

            if (target.FactionId != army.FactionId)
            {
                const int influenceCost = 20;
                if (AreFactionsAtWar(state, army.FactionId, target.FactionId) == false || factionState.Influence < influenceCost)
                {
                    return false;
                }
                factionState.Influence -= influenceCost;
            }

            army.Supply = GetArmySupplyState(origin);
            army.OriginCastleId = origin.CastleId;
            army.TargetCastleId = targetCastleId;
            army.RemainingTravelMonths = army.Supply == WISupplyState.Sufficient ? 1 : 2;
            foreach (WIArmyMemberState member in army.Members)
            {
                origin.HeroIds.Remove(member.HeroId);
                if (origin.GovernorHeroId == member.HeroId)
                {
                    origin.GovernorHeroId = string.Empty;
                    origin.DelegatedToGovernor = false;
                }
            }

            return true;
        }

        // 이동 중인 전투단의 남은 기간, 도착, 보급과 숙련을 처리합니다.
        private static void ResolveArmyMovement(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIArmyState army in state.Armies)
            {
                if (army.IsMoving == false)
                {
                    continue;
                }

                army.RemainingTravelMonths -= 1;
                if (army.RemainingTravelMonths > 0)
                {
                    summary.News.Add($"{army.DisplayName} 이동 중 · 남은 기간 {army.RemainingTravelMonths}개월");
                    continue;
                }

                WICastleRuntimeState target = state.GetCastle(army.TargetCastleId);
                army.CurrentCastleId = army.TargetCastleId;
                army.CohesionExperience += 10;
                UpdateArmyProficiency(army);
                ApplyMarchFatigue(state, army);

                if (target.FactionId == army.FactionId)
                {
                    foreach (WIArmyMemberState member in army.Members)
                    {
                        if (target.HeroIds.Contains(member.HeroId) == false)
                        {
                            target.HeroIds.Add(member.HeroId);
                        }
                    }

                    summary.News.Add($"{army.DisplayName} · {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)} 도착");
                }
                else
                {
                    army.AwaitingBattle = true;
                    summary.News.Add($"{army.DisplayName} · {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)} 성외 도착 · 전투 대기");
                }

                army.TargetCastleId = string.Empty;
            }
        }

        // 예약된 합동 훈련을 처리해 전투단 숙련과 구성원 경험을 높입니다.
        private static void ResolveArmyTraining(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIArmyState army in state.Armies)
            {
                if (army.JointTrainingScheduled == false)
                {
                    continue;
                }

                army.JointTrainingScheduled = false;
                if (army.IsMoving || army.AwaitingBattle)
                {
                    continue;
                }

                army.CohesionExperience += 20;
                foreach (WIArmyMemberState member in army.Members)
                {
                    WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                    character.Experience += 8;
                    character.Fatigue = Mathf.Clamp(character.Fatigue + 10, 0, 100);
                }

                UpdateArmyProficiency(army);
                summary.News.Add($"{army.DisplayName} · 합동 훈련 완료 · 숙련 {army.Proficiency}");
            }
        }

        // 합동 경험 수치에 따라 전투단 숙련 단계를 갱신합니다.
        private static void UpdateArmyProficiency(WIArmyState army)
        {
            army.Proficiency = army.CohesionExperience >= 80
                ? WIUnitProficiency.Elite
                : (army.CohesionExperience >= 30 ? WIUnitProficiency.Trained : WIUnitProficiency.Rookie);
        }

        // 전투 대기 중인 전투단과 성 수비 전력을 비교해 임시 전략 전투 결과를 결정합니다.
        private static void ResolveStrategicBattles(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            List<WIArmyState> attackers = state.Armies.Where(army => army.AwaitingBattle).ToList();
            foreach (WIArmyState attacker in attackers)
            {
                bool alreadyDefending = state.BattleSessions.Any(item =>
                    item.Status != WIBattleSessionStatus.Resolved && item.DefenderArmyIds.Contains(attacker.ArmyId));
                if (alreadyDefending)
                {
                    continue;
                }
                WICastleRuntimeState castle = state.GetCastle(attacker.CurrentCastleId);
                if (castle == null || castle.FactionId == attacker.FactionId)
                {
                    attacker.AwaitingBattle = false;
                    continue;
                }

                WIBattleSessionState session = state.BattleSessions.FirstOrDefault(item =>
                    item.AttackerArmyId == attacker.ArmyId && item.Status != WIBattleSessionStatus.Resolved);
                if (session == null)
                {
                    session = CreateBattleSession(database, state, attacker);
                }
                bool useFallback = session != null && state.UseStrategicBattleFallback &&
                                   (session.PlayerInvolved == false || state.UsePlayerRealTimeBattles == false);
                if (useFallback)
                {
                    WIBattleOutcome outcome = session.AttackerPowerSnapshot > session.DefenderPowerSnapshot
                        ? WIBattleOutcome.Victory
                        : WIBattleOutcome.Defeat;
                    SubmitBattleResult(database, state, session.SessionId, outcome, WIBattleResolutionSource.StrategicFallback, summary);
                }
            }
        }

        // 전투 진입 시점의 참가 전투단과 전력을 고정한 세션 데이터를 생성합니다.
        public static WIBattleSessionState CreateBattleSession(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState attacker)
        {
            if (attacker == null || attacker.AwaitingBattle == false)
            {
                return null;
            }
            WICastleRuntimeState castle = state.GetCastle(attacker.CurrentCastleId);
            if (castle == null || castle.FactionId == attacker.FactionId)
            {
                return null;
            }
            List<WIArmyState> defenders = state.Armies.Where(army =>
                army != attacker && army.FactionId == castle.FactionId &&
                army.CurrentCastleId == castle.CastleId && army.IsOperational).ToList();
            WIArmyState counterAttacker = state.Armies.FirstOrDefault(army =>
                army != attacker && army.AwaitingBattle && army.FactionId == castle.FactionId &&
                army.CurrentCastleId == attacker.OriginCastleId && army.OriginCastleId == castle.CastleId);
            if (counterAttacker != null && defenders.Contains(counterAttacker) == false)
            {
                defenders.Add(counterAttacker);
            }
            WIBattleSessionState session = new WIBattleSessionState
            {
                SessionId = $"battle_{state.NextBattleSessionNumber}",
                CastleId = castle.CastleId,
                AttackerArmyId = attacker.ArmyId,
                CounterAttackerArmyId = counterAttacker?.ArmyId ?? string.Empty,
                AttackerFactionId = attacker.FactionId,
                DefenderFactionId = castle.FactionId,
                AttackerPowerSnapshot = GetArmyBattlePower(database, state, attacker),
                DefenderPowerSnapshot = GetCastleDefensePower(database, state, castle, defenders),
                PlayerInvolved = attacker.FactionId == state.PlayerFactionId || castle.FactionId == state.PlayerFactionId,
                Status = WIBattleSessionStatus.Pending
            };
            session.DefenderArmyIds.AddRange(defenders.Select(army => army.ArmyId));
            session.AttackerHeroIds.AddRange(attacker.Members.Select(member => new WIBattleParticipantState
            {
                HeroId = member.HeroId,
                ArmyId = attacker.ArmyId,
                Role = member.Role
            }));
            foreach (WIArmyState defender in defenders)
            {
                session.DefenderHeroIds.AddRange(defender.Members.Select(member => new WIBattleParticipantState
                {
                    HeroId = member.HeroId,
                    ArmyId = defender.ArmyId,
                    Role = member.Role
                }));
            }
            IEnumerable<string> assignedHeroIds = state.Armies.SelectMany(army => army.Members.Select(member => member.HeroId));
            session.DefenderHeroIds.AddRange(castle.HeroIds
                .Where(heroId => assignedHeroIds.Contains(heroId) == false)
                .Select(heroId => new WIBattleParticipantState
                {
                    HeroId = heroId,
                    ArmyId = string.Empty,
                    Role = WIUnitRole.Melee
                }));
            state.NextBattleSessionNumber += 1;
            state.BattleSessions.Add(session);
            return session;
        }

        // 전투 씬이 사용할 세션을 진행 중 상태로 전환합니다.
        public static bool BeginRealTimeBattle(WIAdministrationState state, string sessionId)
        {
            WIBattleSessionState session = state.BattleSessions.Find(item => item.SessionId == sessionId);
            if (session == null || session.Status != WIBattleSessionStatus.Pending)
            {
                return false;
            }
            session.Status = WIBattleSessionStatus.InProgress;
            return true;
        }

        // 전략 판정 또는 실시간 전투 결과를 캠페인 상태에 동일한 규칙으로 반영합니다.
        public static bool SubmitBattleResult(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string sessionId,
            WIBattleOutcome attackerOutcome,
            WIBattleResolutionSource source,
            WITurnSummary summary)
        {
            WIBattleSessionState session = state.BattleSessions.Find(item => item.SessionId == sessionId);
            if (session == null || session.Status == WIBattleSessionStatus.Resolved || attackerOutcome == WIBattleOutcome.None)
            {
                return false;
            }
            WIArmyState attacker = state.Armies.Find(army => army.ArmyId == session.AttackerArmyId);
            WICastleRuntimeState castle = state.GetCastle(session.CastleId);
            if (attacker == null || castle == null)
            {
                return false;
            }
            List<WIArmyState> defenders = session.DefenderArmyIds
                .Select(id => state.Armies.Find(army => army.ArmyId == id))
                .Where(army => army != null).ToList();
            int margin = Mathf.Abs(session.AttackerPowerSnapshot - session.DefenderPowerSnapshot);
            attacker.LastBattlePower = session.AttackerPowerSnapshot;
            if (attackerOutcome == WIBattleOutcome.Victory)
            {
                foreach (WIArmyState defender in defenders)
                {
                    defender.LastBattlePower = GetArmyBattlePower(database, state, defender);
                    defender.LastBattleOutcome = WIBattleOutcome.Defeat;
                    RetreatAndReorganizeArmy(database, state, defender, castle.CastleId, margin,
                        session.DefenderRetreated, session.AttackerFactionId, summary);
                }
                attacker.LastBattleOutcome = WIBattleOutcome.Victory;
                ApplyBattleConsequences(database, state, attacker, true, margin, false, summary);
                ApplyBattleRelationshipConsequences(database, state, attacker, summary);
                ResolveArmyVictoryAndOccupation(database, state, attacker, summary);
                summary?.News.Add($"전투 결과 · {database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish)} 점령 · {attacker.DisplayName} 승리 ({session.AttackerPowerSnapshot}:{session.DefenderPowerSnapshot})");
            }
            else
            {
                attacker.LastBattleOutcome = WIBattleOutcome.Defeat;
                RetreatAndReorganizeArmy(database, state, attacker, castle.CastleId, margin,
                    session.AttackerRetreated, session.DefenderFactionId, summary);
                foreach (WIArmyState defender in defenders)
                {
                    defender.LastBattleOutcome = WIBattleOutcome.Victory;
                    if (defender.ArmyId == session.CounterAttackerArmyId)
                    {
                        defender.AwaitingBattle = false;
                    }
                    ApplyBattleConsequences(database, state, defender, true, margin, false, summary);
                    ApplyBattleRelationshipConsequences(database, state, defender, summary);
                    if (defender.ArmyId == session.CounterAttackerArmyId)
                    {
                        ResolveArmyVictoryAndOccupation(database, state, defender, summary);
                    }
                }
                summary?.News.Add($"전투 결과 · {database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish)} 방어 성공 · {attacker.DisplayName} 후퇴 ({session.AttackerPowerSnapshot}:{session.DefenderPowerSnapshot})");
            }
            session.AttackerOutcome = attackerOutcome;
            session.ResolutionSource = source;
            session.Status = WIBattleSessionStatus.Resolved;
            return true;
        }

        // 인물 능력, 역할, 숙련, 보급과 피로를 조합해 전투단 전투력을 계산합니다.
        public static int GetArmyBattlePower(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army)
        {
            int power = 0;
            foreach (WIArmyMemberState member in army.Members)
            {
                WIHeroDefinition hero = database.GetHero(member.HeroId);
                WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                if (hero == null || character == null) continue;
                int roleBonus = member.Role == WIUnitRole.Commander ? hero.Leadership / 2 : 10;
                int injuryPenalty = character.InjuryMonths > 0 ? 30 : 0;
                power += Mathf.Max(1, hero.Might + roleBonus - character.Fatigue / 2 - injuryPenalty);
            }
            power += army.Proficiency == WIUnitProficiency.Elite ? 40 : (army.Proficiency == WIUnitProficiency.Trained ? 20 : 0);
            power += army.CohesionExperience / 2;
            power += GetResearchEffectBonus(database, state.GetFactionState(army.FactionId), WIResearchEffectType.BattlePower);
            foreach (WIArmyMemberState member in army.Members)
            {
                WITitleDefinition title = database.GetTitle(state.GetCharacter(member.HeroId)?.TitleId);
                power += title == null ? 0 : title.BattlePowerBonus;
            }
            if (army.Supply == WISupplyState.Shortage) power = power * 80 / 100;
            if (army.Supply == WISupplyState.Depleted) power = power * 60 / 100;
            return Mathf.Max(1, power);
        }

        // 성 방어도와 주둔 인물 및 수비 전투단 전투력을 합산합니다.
        public static int GetCastleDefensePower(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            List<WIArmyState> defenders)
        {
            WICastleDefinition castleDefinition = database.GetCastle(castle.CastleId);
            int specialtyDefense = castleDefinition != null && castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.DefensePower
                ? castleDefinition.SpecialtyEffectValue : 0;
            int power = castle.Defense * database.CastleDefensePowerPercent / 100 +
                        castle.Stability * database.CastleStabilityPowerPercent / 100 + specialtyDefense;
            foreach (string heroId in castle.HeroIds.Where(id => state.Armies.All(army => army.Members.All(member => member.HeroId != id))))
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (hero != null && character != null && character.InjuryMonths <= 0)
                {
                    int heroPower = Mathf.Max(1, hero.Might - character.Fatigue / 2);
                    power += heroPower * database.GarrisonHeroPowerPercent / 100;
                }
            }
            power += defenders.Sum(army => GetArmyBattlePower(database, state, army));
            return Mathf.Max(1, power);
        }

        // 패배 전투단을 가까운 아군 성으로 후퇴시키고 한 달간 재편성 상태로 전환합니다.
        private static void RetreatAndReorganizeArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            string battleCastleId,
            int defeatMargin,
            bool orderlyRetreat,
            string captorFactionId,
            WITurnSummary summary)
        {
            string retreatId = string.Empty;
            if (string.IsNullOrEmpty(army.OriginCastleId) == false && state.GetCastle(army.OriginCastleId)?.FactionId == army.FactionId)
            {
                retreatId = army.OriginCastleId;
            }
            if (string.IsNullOrEmpty(retreatId))
            {
                retreatId = state.GetCastle(battleCastleId)?.AdjacentCastleIds
                    .FirstOrDefault(id => state.GetCastle(id)?.FactionId == army.FactionId);
            }
            if (string.IsNullOrEmpty(retreatId))
            {
                retreatId = state.Castles.FirstOrDefault(castle => castle.FactionId == army.FactionId)?.CastleId;
            }

            army.AwaitingBattle = false;
            army.RemainingTravelMonths = 0;
            army.TargetCastleId = string.Empty;
            army.CurrentCastleId = retreatId;
            army.Mission = WIArmyMission.Reserve;
            army.StrategicTargetCastleId = string.Empty;
            army.ReorganizationMonths = 1;
            WICastleRuntimeState battleCastleState = state.GetCastle(battleCastleId);
            WICastleRuntimeState retreatCastle = state.GetCastle(retreatId);
            foreach (WIArmyMemberState member in army.Members)
            {
                battleCastleState?.HeroIds.Remove(member.HeroId);
                if (retreatCastle != null && retreatCastle.HeroIds.Contains(member.HeroId) == false) retreatCastle.HeroIds.Add(member.HeroId);
            }
            ApplyBattleConsequences(database, state, army, false, defeatMargin, orderlyRetreat, summary);
            ResolveDefeatedCharacterFate(database, state, army, defeatMargin, orderlyRetreat, captorFactionId, summary);
        }

        // 승패와 전력 차이에 따라 피로, 부상, 경험과 공훈을 반영합니다.
        private static void ApplyBattleConsequences(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            bool victory,
            int margin,
            bool orderlyRetreat,
            WITurnSummary summary)
        {
            foreach (WIArmyMemberState member in army.Members)
            {
                WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                if (character == null) continue;
                int fatigueGain = victory ? database.BattleVictoryFatigue
                    : orderlyRetreat ? database.OrderlyRetreatFatigue : database.BattleDefeatFatigue;
                int experienceGain = victory ? database.BattleVictoryExperience : database.BattleDefeatExperience;
                character.Fatigue = Mathf.Clamp(character.Fatigue + fatigueGain, 0, 100);
                character.Experience += experienceGain;
                int meritGain = 0;
                if (victory)
                {
                    meritGain = database.BattleVictoryMerit;
                    character.Merit += meritGain;
                    if (character.BaseGrade == WICharacterGrade.Common && character.PromotedToHero == false)
                    {
                        character.PromotionAchievement = true;
                    }
                }
                bool injured = victory == false && orderlyRetreat == false && margin >= database.BattleInjuryPowerMargin;
                if (injured) character.InjuryMonths = Mathf.Max(character.InjuryMonths, database.BattleInjuryMonths);
                WIHeroDefinition hero = database.GetHero(member.HeroId);
                string result = victory ? "승리" : orderlyRetreat ? "질서 있는 후퇴" : "패배";
                summary?.News.Add($"전투 인물 · {hero?.DisplayName.Get(database.UseEnglish) ?? member.HeroId} · {result} · " +
                    $"공훈 +{meritGain} · 경험 +{experienceGain} · 피로 +{fatigueGain}" +
                    (injured ? $" · 부상 {database.BattleInjuryMonths}개월" : string.Empty));
            }
        }

        // 함께 승리한 전투단원 쌍의 전우 승리를 누적하고 기준 도달 시 관계를 한 단계 개선합니다.
        private static void ApplyBattleRelationshipConsequences(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WITurnSummary summary)
        {
            List<string> memberIds = army.Members.Select(item => item.HeroId).Distinct().OrderBy(item => item).ToList();
            for (int firstIndex = 0; firstIndex < memberIds.Count; firstIndex += 1)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < memberIds.Count; secondIndex += 1)
                {
                    WIRelationshipState relationship = state.GetOrCreateRelationship(memberIds[firstIndex], memberIds[secondIndex]);
                    relationship.SharedBattleVictories += 1;
                    if (relationship.SharedBattleVictories < database.BattleBondVictoryThreshold ||
                        relationship.Level == WIRelationshipLevel.Fondness)
                    {
                        continue;
                    }
                    relationship.SharedBattleVictories = 0;
                    relationship.Level = (WIRelationshipLevel)Mathf.Min(
                        (int)WIRelationshipLevel.Fondness,
                        (int)relationship.Level + 1);
                    WIHeroDefinition first = database.GetHero(memberIds[firstIndex]);
                    WIHeroDefinition second = database.GetHero(memberIds[secondIndex]);
                    summary?.News.Add($"전우 관계 발전 · {first?.DisplayName.Get(database.UseEnglish)} ↔ " +
                        $"{second?.DisplayName.Get(database.UseEnglish)} · {relationship.Level}");
                }
            }
        }

        // 큰 전력 차이로 무너진 전투단원 한 명의 사망, 포로, 전향 또는 후퇴 결과를 결정합니다.
        private static void ResolveDefeatedCharacterFate(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            int defeatMargin,
            bool orderlyRetreat,
            string captorFactionId,
            WITurnSummary summary)
        {
            if (orderlyRetreat || defeatMargin < database.CapturePowerMargin || army.Members.Count == 0)
            {
                return;
            }
            WIArmyMemberState affectedMember = army.Members
                .OrderBy(member => database.GetHero(member.HeroId)?.Grade == WICharacterGrade.Common ? 0 : 1)
                .ThenByDescending(member => state.GetCharacter(member.HeroId)?.Fatigue ?? 0)
                .ThenBy(member => member.HeroId)
                .First();
            WICharacterRuntimeState character = state.GetCharacter(affectedMember.HeroId);
            WIHeroDefinition hero = database.GetHero(affectedMember.HeroId);
            if (character == null || hero == null)
            {
                return;
            }

            WICampaignDifficultyDefinition difficulty = database.GetDifficulty(state.Difficulty);
            int deathWeight = difficulty?.BattleDeathChance ?? 15;
            int captureWeight = difficulty?.BattleCaptureChance ?? 35;
            int defectionWeight = difficulty?.BattleDefectionChance ?? 10;
            if (hero.BattleTraits.Contains(WIBattleTraitType.Survivor))
            {
                deathWeight /= 4;
            }
            if (hero.BattleTraits.Contains(WIBattleTraitType.Elusive))
            {
                captureWeight /= 4;
            }
            if (hero.BattleTraits.Contains(WIBattleTraitType.Unyielding))
            {
                defectionWeight = 0;
            }

            int roll = GetStableBattleFateRoll(state, army, character.HeroId);
            int deathLimit = deathWeight;
            int captureLimit = deathLimit + captureWeight;
            int defectionLimit = captureLimit + defectionWeight;
            string fate;
            if (roll < deathLimit)
            {
                MarkCharacterDead(database, state, character);
                fate = "사망";
            }
            else if (roll < captureLimit)
            {
                character.Captured = true;
                character.CaptorFactionId = captorFactionId;
                character.CapturedFromFactionId = army.FactionId;
                character.CapturedMonthsRemaining = database.CaptureDurationMonths;
                character.RansomRequested = true;
                character.RansomResourceType = roll % 3 == 0 ? WIResourceType.ManaCrystal : WIResourceType.Gold;
                character.RansomAmount = character.RansomResourceType == WIResourceType.Gold
                    ? database.PrisonerRansomGold : Mathf.Max(1, database.PrisonerRansomGold / 3);
                fate = $"포로 · {GetResourceDisplayName(character.RansomResourceType)} {character.RansomAmount} 교환 요청";
            }
            else if (roll < defectionLimit)
            {
                character.JoinedEnemyFactionId = captorFactionId;
                WICastleRuntimeState joinedCastle = state.Castles.FirstOrDefault(item => item.FactionId == captorFactionId);
                if (joinedCastle != null && joinedCastle.HeroIds.Contains(character.HeroId) == false)
                {
                    joinedCastle.HeroIds.Add(character.HeroId);
                }
                fate = "적 진영에 합류";
            }
            else
            {
                fate = "후퇴";
                summary?.News.Add($"전투 이탈 · {hero.DisplayName.Get(database.UseEnglish)} · {fate}");
                return;
            }

            character.Activity = WICharacterActivityType.None;
            army.Members.Remove(affectedMember);
            if (army.Members.Count == 0)
            {
                state.Armies.Remove(army);
            }
            else if (army.Members.Any(member => member.Role == WIUnitRole.Commander) == false)
            {
                WIArmyMemberState successor = army.Members.OrderByDescending(member =>
                    database.GetHero(member.HeroId)?.Leadership ?? 0).First();
                successor.Role = WIUnitRole.Commander;
            }
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                if (character.JoinedEnemyFactionId != castle.FactionId)
                {
                    castle.HeroIds.Remove(affectedMember.HeroId);
                }
            }
            summary?.News.Add($"전투 이탈 · {hero.DisplayName.Get(database.UseEnglish)} · {fate}");
        }

        // 사망한 인물을 등급에 따라 영구 퇴장 또는 일반 인물 재야 복귀 대기로 전환합니다.
        public static void MarkCharacterDead(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICharacterRuntimeState character)
        {
            if (database == null || state == null || character == null)
            {
                return;
            }

            character.IsDead = true;
            character.DeathCount += 1;
            character.Recruited = false;
            character.Discovered = false;
            character.Captured = false;
            character.CaptorFactionId = string.Empty;
            character.CapturedFromFactionId = string.Empty;
            character.CapturedMonthsRemaining = 0;
            character.RansomRequested = false;
            character.JoinedEnemyFactionId = string.Empty;
            character.Activity = WICharacterActivityType.None;
            character.ActivityTargetHeroId = string.Empty;
            character.RecruitmentProgress = 0;
            character.RecruitmentCastleId = string.Empty;
            bool permanentDeath = character.BaseGrade == WICharacterGrade.Hero;
            if (permanentDeath == false)
            {
                character.PromotedToHero = false;
                character.PromotionAchievement = false;
                character.TitleId = string.Empty;
                state.PendingHeroPromotionIds.Remove(character.HeroId);
            }
            character.CommonReturnMonthsRemaining = permanentDeath ? 0 : 6;
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.HeroIds.Remove(character.HeroId);
                if (castle.GovernorHeroId == character.HeroId)
                {
                    castle.GovernorHeroId = string.Empty;
                    castle.DelegatedToGovernor = false;
                }
            }
            foreach (WIArmyState existingArmy in state.Armies)
            {
                existingArmy.Members.RemoveAll(member => member.HeroId == character.HeroId);
            }
            state.PendingRecruitmentEvents.RemoveAll(item => item.CandidateHeroId == character.HeroId ||
                                                             item.RecruiterHeroId == character.HeroId);
        }

        // 사망한 일반 인물을 고용 상태 중복 없이 살아남은 성의 재야 인재로 되돌립니다.
        private static void ResolveCommonCharacterReturns(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            List<WICastleRuntimeState> availableCastles = state.Castles
                .Where(castle => state.GetFactionState(castle.FactionId)?.Eliminated == false)
                .OrderBy(castle => castle.CastleId)
                .ToList();
            if (availableCastles.Count == 0)
            {
                return;
            }

            foreach (WICharacterRuntimeState character in state.Characters.Where(item =>
                         item.IsDead && item.BaseGrade == WICharacterGrade.Common))
            {
                character.PromotedToHero = false;
                character.CommonReturnMonthsRemaining = Mathf.Max(0, character.CommonReturnMonthsRemaining - 1);
                if (character.CommonReturnMonthsRemaining > 0 || IsCharacterEmployedAnywhere(state, character.HeroId))
                {
                    continue;
                }

                int castleIndex = GetStableCommonReturnCastleIndex(state, character.HeroId, availableCastles.Count);
                WICastleRuntimeState returnCastle = availableCastles[castleIndex];
                character.IsDead = false;
                character.Discovered = false;
                character.Recruited = false;
                character.RecruitmentProgress = 0;
                character.RecruitmentCastleId = returnCastle.CastleId;
                character.CommonReturnCount += 1;
                WIHeroDefinition definition = database.GetHero(character.HeroId);
                summary?.News.Add($"재야 인재 소문 · {definition?.DisplayName.Get(database.UseEnglish) ?? character.HeroId} · " +
                                  $"{database.GetCastle(returnCastle.CastleId)?.DisplayName.Get(database.UseEnglish)} 인근");
            }
        }

        // 인물이 성, 전투단, 포로, 전향 또는 영입 상태에 남아 있는지 확인합니다.
        private static bool IsCharacterEmployedAnywhere(WIAdministrationState state, string heroId)
        {
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            return character == null || character.Recruited || character.Captured ||
                   string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false ||
                   state.Castles.Any(castle => castle.HeroIds.Contains(heroId)) ||
                   state.Armies.Any(army => army.Members.Any(member => member.HeroId == heroId));
        }

        // 턴과 인물 ID를 이용해 재현 가능한 재야 복귀 성 인덱스를 반환합니다.
        private static int GetStableCommonReturnCastleIndex(WIAdministrationState state, string heroId, int count)
        {
            unchecked
            {
                int hash = state.Turn * 397;
                foreach (char letter in heroId)
                {
                    hash = hash * 31 + letter;
                }
                return Mathf.Abs(hash % count);
            }
        }

        // 저장과 전투 결과 출처에 관계없이 같은 운명 판정을 만드는 0~99 값을 반환합니다.
        private static int GetStableBattleFateRoll(WIAdministrationState state, WIArmyState army, string heroId)
        {
            unchecked
            {
                int hash = 17;
                string key = $"{state.Turn}|{army.ArmyId}|{heroId}";
                foreach (char character in key)
                {
                    hash = hash * 31 + character;
                }
                return Mathf.Abs(hash % 100);
            }
        }

        // 포로 교환 요청에 표시할 자원 이름을 반환합니다.
        private static string GetResourceDisplayName(WIResourceType resourceType)
        {
            return resourceType == WIResourceType.ManaCrystal ? "마나" :
                resourceType == WIResourceType.Influence ? "영향력" : "금화";
        }

        // 포로 억류 기간을 줄이고 만료된 인물을 원래 진영의 성으로 귀환시킵니다.
        private static void ResolveCapturedCharacters(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterRuntimeState character in state.Characters.Where(item => item.Captured).ToList())
            {
                character.CapturedMonthsRemaining = Mathf.Max(0, character.CapturedMonthsRemaining - 1);
                if (character.CapturedMonthsRemaining > 0) continue;
                if (ReleaseCapturedCharacter(state, character) == false) continue;
                WICastleRuntimeState returnCastle = state.Castles.FirstOrDefault(item => item.HeroIds.Contains(character.HeroId));
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                summary.News.Add($"포로 귀환 · {hero?.DisplayName.Get(database.UseEnglish) ?? character.HeroId} · " +
                    $"{database.GetCastle(returnCastle.CastleId).DisplayName.Get(database.UseEnglish)} 도착");
            }
        }

        // 포로 상태를 해제하고 원소속 진영이 보유한 첫 성으로 인물을 배치합니다.
        private static bool ReleaseCapturedCharacter(WIAdministrationState state, WICharacterRuntimeState character)
        {
            WICastleRuntimeState returnCastle = state.Castles.FirstOrDefault(item => item.FactionId == character.CapturedFromFactionId);
            if (returnCastle == null) return false;
            character.Captured = false;
            character.CaptorFactionId = string.Empty;
            character.CapturedMonthsRemaining = 0;
            character.RansomRequested = false;
            character.RansomAmount = 0;
            if (returnCastle.HeroIds.Contains(character.HeroId) == false) returnCastle.HeroIds.Add(character.HeroId);
            return true;
        }

        // 아군 성에 머무는 전투단의 피로를 회복하고 재편성 남은 기간을 줄입니다.
        private static void ResolveArmyReorganization(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIArmyState army in state.Armies.Where(item => item.IsMoving == false &&
                         item.AwaitingBattle == false &&
                         state.GetCastle(item.CurrentCastleId)?.FactionId == item.FactionId))
            {
                foreach (WIArmyMemberState member in army.Members)
                {
                    WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                    if (character != null)
                    {
                        character.Fatigue = Mathf.Max(0, character.Fatigue - 15);
                    }
                }

                if (army.ReorganizationMonths > 0)
                {
                    army.ReorganizationMonths -= 1;
                    if (army.ReorganizationMonths == 0)
                    {
                        summary.News.Add($"{army.DisplayName} · {database.GetCastle(army.CurrentCastleId).DisplayName.Get(database.UseEnglish)}에서 재편성 완료");
                    }
                }
            }
        }

        // 전투 시스템에서 승리 결과를 넘겨받아 적 성을 점령 상태로 전환합니다.
        public static bool ResolveArmyVictoryAndOccupation(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WITurnSummary summary = null)
        {
            if (army == null || army.AwaitingBattle == false)
            {
                return false;
            }

            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            if (castle == null || castle.FactionId == army.FactionId)
            {
                return false;
            }

            string defeatedFactionId = castle.FactionId;
            HashSet<string> occupyingHeroIds = new HashSet<string>(army.Members.Select(member => member.HeroId));
            WICastleRuntimeState defeatedFallback = state.Castles
                .Where(item => item.CastleId != castle.CastleId && item.FactionId == defeatedFactionId)
                .OrderBy(item => item.AdjacentCastleIds.Contains(castle.CastleId) ? 0 : 1)
                .ThenBy(item => item.CastleId)
                .FirstOrDefault();
            foreach (string heroId in castle.HeroIds.Where(id => occupyingHeroIds.Contains(id) == false).ToList())
            {
                castle.HeroIds.Remove(heroId);
                if (defeatedFallback != null && defeatedFallback.HeroIds.Contains(heroId) == false)
                {
                    defeatedFallback.HeroIds.Add(heroId);
                }
                else if (defeatedFallback == null)
                {
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    if (character != null)
                    {
                        character.Recruited = false;
                        character.Discovered = false;
                    }
                }
            }
            castle.FactionId = army.FactionId;
            castle.Stability = Mathf.Min(castle.Stability, 20);
            castle.OccupationUnrestMonths = 3;
            castle.GovernorHeroId = string.Empty;
            castle.DelegatedToGovernor = false;
            army.AwaitingBattle = false;
            army.OriginCastleId = castle.CastleId;
            army.ReorganizationMonths = Mathf.Max(army.ReorganizationMonths, 1);
            foreach (WIArmyMemberState member in army.Members)
            {
                if (castle.HeroIds.Contains(member.HeroId) == false)
                {
                    castle.HeroIds.Add(member.HeroId);
                }
            }

            if (army.FactionId == state.PlayerFactionId)
            {
                WIOccupationEventSystem.CreatePending(state, castle.CastleId, defeatedFactionId, summary, database);
            }

            return true;
        }

        // 점령지의 영지관과 주둔 전투단 유무에 따라 월간 질서 안정을 처리합니다.
        private static void ResolveOccupationStability(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                if (castle.OccupationUnrestMonths <= 0)
                {
                    continue;
                }

                bool hasGovernor = string.IsNullOrEmpty(castle.GovernorHeroId) == false;
                bool hasGarrison = state.Armies.Exists(army => army.CurrentCastleId == castle.CastleId && army.AwaitingBattle == false);
                if (hasGovernor && hasGarrison)
                {
                    castle.Stability = Mathf.Clamp(castle.Stability + 5, 0, 100);
                    castle.OccupationUnrestMonths -= 1;
                }
                else
                {
                    castle.Stability = Mathf.Clamp(castle.Stability - 3, 0, 100);
                }

                summary.News.Add($"점령지 안정 · {database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish)} · 질서 {castle.Stability} · 불안 {castle.OccupationUnrestMonths}개월");
            }
        }

        // 출발 성의 번영 상태로 전투단 보급 상태를 판정합니다.
        private static WISupplyState GetArmySupplyState(WICastleRuntimeState origin)
        {
            if (origin.Prosperity >= 50) return WISupplyState.Sufficient;
            if (origin.Prosperity >= 25) return WISupplyState.Shortage;
            return WISupplyState.Depleted;
        }

        // 보급 상태에 따라 이동을 마친 전투단원의 피로를 증가시킵니다.
        private static void ApplyMarchFatigue(WIAdministrationState state, WIArmyState army)
        {
            int fatigue = army.Supply == WISupplyState.Sufficient ? 5 : (army.Supply == WISupplyState.Shortage ? 12 : 20);
            foreach (WIArmyMemberState member in army.Members)
            {
                WICharacterRuntimeState character = state.GetCharacter(member.HeroId);
                character.Fatigue = Mathf.Clamp(character.Fatigue + fatigue, 0, 100);
            }
        }

        // 성에 남은 영웅의 흔적 중 현재 사업과 일치하는 성과 보너스를 합산합니다.
        public static int GetLegacyBonus(WICastleRuntimeState castleState, WICastleProjectType projectType)
        {
            int bonus = 0;
            foreach (WIHeroLegacyState legacy in castleState.HeroLegacies)
            {
                if (legacy.ProjectType == projectType)
                {
                    bonus += legacy.Bonus;
                }
            }

            return bonus;
        }

        // 높은 성과를 낸 사업에서 선택 사건과 영웅의 흔적 후보를 생성합니다.
        private static void CreateProjectEventAndLegacyChoice(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WICastleProjectState project,
            WIHeroDefinition manager,
            int gain,
            WITurnSummary summary)
        {
            bool supportsEvent = project.ProjectType == WICastleProjectType.Prosperity ||
                                 project.ProjectType == WICastleProjectType.Technology ||
                                 project.ProjectType == WICastleProjectType.Stability ||
                                 project.ProjectType == WICastleProjectType.Fortification ||
                                 project.ProjectType == WICastleProjectType.Recruitment;
            if (gain >= 10 && supportsEvent)
            {
                state.PendingProjectEvents.Add(new WIPendingProjectEvent
                {
                    CastleId = castleState.CastleId,
                    ProjectType = project.ProjectType,
                    HeroId = project.ManagerHeroId,
                    Title = GetProjectEventTitle(project.ProjectType),
                    HeroChoiceAvailable = GetProjectTraitBonus(database, project.ProjectType, manager) > 0
                });
                summary.News.Add($"선택 사건 발생 · {GetProjectEventTitle(project.ProjectType)}");
            }

            if (gain < 14 || manager == null)
            {
                return;
            }

            string heroName = manager.DisplayName.Get(database.UseEnglish);
            WIHeroLegacyDefinition legacyDefinition = database.GetHeroLegacy(project.ProjectType);
            if (legacyDefinition == null)
            {
                return;
            }
            state.PendingLegacyChoices.Add(new WIPendingLegacyChoice
            {
                CastleId = castleState.CastleId,
                Legacy = new WIHeroLegacyState
                {
                    DefinitionId = legacyDefinition.Id,
                    HeroId = manager.Id,
                    ProjectType = project.ProjectType,
                    DisplayName = $"{heroName}의 {legacyDefinition.DisplayName.Get(database.UseEnglish)}",
                    Description = legacyDefinition.Description.Get(database.UseEnglish),
                    Bonus = legacyDefinition.ProjectGainBonus
                }
            });
            summary.News.Add($"영웅의 흔적 후보 · {heroName}");
        }

        // 새 흔적을 설치하고 교체 대상은 효과 없는 기념 기록으로 보존합니다.
        public static bool InstallHeroLegacy(
            WIAdministrationState state,
            WICastleRuntimeState castle,
            WIPendingLegacyChoice pendingChoice,
            WIHeroLegacyState replacedLegacy = null)
        {
            if (castle == null || pendingChoice == null || state.PendingLegacyChoices.Contains(pendingChoice) == false)
            {
                return false;
            }
            if (castle.HeroLegacies.Count >= 2 && replacedLegacy == null)
            {
                return false;
            }
            if (replacedLegacy != null)
            {
                if (castle.HeroLegacies.Remove(replacedLegacy) == false) return false;
                castle.CommemoratedHeroLegacies.Add(replacedLegacy);
            }
            castle.HeroLegacies.Add(pendingChoice.Legacy);
            state.PendingLegacyChoices.Remove(pendingChoice);
            return true;
        }

        // 사업 종류에 대응하는 사건 제목을 반환합니다.
        private static string GetProjectEventTitle(WICastleProjectType projectType)
        {
            switch (projectType)
            {
                case WICastleProjectType.Prosperity: return "상단과 주민의 분쟁";
                case WICastleProjectType.Technology: return "불안정한 고대 유물";
                case WICastleProjectType.Stability: return "도적과 숨은 첩자";
                case WICastleProjectType.Fortification: return "공사 중 발견된 비밀 통로";
                case WICastleProjectType.Recruitment: return "인재를 둘러싼 경쟁자";
                default: return "사업 현장의 뜻밖의 기회";
            }
        }

        // 수락한 선술집 의뢰를 완료하고 각 성의 의뢰를 최대 세 개로 보충합니다.
        public static void ResolveTavernQuests(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                WIFactionDefinition faction = database.GetFaction(castle.FactionId);
                if (faction == null || faction.PlayerFaction == false)
                {
                    continue;
                }

                for (int index = castle.TavernQuests.Count - 1; index >= 0; index -= 1)
                {
                    WITavernQuestState quest = castle.TavernQuests[index];
                    if (quest.Status != WIQuestStatus.Accepted)
                    {
                        continue;
                    }

                    WITavernQuestDefinition questDefinition = database.GetTavernQuest(quest.QuestType);
                    if (questDefinition == null) continue;
                    if (quest.RemainingMonths <= 0) quest.RemainingMonths = questDefinition.DurationMonths;
                    quest.RemainingMonths -= 1;
                    if (quest.RemainingMonths > 0) continue;

                    WICharacterRuntimeState character = state.GetCharacter(quest.AssignedHeroId);
                    WIHeroDefinition hero = database.GetHero(quest.AssignedHeroId);
                    if (character != null && hero != null)
                    {
                        int aptitude = GetQuestAptitude(hero, questDefinition.Aptitude);
                        bool specialized = aptitude >= questDefinition.AptitudeThreshold;
                        int gold = questDefinition.GoldReward + (specialized ? questDefinition.AptitudeBonusGold : 0);
                        character.Merit += questDefinition.MeritReward;
                        character.Reputation += questDefinition.ReputationReward;
                        character.Fatigue = Mathf.Clamp(character.Fatigue + questDefinition.FatigueCost, 0, 100);
                        summary.GoldGained += gold;
                        ApplyQuestCastleEffect(state, castle, questDefinition, summary);
                        summary.News.Add($"선술집 의뢰 완료 · {questDefinition.DisplayName.Get(database.UseEnglish)} · {hero.DisplayName.Get(database.UseEnglish)} · 금화 +{gold}{(specialized ? " · 적성 보너스" : string.Empty)}");
                    }

                    castle.TavernQuests.RemoveAt(index);
                }

                while (castle.TavernQuests.Count < 3)
                {
                    int questIndex = castle.TavernQuests.Count;
                    castle.TavernQuests.Add(new WITavernQuestState
                    {
                        QuestId = $"{castle.CastleId}_{state.Turn}_{questIndex}",
                        QuestType = (WITavernQuestType)((state.Turn + questIndex) % 5),
                        Status = WIQuestStatus.Available
                    });
                }
            }
        }

        // 선술집 의뢰 종류의 한국어 표시명을 반환합니다.
        public static string GetQuestDisplayName(WITavernQuestType questType)
        {
            string[] names = { "호위와 물자 운송", "괴수 또는 도적 토벌", "실종자와 유적 탐색", "주민·종족 갈등 중재", "첩자 추적" };
            return names[(int)questType];
        }

        // 의뢰 적성 종류에 해당하는 인물 능력치를 반환합니다.
        public static int GetQuestAptitude(WIHeroDefinition hero, WIQuestAptitude aptitude)
        {
            if (hero == null) return 0;
            switch (aptitude)
            {
                case WIQuestAptitude.Leadership: return hero.Leadership;
                case WIQuestAptitude.Might: return hero.Might;
                case WIQuestAptitude.Intelligence: return hero.Intelligence;
                case WIQuestAptitude.Charisma: return hero.Charisma;
                case WIQuestAptitude.Politics: return hero.Politics;
                default: return 0;
            }
        }

        // 의뢰 종류별 성 상태·인재 발견·방첩 결과를 적용합니다.
        private static void ApplyQuestCastleEffect(
            WIAdministrationState state,
            WICastleRuntimeState castle,
            WITavernQuestDefinition definition,
            WITurnSummary summary)
        {
            int value = definition.CastleEffectValue;
            switch (definition.QuestType)
            {
                case WITavernQuestType.Escort:
                    castle.Prosperity = Mathf.Clamp(castle.Prosperity + value, 0, 100);
                    break;
                case WITavernQuestType.Hunt:
                case WITavernQuestType.Mediation:
                    castle.Stability = Mathf.Clamp(castle.Stability + value, 0, 100);
                    break;
                case WITavernQuestType.Search:
                    castle.Technology = Mathf.Clamp(castle.Technology + value, 0, 100);
                    WICharacterRuntimeState candidate = state.Characters.Find(item =>
                        IsAvailableWanderingCandidate(item) &&
                        (item.RecruitmentCastleId == castle.CastleId || string.IsNullOrEmpty(item.RecruitmentCastleId)));
                    if (candidate != null) candidate.Discovered = true;
                    break;
                case WITavernQuestType.Counterintelligence:
                    castle.CounterintelligenceMonths = Mathf.Max(castle.CounterintelligenceMonths, value);
                    break;
            }
        }

        // 이번 달에 배정된 인물 활동을 처리하고 인물 상태와 소식을 갱신합니다.
        private static void ResolveCharacterActivities(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || character.Activity == WICharacterActivityType.None)
                {
                    continue;
                }

                if (CanPerformCharacterActivity(state, character.HeroId, character.Activity) == false)
                {
                    character.Activity = WICharacterActivityType.None;
                    character.ActivityTargetHeroId = string.Empty;
                    continue;
                }

                WIHeroDefinition hero = database.GetHero(character.HeroId);
                switch (character.Activity)
                {
                    case WICharacterActivityType.Search:
                        ResolveSearchActivity(database, state, hero, character, summary);
                        break;
                    case WICharacterActivityType.Socialize:
                        ResolveSocializeActivity(database, state, hero, character, summary);
                        break;
                    case WICharacterActivityType.Recruit:
                        ResolveRecruitActivity(database, state, hero, character, summary);
                        break;
                    case WICharacterActivityType.Training:
                        character.Experience += 10 + hero.Leadership / 10;
                        character.Merit += 3;
                        summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)} · 개인 훈련 완료");
                        break;
                    case WICharacterActivityType.Rest:
                        character.Fatigue = Mathf.Max(0, character.Fatigue - database.CharacterRestFatigueRecovery);
                        character.InjuryMonths = Mathf.Max(0, character.InjuryMonths - 1);
                        summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)} · 휴식과 회복 완료");
                        break;
                }

                if (character.Activity != WICharacterActivityType.Rest)
                {
                    character.Fatigue = Mathf.Clamp(character.Fatigue + 10, 0, 100);
                }

                character.Activity = WICharacterActivityType.None;
                character.ActivityTargetHeroId = string.Empty;
            }
        }

        // 탐색 활동으로 아직 알려지지 않은 인재 한 명을 발견합니다.
        private static void ResolveSearchActivity(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero,
            WICharacterRuntimeState character,
            WITurnSummary summary)
        {
            WICastleRuntimeState searchCastle = state.Castles.Find(castle =>
                castle.HeroIds.Contains(character.HeroId));
            WICharacterRuntimeState candidate = state.Characters.Find(item =>
                IsAvailableWanderingCandidate(item) && item.RecruitmentCastleId == searchCastle?.CastleId);
            if (candidate == null)
            {
                candidate = state.Characters.Find(item =>
                    IsAvailableWanderingCandidate(item) && string.IsNullOrEmpty(item.RecruitmentCastleId));
            }
            character.Reputation += 2;
            if (candidate == null)
            {
                summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)} · 새로운 인재 단서를 찾지 못함");
                return;
            }

            candidate.Discovered = true;
            WIHeroDefinition discoveredHero = database.GetHero(candidate.HeroId);
            summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)}의 탐색 · {discoveredHero.DisplayName.Get(database.UseEnglish)} 발견");
        }

        // 재야 탐색에 노출할 수 있는 생존·미고용 인물인지 확인합니다.
        private static bool IsAvailableWanderingCandidate(WICharacterRuntimeState character)
        {
            return character != null && character.Discovered == false && character.Recruited == false &&
                   character.IsDead == false && character.Captured == false &&
                   string.IsNullOrEmpty(character.JoinedEnemyFactionId);
        }

        // 교류 활동으로 두 인물의 관계를 한 단계 개선합니다.
        private static void ResolveSocializeActivity(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero,
            WICharacterRuntimeState character,
            WITurnSummary summary)
        {
            WICharacterRuntimeState target = state.GetCharacter(character.ActivityTargetHeroId);
            if (target == null || target.Recruited == false)
            {
                return;
            }

            WIRelationshipState relationship = state.GetOrCreateRelationship(character.HeroId, target.HeroId);
            relationship.Level = relationship.Level == WIRelationshipLevel.Conflict
                ? WIRelationshipLevel.Normal
                : WIRelationshipLevel.Fondness;
            WIHeroDefinition targetHero = database.GetHero(target.HeroId);
            summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)}와 {targetHero.DisplayName.Get(database.UseEnglish)} · 관계 {relationship.Level}");
        }

        // 발견한 인재를 설득하고 누적 진척이 충족되면 진영에 영입합니다.
        private static void ResolveRecruitActivity(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero,
            WICharacterRuntimeState character,
            WITurnSummary summary)
        {
            WICharacterRuntimeState target = state.GetCharacter(character.ActivityTargetHeroId);
            if (target == null || target.Discovered == false || target.Recruited)
            {
                return;
            }

            WIHeroDefinition targetHero = database.GetHero(target.HeroId);
            if (character.Reputation < targetHero.RequiredReputation)
            {
                summary.News.Add($"{targetHero.DisplayName.Get(database.UseEnglish)} 영입 보류 · 명성 {targetHero.RequiredReputation} 필요");
                return;
            }

            int progress = database.RecruitmentBaseProgress + hero.Charisma / 4;
            target.RecruitmentProgress = Mathf.Clamp(target.RecruitmentProgress + progress, 0, 100);
            if (target.RecruitmentProgress < 100)
            {
                summary.News.Add($"{targetHero.DisplayName.Get(database.UseEnglish)} 영입 설득 · {target.RecruitmentProgress}%");
                return;
            }

            WIRecruitmentEventDefinition recruitmentEvent = database.GetRecruitmentEvent(targetHero.RecruitmentEventId);
            if (recruitmentEvent == null)
            {
                bool playerRecruitment = IsHeroInFaction(state, character.HeroId, state.PlayerFactionId);
                target.Recruited = true;
                PlaceRecruitedCharacter(state, character.HeroId, target.HeroId);
                if (playerRecruitment)
                {
                    state.PlayerRecruitmentSuccessCount += 1;
                }
                character.Merit += 10;
                character.Reputation += 5;
                summary.News.Add($"{targetHero.DisplayName.Get(database.UseEnglish)} 영입 성공");
                return;
            }

            if (state.PendingRecruitmentEvents.All(item => item.CandidateHeroId != target.HeroId))
            {
                state.PendingRecruitmentEvents.Add(new WIPendingRecruitmentEvent
                {
                    EventId = recruitmentEvent.Id,
                    RecruiterHeroId = character.HeroId,
                    CandidateHeroId = target.HeroId
                });
                summary.News.Add($"영입 요구 사건 · {recruitmentEvent.Title.Get(database.UseEnglish)} · {targetHero.DisplayName.Get(database.UseEnglish)}");
            }
        }

        // 영입 사건 선택지의 명성·공훈·영토·교섭가 조건 충족 여부를 반환합니다.
        public static bool CanChooseRecruitmentEventOption(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIPendingRecruitmentEvent pendingEvent,
            WIRecruitmentEventChoiceDefinition choice)
        {
            WICharacterRuntimeState recruiter = pendingEvent == null ? null : state.GetCharacter(pendingEvent.RecruiterHeroId);
            WIHeroDefinition recruiterDefinition = pendingEvent == null ? null : database.GetHero(pendingEvent.RecruiterHeroId);
            if (recruiter == null || choice == null || recruiter.Reputation < choice.RequiredReputation ||
                recruiter.Merit < choice.RequiredMerit)
            {
                return false;
            }
            int castleCount = state.Castles.Count(item => item.FactionId == state.PlayerFactionId);
            if (castleCount < choice.RequiredFactionCastleCount)
            {
                return false;
            }
            return choice.RequiresNegotiator == false ||
                   (recruiterDefinition != null && recruiterDefinition.Traits.Contains(WITraitType.Negotiator));
        }

        // 영입 요구 사건의 선택 결과를 적용해 후보를 영입합니다.
        public static bool ResolveRecruitmentEvent(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIPendingRecruitmentEvent pendingEvent,
            int choiceIndex,
            WITurnSummary summary)
        {
            WIRecruitmentEventDefinition definition = pendingEvent == null ? null : database.GetRecruitmentEvent(pendingEvent.EventId);
            if (definition == null || choiceIndex < 0 || choiceIndex >= definition.Choices.Count ||
                state.PendingRecruitmentEvents.Contains(pendingEvent) == false)
            {
                return false;
            }
            WIRecruitmentEventChoiceDefinition choice = definition.Choices[choiceIndex];
            if (CanChooseRecruitmentEventOption(database, state, pendingEvent, choice) == false)
            {
                return false;
            }

            WICharacterRuntimeState recruiter = state.GetCharacter(pendingEvent.RecruiterHeroId);
            WICharacterRuntimeState candidate = state.GetCharacter(pendingEvent.CandidateHeroId);
            if (candidate == null || candidate.Recruited)
            {
                return false;
            }
            bool playerRecruitment = IsHeroInFaction(state, recruiter.HeroId, state.PlayerFactionId);
            candidate.Recruited = true;
            PlaceRecruitedCharacter(state, recruiter.HeroId, candidate.HeroId);
            if (playerRecruitment)
            {
                state.PlayerRecruitmentSuccessCount += 1;
            }
            recruiter.Merit += choice.RecruiterMeritGain;
            recruiter.Reputation += 5;
            recruiter.Fatigue = Mathf.Clamp(recruiter.Fatigue + choice.RecruiterFatigueGain, 0, 100);
            state.PendingRecruitmentEvents.Remove(pendingEvent);
            WIHeroDefinition candidateDefinition = database.GetHero(candidate.HeroId);
            summary?.News.Add($"영입 성공 · {candidateDefinition.DisplayName.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}");
            return true;
        }

        // 영입된 인물을 영입 담당자가 현재 머무는 성에 배치합니다.
        private static void PlaceRecruitedCharacter(
            WIAdministrationState state,
            string recruiterHeroId,
            string candidateHeroId)
        {
            WICastleRuntimeState castle = state.Castles.Find(item =>
                item.HeroIds.Contains(recruiterHeroId));
            if (castle != null && castle.HeroIds.Contains(candidateHeroId) == false)
            {
                castle.HeroIds.Add(candidateHeroId);
            }
        }

        // 공훈, 명성과 특별 성취를 갖춘 일반 인물을 영웅 승격 후보로 등록합니다.
        private static void ResolvePromotionCandidates(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || character.BaseGrade != WICharacterGrade.Common ||
                    character.PromotedToHero || character.PromotionAchievement == false ||
                    character.Merit < database.PromotionRequiredMerit ||
                    character.Reputation < database.PromotionRequiredReputation ||
                    state.PendingHeroPromotionIds.Contains(character.HeroId))
                {
                    continue;
                }
                state.PendingHeroPromotionIds.Add(character.HeroId);
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                summary.News.Add($"영웅 승격 후보 · {hero.DisplayName.Get(database.UseEnglish)}");
            }
        }

        // 영향력을 지불하고 승격 후보 일반 인물을 영웅으로 확정합니다.
        public static bool PromoteCommonCharacter(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string heroId)
        {
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            WIFactionRuntimeState faction = state.GetPlayerFactionState();
            if (character == null || faction == null || state.PendingHeroPromotionIds.Contains(heroId) == false ||
                character.BaseGrade != WICharacterGrade.Common || character.PromotedToHero ||
                faction.Influence < database.PromotionInfluenceCost)
            {
                return false;
            }
            faction.Influence -= database.PromotionInfluenceCost;
            character.PromotedToHero = true;
            character.LoyaltyState = WILoyaltyState.Stable;
            state.PendingHeroPromotionIds.Remove(heroId);
            return true;
        }

        // AI가 성향, 선행 조건과 실제 마나 보유량에 맞춰 연구를 선택합니다.
        private static void PlanAIResearch(WIAdministrationDatabaseSO database, WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WIFactionDefinition factionDefinition in database.Factions.Where(item => item.PlayerFaction == false))
            {
                WIFactionRuntimeState faction = state.GetFactionState(factionDefinition.Id);
                if (faction == null || faction.Eliminated || string.IsNullOrEmpty(faction.ActiveResearchId) == false)
                {
                    continue;
                }
                WICharacterRuntimeState researcher = state.Characters.FirstOrDefault(character =>
                    character.Recruited && IsHeroInFaction(state, character.HeroId, faction.FactionId) &&
                    IsAdministrationCapable(state, character.HeroId) &&
                    state.IsCharacterBusy(character.HeroId) == false);
                if (researcher == null)
                {
                    continue;
                }
                int maximumTechnology = state.Castles.Where(castle => castle.FactionId == faction.FactionId)
                    .Select(castle => castle.Technology).DefaultIfEmpty(0).Max();
                List<WIResearchDefinition> candidates = database.ResearchDefinitions
                    .Where(research => faction.CompletedResearchIds.Contains(research.Id) == false)
                    .Where(research => faction.ManaCrystal >= research.ManaCost && maximumTechnology >= research.RequiredTechnology)
                    .Where(research => string.IsNullOrEmpty(research.PrerequisiteResearchId) ||
                                       faction.CompletedResearchIds.Contains(research.PrerequisiteResearchId))
                    .OrderByDescending(research => GetAIResearchPreference(factionDefinition.AIStrategy, research.EffectType)).ToList();
                if (candidates.Count == 0) continue;
                int factionSalt = database.Factions.ToList().FindIndex(item => item.Id == factionDefinition.Id);
                int selectedIndex = GetAICandidateIndex(database, state, candidates.Count, factionSalt);
                WIResearchDefinition research = candidates[selectedIndex];
                if (BeginResearch(database, state, faction.FactionId, research.Id, researcher.HeroId))
                {
                    WIResearchDefinition alternative = candidates.FirstOrDefault(item => item.Id != research.Id);
                    int selectedScore = GetAIResearchPreference(factionDefinition.AIStrategy, research.EffectType);
                    int alternativeScore = alternative == null ? 0 : GetAIResearchPreference(factionDefinition.AIStrategy, alternative.EffectType);
                    AddAIReasonReport(summary, factionDefinition.Id, factionDefinition.DisplayName.Get(database.UseEnglish), "연구",
                        $"상위 {GetAICandidateWindow(database, state, candidates.Count)}개 중 {selectedIndex + 1}순위 " +
                        $"{research.DisplayName.Get(database.UseEnglish)} 선택 · 성향 적합 {selectedScore}점" +
                        (alternative == null ? string.Empty : $" · 비교 {alternative.DisplayName.Get(database.UseEnglish)} {alternativeScore}점"));
                }
            }
        }

        // AI 성향과 연구 효과가 일치하는 정도를 우선순위 점수로 반환합니다.
        private static int GetAIResearchPreference(WIAIStrategy strategy, WIResearchEffectType effectType)
        {
            if (strategy == WIAIStrategy.Prosperity && effectType == WIResearchEffectType.GoldIncomePercent) return 10;
            if (strategy == WIAIStrategy.Development && (effectType == WIResearchEffectType.ManaIncomePerCastle || effectType == WIResearchEffectType.ProjectGain)) return 10;
            if (strategy == WIAIStrategy.Defense && effectType == WIResearchEffectType.InfluenceIncomePerCastle) return 10;
            if (strategy == WIAIStrategy.Aggressive && effectType == WIResearchEffectType.BattlePower) return 10;
            if (strategy == WIAIStrategy.Scheme && effectType == WIResearchEffectType.ProjectGain) return 10;
            return 1;
        }

        // 조건과 비용을 확인해 진영의 연구를 시작합니다.
        public static bool BeginResearch(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string factionId,
            string researchId,
            string researcherHeroId)
        {
            WIFactionRuntimeState faction = state.GetFactionState(factionId);
            WIResearchDefinition research = database.GetResearch(researchId);
            WICharacterRuntimeState researcher = state.GetCharacter(researcherHeroId);
            if (faction == null || research == null || researcher == null || researcher.Recruited == false ||
                IsAdministrationCapable(state, researcherHeroId) == false ||
                string.IsNullOrEmpty(faction.ActiveResearchId) == false || faction.CompletedResearchIds.Contains(researchId) ||
                faction.ManaCrystal < research.ManaCost || IsHeroInFaction(state, researcherHeroId, factionId) == false ||
                state.IsCharacterBusy(researcherHeroId))
            {
                return false;
            }
            if (string.IsNullOrEmpty(research.PrerequisiteResearchId) == false &&
                faction.CompletedResearchIds.Contains(research.PrerequisiteResearchId) == false)
            {
                return false;
            }
            int maximumTechnology = state.Castles
                .Where(castle => castle.FactionId == factionId)
                .Select(castle => castle.Technology)
                .DefaultIfEmpty(0).Max();
            if (maximumTechnology < research.RequiredTechnology)
            {
                return false;
            }
            faction.ManaCrystal -= research.ManaCost;
            faction.ActiveResearchId = research.Id;
            faction.ResearcherHeroId = researcherHeroId;
            faction.ResearchRemainingMonths = Mathf.Max(1, research.DurationMonths);
            return true;
        }

        // 공훈과 영향력 조건을 확인해 인물에게 작위를 수여합니다.
        public static bool AwardTitle(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string factionId,
            string heroId,
            string titleId)
        {
            WIFactionRuntimeState faction = state.GetFactionState(factionId);
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            WITitleDefinition title = database.GetTitle(titleId);
            if (faction == null || character == null || title == null || character.Recruited == false ||
                character.Merit < title.RequiredMerit || faction.Influence < title.InfluenceCost ||
                IsHeroInFaction(state, heroId, factionId) == false || character.TitleId == titleId)
            {
                return false;
            }
            faction.Influence -= title.InfluenceCost;
            character.TitleId = title.Id;
            character.LoyaltyState = WILoyaltyState.Stable;
            return true;
        }

        // 진행 중인 진영 연구의 남은 기간을 계산하고 완료 효과를 활성화합니다.
        private static void ResolveFactionResearch(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIFactionRuntimeState factionState in state.Factions)
            {
                if (string.IsNullOrEmpty(factionState.ActiveResearchId))
                {
                    continue;
                }
                factionState.ResearchRemainingMonths -= 1;
                if (factionState.ResearchRemainingMonths > 0)
                {
                    continue;
                }
                WIResearchDefinition research = database.GetResearch(factionState.ActiveResearchId);
                if (research != null && factionState.CompletedResearchIds.Contains(research.Id) == false)
                {
                    factionState.CompletedResearchIds.Add(research.Id);
                    if (factionState.FactionId == state.PlayerFactionId)
                    {
                        summary.News.Add($"연구 완료 · {research.DisplayName.Get(database.UseEnglish)}");
                    }
                }
                factionState.ActiveResearchId = string.Empty;
                factionState.ResearcherHeroId = string.Empty;
                factionState.ResearchRemainingMonths = 0;
            }
        }

        // 완료 연구의 자원 수입 효과를 성별 월간 수입에 적용합니다.
        private static void ApplyResearchIncomeBonus(
            WIAdministrationDatabaseSO database,
            WIFactionRuntimeState faction,
            WITurnSummary income)
        {
            if (faction == null) return;
            int goldPercent = GetResearchEffectBonus(database, faction, WIResearchEffectType.GoldIncomePercent);
            income.GoldGained += income.GoldGained * goldPercent / 100;
            income.ManaGained += GetResearchEffectBonus(database, faction, WIResearchEffectType.ManaIncomePerCastle);
            income.InfluenceGained += GetResearchEffectBonus(database, faction, WIResearchEffectType.InfluenceIncomePerCastle);
        }

        // 완료한 연구 중 지정한 효과 유형의 총 보너스를 반환합니다.
        private static int GetResearchEffectBonus(
            WIAdministrationDatabaseSO database,
            WIFactionRuntimeState faction,
            WIResearchEffectType effectType)
        {
            if (faction == null) return 0;
            return faction.CompletedResearchIds
                .Select(database.GetResearch)
                .Where(research => research != null && research.EffectType == effectType)
                .Sum(research => research.EffectValue);
        }

        // 담당 인물의 현재 작위가 제공하는 사업 보너스를 반환합니다.
        private static int GetTitleProjectBonus(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero)
        {
            if (hero == null) return 0;
            WITitleDefinition title = database.GetTitle(state.GetCharacter(hero.Id)?.TitleId);
            return title == null ? 0 : title.ProjectBonus;
        }

        // 인물이 해당 진영의 성이나 전투단에 소속되어 있는지 확인합니다.
        private static bool IsHeroInFaction(WIAdministrationState state, string heroId, string factionId)
        {
            return state.Castles.Any(castle => castle.FactionId == factionId && castle.HeroIds.Contains(heroId)) ||
                   state.Armies.Any(army => army.FactionId == factionId && army.Members.Any(member => member.HeroId == heroId));
        }

        // AI 성향과 성 상태에 따라 비용을 지불하고 중점 사업을 배치합니다.
        private static void AssignAIProject(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WIFactionDefinition faction,
            WIFactionRuntimeState factionState,
            WITurnSummary summary)
        {
            int projectCost = GetProjectCost(database, WICastleProjectType.Prosperity, WIProjectInvestment.Basic);
            if (castleState.ActiveProject != null || factionState.Gold < projectCost)
            {
                return;
            }

            WICastleProjectType projectType = ChooseAIProject(castleState, faction.AIStrategy);
            WICampaignVariantDefinition variant = database.GetCampaignVariant(state.CampaignVariant);
            if (projectType == WICastleProjectType.Recruitment &&
                variant?.NonPlayerRecruitmentEnabled == false)
            {
                projectType = WICastleProjectType.Prosperity;
            }
            WIHeroDefinition manager = SelectAIProjectManager(database, state, castleState, projectType);
            if (manager == null)
            {
                return;
            }

            factionState.Gold -= projectCost;
            castleState.ActiveProject = new WICastleProjectState
            {
                ProjectType = projectType,
                Investment = WIProjectInvestment.Basic,
                ManagerHeroId = manager.Id,
                RemainingMonths = 1
            };
            string reason = castleState.Stability < 35 ? $"질서 {castleState.Stability} 보완" :
                castleState.Defense < 35 ? $"방어 {castleState.Defense} 보완" : $"{GetAIStrategyReason(faction.AIStrategy)} 성향 우선";
            AddAIReasonReport(summary, faction.Id, faction.DisplayName.Get(database.UseEnglish), "사업",
                $"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)}에서 {projectType} 선택 · {reason}");
        }

        // AI 성향을 사업과 군사 판단에 사용하는 짧은 한국어 근거로 변환합니다.
        private static string GetAIStrategyReason(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Development: return "개발";
                case WIAIStrategy.Defense: return "방어";
                case WIAIStrategy.Aggressive: return "공세";
                case WIAIStrategy.Scheme: return "모략";
                default: return "번영";
            }
        }

        // 현재 난이도의 후보 범위 안에서 AI 사업 담당 인물을 결정합니다.
        public static WIHeroDefinition SelectAIProjectManager(WIAdministrationDatabaseSO database,
            WIAdministrationState state, WICastleRuntimeState castleState, WICastleProjectType projectType)
        {
            List<WIHeroDefinition> candidates = castleState.HeroIds
                .Where(heroId => state.IsCharacterBusy(heroId) == false)
                .Where(heroId => IsAdministrationCapable(state, heroId))
                .Select(database.GetHero)
                .Where(hero => hero != null)
                .OrderByDescending(hero => GetExpectedProjectGain(database, projectType, hero, WIProjectInvestment.Basic))
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            int castleNumber = int.Parse(castleState.CastleId.Substring(castleState.CastleId.Length - 2));
            return candidates[GetAICandidateIndex(database, state, candidates.Count, castleNumber)];
        }

        // AI 성향과 가장 취약한 성 수치를 조합해 사업 종류를 선택합니다.
        private static WICastleProjectType ChooseAIProject(WICastleRuntimeState castle, WIAIStrategy strategy)
        {
            if (castle.Stability < 35) return WICastleProjectType.Stability;
            if (castle.Defense < 35) return WICastleProjectType.Fortification;
            switch (strategy)
            {
                case WIAIStrategy.Development: return WICastleProjectType.Technology;
                case WIAIStrategy.Defense: return WICastleProjectType.Fortification;
                case WIAIStrategy.Aggressive: return WICastleProjectType.Training;
                case WIAIStrategy.Scheme: return WICastleProjectType.Recruitment;
                default: return WICastleProjectType.Prosperity;
            }
        }

        // 장기 평화로 전선이 하나뿐일 때 인접 AI 진영 사이에 새로운 전쟁 압력을 만듭니다.
        public static bool EnsureAIWarPressure(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            if (database == null || state == null ||
                state.Turn < database.AIWarPressureIntervalMonths ||
                state.Turn % database.AIWarPressureIntervalMonths != 0)
            {
                return false;
            }

            int activeWarFronts = state.DiplomaticRelations.Count(relation =>
                relation.Status == WIDiplomaticStatus.War &&
                state.GetFactionState(relation.FirstFactionId)?.Eliminated == false &&
                state.GetFactionState(relation.SecondFactionId)?.Eliminated == false);
            if (activeWarFronts >= database.AIMinimumActiveWarFronts)
            {
                return false;
            }

            List<WIFactionDefinition> aiFactions = database.Factions
                .Where(faction => faction.PlayerFaction == false &&
                                  state.GetFactionState(faction.Id)?.Eliminated == false)
                .ToList();
            var candidate = (from first in aiFactions
                             from second in aiFactions
                             where string.CompareOrdinal(first.Id, second.Id) < 0
                             let relation = state.GetOrCreateDiplomaticRelation(first.Id, second.Id)
                             let borderCount = CountFactionBorderConnections(database, state, first.Id, second.Id)
                             where borderCount > 0 && relation != null &&
                                   (relation.Status == WIDiplomaticStatus.Neutral ||
                                    relation.Status == WIDiplomaticStatus.Friendly)
                             orderby GetWarPressureScore(first, second, borderCount) descending,
                                 first.Id, second.Id
                             select new { First = first, Second = second }).FirstOrDefault();
            if (candidate == null)
            {
                return false;
            }

            WIFactionDefinition initiator = GetStrategyAggression(candidate.First.AIStrategy) >=
                                            GetStrategyAggression(candidate.Second.AIStrategy)
                ? candidate.First
                : candidate.Second;
            WIFactionDefinition target = initiator == candidate.First ? candidate.Second : candidate.First;
            if (DeclareWar(state, initiator.Id, target.Id) == false)
            {
                return false;
            }

            string initiatorName = initiator.DisplayName.Get(database.UseEnglish);
            string targetName = target.DisplayName.Get(database.UseEnglish);
            summary?.News.Add($"전선 격화 · {initiatorName}이 장기 교착을 깨고 {targetName}에 선전포고");
            AddAIReasonReport(summary, initiator.Id, initiatorName, "군사",
                $"활성 전선 {activeWarFronts}/{database.AIMinimumActiveWarFronts} · 장기 교착 해소를 위해 인접 진영과 전쟁 개시");
            return true;
        }

        // 두 진영이 소유한 성 사이의 인접 경계 연결 수를 계산합니다.
        private static int CountFactionBorderConnections(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string firstFactionId,
            string secondFactionId)
        {
            int count = 0;
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == firstFactionId))
            {
                count += castle.AdjacentCastleIds.Count(id => state.GetCastle(id)?.FactionId == secondFactionId);
            }
            return count;
        }

        // AI 성향과 접경 규모를 조합해 신규 전쟁 후보의 우선순위를 계산합니다.
        private static int GetWarPressureScore(
            WIFactionDefinition first,
            WIFactionDefinition second,
            int borderCount)
        {
            return borderCount * 10 + GetStrategyAggression(first.AIStrategy) +
                   GetStrategyAggression(second.AIStrategy);
        }

        // AI 성향을 전쟁 개시 성향 점수로 변환합니다.
        private static int GetStrategyAggression(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Aggressive:
                    return 8;
                case WIAIStrategy.Scheme:
                    return 5;
                case WIAIStrategy.Defense:
                    return 2;
                default:
                    return 3;
            }
        }

        // AI 진영이 전선 위협도를 평가해 복수 전투단을 방어, 증원과 공격 임무에 배치합니다.
        private static void PlanAIActions(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.PlayerFaction)
                {
                    continue;
                }
                if (state.GetFactionState(faction.Id)?.Eliminated == true) continue;

                List<WICastleRuntimeState> factionCastles = state.Castles.FindAll(item => item.FactionId == faction.Id);
                int armyLimit = Mathf.Max(1, factionCastles.Count / 8) + (faction.AIStrategy == WIAIStrategy.Aggressive ? 1 : 0);
                while (state.Armies.Count(item => item.FactionId == faction.Id) < armyLimit)
                {
                    WICastleRuntimeState baseCastle = factionCastles
                        .OrderByDescending(item => GetCastleThreatScore(database, state, item, faction.Id))
                        .FirstOrDefault(item =>
                        item.FactionId == faction.Id &&
                        item.HeroIds.Count(heroId => state.IsCharacterBusy(heroId) == false) >= 2);
                    string commanderId = baseCastle?.HeroIds.FirstOrDefault(heroId => state.IsCharacterBusy(heroId) == false);
                    if (baseCastle == null || string.IsNullOrEmpty(commanderId))
                    {
                        break;
                    }
                    WIArmyState createdArmy = CreateArmy(database, state, baseCastle, commanderId);
                    FillAIArmy(database, state, baseCastle, createdArmy, faction.AIStrategy);
                }

                List<WIArmyState> idleArmies = state.Armies
                    .Where(item => item.FactionId == faction.Id && item.IsOperational)
                    .ToList();
                WIDiplomaticRelationState jointRelation = state.DiplomaticRelations.FirstOrDefault(item =>
                    item.JointAttackMonthsRemaining > 0 &&
                    (item.FirstFactionId == faction.Id || item.SecondFactionId == faction.Id));
                WICastleRuntimeState jointTarget = state.GetCastle(jointRelation?.JointAttackTargetCastleId);
                WICastleRuntimeState jointStagingCastle = jointTarget == null ? null : factionCastles.FirstOrDefault(item =>
                    item.AdjacentCastleIds.Contains(jointTarget.CastleId));
                WICastleRuntimeState threatenedCastle = factionCastles
                    .Where(item => GetCastleThreatScore(database, state, item, faction.Id) > 0)
                    .OrderByDescending(item => GetCastleThreatScore(database, state, item, faction.Id))
                    .FirstOrDefault();
                WICastleRuntimeState frontlineCastle = factionCastles
                    .Where(item => item.AdjacentCastleIds.Any(adjacentId =>
                    {
                        WICastleRuntimeState adjacent = state.GetCastle(adjacentId);
                        return adjacent != null && adjacent.FactionId != faction.Id &&
                               AreFactionsAtWar(state, faction.Id, adjacent.FactionId);
                    }))
                    .OrderByDescending(item => GetCastleThreatScore(database, state, item, faction.Id))
                    .ThenBy(item => item.CastleId)
                    .FirstOrDefault();
                WICastleRuntimeState stagingCastle = threatenedCastle ?? frontlineCastle;
                int attackInterval = faction.Id == "valdor"
                    ? database.GetCampaignVariant(state.CampaignVariant)?.ValdorAttackIntervalMonths ?? 3
                    : 3;
                bool shouldMarch = faction.AIStrategy == WIAIStrategy.Aggressive
                    ? state.Turn % attackInterval == 0
                    : state.Turn % Mathf.Max(attackInterval, 3) == 0;
                string militaryReason = jointTarget != null ?
                    $"동맹 공동 목표 {database.GetCastle(jointTarget.CastleId).DisplayName.Get(database.UseEnglish)} 우선" :
                    threatenedCastle != null ?
                        $"전선 위협 {GetCastleThreatScore(database, state, threatenedCastle, faction.Id)}점 · {database.GetCastle(threatenedCastle.CastleId).DisplayName.Get(database.UseEnglish)} 방어·증원" :
                    frontlineCastle != null ?
                        $"접경 전선 {database.GetCastle(frontlineCastle.CastleId).DisplayName.Get(database.UseEnglish)} 집결 · {GetAIStrategyReason(faction.AIStrategy)} 성향에 따라 공격 검토" :
                        shouldMarch ? $"{GetAIStrategyReason(faction.AIStrategy)} 성향에 따라 인접 적 공격 검토" : "전선 위협이 낮아 예비대 유지";
                AddAIReasonReport(summary, faction.Id, faction.DisplayName.Get(database.UseEnglish), "군사", militaryReason);
                foreach (WIArmyState army in idleArmies)
                {
                    if (jointTarget != null && jointStagingCastle != null &&
                        AreFactionsAtWar(state, faction.Id, jointTarget.FactionId))
                    {
                        army.StrategicTargetCastleId = jointTarget.CastleId;
                        if (army.CurrentCastleId == jointStagingCastle.CastleId)
                        {
                            if (BeginArmyMarch(database, state, army, jointTarget.CastleId)) army.Mission = WIArmyMission.Attack;
                        }
                        else
                        {
                            string jointStep = GetNextFriendlyStep(database, state, army.CurrentCastleId,
                                jointStagingCastle.CastleId, faction.Id);
                            if (string.IsNullOrEmpty(jointStep) == false && BeginArmyMarch(database, state, army, jointStep))
                                army.Mission = WIArmyMission.Reinforce;
                        }
                        continue;
                    }

                    if (stagingCastle == null)
                    {
                        army.Mission = WIArmyMission.Reserve;
                        army.StrategicTargetCastleId = string.Empty;
                        continue;
                    }

                    army.StrategicTargetCastleId = stagingCastle.CastleId;
                    if (army.CurrentCastleId != stagingCastle.CastleId)
                    {
                        string reinforcementStep = GetNextFriendlyStep(database, state, army.CurrentCastleId, stagingCastle.CastleId, faction.Id);
                        if (string.IsNullOrEmpty(reinforcementStep) == false && BeginArmyMarch(database, state, army, reinforcementStep))
                        {
                            army.Mission = WIArmyMission.Reinforce;
                        }
                        continue;
                    }

                    army.Mission = WIArmyMission.Defend;
                    if (shouldMarch == false)
                    {
                        continue;
                    }

                    WICastleRuntimeState origin = state.GetCastle(army.CurrentCastleId);
                    List<string> targetCandidates = origin?.AdjacentCastleIds
                        .Where(id =>
                        {
                            WICastleRuntimeState target = state.GetCastle(id);
                            return target != null && target.FactionId != faction.Id &&
                                   AreFactionsAtWar(state, faction.Id, target.FactionId) &&
                                   CanAIFactionAttack(database, state, army, target, faction);
                        })
                        .OrderByDescending(id => GetAttackTargetScore(state, id))
                        .ToList();
                    int militarySalt = database.Factions.ToList().FindIndex(item => item.Id == faction.Id) + (army.ArmyId?.Length ?? 0);
                    int targetIndex = targetCandidates == null || targetCandidates.Count == 0 ? -1 :
                        GetAICandidateIndex(database, state, targetCandidates.Count, militarySalt);
                    string targetId = targetIndex < 0 ? string.Empty : targetCandidates[targetIndex];
                    if (string.IsNullOrEmpty(targetId) || BeginArmyMarch(database, state, army, targetId) == false)
                    {
                        continue;
                    }

                    army.Mission = WIArmyMission.Attack;
                    army.StrategicTargetCastleId = targetId;
                    WICastleRuntimeState target = state.GetCastle(targetId);
                    if (target.FactionId == state.PlayerFactionId)
                    {
                        target.InvasionWarning = true;
                        summary.News.Add($"침공 경고 · {faction.DisplayName.Get(database.UseEnglish)}이 {database.GetCastle(targetId).DisplayName.Get(database.UseEnglish)}로 진격 중");
                    }
                }
            }
        }

        // AI 전투단이 지휘관 한 명으로만 원정하지 않도록 성에 최소 한 명을 남기고 전투 인원을 편성합니다.
        private static void FillAIArmy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            WIArmyState army,
            WIAIStrategy strategy)
        {
            if (castle == null || army == null)
            {
                return;
            }

            int recommended = GetRecommendedArmySize(database, army);
            int targetSize = strategy == WIAIStrategy.Aggressive ? Mathf.Min(4, recommended) : Mathf.Min(3, recommended);
            targetSize = Mathf.Min(targetSize, Mathf.Max(1, castle.HeroIds.Count - 1));
            WIUnitRole[] roles = { WIUnitRole.Vanguard, WIUnitRole.Ranged, WIUnitRole.Magic, WIUnitRole.Support };
            int roleIndex = 0;
            foreach (string heroId in castle.HeroIds
                         .Where(id => state.IsCharacterBusy(id) == false)
                         .OrderByDescending(id => database.GetHero(id)?.Might ?? 0)
                         .ToList())
            {
                if (army.Members.Count >= targetSize)
                {
                    break;
                }

                AddArmyMember(database, state, army, heroId, roles[roleIndex % roles.Length]);
                roleIndex += 1;
            }
        }

        // 성의 적 인접 수, 적 전투단 접근과 방어 상태를 조합한 전선 위협도를 반환합니다.
        public static int GetCastleThreatScore(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            string factionId)
        {
            if (castle.FactionId != factionId)
            {
                return 0;
            }

            int enemyBorders = castle.AdjacentCastleIds.Count(id =>
            {
                string adjacentFactionId = state.GetCastle(id)?.FactionId;
                return string.IsNullOrEmpty(adjacentFactionId) == false &&
                       adjacentFactionId != factionId &&
                       AreFactionsAtWar(state, factionId, adjacentFactionId);
            });
            int approachingEnemies = state.Armies.Count(army =>
                army.FactionId != factionId &&
                AreFactionsAtWar(state, factionId, army.FactionId) &&
                ((army.IsMoving && army.TargetCastleId == castle.CastleId) ||
                 (army.AwaitingBattle && army.CurrentCastleId == castle.CastleId)));
            return Mathf.Max(0, enemyBorders * 30 + approachingEnemies * 50 - castle.Defense / 4 - castle.Stability / 5);
        }

        // 점령 가치가 높고 방어가 약한 적 성을 우선하도록 공격 점수를 계산합니다.
        private static int GetAttackTargetScore(WIAdministrationState state, string castleId)
        {
            WICastleRuntimeState castle = state.GetCastle(castleId);
            if (castle == null)
            {
                return int.MinValue;
            }
            int playerPriority = castle.FactionId == state.PlayerFactionId ? 20 : 0;
            return playerPriority + 100 - castle.Defense - castle.Stability / 2;
        }

        // 프리 시나리오의 AI 군주가 비용과 영토를 확인해 방랑 인물을 실제 성에 고용합니다.
        private static void PlanFreeScenarioAIRecruitment(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            WICampaignVariantDefinition variant = database.GetCampaignVariant(state.CampaignVariant);
            if (variant?.NonPlayerRecruitmentEnabled != true || state.Turn < 12 || state.Turn % 12 != 0)
            {
                return;
            }

            const int recruitmentCost = 100;
            foreach (WIFactionDefinition faction in database.Factions.Where(item => item.PlayerFaction == false))
            {
                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                List<WICastleRuntimeState> castles = state.Castles
                    .Where(castle => castle.FactionId == faction.Id)
                    .OrderBy(castle => castle.HeroIds.Count)
                    .ThenBy(castle => castle.CastleId)
                    .ToList();
                if (factionState == null || factionState.Eliminated ||
                    factionState.Gold < recruitmentCost || castles.Count == 0)
                {
                    continue;
                }

                HashSet<string> assigned = new HashSet<string>(state.Castles.SelectMany(castle => castle.HeroIds));
                foreach (string heroId in state.Armies.SelectMany(army => army.Members.Select(member => member.HeroId)))
                {
                    assigned.Add(heroId);
                }
                WICharacterRuntimeState candidate = state.Characters
                    .Where(character => character.Recruited == false && character.IsDead == false &&
                                        character.Captured == false &&
                                        string.IsNullOrEmpty(character.JoinedEnemyFactionId) &&
                                        assigned.Contains(character.HeroId) == false)
                    .OrderByDescending(character => database.GetHero(character.HeroId)?.Charisma ?? 0)
                    .ThenBy(character => character.HeroId)
                    .FirstOrDefault();
                if (candidate == null)
                {
                    continue;
                }

                WICastleRuntimeState destination = castles[0];
                factionState.Gold -= recruitmentCost;
                candidate.Recruited = true;
                candidate.Discovered = true;
                candidate.RecruitmentCastleId = destination.CastleId;
                destination.HeroIds.Add(candidate.HeroId);
                summary.News.Add($"AI 영입 · {faction.DisplayName.Get(database.UseEnglish)} · " +
                                 $"{database.GetHero(candidate.HeroId)?.DisplayName.Get(database.UseEnglish)} · " +
                                 $"{database.GetCastle(destination.CastleId)?.DisplayName.Get(database.UseEnglish)} 배치");
            }
        }

        // 시나리오 보존선과 예상 전력을 검사해 AI 진영의 무모한 원정을 막습니다.
        private static bool CanAIFactionAttack(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            WICastleRuntimeState target,
            WIFactionDefinition faction)
        {
            WICampaignVariantDefinition variant = database.GetCampaignVariant(state.CampaignVariant);
            string preservationFactionId = variant?.AIPreservationFactionId;
            if (string.IsNullOrEmpty(preservationFactionId) == false &&
                target.FactionId == preservationFactionId &&
                variant.ValdorAIPreservationCastleCount > 0 &&
                state.Castles.Count(castle => castle.FactionId == preservationFactionId) <=
                variant.ValdorAIPreservationCastleCount)
            {
                return false;
            }

            List<WIArmyState> defenders = state.Armies.Where(defender =>
                defender.FactionId == target.FactionId && defender.CurrentCastleId == target.CastleId &&
                defender.IsOperational).ToList();
            int attackPower = state.Armies
                .Where(candidate => candidate.FactionId == army.FactionId &&
                                    candidate.CurrentCastleId == army.CurrentCastleId &&
                                    candidate.IsOperational)
                .Sum(candidate => GetArmyBattlePower(database, state, candidate));
            int defensePower = GetCastleDefensePower(database, state, target, defenders);
            int requiredPercent = faction.AIStrategy == WIAIStrategy.Aggressive
                ? database.AggressiveAIAttackPowerPercent
                : database.StandardAIAttackPowerPercent;
            return attackPower * 100 >= defensePower * requiredPercent;
        }

        // 아군 영토만 통과하는 최단 경로의 다음 성을 너비 우선 탐색으로 찾습니다.
        private static string GetNextFriendlyStep(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string originId,
            string destinationId,
            string factionId)
        {
            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>();
            queue.Enqueue(originId);
            previous[originId] = string.Empty;
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (current == destinationId) break;
                WICastleRuntimeState currentCastle = state.GetCastle(current);
                if (currentCastle == null) continue;
                foreach (string adjacentId in currentCastle.AdjacentCastleIds)
                {
                    if (previous.ContainsKey(adjacentId) || state.GetCastle(adjacentId)?.FactionId != factionId) continue;
                    previous[adjacentId] = current;
                    queue.Enqueue(adjacentId);
                }
            }

            if (previous.ContainsKey(destinationId) == false) return string.Empty;
            string step = destinationId;
            while (previous[step] != originId && string.IsNullOrEmpty(previous[step]) == false)
            {
                step = previous[step];
            }
            return step == originId ? string.Empty : step;
        }

        // 현재 이동 및 전투 대기 상태를 기준으로 플레이어 성의 침공 경고를 다시 계산합니다.
        private static void RefreshInvasionWarnings(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.InvasionWarning = false;
            }
            foreach (WIArmyState army in state.Armies.Where(item => item.FactionId != state.PlayerFactionId))
            {
                string threatenedId = army.IsMoving ? army.TargetCastleId : (army.AwaitingBattle ? army.CurrentCastleId : string.Empty);
                WICastleRuntimeState target = state.GetCastle(threatenedId);
                if (target != null && target.FactionId == state.PlayerFactionId)
                {
                    target.InvasionWarning = true;
                }
            }
        }

        // 해당 성이 플레이어 영토와 인접했는지 확인합니다.
        private static bool IsAdjacentToPlayer(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle)
        {
            return castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId);
        }

        // 영지관 위임 성에 이번 달 사업과 담당자를 자동으로 배치합니다.
        private static void AssignDelegatedProject(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState,
            WITurnSummary summary)
        {
            if (castleState.DelegatedToGovernor == false || castleState.ActiveProject != null)
            {
                return;
            }

            WIHeroDefinition governor = database.GetHero(castleState.GovernorHeroId);
            if (governor == null || castleState.HeroIds.Contains(governor.Id) == false ||
                IsAdministrationCapable(state, governor.Id) == false)
            {
                summary.DelegationReports.Add($"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)} · 영지관 부재 · 주둔 인물을 영지관로 임명하면 다음 달부터 위임 가능");
                return;
            }

            if (state.IsCharacterBusy(governor.Id))
            {
                summary.DelegationReports.Add($"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)} · 영지관이 다른 임무 수행 중 · 임무 종료 후 위임 재개");
                return;
            }

            int budget = castleState.GovernorMonthlyBudget >= database.ProjectBalance.IntensiveCost
                ? database.ProjectBalance.IntensiveCost
                : database.ProjectBalance.BasicCost;
            if (state.Gold < budget)
            {
                summary.DelegationReports.Add($"{database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish)} · 금화 부족 · 최소 {budget}G 필요 / 현재 {state.Gold}G");
                return;
            }

            WICastleProjectType projectType = ChooseGovernorProject(castleState);
            WIProjectInvestment investment = budget >= database.ProjectBalance.IntensiveCost
                ? WIProjectInvestment.Intensive
                : WIProjectInvestment.Basic;
            state.Gold -= budget;
            summary.GoldSpent += budget;
            int expectedGain = GetExpectedProjectGain(database, projectType, governor, investment) +
                               GetFactionPolicyBonus(database, state.GetPlayerFactionState().Policy, projectType) +
                               GetResearchEffectBonus(database, state.GetPlayerFactionState(), WIResearchEffectType.ProjectGain) +
                               GetTitleProjectBonus(database, state, governor) + GetLegacyBonus(castleState, projectType) +
                               GetCastleSpecialtyProjectBonus(database.GetCastle(castleState.CastleId), projectType);
            string reason = GetGovernorReason(database, castleState, projectType, governor);
            castleState.ActiveProject = new WICastleProjectState
            {
                ProjectType = projectType,
                Investment = investment,
                ManagerHeroId = governor.Id,
                RemainingMonths = 1,
                Delegated = true,
                ExpectedGain = expectedGain,
                GoldCost = budget,
                PlanReason = reason
            };

            string castleName = database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish);
            summary.DelegationReports.Add($"[위임 계획] {castleName} · {reason} · 예상 +{expectedGain} · 비용 {budget}G · 기간 1개월");
        }

        // 영지관 운영 방침과 성 상태에 따라 이번 달 사업을 선택합니다.
        public static WICastleProjectType ChooseGovernorProject(WICastleRuntimeState castleState)
        {
            switch (castleState.GovernorPolicy)
            {
                case WIGovernorPolicy.Prosperity:
                    return WICastleProjectType.Prosperity;
                case WIGovernorPolicy.Research:
                    return WICastleProjectType.Technology;
                case WIGovernorPolicy.Frontline:
                    return castleState.Stability <= castleState.Defense
                        ? WICastleProjectType.Stability
                        : WICastleProjectType.Fortification;
                case WIGovernorPolicy.Talent:
                    return WICastleProjectType.Recruitment;
                default:
                    int minimum = Mathf.Min(castleState.Prosperity, castleState.Technology, castleState.Stability, castleState.Defense);
                    if (minimum == castleState.Prosperity) return WICastleProjectType.Prosperity;
                    if (minimum == castleState.Technology) return WICastleProjectType.Technology;
                    if (minimum == castleState.Stability) return WICastleProjectType.Stability;
                    return WICastleProjectType.Fortification;
            }
        }

        // 월간 보고에 표시할 영지관의 사업 선택 이유를 만듭니다.
        public static string GetGovernorReason(
            WIAdministrationDatabaseSO database,
            WICastleRuntimeState castleState,
            WICastleProjectType projectType,
            WIHeroDefinition governor)
        {
            string reason;
            switch (castleState.GovernorPolicy)
            {
                case WIGovernorPolicy.Prosperity:
                    reason = "번영 방침으로 금화 수입과 보급 기반 우선";
                    break;
                case WIGovernorPolicy.Research:
                    reason = "연구 방침으로 기술과 마나 기반 우선";
                    break;
                case WIGovernorPolicy.Frontline:
                    reason = projectType == WICastleProjectType.Stability
                        ? $"전선 방침 · 질서 {castleState.Stability}이 방어 {castleState.Defense} 이하"
                        : $"전선 방침 · 방어 {castleState.Defense}이 질서 {castleState.Stability}보다 낮음";
                    break;
                case WIGovernorPolicy.Talent:
                    reason = "인재 방침으로 탐색과 영입 기반 우선";
                    break;
                default:
                    reason = $"균형 방침 · 네 핵심 수치 중 {projectType} 대응 수치가 가장 낮음";
                    break;
            }
            int traitBonus = GetProjectTraitBonus(database, projectType, governor);
            if (traitBonus > 0)
            {
                WITraitDefinition trait = governor.Traits.Select(database.GetTrait)
                    .FirstOrDefault(item => item != null && item.ProjectTypes.Contains(projectType));
                reason += $" · {trait.DisplayName.Get(database.UseEnglish)} 특기 +{traitBonus}";
            }
            return reason;
        }

        // 현재 위임 설정으로 다음 달 선택할 사업·이유·예상 비용과 성과를 반환합니다.
        public static string GetDelegationPreview(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castleState)
        {
            WIHeroDefinition governor = database.GetHero(castleState.GovernorHeroId);
            if (governor == null) return "영지관을 임명해야 위임 계획을 계산할 수 있습니다.";
            WICastleProjectType projectType = ChooseGovernorProject(castleState);
            WIProjectInvestment investment = castleState.GovernorMonthlyBudget >= database.ProjectBalance.IntensiveCost
                ? WIProjectInvestment.Intensive : WIProjectInvestment.Basic;
            int cost = investment == WIProjectInvestment.Intensive
                ? database.ProjectBalance.IntensiveCost : database.ProjectBalance.BasicCost;
            int gain = GetExpectedProjectGain(database, projectType, governor, investment) +
                       GetFactionPolicyBonus(database, state.GetPlayerFactionState().Policy, projectType) +
                       GetResearchEffectBonus(database, state.GetPlayerFactionState(), WIResearchEffectType.ProjectGain) +
                       GetTitleProjectBonus(database, state, governor) + GetLegacyBonus(castleState, projectType) +
                       GetCastleSpecialtyProjectBonus(database.GetCastle(castleState.CastleId), projectType);
            return $"예상 계획 · {projectType} · {GetGovernorReason(database, castleState, projectType, governor)} · 성과 +{gain} · 비용 {cost}G · 1개월";
        }

        // 진영 방침과 일치하는 사업에 적용할 소규모 성과 보너스를 반환합니다.
        public static int GetFactionPolicyBonus(WIAdministrationDatabaseSO database, WIFactionPolicy policy, WICastleProjectType projectType)
        {
            bool matched =
                (policy == WIFactionPolicy.Prosperity && projectType == WICastleProjectType.Prosperity) ||
                (policy == WIFactionPolicy.Development && projectType == WICastleProjectType.Technology) ||
                (policy == WIFactionPolicy.Stability && projectType == WICastleProjectType.Stability) ||
                (policy == WIFactionPolicy.Defense && (projectType == WICastleProjectType.Fortification || projectType == WICastleProjectType.Recovery)) ||
                (policy == WIFactionPolicy.Expedition && projectType == WICastleProjectType.Training) ||
                (policy == WIFactionPolicy.Talent && projectType == WICastleProjectType.Recruitment);
            return matched ? database.ProjectBalance.PolicyGainBonus : 0;
        }

        // 사업 종류에 알맞은 담당 인물 능력치를 반환합니다.
        public static int GetProjectRelevantStat(WICastleProjectType projectType, WIHeroDefinition manager)
        {
            if (manager == null)
            {
                return 0;
            }

            switch (projectType)
            {
                case WICastleProjectType.Prosperity:
                case WICastleProjectType.Fortification:
                case WICastleProjectType.Expansion:
                    return manager.Politics;
                case WICastleProjectType.Technology:
                case WICastleProjectType.Recruitment:
                    return manager.Intelligence;
                case WICastleProjectType.Stability:
                    return Mathf.Max(manager.Might, manager.Charisma);
                case WICastleProjectType.Training:
                    return manager.Leadership;
                case WICastleProjectType.Recovery:
                    return manager.Charisma;
                default:
                    return 0;
            }
        }

        // 사업 결과를 해당 성 수치에 적용합니다.
        private static void ApplyProjectGain(
            WICastleRuntimeState castleState,
            WICastleProjectType projectType,
            int gain)
        {
            switch (projectType)
            {
                case WICastleProjectType.Prosperity:
                    castleState.Prosperity = Mathf.Clamp(castleState.Prosperity + gain, 0, 100);
                    break;
                case WICastleProjectType.Technology:
                    castleState.Technology = Mathf.Clamp(castleState.Technology + gain, 0, 100);
                    break;
                case WICastleProjectType.Stability:
                    castleState.Stability = Mathf.Clamp(castleState.Stability + gain, 0, 100);
                    break;
                case WICastleProjectType.Fortification:
                    castleState.Defense = Mathf.Clamp(castleState.Defense + gain, 0, 100);
                    break;
            }
        }

        // 사업 종류, 담당 인물과 투자 등급으로 예상 성과를 계산합니다.
        public static int GetExpectedProjectGain(
            WIAdministrationDatabaseSO database,
            WICastleProjectType projectType,
            WIHeroDefinition manager,
            WIProjectInvestment investment)
        {
            int relevantStat = GetProjectRelevantStat(projectType, manager);
            WIProjectBalanceDefinition balance = database.ProjectBalance;
            int investmentBonus = investment == WIProjectInvestment.Intensive ? balance.IntensiveGainBonus : 0;
            int traitBonus = GetProjectTraitBonus(database, projectType, manager);
            return Mathf.Clamp(balance.BaseGain + relevantStat / Mathf.Max(1, balance.StatDivisor) + investmentBonus + traitBonus,
                balance.MinimumGain, balance.MaximumGain);
        }

        // 사업 종류와 투자 등급에 맞는 금화 비용을 반환합니다.
        public static int GetProjectCost(WIAdministrationDatabaseSO database, WICastleProjectType projectType, WIProjectInvestment investment)
        {
            WIProjectBalanceDefinition balance = database.ProjectBalance;
            int basicCost = projectType == WICastleProjectType.Expansion
                ? balance.ExpansionBasicCost
                : balance.BasicCost;
            return investment == WIProjectInvestment.Intensive
                ? Mathf.RoundToInt(basicCost * (balance.IntensiveCost / (float)Mathf.Max(1, balance.BasicCost)))
                : basicCost;
        }

        // 담당 인물의 특기가 사업과 일치할 때 적용할 성과 보너스를 반환합니다.
        public static int GetProjectTraitBonus(WIAdministrationDatabaseSO database, WICastleProjectType projectType, WIHeroDefinition manager)
        {
            if (manager == null)
            {
                return 0;
            }

            bool matched = manager.Traits.Any(trait =>
            {
                WITraitDefinition definition = database.GetTrait(trait);
                return definition != null && definition.ProjectTypes.Contains(projectType);
            });
            return matched ? database.ProjectBalance.TraitGainBonus : 0;
        }

        // 번영, 기술, 질서와 성 규모를 바탕으로 월간 자원 수입을 계산합니다.
        public static void AddCastleIncome(
            WICastleDefinition castleDefinition,
            WICastleRuntimeState castleState,
            WITurnSummary summary)
        {
            int sizeMultiplier = (int)castleState.CastleSize + 1;
            int stabilityRate = 50 + castleState.Stability / 2;
            summary.GoldGained += Mathf.Max(1, castleState.Prosperity * sizeMultiplier * stabilityRate / 100);
            summary.ManaGained += Mathf.Max(0, castleState.Technology * sizeMultiplier / 10);
            summary.InfluenceGained += Mathf.Max(1, castleState.Stability * sizeMultiplier / 25);

            if (castleDefinition != null)
            {
                int value = castleDefinition.SpecialtyEffectValue;
                if (castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.GoldIncome) summary.GoldGained += value;
                if (castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.ManaIncome) summary.ManaGained += value;
                if (castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.InfluenceIncome) summary.InfluenceGained += value;
            }

            if (castleState.SpecialFacilityIds.Contains("mage_tower"))
            {
                summary.ManaGained += 10 * sizeMultiplier;
            }

            if (castleState.SpecialFacilityIds.Contains("grand_market"))
            {
                summary.GoldGained += 15 * sizeMultiplier;
            }
        }

        // 성 전문 분야가 지정 사업에 제공하는 성과 보너스를 반환합니다.
        public static int GetCastleSpecialtyProjectBonus(WICastleDefinition castle, WICastleProjectType projectType)
        {
            return castle != null && castle.SpecialtyEffectType == WICastleSpecialtyEffectType.ProjectGain &&
                   castle.SpecialtyProjectType == projectType
                ? castle.SpecialtyEffectValue
                : 0;
        }
    }
}
