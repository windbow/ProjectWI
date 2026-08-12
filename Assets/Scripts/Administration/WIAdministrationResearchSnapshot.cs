using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationResearchSnapshot
    {
        public string Title;
        public string Summary;
        public List<WIAdministrationResearchCardSnapshot> Cards = new();
    }

    public sealed class WIAdministrationResearchCardSnapshot
    {
        public string Action;
        public string Id;
        public string Title;
        public string Description;
        public Sprite Portrait;
        public bool Interactable = true;
    }
}
