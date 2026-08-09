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
        // 모든 성이 기본으로 보유한 시설과 역할을 안내합니다.
        private void OpenBasicFacilityModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("기본 시설");
            panel.Add(new Label("성관 · 영지관 임명과 성 운영"));
            panel.Add(new Label("시장 · 기본 거래와 영지 수입"));
            panel.Add(new Label("훈련소 · 개인 및 합동 훈련"));
            panel.Add(new Label("선술집 · 소문, 인재 단서와 월간 의뢰"));
            panel.Add(new Label("기본 시설은 건설하거나 강화하지 않습니다."));
            Button tavernQuests = new Button(OpenTavernQuestModal);
            tavernQuests.text = "선술집 월간 의뢰 확인";
            panel.Add(tavernQuests);
        }

        // 현재 성의 선술집에 게시된 월간 의뢰를 표시합니다.
        private void OpenTavernQuestModal()
        {
            VisualElement panel = CreateModal("선술집 월간 의뢰");
            if (selectedCastle.TavernQuests.Count == 0)
            {
                panel.Add(new Label("다음 달부터 새로운 의뢰가 게시됩니다."));
                return;
            }

            foreach (WITavernQuestState quest in selectedCastle.TavernQuests)
            {
                WITavernQuestDefinition definition = database.GetTavernQuest(quest.QuestType);
                if (definition == null) continue;
                Button button = new Button(() => OpenQuestHeroModal(quest));
                button.text = quest.Status == WIQuestStatus.Accepted
                    ? $"진행 중 · {definition.DisplayName.Get(database.UseEnglish)} · {quest.RemainingMonths}개월"
                    : $"{definition.DisplayName.Get(database.UseEnglish)} · {definition.DurationMonths}개월 · 금화 {definition.GoldReward} · 공훈 {definition.MeritReward} · 명성 {definition.ReputationReward}";
                button.SetEnabled(quest.Status == WIQuestStatus.Available);
                panel.Add(button);
                panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            }
        }

        // 선술집 의뢰를 맡길 수 있는 주둔 인물을 선택합니다.
        private void OpenQuestHeroModal(WITavernQuestState quest)
        {
            VisualElement panel = CreateModal("의뢰 담당 인물 선택");
            WITavernQuestDefinition definition = database.GetTavernQuest(quest.QuestType);
            if (definition == null) return;
            panel.Add(new Label($"권장 적성 · {definition.Aptitude} {definition.AptitudeThreshold} 이상 · 달성 시 금화 +{definition.AptitudeBonusGold}"));
            bool hasCandidate = false;
            foreach (string heroId in selectedCastle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId))
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition hero = database.GetHero(heroId);
                Button button = new Button(() =>
                {
                    quest.AssignedHeroId = heroId;
                    quest.Status = WIQuestStatus.Accepted;
                    quest.RemainingMonths = definition.DurationMonths;
                    CloseModal();
                    SelectCastle(selectedCastle.CastleId);
                });
                int aptitude = WIAdministrationTurnSystem.GetQuestAptitude(hero, definition.Aptitude);
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · {definition.Aptitude} {aptitude}{(aptitude >= definition.AptitudeThreshold ? " · 적합" : string.Empty)}";
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 현재 성에서 이번 달 개인 활동을 수행할 인물을 선택합니다.
        private void OpenCharacterActivityModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("인재 활동 · 인물 선택");
            bool hasCandidate = false;
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                WIHeroDefinition hero = database.GetHero(heroId);
                if (character == null || hero == null || state.IsCharacterBusy(heroId) || character.InjuryMonths > 0)
                {
                    continue;
                }

                hasCandidate = true;
                Button button = new Button(() => OpenActivityTypeModal(character));
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 피로 {character.Fatigue}";
                panel.Add(button);
                Button moveButton = new Button(() => OpenCharacterTransferModal(character));
                moveButton.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 인접 성 이동";
                panel.Add(moveButton);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 선택 인물이 이동할 수 있는 같은 진영의 인접 성 목록을 표시합니다.
        private void OpenCharacterTransferModal(WICharacterRuntimeState character)
        {
            WICastleDefinition origin = database.GetCastle(selectedCastle.CastleId);
            VisualElement panel = CreateModal("인물 이동");
            bool hasDestination = false;
            foreach (string targetId in origin.AdjacentCastleIds)
            {
                WICastleRuntimeState target = state.GetCastle(targetId);
                if (target == null || target.FactionId != selectedCastle.FactionId)
                {
                    continue;
                }

                hasDestination = true;
                WICastleDefinition targetDefinition = database.GetCastle(targetId);
                Button button = new Button(() =>
                {
                    bool started = WIAdministrationTurnSystem.StartCharacterTransfer(
                        database, state, character.HeroId, targetId);
                    if (started == false)
                    {
                        ShowMessage("이동할 수 없습니다. 임무, 인접 경로와 목적지 슬롯을 확인하십시오.");
                        return;
                    }

                    CloseModal();
                    SelectCastle(selectedCastle.CastleId);
                    ShowMessage($"{targetDefinition.DisplayName.Get(database.UseEnglish)} 이동을 시작했습니다. 다음 달에 도착합니다.");
                });
                button.text = $"{targetDefinition.DisplayName.Get(database.UseEnglish)} · 인물 {target.HeroIds.Count}/{target.GetHeroSlotCount()}";
                panel.Add(button);
            }

            if (hasDestination == false)
            {
                panel.Add(new Label("이동 가능한 같은 진영의 인접 성이 없습니다."));
            }
        }

        // 선택한 인물에게 배정할 개인 활동 종류를 표시합니다.
        private void OpenActivityTypeModal(WICharacterRuntimeState character)
        {
            WIHeroDefinition hero = database.GetHero(character.HeroId);
            VisualElement panel = CreateModal($"{hero.DisplayName.Get(database.UseEnglish)} · 개인 활동");
            AddActivityButton(panel, character, WICharacterActivityType.Search, "탐색 · 방랑 인재와 단서 발견");
            AddActivityButton(panel, character, WICharacterActivityType.Training, "훈련 · 경험과 공훈 획득");
            AddActivityButton(panel, character, WICharacterActivityType.Rest, "휴식 · 피로와 부상 회복");

            Button socialize = new Button(() => OpenSocializeTargetModal(character));
            socialize.text = "교류 · 주둔 인물과 관계 개선";
            panel.Add(socialize);
            Button recruit = new Button(() => OpenRecruitTargetModal(character));
            recruit.text = "영입 · 발견한 인재 설득";
            panel.Add(recruit);
        }

        // 대상이 필요 없는 개인 활동 버튼을 만들어 모달에 추가합니다.
        private void AddActivityButton(
            VisualElement panel,
            WICharacterRuntimeState character,
            WICharacterActivityType activity,
            string label)
        {
            Button button = new Button(() => AssignCharacterActivity(character, activity, string.Empty));
            button.text = label;
            panel.Add(button);
        }

        // 교류할 같은 성의 주둔 인물을 선택합니다.
        private void OpenSocializeTargetModal(WICharacterRuntimeState character)
        {
            VisualElement panel = CreateModal("교류 대상 선택");
            foreach (string targetId in selectedCastle.HeroIds)
            {
                if (targetId == character.HeroId)
                {
                    continue;
                }

                WIHeroDefinition targetHero = database.GetHero(targetId);
                Button button = new Button(() => AssignCharacterActivity(character, WICharacterActivityType.Socialize, targetId));
                button.text = targetHero.DisplayName.Get(database.UseEnglish);
                panel.Add(button);
            }
        }

        // 발견했지만 아직 영입하지 않은 인재 중 영입 대상을 선택합니다.
        private void OpenRecruitTargetModal(WICharacterRuntimeState character)
        {
            VisualElement panel = CreateModal("영입 대상 선택");
            bool hasCandidate = false;
            foreach (WICharacterRuntimeState candidate in state.Characters)
            {
                if (candidate.Discovered == false || candidate.Recruited)
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition targetHero = database.GetHero(candidate.HeroId);
                Button button = new Button(() => AssignCharacterActivity(character, WICharacterActivityType.Recruit, candidate.HeroId));
                string request = targetHero.RecruitmentRequest == null ? string.Empty : targetHero.RecruitmentRequest.Get(database.UseEnglish);
                button.text = $"{targetHero.DisplayName.Get(database.UseEnglish)} · 설득 {candidate.RecruitmentProgress}% · 필요 명성 {targetHero.RequiredReputation} · {request}";
                button.SetEnabled(character.Reputation >= targetHero.RequiredReputation);
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                panel.Add(new Label("먼저 탐색이나 인재 사업으로 인재를 발견해야 합니다."));
            }
        }

        // 선택한 인물에게 이번 달 개인 활동과 대상을 배정합니다.
        private void AssignCharacterActivity(
            WICharacterRuntimeState character,
            WICharacterActivityType activity,
            string targetHeroId)
        {
            character.Activity = activity;
            character.ActivityTargetHeroId = targetHeroId;
            CloseModal();
            SelectCastle(selectedCastle.CastleId);
        }

        // 인물 등급의 UI 표시명을 반환합니다.
        private string GetGradeDisplayName(WICharacterGrade grade)
        {
            return grade == WICharacterGrade.Hero ? "영웅" : "일반";
        }

        // 인물과 연결된 주요 시작 관계를 상대 이름과 단계 문구로 요약합니다.
        private string GetRelationshipSummary(string heroId)
        {
            List<string> entries = new List<string>();
            foreach (WIRelationshipState relationship in state.Relationships)
            {
                string counterpartId = relationship.FirstHeroId == heroId
                    ? relationship.SecondHeroId
                    : relationship.SecondHeroId == heroId ? relationship.FirstHeroId : string.Empty;
                if (string.IsNullOrEmpty(counterpartId)) continue;
                WIHeroDefinition counterpart = database.GetHero(counterpartId);
                if (counterpart == null) continue;
                string level = relationship.Level == WIRelationshipLevel.Conflict ? "갈등"
                    : relationship.Level == WIRelationshipLevel.Fondness ? "친애" : "보통";
                entries.Add($"{counterpart.DisplayName.Get(database.UseEnglish)}({level})");
            }

            return entries.Count == 0 ? string.Empty : $" · 관계 {string.Join(", ", entries)}";
        }

        // 미배치 영웅을 현재 성에 배치하는 모달을 엽니다.
        private void OpenHeroAssignmentModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            if (selectedCastle.HeroIds.Count >= selectedCastle.GetHeroSlotCount())
            {
                ShowMessage("현재 성 규모의 주둔 인물 슬롯이 가득 찼습니다.");
                return;
            }

            List<WIHeroDefinition> candidates = new List<WIHeroDefinition>();
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || state.IsCharacterBusy(character.HeroId))
                {
                    continue;
                }

                string heroId = character.HeroId;
                bool assigned = state.Castles.Exists(item => item.HeroIds.Contains(heroId));
                if (assigned == false)
                {
                    candidates.Add(database.GetHero(heroId));
                }
            }

            if (candidates.Count == 0)
            {
                ShowMessage("배치 가능한 대기 인물이 없습니다. 진행 중인 임무가 끝나거나 전투단을 해산한 뒤 다시 시도하십시오.");
                return;
            }

            ShowChoiceModal(database.GetText("UI_ASSIGN_HERO"), candidates, hero =>
            {
                selectedCastle.HeroIds.Add(hero.Id);
                CloseModal();
                SelectCastle(selectedCastle.CastleId);
            });
        }

        // 영입된 전체 영웅 목록을 표시합니다.
        private void OpenHeroListModal()
        {
            VisualElement panel = CreateModal(database.GetText("UI_ALL_HEROES"));
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false)
                {
                    continue;
                }

                WIHeroDefinition hero = database.GetHero(character.HeroId);
                string activity = character.Activity == WICharacterActivityType.None ? "대기" : character.Activity.ToString();
                if (character.IsDead) activity = "사망";
                else if (character.Captured) activity = $"포로 · {database.GetFaction(character.CaptorFactionId)?.DisplayName.Get(database.UseEnglish)} · {character.CapturedMonthsRemaining}개월";
                WICharacterTransferState transfer = state.CharacterTransfers.Find(item => item.HeroId == character.HeroId);
                if (transfer != null)
                {
                    activity = $"이동 중 · {database.GetCastle(transfer.TargetCastleId).DisplayName.Get(database.UseEnglish)} · {transfer.RemainingMonths}개월";
                }
                WICharacterGrade effectiveGrade = character.PromotedToHero ? WICharacterGrade.Hero : character.BaseGrade;
                WIHeroClassDefinition classDefinition = database.GetHeroClass(hero.HeroClass);
                string classTendency = classDefinition == null
                    ? hero.HeroClass.ToString()
                    : $"{classDefinition.DisplayName.Get(database.UseEnglish)} · 주 {classDefinition.PrimaryStat} / 보조 {classDefinition.SecondaryStat} · 권장 {GetUnitRoleDisplayName(classDefinition.RecommendedRole)}";
                string relationships = GetRelationshipSummary(hero.Id);
                panel.Add(new Label($"[{GetGradeDisplayName(effectiveGrade)}] {hero.DisplayName.Get(database.UseEnglish)} · {classTendency} · 공훈 {character.Merit} · 명성 {character.Reputation} · {character.LoyaltyState} · {activity}{relationships}"));
                panel.Add(new Label($"특기: {GetTraitDisplayText(hero)} · 피로 {character.Fatigue} · 부상 {character.InjuryMonths}개월"));
                if (state.PendingHeroPromotionIds.Contains(character.HeroId))
                {
                    Button promotionButton = new Button(() =>
                    {
                        bool promoted = WIAdministrationTurnSystem.PromoteCommonCharacter(database, state, character.HeroId);
                        CloseModal();
                        RefreshAll();
                        ShowMessage(promoted ? "영웅 승격을 확정했습니다." : "승격에 필요한 영향력이 부족합니다.");
                    });
                    promotionButton.text = $"영웅 승격 · 영향력 {database.PromotionInfluenceCost}";
                    panel.Add(promotionButton);
                }
                Button titleButton = new Button(() => OpenTitleModal(character));
                WITitleDefinition currentTitle = database.GetTitle(character.TitleId);
                titleButton.text = currentTitle == null
                    ? "작위 수여"
                    : $"현재 작위: {currentTitle.DisplayName.Get(database.UseEnglish)} · 변경";
                panel.Add(titleButton);
            }

            foreach (WICharacterRuntimeState candidate in state.Characters)
            {
                if (candidate.Discovered && candidate.Recruited == false)
                {
                    WIHeroDefinition hero = database.GetHero(candidate.HeroId);
                    panel.Add(new Label($"[발견 인재] {hero.DisplayName.Get(database.UseEnglish)} · 영입 설득 {candidate.RecruitmentProgress}%"));
                }
            }
        }

        // 인물의 공훈과 진영 영향력으로 수여할 수 있는 작위를 표시합니다.
        private void OpenTitleModal(WICharacterRuntimeState character)
        {
            WIHeroDefinition hero = database.GetHero(character.HeroId);
            VisualElement panel = CreateModal($"작위 수여 · {hero.DisplayName.Get(database.UseEnglish)} · 공훈 {character.Merit}");
            foreach (WITitleDefinition title in database.TitleDefinitions)
            {
                Button button = new Button(() =>
                {
                    bool awarded = WIAdministrationTurnSystem.AwardTitle(
                        database, state, state.PlayerFactionId, character.HeroId, title.Id);
                    CloseModal();
                    RefreshAll();
                    ShowMessage(awarded ? "작위를 수여했습니다." : "공훈 또는 영향력이 부족합니다.");
                });
                button.text = $"{title.DisplayName.Get(database.UseEnglish)} · 공훈 {title.RequiredMerit} · 영향력 {title.InfluenceCost} · 영지 관리 +{title.ProjectBonus} · 전투 +{title.BattlePowerBonus}";
                panel.Add(button);
            }
        }

        // 인물이 가진 특기 목록을 한국어 UI 문자열로 조합합니다.
        private string GetTraitDisplayText(WIHeroDefinition hero)
        {
            List<string> names = new List<string>();
            foreach (WITraitType trait in hero.Traits)
            {
                WITraitDefinition definition = database.GetTrait(trait);
                if (definition != null)
                {
                    names.Add($"{definition.DisplayName.Get(database.UseEnglish)} · {definition.Description.Get(database.UseEnglish)}");
                }
            }

            return names.Count == 0 ? "없음" : string.Join(", ", names);
        }

    }
}
