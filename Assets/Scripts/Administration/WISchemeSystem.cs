using System;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public readonly struct WISchemeResult
    {
        public WISchemeResult(bool executed, bool succeeded, int successChance, string message)
        {
            Executed = executed;
            Succeeded = succeeded;
            SuccessChance = successChance;
            Message = message;
        }

        public bool Executed { get; }
        public bool Succeeded { get; }
        public int SuccessChance { get; }
        public string Message { get; }
    }

    public static class WISchemeSystem
    {
        // 계략의 조건과 비용을 확인하고 지정된 판정값으로 결과를 즉시 적용합니다.
        public static WISchemeResult Execute(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string schemeId,
            string initiatorFactionId,
            string agentHeroId,
            string targetCastleId,
            string targetHeroId,
            int roll)
        {
            WISchemeDefinition scheme = database.GetScheme(schemeId);
            WIFactionRuntimeState faction = state.GetFactionState(initiatorFactionId);
            WIHeroDefinition agent = database.GetHero(agentHeroId);
            WICastleRuntimeState agentCastle = state.Castles.FirstOrDefault(castle =>
                castle.FactionId == initiatorFactionId && castle.HeroIds.Contains(agentHeroId));
            WICastleRuntimeState targetCastle = state.GetCastle(targetCastleId);
            if (scheme == null || faction == null || agent == null || agentCastle == null || targetCastle == null ||
                state.IsCharacterBusy(agentHeroId) || faction.Influence < scheme.InfluenceCost)
            {
                return new WISchemeResult(false, false, 0, "계략 조건이나 영향력이 부족합니다.");
            }

            bool targetsOwnCastle = scheme.SchemeType == WISchemeType.Counterintelligence;
            if ((targetsOwnCastle && targetCastle.FactionId != initiatorFactionId) ||
                (targetsOwnCastle == false && targetCastle.FactionId == initiatorFactionId))
            {
                return new WISchemeResult(false, false, 0, "계략 대상이 올바르지 않습니다.");
            }

            WICharacterRuntimeState targetCharacter = null;
            if (scheme.SchemeType == WISchemeType.Alienation)
            {
                targetCharacter = state.GetCharacter(targetHeroId);
                if (targetCharacter == null || targetCastle.HeroIds.Contains(targetHeroId) == false)
                {
                    return new WISchemeResult(false, false, 0, "이간할 인물을 선택해야 합니다.");
                }
            }

            faction.Influence -= scheme.InfluenceCost;
            int defensePenalty = targetCastle.Stability / 2 + (targetCastle.CounterintelligenceMonths > 0 ? 20 : 0);
            int chance = Mathf.Clamp(scheme.BaseSuccessChance + agent.Intelligence / 2 - defensePenalty, 10, 90);
            bool succeeded = scheme.SchemeType == WISchemeType.Counterintelligence || roll < chance;
            if (succeeded == false)
            {
                return new WISchemeResult(true, false, chance, $"{scheme.DisplayName.Get(database.UseEnglish)}에 실패했습니다.");
            }

            ApplyEffect(state, scheme, initiatorFactionId, targetCastle, targetCharacter);
            return new WISchemeResult(true, true, chance, $"{scheme.DisplayName.Get(database.UseEnglish)}에 성공했습니다.");
        }

        // 성공한 계략의 정보 공개, 방첩, 치안 또는 충성 효과를 적용합니다.
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

        // 모략 성향 AI가 숨은 자원 없이 유휴 인물과 보유 영향력으로 한 번의 계략을 시도합니다.
        public static void ExecuteAISchemes(WIAdministrationDatabaseSO database, WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WIFactionDefinition faction in database.Factions.Where(item =>
                         item.PlayerFaction == false && item.AIStrategy == WIAIStrategy.Scheme))
            {
                WICastleRuntimeState baseCastle = state.Castles.FirstOrDefault(castle =>
                    castle.FactionId == faction.Id && castle.HeroIds.Any(heroId => state.IsCharacterBusy(heroId) == false));
                string agentId = baseCastle?.HeroIds.FirstOrDefault(heroId => state.IsCharacterBusy(heroId) == false);
                WICastleRuntimeState target = state.Castles
                    .Where(castle => castle.FactionId != faction.Id)
                    .OrderBy(castle => castle.Stability)
                    .FirstOrDefault();
                if (agentId == null || target == null)
                {
                    continue;
                }

                WISchemeResult result = Execute(database, state, "scheme_rumor", faction.Id, agentId,
                    target.CastleId, null, (state.Turn * 17 + target.Stability) % 100);
                if (result.Succeeded && target.FactionId == state.PlayerFactionId)
                {
                    summary.News.Add($"적의 유언비어로 {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)}의 치안이 흔들렸습니다.");
                }
            }
        }
    }
}
