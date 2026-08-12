using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationEventChoiceSnapshot
    {
        public string Title;
        public string Description;
        public List<WIAdministrationEventChoiceCardSnapshot> Choices = new();
    }

    public sealed class WIAdministrationEventChoiceCardSnapshot
    {
        public string Label;
        public bool Interactable = true;
    }
}
