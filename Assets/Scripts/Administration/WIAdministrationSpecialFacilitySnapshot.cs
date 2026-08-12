using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationSpecialFacilitySnapshot
    {
        public string CastleName;
        public int OccupiedSlots;
        public int MaximumSlots;
        public List<WIAdministrationSpecialFacilityOptionSnapshot> Options = new();
    }

    public sealed class WIAdministrationSpecialFacilityOptionSnapshot
    {
        public string FacilityId;
        public string DisplayName;
        public string Description;
        public Sprite Icon;
    }
}
