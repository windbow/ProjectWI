namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 충성 상태를 첩보 UGUI용 한국어로 변환합니다.
        private string GetLoyaltyDisplayName(WILoyaltyState loyalty)
        {
            switch (loyalty)
            {
                case WILoyaltyState.Unsettled:
                    return "동요";
                case WILoyaltyState.Danger:
                    return "위험";
                default:
                    return "안정";
            }
        }

        // 외교 상태를 UGUI용 한국어 이름으로 변환합니다.
        private string GetDiplomaticStatusDisplayName(WIDiplomaticStatus status)
        {
            switch (status)
            {
                case WIDiplomaticStatus.War:
                    return "전쟁";
                case WIDiplomaticStatus.Friendly:
                    return "우호";
                case WIDiplomaticStatus.NonAggression:
                    return "불가침";
                case WIDiplomaticStatus.Alliance:
                    return "동맹";
                default:
                    return "중립";
            }
        }

        // 현재 관계와 다음 행동의 의미를 UGUI에 표시할 짧은 문장으로 설명합니다.
        private string GetDiplomaticDescription(WIDiplomaticRelationState relation)
        {
            switch (relation.Status)
            {
                case WIDiplomaticStatus.War:
                    return "교전 중입니다. 적 성으로 원정할 수 있으며 휴전 교섭을 시도할 수 있습니다.";
                case WIDiplomaticStatus.Friendly:
                    return "사절 교류가 안정되었습니다. 불가침 협정을 제안할 수 있습니다.";
                case WIDiplomaticStatus.NonAggression:
                    return "서로 공격하지 않기로 합의했습니다. 동맹 체결을 제안할 수 있습니다.";
                case WIDiplomaticStatus.Alliance:
                    return $"동맹 관계입니다. 금화 원조 재요청 대기 {relation.AidCooldownMonths}개월.";
                default:
                    return "공식 협정이 없는 중립 관계입니다. 친선 사절을 파견할 수 있습니다.";
            }
        }

        // AI 성향을 진영 정세 UGUI용 한국어 이름으로 변환합니다.
        private string GetAIStrategyDisplayName(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Development:
                    return "개발";
                case WIAIStrategy.Defense:
                    return "수비";
                case WIAIStrategy.Aggressive:
                    return "공세";
                case WIAIStrategy.Scheme:
                    return "모략";
                default:
                    return "부국";
            }
        }
    }
}
