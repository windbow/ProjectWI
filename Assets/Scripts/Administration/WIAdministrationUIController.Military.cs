using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 현재 진영의 전투단 목록과 새 전투단 편성 진입점을 표시합니다.
        private void OpenMilitaryModal()
        {
            WITutorialSystem.Complete(state, "tutorial_military");
            VisualElement panel = CreateModal("군사 · 전투단 목록");
            foreach (WIBattleSessionState session in state.BattleSessions.FindAll(item =>
                         item.Status != WIBattleSessionStatus.Resolved && item.PlayerInvolved))
            {
                Button battleButton = new Button(() => OpenBattleSessionModal(session));
                battleButton.text = $"전투 세션 · {database.GetCastle(session.CastleId).DisplayName.Get(database.UseEnglish)} · {session.Status}";
                panel.Add(battleButton);
            }
            foreach (WIArmyState army in state.Armies)
            {
                if (army.FactionId != state.PlayerFactionId) continue;
                Button armyButton = new Button(() => OpenArmyDetailModal(army));
                string status = army.AwaitingBattle ? "전투 대기" : (army.IsMoving ? $"이동 {army.RemainingTravelMonths}개월" : "주둔");
                armyButton.text = $"{army.DisplayName} · {army.Members.Count}/{WIAdministrationTurnSystem.GetRecommendedArmySize(database, army)} · {army.Proficiency} · 보급 {army.Supply} · {status}";
                panel.Add(armyButton);
            }

            Button create = new Button(OpenArmyCastleModal);
            create.text = "새 전투단 편성";
            panel.Add(create);
        }

        // 실시간 전투에 전달될 참가 진영과 전력 스냅샷을 표시합니다.
        private void OpenBattleSessionModal(WIBattleSessionState session)
        {
            VisualElement panel = CreateModal($"전투 세션 {session.SessionId}");
            panel.Add(new Label($"전장: {database.GetCastle(session.CastleId).DisplayName.Get(database.UseEnglish)}"));
            panel.Add(new Label($"공격 진영: {database.GetFaction(session.AttackerFactionId).DisplayName.Get(database.UseEnglish)} · 전력 {session.AttackerPowerSnapshot}"));
            panel.Add(new Label($"수비 진영: {database.GetFaction(session.DefenderFactionId).DisplayName.Get(database.UseEnglish)} · 전력 {session.DefenderPowerSnapshot}"));
            panel.Add(new Label($"수비 전투단 {session.DefenderArmyIds.Count}개 · 플레이어 참가 {(session.PlayerInvolved ? "예" : "아니오")} · 상태 {session.Status}"));
            if (session.PlayerInvolved && session.Status == WIBattleSessionStatus.Pending)
            {
                Button startBattle = new Button(() =>
                {
                    WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
                    if (service == null || service.StartBattle(session.SessionId) == false)
                    {
                        ShowMessage("전투 씬을 시작할 수 없습니다.");
                    }
                });
                startBattle.text = "전투 시작";
                panel.Add(startBattle);
            }
        }

        // 새 전투단을 편성할 플레이어 소유 성을 선택합니다.
        private void OpenArmyCastleModal()
        {
            VisualElement panel = CreateModal("전투단 편성 성 선택");
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                WIFactionDefinition faction = database.GetFaction(castle.FactionId);
                if (faction == null || faction.PlayerFaction == false)
                {
                    continue;
                }

                Button button = new Button(() => OpenArmyCommanderModal(castle));
                button.text = database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish);
                panel.Add(button);
            }
        }

        // 선택한 성의 대기 인물 중 새 전투단의 대장을 선택합니다.
        private void OpenArmyCommanderModal(WICastleRuntimeState castle)
        {
            VisualElement panel = CreateModal("전투단 대장 선택");
            bool hasCandidate = false;
            foreach (string heroId in castle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId))
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition hero = database.GetHero(heroId);
                Button button = new Button(() =>
                {
                    WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, heroId);
                    CloseModal();
                    if (army != null) OpenArmyDetailModal(army);
                });
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 통솔 {hero.Leadership}";
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 전투단 구성원, 역할, 숙련, 보급과 현재 위치를 표시합니다.
        private void OpenArmyDetailModal(WIArmyState army)
        {
            VisualElement panel = CreateModal(army.DisplayName);
            string strategicTarget = string.IsNullOrEmpty(army.StrategicTargetCastleId)
                ? "없음"
                : database.GetCastle(army.StrategicTargetCastleId).DisplayName.Get(database.UseEnglish);
            panel.Add(new Label($"위치: {database.GetCastle(army.CurrentCastleId).DisplayName.Get(database.UseEnglish)} · 숙련 {army.Proficiency} · 보급 {army.Supply}"));
            panel.Add(new Label($"전략 임무: {army.Mission} · 목표: {strategicTarget}"));
            if (army.ReorganizationMonths > 0)
            {
                panel.Add(new Label($"재편성 중 · 남은 기간 {army.ReorganizationMonths}개월"));
            }
            if (army.LastBattleOutcome != WIBattleOutcome.None)
            {
                panel.Add(new Label($"최근 전투: {army.LastBattleOutcome} · 전투력 {army.LastBattlePower}"));
            }
            foreach (WIArmyMemberState member in army.Members)
            {
                WIHeroDefinition hero = database.GetHero(member.HeroId);
                if (member.Role == WIUnitRole.Commander || army.IsMoving || army.AwaitingBattle)
                {
                    panel.Add(new Label($"{GetUnitRoleDisplayName(member.Role)} · {hero.DisplayName.Get(database.UseEnglish)}"));
                }
                else
                {
                    Button remove = new Button(() =>
                    {
                        WIAdministrationTurnSystem.RemoveArmyMember(state, army, member.HeroId);
                        CloseModal();
                        OpenArmyDetailModal(army);
                    });
                    remove.text = $"{GetUnitRoleDisplayName(member.Role)} · {hero.DisplayName.Get(database.UseEnglish)} · 전투단에서 제외";
                    panel.Add(remove);
                }
            }

            if (army.IsMoving == false && army.AwaitingBattle == false)
            {
                Button addMember = new Button(() => OpenArmyRoleModal(army));
                addMember.text = "전투단원 추가";
                panel.Add(addMember);
                Button march = new Button(() => OpenArmyTargetModal(army));
                march.text = "이동 / 원정";
                panel.Add(march);
                Button training = new Button(() =>
                {
                    WIAdministrationTurnSystem.ScheduleJointTraining(army);
                    CloseModal();
                    OpenArmyDetailModal(army);
                });
                training.text = army.JointTrainingScheduled ? "합동 훈련 예약됨" : "다음 달 합동 훈련";
                training.SetEnabled(army.JointTrainingScheduled == false);
                panel.Add(training);
                Button disband = new Button(() =>
                {
                    WIAdministrationTurnSystem.DisbandArmy(state, army);
                    CloseModal();
                    OpenMilitaryModal();
                });
                disband.text = "전투단 해산";
                panel.Add(disband);
            }
            else if (army.AwaitingBattle)
            {
                panel.Add(new Label("적 성 외곽에서 전투 대기 중입니다. 4단계 전투 결과가 승리로 전달되면 점령 절차가 시작됩니다."));
            }
        }

        // 새 전투단원에게 부여할 전투 역할을 선택합니다.
        private void OpenArmyRoleModal(WIArmyState army)
        {
            VisualElement panel = CreateModal("추가할 역할 선택");
            foreach (WIUnitRole role in System.Enum.GetValues(typeof(WIUnitRole)))
            {
                if (role == WIUnitRole.Commander)
                {
                    continue;
                }

                Button button = new Button(() => OpenArmyMemberModal(army, role));
                button.text = GetUnitRoleDisplayName(role);
                panel.Add(button);
            }
        }

        // 전투단과 같은 성에 있는 대기 인물을 역할에 배치합니다.
        private void OpenArmyMemberModal(WIArmyState army, WIUnitRole role)
        {
            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            VisualElement panel = CreateModal($"{GetUnitRoleDisplayName(role)} 인물 선택");
            bool hasCandidate = false;
            foreach (string heroId in castle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId))
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition hero = database.GetHero(heroId);
                Button button = new Button(() =>
                {
                    WIAdministrationTurnSystem.AddArmyMember(database, state, army, heroId, role);
                    CloseModal();
                    OpenArmyDetailModal(army);
                });
                button.text = hero.DisplayName.Get(database.UseEnglish);
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 전투단이 이동하거나 공격할 인접 성을 선택합니다.
        private void OpenArmyTargetModal(WIArmyState army)
        {
            WICastleDefinition origin = database.GetCastle(army.CurrentCastleId);
            VisualElement panel = CreateModal("이동 / 원정 목표");
            foreach (string targetId in origin.AdjacentCastleIds)
            {
                WICastleRuntimeState target = state.GetCastle(targetId);
                string action = target.FactionId == army.FactionId ? "이동" : "원정";
                Button button = new Button(() =>
                {
                    if (WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, targetId) == false)
                    {
                        ShowMessage(GetArmyMarchFailureMessage(army, target));
                        return;
                    }
                    CloseModal();
                    RefreshAll();
                });
                string cost = target.FactionId == army.FactionId ? string.Empty : " · 영향력 20";
                button.text = $"{action} · {database.GetCastle(targetId).DisplayName.Get(database.UseEnglish)}{cost}";
                panel.Add(button);
            }
        }

        // 원정 시작이 거부된 경우 현재 상태에서 가장 직접적인 해결 방법을 안내합니다.
        private string GetArmyMarchFailureMessage(WIArmyState army, WICastleRuntimeState target)
        {
            if (army == null || target == null) return "원정 정보를 확인할 수 없습니다.";
            if (army.IsMoving) return "이미 이동 중인 전투단입니다.";
            if (army.AwaitingBattle) return "현재 전투 결과를 기다리는 전투단입니다.";
            if (army.ReorganizationMonths > 0) return $"재편성 완료까지 {army.ReorganizationMonths}개월 남았습니다.";
            if (target.FactionId != army.FactionId)
            {
                if (WIAdministrationTurnSystem.AreFactionsAtWar(state, army.FactionId, target.FactionId) == false)
                    return "교전 중인 진영의 성에만 원정할 수 있습니다. 먼저 외교 관계를 확인하십시오.";
                WIFactionRuntimeState faction = state.GetFactionState(army.FactionId);
                if (faction == null || faction.Influence < 20)
                    return "원정에 필요한 영향력 20이 부족합니다.";
            }
            return "현재 경로로 원정할 수 없습니다. 인접 성과 전투단 상태를 확인하십시오.";
        }

        // 선택한 성에 주둔한 전투단 중 원정할 전투단을 선택합니다.
        private void OpenCastleMarchModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("원정 전투단 선택");
            bool found = false;
            foreach (WIArmyState army in state.Armies)
            {
                if (army.CurrentCastleId != selectedCastle.CastleId || army.IsMoving || army.AwaitingBattle)
                {
                    continue;
                }

                found = true;
                Button button = new Button(() => OpenArmyTargetModal(army));
                button.text = $"{army.DisplayName} · {army.Members.Count}명 · {army.Proficiency}";
                panel.Add(button);
            }

            if (found == false)
            {
                panel.Add(new Label("주둔 영웅은 먼저 전투단로 편성해야 원정할 수 있습니다."));
                Button createArmy = new Button(() => OpenArmyCommanderModal(selectedCastle));
                createArmy.text = "이 성의 영웅으로 새 전투단 편성";
                createArmy.tooltip = "대장을 선택한 뒤 전투단원을 추가하고 이동 / 원정을 선택합니다.";
                panel.Add(createArmy);
            }
        }

        // 전투단 역할의 한국어 표시명을 반환합니다.
        private string GetUnitRoleDisplayName(WIUnitRole role)
        {
            string[] names = { "대장", "전위", "근접", "원거리", "마법", "지원" };
            return names[(int)role];
        }

    }
}
