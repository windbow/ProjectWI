using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Battle;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIAdministrationAITurnTests
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const string BattleConfigPath = "Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset";

        // 캠페인 난이도 3종이 숨은 자원 보너스 없이 AI 후보 범위만 다르게 정의되는지 검증합니다.
        [Test]
        public void CampaignDifficulties_DefineDecisionQualityOnly()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(3, database.DifficultyDefinitions.Count);
            Assert.AreEqual(3, database.GetDifficulty(WICampaignDifficulty.Relaxed).AICandidateWindow);
            Assert.AreEqual(2, database.GetDifficulty(WICampaignDifficulty.Standard).AICandidateWindow);
            Assert.AreEqual(1, database.GetDifficulty(WICampaignDifficulty.Hard).AICandidateWindow);

            WIAdministrationState standard = WIAdministrationState.Create(database, WICampaignDifficulty.Standard);
            WIAdministrationState hard = WIAdministrationState.Create(database, WICampaignDifficulty.Hard);
            Assert.AreEqual(standard.Gold, hard.Gold);
            Assert.AreEqual(standard.ManaCrystal, hard.ManaCrystal);
            Assert.AreEqual(standard.Influence, hard.Influence);
        }

        // 선택한 캠페인 난이도가 저장 JSON 왕복 후에도 유지되는지 검증합니다.
        [Test]
        public void CampaignDifficulty_SaveRoundTripPreservesSelection()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState source = WIAdministrationState.Create(database, WICampaignDifficulty.Hard);
            string json = WICampaignSaveSystem.Serialize(source);

            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual(WICampaignDifficulty.Hard, loaded.Difficulty);
        }

        // 첫해 핵심 안내가 지정 월과 중복 없는 ID로 ScriptableObject에 구성됐는지 검증합니다.
        [Test]
        public void TutorialDefinitions_CoverFirstYearMilestones()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 6, 12 },
                database.TutorialDefinitions.Select(item => item.Month).ToArray());
            Assert.AreEqual(database.TutorialDefinitions.Count,
                database.TutorialDefinitions.Select(item => item.Id).Distinct().Count());
            Assert.IsTrue(database.TutorialDefinitions.All(item =>
                string.IsNullOrWhiteSpace(item.Title.Korean) == false &&
                string.IsNullOrWhiteSpace(item.Description.Korean) == false));
        }

        // 월별 안내 확인과 전체 건너뛰기가 캠페인 진행을 막지 않는지 검증합니다.
        [Test]
        public void TutorialProgress_CompletesAndSkipsWithoutBlockingTurns()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WITutorialDefinition first = WITutorialSystem.GetPending(database, state);
            Assert.AreEqual("tutorial_global", first.Id);
            WITutorialSystem.Complete(state, first.Id);
            Assert.IsNull(WITutorialSystem.GetPending(database, state));

            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual("tutorial_castle", WITutorialSystem.GetPending(database, state).Id);
            WITutorialSystem.SkipAll(state);
            Assert.IsNull(WITutorialSystem.GetPending(database, state));
            int turnBefore = state.Turn;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.AreEqual(turnBefore + 1, state.Turn);
        }

        // 확인 및 건너뛰기 상태가 캠페인 저장 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void TutorialProgress_SaveRoundTripPreservesState()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState source = WIAdministrationState.Create(database);
            WITutorialSystem.Complete(source, "tutorial_global");
            WITutorialSystem.SkipAll(source);

            string json = WICampaignSaveSystem.Serialize(source);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.IsTrue(loaded.TutorialSkipped);
            Assert.Contains("tutorial_global", loaded.CompletedTutorialIds);
        }

        // 캠페인 승패 문구와 대륙 통일·세력 소멸 조건이 데이터로 구성됐는지 검증합니다.
        [Test]
        public void CampaignRules_DefineMinimumVictoryAndDefeat()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.IsTrue(database.CampaignRules.VictoryRequiresAllCastles);
            Assert.IsTrue(database.CampaignRules.DefeatWhenNoCastles);
            Assert.IsFalse(string.IsNullOrWhiteSpace(database.CampaignRules.VictoryTitle.Korean));
            Assert.IsFalse(string.IsNullOrWhiteSpace(database.CampaignRules.DefeatDescription.Korean));
        }

        // 플레이어가 대륙의 모든 성을 보유하면 승리를 한 번만 판정하는지 검증합니다.
        [Test]
        public void CampaignResult_AllCastlesOwnedTriggersVictoryOnce()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            foreach (WICastleRuntimeState castle in state.Castles) castle.FactionId = state.PlayerFactionId;

            Assert.IsTrue(WICampaignResultSystem.Evaluate(database, state));
            Assert.AreEqual(WICampaignResult.Victory, state.CampaignResult);
            Assert.AreEqual(state.Turn, state.CampaignResultTurn);
            Assert.IsFalse(WICampaignResultSystem.Evaluate(database, state));
            Assert.AreEqual(WICampaignResult.Victory, state.CampaignResult);
        }

        // 플레이어 소유 성이 하나도 남지 않으면 패배하고 일반 시작 상태는 계속 진행 중인지 검증합니다.
        [Test]
        public void CampaignResult_NoPlayerCastleTriggersDefeat()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState ongoing = WIAdministrationState.Create(database);
            Assert.IsFalse(WICampaignResultSystem.Evaluate(database, ongoing));
            Assert.AreEqual(WICampaignResult.Ongoing, ongoing.CampaignResult);

            ongoing.GetCastle("castle_00").FactionId = "valdor";
            Assert.IsTrue(WICampaignResultSystem.Evaluate(database, ongoing));
            Assert.AreEqual(WICampaignResult.Defeat, ongoing.CampaignResult);
        }

        // 다음 턴 연산 종료 지점에서 캠페인 조건이 자동 판정되는지 검증합니다.
        [Test]
        public void CampaignResult_ExecuteTurnEvaluatesVictory()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            foreach (WICastleRuntimeState castle in state.Castles) castle.FactionId = state.PlayerFactionId;

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(WICampaignResult.Victory, state.CampaignResult);
            Assert.AreEqual(state.Turn, state.CampaignResultTurn);
        }

        // 캠페인 결과와 확인 상태가 저장 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void CampaignResult_SaveRoundTripPreservesAcknowledgement()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState source = WIAdministrationState.Create(database);
            source.GetCastle("castle_00").FactionId = "valdor";
            WICampaignResultSystem.Evaluate(database, source);
            source.CampaignResultAcknowledged = true;

            string json = WICampaignSaveSystem.Serialize(source);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual(WICampaignResult.Defeat, loaded.CampaignResult);
            Assert.AreEqual(source.CampaignResultTurn, loaded.CampaignResultTurn);
            Assert.IsTrue(loaded.CampaignResultAcknowledged);
        }

        // 사업 비용·성과·확장 기간이 ScriptableObject 밸런스 값으로 구성됐는지 검증합니다.
        [Test]
        public void ProjectBalance_DefinesDataDrivenCostsAndGains()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIProjectBalanceDefinition balance = database.ProjectBalance;
            Assert.AreEqual(100, balance.BasicCost);
            Assert.AreEqual(180, balance.IntensiveCost);
            Assert.AreEqual(600, balance.ExpansionBasicCost);
            Assert.AreEqual(3, balance.ExpansionDurationMonths);
            Assert.AreEqual(balance.BasicCost, WIAdministrationTurnSystem.GetProjectCost(
                database, WICastleProjectType.Prosperity, WIProjectInvestment.Basic));
            Assert.AreEqual(1080, WIAdministrationTurnSystem.GetProjectCost(
                database, WICastleProjectType.Expansion, WIProjectInvestment.Intensive));
        }

        // 집중 투자가 더 빠르지만 기본 투자보다 금화당 효율은 조금 낮은 선택인지 검증합니다.
        [Test]
        public void ProjectBalance_IntensiveIsFasterWithEfficiencyTradeoff()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIHeroDefinition manager = database.GetHero("ares");
            int basicGain = WIAdministrationTurnSystem.GetExpectedProjectGain(
                database, WICastleProjectType.Prosperity, manager, WIProjectInvestment.Basic);
            int intensiveGain = WIAdministrationTurnSystem.GetExpectedProjectGain(
                database, WICastleProjectType.Prosperity, manager, WIProjectInvestment.Intensive);
            int basicCost = WIAdministrationTurnSystem.GetProjectCost(
                database, WICastleProjectType.Prosperity, WIProjectInvestment.Basic);
            int intensiveCost = WIAdministrationTurnSystem.GetProjectCost(
                database, WICastleProjectType.Prosperity, WIProjectInvestment.Intensive);

            Assert.Greater(intensiveGain, basicGain);
            Assert.Less(intensiveCost, basicCost * 2);
            Assert.Less(intensiveGain / (float)intensiveCost, basicGain / (float)basicCost);
            Assert.AreEqual(database.ProjectBalance.PolicyGainBonus,
                WIAdministrationTurnSystem.GetFactionPolicyBonus(
                    database, WIFactionPolicy.Prosperity, WICastleProjectType.Prosperity));
        }

        // 기본·집중 투자를 번갈아 12개월 사용해도 첫해 경제가 음수 없이 유지되는지 검증합니다.
        [Test]
        public void ProjectBalance_TwelveMonthActiveSpendingRemainsSolvent()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            WICastleProjectType[] projects =
            {
                WICastleProjectType.Prosperity, WICastleProjectType.Technology,
                WICastleProjectType.Stability, WICastleProjectType.Fortification
            };
            int totalSpent = 0;
            for (int month = 0; month < 12; month++)
            {
                WIProjectInvestment investment = month % 2 == 0
                    ? WIProjectInvestment.Intensive
                    : WIProjectInvestment.Basic;
                int cost = WIAdministrationTurnSystem.GetProjectCost(database, projects[month % projects.Length], investment);
                Assert.GreaterOrEqual(state.Gold, cost, $"{month + 1}개월 사업 예산 부족");
                state.Gold -= cost;
                state.PendingPlayerGoldSpent += cost;
                totalSpent += cost;
                castle.ActiveProject = new WICastleProjectState
                {
                    ProjectType = projects[month % projects.Length],
                    Investment = investment,
                    ManagerHeroId = "ares",
                    RemainingMonths = 1
                };
                WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
                Assert.AreEqual(cost, summary.GoldSpent);
                Assert.GreaterOrEqual(state.Gold, 0);
            }

            Assert.AreEqual(1680, totalSpent);
            Assert.Greater(state.Gold, 0);
            Assert.Less(state.Gold, 1200);
        }

        // 현재 인물·클래스·부대 역할 수가 병사 없는 군사 기획과 일치하는지 기록합니다.
        [Test]
        public void ContentAudit_ReportsCharacterAndCombatTypeCounts()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(8, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Hero));
            Assert.AreEqual(6, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common));
            Assert.AreEqual(9, System.Enum.GetValues(typeof(WIHeroClass)).Length);
            Assert.AreEqual(6, System.Enum.GetValues(typeof(WIUnitRole)).Length);
            TestContext.WriteLine("고유 영웅 8 · 일반 인물 6 · 클래스 9 · 부대 역할 6 · 병사/병종 데이터 없음(기획 의도)");
        }

        // 특기 8종의 표시 문구와 적용 사업이 모두 데이터에 있고 실제 +2 성과로 연결되는지 검증합니다.
        [Test]
        public void TraitDefinitions_CoverAllTraitsAndProjectBonuses()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(8, database.TraitDefinitions.Count);
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(WITraitType)),
                database.TraitDefinitions.Select(item => item.TraitType).ToArray());
            foreach (WITraitDefinition definition in database.TraitDefinitions)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName.Korean));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description.Korean));
                Assert.IsNotEmpty(definition.ProjectTypes.ToList());
                WIHeroDefinition owner = database.Heroes.FirstOrDefault(hero => hero.Traits.Contains(definition.TraitType));
                Assert.IsNotNull(owner, $"특기 보유 인물 누락: {definition.TraitType}");
                Assert.AreEqual(database.ProjectBalance.TraitGainBonus,
                    WIAdministrationTurnSystem.GetProjectTraitBonus(database, definition.ProjectTypes.First(), owner));
                WICastleProjectType unrelated = System.Enum.GetValues(typeof(WICastleProjectType))
                    .Cast<WICastleProjectType>().First(project => owner.Traits.All(trait =>
                        database.GetTrait(trait).ProjectTypes.Contains(project) == false));
                Assert.AreEqual(0, WIAdministrationTurnSystem.GetProjectTraitBonus(database, unrelated, owner));
            }
        }

        // 특기 8종이 각자 단순 성과 보너스와 다른 고유 결과를 만드는지 검증합니다.
        [TestCase(WITraitType.Agronomist)]
        [TestCase(WITraitType.Merchant)]
        [TestCase(WITraitType.Architect)]
        [TestCase(WITraitType.Constable)]
        [TestCase(WITraitType.Scholar)]
        [TestCase(WITraitType.Negotiator)]
        [TestCase(WITraitType.Doctor)]
        [TestCase(WITraitType.Instructor)]
        public void TraitUniqueEffects_ApplyDistinctProjectResults(WITraitType traitType)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WITraitDefinition trait = database.GetTrait(traitType);
            WIHeroDefinition manager = database.Heroes.First(hero => hero.Traits.Contains(traitType));
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            WIFactionRuntimeState faction = state.GetPlayerFactionState();
            WICastleProjectState project = new WICastleProjectState
            {
                ProjectType = trait.ProjectTypes.First(),
                Investment = WIProjectInvestment.Basic,
                ManagerHeroId = manager.Id
            };
            WITurnSummary summary = new WITurnSummary();
            WIArmyState army = new WIArmyState
            {
                ArmyId = "trait_test_army",
                FactionId = state.PlayerFactionId,
                CurrentCastleId = castle.CastleId,
                Supply = WISupplyState.Depleted
            };
            state.Armies.Add(army);
            WICharacterRuntimeState character = state.GetCharacter(castle.HeroIds.First());
            character.InjuryMonths = 3;
            character.Fatigue = 50;
            faction.ActiveResearchId = "trait_test_research";
            faction.ResearchRemainingMonths = 3;

            int goldBefore = faction.Gold;
            int influenceBefore = faction.Influence;
            int injuryBefore = character.InjuryMonths;
            WIAdministrationTurnSystem.ApplyTraitUniqueEffects(database, state, castle, project, manager, summary);

            Assert.IsTrue(summary.News.Any(news => news.Contains("특기 발동")));
            switch (traitType)
            {
                case WITraitType.Agronomist: Assert.AreEqual(WISupplyState.Sufficient, army.Supply); break;
                case WITraitType.Merchant: Assert.AreEqual(trait.UniqueEffectValue, summary.GoldGained); break;
                case WITraitType.Architect: Assert.Greater(summary.GoldGained, 0); break;
                case WITraitType.Constable: Assert.AreEqual(trait.UniqueEffectValue, castle.CounterintelligenceMonths); break;
                case WITraitType.Scholar: Assert.AreEqual(3 - trait.UniqueEffectValue, faction.ResearchRemainingMonths); break;
                case WITraitType.Negotiator: Assert.AreEqual(trait.UniqueEffectValue, summary.InfluenceGained); break;
                case WITraitType.Doctor: Assert.AreEqual(injuryBefore - trait.UniqueEffectValue, character.InjuryMonths); break;
                case WITraitType.Instructor: Assert.AreEqual(trait.UniqueEffectValue, army.CohesionExperience); break;
            }
            Assert.AreEqual(goldBefore, faction.Gold);
            Assert.AreEqual(influenceBefore, faction.Influence);
        }

        // 갈등·친애 관계 사건이 데이터에 두 선택지와 예상 결과를 갖는지 검증합니다.
        [Test]
        public void RelationshipEventDefinitions_ContainConflictAndFondnessChoices()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(2, database.RelationshipEventDefinitions.Count);
            Assert.IsTrue(database.RelationshipEventDefinitions.Any(item => item.RequiredLevel == WIRelationshipLevel.Conflict));
            Assert.IsTrue(database.RelationshipEventDefinitions.Any(item => item.RequiredLevel == WIRelationshipLevel.Fondness));
            foreach (WIRelationshipEventDefinition definition in database.RelationshipEventDefinitions)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Id));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Title.Korean));
                Assert.AreEqual(2, definition.Choices.Count);
                Assert.IsTrue(definition.Choices.All(choice =>
                    string.IsNullOrWhiteSpace(choice.Label.Korean) == false &&
                    string.IsNullOrWhiteSpace(choice.ResultDescription.Korean) == false));
            }
        }

        // 갈등 관계 사건의 과감한 선택이 관계·공적·피로를 바꾸고 같은 사건 재발을 막는지 검증합니다.
        [Test]
        public void RelationshipEvent_ResolvesChoiceAndPreventsDuplicate()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            castle.HeroIds.Clear();
            castle.HeroIds.Add("ares");
            castle.HeroIds.Add("lyria");
            WIRelationshipState relationship = state.GetOrCreateRelationship("ares", "lyria");
            relationship.Level = WIRelationshipLevel.Conflict;
            WITurnSummary summary = new WITurnSummary();

            WIAdministrationTurnSystem.CreateRelationshipEventCandidates(database, state, summary);
            Assert.AreEqual(1, state.PendingRelationshipEvents.Count);
            WIPendingRelationshipEvent pending = state.PendingRelationshipEvents[0];
            int meritBefore = state.GetCharacter("ares").Merit;
            int fatigueBefore = state.GetCharacter("ares").Fatigue;
            Assert.IsTrue(WIAdministrationTurnSystem.ResolveRelationshipEvent(database, state, pending, 1, summary));

            Assert.AreEqual(WIRelationshipLevel.Fondness, relationship.Level);
            Assert.AreEqual(meritBefore + 5, state.GetCharacter("ares").Merit);
            Assert.AreEqual(fatigueBefore + 10, state.GetCharacter("ares").Fatigue);
            Assert.AreEqual(1, state.CompletedRelationshipEventKeys.Count);
            Assert.IsEmpty(state.PendingRelationshipEvents);

            relationship.Level = WIRelationshipLevel.Conflict;
            WIAdministrationTurnSystem.CreateRelationshipEventCandidates(database, state, summary);
            Assert.IsEmpty(state.PendingRelationshipEvents);
        }

        // 대기 관계 사건과 완료 키가 저장·불러오기 후에도 유지되는지 검증합니다.
        [Test]
        public void RelationshipEvent_SaveRoundTripPreservesPendingAndCompletedState()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.PendingRelationshipEvents.Add(new WIPendingRelationshipEvent
            {
                EventId = "relationship_shared_oath",
                FirstHeroId = "ares",
                SecondHeroId = "lyria"
            });
            state.CompletedRelationshipEventKeys.Add("relationship_reconciliation:ares:lyria");

            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual("relationship_shared_oath", loaded.PendingRelationshipEvents.Single().EventId);
            Assert.Contains("relationship_reconciliation:ares:lyria", loaded.CompletedRelationshipEventKeys);
        }

        // 여덟 사업에 대응하는 영웅의 흔적 이름·설명·효과가 모두 데이터에 있는지 검증합니다.
        [Test]
        public void HeroLegacyDefinitions_CoverAllProjectTypes()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(System.Enum.GetValues(typeof(WICastleProjectType)).Length, database.HeroLegacyDefinitions.Count);
            foreach (WICastleProjectType projectType in System.Enum.GetValues(typeof(WICastleProjectType)))
            {
                WIHeroLegacyDefinition definition = database.GetHeroLegacy(projectType);
                Assert.IsNotNull(definition, $"흔적 정의 누락: {projectType}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.DisplayName.Korean));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description.Korean));
                Assert.Greater(definition.ProjectGainBonus, 0);
            }
        }

        // 세 번째 흔적 설치 시 교체 대상만 기념 기록으로 이동하고 새 효과만 활성화되는지 검증합니다.
        [Test]
        public void HeroLegacyReplacement_PreservesMonumentWithoutEffect()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            WIHeroLegacyState prosperity = CreateLegacy(database, WICastleProjectType.Prosperity, "ares");
            WIHeroLegacyState technology = CreateLegacy(database, WICastleProjectType.Technology, "lyria");
            WIHeroLegacyState stability = CreateLegacy(database, WICastleProjectType.Stability, "brom");
            castle.HeroLegacies.Add(prosperity);
            castle.HeroLegacies.Add(technology);
            WIPendingLegacyChoice pending = new WIPendingLegacyChoice { CastleId = castle.CastleId, Legacy = stability };
            state.PendingLegacyChoices.Add(pending);

            Assert.IsTrue(WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pending, prosperity));
            Assert.AreEqual(2, castle.HeroLegacies.Count);
            Assert.Contains(stability, castle.HeroLegacies);
            Assert.Contains(prosperity, castle.CommemoratedHeroLegacies);
            Assert.AreEqual(0, WIAdministrationTurnSystem.GetLegacyBonus(castle, WICastleProjectType.Prosperity));
            Assert.AreEqual(stability.Bonus, WIAdministrationTurnSystem.GetLegacyBonus(castle, WICastleProjectType.Stability));
        }

        // 활성 흔적과 기념 기록이 저장·불러오기 후에도 구분되어 유지되는지 검증합니다.
        [Test]
        public void HeroLegacy_SaveRoundTripPreservesActiveAndCommemoratedRecords()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            castle.HeroLegacies.Add(CreateLegacy(database, WICastleProjectType.Stability, "ares"));
            castle.CommemoratedHeroLegacies.Add(CreateLegacy(database, WICastleProjectType.Prosperity, "lyria"));

            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            WICastleRuntimeState restored = loaded.GetCastle("castle_00");
            Assert.AreEqual("legacy_stability", restored.HeroLegacies.Single().DefinitionId);
            Assert.AreEqual("legacy_prosperity", restored.CommemoratedHeroLegacies.Single().DefinitionId);
        }

        // 테스트용 영웅의 흔적 상태를 데이터 정의로 생성합니다.
        private static WIHeroLegacyState CreateLegacy(
            WIAdministrationDatabaseSO database,
            WICastleProjectType projectType,
            string heroId)
        {
            WIHeroLegacyDefinition definition = database.GetHeroLegacy(projectType);
            return new WIHeroLegacyState
            {
                DefinitionId = definition.Id,
                HeroId = heroId,
                ProjectType = projectType,
                DisplayName = definition.DisplayName.Korean,
                Description = definition.Description.Korean,
                Bonus = definition.ProjectGainBonus
            };
        }

        // 모든 인물이 세 종류의 데이터 기반 등용 요구 사건 중 하나를 참조하는지 검증합니다.
        [Test]
        public void RecruitmentEventDefinitions_AreAssignedToAllCharacters()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(3, database.RecruitmentEventDefinitions.Count);
            Assert.IsTrue(database.Heroes.All(hero => database.GetRecruitmentEvent(hero.RecruitmentEventId) != null));
            foreach (WIRecruitmentEventDefinition definition in database.RecruitmentEventDefinitions)
            {
                Assert.AreEqual(3, definition.Choices.Count);
                Assert.IsTrue(definition.Choices.Any(choice => choice.RequiresNegotiator));
            }
        }

        // 설득 진척 100%에서 즉시 영입하지 않고 요구 사건을 만든 뒤 선택으로 영입하는지 검증합니다.
        [Test]
        public void RecruitmentProgress_CreatesRequirementEventBeforeRecruiting()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICharacterRuntimeState recruiter = state.GetCharacter("ares");
            WICharacterRuntimeState candidate = state.GetCharacter("selene");
            recruiter.Reputation = 100;
            recruiter.Activity = WICharacterActivityType.Recruit;
            recruiter.ActivityTargetHeroId = candidate.HeroId;
            candidate.Discovered = true;
            candidate.Recruited = false;
            candidate.RecruitmentProgress = 99;

            WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.IsFalse(candidate.Recruited);
            Assert.AreEqual(1, state.PendingRecruitmentEvents.Count);
            WIPendingRecruitmentEvent pending = state.PendingRecruitmentEvents.Single();
            Assert.IsTrue(WIAdministrationTurnSystem.ResolveRecruitmentEvent(database, state, pending, 0, summary));
            Assert.IsTrue(candidate.Recruited);
            Assert.IsEmpty(state.PendingRecruitmentEvents);
        }

        // 공적·영토·교섭가 선택지가 각각 실제 조건에 따라 잠기거나 열리는지 검증합니다.
        [Test]
        public void RecruitmentEventOptions_UseMeritTerritoryAndNegotiatorConditions()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICharacterRuntimeState recruiter = state.GetCharacter("ares");
            WIPendingRecruitmentEvent service = new WIPendingRecruitmentEvent
            {
                EventId = "recruit_service", RecruiterHeroId = "ares", CandidateHeroId = "kael"
            };
            WIRecruitmentEventDefinition serviceDefinition = database.GetRecruitmentEvent(service.EventId);
            Assert.IsFalse(WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, service, serviceDefinition.Choices[1]));
            recruiter.Merit = 25;
            Assert.IsTrue(WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, service, serviceDefinition.Choices[1]));
            Assert.IsTrue(WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, service, serviceDefinition.Choices[2]));

            WIPendingRecruitmentEvent faction = new WIPendingRecruitmentEvent
            {
                EventId = "recruit_faction", RecruiterHeroId = "ares", CandidateHeroId = "theron"
            };
            WIRecruitmentEventChoiceDefinition territoryChoice = database.GetRecruitmentEvent(faction.EventId).Choices[1];
            Assert.IsFalse(WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, faction, territoryChoice));
            state.GetCastle("castle_01").FactionId = state.PlayerFactionId;
            state.GetCastle("castle_02").FactionId = state.PlayerFactionId;
            Assert.IsTrue(WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, faction, territoryChoice));
        }

        // 대기 중인 등용 요구 사건이 저장·불러오기 후 유지되는지 검증합니다.
        [Test]
        public void RecruitmentEvent_SaveRoundTripPreservesPendingChoice()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.PendingRecruitmentEvents.Add(new WIPendingRecruitmentEvent
            {
                EventId = "recruit_faction", RecruiterHeroId = "ares", CandidateHeroId = "elwyn"
            });
            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual("elwyn", loaded.PendingRecruitmentEvents.Single().CandidateHeroId);
        }

        // 선술집 의뢰 5종이 서로 다른 기간·적성·보상·성 효과 데이터를 갖는지 검증합니다.
        [Test]
        public void TavernQuestDefinitions_CoverFiveDistinctQuestTypes()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(5, database.TavernQuestDefinitions.Count);
            Assert.AreEqual(5, database.TavernQuestDefinitions.Select(item => item.QuestType).Distinct().Count());
            Assert.Greater(database.TavernQuestDefinitions.Select(item => item.GoldReward).Distinct().Count(), 1);
            Assert.Greater(database.TavernQuestDefinitions.Select(item => item.ReputationReward).Distinct().Count(), 1);
            Assert.IsTrue(database.TavernQuestDefinitions.All(item =>
                item.DurationMonths > 0 && item.CastleEffectValue > 0 && item.AptitudeBonusGold > 0));
        }

        // 각 의뢰가 정의된 보상과 고유 성 효과를 완료 시 적용하는지 검증합니다.
        [TestCase(WITavernQuestType.Escort)]
        [TestCase(WITavernQuestType.Hunt)]
        [TestCase(WITavernQuestType.Search)]
        [TestCase(WITavernQuestType.Mediation)]
        [TestCase(WITavernQuestType.Counterintelligence)]
        public void TavernQuestCompletion_AppliesDefinitionRewardsAndCastleEffect(WITavernQuestType questType)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            WITavernQuestDefinition definition = database.GetTavernQuest(questType);
            WIHeroDefinition hero = database.Heroes.OrderByDescending(item =>
                WIAdministrationTurnSystem.GetQuestAptitude(item, definition.Aptitude)).First();
            WICharacterRuntimeState character = state.GetCharacter(hero.Id);
            int meritBefore = character.Merit;
            int reputationBefore = character.Reputation;
            int prosperityBefore = castle.Prosperity;
            int technologyBefore = castle.Technology;
            int stabilityBefore = castle.Stability;
            castle.TavernQuests.Clear();
            castle.TavernQuests.Add(new WITavernQuestState
            {
                QuestId = "test_quest", QuestType = questType, Status = WIQuestStatus.Accepted,
                AssignedHeroId = hero.Id, RemainingMonths = 1
            });
            WITurnSummary summary = new WITurnSummary();

            WIAdministrationTurnSystem.ResolveTavernQuests(database, state, summary);

            bool specialized = WIAdministrationTurnSystem.GetQuestAptitude(hero, definition.Aptitude) >= definition.AptitudeThreshold;
            Assert.AreEqual(definition.GoldReward + (specialized ? definition.AptitudeBonusGold : 0), summary.GoldGained);
            Assert.AreEqual(meritBefore + definition.MeritReward, character.Merit);
            Assert.AreEqual(reputationBefore + definition.ReputationReward, character.Reputation);
            if (questType == WITavernQuestType.Escort) Assert.AreEqual(prosperityBefore + definition.CastleEffectValue, castle.Prosperity);
            if (questType == WITavernQuestType.Search) Assert.AreEqual(technologyBefore + definition.CastleEffectValue, castle.Technology);
            if (questType == WITavernQuestType.Hunt || questType == WITavernQuestType.Mediation)
                Assert.AreEqual(stabilityBefore + definition.CastleEffectValue, castle.Stability);
            if (questType == WITavernQuestType.Counterintelligence)
                Assert.AreEqual(definition.CastleEffectValue, castle.CounterintelligenceMonths);
        }

        // 여러 달 의뢰의 남은 기간이 저장·불러오기 후 유지되는지 검증합니다.
        [Test]
        public void TavernQuest_SaveRoundTripPreservesRemainingMonths()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            castle.TavernQuests.Add(new WITavernQuestState
            {
                QuestId = "long_quest", QuestType = WITavernQuestType.Hunt, Status = WIQuestStatus.Accepted,
                AssignedHeroId = "ares", RemainingMonths = 2
            });
            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual(2, loaded.GetCastle("castle_00").TavernQuests.Last().RemainingMonths);
        }

        // 태수 방침과 성 수치가 예상한 사업 선택 및 구체적 이유 문장으로 이어지는지 검증합니다.
        [TestCase(WIGovernorPolicy.Prosperity, WICastleProjectType.Prosperity)]
        [TestCase(WIGovernorPolicy.Research, WICastleProjectType.Technology)]
        [TestCase(WIGovernorPolicy.Talent, WICastleProjectType.Recruitment)]
        [TestCase(WIGovernorPolicy.Frontline, WICastleProjectType.Stability)]
        [TestCase(WIGovernorPolicy.Balanced, WICastleProjectType.Prosperity)]
        public void GovernorPlan_ExplainsPolicyAndCastleState(WIGovernorPolicy policy, WICastleProjectType expected)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            castle.GovernorPolicy = policy;
            castle.Prosperity = 10;
            castle.Technology = 40;
            castle.Stability = 20;
            castle.Defense = 30;
            WICastleProjectType selected = WIAdministrationTurnSystem.ChooseGovernorProject(castle);

            Assert.AreEqual(expected, selected);
            string reason = WIAdministrationTurnSystem.GetGovernorReason(database, castle, selected, database.GetHero("ares"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(reason));
            Assert.IsFalse(reason.Contains("방침에 따라"));
        }

        // 위임 설정 화면의 미리보기에 사업·이유·성과·비용·기간이 모두 포함되는지 검증합니다.
        [Test]
        public void GovernorPreview_ShowsExpectedGainCostDurationAndReason()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            castle.GovernorPolicy = WIGovernorPolicy.Prosperity;
            castle.GovernorMonthlyBudget = database.ProjectBalance.IntensiveCost;

            string preview = WIAdministrationTurnSystem.GetDelegationPreview(database, state, castle);
            StringAssert.Contains("Prosperity", preview);
            StringAssert.Contains("성과 +", preview);
            StringAssert.Contains("비용", preview);
            StringAssert.Contains("1개월", preview);
        }

        // 위임 월보가 계획과 실제 결과를 함께 남기고 값이 일치하는지 검증합니다.
        [Test]
        public void GovernorMonthlyReport_ComparesPlanAndActualResult()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            castle.DelegatedToGovernor = true;
            castle.GovernorPolicy = WIGovernorPolicy.Prosperity;
            castle.GovernorMonthlyBudget = database.ProjectBalance.BasicCost;

            WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);

            string plan = summary.DelegationReports.Single(item => item.Contains("[위임 계획]"));
            string result = summary.DelegationReports.Single(item => item.Contains("[위임 결과]"));
            StringAssert.Contains("예상 +", plan);
            StringAssert.Contains("예상 +", result);
            StringAssert.Contains("실제 +", result);
            StringAssert.Contains("비용", result);
        }

        // 60개 성 모두 전문 분야 효과를 가지며 다섯 공통 유형이 실제로 사용되는지 검증합니다.
        [Test]
        public void CastleSpecialties_ClassifyAllCastlesIntoFiveEffectTypes()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(60, database.Castles.Count);
            Assert.IsTrue(database.Castles.All(castle => castle.SpecialtyEffectValue > 0));
            foreach (WICastleSpecialtyEffectType effectType in System.Enum.GetValues(typeof(WICastleSpecialtyEffectType)))
            {
                Assert.IsTrue(database.Castles.Any(castle => castle.SpecialtyEffectType == effectType),
                    $"사용되지 않는 전문 분야 효과: {effectType}");
            }
        }

        // 사업·금화·마나·영향력 전문 분야가 각 계산에 정확히 한 번 반영되는지 검증합니다.
        [Test]
        public void CastleSpecialties_ApplyProjectAndIncomeBonuses()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleDefinition projectCastle = database.Castles.First(item => item.SpecialtyEffectType == WICastleSpecialtyEffectType.ProjectGain);
            Assert.AreEqual(projectCastle.SpecialtyEffectValue,
                WIAdministrationTurnSystem.GetCastleSpecialtyProjectBonus(projectCastle, projectCastle.SpecialtyProjectType));
            WICastleProjectType unrelated = System.Enum.GetValues(typeof(WICastleProjectType)).Cast<WICastleProjectType>()
                .First(item => item != projectCastle.SpecialtyProjectType);
            Assert.AreEqual(0, WIAdministrationTurnSystem.GetCastleSpecialtyProjectBonus(projectCastle, unrelated));

            foreach (WICastleSpecialtyEffectType effectType in new[]
                     { WICastleSpecialtyEffectType.GoldIncome, WICastleSpecialtyEffectType.ManaIncome, WICastleSpecialtyEffectType.InfluenceIncome })
            {
                WICastleDefinition definition = database.Castles.First(item => item.SpecialtyEffectType == effectType);
                WICastleRuntimeState runtime = state.GetCastle(definition.Id);
                WITurnSummary withSpecialty = new WITurnSummary();
                WIAdministrationTurnSystem.AddCastleIncome(definition, runtime, withSpecialty);
                WITurnSummary baseline = new WITurnSummary();
                WIAdministrationTurnSystem.AddCastleIncome(null, runtime, baseline);
                if (effectType == WICastleSpecialtyEffectType.GoldIncome)
                    Assert.AreEqual(definition.SpecialtyEffectValue, withSpecialty.GoldGained - baseline.GoldGained);
                if (effectType == WICastleSpecialtyEffectType.ManaIncome)
                    Assert.AreEqual(definition.SpecialtyEffectValue, withSpecialty.ManaGained - baseline.ManaGained);
                if (effectType == WICastleSpecialtyEffectType.InfluenceIncome)
                    Assert.AreEqual(definition.SpecialtyEffectValue, withSpecialty.InfluenceGained - baseline.InfluenceGained);
            }
        }

        // 방어 전문 분야가 소유 세력과 관계없이 전략 방어 전투력에 적용되는지 검증합니다.
        [Test]
        public void CastleSpecialty_DefensePowerAppliesToPlayerAndAI()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleDefinition definition = database.Castles.First(item => item.SpecialtyEffectType == WICastleSpecialtyEffectType.DefensePower);
            WICastleRuntimeState runtime = state.GetCastle(definition.Id);
            int originalPower = WIAdministrationTurnSystem.GetCastleDefensePower(database, state, runtime, new List<WIArmyState>());
            runtime.FactionId = state.PlayerFactionId;
            int playerPower = WIAdministrationTurnSystem.GetCastleDefensePower(database, state, runtime, new List<WIArmyState>());
            Assert.AreEqual(originalPower, playerPower);
            Assert.GreaterOrEqual(playerPower, runtime.Defense * 2 + runtime.Stability / 2 + definition.SpecialtyEffectValue);
        }

        // 각 난이도에서 12개월을 진행하며 경제·성·부대·인물 상태가 장기적으로 유효한지 검증합니다.
        [TestCase(WICampaignDifficulty.Relaxed)]
        [TestCase(WICampaignDifficulty.Standard)]
        [TestCase(WICampaignDifficulty.Hard)]
        public void CampaignSimulation_TwelveMonthsPreservesStateIntegrity(WICampaignDifficulty difficulty)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database, difficulty);
            Dictionary<string, string> initialOwners = state.Castles.ToDictionary(castle => castle.CastleId, castle => castle.FactionId);

            for (int month = 0; month < 12; month++)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
                AssertCampaignIntegrity(database, state, $"{difficulty} {month + 1}개월");
            }

            int ownershipChanges = state.Castles.Count(castle => initialOwners[castle.CastleId] != castle.FactionId);
            TestContext.WriteLine(
                $"{difficulty}: 턴 {state.Turn}, {state.Year}년 {state.Month:00}월, " +
                $"플레이어 G/M/I {state.Gold}/{state.ManaCrystal}/{state.Influence}, " +
                $"부대 {state.Armies.Count}, 전투 {state.BattleSessions.Count}, 점령 {ownershipChanges}");
            Assert.AreEqual(13, state.Turn);
            Assert.AreEqual(database.StartingYear + 1, state.Year);
            Assert.AreEqual(database.StartingMonth, state.Month);
        }

        // 세 난이도에서 AI가 36개월 동안 경제·사업·연구·부대를 중단 없이 운영하는지 검증합니다.
        [TestCase(WICampaignDifficulty.Relaxed)]
        [TestCase(WICampaignDifficulty.Standard)]
        [TestCase(WICampaignDifficulty.Hard)]
        public void CampaignSimulation_ThirtySixMonthsKeepsAIOperational(WICampaignDifficulty difficulty)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database, difficulty);
            List<WIFactionDefinition> aiFactions = database.Factions.Where(faction => faction.PlayerFaction == false).ToList();
            HashSet<string> factionsWithProjects = new HashSet<string>();
            HashSet<string> factionsWithArmies = new HashSet<string>();
            HashSet<string> factionsWithMovement = new HashSet<string>();
            int maximumBattleCount = 0;
            int completedAIProjects = 0;

            for (int month = 0; month < 36; month++)
            {
                WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
                completedAIProjects += summary.AIProjectsCompleted;
                factionsWithProjects.UnionWith(summary.AIProjectFactionIds);
                AssertCampaignIntegrity(database, state, $"{difficulty} 장기 {month + 1}개월");
                foreach (WIFactionDefinition faction in aiFactions)
                {
                    if (state.Armies.Any(army => army.FactionId == faction.Id))
                    {
                        factionsWithArmies.Add(faction.Id);
                    }
                    if (state.Armies.Any(army => army.FactionId == faction.Id && army.IsMoving))
                    {
                        factionsWithMovement.Add(faction.Id);
                    }
                }
                maximumBattleCount = Mathf.Max(maximumBattleCount, state.BattleSessions.Count);
            }

            foreach (WIFactionDefinition faction in aiFactions)
            {
                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                Assert.Contains(faction.Id, factionsWithProjects.ToList(), $"AI 사업 완료가 없습니다: {faction.Id}");
                Assert.IsNotEmpty(factionState.CompletedResearchIds, $"AI 연구 완료가 없습니다: {faction.Id}");
                Assert.Contains(faction.Id, factionsWithArmies.ToList(), $"AI 부대 생성이 없습니다: {faction.Id}");
            }
            Assert.GreaterOrEqual(completedAIProjects, 36, "AI 사업 완료 횟수가 장기 운영 기준보다 적습니다.");
            Assert.Contains("valdor", factionsWithMovement.ToList(), "공세 AI가 36개월 동안 한 번도 이동하지 않았습니다.");
            Assert.GreaterOrEqual(factionsWithMovement.Count, 2, "전선 위협에 반응해 이동한 AI 세력이 너무 적습니다.");
            Assert.Greater(maximumBattleCount, 0, "36개월 동안 전투 세션이 생성되지 않았습니다.");
            Assert.AreEqual(37, state.Turn);
            Assert.AreEqual(database.StartingYear + 3, state.Year);
            Assert.AreEqual(database.StartingMonth, state.Month);

            string aiResources = string.Join(", ", aiFactions.Select(faction =>
            {
                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                return string.Format("{0}={1}/{2}/{3}", faction.Id, factionState.Gold,
                    factionState.ManaCrystal, factionState.Influence);
            }));
            TestContext.WriteLine(
                $"{difficulty}: 36개월 · AI 부대 {state.Armies.Count(army => army.FactionId != state.PlayerFactionId)} · " +
                $"사업 {completedAIProjects} · 전투 {state.BattleSessions.Count} · AI 연구 {aiFactions.Sum(faction => state.GetFactionState(faction.Id).CompletedResearchIds.Count)} · " +
                $"AI 자원 {aiResources}");
        }

        // 후보가 여러 명일 때 도전 난이도는 최적 담당자를, 여유 난이도는 더 넓은 후보를 선택하는지 검증합니다.
        [Test]
        public void CampaignDifficulty_ChangesAICandidatePrecisionWithoutBonuses()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState hard = WIAdministrationState.Create(database, WICampaignDifficulty.Hard);
            WIAdministrationState relaxed = WIAdministrationState.Create(database, WICampaignDifficulty.Relaxed);
            WICastleRuntimeState hardCastle = hard.GetCastle("castle_01");
            WICastleRuntimeState relaxedCastle = relaxed.GetCastle("castle_01");
            string[] candidates = { "ares", "lyria", "selene" };
            hardCastle.HeroIds = candidates.ToList();
            relaxedCastle.HeroIds = candidates.ToList();

            WIHeroDefinition best = candidates.Select(database.GetHero)
                .OrderByDescending(hero => WIAdministrationTurnSystem.GetExpectedProjectGain(
                    database, WICastleProjectType.Technology, hero, WIProjectInvestment.Basic)).First();
            WIHeroDefinition hardChoice = WIAdministrationTurnSystem.SelectAIProjectManager(
                database, hard, hardCastle, WICastleProjectType.Technology);
            WIHeroDefinition relaxedChoice = WIAdministrationTurnSystem.SelectAIProjectManager(
                database, relaxed, relaxedCastle, WICastleProjectType.Technology);

            Assert.AreEqual(best.Id, hardChoice.Id);
            Assert.AreNotEqual(hardChoice.Id, relaxedChoice.Id);
            Assert.AreEqual(hard.Gold, relaxed.Gold);
            Assert.AreEqual(hard.ManaCrystal, relaxed.ManaCrystal);
            Assert.AreEqual(hard.Influence, relaxed.Influence);
        }

        // 캠페인의 경제·성 수치·참조 관계와 인물 중복 배치 불변 조건을 검사합니다.
        private static void AssertCampaignIntegrity(WIAdministrationDatabaseSO database, WIAdministrationState state, string context)
        {
            Assert.IsTrue(state.Factions.All(faction => faction.Gold >= 0 && faction.ManaCrystal >= 0 && faction.Influence >= 0),
                $"음수 세력 자원: {context}");
            Assert.IsTrue(state.Castles.All(castle => database.GetFaction(castle.FactionId) != null),
                $"무효 성 소유 세력: {context}");
            Assert.IsTrue(state.Castles.All(castle => castle.Prosperity >= 0 && castle.Prosperity <= 100 &&
                castle.Technology >= 0 && castle.Technology <= 100 && castle.Stability >= 0 && castle.Stability <= 100 &&
                castle.Defense >= 0 && castle.Defense <= 100), $"성 수치 범위 이탈: {context}");

            string[] armyHeroIds = state.Armies.SelectMany(army => army.Members.Select(member => member.HeroId)).ToArray();
            Assert.AreEqual(armyHeroIds.Length, armyHeroIds.Distinct().Count(), $"여러 부대에 중복된 인물: {context}");
            foreach (WIArmyState army in state.Armies)
            {
                Assert.IsNotNull(database.GetFaction(army.FactionId), $"무효 부대 세력: {army.ArmyId} · {context}");
                Assert.IsNotNull(database.GetCastle(army.CurrentCastleId), $"무효 부대 현재 성: {army.ArmyId} · {context}");
                if (string.IsNullOrEmpty(army.TargetCastleId) == false)
                {
                    Assert.IsNotNull(database.GetCastle(army.TargetCastleId), $"무효 부대 목표 성: {army.ArmyId} · {context}");
                }
                Assert.AreEqual(1, army.Members.Count(member => member.Role == WIUnitRole.Commander),
                    $"부대 대장 수 오류: {army.ArmyId} · {context}");
            }

            foreach (WICastleRuntimeState castle in state.Castles.Where(castle => castle.ActiveProject != null))
            {
                Assert.IsNotNull(database.GetHero(castle.ActiveProject.ManagerHeroId),
                    $"사업 담당 인물 참조 누락: {castle.CastleId} · {context}");
            }
        }

        // 네 가지 초기 계략이 ScriptableObject 데이터로 구성됐는지 검증합니다.
        [Test]
        public void SchemeDefinitions_ContainPlannedMvpCommands()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(4, database.SchemeDefinitions.Count);
            CollectionAssert.AreEquivalent(new[] { WISchemeType.Investigation, WISchemeType.Counterintelligence, WISchemeType.Rumor, WISchemeType.Alienation },
                database.SchemeDefinitions.Select(item => item.SchemeType));
        }

        // 조사 성공 시 영향력을 소비하고 대상 성 정보를 제한 기간 동안 확보하는지 검증합니다.
        [Test]
        public void Scheme_InvestigationStoresTemporaryIntel()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            int influenceBefore = state.GetFactionState("avalon").Influence;

            WISchemeResult result = WISchemeSystem.Execute(database, state, "scheme_investigation", "avalon", "ares", "castle_01", null, 0);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(influenceBefore - database.GetScheme("scheme_investigation").InfluenceCost, state.GetFactionState("avalon").Influence);
            Assert.AreEqual(3, state.SchemeIntel.Single().RemainingMonths);
        }

        // 방첩이 적 계략 성공률을 낮추고 월간 진행 후 만료되는지 검증합니다.
        [Test]
        public void Scheme_CounterintelligenceReducesChanceAndExpires()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WISchemeResult defense = WISchemeSystem.Execute(database, state, "scheme_counterintelligence", "avalon", "ares", "castle_00", null, 99);
            Assert.IsTrue(defense.Succeeded);

            WISchemeResult blocked = WISchemeSystem.Execute(database, state, "scheme_rumor", "valdor", "lyria", "castle_00", null, 89);
            Assert.IsTrue(blocked.Executed);
            Assert.IsFalse(blocked.Succeeded);
            Assert.AreEqual(3, state.GetCastle("castle_00").CounterintelligenceMonths);
            WISchemeSystem.AdvanceMonth(state);
            Assert.AreEqual(2, state.GetCastle("castle_00").CounterintelligenceMonths);
        }

        // 유언비어와 인재 이간 성공 결과가 각각 치안과 충성 상태에 적용되는지 검증합니다.
        [Test]
        public void Scheme_RumorAndAlienationApplyPlannedEffects()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            int stabilityBefore = state.GetCastle("castle_01").Stability;

            Assert.IsTrue(WISchemeSystem.Execute(database, state, "scheme_rumor", "avalon", "ares", "castle_01", null, 0).Succeeded);
            Assert.AreEqual(stabilityBefore - 10, state.GetCastle("castle_01").Stability);
            Assert.IsTrue(WISchemeSystem.Execute(database, state, "scheme_alienation", "avalon", "ares", "castle_01", "lyria", 0).Succeeded);
            Assert.AreEqual(WILoyaltyState.Unsettled, state.GetCharacter("lyria").LoyaltyState);
        }

        // 성 좌표가 격자가 아닌 각 세력의 지리적 본거지에 배치되었는지 검증합니다.
        [Test]
        public void MapLayout_PlacesFactionsInGeographicRegions()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(60, database.Castles.Count);
            Assert.AreEqual(60, database.Castles.Select(castle => castle.NormalizedMapPosition).Distinct().Count());
            Assert.IsTrue(database.Castles.All(castle => castle.NormalizedMapPosition.x >= 0.08f && castle.NormalizedMapPosition.x <= 0.92f));
            Assert.IsTrue(database.Castles.All(castle => castle.NormalizedMapPosition.y >= 0.08f && castle.NormalizedMapPosition.y <= 0.92f));

            Vector2 valdorCenter = GetFactionMapCenter(database, "valdor");
            Vector2 ironheartCenter = GetFactionMapCenter(database, "ironheart");
            Vector2 sylvanroadCenter = GetFactionMapCenter(database, "sylvanroad");
            Vector2 necropolisCenter = GetFactionMapCenter(database, "necropolis");
            Assert.Less(ironheartCenter.y, valdorCenter.y);
            Assert.Less(sylvanroadCenter.x, valdorCenter.x);
            Assert.Greater(necropolisCenter.y, valdorCenter.y);

            foreach (WICastleDefinition castle in database.Castles)
            {
                Assert.GreaterOrEqual(castle.AdjacentCastleIds.Count, 1, $"고립된 성: {castle.Id}");
                Assert.IsTrue(castle.AdjacentCastleIds.All(adjacentId =>
                    database.GetCastle(adjacentId).AdjacentCastleIds.Contains(castle.Id)), $"단방향 연결: {castle.Id}");
            }
        }

        // 60개 성이 고유 명칭·지형·전문 분야와 중복 없는 UID를 갖는지 검증합니다.
        [Test]
        public void CastleLore_HasNoTemporaryNamesOrEmptyFields()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.AreEqual(60, database.Castles.Count);
            Assert.AreEqual(60, database.Castles.Select(castle => castle.DisplayName.Korean).Distinct().Count());
            Assert.AreEqual(60, database.Castles.Select(castle => castle.DisplayName.English).Distinct().Count());
            Assert.AreEqual(60, database.Castles.Select(castle => castle.DisplayName.Uid).Distinct().Count());

            foreach (WICastleDefinition castle in database.Castles)
            {
                StringAssert.DoesNotMatch(@"성\s*\d+$", castle.DisplayName.Korean, castle.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(castle.TerrainTrait.Korean), $"지형 특성 누락: {castle.Id}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(castle.TerrainTrait.English), $"영문 지형 특성 누락: {castle.Id}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(castle.Specialty.Korean), $"전문 분야 누락: {castle.Id}");
                Assert.IsTrue(castle.Specialty.Korean.Contains("·"), $"랜드마크와 내정 개성 구분 누락: {castle.Id}");
            }
        }

        // 지정한 세력에 속한 성들의 지도 중심 좌표를 계산합니다.
        private static Vector2 GetFactionMapCenter(WIAdministrationDatabaseSO database, string factionId)
        {
            WICastleDefinition[] castles = database.Castles.Where(castle => castle.FactionId == factionId).ToArray();
            return new Vector2(castles.Average(castle => castle.NormalizedMapPosition.x), castles.Average(castle => castle.NormalizedMapPosition.y));
        }

        // 신규 캠페인이 모든 세력 쌍의 관계와 아발론-발도르 전쟁을 초기화하는지 검증합니다.
        [Test]
        public void Diplomacy_InitializesAllFactionRelations()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            Assert.AreEqual(10, state.DiplomaticRelations.Count);
            Assert.AreEqual(WIDiplomaticStatus.War, state.GetOrCreateDiplomaticRelation("avalon", "valdor").Status);
            Assert.AreEqual(WIDiplomaticStatus.Neutral, state.GetOrCreateDiplomaticRelation("avalon", "ironheart").Status);
        }

        // 친선부터 동맹과 원조까지 외교 단계가 자원을 소비하며 순서대로 진행되는지 검증합니다.
        [Test]
        public void Diplomacy_ProgressesToAllianceAndTransfersAid()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIFactionRuntimeState player = state.GetFactionState("avalon");
            WIFactionRuntimeState ironheart = state.GetFactionState("ironheart");
            player.Gold = 1000;
            player.Influence = 1000;
            ironheart.Gold = 1000;

            Assert.IsTrue(WIAdministrationTurnSystem.ImproveDiplomaticRelations(state, "avalon", "ironheart"));
            Assert.IsTrue(WIAdministrationTurnSystem.SignNonAggression(state, "avalon", "ironheart"));
            Assert.IsTrue(WIAdministrationTurnSystem.FormAlliance(state, "avalon", "ironheart"));
            int playerGoldBeforeAid = player.Gold;
            Assert.IsTrue(WIAdministrationTurnSystem.RequestAllianceAid(state, "avalon", "ironheart"));

            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation("avalon", "ironheart");
            Assert.AreEqual(WIDiplomaticStatus.Alliance, relation.Status);
            Assert.AreEqual(playerGoldBeforeAid + WIAdministrationTurnSystem.AllianceAidGold, player.Gold);
            Assert.AreEqual(6, relation.AidCooldownMonths);
            Assert.IsFalse(WIAdministrationTurnSystem.RequestAllianceAid(state, "avalon", "ironheart"));
        }

        // 외교 관계와 원조 대기 시간이 캠페인 저장 왕복 후에도 유지되는지 검증합니다.
        [Test]
        public void Diplomacy_SaveRoundTripPreservesRelations()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation("avalon", "sylvanroad");
            relation.Status = WIDiplomaticStatus.Alliance;
            relation.AidCooldownMonths = 4;

            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            WIDiplomaticRelationState loadedRelation = loaded.GetOrCreateDiplomaticRelation("avalon", "sylvanroad");
            Assert.AreEqual(WIDiplomaticStatus.Alliance, loadedRelation.Status);
            Assert.AreEqual(4, loadedRelation.AidCooldownMonths);
        }

        // 모든 세력 문장이 올바른 단일 Sprite 설정과 원본 크기로 연결됐는지 검증합니다.
        [Test]
        public void FactionEmblems_AreAssignedAsValidSprites()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            Assert.IsNotNull(database);
            Assert.AreEqual(5, database.Factions.Count);
            foreach (WIFactionDefinition faction in database.Factions)
            {
                Assert.IsNotNull(faction.Emblem, $"{faction.Id} 세력 문장이 연결되지 않았습니다.");
                Assert.AreEqual(1254f, faction.Emblem.rect.width, $"{faction.Id} 문장 너비가 원본과 다릅니다.");
                Assert.AreEqual(1254f, faction.Emblem.rect.height, $"{faction.Id} 문장 높이가 원본과 다릅니다.");

                string path = AssetDatabase.GetAssetPath(faction.Emblem);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, $"{faction.Id} 문장 TextureImporter를 찾을 수 없습니다.");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
                Assert.IsTrue(importer.alphaIsTransparency);
                Assert.IsFalse(importer.mipmapEnabled);
            }
        }

        // 5대 세력 수도에 제작된 전경 Sprite가 연결됐는지 검증합니다.
        [Test]
        public void CapitalCastleImages_AreAssignedAsValidSprites()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            string[] castleIds = { "castle_00", "castle_01", "castle_26", "castle_38", "castle_50" };
            string[] imagePaths =
            {
                "Assets/Art/Castles/Castle_Avalon_V1.png",
                "Assets/Art/Castles/Castle_Valdor_V1.png",
                "Assets/Art/Castles/Castle_Ironhold_V1.png",
                "Assets/Art/Castles/Castle_Sylvarion_V1.png",
                "Assets/Art/Castles/Castle_Necropolis_V1.png"
            };

            for (int index = 0; index < castleIds.Length; index++)
            {
                WICastleDefinition capital = database.GetCastle(castleIds[index]);
                Assert.IsNotNull(capital);
                Assert.IsNotNull(capital.CastleImage, $"{castleIds[index]} 수도 전경이 연결되지 않았습니다.");
                Assert.AreEqual(imagePaths[index], AssetDatabase.GetAssetPath(capital.CastleImage));

                TextureImporter importer = AssetImporter.GetAtPath(imagePaths[index]) as TextureImporter;
                Assert.IsNotNull(importer);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
                Assert.IsFalse(importer.mipmapEnabled);
            }
        }

        // 모든 성·인물·특화 시설에 교체 가능한 Sprite 자산이 연결됐는지 검증합니다.
        [Test]
        public void SharedVisualAssets_AreAssignedToAllDefinitions()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.IsTrue(database.Castles.All(castle => castle.CastleImage != null));
            Assert.IsTrue(database.Heroes.All(hero => hero.Portrait != null));
            Assert.IsTrue(database.SpecialFacilities.All(facility => facility.Icon != null));

            foreach (Sprite sprite in database.Heroes.Select(hero => hero.Portrait)
                         .Concat(database.SpecialFacilities.Select(facility => facility.Icon)))
            {
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
                Assert.IsNotNull(importer);
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
                Assert.IsFalse(importer.mipmapEnabled);
            }
        }

        // 한 턴 뒤에도 플레이어와 AI의 경제가 서로 독립적으로 계산되는지 검증합니다.
        [Test]
        public void ExecuteTurn_SeparatesFactionEconomies()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            int playerGoldBefore = state.GetFactionState("avalon").Gold;
            int valdorGoldBefore = state.GetFactionState("valdor").Gold;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(5, state.Factions.Count);
            Assert.Greater(state.GetFactionState("avalon").Gold, playerGoldBefore);
            Assert.Greater(state.GetFactionState("valdor").Gold, valdorGoldBefore);
            Assert.AreNotSame(state.GetFactionState("avalon"), state.GetFactionState("valdor"));
        }

        // 공세 AI가 실제 영향력을 소비해 플레이어 인접 성으로 출정하는지 검증합니다.
        [Test]
        public void ExecuteTurn_AggressiveAIConsumesInfluenceAndWarnsPlayer()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == "valdor"))
            {
                castle.Defense = castle.CastleId == "castle_01" ? 0 : 100;
                castle.Stability = castle.CastleId == "castle_01" ? 0 : 100;
            }
            int influenceBefore = state.GetFactionState("valdor").Influence;
            int expectedIncome = state.Castles
                .Where(castle => castle.FactionId == "valdor")
                .Sum(castle => System.Math.Max(1, castle.Stability * ((int)castle.CastleSize + 1) / 25));

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            WIArmyState army = state.Armies.FirstOrDefault(item => item.FactionId == "valdor");
            Assert.IsNotNull(army);
            Assert.AreEqual("castle_00", army.TargetCastleId);
            Assert.IsTrue(state.GetCastle("castle_00").InvasionWarning);
            Assert.LessOrEqual(state.GetFactionState("valdor").Influence, influenceBefore + expectedIncome - 20);
        }

        // 지휘관이 충분한 공세 세력이 영토 규모에 맞춰 복수 부대를 운용하는지 검증합니다.
        [Test]
        public void ExecuteTurn_AggressiveAIUsesMultipleArmiesWhenCommandersExist()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == "valdor"))
            {
                castle.Defense = castle.CastleId == "castle_01" ? 0 : 100;
                castle.Stability = castle.CastleId == "castle_01" ? 0 : 100;
            }
            WICastleRuntimeState frontline = state.GetCastle("castle_01");
            frontline.HeroIds.Add("selene");
            state.GetCharacter("selene").Discovered = true;
            state.GetCharacter("selene").Recruited = true;

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(3, state.Armies.Count(army => army.FactionId == "valdor"));
            Assert.IsTrue(state.Armies
                .Where(army => army.FactionId == "valdor")
                .All(army => army.Mission == WIArmyMission.Attack && army.TargetCastleId == "castle_00"));
        }

        // 적과 맞닿은 성이 후방 성보다 높은 전선 위협도를 갖는지 검증합니다.
        [Test]
        public void ThreatScore_PrioritizesFrontlineCastle()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            int frontlineScore = WIAdministrationTurnSystem.GetCastleThreatScore(database, state, state.GetCastle("castle_01"), "valdor");
            WICastleRuntimeState rearCastle = state.Castles
                .First(castle => castle.FactionId == "valdor" && database.GetCastle(castle.CastleId).AdjacentCastleIds
                    .All(id => state.GetCastle(id).FactionId == "valdor"));
            int rearScore = WIAdministrationTurnSystem.GetCastleThreatScore(database, state, rearCastle, "valdor");

            Assert.Greater(frontlineScore, rearScore);
        }

        // 수비 전력이 높은 성을 공격한 약한 부대가 원래 성으로 후퇴하고 재편성하는지 검증합니다.
        [Test]
        public void StrategicBattle_DefeatedArmyRetreatsAndReorganizes()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.UsePlayerRealTimeBattles = false;
            PrepareValdorAttackOnAvalon(state);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIArmyState army = state.Armies.First(item => item.FactionId == "valdor");
            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(WIBattleOutcome.Defeat, army.LastBattleOutcome);
            Assert.AreEqual("castle_01", army.CurrentCastleId);
            Assert.AreEqual(1, army.ReorganizationMonths);
            Assert.Greater(state.GetCharacter("lyria").Fatigue, 0);
            Assert.AreEqual("avalon", state.GetCastle("castle_00").FactionId);
        }

        // 충분히 강한 공격 부대가 승리하면 기존 점령 처리와 전투 보상이 함께 적용되는지 검증합니다.
        [Test]
        public void StrategicBattle_VictoryOccupiesCastleAndRewardsMembers()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.UsePlayerRealTimeBattles = false;
            PrepareValdorAttackOnAvalon(state);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIArmyState army = state.Armies.First(item => item.FactionId == "valdor");
            string[] reinforcements = { "selene", "kael", "elwyn" };
            foreach (string heroId in reinforcements)
            {
                state.GetCharacter(heroId).Discovered = true;
                state.GetCharacter(heroId).Recruited = true;
                army.Members.Add(new WIArmyMemberState { HeroId = heroId, Role = WIUnitRole.Melee });
            }

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(WIBattleOutcome.Victory, army.LastBattleOutcome);
            Assert.AreEqual("valdor", state.GetCastle("castle_00").FactionId);
            Assert.AreEqual(3, state.GetCastle("castle_00").OccupationUnrestMonths);
            Assert.Greater(state.GetCharacter("lyria").Merit, 0);
        }

        // 자동 전략 판정도 전투 세션과 공통 결과 API를 거쳐 기록되는지 검증합니다.
        [Test]
        public void StrategicBattle_RecordsResolvedBattleSession()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.UsePlayerRealTimeBattles = false;
            PrepareValdorAttackOnAvalon(state);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            WIBattleSessionState session = state.BattleSessions.First(item => item.PlayerInvolved);
            Assert.AreEqual(WIBattleSessionStatus.Resolved, session.Status);
            Assert.AreEqual(WIBattleResolutionSource.StrategicFallback, session.ResolutionSource);
            Assert.IsTrue(session.PlayerInvolved);
            Assert.Greater(session.AttackerPowerSnapshot, 0);
            Assert.Greater(session.DefenderPowerSnapshot, 0);
        }

        // 실시간 전투 모드에서는 세션을 보류하고 외부 결과를 한 번만 반영하는지 검증합니다.
        [Test]
        public void RealTimeBattle_SubmitsExternalResultOnce()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.UseStrategicBattleFallback = false;
            PrepareValdorAttackOnAvalon(state);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            WIBattleSessionState session = state.BattleSessions.First(item => item.PlayerInvolved);
            Assert.AreEqual(WIBattleSessionStatus.Pending, session.Status);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginRealTimeBattle(state, session.SessionId));
            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(
                database, state, session.SessionId, WIBattleOutcome.Victory,
                WIBattleResolutionSource.RealTimeBattle, new WITurnSummary()));
            Assert.IsFalse(WIAdministrationTurnSystem.SubmitBattleResult(
                database, state, session.SessionId, WIBattleOutcome.Defeat,
                WIBattleResolutionSource.RealTimeBattle, new WITurnSummary()));
            Assert.AreEqual(WIBattleSessionStatus.Resolved, session.Status);
            Assert.AreEqual(WIBattleResolutionSource.RealTimeBattle, session.ResolutionSource);
            Assert.AreEqual("valdor", state.GetCastle("castle_00").FactionId);
        }

        // 기본 설정에서 플레이어 참가 전투가 자동 판정되지 않고 전투 씬 진입을 기다리는지 검증합니다.
        [Test]
        public void PlayerBattle_RemainsPendingForRealTimeScene()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            PrepareValdorAttackOnAvalon(state);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            WIBattleSessionState session = state.BattleSessions.First(item => item.PlayerInvolved);
            Assert.IsTrue(session.PlayerInvolved);
            Assert.AreEqual(WIBattleSessionStatus.Pending, session.Status);
            Assert.IsTrue(state.Armies.First(army => army.ArmyId == session.AttackerArmyId).AwaitingBattle);
        }

        // 캠페인 서비스가 같은 상태 인스턴스를 반복 반환해 씬 복귀 후 데이터가 유지되는지 검증합니다.
        [Test]
        public void CampaignRuntimeService_PreservesStateInstance()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            GameObject serviceObject = new GameObject("CampaignRuntimeTest");
            WICampaignRuntimeService service = serviceObject.AddComponent<WICampaignRuntimeService>();

            WIAdministrationState first = service.GetOrCreateState(database);
            first.Gold += 321;
            WIAdministrationState second = service.GetOrCreateState(database);

            Assert.AreSame(first, second);
            Assert.AreEqual(first.Gold, second.Gold);
            Object.DestroyImmediate(serviceObject);
        }

        // 캠페인의 자원, 성, 부대와 전투 세션이 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void CampaignSave_RoundTripPreservesRuntimeState()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState original = WIAdministrationState.Create(database);
            original.Gold += 777;
            original.GetCastle("castle_00").Stability = 83;
            original.Armies.Add(new WIArmyState
            {
                ArmyId = "army_save_test",
                FactionId = "avalon",
                CurrentCastleId = "castle_00",
                ReorganizationMonths = 1
            });
            original.BattleSessions.Add(CreateSimpleBattleSession());

            string json = WICampaignSaveSystem.Serialize(original);
            bool loaded = WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState restored, out string error);

            Assert.IsTrue(loaded, error);
            Assert.AreEqual(original.Gold, restored.Gold);
            Assert.AreEqual(83, restored.GetCastle("castle_00").Stability);
            Assert.AreEqual("army_save_test", restored.Armies.Single().ArmyId);
            Assert.AreEqual("battle_test", restored.BattleSessions.Single().SessionId);
        }

        // 손상되거나 캠페인이 없는 JSON을 안전하게 거부하는지 검증합니다.
        [Test]
        public void CampaignSave_RejectsInvalidJson()
        {
            Assert.IsFalse(WICampaignSaveSystem.TryDeserialize("{broken", out WIAdministrationState state, out string error));
            Assert.IsNull(state);
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        // 현재 실행 버전보다 새로운 저장 파일을 잘못 불러오지 않는지 검증합니다.
        [Test]
        public void CampaignSave_RejectsFutureVersion()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            string json = WICampaignSaveSystem.Serialize(WIAdministrationState.Create(database));
            json = json.Replace($"\"Version\": {WICampaignSaveSystem.CurrentVersion}", "\"Version\": 999");

            Assert.IsFalse(WICampaignSaveSystem.TryDeserialize(json, out _, out string error));
            StringAssert.Contains("미래", error);
        }

        // 오디오, 화면, 프레임과 언어 설정이 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void SystemSettings_RoundTripPreservesValues()
        {
            WISystemSettingsState original = new WISystemSettingsState
            {
                MasterVolume = 0.35f,
                MusicVolume = 0.45f,
                SfxVolume = 0.55f,
                Fullscreen = false,
                TargetFrameRate = 120,
                UseEnglish = true
            };

            string json = WISystemSettingsService.Serialize(original);
            Assert.IsTrue(WISystemSettingsService.TryDeserialize(json, out WISystemSettingsState restored));
            Assert.AreEqual(original.MasterVolume, restored.MasterVolume);
            Assert.AreEqual(original.MusicVolume, restored.MusicVolume);
            Assert.AreEqual(original.SfxVolume, restored.SfxVolume);
            Assert.AreEqual(original.Fullscreen, restored.Fullscreen);
            Assert.AreEqual(120, restored.TargetFrameRate);
            Assert.IsTrue(restored.UseEnglish);
        }

        // 손상되거나 미래 버전인 시스템 설정을 안전하게 거부하는지 검증합니다.
        [Test]
        public void SystemSettings_RejectsInvalidAndFutureData()
        {
            Assert.IsFalse(WISystemSettingsService.TryDeserialize("{broken", out _));
            Assert.IsFalse(WISystemSettingsService.TryDeserialize("{\"Version\":999}", out _));
        }

        // 연구 시작 시 마나와 담당자를 점유하고 기간 종료 후 완료 목록에 기록하는지 검증합니다.
        [Test]
        public void Research_ConsumesManaAndCompletesAfterTurn()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.GetCastle("castle_00").GovernorHeroId = string.Empty;
            int manaBefore = state.ManaCrystal;

            Assert.IsTrue(WIAdministrationTurnSystem.BeginResearch(
                database, state, state.PlayerFactionId, "mana_circulation", "ares"));
            Assert.Less(state.ManaCrystal, manaBefore);
            Assert.IsTrue(state.IsCharacterBusy("ares"));

            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            Assert.Contains("mana_circulation", state.GetPlayerFactionState().CompletedResearchIds);
            Assert.IsFalse(state.IsCharacterBusy("ares"));
        }

        // 선행 연구를 완료하지 않은 고급 연구를 시작할 수 없는지 검증합니다.
        [Test]
        public void Research_RejectsMissingPrerequisite()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.GetCastle("castle_00").Technology = 100;
            state.GetCastle("castle_00").GovernorHeroId = string.Empty;

            Assert.IsFalse(WIAdministrationTurnSystem.BeginResearch(
                database, state, state.PlayerFactionId, "arcane_administration", "ares"));
        }

        // 완료 연구의 성별 마나 수입 효과가 다음 턴 경제에 적용되는지 검증합니다.
        [Test]
        public void Research_AppliesFactionIncomeBonus()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState baseline = WIAdministrationState.Create(database);
            WIAdministrationState researched = WIAdministrationState.Create(database);
            researched.GetPlayerFactionState().CompletedResearchIds.Add("mana_circulation");

            WIAdministrationTurnSystem.ExecuteTurn(database, baseline);
            WIAdministrationTurnSystem.ExecuteTurn(database, researched);

            Assert.AreEqual(baseline.ManaCrystal + 2, researched.ManaCrystal);
        }

        // 공적과 영향력을 지불한 작위가 인물 상태와 부대 전투력에 반영되는지 검증합니다.
        [Test]
        public void Title_AwardConsumesInfluenceAndAddsBattlePower()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.GetCastle("castle_00").GovernorHeroId = string.Empty;
            WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, state.GetCastle("castle_00"), "ares");
            state.GetCharacter("ares").Merit = 30;
            state.GetCharacter("ares").LoyaltyState = WILoyaltyState.Unsettled;
            int influenceBefore = state.Influence;
            int powerBefore = WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army);

            Assert.IsTrue(WIAdministrationTurnSystem.AwardTitle(
                database, state, state.PlayerFactionId, "ares", "knight"));

            Assert.AreEqual("knight", state.GetCharacter("ares").TitleId);
            Assert.AreEqual(WILoyaltyState.Stable, state.GetCharacter("ares").LoyaltyState);
            Assert.Less(state.Influence, influenceBefore);
            Assert.AreEqual(powerBefore + 8, WIAdministrationTurnSystem.GetArmyBattlePower(database, state, army));
        }

        // AI도 플레이어와 같은 연구 조건과 세력 마나를 사용해 연구를 완료하는지 검증합니다.
        [Test]
        public void AIResearch_UsesSharedResearchRules()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.IsTrue(state.GetFactionState("valdor").CompletedResearchIds.Count > 0);
            Assert.IsTrue(string.IsNullOrEmpty(state.GetFactionState("valdor").ActiveResearchId));
        }

        // 전투 세션 참가자가 진영과 역할에 맞는 초기 진형으로 변환되는지 검증합니다.
        [Test]
        public void BattleRuntimeBuilder_CreatesRoleBasedFormation()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleSessionState session = CreateSimpleBattleSession();

            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, session);

            Assert.AreEqual(2, runtime.Characters.Count);
            Assert.Less(runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker).Position.x, 0f);
            Assert.Greater(runtime.Characters.Single(item => item.Side == WIBattleSide.Defender).Position.x, 0f);
            Assert.IsTrue(runtime.Characters.All(item => item.Health == item.MaxHealth && item.Mana == item.MaxMana));
        }

        // 기본 이동과 공격 루프가 정지하지 않고 한 진영의 승패를 결정하는지 검증합니다.
        [Test]
        public void BattleSimulation_CompletesBasicCombatLoop()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());

            for (int index = 0; index < 2000 && runtime.Finished == false; index += 1)
            {
                WIBattleSimulation.Step(config, runtime, 0.1f);
            }

            Assert.IsTrue(runtime.Finished);
            Assert.AreNotEqual(WIBattleOutcome.None, runtime.AttackerOutcome);
            Assert.IsTrue(runtime.Characters.Any(item => item.IsAlive == false));
        }

        // 위치 사수와 전진 명령이 캐릭터 이동 방식에 다르게 반영되는지 검증합니다.
        [Test]
        public void BattleCommand_HoldStopsMovementAndAdvanceMoves()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker);
            Vector2 startingPosition = attacker.Position;

            runtime.AttackerCommand = WIBattleCommand.Hold;
            WIBattleSimulation.Step(config, runtime, 0.5f);
            Assert.AreEqual(startingPosition, attacker.Position);

            runtime.AttackerCommand = WIBattleCommand.Advance;
            WIBattleSimulation.Step(config, runtime, 0.5f);
            Assert.AreNotEqual(startingPosition, attacker.Position);
        }

        // 같은 위치에 겹친 인물들이 최소 간격에 가깝게 분리되는지 검증합니다.
        [Test]
        public void BattleCollision_SeparatesOverlappingCharacters()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker);
            WIBattleCharacterState defender = runtime.Characters.Single(item => item.Side == WIBattleSide.Defender);
            attacker.Position = Vector2.zero;
            defender.Position = Vector2.zero;
            attacker.CooldownRemaining = 10f;
            defender.CooldownRemaining = 10f;

            WIBattleSimulation.Step(config, runtime, 0.01f);

            Assert.Greater(Vector2.Distance(attacker.Position, defender.Position), 0f);
            Assert.GreaterOrEqual(
                Vector2.Distance(attacker.Position, defender.Position),
                config.MinimumUnitSpacing * config.CollisionResolveStrength - 0.01f);
        }

        // 근접 공격이 대상을 밀어내고 사수 명령이 밀치기 거리를 줄이는지 검증합니다.
        [Test]
        public void BattleMeleeHit_AppliesKnockbackAndHoldResistance()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker);
            WIBattleCharacterState defender = runtime.Characters.Single(item => item.Side == WIBattleSide.Defender);
            attacker.Position = Vector2.zero;
            defender.Position = Vector2.right;
            defender.FormationPosition = defender.Position;
            runtime.DefenderCommand = WIBattleCommand.Advance;

            WIBattleSimulation.Step(config, runtime, 0.01f);
            float advanceDisplacement = defender.Position.x - 1f;

            defender.Health = defender.MaxHealth;
            defender.Position = Vector2.right;
            attacker.CooldownRemaining = 0f;
            runtime.DefenderCommand = WIBattleCommand.Hold;
            WIBattleSimulation.Step(config, runtime, 0.01f);
            float holdDisplacement = defender.Position.x - 1f;

            Assert.Greater(advanceDisplacement, 0f);
            Assert.Greater(holdDisplacement, 0f);
            Assert.Less(holdDisplacement, advanceDisplacement);
        }

        // 전진 중 측면으로 이탈한 인물이 자신의 진형 행으로 복귀하는지 검증합니다.
        [Test]
        public void BattleAdvance_ReturnsCharacterTowardFormationRow()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker);
            attacker.Position += Vector2.up * 3f;
            float distanceBefore = Mathf.Abs(attacker.Position.y - attacker.FormationPosition.y);

            WIBattleSimulation.Step(config, runtime, 0.25f);

            Assert.Less(Mathf.Abs(attacker.Position.y - attacker.FormationPosition.y), distanceBefore);
        }

        // 영웅 스킬이 설정된 마나, 피해와 재사용 대기시간을 적용하는지 검증합니다.
        [Test]
        public void HeroSkill_ConsumesManaAndDamagesEnemy()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState caster = runtime.Characters.Single(item => item.HeroId == "ares");
            WIBattleCharacterState enemy = runtime.Characters.Single(item => item.HeroId == "lyria");
            enemy.Position = caster.Position + Vector2.right;
            int manaBefore = caster.Mana;
            int healthBefore = enemy.Health;

            Assert.IsTrue(WIBattleSimulation.TryActivateHeroSkill(config, runtime, "ares"));
            Assert.Less(caster.Mana, manaBefore);
            Assert.Less(enemy.Health, healthBefore);
            Assert.Greater(caster.SkillCooldownRemaining, 0f);
            Assert.IsFalse(WIBattleSimulation.TryActivateHeroSkill(config, runtime, "ares"));
        }

        // 후퇴 명령이 즉시 상대 진영의 승리로 전투를 종료하는지 검증합니다.
        [Test]
        public void BattleCommand_RetreatEndsBattle()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            runtime.AttackerCommand = WIBattleCommand.Retreat;

            WIBattleOutcome outcome = WIBattleSimulation.Step(config, runtime, 0.1f);

            Assert.AreEqual(WIBattleOutcome.Defeat, outcome);
            Assert.IsTrue(runtime.Finished);
        }

        // 데이터베이스가 영웅과 구분되는 일반 인물 원본 데이터를 포함하는지 검증합니다.
        [Test]
        public void CommonRoster_LoadsSeparateCharacterGrade()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            Assert.AreEqual(6, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common));
            Assert.AreEqual(WICharacterGrade.Common, state.GetCharacter("common_gareth").BaseGrade);
            Assert.IsFalse(state.GetCharacter("common_gareth").PromotedToHero);
        }

        // 공적과 명성뿐 아니라 특별 성취까지 갖춘 일반 인물만 승격 후보가 되는지 검증합니다.
        [Test]
        public void ExecuteTurn_RegistersQualifiedCommonPromotionCandidate()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICharacterRuntimeState character = state.GetCharacter("common_gareth");
            character.Discovered = true;
            character.Recruited = true;
            character.Merit = database.PromotionRequiredMerit;
            character.Reputation = database.PromotionRequiredReputation;
            character.PromotionAchievement = true;

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.Contains(character.HeroId, state.PendingHeroPromotionIds);
        }

        // 승격 확정 시 영향력을 소비하고 인물의 유효 등급을 영웅으로 바꾸는지 검증합니다.
        [Test]
        public void PromoteCommonCharacter_ConsumesInfluenceAndPromotes()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICharacterRuntimeState character = state.GetCharacter("common_gareth");
            character.Discovered = true;
            character.Recruited = true;
            state.PendingHeroPromotionIds.Add(character.HeroId);
            int influenceBefore = state.GetPlayerFactionState().Influence;

            bool promoted = WIAdministrationTurnSystem.PromoteCommonCharacter(database, state, character.HeroId);

            Assert.IsTrue(promoted);
            Assert.IsTrue(character.PromotedToHero);
            Assert.AreEqual(influenceBefore - database.PromotionInfluenceCost, state.GetPlayerFactionState().Influence);
            Assert.IsFalse(state.PendingHeroPromotionIds.Contains(character.HeroId));
        }

        // 유휴 인물이 같은 세력의 인접 성으로 이동하고 다음 달에 도착하는지 검증합니다.
        [Test]
        public void CharacterTransfer_ArrivesAtAdjacentFriendlyCastle()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState origin = state.GetCastle("castle_00");
            string targetId = database.GetCastle(origin.CastleId).AdjacentCastleIds.First();
            WICastleRuntimeState target = state.GetCastle(targetId);
            target.FactionId = origin.FactionId;
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.HeroIds.Remove("ares");
                if (castle.GovernorHeroId == "ares") castle.GovernorHeroId = string.Empty;
            }
            origin.HeroIds.Add("ares");

            Assert.IsTrue(WIAdministrationTurnSystem.StartCharacterTransfer(database, state, "ares", targetId));
            Assert.IsTrue(state.IsCharacterBusy("ares"));
            Assert.IsFalse(origin.HeroIds.Contains("ares"));

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.IsTrue(target.HeroIds.Contains("ares"));
            Assert.IsFalse(state.CharacterTransfers.Any(transfer => transfer.HeroId == "ares"));
        }

        // 적대 성으로의 개인 이동과 이동 중인 인물의 중복 명령을 거부하는지 검증합니다.
        [Test]
        public void CharacterTransfer_RejectsEnemyCastleAndDuplicateOrder()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState origin = state.GetCastle("castle_00");
            string targetId = database.GetCastle(origin.CastleId).AdjacentCastleIds.First();
            WICastleRuntimeState target = state.GetCastle(targetId);
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.HeroIds.Remove("ares");
                if (castle.GovernorHeroId == "ares") castle.GovernorHeroId = string.Empty;
            }
            origin.HeroIds.Add("ares");
            target.FactionId = "valdor";
            Assert.IsFalse(WIAdministrationTurnSystem.StartCharacterTransfer(database, state, "ares", targetId));

            target.FactionId = origin.FactionId;
            Assert.IsTrue(WIAdministrationTurnSystem.StartCharacterTransfer(database, state, "ares", targetId));
            Assert.IsFalse(WIAdministrationTurnSystem.BeginResearch(
                database, state, state.PlayerFactionId, "mana_circulation", "ares"));
        }

        // 활동 중인 인물은 태수가 될 수 없고 태수는 다른 임무에 중복 배정되지 않는지 검증합니다.
        [Test]
        public void GovernorAssignment_EnforcesExclusiveDuty()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState castle = state.GetCastle("castle_00");
            WICharacterRuntimeState character = state.GetCharacter("ares");
            castle.GovernorHeroId = string.Empty;
            character.Activity = WICharacterActivityType.Training;

            Assert.IsFalse(WIAdministrationTurnSystem.AssignGovernor(state, castle.CastleId, "ares"));
            character.Activity = WICharacterActivityType.None;
            Assert.IsTrue(WIAdministrationTurnSystem.AssignGovernor(state, castle.CastleId, "ares"));
            Assert.AreEqual("ares", castle.GovernorHeroId);
            Assert.IsFalse(WIAdministrationTurnSystem.AssignGovernor(state, castle.CastleId, "lyria"));
        }

        // 보유 자원이 모두 0이어도 다음 달 기본 성 수입으로 다시 회복되는지 검증합니다.
        [Test]
        public void ResourceDepletion_RecoversFromZeroOnNextTurn()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIFactionRuntimeState player = state.GetPlayerFactionState();
            player.Gold = 0;
            player.ManaCrystal = 0;
            player.Influence = 0;
            WITurnSummary forecast = WIAdministrationTurnSystem.GetFactionMonthlyIncome(
                database, state, state.PlayerFactionId);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.Greater(forecast.GoldGained, 0);
            Assert.Greater(forecast.ManaGained, 0);
            Assert.Greater(forecast.InfluenceGained, 0);
            Assert.AreEqual(forecast.GoldGained, player.Gold);
            Assert.AreEqual(forecast.ManaGained, player.ManaCrystal);
            Assert.AreEqual(forecast.InfluenceGained, player.Influence);
        }

        // 자원이 부족한 상태의 유료 명령이 거부되고 잔액이 음수가 되지 않는지 검증합니다.
        [Test]
        public void ResourceDepletion_BlocksPaidCommandsWithoutDebt()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIFactionRuntimeState player = state.GetPlayerFactionState();
            player.Gold = 0;
            player.ManaCrystal = 0;
            player.Influence = 0;

            Assert.IsFalse(WIAdministrationTurnSystem.ImproveDiplomaticRelations(state, state.PlayerFactionId, "valdor"));
            Assert.IsFalse(WIAdministrationTurnSystem.DeclareWar(state, state.PlayerFactionId, "ironclad"));
            Assert.IsFalse(WIAdministrationTurnSystem.BeginResearch(
                database, state, state.PlayerFactionId, "mana_circulation", "ares"));
            Assert.AreEqual(0, player.Gold);
            Assert.AreEqual(0, player.ManaCrystal);
            Assert.AreEqual(0, player.Influence);
        }

        // 모든 세력이 자원 0에서 턴을 시작해도 수입 처리 후 음수 잔액이 남지 않는지 검증합니다.
        [Test]
        public void ResourceDepletion_AllFactionsRemainSolventAfterTurn()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            foreach (WIFactionRuntimeState faction in state.Factions)
            {
                faction.Gold = 0;
                faction.ManaCrystal = 0;
                faction.Influence = 0;
            }

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.IsTrue(state.Factions.All(faction => faction.Gold >= 0));
            Assert.IsTrue(state.Factions.All(faction => faction.ManaCrystal >= 0));
            Assert.IsTrue(state.Factions.All(faction => faction.Influence >= 0));
            Assert.Greater(state.GetPlayerFactionState().Gold, 0);
            Assert.Greater(state.GetPlayerFactionState().ManaCrystal, 0);
            Assert.Greater(state.GetPlayerFactionState().Influence, 0);
        }

        // 플레이어 인물이 모두 임무 중이어도 턴이 진행되고 월간 활동 완료 후 다시 대기 상태가 되는지 검증합니다.
        [Test]
        public void AllPlayerCharactersBusy_TurnAdvancesAndReleasesMonthlyActivities()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            List<string> playerCharacterIds = state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId)
                .SelectMany(castle => castle.HeroIds)
                .Distinct()
                .ToList();
            Assert.IsNotEmpty(playerCharacterIds);
            foreach (string heroId in playerCharacterIds)
            {
                state.GetCharacter(heroId).Activity = WICharacterActivityType.Training;
            }
            Assert.IsTrue(playerCharacterIds.All(state.IsCharacterBusy));
            int turnBefore = state.Turn;

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(turnBefore + 1, state.Turn);
            Assert.IsTrue(playerCharacterIds.All(heroId =>
                state.GetCharacter(heroId).Activity == WICharacterActivityType.None));
            Assert.IsTrue(playerCharacterIds.All(heroId => state.IsCharacterBusy(heroId) == false));
        }

        // 모든 인물이 임무 중일 때 연구·이동·부대 명령이 중복 배정 없이 거부되는지 검증합니다.
        [Test]
        public void AllPlayerCharactersBusy_RejectsDuplicateAssignments()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState origin = state.GetCastle("castle_00");
            List<string> playerCharacterIds = state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId)
                .SelectMany(castle => castle.HeroIds)
                .Distinct()
                .ToList();
            foreach (string heroId in playerCharacterIds)
            {
                state.GetCharacter(heroId).Activity = WICharacterActivityType.Training;
            }
            origin.GovernorHeroId = string.Empty;
            string destinationId = database.GetCastle(origin.CastleId).AdjacentCastleIds.First();
            state.GetCastle(destinationId).FactionId = state.PlayerFactionId;
            int armyCountBefore = state.Armies.Count;
            int manaBefore = state.ManaCrystal;

            Assert.IsFalse(WIAdministrationTurnSystem.BeginResearch(
                database, state, state.PlayerFactionId, "mana_circulation", "ares"));
            Assert.IsFalse(WIAdministrationTurnSystem.StartCharacterTransfer(
                database, state, "ares", destinationId));
            Assert.IsNull(WIAdministrationTurnSystem.CreateArmy(database, state, origin, "ares"));
            Assert.AreEqual(armyCountBefore, state.Armies.Count);
            Assert.AreEqual(manaBefore, state.ManaCrystal);
            Assert.IsTrue(playerCharacterIds.All(state.IsCharacterBusy));
        }

        // 전투 런타임 단위 테스트에 사용할 최소 참가자 세션을 생성합니다.
        private static WIBattleSessionState CreateSimpleBattleSession()
        {
            WIBattleSessionState session = new WIBattleSessionState { SessionId = "battle_test" };
            session.AttackerHeroIds.Add(new WIBattleParticipantState
            {
                HeroId = "ares",
                ArmyId = "army_attacker",
                Role = WIUnitRole.Commander
            });
            session.DefenderHeroIds.Add(new WIBattleParticipantState
            {
                HeroId = "lyria",
                ArmyId = "army_defender",
                Role = WIUnitRole.Vanguard
            });
            return session;
        }

        // 발도르가 첫 턴에 아발론을 공격하도록 전선 조건을 고정합니다.
        private static void PrepareValdorAttackOnAvalon(WIAdministrationState state)
        {
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == "valdor"))
            {
                castle.Defense = castle.CastleId == "castle_01" ? 0 : 100;
                castle.Stability = castle.CastleId == "castle_01" ? 0 : 100;
            }
        }
    }
}
