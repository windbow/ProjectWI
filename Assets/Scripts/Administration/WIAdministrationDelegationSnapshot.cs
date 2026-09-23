using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationDelegationSnapshot
    {
        public string CastleName;
        public string GovernorHeroId;
        public WIGovernorPolicy Policy;
        public int MonthlyBudget;
        public int BasicBudget;
        public int IntensiveBudget;
        public bool Delegated;
        public string Preview;
        // 기본 운영 안내와 전환 버튼의 현지화된 문구입니다.
        public string OperationHelp;
        public string ToggleLabel;
        public List<WIAdministrationGovernorCandidateSnapshot> Candidates = new();
    }

    public sealed class WIAdministrationGovernorCandidateSnapshot
    {
        public string HeroId;
        public string DisplayName;
        public string Summary;
        public Sprite Portrait;
        public bool Interactable;
    }
}
