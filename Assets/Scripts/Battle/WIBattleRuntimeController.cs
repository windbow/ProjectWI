using System;
using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    public class WIBattleRuntimeController : MonoBehaviour
    {
        [SerializeField] private WIBattleConfigSO config;
        [SerializeField] private WIBattleCharacterView characterPrefab;
        [SerializeField] private Transform characterRoot;

        private WIBattleRuntimeState runtime;
        private readonly List<WIBattleCharacterView> views = new List<WIBattleCharacterView>();

        public WIBattleRuntimeState Runtime => runtime;
        public event Action<WIBattleOutcome> BattleFinished;

        // 전달받은 전투 세션으로 런타임 상태와 교체 가능한 캐릭터 표시 오브젝트를 생성합니다.
        public void Initialize(
            WIAdministrationDatabaseSO database,
            WIBattleSessionState session)
        {
            runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                WIBattleCharacterView view = Instantiate(characterPrefab, characterRoot == null ? transform : characterRoot);
                view.Bind(character, config.PlaceholderSprite);
                views.Add(view);
            }
        }

        // 프레임 시간만큼 표적 탐색, 이동, 공격과 승패 판정을 진행합니다.
        public void SimulateStep(float deltaTime)
        {
            if (runtime == null || runtime.Finished)
            {
                return;
            }
            WIBattleOutcome outcome = WIBattleSimulation.Step(config, runtime, deltaTime);
            RefreshViews();
            if (outcome != WIBattleOutcome.None)
            {
                BattleFinished?.Invoke(outcome);
            }
        }

        // 지정한 진영의 현재 부대 명령과 선택 집중 목표를 변경합니다.
        public void SetCommand(WIBattleSide side, WIBattleCommand command, string focusHeroId = "")
        {
            if (runtime == null || runtime.Finished)
            {
                return;
            }
            if (side == WIBattleSide.Attacker)
            {
                runtime.AttackerCommand = command;
                runtime.AttackerFocusHeroId = focusHeroId;
            }
            else
            {
                runtime.DefenderCommand = command;
                runtime.DefenderFocusHeroId = focusHeroId;
            }
        }

        // 선택한 영웅의 ScriptableObject 액티브 스킬 사용을 시도합니다.
        public bool TryActivateHeroSkill(string heroId)
        {
            return WIBattleSimulation.TryActivateHeroSkill(config, runtime, heroId);
        }

        // 매 프레임 전투 시뮬레이션을 진행합니다.
        private void Update()
        {
            SimulateStep(Time.deltaTime);
        }

        // 런타임 위치와 생존 상태를 캐릭터 표시 오브젝트에 반영합니다.
        private void RefreshViews()
        {
            foreach (WIBattleCharacterView view in views)
            {
                view.Refresh();
            }
        }

    }
}
