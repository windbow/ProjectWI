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
