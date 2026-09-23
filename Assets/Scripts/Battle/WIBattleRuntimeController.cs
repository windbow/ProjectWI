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
        [SerializeField] private Transform arenaRoot;
        [SerializeField] private Transform visualEffectRoot;
        [SerializeField] private Transform projectileRoot;
        [SerializeField] private GameObject visualEffectPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private WIBattleCameraController cameraController;

        private WIBattleRuntimeState runtime;
        private readonly List<WIBattleCharacterView> views = new List<WIBattleCharacterView>();
        private readonly Dictionary<int, SpriteRenderer> visualEffects = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, SpriteRenderer> projectileViews = new Dictionary<int, SpriteRenderer>();
        private WIBattleSpriteRendererPool visualEffectPool;
        private WIBattleSpriteRendererPool projectilePool;
        private string selectedHeroId;
        // 현재 캠페인의 후발 전투단 합류 예약과 캐릭터 데이터 원본입니다.
        private WIBattleReinforcementSystem reinforcements;
        private WIAdministrationDatabaseSO battleDatabase;
        public string ReinforcementStatus => reinforcements?.GetStatus(runtime?.ElapsedSeconds ?? 0f) ?? string.Empty;

        public WIBattleRuntimeState Runtime => runtime;
        public WIBattleConfigSO Config => config;
        public WIBattleCameraController CameraController => cameraController;
        public event Action<WIBattleOutcome> BattleFinished;

        // 전달받은 전투 세션으로 런타임 상태와 교체 가능한 캐릭터 표시 오브젝트를 생성합니다.
        public void Initialize(
            WIAdministrationDatabaseSO database,
            WIBattleSessionState session, WIAdministrationState campaignState = null)
        {
            PrepareBattlePresentation();
            PreparePresentationPools();
            runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            battleDatabase = database;
            reinforcements = campaignState == null ? null : new WIBattleReinforcementSystem(config, database, campaignState, session);
            if (cameraController != null)
            {
                cameraController.ZoomLevelChanged -= HandleZoomLevelChanged;
                cameraController.ZoomLevelChanged += HandleZoomLevelChanged;
            }
            cameraController?.FrameCombatants(runtime.Characters);
            foreach (WIBattleCharacterState character in runtime.Characters)
            {
                WIBattleCharacterView view = Instantiate(characterPrefab, characterRoot == null ? transform : characterRoot);
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                view.Bind(character, config, hero?.BattleSprite);
                views.Add(view);
            }
            HandleZoomLevelChanged(cameraController == null ? WIBattleZoomLevel.C : cameraController.CurrentZoomLevel);
        }

        // 카메라 C 단계에서만 모든 캐릭터의 소프트 외곽선을 활성화합니다.
        private void HandleZoomLevelChanged(WIBattleZoomLevel zoomLevel)
        {
            bool enableFarOutline = zoomLevel == WIBattleZoomLevel.C;
            foreach (WIBattleCharacterView view in views)
            {
                view.SetFarOutlineEnabled(enableFarOutline);
            }
        }

        // 카메라 단계 변경 이벤트 연결을 해제합니다.
        private void OnDestroy()
        {
            if (cameraController != null)
            {
                cameraController.ZoomLevelChanged -= HandleZoomLevelChanged;
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
            reinforcements?.Advance(config, runtime);
            while (views.Count < runtime.Characters.Count)
            {
                WIBattleCharacterState character = runtime.Characters[views.Count];
                WIBattleCharacterView view = Instantiate(characterPrefab, characterRoot == null ? transform : characterRoot);
                view.Bind(character, config, battleDatabase.GetHero(character.HeroId)?.BattleSprite);
                view.SetFarOutlineEnabled(cameraController == null || cameraController.CurrentZoomLevel == WIBattleZoomLevel.C);
                views.Add(view);
            }
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

        // 씬에 연결된 카메라와 전장 프리팹을 사용해 전투 표시 환경을 준비합니다.
        private void PrepareBattlePresentation()
        {
            if (cameraController == null)
            {
                Debug.LogError("전투 카메라 컨트롤러가 BattleRuntime에 연결되지 않았습니다.", this);
                return;
            }
            cameraController.Initialize(config);

            if (config.ArenaPrefab == null)
            {
                Debug.LogError("전투 전장 프리팹이 BattleConfig에 연결되지 않았습니다.", config);
                return;
            }

            Transform parent = arenaRoot == null ? transform : arenaRoot;
            GameObject arenaPrefabInstance = Instantiate(config.ArenaPrefab, parent);
            arenaPrefabInstance.name = config.ArenaPrefab.name;
        }

        // 전투 효과와 투사체 프리팹을 재사용할 표시 오브젝트 풀을 준비합니다.
        private void PreparePresentationPools()
        {
            Transform effectParent = visualEffectRoot == null ? transform : visualEffectRoot;
            visualEffectPool = new WIBattleSpriteRendererPool(visualEffectPrefab, effectParent);
            if (visualEffectPool.IsValid == false)
            {
                Debug.LogError("전투 효과 프리팹과 SpriteRenderer 연결을 확인해야 합니다.", this);
                visualEffectPool = null;
            }

            Transform projectileParent = projectileRoot == null ? transform : projectileRoot;
            projectilePool = new WIBattleSpriteRendererPool(projectilePrefab, projectileParent);
            if (projectilePool.IsValid == false)
            {
                Debug.LogError("전투 투사체 프리팹과 SpriteRenderer 연결을 확인해야 합니다.", this);
                projectilePool = null;
            }
        }

        // 시뮬레이션이 예약한 근접 섬광과 원거리 발사체 도형을 생성하고 이동시킵니다.
        private void RefreshVisualEffects()
        {
            foreach (WIBattleVisualEffectState effect in runtime.VisualEffects)
            {
                if (visualEffects.ContainsKey(effect.EffectId) == false)
                {
                    if (visualEffectPool == null)
                    {
                        return;
                    }
                    SpriteRenderer renderer = visualEffectPool.Acquire();
                    renderer.name = effect.EffectType.ToString();
                    renderer.color = GetVisualEffectColor(effect);
                    renderer.sortingOrder = 20;
                    float size = effect.Radius > 0f ? effect.Radius * 2f : effect.EffectType == WIBattleVisualEffectType.Projectile
                        ? config.PlaceholderProjectileSize
                        : config.PlaceholderCharacterSize * 0.55f;
                    renderer.transform.localScale = Vector3.one * size;
                    visualEffects[effect.EffectId] = renderer;
                }
                float progress = effect.Duration <= 0f ? 1f : Mathf.Clamp01((runtime.ElapsedSeconds - effect.StartedAt) / effect.Duration);
                visualEffects[effect.EffectId].transform.position = effect.EffectType == WIBattleVisualEffectType.Projectile
                    ? Vector2.Lerp(effect.StartPosition, effect.EndPosition, progress)
                    : effect.EndPosition;
            }
            foreach (int effectId in visualEffects.Keys.Where(id => runtime.VisualEffects.All(item => item.EffectId != id)).ToList())
            {
                visualEffectPool?.Release(visualEffects[effectId]);
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
                    if (projectilePool == null)
                    {
                        return;
                    }
                    SpriteRenderer renderer = projectilePool.Acquire();
                    renderer.name = "Projectile";
                    renderer.color = projectile.Side == WIBattleSide.Attacker
                        ? config.AttackerPlaceholderColor
                        : config.DefenderPlaceholderColor;
                    renderer.sortingOrder = 20;
                    renderer.transform.localScale = Vector3.one * config.PlaceholderProjectileSize;
                    projectileViews[projectile.ProjectileId] = renderer;
                }
                projectileViews[projectile.ProjectileId].transform.position = projectile.Position;
            }
            foreach (int projectileId in projectileViews.Keys.Where(id => runtime.Projectiles.All(item => item.ProjectileId != id)).ToList())
            {
                projectilePool?.Release(projectileViews[projectileId]);
                projectileViews.Remove(projectileId);
            }
        }

    }
}
