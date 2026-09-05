using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WICampaignAutoPlayer
    {
        // 가상 플레이어가 억류된 아군을 영웅 우선으로 맞교환하거나 자원 여유에 따라 몸값으로 귀환시킵니다.
        public static bool ResolvePrisonerPolicy(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIAutoPlayerPolicy policy,
            WIAutoCampaignMetrics metrics,
            WIAutoStrategicPlan plan,
            int month)
        {
            if (database == null || state == null || metrics == null)
            {
                return false;
            }

            string playerFactionId = state.PlayerFactionId;
            WICharacterRuntimeState prisoner = state.Characters
                .Where(character => character.Captured &&
                                    character.CapturedFromFactionId == playerFactionId &&
                                    character.RansomRequested)
                .OrderByDescending(character => IsEffectiveHero(character))
                .ThenBy(character => character.CapturedMonthsRemaining)
                .ThenBy(character => character.HeroId)
                .FirstOrDefault();
            if (prisoner == null)
            {
                return false;
            }

            WICharacterRuntimeState exchangeCandidate = state.Characters
                .Where(character => character.Captured &&
                                    character.CapturedFromFactionId == prisoner.CaptorFactionId &&
                                    character.CaptorFactionId == playerFactionId)
                .OrderBy(character => IsEffectiveHero(character))
                .ThenBy(character => character.CapturedMonthsRemaining)
                .ThenBy(character => character.HeroId)
                .FirstOrDefault();
            if (exchangeCandidate != null && WIAdministrationTurnSystem.ExchangePrisoners(
                    state, playerFactionId, prisoner.CaptorFactionId,
                    prisoner.HeroId, exchangeCandidate.HeroId))
            {
                metrics.PrisonerExchanges += 1;
                metrics.PrisonersRecovered += 1;
                metrics.RecordDecision(month, plan, "PRISONER_EXCHANGED",
                    $"{prisoner.HeroId}을(를) {exchangeCandidate.HeroId}과(와) 맞교환했습니다.");
                return true;
            }

            WIFactionRuntimeState player = state.GetPlayerFactionState();
            if (CanPayPrisonerRansom(state, player, prisoner, policy) == false)
            {
                metrics.RecordDecision(month, plan, "PRISONER_RANSOM_DEFERRED",
                    $"{prisoner.HeroId}의 몸값 지불을 자원 비축 또는 일반 병력 여유 때문에 보류했습니다.");
                return false;
            }

            WIResourceType resourceType = prisoner.RansomResourceType;
            int amount = prisoner.RansomAmount;
            if (WIAdministrationTurnSystem.RansomPrisoner(
                    database, state, playerFactionId, prisoner.HeroId) == false)
            {
                return false;
            }
            metrics.PrisonerRansoms += 1;
            metrics.PrisonersRecovered += 1;
            if (resourceType == WIResourceType.ManaCrystal)
            {
                metrics.PrisonerRansomManaSpent += amount;
            }
            else
            {
                metrics.PrisonerRansomGoldSpent += amount;
            }
            metrics.RecordDecision(month, plan, "PRISONER_RANSOMED",
                $"{prisoner.HeroId}의 몸값으로 {resourceType} {amount}을(를) 지불했습니다.");
            return true;
        }

        // 영웅은 가용 자원이 있으면 구조하고 일반 인물은 병력 부족과 정책별 최소 비축량을 함께 만족할 때 구조합니다.
        private static bool CanPayPrisonerRansom(
            WIAdministrationState state,
            WIFactionRuntimeState player,
            WICharacterRuntimeState prisoner,
            WIAutoPlayerPolicy policy)
        {
            if (player == null || prisoner == null)
            {
                return false;
            }
            int available = prisoner.RansomResourceType == WIResourceType.ManaCrystal
                ? player.ManaCrystal : player.Gold;
            if (available < prisoner.RansomAmount)
            {
                return false;
            }
            if (IsEffectiveHero(prisoner))
            {
                return true;
            }

            int castleCount = state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId);
            int employedCount = state.Characters.Count(character => character.Recruited &&
                character.IsDead == false && character.Captured == false &&
                (state.Castles.Any(castle => castle.FactionId == state.PlayerFactionId &&
                                             castle.HeroIds.Contains(character.HeroId)) ||
                 state.Armies.Any(army => army.FactionId == state.PlayerFactionId &&
                                          army.Members.Any(member => member.HeroId == character.HeroId))));
            int targetRosterSize = 14 + System.Math.Max(0, castleCount - 1) * 8;
            if (employedCount >= targetRosterSize)
            {
                return false;
            }

            int reserve = prisoner.RansomResourceType == WIResourceType.ManaCrystal
                ? policy == WIAutoPlayerPolicy.Administration ? 300 :
                  policy == WIAutoPlayerPolicy.Balanced ? 150 : 75
                : policy == WIAutoPlayerPolicy.Administration ? 1000 :
                  policy == WIAutoPlayerPolicy.Balanced ? 500 : 250;
            return available - prisoner.RansomAmount >= reserve;
        }

        // 태생 영웅 또는 승격한 일반 인물을 포로 구조 우선순위의 영웅으로 판정합니다.
        private static bool IsEffectiveHero(WICharacterRuntimeState character)
        {
            return character != null &&
                   (character.BaseGrade == WICharacterGrade.Hero || character.PromotedToHero);
        }
    }
}
