using UnityEngine;
using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationWorldSnapshot
    {
        public bool CampaignStarted;
        public bool WorldVisible;
        public string FactionName;
        public string Date;
        public string TurnDescription;
        public string Gold;
        public string Mana;
        public string Influence;
        public string CastleName;
        public string CastleOwner;
        public string CastleStats;
        public string CastleHeroes;
        public Sprite MapImage;
        public Sprite CastleImage;
        public string ObjectiveTitle;
        public string ObjectiveProgress;
        public float ObjectiveProgressNormalized;
        public string BattleAlert;
        public bool HasBattleAlert;
        public string MonthlyNews;
        public bool CanEndTurn;
        public string EndTurnText;
        public List<WIAdministrationMapNodeSnapshot> MapNodes = new List<WIAdministrationMapNodeSnapshot>();
    }

    public sealed class WIAdministrationMapNodeSnapshot
    {
        public string CastleId;
        public string DisplayName;
        public string FactionId;
        public bool Selected;
        public string Tooltip;
    }
}
