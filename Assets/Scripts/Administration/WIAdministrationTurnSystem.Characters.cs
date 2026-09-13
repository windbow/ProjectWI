using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {

        // 성에 등록된 인물 중 전투단 편성 인원을 제외한 거주 인물 목록을 반환합니다.
        public static List<string> GetCastleResidentHeroIds(WIAdministrationState state, WICastleRuntimeState castle)
        {
            if (state == null || castle == null)
            {
                return new List<string>();
            }
            var armyMembers = new HashSet<string>(state.Armies.SelectMany(army => army.Members).Select(member => member.HeroId));
            return castle.HeroIds.Where(id => armyMembers.Contains(id) == false).Distinct().ToList();
        }

        // 이번 달에 배정된 인물 활동을 처리하고 인물 상태와 소식을 갱신합니다.
        private static void ResolveCharacterActivities(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || character.Activity == WICharacterActivityType.None)
                {
                    continue;
                }

                if (CanPerformCharacterActivity(state, character.HeroId, character.Activity,
                    database.Automation.TalentOfficeCapacity) == false)
                {
                    character.Activity = WICharacterActivityType.None;
                    character.ActivityTargetHeroId = string.Empty;
                    continue;
                }

                WIHeroDefinition hero = database.GetHero(character.HeroId);
                switch (character.Activity)
                {
                    case WICharacterActivityType.Search:
                        ResolveSearchActivity(database, state, hero, character, summary);
                        break;
                    case WICharacterActivityType.Socialize:
                        ResolveSocializeActivity(database, state, hero, character, summary);
                        break;
                    case WICharacterActivityType.Recruit:
                        ResolveRecruitActivity(database, state, hero, character, summary);
                        break;
                    case WICharacterActivityType.Training:
                        character.Experience += 10 + hero.Leadership / 10;
                        character.Merit += 3;
                        summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)} · 개인 훈련 완료");
                        break;
                    case WICharacterActivityType.Rest:
                        character.Fatigue = Mathf.Max(0, character.Fatigue - database.CharacterRestFatigueRecovery);
                        character.InjuryMonths = Mathf.Max(0, character.InjuryMonths - 1);
                        summary.News.Add($"{hero.DisplayName.Get(database.UseEnglish)} · 휴식과 회복 완료");
                        break;
                }

                if (character.Activity != WICharacterActivityType.Rest)
                {
                    character.Fatigue = Mathf.Clamp(character.Fatigue + 10, 0, 100);
                }

                character.Activity = WICharacterActivityType.None;
                character.ActivityTargetHeroId = string.Empty;
            }
        }


        // 공훈, 명성과 특별 성취를 갖춘 일반 인물을 영웅 승격 후보로 등록합니다.
        private static void ResolvePromotionCandidates(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || character.BaseGrade != WICharacterGrade.Common ||
                    character.PromotedToHero || character.PromotionAchievement == false ||
                    character.Merit < database.PromotionRequiredMerit ||
                    character.Reputation < database.PromotionRequiredReputation ||
                    state.PendingHeroPromotionIds.Contains(character.HeroId))
                {
                    continue;
                }
                state.PendingHeroPromotionIds.Add(character.HeroId);
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                summary.News.Add($"영웅 승격 후보 · {hero.DisplayName.Get(database.UseEnglish)}");
            }
        }

        // 영향력을 지불하고 승격 후보 일반 인물을 영웅으로 확정합니다.
        public static bool PromoteCommonCharacter(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string heroId)
        {
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            WIFactionRuntimeState faction = state.GetPlayerFactionState();
            if (character == null || faction == null || state.PendingHeroPromotionIds.Contains(heroId) == false ||
                character.BaseGrade != WICharacterGrade.Common || character.PromotedToHero ||
                faction.Influence < database.PromotionInfluenceCost)
            {
                return false;
            }
            faction.Influence -= database.PromotionInfluenceCost;
            character.PromotedToHero = true;
            character.LoyaltyState = WILoyaltyState.Stable;
            state.PendingHeroPromotionIds.Remove(heroId);
            return true;
        }

        // 같은 진영 영토의 전체 경로를 계산해 최종 목적지까지 인물 이동을 시작합니다.
        public static bool StartCharacterTransfer(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            string heroId,
            string targetCastleId)
        {
            WICastleRuntimeState origin = state.Castles.FirstOrDefault(castle => castle.HeroIds.Contains(heroId));
            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            int reservedSlots = state.CharacterTransfers.Count(transfer => transfer.TargetCastleId == targetCastleId);
            List<string> route = origin == null
                ? new List<string>()
                : GetCharacterTransferPath(state, origin.CastleId, targetCastleId);
            if (origin == null || target == null || origin.CastleId == targetCastleId ||
                origin.FactionId != target.FactionId ||
                route.Count == 0 ||
                GetCastleResidentHeroIds(state, target).Count + reservedSlots >= target.GetHeroSlotCount() ||
                state.IsCharacterBusy(heroId) || origin.GovernorHeroId == heroId)
            {
                return false;
            }

            origin.HeroIds.Remove(heroId);
            state.CharacterTransfers.Add(new WICharacterTransferState
            {
                HeroId = heroId,
                OriginCastleId = origin.CastleId,
                TargetCastleId = targetCastleId,
                CurrentCastleId = origin.CastleId,
                RouteCastleIds = route,
                RouteIndex = 0,
                RemainingMonths = route.Count
            });
            return true;
        }

        // 아군 성만 통과하는 출발 성에서 최종 목적지까지의 경유 성 목록을 반환합니다.
        public static List<string> GetCharacterTransferPath(
            WIAdministrationState state,
            string originCastleId,
            string targetCastleId)
        {
            WICastleRuntimeState origin = state?.GetCastle(originCastleId);
            WICastleRuntimeState target = state?.GetCastle(targetCastleId);
            if (origin == null || target == null || origin.FactionId != target.FactionId ||
                originCastleId == targetCastleId)
            {
                return new List<string>();
            }

            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>();
            queue.Enqueue(originCastleId);
            previous[originCastleId] = string.Empty;
            while (queue.Count > 0)
            {
                string currentId = queue.Dequeue();
                WICastleRuntimeState current = state.GetCastle(currentId);
                if (current == null)
                {
                    continue;
                }
                foreach (string adjacentId in current.AdjacentCastleIds)
                {
                    if (previous.ContainsKey(adjacentId) ||
                        state.GetCastle(adjacentId)?.FactionId != origin.FactionId)
                    {
                        continue;
                    }
                    previous[adjacentId] = currentId;
                    if (adjacentId == targetCastleId)
                    {
                        queue.Clear();
                        break;
                    }
                    queue.Enqueue(adjacentId);
                }
            }
            if (previous.ContainsKey(targetCastleId) == false)
            {
                return new List<string>();
            }

            List<string> route = new List<string>();
            string step = targetCastleId;
            while (step != originCastleId)
            {
                route.Add(step);
                step = previous[step];
            }
            route.Reverse();
            return route;
        }

        // 성에 주둔한 유휴 인물을 영지관로 임명하거나 기존 영지관을 교체합니다.
        public static bool AssignGovernor(WIAdministrationState state, string castleId, string heroId)
        {
            WICastleRuntimeState castle = state.GetCastle(castleId);
            if (castle == null || castle.HeroIds.Contains(heroId) == false ||
                state.IsCharacterBusy(heroId) || IsAdministrationCapable(state, heroId) == false)
            {
                return false;
            }

            foreach (WICastleRuntimeState other in state.Castles.Where(item => item.GovernorHeroId == heroId))
            {
                other.GovernorHeroId = string.Empty;
                other.DelegatedToGovernor = false;
            }
            castle.GovernorHeroId = heroId;
            return true;
        }

        // 등급이나 승격 여부와 무관하게 내정 특성과 생존 상태를 확인합니다.
        public static bool IsAdministrationCapable(WIAdministrationState state, string heroId)
        {
            return HasOperationalTrait(state, heroId, WITraitType.Administration);
        }

        // 생존·비포로 인물이 지정한 업무 특성을 보유했는지 확인합니다.
        public static bool HasOperationalTrait(WIAdministrationState state, string heroId, WITraitType trait)
        {
            WICharacterRuntimeState character = state?.GetCharacter(heroId);
            return character != null && character.IsDead == false && character.Captured == false &&
                   character.Traits != null && character.Traits.Contains(trait);
        }

        // 선술집 인재실의 탐색·영입 자격을 확인합니다.
        public static bool CanRecruitTalent(WIAdministrationState state, string heroId)
        {
            return HasOperationalTrait(state, heroId, WITraitType.TalentRecruitment);
        }

        // 연구 담당 자격인 학자 특성을 확인합니다.
        public static bool CanResearch(WIAdministrationState state, string heroId)
        {
            return HasOperationalTrait(state, heroId, WITraitType.Scholar);
        }

        // 계략 담당 자격인 첩보 특성을 확인합니다.
        public static bool CanPerformScheme(WIAdministrationState state, string heroId)
        {
            return HasOperationalTrait(state, heroId, WITraitType.Espionage);
        }

        // 인물이 해당 진영의 성이나 전투단에 소속되어 있는지 확인합니다.
        private static bool IsHeroInFaction(WIAdministrationState state, string heroId, string factionId)
        {
            return state.Castles.Any(castle => castle.FactionId == factionId && castle.HeroIds.Contains(heroId)) ||
                   state.Armies.Any(army => army.FactionId == factionId && army.Members.Any(member => member.HeroId == heroId));
        }

        // 훈련과 휴식은 모든 인물이 수행하고 탐색·교류·영입에는 내정 특성이 필요합니다.
        public static bool CanPerformCharacterActivity(
            WIAdministrationState state,
            string heroId,
            WICharacterActivityType activity,
            int talentOfficeCapacity = 2)
        {
            WICharacterRuntimeState character = state?.GetCharacter(heroId);
            if (character == null || character.IsDead || character.Captured)
            {
                return false;
            }
            if (activity == WICharacterActivityType.Training || activity == WICharacterActivityType.Rest ||
                activity == WICharacterActivityType.Socialize)
            {
                return true;
            }
            if ((activity == WICharacterActivityType.Search || activity == WICharacterActivityType.Recruit) &&
                CanUseTalentOffice(state, heroId, talentOfficeCapacity) == false)
            {
                return false;
            }
            return activity != WICharacterActivityType.None && CanRecruitTalent(state, heroId);
        }

        // 선술집 인재실의 탐색·영입 담당자 두 자리 중 하나를 사용할 수 있는지 확인합니다.
        public static bool CanUseTalentOffice(WIAdministrationState state, string heroId, int capacity = 2)
        {
            WICastleRuntimeState castle = state?.Castles.FirstOrDefault(item => item.HeroIds.Contains(heroId));
            if (castle == null)
            {
                return false;
            }
            List<string> assigned = castle.HeroIds.Where(id =>
            {
                WICharacterRuntimeState character = state.GetCharacter(id);
                return character != null &&
                       (IsTalentOfficeActivity(character.Activity) || IsTalentOfficeActivity(character.StandingActivity));
            }).Take(Mathf.Max(1, capacity)).ToList();
            return assigned.Contains(heroId) || assigned.Count < Mathf.Max(1, capacity);
        }

        // 탐색과 영입을 선술집 인재실 전담 활동으로 구분합니다.
        public static bool IsTalentOfficeActivity(WICharacterActivityType activity)
        {
            return activity == WICharacterActivityType.Search || activity == WICharacterActivityType.Recruit;
        }

        // 현재 성의 선술집 인재실에 배치된 탐색·영입 담당자 수를 반환합니다.
        public static int GetTalentOfficeWorkerCount(WIAdministrationState state, WICastleRuntimeState castle)
        {
            if (state == null || castle == null)
            {
                return 0;
            }
            return castle.HeroIds.Count(id =>
            {
                WICharacterRuntimeState character = state.GetCharacter(id);
                return character != null &&
                       (IsTalentOfficeActivity(character.Activity) || IsTalentOfficeActivity(character.StandingActivity));
            });
        }

        // 이동 인물을 매월 한 경유 성씩 진행시키고 경로 단절 시 재탐색하거나 현재 성에 정착시킵니다.
        private static void ResolveCharacterTransfers(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            foreach (WICharacterTransferState transfer in state.CharacterTransfers.ToList())
            {
                NormalizeCharacterTransfer(state, transfer);
                WICastleRuntimeState current = state.GetCastle(transfer.CurrentCastleId);
                WICastleRuntimeState target = state.GetCastle(transfer.TargetCastleId);
                bool routeValid = current != null && target != null &&
                                  current.FactionId == target.FactionId &&
                                  transfer.RouteIndex < transfer.RouteCastleIds.Count &&
                                  current.AdjacentCastleIds.Contains(transfer.RouteCastleIds[transfer.RouteIndex]) &&
                                  state.GetCastle(transfer.RouteCastleIds[transfer.RouteIndex])?.FactionId == current.FactionId;
                if (routeValid == false)
                {
                    List<string> newRoute = current == null
                        ? new List<string>()
                        : GetCharacterTransferPath(state, current.CastleId, transfer.TargetCastleId);
                    if (newRoute.Count == 0)
                    {
                        SettleInterruptedCharacterTransfer(database, state, summary, transfer, current);
                        continue;
                    }
                    transfer.RouteCastleIds = newRoute;
                    transfer.RouteIndex = 0;
                }

                string nextCastleId = transfer.RouteCastleIds[transfer.RouteIndex];
                transfer.CurrentCastleId = nextCastleId;
                transfer.RouteIndex += 1;
                transfer.RemainingMonths = transfer.RouteCastleIds.Count - transfer.RouteIndex;
                if (transfer.RouteIndex < transfer.RouteCastleIds.Count)
                {
                    continue;
                }

                target = state.GetCastle(transfer.TargetCastleId);
                if (target != null && GetCastleResidentHeroIds(state, target).Count < target.GetHeroSlotCount())
                {
                    target.HeroIds.Add(transfer.HeroId);
                    string heroName = database.GetHero(transfer.HeroId).DisplayName.Get(database.UseEnglish);
                    string castleName = database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish);
                    summary.News.Add($"{heroName} 이동 완료 · {castleName}");
                }
                else
                {
                    SettleInterruptedCharacterTransfer(database, state, summary, transfer,
                        state.GetCastle(transfer.CurrentCastleId));
                    continue;
                }
                state.CharacterTransfers.Remove(transfer);
            }
        }

        // 이전 저장의 인접 이동 데이터를 새 경로 이동 상태로 보정합니다.
        private static void NormalizeCharacterTransfer(
            WIAdministrationState state,
            WICharacterTransferState transfer)
        {
            if (string.IsNullOrEmpty(transfer.CurrentCastleId))
            {
                transfer.CurrentCastleId = transfer.OriginCastleId;
            }
            if (transfer.RouteCastleIds == null || transfer.RouteCastleIds.Count == 0)
            {
                transfer.RouteCastleIds = GetCharacterTransferPath(
                    state, transfer.CurrentCastleId, transfer.TargetCastleId);
                transfer.RouteIndex = 0;
                transfer.RemainingMonths = Mathf.Max(1, transfer.RouteCastleIds.Count);
            }
        }

        // 경로가 끊기거나 목적지가 가득 찬 인물을 현재 경유 성 또는 출발 성에 안전하게 되돌립니다.
        private static void SettleInterruptedCharacterTransfer(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary,
            WICharacterTransferState transfer,
            WICastleRuntimeState current)
        {
            WICastleRuntimeState origin = state.GetCastle(transfer.OriginCastleId);
            string factionId = current?.FactionId ?? origin?.FactionId ?? string.Empty;
            WICastleRuntimeState settlement = current != null && current.FactionId == factionId &&
                                              GetCastleResidentHeroIds(state, current).Count < current.GetHeroSlotCount()
                ? current
                : origin != null && origin.FactionId == factionId &&
                  GetCastleResidentHeroIds(state, origin).Count < origin.GetHeroSlotCount()
                    ? origin
                    : state.Castles.FirstOrDefault(castle => castle.FactionId == factionId &&
                        GetCastleResidentHeroIds(state, castle).Count < castle.GetHeroSlotCount());
            if (settlement != null && settlement.HeroIds.Contains(transfer.HeroId) == false)
            {
                settlement.HeroIds.Add(transfer.HeroId);
            }
            string heroName = database.GetHero(transfer.HeroId)?.DisplayName.Get(database.UseEnglish) ?? transfer.HeroId;
            string castleName = settlement == null
                ? "배치 가능한 성 없음"
                : database.GetCastle(settlement.CastleId)?.DisplayName.Get(database.UseEnglish) ?? settlement.CastleId;
            summary.News.Add($"{heroName} 이동 중단 · {castleName}");
            state.CharacterTransfers.Remove(transfer);
        }
    }
}
