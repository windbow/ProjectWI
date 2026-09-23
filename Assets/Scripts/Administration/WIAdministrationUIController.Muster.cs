using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 기존 군사 카드 프리팹에 바인딩할 성별 모병 화면을 구성합니다.
        private bool BuildMusterPanel(string mode, string context, WIAdministrationMilitarySnapshot snapshot, out string error)
        {
            error = string.Empty;
            snapshot.Title = database.GetText("UI_MUSTER_TITLE");
            snapshot.Summary = database.GetText("UI_MUSTER_HINT");
            if (database.MusterConfig == null)
            {
                error = database.GetText("UI_MUSTER_INVALID");
                return false;
            }
            if (mode == "muster-castles")
            {
                foreach (WICastleRuntimeState item in state.Castles.Where(item => item.FactionId == state.PlayerFactionId))
                {
                    snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "muster-castle", Id = item.CastleId,
                        Title = database.GetCastle(item.CastleId).DisplayName.Get(database.UseEnglish),
                        Description = string.Format(database.GetText("UI_MUSTER_CASTLE"), WIAdministrationTurnSystem.GetMusterMonthlyLimit(database, item),
                            item.MusterOrder?.HeroIds.Count ?? 0) });
                }
                return true;
            }
            WICastleRuntimeState castle = state.GetCastle(context);
            if (castle == null || castle.FactionId != state.PlayerFactionId)
            {
                error = database.GetText("UI_MUSTER_INVALID");
                return false;
            }
            WIMusterPolicy policy = GetDisplayedMusterPolicy(castle);
            WIArmyState army = state.Armies.FirstOrDefault(item => item.ArmyId == policy.ArmyId && item.FactionId == state.PlayerFactionId);
            string destination = army?.DisplayName ?? database.GetText("UI_MUSTER_RESERVE");
            int unitCost = database.MusterConfig.GoldPerCharacter;
            snapshot.Title += " · " + database.GetCastle(context).DisplayName.Get(database.UseEnglish);
            snapshot.Summary = string.Format(database.GetText("UI_MUSTER_STATUS"), state.Gold,
                WIAdministrationTurnSystem.GetMusterFactionCount(state, state.PlayerFactionId),
                WIAdministrationTurnSystem.GetMusterFactionCapacity(database, state, state.PlayerFactionId),
                database.GetText("UI_MUSTER_ROLE_" + (int)policy.Role), destination,
                castle.MusterOrder?.HeroIds.Count ?? 0, policy.Enabled == true ? database.GetText("UI_MUSTER_ON") : database.GetText("UI_MUSTER_OFF"),
                policy.TargetSize, policy.MonthlyBudget);
            if (castle.MusterOrder != null)
            {
                AddMusterCard(snapshot, "muster-cancel", "UI_MUSTER_CANCEL", string.Format(database.GetText("UI_MUSTER_REFUND"), castle.MusterOrder.GoldPaid));
            }
            else
            {
                for (int count = 1; count <= WIAdministrationTurnSystem.GetMusterMonthlyLimit(database, castle); count++)
                {
                    bool allowed = WIAdministrationTurnSystem.CanQueueMuster(database, state, castle, state.PlayerFactionId,
                        policy.Role, count, policy.ArmyId, out string reason);
                    snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "muster-queue", Value = count,
                        Title = string.Format(database.GetText("UI_MUSTER_QUEUE"), count, count * unitCost),
                        Description = allowed == true ? database.GetText("UI_MUSTER_NEXT_MONTH") : reason, Interactable = allowed });
                }
            }
            foreach (WIUnitRole role in new[] { WIUnitRole.Vanguard, WIUnitRole.Melee, WIUnitRole.Ranged, WIUnitRole.Magic, WIUnitRole.Support })
            {
                AddMusterCard(snapshot, "muster-role", "UI_MUSTER_ROLE_" + (int)role,
                    policy.Role == role ? database.GetText("UI_MUSTER_SELECTED") : database.GetText("UI_MUSTER_CHOOSE_ROLE"), (int)role);
            }
            AddMusterCard(snapshot, "muster-army", "UI_MUSTER_RESERVE", database.GetText("UI_MUSTER_RESERVE_HINT"));
            foreach (WIArmyState target in state.Armies.Where(item => item.FactionId == state.PlayerFactionId &&
                item.CurrentCastleId == context && item.IsOperational == true))
            {
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = "muster-army", Id = target.ArmyId,
                    Title = string.Format(database.GetText("UI_MUSTER_DESTINATION"), target.DisplayName),
                    Description = string.Format(database.GetText("UI_MUSTER_ARMY_SIZE"), target.Members.Count,
                        WIAdministrationTurnSystem.GetRecommendedArmySize(database, target)) });
            }
            AddMusterCard(snapshot, "muster-toggle", policy.Enabled == true ? "UI_MUSTER_STOP" : "UI_MUSTER_START",
                database.GetText("UI_MUSTER_AUTO_HINT"), 0, policy.Enabled == true || army != null);
            AddMusterCard(snapshot, "muster-target", "UI_MUSTER_TARGET_LESS", string.Format(database.GetText("UI_MUSTER_TARGET"), policy.TargetSize), -1, army != null && policy.TargetSize > 1);
            AddMusterCard(snapshot, "muster-target", "UI_MUSTER_TARGET_MORE", string.Format(database.GetText("UI_MUSTER_TARGET"), policy.TargetSize), 1,
                army != null && policy.TargetSize < WIAdministrationTurnSystem.GetRecommendedArmySize(database, army));
            AddMusterCard(snapshot, "muster-budget", "UI_MUSTER_BUDGET_LESS", string.Format(database.GetText("UI_MUSTER_BUDGET"), policy.MonthlyBudget), -unitCost, policy.MonthlyBudget > unitCost);
            AddMusterCard(snapshot, "muster-budget", "UI_MUSTER_BUDGET_MORE", string.Format(database.GetText("UI_MUSTER_BUDGET"), policy.MonthlyBudget), unitCost,
                policy.MonthlyBudget < unitCost * WIAdministrationTurnSystem.GetMusterMonthlyLimit(database, castle));
            return true;
        }

        // 구 저장에 설정이 없으면 실제 저장을 바꾸지 않고 기본 모병 선택값을 반환합니다.
        private WIMusterPolicy GetDisplayedMusterPolicy(WICastleRuntimeState castle)
        {
            WIMusterPolicy policy = castle.MusterPolicy;
            if (policy == null || policy.FactionId != castle.FactionId)
            {
                return new WIMusterPolicy { FactionId = castle.FactionId, Role = WIUnitRole.Melee,
                    TargetSize = 4, MonthlyBudget = database.MusterConfig.GoldPerCharacter };
            }
            return policy;
        }

        // 고정 문자열 UID를 사용하여 재사용 군사 목록의 명령 데이터를 추가합니다.
        private void AddMusterCard(WIAdministrationMilitarySnapshot snapshot, string kind, string titleUid, string description, int value = 0, bool enabled = true)
        {
            snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot { Kind = kind, Title = database.GetText(titleUid),
                Description = description, Value = value, Interactable = enabled });
        }

        // 사용자 소유권을 재확인하고 모병 예약 또는 자동 충원 설정만 갱신합니다.
        private bool ExecuteMusterAction(string action, string context, string id, int value, out string error)
        {
            error = database.GetText("UI_MUSTER_INVALID");
            WICastleRuntimeState castle = state.GetCastle(context);
            if (castle == null || castle.FactionId != state.PlayerFactionId || database.MusterConfig == null)
            {
                return false;
            }
            WIMusterPolicy current = GetDisplayedMusterPolicy(castle);
            if (action == "muster-queue")
            {
                bool result = WIAdministrationTurnSystem.TryQueueMuster(database, state, castle, state.PlayerFactionId,
                    current.Role, value, current.ArmyId, out error);
                if (result == true)
                {
                    RefreshAll();
                }
                return result;
            }
            if (action == "muster-cancel")
            {
                // 취소 후 같은 월 자동 지시가 즉시 재예약하지 않도록 자동 충원도 중지합니다.
                if (WIAdministrationTurnSystem.CancelMuster(state, castle, state.PlayerFactionId) == false)
                {
                    return false;
                }
                current.Enabled = false;
                castle.MusterPolicy = current;
            }
            else
            {
                WIUnitRole role = current.Role;
                string armyId = current.ArmyId;
                int target = current.TargetSize;
                int budget = current.MonthlyBudget;
                bool enabled = current.Enabled;
                switch (action)
                {
                    case "muster-role": role = (WIUnitRole)value; break;
                    case "muster-army": armyId = id; enabled = false; break;
                    case "muster-target": target += value; break;
                    case "muster-budget": budget += value; break;
                    case "muster-toggle": enabled = enabled == false; break;
                    default: return false;
                }
                WIArmyState army = state.Armies.FirstOrDefault(item => item.ArmyId == armyId && item.FactionId == state.PlayerFactionId);
                target = Mathf.Clamp(target, 1, army == null ? 4 : WIAdministrationTurnSystem.GetRecommendedArmySize(database, army));
                budget = Mathf.Clamp(budget, database.MusterConfig.GoldPerCharacter,
                    database.MusterConfig.GoldPerCharacter * WIAdministrationTurnSystem.GetMusterMonthlyLimit(database, castle));
                if (WIAdministrationTurnSystem.SetMusterPolicy(database, state, castle, state.PlayerFactionId, role, armyId, target, budget, enabled) == false)
                {
                    return false;
                }
            }
            error = string.Empty;
            RefreshAll();
            return true;
        }
    }
}
