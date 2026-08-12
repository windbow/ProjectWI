using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationCharacterActivitySnapshot
    {
        public string CastleName;
        public List<WIAdministrationCharacterActivityCandidateSnapshot> Candidates = new();
    }

    public sealed class WIAdministrationCharacterActivityCandidateSnapshot
    {
        public string HeroId;
        public string DisplayName;
        public string Summary;
        public Sprite Portrait;
        public bool Interactable = true;
    }
}
