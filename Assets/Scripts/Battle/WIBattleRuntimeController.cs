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
        // 지형 구역·배치 구역·스킬 범위 미리보기에 쓰는 흰색 원 프리팹입니다.
        [SerializeField] private SpriteRenderer zoneMarkerPrefab;
        // 배치 구역처럼 사각형 영역 표시에 쓰는 흰색 사각형 프리팹입니다.
        [SerializeField] private SpriteRenderer rectZoneMarkerPrefab;

        private WIBattleRuntimeState runtime;
        private readonly List<WIBattleCharacterView> views = new List<WIBattleCharacterView>();
        private readonly Dictionary<int, SpriteRenderer> visualEffects = new Dictionary<int, SpriteRenderer>();
        // 검격·투사체·실제 피격 파티클의 수명과 재사용을 담당합니다.
        private WIBattleEffectPresenter effectPresenter;
        private WIBattleSpriteRendererPool visualEffectPool;

        private string selectedHeroId;
        // HUD에서 선택한 플레이어 분대 번호 목록입니다.
        private readonly HashSet<int> selectedSquadIds = new HashSet<int>();
        // 스킬 위치 지정 중 표시하는 시전 거리·효과 범위 원입니다.
        private SpriteRenderer skillRangePreview;
        private SpriteRenderer skillAreaPreview;
        // 배치 단계 동안 표시하는 플레이어 배치 가능 구역입니다.
        private SpriteRenderer deploymentZoneMarker;

        // 결정적 순간에 전투를 아주 잠깐 멈추는 히트 스톱 남은 시간입니다.
        private float hitStopRemaining;
        // 명령 지점에 퍼지는 원 표시와 각 원의 경과 시간·색입니다.
        private readonly List<SpriteRenderer> orderPings = new List<SpriteRenderer>();
        private readonly List<float> orderPingElapsed = new List<float>();
        private readonly List<Color> orderPingColors = new List<Color>();
        private int nextOrderPing;
        private const int OrderPingPoolSize = 6;

        // 전술 일시정지 중이면 시뮬레이션을 멈추고 표시만 갱신합니다.
        public bool IsPaused { get; set; }
        // 현재 캠페인의 후발 전투단 합류 예약과 캐릭터 데이터 원본입니다.
        private WIBattleReinforcementSystem reinforcements;
        private WIAdministrationDatabaseSO battleDatabase;

        // HUD 문자열 UID 조회에 사용하는 전투 데이터베이스입니다.
        public WIAdministrationDatabaseSO Database => battleDatabase;
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
            runtime.IsDeploying = config.UseDeploymentPhase;
            CreateZoneMarkers();
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
            effectPresenter?.Dispose();
            if (cameraController != null)
            {
                cameraController.ZoomLevelChanged -= HandleZoomLevelChanged;
            }
        }

        // 프레임 시간만큼 표적 탐색, 이동, 공격과 승패 판정을 진행합니다.
        public void SimulateStep(float deltaTime)
        {
            RefreshOrderPings(Time.unscaledDeltaTime);
            if (runtime == null || runtime.Finished == true)
            {
                if (runtime != null)
                {
                    RefreshViews(deltaTime);
                    effectPresenter?.Refresh(runtime, deltaTime);
                }
                return;
            }
            if (IsPaused == true)
            {
                RefreshViews(0f);
                return;
            }
            if (hitStopRemaining > 0f)
            {
                hitStopRemaining -= Time.unscaledDeltaTime;
                RefreshViews(0f);
                return;
            }
            WIBattleOutcome outcome = WIBattleSimulation.Advance(config, runtime, deltaTime);
            reinforcements?.Advance(config, runtime);
            ConsumeFeedbackEvents();
            bool spawnedDuringBattle = views.Count > 0;
            while (views.Count < runtime.Characters.Count)
            {
                WIBattleCharacterState character = runtime.Characters[views.Count];
                WIBattleCharacterView view = Instantiate(characterPrefab, characterRoot == null ? transform : characterRoot);
                view.Bind(character, config, battleDatabase.GetHero(character.HeroId)?.BattleSprite);
                view.SetFarOutlineEnabled(cameraController == null || cameraController.CurrentZoomLevel == WIBattleZoomLevel.C);
                if (spawnedDuringBattle == true)
                {
                    view.PlaySpawn();
                }
                views.Add(view);
            }
            RefreshViews(deltaTime);
            RefreshVisualEffects();
            effectPresenter?.Refresh(runtime, deltaTime);
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
            WIBattleSimulation.SetSideCommand(runtime, side, command, focusHeroId);
        }

        // 선택한 분대에만 전투단 명령을 내립니다.
        public void SetSquadCommand(ICollection<int> squadIds, WIBattleCommand command, string focusHeroId = "")
        {
            if (runtime == null || runtime.Finished == true)
            {
                return;
            }
            WIBattleSimulation.SetSquadCommand(runtime, squadIds, command, focusHeroId);
        }

        // 선택한 분대를 지정한 전장 좌표로 이동시킵니다.
        public void OrderSquadsMove(ICollection<int> squadIds, Vector2 destination)
        {
            if (runtime == null || runtime.Finished == true)
            {
                return;
            }
            WIBattleSimulation.OrderSquadsMove(config, runtime, squadIds, destination);
        }

        // 선택한 분대가 지정한 적을 집중 공격하게 합니다.
        public void OrderSquadsAttack(ICollection<int> squadIds, string enemyHeroId)
        {
            if (runtime == null || runtime.Finished == true)
            {
                return;
            }
            WIBattleSimulation.OrderSquadsAttack(runtime, squadIds, enemyHeroId);
        }

        // 선택 표시할 분대 번호 목록을 교체합니다.
        public void SetSelectedSquads(IEnumerable<int> squadIds)
        {
            selectedSquadIds.Clear();
            foreach (int squadId in squadIds)
            {
                selectedSquadIds.Add(squadId);
            }
            if (runtime != null)
            {
                RefreshViews(0f);
            }
        }

        // 전장 좌표에서 가장 가까운 생존 인물을 선택 상태 변경 없이 반환합니다.
        public WIBattleCharacterState FindCharacterAt(Vector2 worldPosition)
        {
            return runtime?.Characters
                .Where(item => item.IsAlive && Vector2.Distance(item.Position, worldPosition) <= config.CharacterSelectionRadius)
                .OrderBy(item => Vector2.SqrMagnitude(item.Position - worldPosition))
                .FirstOrDefault();
        }

        // 선택한 영웅의 ScriptableObject 액티브 스킬 사용을 시도합니다.
        public bool TryActivateHeroSkill(string heroId, Vector2? targetPoint = null)
        {
            return WIBattleSimulation.TryActivateHeroSkill(config, runtime, heroId, targetPoint);
        }

        // 스킬 위치 지정 중 시전 가능 거리와 효과 범위를 구역 표시 프리팹 원으로 미리 보여 줍니다.
        public void ShowSkillPreview(Vector2 casterPosition, float castRange, Vector2 center, float radius)
        {
            if (skillRangePreview == null || skillAreaPreview == null)
            {
                return;
            }
            skillRangePreview.gameObject.SetActive(true);
            skillAreaPreview.gameObject.SetActive(true);
            skillRangePreview.color = config.SkillRangePreviewColor;
            skillRangePreview.transform.position = casterPosition;
            skillRangePreview.transform.localScale = Vector3.one * castRange * 2f;
            skillAreaPreview.color = config.SkillAreaPreviewColor;
            skillAreaPreview.transform.position = WIBattleSimulation.ClampSkillTarget(casterPosition, center, castRange);
            skillAreaPreview.transform.localScale = Vector3.one * radius * 2f;
        }

        // 스킬 범위 미리보기를 숨깁니다.
        public void HideSkillPreview()
        {
            skillRangePreview?.gameObject.SetActive(false);
            skillAreaPreview?.gameObject.SetActive(false);
        }

        // 전투 설정의 지형 구역, 배치 구역과 스킬 미리보기 원을 구역 표시 프리팹으로 생성합니다.
        private void CreateZoneMarkers()
        {
            if (zoneMarkerPrefab == null)
            {
                Debug.LogError("구역 표시 프리팹(zoneMarkerPrefab)이 BattleRuntime에 연결되지 않았습니다.", this);
                return;
            }
            Transform parent = visualEffectRoot == null ? transform : visualEffectRoot;
            foreach (WIBattleTerrainZoneDefinition zone in config.TerrainZones)
            {
                SpriteRenderer marker = Instantiate(zoneMarkerPrefab, parent);
                marker.name = "TerrainZone_" + zone.TerrainType;
                marker.color = config.GetTerrainZoneColor(zone.TerrainType);
                marker.transform.position = zone.Center;
                marker.transform.localScale = Vector3.one * zone.Radius * 2f;
            }
            skillRangePreview = Instantiate(zoneMarkerPrefab, parent);
            skillRangePreview.name = "SkillRangePreview";
            skillRangePreview.sortingOrder = 1600;
            skillAreaPreview = Instantiate(zoneMarkerPrefab, parent);
            skillAreaPreview.name = "SkillAreaPreview";
            skillAreaPreview.sortingOrder = 1601;
            HideSkillPreview();
            if (rectZoneMarkerPrefab == null)
            {
                Debug.LogError("사각형 구역 표시 프리팹(rectZoneMarkerPrefab)이 BattleRuntime에 연결되지 않았습니다.", this);
                return;
            }
            deploymentZoneMarker = Instantiate(rectZoneMarkerPrefab, parent);
            deploymentZoneMarker.name = "DeploymentZone";
            deploymentZoneMarker.color = config.DeploymentZoneColor;
            deploymentZoneMarker.gameObject.SetActive(false);
        }

        // 시뮬레이션이 남긴 연출 사건을 화면 흔들림·히트 스톱으로 바꾸고 목록을 비웁니다.
        private void ConsumeFeedbackEvents()
        {
            foreach (WIBattleFeedbackEvent feedback in runtime.FeedbackEvents)
            {
                cameraController?.AddTrauma(config.GetShakeTrauma(feedback.Type));
                hitStopRemaining = Mathf.Max(hitStopRemaining, config.GetHitStop(feedback.Type));
            }
            runtime.FeedbackEvents.Clear();
        }

        // 이동·공격 명령 지점에 퍼지며 사라지는 원을 표시합니다.
        public void ShowOrderPing(Vector2 position, bool attack)
        {
            if (orderPings.Count == 0 && zoneMarkerPrefab != null)
            {
                Transform parent = visualEffectRoot == null ? transform : visualEffectRoot;
                for (int index = 0; index < OrderPingPoolSize; index += 1)
                {
                    SpriteRenderer ping = Instantiate(zoneMarkerPrefab, parent);
                    ping.name = "OrderPing";
                    ping.sortingOrder = 1590;
                    ping.gameObject.SetActive(false);
                    orderPings.Add(ping);
                    orderPingElapsed.Add(float.MaxValue);
                    orderPingColors.Add(Color.white);
                }
            }
            if (orderPings.Count == 0)
            {
                return;
            }
            int slot = nextOrderPing;
            nextOrderPing = (nextOrderPing + 1) % orderPings.Count;
            orderPings[slot].transform.position = position;
            orderPings[slot].gameObject.SetActive(true);
            orderPingElapsed[slot] = 0f;
            orderPingColors[slot] = attack == true ? config.OrderPingAttackColor : config.OrderPingMoveColor;
        }

        // 명령 지점 원을 넓히며 흐리게 하고 끝나면 숨깁니다.
        private void RefreshOrderPings(float deltaTime)
        {
            for (int index = 0; index < orderPings.Count; index += 1)
            {
                if (orderPingElapsed[index] == float.MaxValue)
                {
                    continue;
                }
                orderPingElapsed[index] += deltaTime;
                float progress = orderPingElapsed[index] / config.OrderPingDuration;
                if (progress >= 1f)
                {
                    orderPingElapsed[index] = float.MaxValue;
                    orderPings[index].gameObject.SetActive(false);
                    continue;
                }
                float eased = 1f - (1f - progress) * (1f - progress);
                orderPings[index].transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.4f, eased);
                Color color = orderPingColors[index];
                color.a *= 1f - progress;
                orderPings[index].color = color;
            }
        }

        // 플레이어 진영의 배치 구역 표시를 켜거나 끕니다.
        public void ShowDeploymentZone(WIBattleSide side, bool visible)
        {
            if (deploymentZoneMarker == null)
            {
                return;
            }
            deploymentZoneMarker.gameObject.SetActive(visible);
            if (visible == false)
            {
                return;
            }
            Vector2 rangeX = WIBattleSimulation.GetDeploymentRangeX(config, side);
            deploymentZoneMarker.transform.position = new Vector2((rangeX.x + rangeX.y) * 0.5f, 0f);
            deploymentZoneMarker.transform.localScale = new Vector3(rangeX.y - rangeX.x, config.ArenaSize.y, 1f);
        }

        // 배치 단계인지 여부입니다.
        public bool IsDeploying => runtime != null && runtime.IsDeploying == true;

        // 배치 단계에서 선택 분대를 배치 구역 안으로 옮깁니다.
        public void DeploySquads(ICollection<int> squadIds, Vector2 destination)
        {
            if (IsDeploying == false)
            {
                return;
            }
            WIBattleSimulation.DeploySquads(config, runtime, squadIds, destination);
            RefreshViews(0f);
        }

        // 배치 단계를 끝내고 전투를 시작합니다.
        public void StartBattle()
        {
            if (runtime == null)
            {
                return;
            }
            runtime.IsDeploying = false;
            ShowDeploymentZone(WIBattleSide.Attacker, false);
        }

        // 직접 지휘 중인 영웅 ID이며 없으면 빈 문자열입니다.
        public string ControlledHeroId => runtime?.ControlledHeroId ?? string.Empty;

        // 분대장을 직접 지휘하기 시작합니다.
        public bool StartHeroControl(int squadId)
        {
            return runtime != null && runtime.Finished == false && WIBattleSimulation.StartHeroControl(runtime, squadId);
        }

        // 직접 지휘 중인 영웅을 지정 위치로 이동시킵니다.
        public void OrderControlledHeroMove(Vector2 destination)
        {
            if (runtime != null)
            {
                WIBattleSimulation.OrderControlledHeroMove(config, runtime, destination);
            }
        }

        // 직접 지휘를 끝냅니다.
        public void StopHeroControl()
        {
            if (runtime != null)
            {
                WIBattleSimulation.StopHeroControl(runtime);
            }
        }

        // 매 프레임 전투 시뮬레이션을 진행합니다.
        private void Update()
        {
            SimulateStep(Time.deltaTime);
        }

        // 런타임 위치와 생존 상태를 캐릭터 표시 오브젝트에 반영합니다.
        private void RefreshViews(float deltaTime)
        {
            foreach (WIBattleCharacterView view in views)
            {
                view.Refresh(runtime.InterpolationAlpha, deltaTime);
                bool focused = runtime.AttackerFocusHeroId == view.HeroId || runtime.DefenderFocusHeroId == view.HeroId;
                view.SetFocused(focused);
                view.SetSelected(selectedHeroId == view.HeroId || selectedSquadIds.Contains(view.SquadId) == true);
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
            RefreshViews(0f);
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
            arenaPrefabInstance.transform.localScale = Vector3.one * config.ArenaPrefabScale;
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

            effectPresenter?.Dispose();
            effectPresenter = new WIBattleEffectPresenter(config, effectParent);
        }

        // 기존 스킬의 범위 안내 표시를 갱신하며 공격 파티클은 별도 표시기로 전달합니다.
        private void RefreshVisualEffects()
        {
            foreach (WIBattleVisualEffectState effect in runtime.VisualEffects)
            {
                if (effect.EffectType == WIBattleVisualEffectType.MeleeHit || effect.EffectType == WIBattleVisualEffectType.Hit)
                {
                    continue;
                }
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

    }
}
