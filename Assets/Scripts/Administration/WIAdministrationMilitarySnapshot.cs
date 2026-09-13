using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMilitarySnapshot
    {
        public string Title;
        public string Summary;
        // 현재 전투단에 추가할 수 있는 인원 수입니다.
        public int AvailableMemberSlots;
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
