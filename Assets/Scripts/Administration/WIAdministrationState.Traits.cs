using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationState
    {
        // 저장본의 특성을 편집 가능한 마스터 데이터와 동기화하고 자격을 잃은 영지관을 해제합니다.
        public void SynchronizeCharacterTraits(WIAdministrationDatabaseSO database)
        {
            foreach (WICharacterRuntimeState character in Characters)
            {
                WIHeroDefinition definition = database.GetHero(character.HeroId);
                if (definition == null)
                {
                    character.Traits = new List<WITraitType>();
                    continue;
                }
                if (character.Traits == null || character.Traits.SequenceEqual(definition.Traits) == false)
                {
                    character.Traits = definition.Traits.ToList();
                }
            }
            foreach (WICastleRuntimeState castle in Castles)
            {
                if (string.IsNullOrEmpty(castle.GovernorHeroId) == false &&
                    WIAdministrationTurnSystem.IsAdministrationCapable(this, castle.GovernorHeroId) == false)
                {
                    castle.GovernorHeroId = string.Empty;
                    castle.DelegatedToGovernor = false;
                }
            }
        }
    }
}
