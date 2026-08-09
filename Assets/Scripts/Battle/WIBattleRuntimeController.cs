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
        private readonly Dictionary<int, SpriteRenderer> visualEffects = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, SpriteRenderer> projectileViews = new Dictionary<int, SpriteRenderer>();
        private WIBattleCameraController cameraController;
        private string selectedHeroId;

        public WIBattleRuntimeState Runtime => runtime;
        public WIBattleConfigSO Config => config;
        public WIBattleCameraController CameraController => cameraController;
        public event Action<WIBattleOutcome> BattleFinished;

        // 전달받은 전투 세션으로 런타임 상태와 교체 가능한 캐릭터 표시 오브젝트를 생성합니다.
        public void Initialize(
            WIAdministrationDatabaseSO database,
            WIBattleSessionState session)
        {
            PreparePlaceholderArena();
            runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                WIBattleCharacterView view = Instantiate(characterPrefab, characterRoot == null ? transform : characterRoot);
                view.Bind(character, config);
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
            RefreshVisualEffects();
            RefreshProjectileViews();
            if (outcome != WIBattleOutcome.None)
            {
                BattleFinished?.Invoke(outcome);
            }
        }

        // 지정한 진영의 현재 전투단 명령과 선택 집중 목표를 변경합니다.
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
                bool focused = runtime.AttackerFocusHeroId == view.HeroId || runtime.DefenderFocusHeroId == view.HeroId;
                view.SetFocused(focused);
                view.SetSelected(selectedHeroId == view.HeroId);
            }
        }

        // 클릭한 전장 좌표에서 가장 가까운 생존 인물을 선택하고 상세 표시용 상태를 반환합니다.
        public WIBattleCharacterState SelectCharacterAt(Vector2 worldPosition)
        {
            WIBattleCharacterState selected = runtime?.Characters
                .Where(item => item.IsAlive && Vector2.Distance(item.Position, worldPosition) <= config.CharacterSelectionRadius)
                .OrderBy(item => Vector2.SqrMagnitude(item.Position - worldPosition))
                .FirstOrDefault();
            selectedHeroId = selected?.HeroId ?? string.Empty;
            RefreshViews();
            return selected;
        }

        // 전투 아트가 없어도 공간을 확인할 수 있도록 격자 전장과 2D 직교 카메라를 준비합니다.
        private void PreparePlaceholderArena()
        {
            Camera battleCamera = Camera.main;
            if (battleCamera != null)
            {
                cameraController = battleCamera.GetComponent<WIBattleCameraController>();
                if (cameraController == null) cameraController = battleCamera.gameObject.AddComponent<WIBattleCameraController>();
                cameraController.Initialize(config);
            }
            GameObject arena = new GameObject("PlaceholderArenaGrid");
            arena.transform.SetParent(transform, false);
            arena.transform.position = new Vector3(0f, 0f, 2f);
            SpriteRenderer renderer = arena.AddComponent<SpriteRenderer>();
            renderer.sprite = WIBattlePlaceholderSprites.GetArenaGrid();
            renderer.sortingOrder = -100;
        }

        // 시뮬레이션이 예약한 근접 섬광과 원거리 발사체 도형을 생성하고 이동시킵니다.
        private void RefreshVisualEffects()
        {
            foreach (WIBattleVisualEffectState effect in runtime.VisualEffects)
            {
                if (visualEffects.ContainsKey(effect.EffectId) == false)
                {
                    GameObject effectObject = new GameObject(effect.EffectType.ToString());
                    effectObject.transform.SetParent(transform, false);
                    SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = effect.EffectType == WIBattleVisualEffectType.Projectile
                        ? WIBattlePlaceholderSprites.GetSquare()
                        : WIBattlePlaceholderSprites.GetCircle();
                    renderer.color = GetVisualEffectColor(effect);
                    renderer.sortingOrder = 20;
                    float size = effect.Radius > 0f ? effect.Radius * 2f : effect.EffectType == WIBattleVisualEffectType.Projectile
                        ? config.PlaceholderProjectileSize
                        : config.PlaceholderCharacterSize * 0.55f;
                    effectObject.transform.localScale = Vector3.one * size;
                    visualEffects[effect.EffectId] = renderer;
                }
                float progress = effect.Duration <= 0f ? 1f : Mathf.Clamp01((runtime.ElapsedSeconds - effect.StartedAt) / effect.Duration);
                visualEffects[effect.EffectId].transform.position = effect.EffectType == WIBattleVisualEffectType.Projectile
                    ? Vector2.Lerp(effect.StartPosition, effect.EndPosition, progress)
                    : effect.EndPosition;
            }
            foreach (int effectId in visualEffects.Keys.Where(id => runtime.VisualEffects.All(item => item.EffectId != id)).ToList())
            {
                Destroy(visualEffects[effectId].gameObject);
                visualEffects.Remove(effectId);
            }
        }

        // 스킬 종류별 범위 표시 색상과 일반 공격 진영색을 반환합니다.
        private Color GetVisualEffectColor(WIBattleVisualEffectState effect)
        {
            if (effect.EffectType == WIBattleVisualEffectType.SkillDamage) return new Color(0.95f, 0.22f, 0.12f, 0.32f);
            if (effect.EffectType == WIBattleVisualEffectType.SkillHeal) return new Color(0.2f, 0.95f, 0.4f, 0.3f);
            if (effect.EffectType == WIBattleVisualEffectType.SkillCommand) return new Color(0.95f, 0.78f, 0.16f, 0.3f);
            return effect.Side == WIBattleSide.Attacker
                ? config.AttackerPlaceholderColor
                : config.DefenderPlaceholderColor;
        }

        // 실제 충돌 판정을 수행하는 원거리 발사체 상태를 작은 사각형으로 표시합니다.
        private void RefreshProjectileViews()
        {
            foreach (WIBattleProjectileState projectile in runtime.Projectiles)
            {
                if (projectileViews.ContainsKey(projectile.ProjectileId) == false)
                {
                    GameObject projectileObject = new GameObject("Projectile");
                    projectileObject.transform.SetParent(transform, false);
                    SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = WIBattlePlaceholderSprites.GetSquare();
                    renderer.color = projectile.Side == WIBattleSide.Attacker
                        ? config.AttackerPlaceholderColor
                        : config.DefenderPlaceholderColor;
                    renderer.sortingOrder = 20;
                    projectileObject.transform.localScale = Vector3.one * config.PlaceholderProjectileSize;
                    projectileViews[projectile.ProjectileId] = renderer;
                }
                projectileViews[projectile.ProjectileId].transform.position = projectile.Position;
            }
            foreach (int projectileId in projectileViews.Keys.Where(id => runtime.Projectiles.All(item => item.ProjectileId != id)).ToList())
            {
                Destroy(projectileViews[projectileId].gameObject);
                projectileViews.Remove(projectileId);
            }
        }

    }
}
