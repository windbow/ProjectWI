using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMilitarySnapshot
    {
        public string Title;
        public string Summary;
        public List<WIAdministrationMilitaryItemSnapshot> Items = new();
    }

    public sealed class WIAdministrationMilitaryItemSnapshot
    {
        public string Kind;
        public string Id;
        public string Title;
        public string Description;
        public int Value;
        public bool Interactable = true;
    }
}
