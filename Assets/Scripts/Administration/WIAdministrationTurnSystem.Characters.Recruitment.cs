using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 탐색 활동으로 아직 알려지지 않은 인재 한 명을 발견합니다.
        private static void ResolveSearchActivity(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero,
            WICharacterRuntimeState character,
            WITurnSummary summary)
        {
            WICastleRuntimeState searchCastle = state.Castles.Find(castle =>
                castle.HeroIds.Contains(character.HeroId));
            WICharacterRuntimeState candidate = state.Characters.Find(item =>
                IsAvailableWanderingCandidate(item) && item.RecruitmentCastleId == searchCastle?.CastleId);
            if (candidate == null)
            {
                candidate = state.Characters.Find(item =>
                    IsAvailableWanderingCandidate(item) && string.IsNullOrEmpty(item.RecruitmentCastleId));
            }
            int guildReputationBonus = HasSpecialFacility(searchCastle, "adventurers_guild") ? 2 : 0;
            character.Reputation += 2 + guildReputationBonus;
            if (candidate == null)
            {
                summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)} · 새로운 인재 단서를 찾지 못함");
                return;
            }

            candidate.Discovered = true;
            WIHeroDefinition discoveredHero = database.GetHero(candidate.HeroId);
            summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)}의 탐색 · {discoveredHero.DisplayName.Get(database.UseEnglish)} 발견");
        }

        // 재야 탐색에 노출할 수 있는 생존·미고용 인물인지 확인합니다.
        private static bool IsAvailableWanderingCandidate(WICharacterRuntimeState character)
        {
            return IsHeroRecruitmentCandidate(character) == true && character.Discovered == false;
        }

        // 교류 활동으로 두 인물의 관계를 한 단계 개선합니다.
        private static void ResolveSocializeActivity(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero,
            WICharacterRuntimeState character,
            WITurnSummary summary)
        {
            WICharacterRuntimeState target = state.GetCharacter(character.ActivityTargetHeroId);
            if (target == null || target.Recruited == false)
            {
                return;
            }

            WIRelationshipState relationship = state.GetOrCreateRelationship(character.HeroId, target.HeroId);
            relationship.Level = relationship.Level == WIRelationshipLevel.Conflict
                ? WIRelationshipLevel.Normal
                : WIRelationshipLevel.Fondness;
            WIHeroDefinition targetHero = database.GetHero(target.HeroId);
            summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)}와 {targetHero.DisplayName.Get(database.UseEnglish)} · 관계 {relationship.Level}");
        }

        // 발견한 인재를 설득하고 누적 진척이 충족되면 진영에 영입합니다.
        private static void ResolveRecruitActivity(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIHeroDefinition hero,
            WICharacterRuntimeState character,
            WITurnSummary summary)
        {
            WICharacterRuntimeState target = state.GetCharacter(character.ActivityTargetHeroId);
            if (IsHeroRecruitmentCandidate(target) == false || target.Discovered == false)
            {
                return;
            }

            WIHeroDefinition targetHero = database.GetHero(target.HeroId);
            if (character.Reputation < targetHero.RequiredReputation)
            {
                summary.News.Add($"{targetHero.DisplayName.Get(database.UseEnglish)} 영입 보류 · 명성 {targetHero.RequiredReputation} 필요");
                return;
            }

            WICastleRuntimeState recruitmentCastle = state.Castles.Find(castle =>
                castle.HeroIds.Contains(character.HeroId));
            int guildProgressBonus = HasSpecialFacility(recruitmentCastle, "adventurers_guild") ? 10 : 0;
            int progress = database.RecruitmentBaseProgress + hero.Charisma / 4 + guildProgressBonus;
            target.RecruitmentProgress = Mathf.Clamp(target.RecruitmentProgress + progress, 0, 100);
            if (target.RecruitmentProgress < 100)
            {
                summary.News.Add($"{targetHero.DisplayName.Get(database.UseEnglish)} 영입 설득 · {target.RecruitmentProgress}%");
                return;
            }

            WIRecruitmentEventDefinition recruitmentEvent = database.GetRecruitmentEvent(targetHero.RecruitmentEventId);
            if (recruitmentEvent == null)
            {
                bool playerRecruitment = IsHeroInFaction(state, character.HeroId, state.PlayerFactionId);
                target.Recruited = true;
                PlaceRecruitedCharacter(state, character.HeroId, target.HeroId);
                if (playerRecruitment)
                {
                    state.PlayerRecruitmentSuccessCount += 1;
                }
                character.Merit += 10;
                character.Reputation += 5;
                summary.News.Add($"{targetHero.DisplayName.Get(database.UseEnglish)} 영입 성공");
                return;
            }

            if (state.PendingRecruitmentEvents.All(item => item.CandidateHeroId != target.HeroId))
            {
                state.PendingRecruitmentEvents.Add(new WIPendingRecruitmentEvent
                {
                    EventId = recruitmentEvent.Id,
                    RecruiterHeroId = character.HeroId,
                    CandidateHeroId = target.HeroId
                });
                summary.News.Add($"영입 요구 사건 · {recruitmentEvent.Title.Get(database.UseEnglish)} · {targetHero.DisplayName.Get(database.UseEnglish)}");
            }
        }

        // 영입 사건 선택지의 명성·공훈·영토·교섭가 조건 충족 여부를 반환합니다.
        public static bool CanChooseRecruitmentEventOption(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIPendingRecruitmentEvent pendingEvent,
            WIRecruitmentEventChoiceDefinition choice)
        {
            WICharacterRuntimeState recruiter = pendingEvent == null ? null : state.GetCharacter(pendingEvent.RecruiterHeroId);
            WIHeroDefinition recruiterDefinition = pendingEvent == null ? null : database.GetHero(pendingEvent.RecruiterHeroId);
            if (recruiter == null || choice == null || recruiter.Reputation < choice.RequiredReputation ||
                recruiter.Merit < choice.RequiredMerit)
            {
                return false;
            }
            int castleCount = state.Castles.Count(item => item.FactionId == state.PlayerFactionId);
            if (castleCount < choice.RequiredFactionCastleCount)
            {
                return false;
            }
            return choice.RequiresNegotiator == false ||
                   (recruiterDefinition != null && recruiterDefinition.Traits.Contains(WITraitType.Negotiator));
        }

        // 영입 요구 사건의 선택 결과를 적용해 후보를 영입합니다.
        public static bool ResolveRecruitmentEvent(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WIPendingRecruitmentEvent pendingEvent,
            int choiceIndex,
            WITurnSummary summary)
        {
            WIRecruitmentEventDefinition definition = pendingEvent == null ? null : database.GetRecruitmentEvent(pendingEvent.EventId);
            if (definition == null || choiceIndex < 0 || choiceIndex >= definition.Choices.Count ||
                state.PendingRecruitmentEvents.Contains(pendingEvent) == false)
            {
                return false;
            }
            WIRecruitmentEventChoiceDefinition choice = definition.Choices[choiceIndex];
            if (CanChooseRecruitmentEventOption(database, state, pendingEvent, choice) == false)
            {
                return false;
            }

            WICharacterRuntimeState recruiter = state.GetCharacter(pendingEvent.RecruiterHeroId);
            WICharacterRuntimeState candidate = state.GetCharacter(pendingEvent.CandidateHeroId);
            if (IsHeroRecruitmentCandidate(candidate) == false)
            {
                return false;
            }
            bool playerRecruitment = IsHeroInFaction(state, recruiter.HeroId, state.PlayerFactionId);
            candidate.Recruited = true;
            PlaceRecruitedCharacter(state, recruiter.HeroId, candidate.HeroId);
            if (playerRecruitment)
            {
                state.PlayerRecruitmentSuccessCount += 1;
            }
            recruiter.Merit += choice.RecruiterMeritGain;
            recruiter.Reputation += 5;
            recruiter.Fatigue = Mathf.Clamp(recruiter.Fatigue + choice.RecruiterFatigueGain, 0, 100);
            state.PendingRecruitmentEvents.Remove(pendingEvent);
            WIHeroDefinition candidateDefinition = database.GetHero(candidate.HeroId);
            summary?.News.Add($"영입 성공 · {candidateDefinition.DisplayName.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}");
            return true;
        }

        // 영입된 인물을 영입 담당자가 현재 머무는 성에 배치합니다.
        private static void PlaceRecruitedCharacter(
            WIAdministrationState state,
            string recruiterHeroId,
            string candidateHeroId)
        {
            WICastleRuntimeState castle = state.Castles.Find(item =>
                item.HeroIds.Contains(recruiterHeroId));
            if (castle != null && castle.HeroIds.Contains(candidateHeroId) == false)
            {
                castle.HeroIds.Add(candidateHeroId);
            }
        }

    }
}
