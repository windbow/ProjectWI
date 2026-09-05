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
        public string ClassName;
        public WICharacterGrade Grade;
        public Sprite Portrait;
        public bool Interactable = true;
    }
}
