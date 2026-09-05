using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
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
                if (character == null)
                {
                    continue;
                }
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
    }
}
