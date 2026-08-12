using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationSchemeSnapshot
    {
        public string Title;
        public string Summary;
        public List<WIAdministrationSchemeCardSnapshot> Cards = new();
    }

    public sealed class WIAdministrationSchemeCardSnapshot
    {
        public string Action;
        public string Id;
        public string Title;
        public string Description;
        public Sprite Portrait;
        public bool Interactable = true;
    }
}
