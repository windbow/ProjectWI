using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 영입 사건 선택지의 잠금 조건을 UGUI에 표시할 짧은 문장으로 반환합니다.
        private string GetRecruitmentChoiceCondition(WIRecruitmentEventChoiceDefinition choice)
        {
            List<string> conditions = new List<string>();
            if (choice.RequiredReputation > 0)
            {
                conditions.Add($"명성 {choice.RequiredReputation}");
            }
            if (choice.RequiredMerit > 0)
            {
                conditions.Add($"공훈 {choice.RequiredMerit}");
            }
            if (choice.RequiredFactionCastleCount > 0)
            {
                conditions.Add($"영토 {choice.RequiredFactionCastleCount}성");
            }
            if (choice.RequiresNegotiator)
            {
                conditions.Add("교섭가 특기");
            }
            return conditions.Count == 0 ? "조건 없음" : string.Join(" · ", conditions);
        }

        // 진영 방침의 UGUI 표시명을 반환합니다.
        private string GetFactionPolicyDisplayName(WIFactionPolicy policy)
        {
            string[] names = { "부국", "개발", "안정", "수비", "원정", "인재" };
            return names[(int)policy];
        }

        // 진영 방침의 핵심 효과 설명을 반환합니다.
        private string GetFactionPolicyDescription(WIFactionPolicy policy)
        {
            string[] descriptions =
            {
                "번영 사업 강화", "기술 사업 강화", "질서 사업 강화",
                "방어·회복 강화", "훈련과 원정 준비 강화", "탐색·영입 강화"
            };
            return descriptions[(int)policy];
        }

        // 영지관 운영 방침의 UGUI 표시명을 반환합니다.
        private string GetGovernorPolicyDisplayName(WIGovernorPolicy policy)
        {
            string[] names = { "균형", "번영", "연구", "전선", "인재" };
            return names[(int)policy];
        }
    }
}
