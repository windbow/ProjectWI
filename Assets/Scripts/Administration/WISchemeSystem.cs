using System;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public readonly struct WISchemeResult
    {
        public WISchemeResult(bool executed, bool succeeded, bool detected, int successChance, int detectionChance, string message)
        {
            Executed = executed;
            Succeeded = succeeded;
            Detected = detected;
            SuccessChance = successChance;
            DetectionChance = detectionChance;
            Message = message;
        }

        public bool Executed { get; }
        public bool Succeeded { get; }
        public bool Detected { get; }
        public int SuccessChance { get; }
        public int DetectionChance { get; }
        public string Message { get; }
    }

    public static class WISchemeSystem
    {
        // 조건과 영향력을 확인해 담당 인물을 한 달 동안 점유하는 첩보 임무를 예약합니다.
        public static bool TrySchedule(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string schemeId,
            string initiatorFactionId,
            string agentHeroId,
            string targetCastleId,
            string targetHeroId,
            int roll,
            out string message)
        {
            WISchemeDefinition scheme = database.GetScheme(schemeId);
            WIFactionRuntimeState faction = state.GetFactionState(initiatorFactionId);
            WIHeroDefinition agent = database.GetHero(agentHeroId);
            WICastleRuntimeState agentCastle = state.Castles.FirstOrDefault(castle =>
                castle.FactionId == initiatorFactionId && castle.HeroIds.Contains(agentHeroId));
            WICastleRuntimeState targetCastle = state.GetCastle(targetCastleId);
            state.SchemeMissions = state.SchemeMissions ?? new System.Collections.Generic.List<WISchemeMissionState>();
            if (scheme == null || faction == null || agent == null || agentCastle == null || targetCastle == null ||
                WIAdministrationTurnSystem.CanPerformScheme(state, agentHeroId) == false ||
                state.IsCharacterBusy(agentHeroId) || faction.Influence < scheme.InfluenceCost)
            {
                message = "첩보 조건이나 영향력이 부족합니다.";
                return false;
            }
            bool targetsOwnCastle = scheme.SchemeType == WISchemeType.Counterintelligence;
            if ((targetsOwnCastle && targetCastle.FactionId != initiatorFactionId) ||
                (targetsOwnCastle == false && targetCastle.FactionId == initiatorFactionId))
            {
                message = "첩보 대상이 올바르지 않습니다.";
                return false;
            }
            if (scheme.SchemeType == WISchemeType.Alienation &&
                (string.IsNullOrEmpty(targetHeroId) || targetCastle.HeroIds.Contains(targetHeroId) == false))
            {
                message = "이간할 인물을 선택해야 합니다.";
                return false;
            }
            faction.Influence -= scheme.InfluenceCost;
            state.SchemeMissions.Add(new WISchemeMissionState
            {
                SchemeId = schemeId,
                InitiatorFactionId = initiatorFactionId,
                AgentHeroId = agentHeroId,
                TargetCastleId = targetCastleId,
                TargetHeroId = targetHeroId,
                RemainingMonths = 1,
                ResolutionRoll = Mathf.Clamp(roll, 0, 99)
            });
            message = $"{scheme.DisplayName.Get(database.UseEnglish)} 임무를 시작했습니다. 다음 턴에 결과를 판정합니다.";
            return true;
        }

        // 첩보의 조건과 비용을 확인하고 지정된 판정값으로 결과를 즉시 적용합니다.
        public static WISchemeResult Execute(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string schemeId,
            string initiatorFactionId,
            string agentHeroId,
            string targetCastleId,
            string targetHeroId,
            int roll,
            bool consumeInfluence = true)
        {
            WISchemeDefinition scheme = database.GetScheme(schemeId);
            WIFactionRuntimeState faction = state.GetFactionState(initiatorFactionId);
            WIHeroDefinition agent = database.GetHero(agentHeroId);
            WICastleRuntimeState agentCastle = state.Castles.FirstOrDefault(castle =>
                castle.FactionId == initiatorFactionId && castle.HeroIds.Contains(agentHeroId));
            WICastleRuntimeState targetCastle = state.GetCastle(targetCastleId);
            if (scheme == null || faction == null || agent == null || agentCastle == null || targetCastle == null ||
                WIAdministrationTurnSystem.CanPerformScheme(state, agentHeroId) == false ||
                state.IsCharacterBusy(agentHeroId) || (consumeInfluence && faction.Influence < scheme.InfluenceCost))
            {
                return new WISchemeResult(false, false, false, 0, 0, "첩보 조건이나 영향력이 부족합니다.");
            }

            bool targetsOwnCastle = scheme.SchemeType == WISchemeType.Counterintelligence;
            if ((targetsOwnCastle && targetCastle.FactionId != initiatorFactionId) ||
                (targetsOwnCastle == false && targetCastle.FactionId == initiatorFactionId))
            {
                return new WISchemeResult(false, false, false, 0, 0, "첩보 대상이 올바르지 않습니다.");
            }

            WICharacterRuntimeState targetCharacter = null;
            if (scheme.SchemeType == WISchemeType.Alienation)
            {
                targetCharacter = state.GetCharacter(targetHeroId);
                if (targetCharacter == null || targetCastle.HeroIds.Contains(targetHeroId) == false)
                {
                    return new WISchemeResult(false, false, false, 0, 0, "이간할 인물을 선택해야 합니다.");
                }
            }

            if (consumeInfluence) faction.Influence -= scheme.InfluenceCost;
            int chance = CalculateSuccessChance(scheme, agent, targetCastle);
            if (WIAdministrationTurnSystem.HasSpecialFacility(agentCastle, "spy_outpost"))
            {
                chance = Mathf.Clamp(chance + 10, 10, 95);
            }
            bool succeeded = scheme.SchemeType == WISchemeType.Counterintelligence || roll < chance;
            int detectionChance = CalculateDetectionChance(scheme, agent, targetCastle, succeeded);
            if (WIAdministrationTurnSystem.HasSpecialFacility(agentCastle, "spy_outpost"))
            {
                detectionChance = Mathf.Clamp(detectionChance - 10, 0, 90);
            }
            if (WIAdministrationTurnSystem.HasSpecialFacility(targetCastle, "spy_outpost"))
            {
                detectionChance = Mathf.Clamp(detectionChance + 10, 0, 95);
            }
            int detectionRoll = (Mathf.Clamp(roll, 0, 99) * 37 + 17) % 100;
            bool detected = scheme.SchemeType != WISchemeType.Counterintelligence && detectionRoll < detectionChance;
            if (succeeded)
            {
                ApplyEffect(state, scheme, initiatorFactionId, targetCastle, targetCharacter);
            }

            if (detected)
            {
                WorsenDiplomaticRelation(state, initiatorFactionId, targetCastle.FactionId);
            }

            string outcome = succeeded ? "성공" : "실패";
            string exposure = detected ? "발각되어 외교 관계가 악화되었습니다." : "정체는 드러나지 않았습니다.";
            return new WISchemeResult(true, succeeded, detected, chance, detectionChance,
                $"{scheme.DisplayName.Get(database.UseEnglish)}에 {outcome}했습니다. {exposure}");
        }

        // 담당 인물의 지력과 대상 성의 질서·방첩을 사용해 첩보 발각 확률을 계산합니다.
        public static int CalculateDetectionChance(WISchemeDefinition scheme, WIHeroDefinition agent,
            WICastleRuntimeState targetCastle, bool succeeded)
        {
            if (scheme == null || agent == null || targetCastle == null || scheme.SchemeType == WISchemeType.Counterintelligence)
            {
                return 0;
            }

            int counterintelligenceBonus = targetCastle.CounterintelligenceMonths > 0 ? 20 : 0;
            int failureBonus = succeeded ? 0 : scheme.FailureDetectionBonus;
            return Mathf.Clamp(scheme.BaseDetectionChance + targetCastle.Stability / 4 + counterintelligenceBonus +
                               failureBonus - agent.Intelligence / 3, 5, 90);
        }

        // 첩보의 기본 확률, 담당 지력, 대상 질서와 방첩을 합산해 성공 확률을 반환합니다.
        public static int CalculateSuccessChance(WISchemeDefinition scheme, WIHeroDefinition agent,
            WICastleRuntimeState targetCastle)
        {
            if (scheme == null || agent == null || targetCastle == null) return 0;
            if (scheme.SchemeType == WISchemeType.Counterintelligence) return 100;
            int defensePenalty = targetCastle.Stability / 2 + (targetCastle.CounterintelligenceMonths > 0 ? 20 : 0);
            return Mathf.Clamp(scheme.BaseSuccessChance + agent.Intelligence / 2 - defensePenalty, 10, 90);
        }

        // 발각된 적대 첩보의 두 진영 외교 단계를 한 단계 악화시킵니다.
        private static void WorsenDiplomaticRelation(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            if (relation == null) return;
            switch (relation.Status)
            {
                case WIDiplomaticStatus.Alliance:
                    relation.Status = WIDiplomaticStatus.NonAggression;
                    break;
                case WIDiplomaticStatus.NonAggression:
                    relation.Status = WIDiplomaticStatus.Friendly;
                    break;
                case WIDiplomaticStatus.Friendly:
                    relation.Status = WIDiplomaticStatus.Neutral;
                    break;
            }
            relation.AidCooldownMonths = 0;
        }

        // 성공한 첩보의 정보 공개, 방첩, 질서 또는 충성 효과를 적용합니다.
        private static void ApplyEffect(WIAdministrationState state, WISchemeDefinition scheme, string factionId,
            WICastleRuntimeState targetCastle, WICharacterRuntimeState targetCharacter)
        {
            switch (scheme.SchemeType)
            {
                case WISchemeType.Investigation:
                    WISchemeIntelState intel = state.SchemeIntel.FirstOrDefault(item =>
                        item.ObserverFactionId == factionId && item.TargetCastleId == targetCastle.CastleId);
                    if (intel == null)
                    {
                        intel = new WISchemeIntelState { ObserverFactionId = factionId, TargetCastleId = targetCastle.CastleId };
                        state.SchemeIntel.Add(intel);
                    }
                    intel.RemainingMonths = scheme.DurationMonths;
                    break;
                case WISchemeType.Counterintelligence:
                    targetCastle.CounterintelligenceMonths = scheme.DurationMonths;
                    break;
                case WISchemeType.Rumor:
                    targetCastle.Stability = Mathf.Clamp(targetCastle.Stability - scheme.EffectValue, 0, 100);
                    break;
                case WISchemeType.Alienation:
                    targetCharacter.LoyaltyState = targetCharacter.LoyaltyState == WILoyaltyState.Stable
                        ? WILoyaltyState.Unsettled
                        : WILoyaltyState.Danger;
                    break;
            }
        }

        // 월이 바뀔 때 조사 정보와 방첩의 남은 기간을 감소시키고 만료 정보를 제거합니다.
        public static void AdvanceMonth(WIAdministrationState state)
        {
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.CounterintelligenceMonths = Math.Max(0, castle.CounterintelligenceMonths - 1);
            }

            foreach (WISchemeIntelState intel in state.SchemeIntel)
            {
                intel.RemainingMonths--;
            }
            state.SchemeIntel.RemoveAll(intel => intel.RemainingMonths <= 0);
        }

        // 한 달을 마친 첩보 임무를 공통 즉시 판정 함수로 해결하고 담당 인물을 복귀시킵니다.
        public static void ResolveScheduledMissions(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            state.SchemeMissions = state.SchemeMissions ?? new System.Collections.Generic.List<WISchemeMissionState>();
            foreach (WISchemeMissionState mission in state.SchemeMissions.ToList())
            {
                mission.RemainingMonths -= 1;
                if (mission.RemainingMonths > 0) continue;
                state.SchemeMissions.Remove(mission);
                WISchemeResult result = Execute(database, state, mission.SchemeId, mission.InitiatorFactionId,
                    mission.AgentHeroId, mission.TargetCastleId, mission.TargetHeroId, mission.ResolutionRoll, false);
                WIHeroDefinition agent = database.GetHero(mission.AgentHeroId);
                WICastleDefinition target = database.GetCastle(mission.TargetCastleId);
                summary.News.Add($"첩보 결과 · {agent?.DisplayName.Get(database.UseEnglish)} · " +
                    $"{target?.DisplayName.Get(database.UseEnglish)} · {result.Message} · " +
                    $"성공률 {result.SuccessChance}% · 발각률 {result.DetectionChance}%");
            }
        }

        // 모략 성향 AI가 숨은 자원 없이 유휴 인물과 보유 영향력으로 한 번의 첩보를 시도합니다.
        public static void ScheduleAISchemes(WIAdministrationDatabaseSO database, WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WIFactionDefinition faction in database.Factions.Where(item =>
                         item.PlayerFaction == false && item.AIStrategy == WIAIStrategy.Scheme))
            {
                if (state.GetFactionState(faction.Id)?.Eliminated == true) continue;
                WICastleRuntimeState baseCastle = state.Castles.FirstOrDefault(castle =>
                    castle.FactionId == faction.Id && castle.HeroIds.Any(heroId =>
                        state.IsCharacterBusy(heroId) == false &&
                        WIAdministrationTurnSystem.CanPerformScheme(state, heroId)));
                string agentId = baseCastle?.HeroIds.FirstOrDefault(heroId =>
                    state.IsCharacterBusy(heroId) == false &&
                    WIAdministrationTurnSystem.CanPerformScheme(state, heroId));
                System.Collections.Generic.List<WICastleRuntimeState> targets = state.Castles
                    .Where(castle => castle.FactionId != faction.Id)
                    .OrderBy(castle => castle.Stability)
                    .ToList();
                int factionSalt = database.Factions.ToList().FindIndex(item => item.Id == faction.Id);
                int targetIndex = targets.Count == 0 ? -1 :
                    WIAdministrationTurnSystem.GetAICandidateIndex(database, state, targets.Count, factionSalt);
                WICastleRuntimeState target = targetIndex < 0 ? null : targets[targetIndex];
                if (agentId == null || target == null)
                {
                    continue;
                }

                if (TrySchedule(database, state, "scheme_rumor", faction.Id, agentId,
                    target.CastleId, null, (state.Turn * 17 + target.Stability) % 100, out _))
                {
                    summary.News.Add($"적 첩보 동향 · {database.GetFaction(faction.Id).DisplayName.Get(database.UseEnglish)}가 모략 임무를 시작했습니다.");
                    WIAdministrationTurnSystem.AddAIReasonReport(summary, faction.Id,
                        faction.DisplayName.Get(database.UseEnglish), "첩보",
                        $"질서 하위 {WIAdministrationTurnSystem.GetAICandidateWindow(database, state, targets.Count)}개 중 " +
                        $"{targetIndex + 1}순위 {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)}({target.Stability})에 유언비어 배정");
                }
            }
        }
    }
}
