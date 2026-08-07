using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Battle;
using ProjectWI.Systems;
using ProjectWI.Editor;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIAdministrationAITurnTests
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private const string BattleConfigPath = "Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset";

        // 시작 영웅이 있는 수도에서 출정 창의 부대 편성 경로를 거쳐 인접 적 성으로 이동할 수 있는지 검증합니다.
        [Test]
        public void StartingCampaign_CanCreateArmyAndMarchFromCapital()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState capital = state.GetCastle("castle_00");
            Assert.GreaterOrEqual(capital.HeroIds.Count, 1);
            Assert.AreEqual(0, state.Armies.Count(army => army.FactionId == state.PlayerFactionId));

            WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, capital, "ares");
            Assert.IsNotNull(army);
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_01"));
            Assert.Greater(army.RemainingTravelMonths, 0);
            Assert.AreEqual("castle_01", army.TargetCastleId);
        }

        // 플레이어 출정 부대가 적 성에 도착한 턴에 실시간 전투 진입용 대기 세션이 생성되는지 검증합니다.
        [Test]
        public void PlayerMarch_ArrivalCreatesPendingRealtimeBattle()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, state.GetCastle("castle_00"), "ares");
            Assert.IsTrue(WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, "castle_01"));

            while (army.IsMoving) WIAdministrationTurnSystem.ExecuteTurn(database, state);

            WIBattleSessionState session = state.BattleSessions.Single(item => item.AttackerArmyId == army.ArmyId);
            Assert.IsTrue(session.PlayerInvolved);
            Assert.AreEqual(WIBattleSessionStatus.Pending, session.Status);
            Assert.IsTrue(state.UsePlayerRealTimeBattles);
            Assert.IsTrue(state.UseStrategicBattleFallback);
        }

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

        // 난이도별 AI 후보 범위가 여유 3·표준 2·도전 1로 실제 선택 함수에 적용되는지 검증합니다.
        [Test]
        public void CampaignDifficulty_ControlsAICandidateQualityWindow()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState relaxed = WIAdministrationState.Create(database, WICampaignDifficulty.Relaxed);
            WIAdministrationState standard = WIAdministrationState.Create(database, WICampaignDifficulty.Standard);
            WIAdministrationState hard = WIAdministrationState.Create(database, WICampaignDifficulty.Hard);

            Assert.AreEqual(3, WIAdministrationTurnSystem.GetAICandidateWindow(database, relaxed, 5));
            Assert.AreEqual(2, WIAdministrationTurnSystem.GetAICandidateWindow(database, standard, 5));
            Assert.AreEqual(1, WIAdministrationTurnSystem.GetAICandidateWindow(database, hard, 5));
            Assert.AreEqual(0, WIAdministrationTurnSystem.GetAICandidateIndex(database, hard, 5, 99));
        }

        // 동일 턴과 소금값에서는 선택이 재현되며 각 난이도의 상위 후보 범위를 벗어나지 않는지 검증합니다.
        [Test]
        public void CampaignDifficulty_AICandidateSelectionIsDeterministic()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            foreach (WICampaignDifficulty difficulty in System.Enum.GetValues(typeof(WICampaignDifficulty)))
            {
                WIAdministrationState state = WIAdministrationState.Create(database, difficulty);
                state.Turn = 7;
                int first = WIAdministrationTurnSystem.GetAICandidateIndex(database, state, 6, 4);
                int second = WIAdministrationTurnSystem.GetAICandidateIndex(database, state, 6, 4);
                Assert.AreEqual(first, second);
                Assert.Less(first, WIAdministrationTurnSystem.GetAICandidateWindow(database, state, 6));
            }
        }

        // 모략 AI가 도전에서는 최저 치안 1순위, 여유에서는 상위 후보 범위의 다른 목표도 선택하는지 검증합니다.
        [Test]
        public void CampaignDifficulty_ChangesSchemeTargetQualityWithoutResourceBonus()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState relaxed = WIAdministrationState.Create(database, WICampaignDifficulty.Relaxed);
            WIAdministrationState hard = WIAdministrationState.Create(database, WICampaignDifficulty.Hard);
            WITurnSummary relaxedSummary = new WITurnSummary();
            WITurnSummary hardSummary = new WITurnSummary();

            WISchemeSystem.ScheduleAISchemes(database, relaxed, relaxedSummary);
            WISchemeSystem.ScheduleAISchemes(database, hard, hardSummary);

            WISchemeMissionState relaxedMission = relaxed.SchemeMissions.Single();
            WISchemeMissionState hardMission = hard.SchemeMissions.Single();
            Assert.AreNotEqual(relaxedMission.TargetCastleId, hardMission.TargetCastleId);
            Assert.GreaterOrEqual(relaxed.GetCastle(relaxedMission.TargetCastleId).Stability,
                hard.GetCastle(hardMission.TargetCastleId).Stability);
            Assert.AreEqual(relaxed.GetFactionState("necropolis").Influence + database.GetScheme("scheme_rumor").InfluenceCost,
                hard.GetFactionState("necropolis").Influence + database.GetScheme("scheme_rumor").InfluenceCost);
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
            Assert.AreEqual(100, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Hero));
            Assert.AreEqual(400, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common));
            Assert.AreEqual(12, System.Enum.GetValues(typeof(WIHeroClass)).Length);
            Assert.AreEqual(6, System.Enum.GetValues(typeof(WIUnitRole)).Length);
            TestContext.WriteLine("영웅 100 · 일반 인물 400 · 클래스 12 · 부대 역할 6 · 병사/병종 데이터 없음(기획 의도)");
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

        // 영토가 사라진 세력이 한 번만 멸망하고 연구·부대·계략·외교 약속이 정리되는지 검증합니다.
        [Test]
        public void FactionElimination_CleansRuntimeActionsAndReportsOnce()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == "valdor"))
                castle.FactionId = "ironheart";
            WIFactionRuntimeState valdor = state.GetFactionState("valdor");
            valdor.ActiveResearchId = database.ResearchDefinitions[0].Id;
            valdor.ResearcherHeroId = "lyria";
            valdor.ResearchRemainingMonths = 2;
            state.Armies.Add(new WIArmyState
            {
                ArmyId = "last_valdor_army", FactionId = "valdor", CurrentCastleId = "castle_01",
                Members = new List<WIArmyMemberState> { new WIArmyMemberState { HeroId = "lyria", Role = WIUnitRole.Commander } }
            });
            state.SchemeMissions.Add(new WISchemeMissionState { SchemeId = "scheme_rumor", InitiatorFactionId = "valdor", AgentHeroId = "lyria" });
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation("valdor", "ironheart");
            relation.JointAttackTargetCastleId = "castle_00";
            relation.JointAttackMonthsRemaining = 3;
            WITurnSummary first = new WITurnSummary();

            WIAdministrationTurnSystem.ResolveFactionEliminations(database, state, first);
            WIAdministrationTurnSystem.ResolveFactionEliminations(database, state, first);

            Assert.IsTrue(valdor.Eliminated);
            Assert.AreEqual(state.Turn, valdor.EliminatedTurn);
            Assert.IsTrue(string.IsNullOrEmpty(valdor.ActiveResearchId));
            Assert.IsFalse(state.Armies.Any(item => item.FactionId == "valdor"));
            Assert.IsFalse(state.SchemeMissions.Any(item => item.InitiatorFactionId == "valdor"));
            Assert.AreEqual(0, relation.JointAttackMonthsRemaining);
            Assert.AreEqual(1, first.News.Count(item => item.Contains("세력 멸망")));
            Assert.IsFalse(state.GetCharacter("lyria").Recruited);
        }

        // 멸망 세력과 최초 발생 턴이 저장·불러오기 후에도 유지되는지 검증합니다.
        [Test]
        public void FactionElimination_SaveRoundTripPreservesState()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIFactionRuntimeState faction = state.GetFactionState("valdor");
            faction.Eliminated = true;
            faction.EliminatedTurn = 27;

            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.IsTrue(loaded.GetFactionState("valdor").Eliminated);
            Assert.AreEqual(27, loaded.GetFactionState("valdor").EliminatedTurn);
        }

        // 세 난이도의 60·120개월 자동 진행에서 멸망 상태와 런타임 참조 무결성이 유지되는지 검증합니다.
        [TestCase(WICampaignDifficulty.Relaxed, 60)]
        [TestCase(WICampaignDifficulty.Standard, 60)]
        [TestCase(WICampaignDifficulty.Hard, 60)]
        [TestCase(WICampaignDifficulty.Relaxed, 120)]
        [TestCase(WICampaignDifficulty.Standard, 120)]
        [TestCase(WICampaignDifficulty.Hard, 120)]
        public void CampaignSimulation_LongRunPreservesEliminationIntegrity(WICampaignDifficulty difficulty, int months)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database, difficulty);
            state.UseStrategicBattleFallback = true;
            state.UsePlayerRealTimeBattles = false;

            for (int month = 0; month < months; month += 1)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
                AssertCampaignIntegrity(database, state, $"{difficulty} {months}개월 중 {month + 1}");
            }

            foreach (WIFactionRuntimeState faction in state.Factions)
            {
                bool hasTerritory = state.Castles.Any(castle => castle.FactionId == faction.FactionId);
                Assert.AreEqual(hasTerritory == false, faction.Eliminated, faction.FactionId);
                if (faction.Eliminated) Assert.IsFalse(state.Armies.Any(army => army.FactionId == faction.FactionId));
            }
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
            Assert.IsTrue(WIInformationVisibility.CanViewCastleDetails(state, "avalon", state.GetCastle("castle_01")));
        }

        // 계략 예약이 영향력을 즉시 소비하고 담당 인물을 판정 전까지 점유하는지 검증합니다.
        [Test]
        public void SchemeMission_ScheduleConsumesInfluenceAndOccupiesAgent()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WISchemeDefinition scheme = database.GetScheme("scheme_investigation");
            int influenceBefore = state.GetFactionState("avalon").Influence;

            bool scheduled = WISchemeSystem.TrySchedule(database, state, scheme.Id, "avalon", "ares",
                "castle_01", null, 0, out string message);

            Assert.IsTrue(scheduled, message);
            Assert.AreEqual(influenceBefore - scheme.InfluenceCost, state.GetFactionState("avalon").Influence);
            Assert.AreEqual(1, state.SchemeMissions.Single().RemainingMonths);
            Assert.IsTrue(state.IsCharacterBusy("ares"));
            Assert.IsEmpty(state.SchemeIntel);
        }

        // 한 달이 지난 계략을 판정해 정보를 획득하고 담당 인물을 복귀시키는지 검증합니다.
        [Test]
        public void SchemeMission_ResolvesAfterOneMonthAndReleasesAgent()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            Assert.IsTrue(WISchemeSystem.TrySchedule(database, state, "scheme_investigation", "avalon", "ares",
                "castle_01", null, 0, out string message), message);
            WITurnSummary summary = new WITurnSummary();

            WISchemeSystem.ResolveScheduledMissions(database, state, summary);

            Assert.IsEmpty(state.SchemeMissions);
            Assert.IsFalse(state.IsCharacterBusy("ares"));
            Assert.AreEqual(database.GetScheme("scheme_investigation").DurationMonths, state.SchemeIntel.Single().RemainingMonths);
            Assert.IsTrue(summary.News.Any(item => item.Contains("계략 결과")));
        }

        // 진행 중인 계략의 담당 인물·대상·남은 기간이 저장 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void SchemeMission_SaveRoundTripPreservesAssignment()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            Assert.IsTrue(WISchemeSystem.TrySchedule(database, state, "scheme_investigation", "avalon", "ares",
                "castle_01", null, 17, out string message), message);

            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);

            WISchemeMissionState mission = loaded.SchemeMissions.Single();
            Assert.AreEqual("ares", mission.AgentHeroId);
            Assert.AreEqual("castle_01", mission.TargetCastleId);
            Assert.AreEqual(1, mission.RemainingMonths);
            Assert.AreEqual(17, mission.ResolutionRoll);
            Assert.IsTrue(loaded.IsCharacterBusy("ares"));
        }

        // 계략별 발각 기본값과 실패 가산치가 ScriptableObject 데이터에 구성됐는지 검증합니다.
        [Test]
        public void SchemeDefinitions_DefineDetectionBalance()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            foreach (WISchemeDefinition scheme in database.SchemeDefinitions)
            {
                if (scheme.SchemeType == WISchemeType.Counterintelligence)
                {
                    Assert.AreEqual(0, scheme.BaseDetectionChance);
                    Assert.AreEqual(0, scheme.FailureDetectionBonus);
                    continue;
                }
                Assert.Greater(scheme.BaseDetectionChance, 0, scheme.Id);
                Assert.Greater(scheme.FailureDetectionBonus, 0, scheme.Id);
            }
        }

        // 실패한 적대 계략이 발각되면 성공 효과 없이 외교 관계가 한 단계 악화되는지 검증합니다.
        [Test]
        public void Scheme_FailedDetectionWorsensDiplomaticRelation()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState target = state.GetCastle("castle_01");
            target.Stability = 100;
            target.CounterintelligenceMonths = 2;
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation("avalon", target.FactionId);
            relation.Status = WIDiplomaticStatus.Alliance;

            WISchemeResult result = WISchemeSystem.Execute(database, state, "scheme_investigation", "avalon",
                "ares", target.CastleId, null, 11);

            Assert.IsTrue(result.Executed);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Detected);
            Assert.Greater(result.DetectionChance, 0);
            Assert.AreEqual(WIDiplomaticStatus.NonAggression, relation.Status);
            Assert.IsEmpty(state.SchemeIntel);
        }

        // 은밀하게 성공한 계략은 효과만 적용하고 외교 관계를 유지하는지 검증합니다.
        [Test]
        public void Scheme_UndetectedSuccessPreservesDiplomaticRelation()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState target = state.GetCastle("castle_01");
            target.Stability = 0;
            target.CounterintelligenceMonths = 0;
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation("avalon", target.FactionId);
            relation.Status = WIDiplomaticStatus.Friendly;

            WISchemeResult result = WISchemeSystem.Execute(database, state, "scheme_investigation", "avalon",
                "ares", target.CastleId, null, 0);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.Detected);
            Assert.AreEqual(WIDiplomaticStatus.Friendly, relation.Status);
            Assert.IsNotEmpty(state.SchemeIntel);
        }

        // 방첩·치안은 발각률을 높이고 담당 인물 지력은 발각률을 낮추는지 검증합니다.
        [Test]
        public void Scheme_DetectionChanceUsesDefenseAndIntelligence()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WISchemeDefinition scheme = database.GetScheme("scheme_rumor");
            WIHeroDefinition agent = database.GetHero("ares");
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState target = state.GetCastle("castle_01");
            target.Stability = 20;
            target.CounterintelligenceMonths = 0;
            int unguarded = WISchemeSystem.CalculateDetectionChance(scheme, agent, target, true);
            target.Stability = 80;
            target.CounterintelligenceMonths = 2;
            int guarded = WISchemeSystem.CalculateDetectionChance(scheme, agent, target, true);

            Assert.Greater(guarded, unguarded);
            Assert.Greater(WISchemeSystem.CalculateDetectionChance(scheme, agent, target, false), guarded);
        }

        // 미조사 적 성은 상세 정보가 숨겨지고 조사 만료 후 다시 비공개가 되는지 검증합니다.
        [Test]
        public void InformationVisibility_EnemyCastleRequiresActiveInvestigation()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState enemyCastle = state.GetCastle("castle_01");
            Assert.IsFalse(WIInformationVisibility.CanViewCastleDetails(state, "avalon", enemyCastle));
            state.SchemeIntel.Add(new WISchemeIntelState
            {
                ObserverFactionId = "avalon",
                TargetCastleId = enemyCastle.CastleId,
                RemainingMonths = 1
            });
            Assert.IsTrue(WIInformationVisibility.CanViewCastleDetails(state, "avalon", enemyCastle));

            WISchemeSystem.AdvanceMonth(state);

            Assert.IsFalse(WIInformationVisibility.CanViewCastleDetails(state, "avalon", enemyCastle));
            Assert.IsEmpty(state.SchemeIntel);
        }

        // 동맹 성은 조사 없이 상세 정보가 공개되는지 검증합니다.
        [Test]
        public void InformationVisibility_AllianceSharesCastleDetails()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState alliedCastle = state.Castles.First(item => item.FactionId == "ironheart");
            state.GetOrCreateDiplomaticRelation("avalon", "ironheart").Status = WIDiplomaticStatus.Alliance;

            Assert.IsTrue(WIInformationVisibility.CanViewCastleDetails(state, "avalon", alliedCastle));
        }

        // 플레이어 참가 전투 접촉은 성 내정 수치가 아닌 해당 전장의 군사 정보만 공개하는지 검증합니다.
        [Test]
        public void InformationVisibility_BattleContactRevealsMilitaryOnly()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState enemyCastle = state.GetCastle("castle_01");
            state.BattleSessions.Add(new WIBattleSessionState
            {
                SessionId = "contact_test",
                CastleId = enemyCastle.CastleId,
                PlayerInvolved = true,
                Status = WIBattleSessionStatus.Pending
            });

            Assert.IsFalse(WIInformationVisibility.CanViewCastleDetails(state, "avalon", enemyCastle));
            Assert.IsTrue(WIInformationVisibility.CanViewMilitaryDetails(state, "avalon", enemyCastle));
        }

        // 조사 정보의 관찰 세력·대상 성·남은 기간이 저장 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void InformationVisibility_InvestigationSaveRoundTripPreservesAccess()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.SchemeIntel.Add(new WISchemeIntelState
            {
                ObserverFactionId = "avalon",
                TargetCastleId = "castle_01",
                RemainingMonths = 2
            });
            string json = WICampaignSaveSystem.Serialize(state);

            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.IsTrue(WIInformationVisibility.CanViewCastleDetails(loaded, "avalon", loaded.GetCastle("castle_01")));
            Assert.AreEqual(2, loaded.SchemeIntel.Single().RemainingMonths);
        }

        // 방첩이 적 계략 성공률을 낮추고 월간 진행 후 만료되는지 검증합니다.
        [Test]
        public void Scheme_CounterintelligenceReducesChanceAndExpires()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WISchemeResult defense = WISchemeSystem.Execute(database, state, "scheme_counterintelligence", "avalon", "ares", "castle_00", null, 99);
            Assert.IsTrue(defense.Succeeded);

            WISchemeResult blocked = WISchemeSystem.Execute(database, state, "scheme_rumor", "valdor", "elwyn", "castle_00", null, 89);
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
            Assert.IsTrue(WISchemeSystem.Execute(database, state, "scheme_alienation", "avalon", "ares", "castle_01", "elwyn", 0).Succeeded);
            Assert.AreEqual(WILoyaltyState.Unsettled, state.GetCharacter("elwyn").LoyaltyState);
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

        // 원소속 세력이 몸값을 지불하면 금화가 포획 세력으로 이동하고 포로가 즉시 귀환하는지 검증합니다.
        [Test]
        public void Diplomacy_RansomTransfersGoldAndReleasesPrisoner()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICharacterRuntimeState prisoner = state.GetCharacter("ares");
            prisoner.Captured = true;
            prisoner.CapturedFromFactionId = "avalon";
            prisoner.CaptorFactionId = "valdor";
            prisoner.CapturedMonthsRemaining = 3;
            foreach (WICastleRuntimeState castle in state.Castles) castle.HeroIds.Remove("ares");
            int requesterGold = state.GetFactionState("avalon").Gold;
            int captorGold = state.GetFactionState("valdor").Gold;

            Assert.IsTrue(WIAdministrationTurnSystem.RansomPrisoner(database, state, "avalon", "ares"));

            Assert.IsFalse(prisoner.Captured);
            Assert.AreEqual(requesterGold - database.PrisonerRansomGold, state.GetFactionState("avalon").Gold);
            Assert.AreEqual(captorGold + database.PrisonerRansomGold, state.GetFactionState("valdor").Gold);
            Assert.IsTrue(state.Castles.Any(item => item.FactionId == "avalon" && item.HeroIds.Contains("ares")));
        }

        // 양측이 서로 억류한 포로를 맞교환하면 비용 없이 두 인물이 각 원소속으로 귀환하는지 검증합니다.
        [Test]
        public void Diplomacy_PrisonerExchangeReleasesBothSides()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICharacterRuntimeState ares = state.GetCharacter("ares");
            WICharacterRuntimeState lyria = state.GetCharacter("lyria");
            ares.Captured = true; ares.CapturedFromFactionId = "avalon"; ares.CaptorFactionId = "valdor";
            lyria.Captured = true; lyria.CapturedFromFactionId = "valdor"; lyria.CaptorFactionId = "avalon";
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.HeroIds.Remove("ares");
                castle.HeroIds.Remove("lyria");
            }

            Assert.IsTrue(WIAdministrationTurnSystem.ExchangePrisoners(state, "avalon", "valdor", "ares", "lyria"));
            Assert.IsFalse(ares.Captured);
            Assert.IsFalse(lyria.Captured);
            Assert.IsTrue(state.Castles.Any(item => item.FactionId == "avalon" && item.HeroIds.Contains("ares")));
            Assert.IsTrue(state.Castles.Any(item => item.FactionId == "valdor" && item.HeroIds.Contains("lyria")));
        }

        // 동맹 양측이 교전 중인 성을 공동 공격 목표로 지정하고 비용과 기간을 저장하는지 검증합니다.
        [Test]
        public void Diplomacy_JointAttackStoresSharedTarget()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIDiplomaticRelationState alliance = state.GetOrCreateDiplomaticRelation("avalon", "ironheart");
            alliance.Status = WIDiplomaticStatus.Alliance;
            state.GetOrCreateDiplomaticRelation("avalon", "valdor").Status = WIDiplomaticStatus.War;
            state.GetOrCreateDiplomaticRelation("ironheart", "valdor").Status = WIDiplomaticStatus.War;
            int influenceBefore = state.GetFactionState("avalon").Influence;

            Assert.IsTrue(WIAdministrationTurnSystem.ProposeJointAttack(database, state, "avalon", "ironheart", "castle_01"));

            Assert.AreEqual("castle_01", alliance.JointAttackTargetCastleId);
            Assert.AreEqual(database.JointAttackDurationMonths, alliance.JointAttackMonthsRemaining);
            Assert.AreEqual(influenceBefore - database.JointAttackInfluenceCost, state.GetFactionState("avalon").Influence);
        }

        // 공동 공격 목표와 남은 기간이 캠페인 저장 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void Diplomacy_JointAttackSaveRoundTripPreservesCommitment()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation("avalon", "ironheart");
            relation.Status = WIDiplomaticStatus.Alliance;
            relation.JointAttackTargetCastleId = "castle_01";
            relation.JointAttackMonthsRemaining = 2;

            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            WIDiplomaticRelationState restored = loaded.GetOrCreateDiplomaticRelation("avalon", "ironheart");
            Assert.AreEqual("castle_01", restored.JointAttackTargetCastleId);
            Assert.AreEqual(2, restored.JointAttackMonthsRemaining);
        }

        // 동맹 AI 부대가 공동 공격 목표와 인접한 집결지에서 해당 목표로 우선 출정하는지 검증합니다.
        [Test]
        public void Diplomacy_JointAttackDirectsAlliedAIArmy()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState staging = state.Castles.First(item => item.FactionId == "ironheart" &&
                database.GetCastle(item.CastleId).AdjacentCastleIds.Count > 0);
            string targetId = database.GetCastle(staging.CastleId).AdjacentCastleIds[0];
            state.GetCastle(targetId).FactionId = "valdor";
            state.GetOrCreateDiplomaticRelation("avalon", "ironheart").Status = WIDiplomaticStatus.Alliance;
            state.GetOrCreateDiplomaticRelation("avalon", "valdor").Status = WIDiplomaticStatus.War;
            state.GetOrCreateDiplomaticRelation("ironheart", "valdor").Status = WIDiplomaticStatus.War;
            WIArmyState alliedArmy = new WIArmyState
            {
                ArmyId = "joint_attack_test",
                FactionId = "ironheart",
                CurrentCastleId = staging.CastleId
            };
            state.Armies.Add(alliedArmy);
            Assert.IsTrue(WIAdministrationTurnSystem.ProposeJointAttack(database, state, "avalon", "ironheart", targetId));

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(WIArmyMission.Attack, alliedArmy.Mission);
            Assert.AreEqual(targetId, alliedArmy.StrategicTargetCastleId);
            Assert.AreEqual(targetId, alliedArmy.TargetCastleId);
            Assert.IsTrue(alliedArmy.IsMoving);
        }

        // 한 턴 실행 후 모든 AI 세력의 사업 또는 군사 판단 근거가 월보에 기록되는지 검증합니다.
        [Test]
        public void AIReasonReport_CoversEveryAIFactionEachTurn()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.IsNotEmpty(summary.AIReasonReports);
            foreach (WIFactionDefinition faction in database.Factions.Where(item => item.PlayerFaction == false))
            {
                Assert.IsTrue(summary.AIReasonReports.Any(item => item.Contains($"[AI 판단] {faction.Id} ·")), faction.Id);
            }
            Assert.LessOrEqual(summary.AIReasonReports.Count, 12);
        }

        // 동일 세력·분야의 판단은 한 번만 기록되고 전체 월보 상한을 넘지 않는지 검증합니다.
        [Test]
        public void AIReasonReport_DeduplicatesAndCapsEntries()
        {
            WITurnSummary summary = new WITurnSummary();
            WIAdministrationTurnSystem.AddAIReasonReport(summary, "valdor", "발도르", "군사", "첫 판단");
            WIAdministrationTurnSystem.AddAIReasonReport(summary, "valdor", "발도르", "군사", "중복 판단");
            for (int index = 0; index < 20; index += 1)
            {
                WIAdministrationTurnSystem.AddAIReasonReport(summary, $"faction_{index}", $"세력 {index}", "사업", "판단");
            }

            Assert.AreEqual(12, summary.AIReasonReports.Count);
            Assert.AreEqual(1, summary.AIReasonReports.Count(item => item.Contains("valdor · 군사")));
        }

        // AI 판단 근거가 저장·불러오기 후에도 최근 월보에 유지되는지 검증합니다.
        [Test]
        public void AIReasonReport_SaveRoundTripPreservesMonthlyReasons()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.LastMonthlyReport = new WITurnSummary();
            state.LastMonthlyReport.AIReasonReports.Add("[AI 판단] valdor · 군사 · 발도르 · 공세 성향");

            string json = WICampaignSaveSystem.Serialize(state, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual(state.LastMonthlyReport.AIReasonReports.Single(), loaded.LastMonthlyReport.AIReasonReports.Single());
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

        // 기존 시각 자산은 유효하고 신규 인물은 추후 교체 가능한 빈 Sprite 슬롯을 허용하는지 검증합니다.
        [Test]
        public void SharedVisualAssets_AreAssignedToAllDefinitions()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            Assert.IsTrue(database.Castles.All(castle => castle.CastleImage != null));
            string[] portraitRequiredIds =
            {
                "ares", "lyria", "brom", "selene", "kael", "morrigan", "theron", "elwyn",
                "common_gareth", "common_mira", "common_thane", "common_nym", "common_raska", "common_veil"
            };
            Assert.IsTrue(database.Heroes.Where(hero => portraitRequiredIds.Contains(hero.Id))
                .All(hero => hero.Portrait != null));
            Assert.IsTrue(database.SpecialFacilities.All(facility => facility.Icon != null));

            foreach (Sprite sprite in database.Heroes.Select(hero => hero.Portrait).Where(sprite => sprite != null)
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
            int expectedIncome = WIAdministrationTurnSystem.GetFactionMonthlyIncome(
                database, state, "valdor").InfluenceGained;

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
            string[] additionalCommanders =
            {
                "hero_009", "hero_010", "hero_011", "hero_012", "hero_013",
                "hero_014", "hero_015", "hero_016", "hero_017", "hero_018",
                "hero_019", "hero_020", "hero_021"
            };
            foreach (string heroId in additionalCommanders)
            {
                frontline.HeroIds.Add(heroId);
                state.GetCharacter(heroId).Discovered = true;
                state.GetCharacter(heroId).Recruited = true;
            }

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            int expectedArmyLimit = Mathf.Max(1, state.Castles.Count(castle => castle.FactionId == "valdor") / 8) + 1;
            Assert.AreEqual(expectedArmyLimit, state.Armies.Count(army => army.FactionId == "valdor"));
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
            string attackerHeroId = army.Members[0].HeroId;
            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(WIBattleOutcome.Defeat, army.LastBattleOutcome);
            Assert.AreEqual("castle_01", army.CurrentCastleId);
            Assert.AreEqual(1, army.ReorganizationMonths);
            Assert.Greater(state.GetCharacter(attackerHeroId).Fatigue, 0);
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
            string commanderId = army.Members[0].HeroId;
            string[] reinforcements = database.Heroes
                .Where(hero => hero.Grade == WICharacterGrade.Hero && army.Members.All(member => member.HeroId != hero.Id))
                .Select(hero => hero.Id).Take(3).ToArray();
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
            Assert.Greater(state.GetCharacter(commanderId).Merit, 0);
            WIRelationshipState battleBond = state.GetOrCreateRelationship(commanderId, reinforcements[0]);
            Assert.AreEqual(1, battleBond.SharedBattleVictories);
            Assert.IsTrue(state.LastMonthlyReport.News.Any(item => item.Contains("전투 인물")));
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

        // 플레이어 참가 전투가 남아 있으면 UI에서 다음 턴을 차단할 상태로 판정되는지 검증합니다.
        [Test]
        public void UnresolvedPlayerBattle_BlocksNextTurn()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.BattleSessions.Add(new WIBattleSessionState
            {
                SessionId = "pending_player_battle",
                PlayerInvolved = true,
                Status = WIBattleSessionStatus.Pending
            });
            Assert.IsTrue(WIAdministrationTurnSystem.HasUnresolvedPlayerBattles(state));
            string controller = System.IO.File.ReadAllText("Assets/Scripts/Administration/WIAdministrationUIController.cs");
            StringAssert.Contains("if (WIAdministrationTurnSystem.HasUnresolvedPlayerBattles(state))", controller);
            StringAssert.Contains("OpenMonthlyReportModal();", controller);
        }

        // 서로의 출발 성으로 교차 출정한 두 부대가 한 전투 세션으로 합쳐지는지 검증합니다.
        [Test]
        public void ReciprocalInvasions_CreateSingleMeetingBattle()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.UseStrategicBattleFallback = false;
            WIArmyState avalonArmy = new WIArmyState
            {
                ArmyId = "avalon_cross", DisplayName = "아발론 교차군", FactionId = "avalon",
                CurrentCastleId = "castle_00", OriginCastleId = "castle_00", TargetCastleId = "castle_01",
                RemainingTravelMonths = 1
            };
            avalonArmy.Members.Add(new WIArmyMemberState { HeroId = "ares", Role = WIUnitRole.Commander });
            WIArmyState valdorArmy = new WIArmyState
            {
                ArmyId = "valdor_cross", DisplayName = "발도르 교차군", FactionId = "valdor",
                CurrentCastleId = "castle_01", OriginCastleId = "castle_01", TargetCastleId = "castle_00",
                RemainingTravelMonths = 1
            };
            valdorArmy.Members.Add(new WIArmyMemberState { HeroId = "lyria", Role = WIUnitRole.Commander });
            state.Armies.Add(avalonArmy);
            state.Armies.Add(valdorArmy);

            WIAdministrationTurnSystem.ExecuteTurn(database, state);

            List<WIBattleSessionState> pending = state.BattleSessions
                .Where(session => session.Status == WIBattleSessionStatus.Pending).ToList();
            Assert.AreEqual(1, pending.Count);
            CollectionAssert.Contains(pending[0].DefenderArmyIds, valdorArmy.ArmyId);
            Assert.AreEqual(valdorArmy.ArmyId, pending[0].CounterAttackerArmyId);
        }

        // 큰 전력 차이로 패배하면 질서 있는 후퇴가 아닌 경우 포로가 발생하고 저장되는지 검증합니다.
        [Test]
        public void BattleConsequences_SevereDefeatCapturesCharacterAndSavesState()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.UseStrategicBattleFallback = false;
            PrepareValdorAttackOnAvalon(state);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIBattleSessionState session = state.BattleSessions.First(item => item.PlayerInvolved);
            session.AttackerPowerSnapshot = 10;
            session.DefenderPowerSnapshot = 100;
            WITurnSummary summary = new WITurnSummary();

            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(
                database, state, session.SessionId, WIBattleOutcome.Defeat,
                WIBattleResolutionSource.RealTimeBattle, summary));

            string attackerHeroId = session.AttackerHeroIds[0].HeroId;
            WICharacterRuntimeState captured = state.GetCharacter(attackerHeroId);
            Assert.IsTrue(captured.Captured);
            Assert.AreEqual("avalon", captured.CaptorFactionId);
            Assert.AreEqual(database.CaptureDurationMonths, captured.CapturedMonthsRemaining);
            Assert.IsTrue(state.IsCharacterBusy(attackerHeroId));
            Assert.IsTrue(summary.News.Any(item => item.Contains("포로 발생")));
            string json = WICampaignSaveSystem.Serialize(state);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.IsTrue(loaded.GetCharacter(attackerHeroId).Captured);
            Assert.AreEqual(database.CaptureDurationMonths, loaded.GetCharacter(attackerHeroId).CapturedMonthsRemaining);
        }

        // 플레이어가 명시적으로 후퇴한 패배에는 포로와 중상 대신 낮은 피로만 적용되는지 검증합니다.
        [Test]
        public void BattleConsequences_OrderlyRetreatPreventsCaptureAndInjury()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            state.UseStrategicBattleFallback = false;
            PrepareValdorAttackOnAvalon(state);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WIBattleSessionState session = state.BattleSessions.First(item => item.PlayerInvolved);
            session.AttackerPowerSnapshot = 10;
            session.DefenderPowerSnapshot = 100;
            session.AttackerRetreated = true;
            string attackerHeroId = session.AttackerHeroIds[0].HeroId;
            int fatigueBefore = state.GetCharacter(attackerHeroId).Fatigue;

            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(
                database, state, session.SessionId, WIBattleOutcome.Defeat,
                WIBattleResolutionSource.RealTimeBattle, new WITurnSummary()));

            WICharacterRuntimeState character = state.GetCharacter(attackerHeroId);
            Assert.IsFalse(character.Captured);
            Assert.AreEqual(0, character.InjuryMonths);
            Assert.AreEqual(fatigueBefore + database.OrderlyRetreatFatigue, character.Fatigue);
            Assert.IsFalse(character.IsDead);
        }

        // 동일한 전투 결과가 전략 자동 판정과 실시간 반환 출처에 관계없이 같은 상태를 만드는지 검증합니다.
        [Test]
        public void BattleConsequences_StrategicAndRealTimeSourcesUseSameResultPath()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState strategic = WIAdministrationState.Create(database);
            strategic.UseStrategicBattleFallback = false;
            PrepareValdorAttackOnAvalon(strategic);
            WIAdministrationTurnSystem.ExecuteTurn(database, strategic);
            WIAdministrationTurnSystem.ExecuteTurn(database, strategic);
            string json = WICampaignSaveSystem.Serialize(strategic);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState realTime, out string error), error);
            WIBattleSessionState strategicSession = strategic.BattleSessions.First(item => item.PlayerInvolved);
            WIBattleSessionState realTimeSession = realTime.BattleSessions.First(item => item.SessionId == strategicSession.SessionId);
            WITurnSummary strategicSummary = new WITurnSummary();
            WITurnSummary realTimeSummary = new WITurnSummary();

            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(
                database, strategic, strategicSession.SessionId, WIBattleOutcome.Defeat,
                WIBattleResolutionSource.StrategicFallback, strategicSummary));
            Assert.IsTrue(WIAdministrationTurnSystem.SubmitBattleResult(
                database, realTime, realTimeSession.SessionId, WIBattleOutcome.Defeat,
                WIBattleResolutionSource.RealTimeBattle, realTimeSummary));

            WICharacterRuntimeState strategicCharacter = strategic.GetCharacter("lyria");
            WICharacterRuntimeState realTimeCharacter = realTime.GetCharacter("lyria");
            Assert.AreEqual(strategicCharacter.Merit, realTimeCharacter.Merit);
            Assert.AreEqual(strategicCharacter.Experience, realTimeCharacter.Experience);
            Assert.AreEqual(strategicCharacter.Fatigue, realTimeCharacter.Fatigue);
            Assert.AreEqual(strategicCharacter.InjuryMonths, realTimeCharacter.InjuryMonths);
            Assert.AreEqual(strategicCharacter.Captured, realTimeCharacter.Captured);
            CollectionAssert.AreEqual(strategicSummary.News, realTimeSummary.News);
        }

        // 전투 관계 누적과 인물별 보상 수치가 저장 JSON 왕복 후 유지되는지 검증합니다.
        [Test]
        public void BattleConsequences_SaveRoundTripPreservesRewardsAndBattleBond()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIRelationshipState relationship = state.GetOrCreateRelationship("ares", "lyria");
            relationship.SharedBattleVictories = 1;
            WICharacterRuntimeState character = state.GetCharacter("ares");
            character.Merit = database.BattleVictoryMerit;
            character.Experience = database.BattleVictoryExperience;
            character.Fatigue = database.BattleVictoryFatigue;
            character.InjuryMonths = database.BattleInjuryMonths;
            string json = WICampaignSaveSystem.Serialize(state);

            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual(1, loaded.GetOrCreateRelationship("ares", "lyria").SharedBattleVictories);
            Assert.AreEqual(character.Merit, loaded.GetCharacter("ares").Merit);
            Assert.AreEqual(character.Experience, loaded.GetCharacter("ares").Experience);
            Assert.AreEqual(character.Fatigue, loaded.GetCharacter("ares").Fatigue);
            Assert.AreEqual(character.InjuryMonths, loaded.GetCharacter("ares").InjuryMonths);
        }

        // 포로가 설정된 억류 기간 후 원래 세력의 성으로 자동 귀환하는지 검증합니다.
        [Test]
        public void CapturedCharacter_ReturnsAfterConfiguredDuration()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICharacterRuntimeState character = state.GetCharacter("lyria");
            character.Captured = true;
            character.CaptorFactionId = "avalon";
            character.CapturedFromFactionId = "valdor";
            character.CapturedMonthsRemaining = database.CaptureDurationMonths;
            foreach (WICastleRuntimeState castle in state.Castles) castle.HeroIds.Remove("lyria");

            for (int month = 0; month < database.CaptureDurationMonths; month += 1)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database, state);
            }

            Assert.IsFalse(character.Captured);
            bool returnedToValdorCastle = state.Castles.Any(castle => castle.FactionId == "valdor" && castle.HeroIds.Contains("lyria"));
            bool assignedToValdorArmy = state.Armies.Any(army => army.FactionId == "valdor" && army.Members.Any(member => member.HeroId == "lyria"));
            Assert.IsTrue(returnedToValdorCastle || assignedToValdorArmy);
            Assert.IsFalse(character.IsDead);
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

        // 집중 공격 명령이 가장 가까운 적보다 지정한 표적을 우선 공격하는지 검증합니다.
        [Test]
        public void BattleCommand_FocusPrioritizesSelectedTarget()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleSessionState session = new WIBattleSessionState { SessionId = "focus_test" };
            session.AttackerHeroIds.Add(new WIBattleParticipantState { HeroId = "ares", Role = WIUnitRole.Melee });
            session.DefenderHeroIds.Add(new WIBattleParticipantState { HeroId = "lyria", Role = WIUnitRole.Melee });
            session.DefenderHeroIds.Add(new WIBattleParticipantState { HeroId = "brom", Role = WIUnitRole.Melee });
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.HeroId == "ares");
            WIBattleCharacterState nearest = runtime.Characters.Single(item => item.HeroId == "lyria");
            WIBattleCharacterState focused = runtime.Characters.Single(item => item.HeroId == "brom");
            attacker.Position = Vector2.zero;
            nearest.Position = Vector2.right * 0.5f;
            focused.Position = Vector2.right;
            runtime.AttackerCommand = WIBattleCommand.Focus;
            runtime.AttackerFocusHeroId = focused.HeroId;
            int nearestHealth = nearest.Health;
            int focusedHealth = focused.Health;

            WIBattleSimulation.Step(config, runtime, 0.01f);

            Assert.AreEqual(nearestHealth, nearest.Health);
            Assert.Less(focused.Health, focusedHealth);
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

        // 실제 아트가 없어도 캐릭터와 전장에 사용할 절차형 도형이 생성되는지 검증합니다.
        [Test]
        public void BattlePlaceholders_CreateCharacterShapesAndArenaGrid()
        {
            Assert.IsNotNull(WIBattlePlaceholderSprites.GetCircle());
            Assert.IsNotNull(WIBattlePlaceholderSprites.GetSquare());
            Assert.IsNotNull(WIBattlePlaceholderSprites.GetArenaGrid());
            Assert.Greater(WIBattlePlaceholderSprites.GetArenaGrid().bounds.size.x, 10f);
        }

        // PC 전투 카메라가 확대 범위와 16:9 전장 경계를 넘지 않도록 좌표를 제한하는지 검증합니다.
        [Test]
        public void BattleCamera_ClampsZoomAndPositionInsideArena()
        {
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            GameObject cameraObject = new GameObject("BattleCameraTest");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.aspect = 16f / 9f;
            WIBattleCameraController controller = cameraObject.AddComponent<WIBattleCameraController>();
            controller.Initialize(config);
            controller.Zoom(-100f);

            Vector2 clamped = controller.GetClampedPosition(new Vector2(100f, 100f));

            Assert.AreEqual(config.CameraMinimumZoom, camera.orthographicSize, 0.001f);
            Assert.LessOrEqual(clamped.x, config.ArenaSize.x * 0.5f);
            Assert.LessOrEqual(clamped.y, config.ArenaSize.y * 0.5f);
            Assert.GreaterOrEqual(clamped.x, 0f);
            Assert.GreaterOrEqual(clamped.y, 0f);
            Object.DestroyImmediate(cameraObject);
        }

        // 20·40·60명 장시간 교전이 시간 목표 안에서 끝나고 임시 상태가 무한 증가하지 않는지 검증합니다.
        [TestCase(20)]
        [TestCase(40)]
        [TestCase(60)]
        public void BattlePerformance_ManyCharactersRemainBounded(int characterCount)
        {
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattlePerformanceBenchmark.CreateScenario(config, characterCount);
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int step = 0; step < 600; step += 1)
            {
                WIBattleSimulation.Step(config, runtime, 1f / 60f);
            }
            stopwatch.Stop();

            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, $"{characterCount}명 600스텝 성능 목표 초과");
            Assert.Less(runtime.Projectiles.Count, characterCount * 3);
            Assert.Less(runtime.VisualEffects.Count, characterCount * 2);
            Assert.IsTrue(runtime.Characters.All(item =>
                float.IsNaN(item.Position.x) == false && float.IsNaN(item.Position.y) == false));
        }

        // 근접은 즉시 타격하고 원거리는 발사체 충돌 시점에 피해를 주는지 검증합니다.
        [Test]
        public void BattleAttack_MeleeHitsImmediatelyAndProjectileDelaysDamage()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker);
            WIBattleCharacterState defender = runtime.Characters.Single(item => item.Side == WIBattleSide.Defender);
            attacker.Position = Vector2.zero;
            defender.Position = Vector2.right;

            attacker.Role = WIUnitRole.Melee;
            WIBattleSimulation.Step(config, runtime, 0.01f);
            Assert.IsTrue(runtime.VisualEffects.Any(item => item.EffectType == WIBattleVisualEffectType.MeleeHit));

            runtime.VisualEffects.Clear();
            attacker.Role = WIUnitRole.Ranged;
            attacker.AttackRange = config.RangedRange;
            attacker.Position = Vector2.zero;
            defender.Position = Vector2.right * 3f;
            attacker.CooldownRemaining = 0f;
            int healthBeforeProjectile = defender.Health;
            WIBattleSimulation.Step(config, runtime, 0.01f);
            Assert.AreEqual(healthBeforeProjectile, defender.Health);
            Assert.AreEqual(1, runtime.Projectiles.Count);

            for (int index = 0; index < 20 && defender.Health == healthBeforeProjectile; index += 1)
            {
                WIBattleSimulation.Step(config, runtime, 0.05f);
            }
            Assert.Less(defender.Health, healthBeforeProjectile);
        }

        // 원거리 인물도 설정된 사거리 밖에서는 발사하지 않고 접근하는지 검증합니다.
        [Test]
        public void BattleProjectile_DoesNotLaunchOutsideAttackRange()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker);
            WIBattleCharacterState defender = runtime.Characters.Single(item => item.Side == WIBattleSide.Defender);
            attacker.Role = WIUnitRole.Ranged;
            attacker.AttackRange = config.RangedRange;
            attacker.Position = Vector2.left * 4f;
            defender.Position = Vector2.right * 4f;

            WIBattleSimulation.Step(config, runtime, 0.1f);

            Assert.AreEqual(0, runtime.Projectiles.Count);
            Assert.Greater(attacker.Position.x, -4f);
        }

        // 기본 오발 비활성 규칙에서 아군이 탄도 사이에 있어도 피해를 받지 않는지 검증합니다.
        [Test]
        public void BattleProjectile_DefaultRulePreventsFriendlyFire()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, CreateSimpleBattleSession());
            WIBattleCharacterState attacker = runtime.Characters.Single(item => item.Side == WIBattleSide.Attacker);
            WIBattleCharacterState defender = runtime.Characters.Single(item => item.Side == WIBattleSide.Defender);
            WIBattleCharacterState ally = new WIBattleCharacterState
            {
                HeroId = "friendly_blocker",
                Side = WIBattleSide.Attacker,
                Role = WIUnitRole.Melee,
                Position = Vector2.right * 1.5f,
                FormationPosition = Vector2.right * 1.5f,
                MaxHealth = 100,
                Health = 100,
                AttackRange = config.MeleeRange,
                MoveSpeed = 0f,
                CooldownRemaining = 100f
            };
            runtime.Characters.Add(ally);
            attacker.Role = WIUnitRole.Ranged;
            attacker.AttackRange = config.RangedRange;
            attacker.Position = Vector2.zero;
            defender.Position = Vector2.right * 3f;
            runtime.AttackerCommand = WIBattleCommand.Hold;
            int defenderHealthBefore = defender.Health;

            for (int index = 0; index < 20 && defender.Health == defenderHealthBefore; index += 1)
            {
                WIBattleSimulation.Step(config, runtime, 0.05f);
            }

            Assert.AreEqual(100, ally.Health);
            Assert.Less(defender.Health, defenderHealthBefore);
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
            Assert.IsTrue(runtime.VisualEffects.Any(item => item.EffectType == WIBattleVisualEffectType.SkillDamage));
            Assert.IsFalse(WIBattleSimulation.TryActivateHeroSkill(config, runtime, "ares"));
        }

        // 고유 영웅 8명 모두 중복 없는 액티브 스킬을 가지며 세 효과 유형을 포함하는지 검증합니다.
        [Test]
        public void HeroSkills_CoverAllEightHeroesAndThreeEffectTypes()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            string[] heroIds = database.Heroes.Where(item => item.ActiveSkillAvailable).Select(item => item.Id).ToArray();

            Assert.AreEqual(8, heroIds.Length);
            Assert.IsTrue(heroIds.All(id => config.GetHeroSkill(id) != null));
            Assert.AreEqual(8, config.HeroSkills.Select(item => item.DisplayName).Distinct().Count());
            Assert.IsTrue(config.HeroSkills.Any(item => item.SkillType == WIBattleSkillType.AreaDamage));
            Assert.IsTrue(config.HeroSkills.Any(item => item.SkillType == WIBattleSkillType.HealAllies));
            Assert.IsTrue(config.HeroSkills.Any(item => item.SkillType == WIBattleSkillType.CommandBuff));
            Assert.IsTrue(database.Heroes.Where(item => item.ActiveSkillAvailable)
                .All(item => item.Grade == WICharacterGrade.Hero));
        }

        // 회복 스킬이 설정 범위 안의 아군만 회복하고 초록 범위 효과를 예약하는지 검증합니다.
        [Test]
        public void HeroSkill_HealAffectsOnlyAlliesInsideRange()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIBattleSessionState session = new WIBattleSessionState { SessionId = "heal_skill_test" };
            session.AttackerHeroIds.Add(new WIBattleParticipantState { HeroId = "selene", Role = WIUnitRole.Support });
            session.AttackerHeroIds.Add(new WIBattleParticipantState { HeroId = "ares", Role = WIUnitRole.Melee });
            session.AttackerHeroIds.Add(new WIBattleParticipantState { HeroId = "brom", Role = WIUnitRole.Melee });
            session.DefenderHeroIds.Add(new WIBattleParticipantState { HeroId = "morrigan", Role = WIUnitRole.Magic });
            WIBattleRuntimeState runtime = WIBattleRuntimeBuilder.Build(config, database, session);
            WIBattleCharacterState caster = runtime.Characters.Single(item => item.HeroId == "selene");
            WIBattleCharacterState nearAlly = runtime.Characters.Single(item => item.HeroId == "ares");
            WIBattleCharacterState farAlly = runtime.Characters.Single(item => item.HeroId == "brom");
            nearAlly.Position = caster.Position + Vector2.right;
            farAlly.Position = caster.Position + Vector2.right * 8f;
            nearAlly.Health -= 40;
            farAlly.Health -= 40;
            int farHealthBefore = farAlly.Health;

            Assert.IsTrue(WIBattleSimulation.TryActivateHeroSkill(config, runtime, "selene"));

            Assert.Greater(nearAlly.Health, nearAlly.MaxHealth - 40);
            Assert.AreEqual(farHealthBefore, farAlly.Health);
            Assert.IsTrue(runtime.VisualEffects.Any(item => item.EffectType == WIBattleVisualEffectType.SkillHeal));
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

            Assert.AreEqual(400, database.Heroes.Count(hero => hero.Grade == WICharacterGrade.Common));
            Assert.AreEqual(WICharacterGrade.Common, state.GetCharacter("common_gareth").BaseGrade);
            Assert.IsFalse(state.GetCharacter("common_gareth").PromotedToHero);
        }

        // 일반 인물 1차 확장이 10종 이상 클래스를 사용하고 각 세력 시작 성에 최소 두 명을 배치하는지 검증합니다.
        [Test]
        public void CommonRoster_FirstExpansionBalancesClassesAndFactions()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            List<WIHeroDefinition> commonCharacters = database.Heroes
                .Where(hero => hero.Grade == WICharacterGrade.Common).ToList();

            Assert.GreaterOrEqual(commonCharacters.Select(hero => hero.HeroClass).Distinct().Count(), 10);
            Assert.Contains(WIHeroClass.Strategist, commonCharacters.Select(hero => hero.HeroClass).ToList());
            Assert.Contains(WIHeroClass.Alchemist, commonCharacters.Select(hero => hero.HeroClass).ToList());
            Assert.Contains(WIHeroClass.Warlock, commonCharacters.Select(hero => hero.HeroClass).ToList());
            foreach (WIFactionDefinition faction in database.Factions)
            {
                int startingCommons = state.Castles.Where(castle => castle.FactionId == faction.Id)
                    .SelectMany(castle => castle.HeroIds).Distinct()
                    .Count(heroId => database.GetHero(heroId)?.Grade == WICharacterGrade.Common);
                Assert.GreaterOrEqual(startingCommons, 2, faction.Id);
            }
        }

        // 신규 일반 인물의 ID·현지화·능력치·등용 사건 참조가 모두 유효한지 검증합니다.
        [Test]
        public void CommonRoster_FirstExpansionHasValidDataReferences()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            List<WIHeroDefinition> commons = database.Heroes.Where(hero => hero.Grade == WICharacterGrade.Common).ToList();

            Assert.AreEqual(commons.Count, commons.Select(hero => hero.Id).Distinct().Count());
            foreach (WIHeroDefinition hero in commons)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(hero.DisplayName.Korean), hero.Id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(hero.DisplayName.English), hero.Id);
                Assert.IsTrue(hero.Leadership > 0 && hero.Might > 0 && hero.Intelligence > 0 && hero.Charisma > 0 && hero.Politics > 0, hero.Id);
                Assert.IsNotNull(database.GetRecruitmentEvent(hero.RecruitmentEventId), hero.Id);
            }
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
            target.HeroIds.Clear();
            target.GovernorHeroId = string.Empty;
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
            target.HeroIds.Clear();
            target.GovernorHeroId = string.Empty;
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
            Assert.IsTrue(WIAdministrationTurnSystem.AssignGovernor(state, castle.CastleId, "lyria"));
            Assert.AreEqual("lyria", castle.GovernorHeroId);
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

        // 12개 직업의 능력치 성향과 권장 역할 데이터가 빠짐없이 정의되는지 검증합니다.
        [Test]
        public void HeroClasses_DefineStatTendenciesAndRecommendedRoles()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            Assert.AreEqual(12, database.HeroClassDefinitions.Count);
            CollectionAssert.AreEquivalent(
                System.Enum.GetValues(typeof(WIHeroClass)).Cast<WIHeroClass>(),
                database.HeroClassDefinitions.Select(item => item.HeroClass));
            Assert.IsTrue(database.HeroClassDefinitions.All(item => item.DisplayName != null));
            Assert.IsTrue(database.HeroClassDefinitions.All(item => item.Description != null));
            Assert.IsTrue(database.HeroClassDefinitions.All(item => item.PrimaryStat != item.SecondaryStat));
        }

        // 일반 인물은 직업 공용 기술을, 고유 영웅은 전용 기술을 우선 사용하는지 검증합니다.
        [Test]
        public void BattleSkills_ResolveUniqueAndCommonCharacterSkills()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIBattleConfigSO config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>(BattleConfigPath);
            WIHeroDefinition common = database.Heroes.First(item => item.Grade == WICharacterGrade.Common);

            Assert.AreEqual(12, config.ClassSkills.Count);
            CollectionAssert.AreEquivalent(
                System.Enum.GetValues(typeof(WIHeroClass)).Cast<WIHeroClass>(),
                config.ClassSkills.Select(item => item.HeroClass));
            Assert.IsTrue(config.ClassSkills.All(item => item.Skill != null && string.IsNullOrEmpty(item.Skill.DisplayName) == false));
            Assert.AreSame(config.GetClassSkill(common.HeroClass), config.GetCharacterSkill(common.Id, common.HeroClass));
            Assert.AreSame(config.GetHeroSkill("ares"), config.GetCharacterSkill("ares", WIHeroClass.MagicSwordsman));
        }

        // 아발론 플레이어가 소유 성만 직접 관리하고 타 세력 성은 관리하지 못하는지 검증합니다.
        [Test]
        public void CastleManagement_AllowsOnlyPlayerOwnedCastles()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICastleRuntimeState ownCastle = state.Castles.First(item => item.FactionId == state.PlayerFactionId);
            WICastleRuntimeState foreignCastle = state.Castles.First(item => item.FactionId != state.PlayerFactionId);

            Assert.IsTrue(WIAdministrationTurnSystem.CanPlayerManageCastle(state, ownCastle));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPlayerManageCastle(state, foreignCastle));

            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(
                state.PlayerFactionId, foreignCastle.FactionId);
            relation.Status = WIDiplomaticStatus.Alliance;
            Assert.IsTrue(WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, foreignCastle));
            Assert.IsFalse(WIAdministrationTurnSystem.CanPlayerManageCastle(state, foreignCastle),
                "동맹 성은 상세 정보를 공유해도 직접 내정할 수 없어야 합니다.");
        }

        // 아발론의 첫 목표가 시작 상태에서 미완료이며 수도 번영 조건과 보상을 정의하는지 검증합니다.
        [Test]
        public void AvalonOpeningObjective_DefinesSituationConditionAndReward()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICampaignObjectiveDefinition objective = WICampaignObjectiveSystem.GetCurrent(database, state);

            Assert.NotNull(objective);
            Assert.AreEqual("avalon_restore_capital", objective.Id);
            Assert.AreEqual("castle_00", objective.TargetCastleId);
            Assert.Less(WICampaignObjectiveSystem.GetProgress(state, objective), objective.TargetValue);
            Assert.IsFalse(string.IsNullOrWhiteSpace(objective.Situation.Korean));
            Assert.Greater(objective.RewardGold + objective.RewardMana + objective.RewardInfluence, 0);
        }

        // 수도 번영 목표 달성 시 한 번만 보상을 지급하고 월보에 기록하는지 검증합니다.
        [Test]
        public void AvalonOpeningObjective_CompletesAndRewardsOnlyOnce()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WICampaignObjectiveDefinition objective = database.GetCampaignObjective("avalon_restore_capital");
            state.GetCastle(objective.TargetCastleId).Prosperity = objective.TargetValue;
            int goldBefore = state.Gold;
            WITurnSummary summary = new WITurnSummary();

            Assert.IsTrue(WICampaignObjectiveSystem.Evaluate(database, state, summary));
            Assert.AreEqual(goldBefore + objective.RewardGold, state.Gold);
            Assert.Contains(objective.Id, state.CompletedCampaignObjectiveIds);
            Assert.IsTrue(summary.News.Any(item => item.Contains("캠페인 목표 완료")));
            Assert.IsFalse(WICampaignObjectiveSystem.Evaluate(database, state, summary));
            Assert.AreEqual(goldBefore + objective.RewardGold, state.Gold);
        }

        // 완료한 캠페인 목표가 저장과 불러오기 후에도 유지되는지 검증합니다.
        [Test]
        public void CampaignObjectiveProgress_SaveRoundTripPreservesCompletion()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState source = WIAdministrationState.Create(database);
            source.CompletedCampaignObjectiveIds.Add("avalon_restore_capital");

            WIAdministrationState loaded = JsonUtility.FromJson<WIAdministrationState>(JsonUtility.ToJson(source));

            Assert.Contains("avalon_restore_capital", loaded.CompletedCampaignObjectiveIds);
            Assert.AreEqual("avalon_secure_border", WICampaignObjectiveSystem.GetCurrent(database, loaded).Id);
        }

        // 첫 내정부터 국경·세력 격파·대륙 통일까지 네 단계 목표가 순서대로 정의되는지 검증합니다.
        [Test]
        public void CampaignObjectives_DefineOrderedProgressionToUnification()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            CollectionAssert.AreEqual(new[]
            {
                "avalon_restore_capital", "avalon_secure_border",
                "avalon_break_valdor", "avalon_unify_continent"
            }, database.CampaignObjectives.Select(item => item.Id).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                WICampaignObjectiveType.CastleProsperity, WICampaignObjectiveType.PlayerCastleCount,
                WICampaignObjectiveType.FactionEliminated, WICampaignObjectiveType.ContinentalUnification
            }, database.CampaignObjectives.Select(item => item.ObjectiveType).ToArray());
            Assert.IsTrue(database.CampaignObjectives.All(item => item.TargetValue > 0));
        }

        // 캠페인 상태를 단계별 조건으로 진행해 최종 목표와 기존 승리 판정이 함께 완료되는지 검증합니다.
        [Test]
        public void CampaignObjectives_ProgressFromOpeningToVictory()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WITurnSummary summary = new WITurnSummary();

            state.GetCastle("castle_00").Prosperity = 50;
            Assert.IsTrue(WICampaignObjectiveSystem.Evaluate(database, state, summary));
            Assert.AreEqual("avalon_secure_border", WICampaignObjectiveSystem.GetCurrent(database, state).Id);

            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.CastleId == "castle_01" || item.CastleId == "castle_02"))
                castle.FactionId = state.PlayerFactionId;
            Assert.IsTrue(WICampaignObjectiveSystem.Evaluate(database, state, summary));
            Assert.AreEqual("avalon_break_valdor", WICampaignObjectiveSystem.GetCurrent(database, state).Id);

            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == "valdor"))
                castle.FactionId = state.PlayerFactionId;
            WIAdministrationTurnSystem.ResolveFactionEliminations(database, state, summary);
            Assert.IsTrue(WICampaignObjectiveSystem.Evaluate(database, state, summary));
            Assert.AreEqual("avalon_unify_continent", WICampaignObjectiveSystem.GetCurrent(database, state).Id);

            foreach (WICastleRuntimeState castle in state.Castles) castle.FactionId = state.PlayerFactionId;
            Assert.IsTrue(WICampaignObjectiveSystem.Evaluate(database, state, summary));
            WICampaignResultSystem.Evaluate(database, state);
            Assert.IsNull(WICampaignObjectiveSystem.GetCurrent(database, state));
            Assert.AreEqual(WICampaignResult.Victory, state.CampaignResult);
            Assert.AreEqual(4, summary.News.Count(item => item.Contains("캠페인 목표 완료")));
        }

        // 다섯 세력에 주요 인물 관계가 하나씩 있고 같은 시작 성에서 사건 조건을 갖추는지 검증합니다.
        [Test]
        public void StartingRelationships_CoverEveryFactionAndCreateRuntimeNetwork()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);

            Assert.AreEqual(database.Factions.Count, database.StartingRelationships.Count);
            CollectionAssert.AreEquivalent(database.Factions.Select(item => item.Id),
                database.StartingRelationships.Select(item => item.FactionId));
            foreach (WIStartingRelationshipDefinition definition in database.StartingRelationships)
            {
                Assert.AreNotEqual(WIRelationshipLevel.Normal, definition.Level);
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Context.Korean));
                WICastleRuntimeState castle = state.Castles.Single(item =>
                    item.HeroIds.Contains(definition.FirstHeroId) && item.HeroIds.Contains(definition.SecondHeroId));
                Assert.AreEqual(definition.FactionId, castle.FactionId);
                Assert.AreEqual(definition.Level,
                    state.GetOrCreateRelationship(definition.FirstHeroId, definition.SecondHeroId).Level);
            }
        }

        // 각 세력의 주요 관계가 플레이어 영지 안에서 실제 관계 사건 후보로 연결되는지 검증합니다.
        [Test]
        public void StartingRelationships_AllResolveToTurnEventCandidates()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            foreach (WIStartingRelationshipDefinition definition in database.StartingRelationships)
            {
                WIAdministrationState state = WIAdministrationState.Create(database);
                WIRelationshipState relationship = state.GetOrCreateRelationship(
                    definition.FirstHeroId, definition.SecondHeroId);
                state.Relationships = new List<WIRelationshipState> { relationship };
                WICastleRuntimeState castle = state.Castles.Single(item =>
                    item.HeroIds.Contains(definition.FirstHeroId) && item.HeroIds.Contains(definition.SecondHeroId));
                castle.FactionId = state.PlayerFactionId;

                WIAdministrationTurnSystem.CreateRelationshipEventCandidates(database, state, new WITurnSummary());

                Assert.AreEqual(1, state.PendingRelationshipEvents.Count, definition.FactionId);
                Assert.AreEqual(definition.FirstHeroId, state.PendingRelationshipEvents[0].FirstHeroId);
                Assert.AreEqual(definition.SecondHeroId, state.PendingRelationshipEvents[0].SecondHeroId);
            }
        }

        // 다섯 세력의 수도 사건이 고유 조건과 두 선택지를 갖는지 검증합니다.
        [Test]
        public void RegionalEvents_CoverEveryFactionCapital()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            Assert.AreEqual(database.Factions.Count, database.RegionalEventDefinitions.Count);
            CollectionAssert.AreEquivalent(database.Factions.Select(item => item.Id),
                database.RegionalEventDefinitions.Select(item => item.OriginFactionId));
            Assert.AreEqual(database.RegionalEventDefinitions.Count,
                database.RegionalEventDefinitions.Select(item => item.Id).Distinct().Count());
            Assert.IsTrue(database.RegionalEventDefinitions.All(item =>
                database.GetCastle(item.TargetCastleId) != null && item.Choices.Count == 2));
        }

        // 플레이어가 소유한 대상 지역 사건만 발생하고 다른 세력 사건은 점령 전 차단되는지 검증합니다.
        [Test]
        public void RegionalEvents_RequirePlayerOwnershipAndMinimumTurn()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WITurnSummary summary = new WITurnSummary();

            WIRegionalEventDefinition first = WIRegionalEventSystem.CreateCandidate(database, state, summary);
            Assert.AreEqual("region_avalon_lake", first.Id);
            state.PendingRegionalEvents.Clear();
            state.CompletedRegionalEventIds.Add(first.Id);
            Assert.IsNull(WIRegionalEventSystem.CreateCandidate(database, state, summary));

            WIRegionalEventDefinition valdor = database.RegionalEventDefinitions.First(item => item.OriginFactionId == "valdor");
            state.Turn = valdor.MinimumTurn;
            state.GetCastle(valdor.TargetCastleId).FactionId = state.PlayerFactionId;
            Assert.AreEqual(valdor.Id, WIRegionalEventSystem.CreateCandidate(database, state, summary).Id);
        }

        // 지역 사건 선택이 자원·성 수치에 적용되고 완료 상태가 저장 왕복되는지 검증합니다.
        [Test]
        public void RegionalEventChoice_AppliesEffectsAndPersistsCompletion()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIRegionalEventDefinition definition = database.GetRegionalEvent("region_avalon_lake");
            WITurnSummary summary = new WITurnSummary();
            WIRegionalEventSystem.CreateCandidate(database, state, summary);
            WIPendingRegionalEvent pending = state.PendingRegionalEvents.Single();
            WIRegionalEventChoiceDefinition choice = definition.Choices[0];
            int goldBefore = state.Gold;
            int prosperityBefore = state.GetCastle(definition.TargetCastleId).Prosperity;

            Assert.IsTrue(WIRegionalEventSystem.Resolve(database, state, pending, 0, summary));
            Assert.AreEqual(goldBefore + choice.GoldDelta, state.Gold);
            Assert.AreEqual(prosperityBefore + choice.ProsperityDelta,
                state.GetCastle(definition.TargetCastleId).Prosperity);
            Assert.Contains(definition.Id, state.CompletedRegionalEventIds);

            WIAdministrationState loaded = JsonUtility.FromJson<WIAdministrationState>(JsonUtility.ToJson(state));
            Assert.Contains(definition.Id, loaded.CompletedRegionalEventIds);
            Assert.AreEqual(0, loaded.PendingRegionalEvents.Count);
        }

        // 점령 통치 선택지 3종과 다섯 세력의 멸망 서사가 모두 데이터화됐는지 검증합니다.
        [Test]
        public void OccupationAndEliminationContent_CoversRulesAndFactions()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            Assert.AreEqual(3, database.OccupationChoices.Count);
            Assert.AreEqual(3, database.OccupationChoices.Select(item => item.Id).Distinct().Count());
            Assert.IsTrue(database.OccupationChoices.All(item => item.UnrestMonths >= 1));
            Assert.AreEqual(database.Factions.Count, database.FactionEliminationNarratives.Count);
            CollectionAssert.AreEquivalent(database.Factions.Select(item => item.Id),
                database.FactionEliminationNarratives.Select(item => item.FactionId));
        }

        // 플레이어 점령 직후 통치 사건이 생성되고 선택 결과가 불안·자원·성 수치에 적용되는지 검증합니다.
        [Test]
        public void PlayerOccupation_CreatesAndResolvesGovernanceChoice()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            WIArmyState army = new WIArmyState
            {
                ArmyId = "occupation_test", FactionId = state.PlayerFactionId,
                CurrentCastleId = "castle_01", AwaitingBattle = true,
                Members = new List<WIArmyMemberState> { new WIArmyMemberState { HeroId = "ares", Role = WIUnitRole.Commander } }
            };
            state.Armies.Add(army);
            WITurnSummary summary = new WITurnSummary();

            Assert.IsTrue(WIAdministrationTurnSystem.ResolveArmyVictoryAndOccupation(database, state, army, summary));
            WIPendingOccupationEvent pending = state.PendingOccupationEvents.Single();
            Assert.AreEqual("valdor", pending.DefeatedFactionId);
            WIOccupationChoiceDefinition choice = database.OccupationChoices[0];
            int goldBefore = state.Gold;

            Assert.IsTrue(WIOccupationEventSystem.Resolve(database, state, pending, 0, summary));
            Assert.AreEqual(goldBefore + choice.GoldDelta, state.Gold);
            Assert.AreEqual(choice.UnrestMonths, state.GetCastle("castle_01").OccupationUnrestMonths);
            Assert.Contains(choice.Id, state.OccupationPolicyHistory);
            Assert.AreEqual(0, state.PendingOccupationEvents.Count);
            Assert.IsTrue(summary.News.Any(item => item.Contains("점령 통치 결정")));
        }

        // 멸망 월보가 세력별 고유 제목과 설명을 사용하고 한 번만 기록되는지 검증합니다.
        [Test]
        public void FactionElimination_UsesFactionNarrativeOnce()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database);
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == "valdor"))
                castle.FactionId = state.PlayerFactionId;
            WITurnSummary summary = new WITurnSummary();

            WIAdministrationTurnSystem.ResolveFactionEliminations(database, state, summary);
            WIAdministrationTurnSystem.ResolveFactionEliminations(database, state, summary);

            WIFactionEliminationNarrativeDefinition narrative = database.GetFactionEliminationNarrative("valdor");
            Assert.AreEqual(1, summary.News.Count(item => item.Contains(narrative.Title.Korean)));
            Assert.IsTrue(summary.News.Single(item => item.Contains(narrative.Title.Korean)).Contains(narrative.Description.Korean));
        }

        // 최소 두 결말이 고유 제목과 설명으로 ScriptableObject에 정의되는지 검증합니다.
        [Test]
        public void CampaignEndings_DefineConcordAndDominion()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            Assert.AreEqual(2, database.CampaignEndings.Count);
            CollectionAssert.AreEquivalent(new[] { WICampaignEndingType.Concord, WICampaignEndingType.Dominion },
                database.CampaignEndings.Select(item => item.EndingType));
            Assert.IsTrue(database.CampaignEndings.All(item =>
                string.IsNullOrWhiteSpace(item.Title.Korean) == false &&
                string.IsNullOrWhiteSpace(item.Description.Korean) == false));
        }

        // 회유·자치가 우세하면 화합, 군정이 우세하면 철권 결말로 분기되는지 검증합니다.
        [Test]
        public void CampaignEnding_UsesOccupationPolicyHistory()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState concord = WIAdministrationState.Create(database);
            concord.OccupationPolicyHistory.AddRange(new[] { "conciliation", "local_autonomy", "martial_law" });
            Assert.AreEqual(WICampaignEndingType.Concord, WICampaignResultSystem.DetermineEnding(concord));

            WIAdministrationState dominion = WIAdministrationState.Create(database);
            dominion.OccupationPolicyHistory.AddRange(new[] { "martial_law", "martial_law", "conciliation" });
            foreach (WICastleRuntimeState castle in dominion.Castles) castle.FactionId = dominion.PlayerFactionId;
            Assert.IsTrue(WICampaignResultSystem.Evaluate(database, dominion));
            Assert.AreEqual(WICampaignEndingType.Dominion, dominion.CampaignEnding);
        }

        // 판정된 결말과 점령 선택 이력이 저장·불러오기 후에도 유지되는지 검증합니다.
        [Test]
        public void CampaignEnding_SaveRoundTripPreservesEndingAndHistory()
        {
            WIAdministrationState source = new WIAdministrationState
            {
                CampaignResult = WICampaignResult.Victory,
                CampaignEnding = WICampaignEndingType.Dominion
            };
            source.OccupationPolicyHistory.Add("martial_law");

            string json = WICampaignSaveSystem.Serialize(source, false);
            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(json, out WIAdministrationState loaded, out string error), error);
            Assert.AreEqual(WICampaignEndingType.Dominion, loaded.CampaignEnding);
            Assert.Contains("martial_law", loaded.OccupationPolicyHistory);
        }

        // 세 시작 변형이 영토 또는 관계만 바꾸도록 데이터에 정의되는지 검증합니다.
        [Test]
        public void CampaignVariants_DefineClassicBorderAndCourtStarts()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);

            Assert.AreEqual(3, database.CampaignVariants.Count);
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(WICampaignVariant)).Cast<WICampaignVariant>(),
                database.CampaignVariants.Select(item => item.Variant));
            Assert.IsTrue(database.CampaignVariants.All(item =>
                string.IsNullOrWhiteSpace(item.DisplayName.Korean) == false &&
                string.IsNullOrWhiteSpace(item.Description.Korean) == false));
        }

        // 국경 수비대는 영토를, 분열된 궁정은 핵심 관계를 바꾸며 자원은 동일한지 검증합니다.
        [Test]
        public void CampaignVariants_ChangeTerritoryOrRelationshipWithoutResourceBonus()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState classic = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.Classic);
            WIAdministrationState border = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.BorderGarrison);
            WIAdministrationState court = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.DividedCourt);

            Assert.AreEqual(classic.Gold, border.Gold);
            Assert.AreEqual(classic.ManaCrystal, border.ManaCrystal);
            Assert.AreEqual(classic.Influence, border.Influence);
            Assert.AreEqual(classic.Castles.Count(item => item.FactionId == classic.PlayerFactionId) + 1,
                border.Castles.Count(item => item.FactionId == border.PlayerFactionId));
            Assert.AreEqual(WIRelationshipLevel.Fondness, classic.GetOrCreateRelationship("ares", "common_alden").Level);
            Assert.AreEqual(WIRelationshipLevel.Conflict, court.GetOrCreateRelationship("ares", "common_alden").Level);
            Assert.AreEqual(WICampaignVariant.DividedCourt, court.CampaignVariant);
        }

        // 각 시작 변형이 첫해 12개월 동안 상태 무결성과 저장 값을 유지하는지 검증합니다.
        [TestCase(WICampaignVariant.Classic)]
        [TestCase(WICampaignVariant.BorderGarrison)]
        [TestCase(WICampaignVariant.DividedCourt)]
        public void CampaignVariant_FirstYearSimulationRemainsValid(WICampaignVariant variant)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, variant);
            for (int month = 0; month < 12 && state.CampaignResult == WICampaignResult.Ongoing; month += 1)
                WIAdministrationTurnSystem.ExecuteTurn(database, state);

            Assert.AreEqual(variant, state.CampaignVariant);
            Assert.IsTrue(state.Factions.All(item => item.Gold >= 0 && item.ManaCrystal >= 0 && item.Influence >= 0));
            Assert.IsTrue(state.Castles.All(item => item.Prosperity >= 0 && item.Prosperity <= 100 &&
                item.Technology >= 0 && item.Technology <= 100 && item.Stability >= 0 && item.Stability <= 100));
            WIAdministrationState loaded = JsonUtility.FromJson<WIAdministrationState>(JsonUtility.ToJson(state));
            Assert.AreEqual(variant, loaded.CampaignVariant);
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
