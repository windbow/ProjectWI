using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
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
                if (castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.GoldIncome)
                {
                    summary.GoldGained += value;
                }
                if (castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.ManaIncome)
                {
                    summary.ManaGained += value;
                }
                if (castleDefinition.SpecialtyEffectType == WICastleSpecialtyEffectType.InfluenceIncome)
                {
                    summary.InfluenceGained += value;
                }
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
    }
}
