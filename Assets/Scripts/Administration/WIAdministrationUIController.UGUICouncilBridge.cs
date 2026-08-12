namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // QA에서 전역 의회 UGUI를 바로 표시합니다.
        public void OpenCouncilUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUICouncilRequested?.Invoke();
        }

        // 현재 방침과 선택 가능한 모든 월간 진영 방침을 카드 스냅샷으로 구성합니다.
        public bool TryGetUGUICouncilSnapshot(out WIAdministrationCouncilSnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null) return false;
            snapshot = new WIAdministrationCouncilSnapshot
            {
                Summary = $"현재 방침 · {GetFactionPolicyDisplayName(state.FactionPolicy)}\n방침은 즉시 적용되며 다음 월말 사업 성과 계산에 반영됩니다."
            };
            foreach (WIFactionPolicy policy in System.Enum.GetValues(typeof(WIFactionPolicy)))
            {
                snapshot.Cards.Add(new WIAdministrationCouncilCardSnapshot
                {
                    Policy = policy,
                    Title = GetFactionPolicyDisplayName(policy),
                    Description = GetFactionPolicyDescription(policy),
                    Selected = state.FactionPolicy == policy
                });
            }
            return true;
        }

        // 선택한 월간 진영 방침을 플레이어 진영 상태에 저장합니다.
        public bool SetUGUICouncilPolicy(WIFactionPolicy policy)
        {
            if (state == null || System.Enum.IsDefined(typeof(WIFactionPolicy), policy) == false) return false;
            state.FactionPolicy = policy;
            RefreshAll();
            return true;
        }
    }
}
