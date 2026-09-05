using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 큰 전력 차이로 무너진 전투단원 한 명의 사망, 포로, 전향 또는 후퇴 결과를 결정합니다.
        private static void ResolveDefeatedCharacterFate(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIArmyState army,
            int defeatMargin,
            bool orderlyRetreat,
            string captorFactionId,
            WITurnSummary summary)
        {
            if (orderlyRetreat || defeatMargin < database.CapturePowerMargin || army.Members.Count == 0)
            {
                return;
            }
            List<WIArmyMemberState> commonMembers = army.Members
                .Where(member => database.GetHero(member.HeroId)?.Grade == WICharacterGrade.Common).ToList();
            List<WIArmyMemberState> heroMembers = army.Members
                .Where(member => database.GetHero(member.HeroId)?.Grade == WICharacterGrade.Hero).ToList();
            bool exposeHero = heroMembers.Count > 0 && (commonMembers.Count == 0 ||
                GetStableBattleFateRoll(state, army, "hero_exposure") < 20);
            List<WIArmyMemberState> exposedMembers = exposeHero ? heroMembers : commonMembers;
            if (exposedMembers.Count == 0)
            {
                exposedMembers = army.Members;
            }
            WIArmyMemberState affectedMember = exposedMembers
                .OrderByDescending(member => state.GetCharacter(member.HeroId)?.Fatigue ?? 0)
                .ThenBy(member => member.HeroId)
                .First();
            WICharacterRuntimeState character = state.GetCharacter(affectedMember.HeroId);
            WIHeroDefinition hero = database.GetHero(affectedMember.HeroId);
            if (character == null || hero == null)
            {
                return;
            }

            WICampaignDifficultyDefinition difficulty = database.GetDifficulty(state.Difficulty);
            int deathWeight = difficulty?.BattleDeathChance ?? 15;
            int captureWeight = difficulty?.BattleCaptureChance ?? 35;
            int defectionWeight = difficulty?.BattleDefectionChance ?? 10;
            if (hero.BattleTraits.Contains(WIBattleTraitType.Survivor))
            {
                deathWeight /= 4;
            }
            if (hero.BattleTraits.Contains(WIBattleTraitType.Elusive))
            {
                captureWeight /= 4;
            }
            if (hero.BattleTraits.Contains(WIBattleTraitType.Unyielding))
            {
                defectionWeight = 0;
            }

            int roll = GetStableBattleFateRoll(state, army, character.HeroId);
            int deathLimit = deathWeight;
            int captureLimit = deathLimit + captureWeight;
            int defectionLimit = captureLimit + defectionWeight;
            string fate;
            if (roll < deathLimit)
            {
                MarkCharacterDead(database, state, character);
                fate = "사망";
            }
            else if (roll < captureLimit)
            {
                character.Captured = true;
                character.CaptorFactionId = captorFactionId;
                character.CapturedFromFactionId = army.FactionId;
                character.CapturedMonthsRemaining = database.CaptureDurationMonths;
                character.RansomRequested = true;
                character.RansomResourceType = roll % 3 == 0 ? WIResourceType.ManaCrystal : WIResourceType.Gold;
                character.RansomAmount = character.RansomResourceType == WIResourceType.Gold
                    ? database.PrisonerRansomGold : Mathf.Max(1, database.PrisonerRansomGold / 3);
                fate = $"포로 · {GetResourceDisplayName(character.RansomResourceType)} {character.RansomAmount} 교환 요청";
            }
            else if (roll < defectionLimit)
            {
                character.JoinedEnemyFactionId = captorFactionId;
                WICastleRuntimeState joinedCastle = state.Castles.FirstOrDefault(item => item.FactionId == captorFactionId);
                if (joinedCastle != null && joinedCastle.HeroIds.Contains(character.HeroId) == false)
                {
                    joinedCastle.HeroIds.Add(character.HeroId);
                }
                fate = "적 진영에 합류";
            }
            else
            {
                fate = "후퇴";
                summary?.News.Add($"전투 이탈 · {hero.DisplayName.Get(database.UseEnglish)} · {fate}");
                return;
            }

            character.Activity = WICharacterActivityType.None;
            army.Members.Remove(affectedMember);
            if (army.Members.Count == 0)
            {
                state.Armies.Remove(army);
            }
            else if (army.Members.Any(member => member.Role == WIUnitRole.Commander) == false)
            {
                WIArmyMemberState successor = army.Members.OrderByDescending(member =>
                    database.GetHero(member.HeroId)?.Leadership ?? 0).First();
                successor.Role = WIUnitRole.Commander;
            }
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                if (character.JoinedEnemyFactionId != castle.FactionId)
                {
                    castle.HeroIds.Remove(affectedMember.HeroId);
                }
            }
            summary?.News.Add($"전투 이탈 · {hero.DisplayName.Get(database.UseEnglish)} · {fate}");
        }

        // 별도 수비 전투단이 없는 성의 주둔 인물도 패전 운명 판정 대상에 포함합니다.
        private static void ResolveDefeatedGarrisonCharacterFate(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICastleRuntimeState castle,
            int defeatMargin,
            bool orderlyRetreat,
            string captorFactionId,
            WITurnSummary summary)
        {
            if (castle == null)
            {
                return;
            }

            WIArmyState garrison = new WIArmyState
            {
                ArmyId = $"garrison_{castle.CastleId}",
                FactionId = castle.FactionId,
                CurrentCastleId = castle.CastleId
            };
            foreach (string heroId in castle.HeroIds.OrderBy(id => id))
            {
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (character == null || character.IsDead || character.Captured ||
                    string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false)
                {
                    continue;
                }
                garrison.Members.Add(new WIArmyMemberState
                {
                    HeroId = heroId,
                    Role = WIUnitRole.Melee
                });
            }

            ResolveDefeatedCharacterFate(
                database, state, garrison, defeatMargin, orderlyRetreat, captorFactionId, summary);
        }

        // 사망한 인물을 등급에 따라 영구 퇴장 또는 일반 인물 재야 복귀 대기로 전환합니다.
        public static void MarkCharacterDead(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WICharacterRuntimeState character)
        {
            if (database == null || state == null || character == null)
            {
                return;
            }

            character.IsDead = true;
            character.DeathCount += 1;
            character.Recruited = false;
            character.Discovered = false;
            character.Captured = false;
            character.CaptorFactionId = string.Empty;
            character.CapturedFromFactionId = string.Empty;
            character.CapturedMonthsRemaining = 0;
            character.RansomRequested = false;
            character.JoinedEnemyFactionId = string.Empty;
            character.Activity = WICharacterActivityType.None;
            character.ActivityTargetHeroId = string.Empty;
            character.RecruitmentProgress = 0;
            character.RecruitmentCastleId = string.Empty;
            bool permanentDeath = character.BaseGrade == WICharacterGrade.Hero;
            if (permanentDeath == false)
            {
                character.PromotedToHero = false;
                character.PromotionAchievement = false;
                character.TitleId = string.Empty;
                state.PendingHeroPromotionIds.Remove(character.HeroId);
            }
            character.CommonReturnMonthsRemaining = permanentDeath ? 0 : 6;
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.HeroIds.Remove(character.HeroId);
                if (castle.GovernorHeroId == character.HeroId)
                {
                    castle.GovernorHeroId = string.Empty;
                    castle.DelegatedToGovernor = false;
                }
            }
            foreach (WIArmyState existingArmy in state.Armies)
            {
                existingArmy.Members.RemoveAll(member => member.HeroId == character.HeroId);
            }
            state.PendingRecruitmentEvents.RemoveAll(item => item.CandidateHeroId == character.HeroId ||
                                                             item.RecruiterHeroId == character.HeroId);
        }

        // 사망한 일반 인물을 고용 상태 중복 없이 살아남은 성의 재야 인재로 되돌립니다.
        private static void ResolveCommonCharacterReturns(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            List<WICastleRuntimeState> availableCastles = state.Castles
                .Where(castle => state.GetFactionState(castle.FactionId)?.Eliminated == false)
                .OrderBy(castle => castle.CastleId)
                .ToList();
            if (availableCastles.Count == 0)
            {
                return;
            }

            foreach (WICharacterRuntimeState character in state.Characters.Where(item =>
                         item.IsDead && item.BaseGrade == WICharacterGrade.Common))
            {
                character.PromotedToHero = false;
                character.CommonReturnMonthsRemaining = Mathf.Max(0, character.CommonReturnMonthsRemaining - 1);
                if (character.CommonReturnMonthsRemaining > 0 || IsCharacterEmployedAnywhere(state, character.HeroId))
                {
                    continue;
                }

                int castleIndex = GetStableCommonReturnCastleIndex(state, character.HeroId, availableCastles.Count);
                WICastleRuntimeState returnCastle = availableCastles[castleIndex];
                character.IsDead = false;
                character.Discovered = false;
                character.Recruited = false;
                character.RecruitmentProgress = 0;
                character.RecruitmentCastleId = returnCastle.CastleId;
                character.CommonReturnCount += 1;
                WIHeroDefinition definition = database.GetHero(character.HeroId);
                summary?.News.Add($"재야 인재 소문 · {definition?.DisplayName.Get(database.UseEnglish) ?? character.HeroId} · " +
                                  $"{database.GetCastle(returnCastle.CastleId)?.DisplayName.Get(database.UseEnglish)} 인근");
            }
        }

        // 인물이 성, 전투단, 포로, 전향 또는 영입 상태에 남아 있는지 확인합니다.
        private static bool IsCharacterEmployedAnywhere(WIAdministrationState state, string heroId)
        {
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            return character == null || character.Recruited || character.Captured ||
                   string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false ||
                   state.Castles.Any(castle => castle.HeroIds.Contains(heroId)) ||
                   state.Armies.Any(army => army.Members.Any(member => member.HeroId == heroId));
        }

        // 턴과 인물 ID를 이용해 재현 가능한 재야 복귀 성 인덱스를 반환합니다.
        private static int GetStableCommonReturnCastleIndex(WIAdministrationState state, string heroId, int count)
        {
            unchecked
            {
                int hash = state.Turn * 397;
                foreach (char letter in heroId)
                {
                    hash = hash * 31 + letter;
                }
                return Mathf.Abs(hash % count);
            }
        }

        // 저장과 전투 결과 출처에 관계없이 같은 운명 판정을 만드는 0~99 값을 반환합니다.
        private static int GetStableBattleFateRoll(WIAdministrationState state, WIArmyState army, string heroId)
        {
            unchecked
            {
                int hash = 17 + state.SimulationSeed * 7919;
                string key = $"{state.Turn}|{army.ArmyId}|{heroId}";
                foreach (char character in key)
                {
                    hash = hash * 31 + character;
                }
                return Mathf.Abs(hash % 100);
            }
        }

        // 포로 교환 요청에 표시할 자원 이름을 반환합니다.
        private static string GetResourceDisplayName(WIResourceType resourceType)
        {
            return resourceType == WIResourceType.ManaCrystal ? "마나" :
                resourceType == WIResourceType.Influence ? "영향력" : "금화";
        }

        // 포로 억류 기간을 줄이고 만료된 인물을 원래 진영의 성으로 귀환시킵니다.
        private static void ResolveCapturedCharacters(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterRuntimeState character in state.Characters.Where(item => item.Captured).ToList())
            {
                character.CapturedMonthsRemaining = Mathf.Max(0, character.CapturedMonthsRemaining - 1);
                if (character.CapturedMonthsRemaining > 0)
                {
                    continue;
                }
                if (ReleaseCapturedCharacter(state, character) == false)
                {
                    continue;
                }
                WICastleRuntimeState returnCastle = state.Castles.FirstOrDefault(item => item.HeroIds.Contains(character.HeroId));
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                summary.News.Add($"포로 귀환 · {hero?.DisplayName.Get(database.UseEnglish) ?? character.HeroId} · " +
                    $"{database.GetCastle(returnCastle.CastleId).DisplayName.Get(database.UseEnglish)} 도착");
            }
        }

        // 포로 상태를 해제하고 원소속 진영이 보유한 첫 성으로 인물을 배치합니다.
        private static bool ReleaseCapturedCharacter(WIAdministrationState state, WICharacterRuntimeState character)
        {
            WICastleRuntimeState returnCastle = state.Castles.FirstOrDefault(item => item.FactionId == character.CapturedFromFactionId);
            if (returnCastle == null)
            {
                return false;
            }
            character.Captured = false;
            character.CaptorFactionId = string.Empty;
            character.CapturedMonthsRemaining = 0;
            character.RansomRequested = false;
            character.RansomAmount = 0;
            if (returnCastle.HeroIds.Contains(character.HeroId) == false)
            {
                returnCastle.HeroIds.Add(character.HeroId);
            }
            return true;
        }

    }
}
