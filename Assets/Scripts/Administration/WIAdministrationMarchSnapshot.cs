using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMarchSnapshot
    {
        public string CastleName;
        public List<WIAdministrationMarchOptionSnapshot> Options = new();
    }

    public sealed class WIAdministrationMarchOptionSnapshot
    {
        public string Id;
        public string DisplayName;
        public string Summary;
        public Sprite Image;
        public bool Interactable = true;
    }
}
