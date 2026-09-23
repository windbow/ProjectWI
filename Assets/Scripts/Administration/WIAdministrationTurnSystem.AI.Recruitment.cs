using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 프리 시나리오의 AI 군주가 비용과 영토를 확인해 방랑 인물을 실제 성에 고용합니다.
        private static void PlanFreeScenarioAIRecruitment(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            WICampaignVariantDefinition variant = database.GetCampaignVariant(state.CampaignVariant);
            if (variant?.NonPlayerRecruitmentEnabled != true || state.Turn < 12 || state.Turn % 12 != 0)
            {
                return;
            }

            const int recruitmentCost = 100;
            foreach (WIFactionDefinition faction in database.Factions.Where(item => item.PlayerFaction == false))
            {
                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                List<WICastleRuntimeState> castles = state.Castles
                    .Where(castle => castle.FactionId == faction.Id)
                    .OrderBy(castle => castle.HeroIds.Count)
                    .ThenBy(castle => castle.CastleId)
                    .ToList();
                if (factionState == null || factionState.Eliminated ||
                    factionState.Gold < recruitmentCost || castles.Count == 0)
                {
                    continue;
                }

                HashSet<string> assigned = new HashSet<string>(state.Castles.SelectMany(castle => castle.HeroIds));
                foreach (string heroId in state.Armies.SelectMany(army => army.Members.Select(member => member.HeroId)))
                {
                    assigned.Add(heroId);
                }
                WICharacterRuntimeState candidate = state.Characters
                    .Where(character => IsHeroRecruitmentCandidate(character) == true && character.IsDead == false &&
                                        character.Captured == false &&
                                        string.IsNullOrEmpty(character.JoinedEnemyFactionId) &&
                                        assigned.Contains(character.HeroId) == false)
                    .OrderByDescending(character => database.GetHero(character.HeroId)?.Charisma ?? 0)
                    .ThenBy(character => character.HeroId)
                    .FirstOrDefault();
                if (candidate == null)
                {
                    continue;
                }

                WICastleRuntimeState destination = castles[0];
                factionState.Gold -= recruitmentCost;
                candidate.Recruited = true;
                candidate.Discovered = true;
                candidate.RecruitmentCastleId = destination.CastleId;
                destination.HeroIds.Add(candidate.HeroId);
                summary.News.Add($"AI 영입 · {faction.DisplayName.Get(database.UseEnglish)} · " +
                                 $"{database.GetHero(candidate.HeroId)?.DisplayName.Get(database.UseEnglish)} · " +
                                 $"{database.GetCastle(destination.CastleId)?.DisplayName.Get(database.UseEnglish)} 배치");
            }
        }


    }
}
