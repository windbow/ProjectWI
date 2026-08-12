using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationCouncilSnapshot
    {
        public string Summary;
        public List<WIAdministrationCouncilCardSnapshot> Cards = new();
    }

    public sealed class WIAdministrationCouncilCardSnapshot
    {
        public WIFactionPolicy Policy;
        public string Title;
        public string Description;
        public bool Selected;
    }
}
