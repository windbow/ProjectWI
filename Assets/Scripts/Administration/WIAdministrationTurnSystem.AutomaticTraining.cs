using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 성에 머무는 인물은 개별 지시 없이 훈련하고 과로·부상 시 회복하며 원정을 우선합니다.
        private static void ResolveAutomaticCharacterTraining(WIAdministrationDatabaseSO database,
            WIAdministrationState state, WITurnSummary summary)
        {
            if (database.Automation.AutomaticTrainingEnabled == false)
            {
                return;
            }
            var unavailable = new HashSet<string>(state.Armies.Where(army => army.IsMoving == true ||
                army.AwaitingBattle == true || army.ReorganizationMonths > 0 || army.JointTrainingScheduled == true)
                .SelectMany(army => army.Members.Select(member => member.HeroId)));
            var stationed = state.Castles.SelectMany(castle => castle.HeroIds.Select(id => new { Id = id, Castle = castle }))
                .GroupBy(item => item.Id).ToDictionary(group => group.Key, group => group.First().Castle);
            var playerHeroes = new HashSet<string>(state.Castles.Where(castle => castle.FactionId == state.PlayerFactionId)
                .SelectMany(castle => castle.HeroIds));
            int trained = 0;
            int recovered = 0;
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || character.IsDead == true || character.Captured == true ||
                    character.Activity != WICharacterActivityType.None || stationed.ContainsKey(character.HeroId) == false ||
                    unavailable.Contains(character.HeroId) == true ||
                    state.IsCharacterBusy(character.HeroId, ignoreArmy: true, ignoreAdministration: true) == true)
                {
                    continue;
                }
                if (character.Fatigue >= database.Automation.RestStartFatigue || character.InjuryMonths > 0)
                {
                    character.AutomaticRecovery = true;
                }
                if (character.Fatigue <= database.Automation.RestEndFatigue && character.InjuryMonths == 0)
                {
                    character.AutomaticRecovery = false;
                }
                if (character.AutomaticRecovery == true)
                {
                    character.Fatigue = Mathf.Max(0, character.Fatigue - database.CharacterRestFatigueRecovery);
                    character.InjuryMonths = Mathf.Max(0, character.InjuryMonths - 1);
                    if (playerHeroes.Contains(character.HeroId) == true)
                    {
                        recovered += 1;
                    }
                    continue;
                }
                int facilityBonus = HasSpecialFacility(stationed[character.HeroId], "knightly_order") == true
                    ? FacilityTrainingExperienceBonus : 0;
                int gain = Mathf.Min(database.Automation.AutomaticTrainingExperience + facilityBonus,
                    Mathf.Max(0, database.Automation.TrainingTargetExperience - character.Experience));
                if (gain <= 0)
                {
                    continue;
                }
                character.Experience += gain;
                character.Fatigue = Mathf.Clamp(character.Fatigue + database.Automation.AutomaticTrainingFatigue, 0, 100);
                if (playerHeroes.Contains(character.HeroId) == true)
                {
                    trained += 1;
                }
            }
            if (trained > 0 || recovered > 0)
            {
                summary.News.Add(string.Format(database.GetText("REPORT_AUTOMATIC_TRAINING"), trained, recovered));
            }
        }
    }
}
