using System.Collections.Generic;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Battle
{
    // 전투 상태를 사전 제작 파티클 프리팹에 바인딩하고 비활성 인스턴스를 재사용합니다.
    public sealed class WIBattleEffectPresenter
    {
        // 풀에 보관하는 프리팹 인스턴스와 미리 조회한 파티클 참조입니다.
        private sealed class WIInstance
        {
            public GameObject Object;
            public ParticleSystem Root;
            public ParticleSystemRenderer[] Renderers;
            public int Kind;
            public float Age;
            public float Duration;
        }

        // 표시 설정, 부모, 종류별 비활성 풀과 활성 표시 목록입니다.
        private readonly WIBattleConfigSO config;
        private readonly Transform parent;
        private readonly GameObject[] prefabs;
        private readonly Stack<WIInstance>[] pools = new Stack<WIInstance>[4];
        private readonly List<WIInstance> bursts = new List<WIInstance>();
        private readonly Dictionary<int, WIInstance> projectiles = new Dictionary<int, WIInstance>();
        private readonly HashSet<int> currentProjectiles = new HashSet<int>();
        private readonly List<int> removals = new List<int>();
        // 재생한 단발 표시를 프레임마다 다시 생성하지 않도록 마지막 식별자를 저장합니다.
        private int lastEffectId;

        // 네 종류 프리팹을 검증하고 누락 시 대체 생성 없이 오류를 알립니다.
        public WIBattleEffectPresenter(WIBattleConfigSO settings, Transform root)
        {
            config = settings;
            parent = root;
            prefabs = new[] { config.SlashEffectPrefab, config.ArrowEffectPrefab, config.MagicEffectPrefab, config.HitEffectPrefab };
            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = new Stack<WIInstance>();
                if (prefabs[i] == null || prefabs[i].GetComponent<ParticleSystem>() == null)
                {
                    Debug.LogError("BattleConfig의 검격/화살/마법/피격 프리팹 연결을 확인하세요. 종류 번호: " + i, config);
                    prefabs[i] = null;
                }
            }
        }

        // 단발 효과를 진행하고 실제 투사체 위치 및 방향에 외형을 맞춥니다.
        public void Refresh(WIBattleRuntimeState runtime, float deltaTime)
        {
            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                WIInstance instance = bursts[i];
                instance.Age += Mathf.Max(0f, deltaTime);
                if (instance.Age >= instance.Duration)
                {
                    Release(instance);
                    bursts.RemoveAt(i);
                    continue;
                }
                Sample(instance);
            }
            foreach (WIBattleVisualEffectState effect in runtime.VisualEffects)
            {
                if (effect.EffectId <= lastEffectId)
                {
                    continue;
                }
                lastEffectId = effect.EffectId;
                if (effect.EffectType != WIBattleVisualEffectType.MeleeHit && effect.EffectType != WIBattleVisualEffectType.Hit)
                {
                    continue;
                }
                float age = Mathf.Max(0.001f, runtime.ElapsedSeconds - effect.StartedAt);
                if (age >= effect.Duration)
                {
                    continue;
                }
                bool slash = effect.EffectType == WIBattleVisualEffectType.MeleeHit;
                WIInstance instance = Acquire(slash == true ? 0 : 3);
                if (instance == null)
                {
                    continue;
                }
                instance.Age = age;
                instance.Duration = effect.Duration;
                Vector2 direction = effect.EndPosition - effect.StartPosition;
                float scale = slash == true ? config.SlashEffectScale : config.HitEffectScale;
                Vector2 position = effect.EndPosition;
                if (slash == true)
                {
                    position -= direction.normalized * (0.65f * scale);
                }
                Place(instance, position, direction, scale);
                Sample(instance);
                bursts.Add(instance);
            }
            currentProjectiles.Clear();
            if (runtime.Finished == false)
            {
                foreach (WIBattleProjectileState projectile in runtime.Projectiles)
                {
                    currentProjectiles.Add(projectile.ProjectileId);
                    if (projectiles.TryGetValue(projectile.ProjectileId, out WIInstance instance) == false)
                    {
                        bool magic = projectile.ShooterRole == WIUnitRole.Magic || projectile.ShooterRole == WIUnitRole.Support;
                        instance = Acquire(magic == true ? 2 : 1);
                        if (instance == null)
                        {
                            continue;
                        }
                        // 비행 중 외형은 소멸시키지 않고 명중/만료 상태가 제거될 때 회수합니다.
                        instance.Root.Simulate(0.08f, true, true, false);
                        projectiles.Add(projectile.ProjectileId, instance);
                    }
                    Vector2 position = projectile.HasPreviousPosition == true
                        ? Vector2.Lerp(projectile.PreviousPosition, projectile.Position, runtime.InterpolationAlpha)
                        : projectile.Position;
                    Place(instance, position, projectile.TargetPosition - position, config.ProjectileEffectScale);
                }
            }
            removals.Clear();
            foreach (KeyValuePair<int, WIInstance> pair in projectiles)
            {
                if (currentProjectiles.Contains(pair.Key) == false)
                {
                    Release(pair.Value);
                    removals.Add(pair.Key);
                }
            }
            foreach (int id in removals)
            {
                projectiles.Remove(id);
            }
        }

        // 미리 제작된 프리팹을 복제하고 투사체의 자체 이동을 제거해 이중 이동을 방지합니다.
        private WIInstance Acquire(int kind)
        {
            if (prefabs[kind] == null)
            {
                return null;
            }
            WIInstance instance;
            if (pools[kind].Count > 0)
            {
                instance = pools[kind].Pop();
            }
            else
            {
                GameObject effect = Object.Instantiate(prefabs[kind], parent);
                instance = new WIInstance
                {
                    Object = effect,
                    Root = effect.GetComponent<ParticleSystem>(),
                    Renderers = effect.GetComponentsInChildren<ParticleSystemRenderer>(),
                    Kind = kind
                };
                foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = particle.main;
                    main.playOnAwake = false;
                    main.stopAction = ParticleSystemStopAction.None;
                    if (kind == 1 || kind == 2)
                    {
                        var velocity = particle.velocityOverLifetime;
                        velocity.enabled = false;
                    }
                }
            }
            instance.Object.SetActive(true);
            instance.Root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return instance;
        }

        // XY 방향과 캐릭터 깊이 순서에 맞춰 효과의 위치·회전·크기를 반영합니다.
        private void Place(WIInstance instance, Vector2 position, Vector2 direction, float scale)
        {
            instance.Object.transform.position = position + Vector2.up * config.EffectHeight;
            instance.Object.transform.localScale = Vector3.one * scale;
            instance.Object.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            int order = 1000 - Mathf.RoundToInt(position.y * 100f) + 5;
            for (int i = 0; i < instance.Renderers.Length; i++)
            {
                instance.Renderers[i].sortingOrder = order + i;
            }
        }

        // 전투 시간 진행에 맞춰 단발 파티클을 샘플링하여 일시정지와 배속을 일치시킵니다.
        private static void Sample(WIInstance instance)
        {
            float authoredDuration = instance.Kind == 0 ? 0.285f : 0.32f;
            instance.Root.Simulate(instance.Age / instance.Duration * authoredDuration, true, true, false);
        }

        // 잔여 파티클을 지운 후 다음 공격에서 같은 인스턴스를 재사용합니다.
        private void Release(WIInstance instance)
        {
            instance.Root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instance.Object.SetActive(false);
            pools[instance.Kind].Push(instance);
        }

        // 전투 재초기화와 씬 종료 시 활성 및 비활성 표시 오브젝트를 정리합니다.
        public void Dispose()
        {
            foreach (WIInstance instance in bursts)
            {
                Release(instance);
            }
            bursts.Clear();
            foreach (WIInstance instance in projectiles.Values)
            {
                Release(instance);
            }
            projectiles.Clear();
            foreach (Stack<WIInstance> pool in pools)
            {
                while (pool.Count > 0)
                {
                    GameObject instance = pool.Pop().Object;
                    if (instance != null)
                    {
                        if (Application.isPlaying == true)
                        {
                            Object.Destroy(instance);
                        }
                        else
                        {
                            Object.DestroyImmediate(instance);
                        }
                    }
                }
            }
        }
    }
}
