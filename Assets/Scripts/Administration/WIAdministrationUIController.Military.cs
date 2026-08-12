using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 원정 시작이 거부된 경우 현재 상태에서 가장 직접적인 해결 방법을 안내합니다.
        private string GetArmyMarchFailureMessage(WIArmyState army, WICastleRuntimeState target)
        {
            if (army == null || target == null) return "원정 정보를 확인할 수 없습니다.";
            if (army.IsMoving) return "이미 이동 중인 전투단입니다.";
            if (army.AwaitingBattle) return "현재 전투 결과를 기다리는 전투단입니다.";
            if (army.ReorganizationMonths > 0) return $"재편성 완료까지 {army.ReorganizationMonths}개월 남았습니다.";
            if (target.FactionId != army.FactionId)
            {
                if (WIAdministrationTurnSystem.AreFactionsAtWar(state, army.FactionId, target.FactionId) == false)
                    return "교전 중인 진영의 성에만 원정할 수 있습니다. 먼저 외교 관계를 확인하십시오.";
                WIFactionRuntimeState faction = state.GetFactionState(army.FactionId);
                if (faction == null || faction.Influence < 20)
                    return "원정에 필요한 영향력 20이 부족합니다.";
            }
            return "현재 경로로 원정할 수 없습니다. 인접 성과 전투단 상태를 확인하십시오.";
        }

        // 전투단 역할의 한국어 표시명을 반환합니다.
        private static string GetUnitRoleDisplayName(WIUnitRole role)
        {
            string[] names = { "대장", "전위", "근접", "원거리", "마법", "지원" };
            return names[(int)role];
        }
    }
}
