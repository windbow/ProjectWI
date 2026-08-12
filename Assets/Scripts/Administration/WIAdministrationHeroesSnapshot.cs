using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationHeroesSnapshot
    {
        public string Title;
        public string Summary;
        public List<WIAdministrationHeroCardSnapshot> Cards = new();
    }

    public sealed class WIAdministrationHeroCardSnapshot
    {
        public string Action;
        public string Id;
        public string Title;
        public string Description;
        public Sprite Portrait;
        public bool Interactable = true;
    }
}
