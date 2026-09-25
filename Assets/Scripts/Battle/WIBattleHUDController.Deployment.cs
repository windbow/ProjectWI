using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWI.Battle
{
    public partial class WIBattleHUDController
    {
        // UXML에 미리 만든 배치 단계 전투 시작 버튼입니다.
        private Button deployStartButton;
        // 배치 단계 표시를 적용했는지 여부입니다.
        private bool deploymentShown;

        // 직접 지휘 중인 영웅이 있는지 여부입니다.
        private bool IsControllingHero => string.IsNullOrEmpty(battleController?.ControlledHeroId) == false;

        // 전투 시작 버튼을 찾아 연결합니다.
        private void BindDeploymentInput(VisualElement root)
        {
            deployStartButton = root.Q<Button>("deploy-start");
            if (deployStartButton == null)
            {
                Debug.LogError("전투 HUD UXML에 deploy-start 버튼이 없습니다.", this);
                return;
            }
            deployStartButton.clicked += StartBattleFromDeployment;
        }

        // 배치 단계 동안 시작 버튼·배치 구역·안내를 표시하고 끝나면 숨깁니다.
        private void RefreshDeploymentState()
        {
            bool deploying = battleController.IsDeploying;
            if (deployStartButton != null)
            {
                deployStartButton.style.display = deploying == true ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (deploying == true && deploymentShown == false)
            {
                deploymentShown = true;
                if (deployStartButton != null)
                {
                    deployStartButton.text = Text("UI_BATTLE_DEPLOY_START");
                }
                battleController.ShowDeploymentZone(playerSide, true);
                commandFeedbackLabel.text = Text("UI_BATTLE_DEPLOY_HINT");
            }
        }

        // 배치 단계를 끝내고 전투를 시작합니다.
        private void StartBattleFromDeployment()
        {
            if (battleController == null || battleController.IsDeploying == false)
            {
                return;
            }
            battleController.StartBattle();
            commandFeedbackLabel.text = Text("UI_BATTLE_DEPLOY_STARTED");
        }

        // 선택한 첫 분대의 분대장을 직접 지휘하거나, 이미 지휘 중이면 해제합니다.
        private void ToggleHeroControl()
        {
            if (battleController == null || battleController.Runtime == null || battleController.IsDeploying == true)
            {
                return;
            }
            if (IsControllingHero == true)
            {
                battleController.StopHeroControl();
                commandFeedbackLabel.text = Text("UI_BATTLE_CONTROL_OFF");
                return;
            }
            if (HasSquadSelection == false)
            {
                commandFeedbackLabel.text = Text("UI_BATTLE_CONTROL_NEED_SQUAD");
                return;
            }
            int squadId = selectedSquadIds.Min();
            if (battleController.StartHeroControl(squadId) == false)
            {
                return;
            }
            selectedSquadIds.Clear();
            selectedSquadIds.Add(squadId);
            ApplySquadSelection();
            WIBattleCharacterState hero = battleController.Runtime.Characters.Find(item => item.HeroId == battleController.ControlledHeroId);
            commandFeedbackLabel.text = Text("UI_BATTLE_CONTROL_ON", hero?.DisplayName);
        }

        // 직접 지휘 중인 영웅의 스킬 버튼을 누른 것과 같이 처리합니다.
        private void CastControlledHeroSkill()
        {
            if (IsControllingHero == true)
            {
                HandleSkillButton(battleController.ControlledHeroId);
            }
        }

        // 좌표가 속한 지형 이름을 반환합니다.
        private string GetTerrainName(Vector2 position)
        {
            if (WIBattleSimulation.TryGetTerrainAt(battleController.Config, position, out WIBattleTerrainType terrainType) == false)
            {
                return Text("UI_BATTLE_TERRAIN_NONE");
            }
            if (terrainType == WIBattleTerrainType.HighGround) return Text("UI_BATTLE_TERRAIN_HIGH_GROUND");
            if (terrainType == WIBattleTerrainType.Forest) return Text("UI_BATTLE_TERRAIN_FOREST");
            return Text("UI_BATTLE_TERRAIN_NARROW");
        }
    }
}
