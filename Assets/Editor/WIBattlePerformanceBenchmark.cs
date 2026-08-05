using System.Diagnostics;
using ProjectWI.Administration;
using ProjectWI.Battle;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WIBattlePerformanceBenchmark
    {
        private const string BattleConfigPath = "Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset";

        [MenuItem("ProjectWI/Verification/Run Battle Performance Benchmark")]
        // 20·40·60명 전투를 각각 600스텝 실행하고 처리 시간과 잔여 상태를 콘솔에 기록합니다.
        public static void RunBenchmark()
        {
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            if (config == null)
            {
                UnityEngine.Debug.LogError($"전투 설정을 찾을 수 없습니다: {BattleConfigPath}");
                return;
            }
            foreach (int characterCount in new[] { 20, 40, 60 })
            {
                WIBattleRuntimeState runtime = CreateScenario(config, characterCount);
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int step = 0; step < 600; step += 1)
                {
                    WIBattleSimulation.Step(config, runtime, 1f / 60f);
                }
                stopwatch.Stop();
                UnityEngine.Debug.Log(
                    $"전투 성능 · {characterCount}명 · 600스텝 {stopwatch.Elapsed.TotalMilliseconds:0.00}ms · " +
                    $"발사체 {runtime.Projectiles.Count} · 효과 {runtime.VisualEffects.Count}");
            }
        }

        // 성능 측정용으로 양 진영 인원을 균등 배치한 장시간 교전 상태를 생성합니다.
        public static WIBattleRuntimeState CreateScenario(WIBattleConfigSO config, int characterCount)
        {
            WIBattleRuntimeState runtime = new WIBattleRuntimeState
            {
                AttackerCommand = WIBattleCommand.Advance,
                DefenderCommand = WIBattleCommand.Advance
            };
            int perSide = characterCount / 2;
            for (int index = 0; index < characterCount; index += 1)
            {
                bool attacker = index < perSide;
                int sideIndex = attacker ? index : index - perSide;
                WIUnitRole role = sideIndex % 4 == 0 ? WIUnitRole.Ranged : WIUnitRole.Melee;
                float x = attacker ? -4f - sideIndex % 3 * 0.7f : 4f + sideIndex % 3 * 0.7f;
                float y = (sideIndex - (perSide - 1) * 0.5f) * 0.55f;
                Vector2 position = new Vector2(x, y);
                runtime.Characters.Add(new WIBattleCharacterState
                {
                    HeroId = $"performance_{index}",
                    DisplayName = $"성능 인물 {index}",
                    Side = attacker ? WIBattleSide.Attacker : WIBattleSide.Defender,
                    Role = role,
                    Position = position,
                    FormationPosition = position,
                    MaxHealth = 100000,
                    Health = 100000,
                    MaxMana = 100,
                    Mana = 100,
                    AttackDamage = 1,
                    AttackRange = role == WIUnitRole.Ranged ? config.RangedRange : config.MeleeRange,
                    MoveSpeed = config.MoveSpeed
                });
            }
            return runtime;
        }
    }
}
