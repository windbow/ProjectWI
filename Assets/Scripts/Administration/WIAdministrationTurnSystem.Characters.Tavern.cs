using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
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
                    if (questDefinition == null)
                    {
                        continue;
                    }
                    if (quest.RemainingMonths <= 0)
                    {
                        quest.RemainingMonths = questDefinition.DurationMonths;
                    }
                    quest.RemainingMonths -= 1;
                    if (quest.RemainingMonths > 0)
                    {
                        continue;
                    }

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
            if (hero == null)
            {
                return 0;
            }
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
                    if (candidate != null)
                    {
                        candidate.Discovered = true;
                    }
                    break;
                case WITavernQuestType.Counterintelligence:
                    castle.CounterintelligenceMonths = Mathf.Max(castle.CounterintelligenceMonths, value);
                    break;
            }
        }

    }
}
