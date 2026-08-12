using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationFactionSnapshot
    {
        public string Summary;
        public List<WIAdministrationFactionCardSnapshot> Cards = new();
    }

    public sealed class WIAdministrationFactionCardSnapshot
    {
        public string Title;
        public string Description;
    }
}
