using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationHeroAssignmentSnapshot
    {
        public string CastleName;
        public int OccupiedSlots;
        public int MaximumSlots;
        public List<WIAdministrationHeroAssignmentCandidateSnapshot> Candidates =
            new List<WIAdministrationHeroAssignmentCandidateSnapshot>();
    }

    public sealed class WIAdministrationHeroAssignmentCandidateSnapshot
    {
        public string HeroId;
        public string DisplayName;
        public string Summary;
        public Sprite Portrait;
    }
}
