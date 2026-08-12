using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationDiplomacySnapshot
    {
        public string Title;
        public string Summary;
        public List<WIAdministrationDiplomacyCardSnapshot> Cards = new();
    }

    public sealed class WIAdministrationDiplomacyCardSnapshot
    {
        public string Action;
        public string Id;
        public string Title;
        public string Description;
        public bool Interactable = true;
    }
}
