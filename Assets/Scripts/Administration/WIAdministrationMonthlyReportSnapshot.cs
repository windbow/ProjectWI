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
        public string Body;
        public List<WIAdministrationReportActionSnapshot> Actions = new();
    }

    public sealed class WIAdministrationReportActionSnapshot
    {
        public WIAdministrationReportActionType Type;
        public string Id;
        public string Caption;
        public bool Danger;
    }
}
