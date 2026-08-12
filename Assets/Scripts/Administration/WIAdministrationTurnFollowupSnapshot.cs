using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public enum WIAdministrationTurnFollowupMode
    {
        Processing,
        CampaignResult,
        Tutorial,
        Message
    }

    public sealed class WIAdministrationTurnFollowupSnapshot
    {
        public WIAdministrationTurnFollowupMode Mode;
        public string Title;
        public string Description;
        public List<string> Choices = new();
    }
}
