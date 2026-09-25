using System.Linq;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Battle;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIBattleEffectTests
    {
        // 실제 연결된 전투 설정으로 표시기를 검사합니다.
        private WIBattleConfigSO config;
        private GameObject root;
        private WIBattleEffectPresenter presenter;

        // 매 검사마다 독립 표시 부모와 연결된 프리팹을 준비합니다.
        [SetUp]
        public void SetUp()
        {
            config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            root = new GameObject("WI_EffectTestRoot");
            presenter = new WIBattleEffectPresenter(config, root.transform);
        }

        // 생성된 파티클과 검사 부모를 정리합니다.
        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
            Object.DestroyImmediate(root);
        }

        // 역할별 외형, 자체 이동 제거, 수명보다 긴 비행 표시와 만료 후 풀 재사용을 검사합니다.
        [TestCase(WIUnitRole.Ranged, "Arrow")]
        [TestCase(WIUnitRole.Magic, "Magic")]
        [TestCase(WIUnitRole.Support, "Magic")]
        public void ProjectileFollowsStateAndReusesPrefab(WIUnitRole role, string name)
        {
            var runtime = new WIBattleRuntimeState();
            var projectile = new WIBattleProjectileState { ProjectileId = 1, ShooterRole = role, Position = Vector2.zero, TargetPosition = Vector2.left * 5f };
            runtime.Projectiles.Add(projectile);
            presenter.Refresh(runtime, 0.01f);
            Transform view = root.transform.GetChild(0);
            Assert.That(view.name, Does.Contain(name));
            Assert.Less(view.right.x, -0.99f);
            Assert.IsTrue(view.GetComponentsInChildren<ParticleSystem>().Any(particle => particle.particleCount > 0));
            foreach (ParticleSystem particle in view.GetComponentsInChildren<ParticleSystem>())
            {
                Assert.IsFalse(particle.velocityOverLifetime.enabled);
            }
            projectile.Position = new Vector2(-2f, 1f);
            presenter.Refresh(runtime, 1.4f);
            Assert.Less(Vector3.Distance(view.position, new Vector3(-2f, 1f + config.EffectHeight, 0f)), 0.001f);
            Assert.IsTrue(view.GetComponentsInChildren<ParticleSystem>().Any(particle => particle.particleCount > 0));
            runtime.Projectiles.Clear();
            presenter.Refresh(runtime, 0f);
            Assert.IsFalse(view.gameObject.activeSelf);
            projectile.ProjectileId = 2;
            runtime.Projectiles.Add(projectile);
            presenter.Refresh(runtime, 0f);
            Assert.AreEqual(1, root.transform.childCount);
            Assert.IsTrue(view.gameObject.activeSelf);
            runtime.Finished = true;
            presenter.Refresh(runtime, 0f);
            Assert.IsFalse(view.gameObject.activeSelf);
        }

        // 마지막 일격도 한 번만 표시하고 전투 종료 뒤에는 남은 효과를 회수합니다.
        [Test]
        public void FinalMeleeAndHitFinishWithoutRespawning()
        {
            var runtime = new WIBattleRuntimeState { Finished = true };
            runtime.VisualEffects.Add(new WIBattleVisualEffectState { EffectId = 1, EffectType = WIBattleVisualEffectType.MeleeHit, EndPosition = Vector2.right, Duration = config.SlashEffectDuration });
            runtime.VisualEffects.Add(new WIBattleVisualEffectState { EffectId = 2, EffectType = WIBattleVisualEffectType.Hit, EndPosition = Vector2.right, Duration = config.HitEffectDuration });
            presenter.Refresh(runtime, 0f);
            Assert.AreEqual(2, root.transform.childCount);
            presenter.Refresh(runtime, 1f);
            presenter.Refresh(runtime, 1f);
            Assert.AreEqual(2, root.transform.childCount);
            foreach (Transform child in root.transform)
            {
                Assert.IsFalse(child.gameObject.activeSelf);
            }
        }

        // 투사체의 실제 충돌 위치에만 피격 이벤트가 생기고 만료는 피해를 만들지 않는지 검사합니다.
        [TestCase(true)]
        [TestCase(false)]
        public void HitEventRequiresProjectileCollision(bool collide)
        {
            var runtime = new WIBattleRuntimeState();
            runtime.Characters.Add(new WIBattleCharacterState { HeroId = "source", Side = WIBattleSide.Attacker, Position = Vector2.left * 5f, Health = 100, MaxHealth = 100, CooldownRemaining = 10f });
            var target = new WIBattleCharacterState { HeroId = "target", Side = WIBattleSide.Defender, Position = Vector2.zero, Health = 100, MaxHealth = 100, CooldownRemaining = 10f };
            runtime.Characters.Add(target);
            runtime.Projectiles.Add(new WIBattleProjectileState { ProjectileId = 1, ShooterHeroId = "source", TargetHeroId = collide == true ? "target" : "missing", Side = WIBattleSide.Attacker, Position = collide == true ? Vector2.left * 0.1f : Vector2.up * 4f, TargetPosition = Vector2.up * 4f, RemainingLifetime = collide == true ? 1f : 0f, Damage = 10 });
            WIBattleSimulation.Step(config, runtime, 0.05f);
            Assert.AreEqual(collide == true ? 90 : 100, target.Health);
            Assert.AreEqual(collide == true ? 1 : 0, runtime.VisualEffects.Count(effect => effect.EffectType == WIBattleVisualEffectType.Hit));
            Assert.AreEqual(0, runtime.Projectiles.Count);
        }
    }
}
