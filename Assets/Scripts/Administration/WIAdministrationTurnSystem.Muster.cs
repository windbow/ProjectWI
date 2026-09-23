using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 사망·포로·적 소속 인물을 제외한 영웅만 탐색과 설득 대상으로 허용합니다.
        public static bool IsHeroRecruitmentCandidate(WICharacterRuntimeState character)
        {
            return character != null && (character.BaseGrade == WICharacterGrade.Hero || character.PromotedToHero == true) &&
                   character.Recruited == false && character.IsDead == false && character.Captured == false &&
                   string.IsNullOrEmpty(character.JoinedEnemyFactionId);
        }

        // 기존 일반 인물의 클래스 권장 역할을 모병 역할로 사용합니다.
        public static WIUnitRole GetMusterRole(WIAdministrationDatabaseSO database, string heroId)
        {
            WIHeroDefinition hero = database.GetHero(heroId);
            return hero == null ? WIUnitRole.Commander :
                database.GetHeroClass(hero.HeroClass)?.RecommendedRole ?? WIUnitRole.Melee;
        }

        // 성 규모별로 한 달에 모집할 수 있는 인원을 반환합니다.
        public static int GetMusterMonthlyLimit(WIAdministrationDatabaseSO database, WICastleRuntimeState castle)
        {
            return database.MusterConfig == null || castle == null ? 0 :
                castle.CastleSize == WICastleSize.Small ? database.MusterConfig.SmallCastleMonthlyLimit :
                database.MusterConfig.DevelopedCastleMonthlyLimit;
        }

        // 세력의 성 수에 따른 총 고용 인원 상한을 반환합니다. 기존 초과 인원은 제거하지 않습니다.
        public static int GetMusterFactionCapacity(WIAdministrationDatabaseSO database, WIAdministrationState state, string factionId)
        {
            WIMusterConfigSO config = database.MusterConfig;
            return config == null ? 0 : Mathf.Min(config.FactionMaximumCapacity, config.FactionBaseCapacity +
                state.Castles.Count(castle => castle.FactionId == factionId) * config.CapacityPerCastle);
        }

        // 성·전투단·이동 중 인물을 중복 없이 집계하고 모집 예약 인원을 더합니다.
        public static int GetMusterFactionCount(WIAdministrationState state, string factionId)
        {
            HashSet<string> ids = new HashSet<string>(state.Castles.Where(castle => castle.FactionId == factionId)
                .SelectMany(castle => castle.HeroIds));
            ids.UnionWith(state.Armies.Where(army => army.FactionId == factionId).SelectMany(army => army.Members.Select(member => member.HeroId)));
            ids.UnionWith(state.CharacterTransfers.Where(transfer => state.GetCastle(transfer.OriginCastleId)?.FactionId == factionId)
                .Select(transfer => transfer.HeroId));
            ids.RemoveWhere(id => state.GetCharacter(id) == null || state.GetCharacter(id).IsDead == true);
            ids.UnionWith(state.Castles.Where(castle => castle.MusterOrder?.FactionId == factionId)
                .SelectMany(castle => castle.MusterOrder.HeroIds));
            return ids.Count;
        }

        // 다른 성이나 전투단에 속하지 않은 생존 일반 후보를 예약과 중복되지 않게 조회합니다.
        private static List<string> GetMusterCandidates(WIAdministrationDatabaseSO database, WIAdministrationState state, WIUnitRole role)
        {
            HashSet<string> unavailable = new HashSet<string>(state.Castles.SelectMany(castle => castle.HeroIds));
            unavailable.UnionWith(state.Armies.SelectMany(army => army.Members.Select(member => member.HeroId)));
            unavailable.UnionWith(state.CharacterTransfers.Select(transfer => transfer.HeroId));
            unavailable.UnionWith(state.Castles.Where(castle => castle.MusterOrder != null).SelectMany(castle => castle.MusterOrder.HeroIds));
            return state.Characters.Where(character => character.BaseGrade == WICharacterGrade.Common &&
                character.PromotedToHero == false && character.Recruited == false && character.IsDead == false &&
                character.Captured == false && character.CommonReturnMonthsRemaining <= 0 &&
                string.IsNullOrEmpty(character.JoinedEnemyFactionId) && unavailable.Contains(character.HeroId) == false &&
                GetMusterRole(database, character.HeroId) == role)
                .OrderBy(character => character.HeroId).Select(character => character.HeroId).ToList();
        }

        // 목적 전투단이 현재 모집 성에서 정상 편성 가능한 상태인지 확인합니다.
        private static WIArmyState GetMusterArmy(WIAdministrationState state, WICastleRuntimeState castle, string armyId)
        {
            return state.Armies.FirstOrDefault(army => army.ArmyId == armyId && army.FactionId == castle.FactionId &&
                army.CurrentCastleId == castle.CastleId && army.IsOperational == true);
        }

        // 이동 예약을 포함한 성 대기 공간 또는 지정 전투단의 빈자리를 반환합니다.
        private static int GetMusterDestinationSpace(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WICastleRuntimeState castle, string armyId)
        {
            if (string.IsNullOrEmpty(armyId) == false)
            {
                WIArmyState army = GetMusterArmy(state, castle, armyId);
                return army == null ? 0 : GetRecommendedArmySize(database, army) - army.Members.Count;
            }
            return castle.GetHeroSlotCount() - GetCastleResidentHeroIds(state, castle).Count -
                state.CharacterTransfers.Count(transfer => transfer.TargetCastleId == castle.CastleId);
        }

        // 비용·인원·예약·소유권을 먼저 검사하여 모병 실패 시 아무 상태도 변경하지 않습니다.
        public static bool CanQueueMuster(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WICastleRuntimeState castle, string factionId, WIUnitRole role, int count, string armyId, out string error)
        {
            error = database.GetText("UI_MUSTER_INVALID");
            WIFactionRuntimeState faction = state.GetFactionState(factionId);
            if (database.MusterConfig == null || castle == null || state.Castles.Contains(castle) == false ||
                castle.FactionId != factionId || faction == null || faction.Eliminated == true ||
                role < WIUnitRole.Vanguard || role > WIUnitRole.Support || count <= 0)
            {
                return false;
            }
            if (castle.MusterOrder != null)
            {
                error = database.GetText("UI_MUSTER_ALREADY");
                return false;
            }
            if (count > GetMusterMonthlyLimit(database, castle) ||
                GetMusterFactionCount(state, factionId) + count > GetMusterFactionCapacity(database, state, factionId))
            {
                error = database.GetText("UI_MUSTER_LIMIT");
                return false;
            }
            if (GetMusterDestinationSpace(database, state, castle, armyId) < count ||
                state.BattleSessions.Any(session => session.CastleId == castle.CastleId && session.Status != WIBattleSessionStatus.Resolved))
            {
                error = database.GetText("UI_MUSTER_SPACE");
                return false;
            }
            if ((long)count * database.MusterConfig.GoldPerCharacter > faction.Gold)
            {
                error = database.GetText("UI_MUSTER_GOLD");
                return false;
            }
            if (GetMusterCandidates(database, state, role).Count < count)
            {
                error = database.GetText("UI_MUSTER_POOL");
                return false;
            }
            error = string.Empty;
            return true;
        }

        // 기존 이름 있는 일반 인물을 예약하고 비용을 한 번만 지불합니다.
        public static bool TryQueueMuster(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WICastleRuntimeState castle, string factionId, WIUnitRole role, int count, string armyId, out string error)
        {
            if (CanQueueMuster(database, state, castle, factionId, role, count, armyId, out error) == false)
            {
                return false;
            }
            int cost = count * database.MusterConfig.GoldPerCharacter;
            castle.MusterOrder = new WIMusterOrder
            {
                FactionId = factionId, ArmyId = armyId, OrderedTurn = state.Turn,
                GoldPaid = cost, HeroIds = GetMusterCandidates(database, state, role).Take(count).ToList()
            };
            state.GetFactionState(factionId).Gold -= cost;
            if (factionId == state.PlayerFactionId)
            {
                state.PendingPlayerGoldSpent += cost;
            }
            return true;
        }

        // 모집 예약을 해제하고 지불한 세력에 비용을 돌려줍니다.
        public static bool CancelMuster(WIAdministrationState state, WICastleRuntimeState castle, string factionId)
        {
            WIMusterOrder order = castle?.MusterOrder;
            if (order == null || order.FactionId != factionId)
            {
                return false;
            }
            WIFactionRuntimeState faction = state.GetFactionState(order.FactionId);
            if (faction != null)
            {
                faction.Gold += order.GoldPaid;
            }
            if (order.FactionId == state.PlayerFactionId)
            {
                state.PendingPlayerGoldSpent = Mathf.Max(0, state.PendingPlayerGoldSpent - order.GoldPaid);
            }
            castle.MusterOrder = null;
            return true;
        }

        // 자동 충원의 대상·목표 인원·월 예산을 검증하고 성별 지시로 저장합니다.
        public static bool SetMusterPolicy(WIAdministrationDatabaseSO database, WIAdministrationState state,
            WICastleRuntimeState castle, string factionId, WIUnitRole role, string armyId, int target, int budget, bool enabled)
        {
            if (castle == null || castle.FactionId != factionId || database.MusterConfig == null ||
                role < WIUnitRole.Vanguard || role > WIUnitRole.Support || budget < 0)
            {
                return false;
            }
            WIArmyState army = state.Armies.FirstOrDefault(item => item.ArmyId == armyId && item.FactionId == factionId);
            if (enabled == true && (army == null || target < 1 || target > GetRecommendedArmySize(database, army) ||
                budget < database.MusterConfig.GoldPerCharacter))
            {
                return false;
            }
            castle.MusterPolicy = new WIMusterPolicy { FactionId = factionId, ArmyId = armyId,
                Role = role, TargetSize = target, MonthlyBudget = budget, Enabled = enabled };
            return true;
        }

        // 다음 달 합류 시 소유권과 공간을 다시 검사하고 출정한 전투단에는 원격 충원하지 않습니다.
        public static void ResolveMusterOrders(WIAdministrationDatabaseSO database, WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.MusterOrder != null).ToList())
            {
                WIMusterOrder order = castle.MusterOrder;
                if (order.OrderedTurn > state.Turn)
                {
                    continue;
                }
                WIArmyState army = GetMusterArmy(state, castle, order.ArmyId);
                string destination = army?.ArmyId;
                bool valid = castle.FactionId == order.FactionId && state.GetFactionState(order.FactionId)?.Eliminated == false &&
                    GetMusterFactionCount(state, order.FactionId) <= GetMusterFactionCapacity(database, state, order.FactionId) &&
                    GetMusterDestinationSpace(database, state, castle, destination) >= order.HeroIds.Count &&
                    state.BattleSessions.Any(session => session.CastleId == castle.CastleId && session.Status != WIBattleSessionStatus.Resolved) == false &&
                    order.HeroIds.All(id => state.IsCommonCharacter(id) == true && state.GetCharacter(id).IsDead == false &&
                        state.GetCharacter(id).Recruited == false && state.GetCharacter(id).Captured == false &&
                        string.IsNullOrEmpty(state.GetCharacter(id).JoinedEnemyFactionId));
                if (valid == false)
                {
                    int refund = order.GoldPaid;
                    CancelMuster(state, castle, order.FactionId);
                    if (order.FactionId == state.PlayerFactionId)
                    {
                        summary.GoldSpent = Mathf.Max(0, summary.GoldSpent - refund);
                        summary.News.Add(string.Format(database.GetText("REPORT_MUSTER_CANCEL"), database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish), refund));
                    }
                    continue;
                }
                foreach (string id in order.HeroIds)
                {
                    WICharacterRuntimeState character = state.GetCharacter(id);
                    character.Recruited = true;
                    character.Discovered = true;
                    character.RecruitmentCastleId = castle.CastleId;
                    castle.HeroIds.Add(id);
                    if (army != null)
                    {
                        AddArmyMember(database, state, army, id, GetMusterRole(database, id));
                    }
                }
                if (order.FactionId == state.PlayerFactionId)
                {
                    summary.News.Add(string.Format(database.GetText("REPORT_MUSTER_DONE"),
                        database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish),
                        string.Join(", ", order.HeroIds.Select(id => database.GetHero(id).DisplayName.Get(database.UseEnglish)))));
                }
                castle.MusterOrder = null;
            }
        }

        // 성별 자동 충원을 월 예산 안에서 준비하고 같은 전투단을 여러 성이 중복 충원하지 않게 합니다.
        public static void PlanMusterPolicies(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            if (database.MusterConfig == null)
            {
                return;
            }
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                WIMusterPolicy policy = castle.MusterPolicy;
                if (policy == null || policy.Enabled == false)
                {
                    continue;
                }
                if (policy.FactionId != castle.FactionId || state.Armies.Any(army => army.ArmyId == policy.ArmyId && army.FactionId == policy.FactionId) == false)
                {
                    policy.Enabled = false;
                    continue;
                }
                WIArmyState target = GetMusterArmy(state, castle, policy.ArmyId);
                if (target == null || castle.MusterOrder != null)
                {
                    continue;
                }
                int count = Mathf.Min(GetMusterMonthlyLimit(database, castle), policy.TargetSize - target.Members.Count,
                    policy.MonthlyBudget / database.MusterConfig.GoldPerCharacter);
                for (; count > 0; count--)
                {
                    if (TryQueueMuster(database, state, castle, castle.FactionId, policy.Role, count, policy.ArmyId, out _) == true)
                    {
                        break;
                    }
                }
            }
        }

        // AI는 영웅 영입 허용과 무관하게 실제 비용과 동일 상한으로 부족한 성의 일반 인원을 모집합니다.
        public static void PlanAIMuster(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            WIMusterConfigSO config = database.MusterConfig;
            if (config == null)
            {
                return;
            }
            foreach (WIFactionRuntimeState faction in state.Factions.Where(item => item.FactionId != state.PlayerFactionId && item.Eliminated == false))
            {
                if (GetMusterFactionCount(state, faction.FactionId) >= GetMusterFactionCapacity(database, state, faction.FactionId))
                {
                    continue;
                }
                int orders = 0;
                foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == faction.FactionId)
                    .OrderBy(item => item.HeroIds.Count).ThenBy(item => item.CastleId))
                {
                    if (orders >= config.AIOrdersPerMonth)
                    {
                        break;
                    }
                    bool front = castle.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId != faction.FactionId);
                    int desired = front == true ? config.AIFrontTarget : config.AIRearTarget;
                    if (castle.HeroIds.Count >= desired)
                    {
                        continue;
                    }
                    WIArmyState army = state.Armies.FirstOrDefault(item => item.FactionId == faction.FactionId && item.CurrentCastleId == castle.CastleId &&
                        item.IsOperational == true && item.Members.Count < GetRecommendedArmySize(database, item));
                    foreach (WIUnitRole role in new[] { WIUnitRole.Melee, WIUnitRole.Ranged, WIUnitRole.Support, WIUnitRole.Vanguard, WIUnitRole.Magic }
                        .OrderBy(role => castle.HeroIds.Count(id => GetMusterRole(database, id) == role)))
                    {
                        if (TryQueueMuster(database, state, castle, faction.FactionId, role, 1, army?.ArmyId, out _) == true)
                        {
                            orders++;
                            break;
                        }
                    }
                }
            }
        }
    }
}
