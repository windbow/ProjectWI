using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationTerritorySnapshot
    {
        public bool Visible;
        public bool Manageable;
        public bool CanChooseSpecialFacility;
        public string FactionName;
        public string Date;
        public string Gold;
        public string Mana;
        public string Influence;
        public string CastleTitle;
        public string CastleInfo;
        public Sprite CastleImage;
        public Sprite GovernorPortrait;
        public string GovernorName;
        public string Prosperity;
        public string Technology;
        public string Stability;
        public string Defense;
        public string Income;
        public string ProjectStatus;
        public List<WIAdministrationSlotSnapshot> HeroSlots = new List<WIAdministrationSlotSnapshot>();
        public List<WIAdministrationSlotSnapshot> FacilitySlots = new List<WIAdministrationSlotSnapshot>();
    }

    public sealed class WIAdministrationSlotSnapshot
    {
        public bool Visible;
        public bool Occupied;
        public string Caption;
        public Sprite Image;
    }

    public enum WIAdministrationTerritoryCommand
    {
        FocusProject,
        AssignHero,
        CharacterActivity,
        ChooseSpecialFacility,
        BasicFacility,
        Delegation,
        March,
        CastleRecord
    }
}
