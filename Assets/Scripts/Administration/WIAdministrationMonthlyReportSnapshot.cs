using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public enum WIAdministrationReportActionType
    {
        ProjectEvent, RelationshipEvent, RegionalEvent, OccupationEvent,
        RecruitmentEvent, LegacyChoice, Battle
    }

    public sealed class WIAdministrationMonthlyReportSnapshot
    {
        public string Title;
        public string TerritorySummary;
        public string CharacterSummary;
        public string ArmySummary;
        public string StabilitySummary;
        public string ResearchSummary;
        public string GoldSummary;
        public string ManaSummary;
        public string InfluenceSummary;
        public List<string> Operations = new();
        public List<string> News = new();
        public List<WIAdministrationReportActionSnapshot> Actions = new();
    }

    public sealed class WIAdministrationReportActionSnapshot
    {
        public WIAdministrationReportActionType Type;
        public string Id;
        public string Caption;
        public string Category;
        public string Detail;
        public bool Danger;
    }
}
