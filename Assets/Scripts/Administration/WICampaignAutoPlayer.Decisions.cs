using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WICampaignAutoPlayer
    {
        // 정책별 점수로 사업·관계·지역·점령·영입·흔적 선택을 자동 해결합니다.
        private static void ResolvePendingDecisions(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WITurnSummary summary)
        {
            foreach (WIPendingProjectEvent pending in state.PendingProjectEvents.ToList())
            {
                bool bold = policy == WIAutoPlayerPolicy.Aggressive;
                int gain = pending.HeroChoiceAvailable && policy != WIAutoPlayerPolicy.Administration ? 5 : (bold ? 4 : 2);
                if (WIAdministrationTurnSystem.ResolveProjectEvent(state, pending, gain, bold, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"project:{policy}");
                }
            }

            foreach (WIPendingRelationshipEvent pending in state.PendingRelationshipEvents.ToList())
            {
                WIRelationshipEventDefinition definition = database.GetRelationshipEvent(pending.EventId);
                int index = SelectBestIndex(definition?.Choices, choice =>
                    choice.RelationshipShift * (policy == WIAutoPlayerPolicy.Aggressive ? 2 : 5) +
                    choice.MeritDelta * 2 - Mathf.Max(0, choice.FatigueDelta));
                if (index >= 0 && WIAdministrationTurnSystem.ResolveRelationshipEvent(
                        database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"relationship:{index}");
                }
            }

            foreach (WIPendingRegionalEvent pending in state.PendingRegionalEvents.ToList())
            {
                WIRegionalEventDefinition definition = database.GetRegionalEvent(pending.EventId);
                bool reserveWarInfluence = ShouldReserveValdorWarInfluence(state);
                int index = SelectBestIndex(definition?.Choices,
                    choice => ScoreRegionalChoice(choice, policy),
                    choice => WIRegionalEventSystem.CanChoose(state, choice) &&
                              (reserveWarInfluence == false || choice.InfluenceDelta >= 0));
                if (index >= 0 && WIRegionalEventSystem.Resolve(database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"regional:{index}");
                }
            }

            foreach (WIPendingOccupationEvent pending in state.PendingOccupationEvents.ToList())
            {
                bool reserveWarInfluence = ShouldReserveValdorWarInfluence(state);
                int index = SelectBestIndex(database.OccupationChoices,
                    choice => ScoreOccupationChoice(choice, policy),
                    choice => WIOccupationEventSystem.CanChoose(state, choice) &&
                              (reserveWarInfluence == false || choice.InfluenceDelta >= 0));
                if (index >= 0 && WIOccupationEventSystem.Resolve(database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"occupation:{index}");
                }
            }

            foreach (WIPendingRecruitmentEvent pending in state.PendingRecruitmentEvents.ToList())
            {
                WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(pending.EventId);
                int index = SelectBestIndex(definition?.Choices,
                    choice => choice.RecruiterMeritGain * 2 - Mathf.Max(0, choice.RecruiterFatigueGain),
                    choice => WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(
                        database, state, pending, choice));
                if (index >= 0 && WIAdministrationTurnSystem.ResolveRecruitmentEvent(
                        database, state, pending, index, summary))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice($"recruitment:{index}");
                }
            }

            foreach (WIPendingLegacyChoice pending in state.PendingLegacyChoices.ToList())
            {
                WICastleRuntimeState castle = state.GetCastle(pending.CastleId);
                WIHeroLegacyState replaced = castle != null && castle.HeroLegacies.Count >= 2
                    ? castle.HeroLegacies.OrderBy(item => item.Bonus).FirstOrDefault()
                    : null;
                if (WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pending, replaced))
                {
                    metrics.DecisionsResolved += 1;
                    metrics.RecordChoice("legacy");
                }
            }
        }

        // 발도르 최종 전쟁이 끊긴 경우 선전포고 비용을 모을 때까지 사건의 영향력 소비를 보류합니다.
        private static bool ShouldReserveValdorWarInfluence(WIAdministrationState state)
        {
            return IsFinalValdorCampaign(state) &&
                   WIAdministrationTurnSystem.AreFactionsAtWar(state, state.PlayerFactionId, "valdor") == false &&
                   state.Influence < WIAdministrationTurnSystem.DeclareWarInfluenceCost;
        }

        // 지역 사건 선택지를 정책별 자원·성장·군사 가치로 평가합니다.
        private static int ScoreRegionalChoice(
            WIRegionalEventChoiceDefinition choice,
            WIAutoPlayerPolicy policy)
        {
            int economy = choice.GoldDelta + choice.ManaDelta * 2 + choice.InfluenceDelta * 2;
            int growth = choice.ProsperityDelta * 4 + choice.TechnologyDelta * 4;
            int security = choice.StabilityDelta * 4 + choice.DefenseDelta * 4;
            if (policy == WIAutoPlayerPolicy.Administration)
            {
                return economy + growth * 2 + security;
            }
            if (policy == WIAutoPlayerPolicy.Aggressive)
            {
                return economy + growth + security * 2;
            }
            return economy + growth + security;
        }

        // 점령 통치 선택지를 정책별 안정·성장·방어 가치로 평가합니다.
        private static int ScoreOccupationChoice(
            WIOccupationChoiceDefinition choice,
            WIAutoPlayerPolicy policy)
        {
            int economy = choice.GoldDelta + choice.InfluenceDelta * 2;
            int administration = choice.StabilityDelta * 5 + choice.ProsperityDelta * 4 - choice.UnrestMonths * 3;
            int military = choice.DefenseDelta * 6 - choice.UnrestMonths * 2;
            return policy == WIAutoPlayerPolicy.Aggressive
                ? economy + administration + military * 2
                : (policy == WIAutoPlayerPolicy.Administration
                    ? economy + administration * 2 + military
                    : economy + administration + military);
        }

        // 선택지 목록에서 조건을 만족하는 최고 점수의 인덱스를 반환합니다.
        private static int SelectBestIndex<T>(
            IReadOnlyList<T> choices,
            Func<T, int> score,
            Func<T, bool> predicate = null)
        {
            if (choices == null || choices.Count == 0)
            {
                return -1;
            }

            int selectedIndex = -1;
            int selectedScore = int.MinValue;
            for (int index = 0; index < choices.Count; index += 1)
            {
                if (predicate != null && predicate(choices[index]) == false)
                {
                    continue;
                }
                int currentScore = score(choices[index]);
                if (currentScore > selectedScore)
                {
                    selectedIndex = index;
                    selectedScore = currentScore;
                }
            }
            return selectedIndex;
        }

        // 현재 해결을 기다리는 플레이어 선택 수를 반환합니다.
        private static int CountPendingDecisions(WIAdministrationState state)
        {
            return state.PendingProjectEvents.Count + state.PendingRelationshipEvents.Count +
                   state.PendingRegionalEvents.Count + state.PendingOccupationEvents.Count +
                   state.PendingRecruitmentEvents.Count + state.PendingLegacyChoices.Count;
        }
    }
}
