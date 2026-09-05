using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 영토가 사라진 진영을 한 번만 멸망 처리하고 잔존 행동·전투단·인물·외교 약속을 정리합니다.
        public static void ResolveFactionEliminations(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WIFactionRuntimeState faction in state.Factions.Where(item => item.Eliminated == false).ToList())
            {
                if (state.Castles.Any(castle => castle.FactionId == faction.FactionId))
                {
                    continue;
                }
                faction.Eliminated = true;
                faction.EliminatedTurn = state.Turn;
                faction.ActiveResearchId = string.Empty;
                faction.ResearcherHeroId = string.Empty;
                faction.ResearchRemainingMonths = 0;

                HashSet<string> removedCharacterIds = new HashSet<string>(state.Armies
                    .Where(army => army.FactionId == faction.FactionId)
                    .SelectMany(army => army.Members).Select(member => member.HeroId));
                foreach (WICharacterRuntimeState prisoner in state.Characters.Where(character =>
                             character.Captured && character.CapturedFromFactionId == faction.FactionId))
                {
                    removedCharacterIds.Add(prisoner.HeroId);
                }
                foreach (WICharacterRuntimeState releasedPrisoner in state.Characters.Where(character =>
                             character.Captured && character.CaptorFactionId == faction.FactionId).ToList())
                {
                    ReleaseCapturedCharacter(state, releasedPrisoner);
                }
                state.Armies.RemoveAll(army => army.FactionId == faction.FactionId);
                state.BattleSessions.RemoveAll(session => session.AttackerFactionId == faction.FactionId ||
                                                         session.DefenderFactionId == faction.FactionId);
                state.SchemeMissions.RemoveAll(mission => mission.InitiatorFactionId == faction.FactionId);
                foreach (WIDiplomaticRelationState relation in state.DiplomaticRelations.Where(relation =>
                             relation.FirstFactionId == faction.FactionId || relation.SecondFactionId == faction.FactionId))
                {
                    relation.JointAttackTargetCastleId = string.Empty;
                    relation.JointAttackMonthsRemaining = 0;
                    relation.AidCooldownMonths = 0;
                }
                foreach (string heroId in removedCharacterIds)
                {
                    foreach (WICastleRuntimeState castle in state.Castles)
                    {
                        castle.HeroIds.Remove(heroId);
                    }
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    if (character == null)
                    {
                        continue;
                    }
                    character.Recruited = false;
                    character.Captured = false;
                    character.CaptorFactionId = string.Empty;
                    character.CapturedFromFactionId = string.Empty;
                    character.CapturedMonthsRemaining = 0;
                    character.Activity = WICharacterActivityType.None;
                }

                WIFactionDefinition definition = database.GetFaction(faction.FactionId);
                WIFactionEliminationNarrativeDefinition narrative = database.GetFactionEliminationNarrative(faction.FactionId);
                string title = narrative?.Title.Get(database.UseEnglish) ?? "진영 멸망";
                string description = narrative?.Description.Get(database.UseEnglish) ?? "모든 영토를 상실했습니다.";
                summary?.News.Add($"진영 멸망 · {definition?.DisplayName.Get(database.UseEnglish) ?? faction.FactionId} · " +
                                  $"{title} · 제 {state.Turn}턴 · {description}");
            }
        }
    }
}
