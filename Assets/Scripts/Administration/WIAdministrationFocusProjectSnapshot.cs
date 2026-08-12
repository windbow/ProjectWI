using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationFocusProjectSnapshot
    {
        public string CastleName;
        public List<WIAdministrationProjectOptionSnapshot> Projects = new List<WIAdministrationProjectOptionSnapshot>();
    }

    public sealed class WIAdministrationProjectOptionSnapshot
    {
        public WICastleProjectType ProjectType;
        public string DisplayName;
        public int BasicCost;
        public int IntensiveCost;
    }

    public sealed class WIAdministrationProjectManagerSnapshot
    {
        public string HeroId;
        public string DisplayName;
        public Sprite Portrait;
        public int ExpectedGain;
        public string TraitText;
    }
}
