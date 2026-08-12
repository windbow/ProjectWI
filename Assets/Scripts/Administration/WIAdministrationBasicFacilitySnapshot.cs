using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationBasicFacilitySnapshot
    {
        public string CastleName;
        public List<WIAdministrationTavernQuestSnapshot> Quests = new();
        public List<WIAdministrationQuestHeroSnapshot> Heroes = new();
    }

    public sealed class WIAdministrationTavernQuestSnapshot
    {
        public string QuestId;
        public string DisplayName;
        public string Summary;
        public string Description;
        public bool Available;
    }

    public sealed class WIAdministrationQuestHeroSnapshot
    {
        public string HeroId;
        public string DisplayName;
        public string Summary;
        public Sprite Portrait;
    }
}
