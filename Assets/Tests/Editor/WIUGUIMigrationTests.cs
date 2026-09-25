using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectWI.Tests.Editor
{
    [ExecuteAlways]
    public sealed class WITestLazyUGUIPanelController :
        ProjectWI.Administration.WIAdministrationUGUIPanelController
    {
        // 최초 생성 직후 표시 요청을 받은 횟수와 전달 인자를 기록합니다.
        public int RequestCount;
        public string LastId;

        // 실제 화면처럼 활성화될 때 행정 화면 요청을 구독합니다.
        private void OnEnable()
        {
            if (ResolveAdministrationController() == null)
            {
                return;
            }
            administrationController.UGUIDelegationRequested += HandleRequest;
            administrationController.UGUIEventChoiceRequested += HandleChoice;
        }

        // 검사 종료 시 이벤트 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController == null)
            {
                return;
            }
            administrationController.UGUIDelegationRequested -= HandleRequest;
            administrationController.UGUIEventChoiceRequested -= HandleChoice;
        }

        // 인자 없는 최초 표시 요청을 기록합니다.
        private void HandleRequest()
        {
            RequestCount += 1;
        }

        // 선택 화면 최초 요청의 인자를 기록합니다.
        private void HandleChoice(ProjectWI.Administration.WIAdministrationReportActionType type, string id)
        {
            RequestCount += 1;
            LastId = id;
        }
    }

    public class WIUGUIMigrationTests
    {
        private const string CampaignTitlePrefabPath = "Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab";
        private const string WorldPrefabPath = "Assets/Prefabs/Administration/WIAdministrationWorldUGUI.prefab";
        private const string TerritoryPrefabPath = "Assets/Prefabs/Administration/WIAdministrationTerritoryUGUI.prefab";
        private const string TopHudPrefabPath = "Assets/Prefabs/Administration/WIAdministrationTopHUDUGUI.prefab";
        private const string EndTurnPrefabPath = "Assets/Prefabs/Administration/WIAdministrationEndTurnUGUI.prefab";
        private const string FocusProjectPrefabPath = "Assets/Prefabs/Administration/WIAdministrationFocusProjectUGUI.prefab";
        private const string HeroAssignmentPrefabPath = "Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab";
        private const string CharacterActivityPrefabPath = "Assets/Prefabs/Administration/WIAdministrationCharacterActivityUGUI.prefab";
        private const string SpecialFacilityPrefabPath = "Assets/Prefabs/Administration/WIAdministrationSpecialFacilityUGUI.prefab";
        private const string BasicFacilityPrefabPath = "Assets/Prefabs/Administration/WIAdministrationBasicFacilityUGUI.prefab";
        private const string DelegationPrefabPath = "Assets/Prefabs/Administration/WIAdministrationDelegationUGUI.prefab";
        private const string MarchPrefabPath = "Assets/Prefabs/Administration/WIAdministrationMarchUGUI.prefab";
        private const string CastleRecordPrefabPath = "Assets/Prefabs/Administration/WIAdministrationCastleRecordUGUI.prefab";
        private const string ObjectivePrefabPath = "Assets/Prefabs/Administration/WIAdministrationObjectiveUGUI.prefab";
        private const string MonthlyReportPrefabPath = "Assets/Prefabs/Administration/WIAdministrationMonthlyReportUGUI.prefab";
        private const string MilitaryPrefabPath = "Assets/Prefabs/Administration/WIAdministrationMilitaryUGUI.prefab";
        private const string HeroesPrefabPath = "Assets/Prefabs/Administration/WIAdministrationHeroesUGUI.prefab";
        private const string DiplomacyPrefabPath = "Assets/Prefabs/Administration/WIAdministrationDiplomacyUGUI.prefab";
        private const string SchemePrefabPath = "Assets/Prefabs/Administration/WIAdministrationSchemeUGUI.prefab";
        private const string ResearchPrefabPath = "Assets/Prefabs/Administration/WIAdministrationResearchUGUI.prefab";
        private const string FactionPrefabPath = "Assets/Prefabs/Administration/WIAdministrationFactionUGUI.prefab";
        private const string CouncilPrefabPath = "Assets/Prefabs/Administration/WIAdministrationCouncilUGUI.prefab";
        private const string EventChoicePrefabPath = "Assets/Prefabs/Administration/WIAdministrationEventChoiceUGUI.prefab";
        private const string TurnFollowupPrefabPath = "Assets/Prefabs/Administration/WIAdministrationTurnFollowupUGUI.prefab";
        private const string TurnFollowupInfoPanelPath = "Assets/Resources/UI/Generated/bg_type_a.png";
        private const string SystemPrefabPath = "Assets/Prefabs/Administration/WIAdministrationSystemUGUI.prefab";
        private const string AdministrationRuntimePrefabPath = "Assets/Prefabs/Administration/WIAdministrationUI.prefab";
        private const string AdministrationDatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        // 아레스의 얼굴 초상화와 전투 전신 Sprite가 올바른 원본 에셋을 사용하는지 확인합니다.
        [Test]
        public void AresUsesFacePortraitAndBattleSprite()
        {
            ProjectWI.Administration.WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<ProjectWI.Administration.WIAdministrationDatabaseSO>(AdministrationDatabasePath);
            ProjectWI.Administration.WIHeroDefinition ares = database.GetHero("ares");

            Assert.That(ares, Is.Not.Null);
            Assert.That(ares.Portrait, Is.Not.Null);
            Assert.That(ares.BattleSprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(ares.Portrait), Is.EqualTo("Assets/Art/Characters/Ares/Ares_Portrait_Face_V1.png"));
            Assert.That(AssetDatabase.GetAssetPath(ares.BattleSprite), Is.EqualTo("Assets/Art/Characters/Ares/Ares_Battle_1WU_A_OutlineBake_V1.png"));
            Assert.That(ares.BattleSprite.texture.format, Is.Not.EqualTo(TextureFormat.RGB24));
        }

        // 전투 인원 밀도 테스트를 위해 전체 캐릭터가 아레스 전투 Sprite를 공유하는지 확인합니다.
        [Test]
        public void AllCharactersUseAresBattleSpriteForBattleDensityTest()
        {
            ProjectWI.Administration.WIAdministrationDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<ProjectWI.Administration.WIAdministrationDatabaseSO>(AdministrationDatabasePath);

            Assert.That(database.Heroes.Count, Is.EqualTo(1200));
            Assert.That(database.Heroes.Count(hero =>
                hero.Grade == ProjectWI.Administration.WICharacterGrade.Hero), Is.EqualTo(200));
            Assert.That(database.Heroes.Count(hero =>
                hero.Grade == ProjectWI.Administration.WICharacterGrade.Common), Is.EqualTo(1000));
            foreach (ProjectWI.Administration.WIHeroDefinition character in database.Heroes)
            {
                Assert.That(character.BattleSprite, Is.Not.Null, character.Id);
                Assert.That(
                    AssetDatabase.GetAssetPath(character.BattleSprite),
                    Is.EqualTo("Assets/Art/Characters/Ares/Ares_Battle_1WU_A_OutlineBake_V1.png"),
                    character.Id);
            }
        }

        // 캠페인 타이틀 프리팹이 레거시 Text 없이 Dynamic TMP 폰트만 사용하는지 확인합니다.
        [Test]
        public void CampaignTitlePrefabUsesDynamicTextMeshProFonts()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignTitlePrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);

            TMP_Text[] labels = prefab.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(labels.Length, Is.GreaterThan(0));

            foreach (TMP_Text label in labels)
            {
                Assert.That(label.font, Is.Not.Null, $"{label.name}에 TMP 폰트가 연결되지 않았습니다.");
                Assert.That(
                    label.font.atlasPopulationMode,
                    Is.EqualTo(AtlasPopulationMode.Dynamic),
                    $"{label.name}의 폰트가 Dynamic 모드가 아닙니다.");
            }
        }

        // 개별 화면 프리팹이 씬의 공용 EventSystem을 중복 생성하지 않는지 확인합니다.
        [Test]
        public void CampaignTitlePrefabDoesNotContainEventSystem()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignTitlePrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
        }

        // 캠페인 시작 화면이 열려 있는 동안 월드와 영지 UGUI가 함께 숨겨지는지 확인합니다.
        [Test]
        public void CampaignTitleVisibilityGatesWorldScreens()
        {
            const string bridgePath = "Assets/Scripts/Administration/WIAdministrationUIController.UGUIBridge.cs";
            const string worldSnapshotPath =
                "Assets/Scripts/Administration/WIAdministrationUIController.UGUIWorldSnapshot.cs";
            const string titleControllerPath = "Assets/Scripts/Administration/WICampaignTitleUGUIController.cs";
            string bridgeSource = System.IO.File.ReadAllText(bridgePath);
            string worldSnapshotSource = System.IO.File.ReadAllText(worldSnapshotPath);
            string titleControllerSource = System.IO.File.ReadAllText(titleControllerPath);

            StringAssert.Contains("private bool uguiCampaignTitleVisible = true;", bridgeSource);
            Assert.That(
                (bridgeSource + worldSnapshotSource).Split("uguiCampaignTitleVisible == false").Length - 1,
                Is.GreaterThanOrEqualTo(2),
                "월드와 영지 화면 모두 캠페인 타이틀 표시 상태를 확인해야 합니다.");
            StringAssert.Contains("SetUGUICampaignTitleVisibility(campaignStarted == false)", titleControllerSource);
            StringAssert.Contains("SetUGUICampaignTitleVisibility(true)", titleControllerSource);
        }

        // 월드 프리팹의 하단 명령 버튼 배열에 제거된 상단 HUD 버튼의 빈 참조가 남지 않았는지 확인합니다.
        [Test]
        public void WorldCommandButtonsContainNoMissingReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);
            ProjectWI.Administration.WIAdministrationWorldUGUIController controller =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationWorldUGUIController>();
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty buttons = serializedController.FindProperty("commandButtons");
            SerializedProperty actions = serializedController.FindProperty("commandActions");

            Assert.That(buttons.arraySize, Is.EqualTo(7));
            Assert.That(actions.arraySize, Is.EqualTo(buttons.arraySize));
            for (int index = 0; index < buttons.arraySize; index += 1)
            {
                Assert.That(buttons.GetArrayElementAtIndex(index).objectReferenceValue, Is.Not.Null, $"명령 버튼 {index}번 참조가 비어 있습니다.");
            }
        }

        // 월드와 영지 화면이 동일한 공용 다음 턴 중첩 프리팹을 사용하는지 확인합니다.
        [Test]
        public void WorldAndTerritoryUseSharedEndTurnPrefab()
        {
            GameObject sharedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EndTurnPrefabPath);
            GameObject worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);
            GameObject territoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerritoryPrefabPath);

            Assert.That(sharedPrefab, Is.Not.Null);
            AssertSharedEndTurnSource(worldPrefab);
            AssertSharedEndTurnSource(territoryPrefab);
        }

        // 화면 컨트롤러의 공용 다음 턴 참조가 지정 프리팹에서 온 것인지 확인합니다.
        private static void AssertSharedEndTurnSource(GameObject screenPrefab)
        {
            ProjectWI.Administration.WIAdministrationEndTurnUGUIController endTurn =
                screenPrefab.GetComponentInChildren<ProjectWI.Administration.WIAdministrationEndTurnUGUIController>(true);
            Assert.That(endTurn, Is.Not.Null, $"{screenPrefab.name}에 공용 다음 턴 버튼이 없습니다.");

            Object source = PrefabUtility.GetCorrespondingObjectFromSource(endTurn.gameObject);
            Assert.That(source, Is.Not.Null, $"{screenPrefab.name}의 다음 턴 버튼이 중첩 프리팹이 아닙니다.");
            Assert.That(AssetDatabase.GetAssetPath(source), Is.EqualTo(EndTurnPrefabPath));
        }

        // 행정 슈퍼 컨트롤러가 숨은 UI Toolkit 캠페인 카드를 더 이상 동적으로 만들지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerDoesNotBuildLegacyCampaignStartCards()
        {
            System.Reflection.MethodInfo legacyBuilder = typeof(ProjectWI.Administration.WIAdministrationUIController)
                .GetMethod("BuildCampaignStartScreen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(legacyBuilder, Is.Null);
        }

        // 행정 런타임 프리팹이 UI Toolkit 문서 없이 상태 브리지 컨트롤러만 유지하는지 확인합니다.
        [Test]
        public void AdministrationRuntimePrefabDoesNotRequireUIDocument()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdministrationRuntimePrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<UnityEngine.UIElements.UIDocument>(), Is.Null);
        }

        // 군사 기능이 레거시 UI Toolkit 모달 진입 메서드를 더 이상 보유하지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerContainsNoLegacyMilitaryModalEntryPoints()
        {
            System.Type controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public;
            string[] legacyMethods =
            {
                "OpenMilitaryModal", "OpenBattleSessionModal", "OpenArmyCastleModal", "OpenArmyCommanderModal",
                "OpenArmyDetailModal", "OpenArmyRoleModal", "OpenArmyMemberModal", "OpenArmyTargetModal",
                "OpenCastleMarchModal", "OpenUGUIMilitaryItem", "OpenUGUIArmyCreation"
            };
            foreach (string method in legacyMethods)
                Assert.That(controllerType.GetMethod(method, flags), Is.Null, $"레거시 군사 모달 메서드가 남아 있습니다: {method}");
        }

        // 인물과 시설 기능이 레거시 UI Toolkit 모달 진입 메서드를 더 이상 보유하지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerContainsNoLegacyCharacterModalEntryPoints()
        {
            System.Type controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public;
            string[] legacyMethods =
            {
                "OpenBasicFacilityModal", "OpenTavernQuestModal", "OpenQuestHeroModal",
                "OpenCharacterActivityModal", "OpenActivityTypeModal", "AddActivityButton",
                "OpenSocializeTargetModal", "OpenRecruitTargetModal", "AssignCharacterActivity",
                "GetRelationshipSummary", "OpenHeroAssignmentModal", "OpenHeroListModal", "OpenTitleModal"
            };

            foreach (string method in legacyMethods)
            {
                Assert.That(controllerType.GetMethod(method, flags), Is.Null, $"레거시 인물 모달 메서드가 남아 있습니다: {method}");
            }
        }

        // 중점 사업·연구·의회·시스템 기능이 레거시 UI Toolkit 화면 생성 메서드를 보유하지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerContainsNoLegacyGovernanceModalEntryPoints()
        {
            System.Type controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public;
            string[] legacyMethods =
            {
                "OpenFocusProjectModal", "OpenProjectManagerModal", "OpenFactionPolicyModal",
                "OpenResearchModal", "OpenResearcherModal", "OpenSystemModal", "AddSettingsControls",
                "SaveCampaignSlot", "LoadCampaignSlot"
            };

            foreach (string method in legacyMethods)
            {
                Assert.That(controllerType.GetMethod(method, flags), Is.Null, $"레거시 통치 모달 메서드가 남아 있습니다: {method}");
            }
        }

        // 외교·첩보·진영 정세 기능이 레거시 UI Toolkit 화면 생성 메서드를 보유하지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerContainsNoLegacyRealmRelationsModalEntryPoints()
        {
            System.Type controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public;
            string[] legacyMethods =
            {
                "OpenFactionOverviewModal", "OpenDiplomacyModal", "OpenDiplomacyTargetModal",
                "ExecuteDiplomaticCommand", "OpenSchemeModal", "OpenSchemeAgentModal",
                "OpenSchemeTargetCastleModal", "OpenSchemeTargetHeroModal", "ExecuteScheme"
            };

            foreach (string method in legacyMethods)
            {
                Assert.That(controllerType.GetMethod(method, flags), Is.Null, $"레거시 외교·첩보 모달 메서드가 남아 있습니다: {method}");
            }
        }

        // 월간 보고·영지관 위임·선택 사건 기능이 레거시 UI Toolkit 화면 생성 메서드를 보유하지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerContainsNoLegacyRealmReportModalEntryPoints()
        {
            System.Type controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public;
            string[] legacyMethods =
            {
                "OpenDelegationModal", "SetGovernorBudget", "OpenMonthlyReportModal",
                "OpenRegionalEventModal", "OpenOccupationEventModal", "OpenRecruitmentEventModal",
                "ResolveRecruitmentEventChoice", "OpenRelationshipEventModal",
                "ResolveRelationshipEventChoice", "OpenProjectEventModal", "ResolveProjectEvent",
                "ConfirmLegacyChoice"
            };

            foreach (string method in legacyMethods)
            {
                Assert.That(controllerType.GetMethod(method, flags), Is.Null, $"레거시 월간 보고 모달 메서드가 남아 있습니다: {method}");
            }
        }

        // 성 상세 기록과 특화 시설 선택이 레거시 UI Toolkit 모달 진입점을 보유하지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerContainsNoLegacyTerritoryModalEntryPoints()
        {
            System.Type controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public;
            string[] legacyMethods = { "OpenCastleRecordModal", "OpenSpecialFacilityModal" };

            foreach (string method in legacyMethods)
            {
                Assert.That(controllerType.GetMethod(method, flags), Is.Null, $"레거시 영지 모달 메서드가 남아 있습니다: {method}");
            }
        }

        // 턴 처리·전투 알림·캠페인 결과·튜토리얼이 레거시 UI Toolkit 화면 메서드를 보유하지 않는지 확인합니다.
        [Test]
        public void AdministrationControllerContainsNoLegacyTurnModalEntryPoints()
        {
            System.Type controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic |
                                                   System.Reflection.BindingFlags.Public;
            string[] legacyMethods =
            {
                "ExecuteTurnRoutine", "ShowTurnSummary", "AddPendingBattleActions",
                "ShowCampaignResult", "AddCampaignResultContent", "ShowCurrentTutorial",
                "AddPendingTutorial", "AddTutorialContent"
            };

            foreach (string method in legacyMethods)
            {
                Assert.That(controllerType.GetMethod(method, flags), Is.Null, $"레거시 턴 모달 메서드가 남아 있습니다: {method}");
            }
        }

        // 행정 런타임 컨트롤러가 UI Toolkit 형식과 공통 동적 모달 생성 코드에 더 이상 의존하지 않는지 확인합니다.
        [Test]
        public void AdministrationRuntimeControllerContainsNoUIToolkitPresentationCode()
        {
            string[] controllerPaths =
            {
                "Assets/Scripts/Administration/WIAdministrationUIController.cs",
                "Assets/Scripts/Administration/WIAdministrationUIController.World.cs",
                "Assets/Scripts/Administration/WIAdministrationUIController.Territory.cs",
                "Assets/Scripts/Administration/WIAdministrationUIController.Turn.cs"
            };
            string source = string.Empty;
            foreach (string path in controllerPaths)
            {
                source += System.IO.File.ReadAllText(path);
            }

            StringAssert.DoesNotContain("UnityEngine.UIElements", source);
            StringAssert.DoesNotContain("UIDocument", source);
            StringAssert.DoesNotContain("VisualElement", source);
            StringAssert.DoesNotContain("CreateModal", source);
            StringAssert.DoesNotContain("LegacyToolkitPresentationEnabled", source);
        }

        // 등록된 완성 UGUI 프리팹이 활성 상태로 생성되어 자체 표시 초기화와 이벤트 구독을 수행하는지 확인합니다.
        [Test]
        public void ScreenBootstrapInstantiatesRegisteredPrefabs()
        {
            GameObject root = new GameObject("BootstrapTestRoot");
            ProjectWI.Administration.WIAdministrationUIController administrationController =
                root.AddComponent<ProjectWI.Administration.WIAdministrationUIController>();
            GameObject first = new GameObject("FirstScreenPrefab");
            GameObject second = new GameObject("SecondScreenPrefab");
            GameObject firstChild = new GameObject("FirstScreenChild");
            firstChild.transform.SetParent(first.transform);
            ProjectWI.Administration.WIUIScreenManager screenManager =
                root.AddComponent<ProjectWI.Administration.WIUIScreenManager>();
            SerializedObject serialized = new SerializedObject(screenManager);
            serialized.FindProperty("administrationController").objectReferenceValue = administrationController;
            SerializedProperty screens = serialized.FindProperty("screenPrefabs");
            screens.arraySize = 2;
            screens.GetArrayElementAtIndex(0).objectReferenceValue = first;
            screens.GetArrayElementAtIndex(1).objectReferenceValue = second;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            typeof(ProjectWI.Administration.WIUIScreenManager)
                .GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(screenManager, null);

            Assert.That(root.transform.childCount, Is.EqualTo(2));
            Assert.That(root.transform.GetChild(0).gameObject.activeSelf, Is.True);
            Assert.That(root.transform.GetChild(1).gameObject.activeSelf, Is.True);
            Assert.That(UnityEditor.SceneVisibilityManager.instance.IsPickingDisabled(
                root.transform.GetChild(0).gameObject, false), Is.True);
            Assert.That(UnityEditor.SceneVisibilityManager.instance.IsPickingDisabled(
                root.transform.GetChild(0).GetChild(0).gameObject, false), Is.False);
            Assert.That(screenManager.TryGetScreenInstance(first, out GameObject firstInstance), Is.True);
            Assert.That(firstInstance, Is.SameAs(root.transform.GetChild(0).gameObject));
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(root);
        }

        // 일반 화면은 시작 시 만들지 않고 최초 타입 요청에서 한 번만 생성해 캐시하는지 확인합니다.
        [Test]
        public void ScreenBootstrapLazilyInstantiatesTypedScreensOnce()
        {
            GameObject root = new GameObject("LazyBootstrapTestRoot");
            ProjectWI.Administration.WIAdministrationUIController administrationController =
                root.AddComponent<ProjectWI.Administration.WIAdministrationUIController>();
            GameObject lazyPrefab = new GameObject("LazyScreenPrefab");
            lazyPrefab.AddComponent<WITestLazyUGUIPanelController>();
            ProjectWI.Administration.WIUIScreenManager screenManager =
                root.AddComponent<ProjectWI.Administration.WIUIScreenManager>();
            SerializedObject serialized = new SerializedObject(screenManager);
            serialized.FindProperty("administrationController").objectReferenceValue = administrationController;
            SerializedProperty screens = serialized.FindProperty("screenPrefabs");
            screens.arraySize = 1;
            screens.GetArrayElementAtIndex(0).objectReferenceValue = lazyPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            typeof(ProjectWI.Administration.WIUIScreenManager)
                .GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(screenManager, null);

            Assert.That(root.transform.childCount, Is.Zero);
            Assert.That(screenManager.EnsureScreen<WITestLazyUGUIPanelController>(), Is.True);
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(screenManager.EnsureScreen<WITestLazyUGUIPanelController>(), Is.True);
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(screenManager.TryGetScreen(out WITestLazyUGUIPanelController controller), Is.True);
            Assert.That(controller, Is.SameAs(root.transform.GetChild(0).GetComponent<WITestLazyUGUIPanelController>()));

            Object.DestroyImmediate(lazyPrefab);
            Object.DestroyImmediate(root);
        }

        // 지연 생성된 화면의 구독자가 첫 요청부터 이벤트와 인자를 받는지 확인합니다.
        [TestCase(false)]
        [TestCase(true)]
        public void LazyScreenReceivesFirstRequest(bool withArguments)
        {
            GameObject root = new GameObject("FirstRequestTestRoot");
            GameObject prefab = new GameObject("FirstRequestPrefab");
            try
            {
                var administration = root.AddComponent<ProjectWI.Administration.WIAdministrationUIController>();
                prefab.AddComponent<WITestLazyUGUIPanelController>();
                var manager = root.AddComponent<ProjectWI.Administration.WIUIScreenManager>();
                SerializedObject serialized = new SerializedObject(manager);
                serialized.FindProperty("administrationController").objectReferenceValue = administration;
                SerializedProperty screens = serialized.FindProperty("screenPrefabs");
                screens.arraySize = 1;
                screens.GetArrayElementAtIndex(0).objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(ProjectWI.Administration.WIUIScreenManager).GetMethod("Awake", flags).Invoke(manager, null);
                var controllerType = typeof(ProjectWI.Administration.WIAdministrationUIController);
                var request = controllerType.GetMethods(flags).Single(method =>
                    method.Name == "RaiseUGUIScreenRequest" &&
                    method.GetGenericArguments().Length == (withArguments ? 3 : 1));

                Assert.That(root.transform.childCount, Is.Zero);
                if (withArguments)
                {
                    System.Func<System.Action<ProjectWI.Administration.WIAdministrationReportActionType, string>> getter =
                        () => (System.Action<ProjectWI.Administration.WIAdministrationReportActionType, string>)
                            controllerType.GetField("UGUIEventChoiceRequested", flags).GetValue(administration);
                    request.MakeGenericMethod(typeof(WITestLazyUGUIPanelController),
                        typeof(ProjectWI.Administration.WIAdministrationReportActionType), typeof(string))
                        .Invoke(administration, new object[] { getter, default(ProjectWI.Administration.WIAdministrationReportActionType), "first-choice" });
                }
                else
                {
                    System.Func<System.Action> getter = () => (System.Action)
                        controllerType.GetField("UGUIDelegationRequested", flags).GetValue(administration);
                    request.MakeGenericMethod(typeof(WITestLazyUGUIPanelController))
                        .Invoke(administration, new object[] { getter });
                }

                Assert.That(manager.TryGetScreen(out WITestLazyUGUIPanelController screen), Is.True);
                Assert.That(screen.RequestCount, Is.EqualTo(1));
                if (withArguments)
                {
                    Assert.That(screen.LastId, Is.EqualTo("first-choice"));
                }
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(root);
            }
        }

        // 런타임에 생성되는 UGUI 프리팹 계층에 누락된 MonoBehaviour가 없는지 확인합니다.
        [Test]
        public void AdministrationUGUIPrefabsContainNoMissingScripts()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Administration" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("UGUI.prefab") == false) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                {
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero,
                        $"누락 스크립트가 있는 UGUI 프리팹: {path} · {child.name}");
                }
            }
        }

        // 월드 프리팹이 레거시 Text 없이 화면 전용 컨트롤러와 TMP만 사용하는지 확인합니다.
        [Test]
        public void WorldPrefabUsesDedicatedControllerAndTextMeshPro()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationWorldUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<TMP_Text>(true).Length, Is.GreaterThan(10));
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
        }

        // 성 상세 6개 행이 왼쪽 제목과 오른쪽 값 텍스트로 분리되어 직렬화되었는지 확인합니다.
        [Test]
        public void WorldCastleDetailRowsSeparateTitlesAndValues()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);
            ProjectWI.Administration.WIAdministrationWorldUGUIController controller =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationWorldUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty valueRows = serialized.FindProperty("castleDetailValueRows");
            Transform panel = prefab.transform.Find("WorldContent/WorldBody/CastleSummaryPanel");

            Assert.That(panel, Is.Not.Null);
            Assert.That(valueRows.arraySize, Is.EqualTo(6));
            for (int index = 0; index < 6; index += 1)
            {
                TMP_Text title = panel.Find($"CastleDetailRow{index + 1}")?.GetComponent<TMP_Text>();
                TMP_Text value = panel.Find($"CastleDetailValue{index + 1}")?.GetComponent<TMP_Text>();

                Assert.That(title, Is.Not.Null);
                Assert.That(value, Is.Not.Null);
                Assert.That(title.alignment, Is.EqualTo(TextAlignmentOptions.MidlineLeft));
                Assert.That(value.alignment, Is.EqualTo(TextAlignmentOptions.MidlineRight));
                Assert.That(title.rectTransform.anchorMax.x, Is.EqualTo(0.48f).Within(0.0001f));
                Assert.That(value.rectTransform.anchorMin.x, Is.EqualTo(0.48f).Within(0.0001f));
                Assert.That(valueRows.GetArrayElementAtIndex(index).objectReferenceValue, Is.EqualTo(value));
            }
        }

        // 월드 지도 프리팹에 60개 성 노드와 직렬화된 데이터 연결이 고정 배치되었는지 확인합니다.
        [Test]
        public void WorldMapPrefabContainsSixtyBoundCastleNodes()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);
            ProjectWI.Administration.WIAdministrationMapUGUIController controller =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationMapUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);

            Assert.That(controller, Is.Not.Null);
            Assert.That(serialized.FindProperty("castleButtons").arraySize, Is.EqualTo(60));
            Assert.That(serialized.FindProperty("castleMarkers").arraySize, Is.EqualTo(60));
            Assert.That(serialized.FindProperty("castleLabels").arraySize, Is.EqualTo(60));
            Assert.That(serialized.FindProperty("castleIds").arraySize, Is.EqualTo(60));
        }

        // 시나리오별 중복 월드 프리팹 없이 공용 월드 프리팹 하나만 유지하는지 확인합니다.
        [Test]
        public void WorldMapUsesSingleSharedPrefab()
        {
            const string freeWorldPrefabPath =
                "Assets/Prefabs/Administration/WIAdministrationWorldUGUI_Free.prefab";

            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(freeWorldPrefabPath), Is.Null);
        }

        // 영지 프리팹이 시안형 하단 요약에 맞는 영웅·시설 슬롯과 고정 명령을 갖는지 확인합니다.
        [Test]
        public void TerritoryPrefabContainsFixedSlotsAndDedicatedController()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerritoryPrefabPath);
            ProjectWI.Administration.WIAdministrationTerritoryUGUIController controller =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationTerritoryUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);
            Transform bottomSummaryPanel = prefab.transform.Find("TerritoryContent/BottomSummaryPanel");
            Image bottomSummaryImage = bottomSummaryPanel?.GetComponent<Image>();

            Assert.That(prefab, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("heroSlots").arraySize, Is.EqualTo(4));
            Assert.That(serialized.FindProperty("facilitySlots").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("facilitySlotButtons").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("commandButtons").arraySize, Is.EqualTo(6));
            Assert.That(serialized.FindProperty("basicFacilityButtons").arraySize, Is.EqualTo(4));
            Assert.That(serialized.FindProperty("basicFacilityActions").arraySize, Is.EqualTo(4));
            Assert.That(serialized.FindProperty("topHUD").objectReferenceValue, Is.Not.Null);
            Assert.That(bottomSummaryPanel, Is.Not.Null);
            Assert.That(bottomSummaryImage?.sprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(bottomSummaryImage.sprite),
                Is.EqualTo("Assets/Resources/UI/Generated/territory_bottom_panel_v1.png"));
        }

        // 월드와 성 내정 화면이 동일한 공용 상단 HUD 프리팹을 중첩 사용하는지 확인합니다.
        [Test]
        public void TerritoryTopHudMatchesWorldTopHud()
        {
            GameObject worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldPrefabPath);
            GameObject territoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerritoryPrefabPath);
            GameObject topHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopHudPrefabPath);
            Transform worldTop = worldPrefab.transform.Find("WorldContent/TopHUD");
            Transform territoryTop = territoryPrefab.transform.Find("TerritoryContent/TopHUD");
            ProjectWI.Administration.WIAdministrationTopHUDUGUIController topHudController =
                topHudPrefab.GetComponent<ProjectWI.Administration.WIAdministrationTopHUDUGUIController>();
            SerializedObject topHudSerialized = new SerializedObject(topHudController);

            Assert.That(topHudPrefab, Is.Not.Null);
            Assert.That(topHudController, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("factionLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("dateLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("goldLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("manaLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("influenceLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("monthlyReportButton").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("councilButton").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("researchButton").objectReferenceValue, Is.Not.Null);
            Assert.That(topHudSerialized.FindProperty("systemButton").objectReferenceValue, Is.Not.Null);
            Assert.That(worldTop, Is.Not.Null);
            Assert.That(territoryTop, Is.Not.Null);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(worldTop.gameObject),
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(TopHudPrefabPath)));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(territoryTop.gameObject),
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(TopHudPrefabPath)));
            Assert.That(territoryTop.childCount, Is.EqualTo(worldTop.childCount));
            Assert.That(territoryTop.GetComponent<RectTransform>().anchorMin,
                Is.EqualTo(worldTop.GetComponent<RectTransform>().anchorMin));
            Assert.That(territoryTop.GetComponent<RectTransform>().anchorMax,
                Is.EqualTo(worldTop.GetComponent<RectTransform>().anchorMax));
            Assert.That(territoryTop.GetComponent<RectTransform>().offsetMin,
                Is.EqualTo(worldTop.GetComponent<RectTransform>().offsetMin));
            Assert.That(territoryTop.GetComponent<RectTransform>().offsetMax,
                Is.EqualTo(worldTop.GetComponent<RectTransform>().offsetMax));

            for (int index = 0; index < worldTop.childCount; index += 1)
            {
                Transform expected = worldTop.GetChild(index);
                Transform actual = territoryTop.GetChild(index);
                RectTransform expectedRect = expected.GetComponent<RectTransform>();
                RectTransform actualRect = actual.GetComponent<RectTransform>();

                Assert.That(actual.name, Is.EqualTo(expected.name));
                Assert.That(actual.gameObject.activeSelf, Is.EqualTo(expected.gameObject.activeSelf));
                Assert.That(actualRect.anchorMin, Is.EqualTo(expectedRect.anchorMin), actual.name);
                Assert.That(actualRect.anchorMax, Is.EqualTo(expectedRect.anchorMax), actual.name);
                Assert.That(actualRect.offsetMin, Is.EqualTo(expectedRect.offsetMin), actual.name);
                Assert.That(actualRect.offsetMax, Is.EqualTo(expectedRect.offsetMax), actual.name);

                Image expectedImage = expected.GetComponent<Image>();
                Image actualImage = actual.GetComponent<Image>();
                Assert.That(actualImage == null, Is.EqualTo(expectedImage == null), actual.name);
                if (expectedImage != null)
                {
                    Assert.That(actualImage.color, Is.EqualTo(expectedImage.color), actual.name);
                    Object expectedSprite = new SerializedObject(expectedImage).FindProperty("m_Sprite").objectReferenceValue;
                    Object actualSprite = new SerializedObject(actualImage).FindProperty("m_Sprite").objectReferenceValue;
                    Assert.That(actualSprite, Is.EqualTo(expectedSprite), actual.name);
                }

                TMP_Text expectedText = expected.GetComponent<TMP_Text>();
                TMP_Text actualText = actual.GetComponent<TMP_Text>();
                Assert.That(actualText == null, Is.EqualTo(expectedText == null), actual.name);
                if (expectedText != null)
                {
                    Assert.That(actualText.fontSize, Is.EqualTo(expectedText.fontSize), actual.name);
                    Assert.That(actualText.alignment, Is.EqualTo(expectedText.alignment), actual.name);
                }
            }
        }

        // 중점 사업 프리팹이 공통 모달 프레임과 8개 사업·담당자 선택 슬롯을 갖는지 확인합니다.
        [Test]
        public void FocusProjectPrefabUsesReusableModalAndFixedOptions()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FocusProjectPrefabPath);
            ProjectWI.Administration.WIAdministrationFocusProjectUGUIController focus =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationFocusProjectUGUIController>();
            ProjectWI.Administration.WIAdministrationModalUGUIController modal =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>();
            SerializedObject serialized = new SerializedObject(focus);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(focus, Is.Not.Null);
            Assert.That(modal, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("projectNames").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("basicButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("intensiveButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("managerButtons").arraySize, Is.EqualTo(8));
        }

        // 영웅 배치 프리팹이 공통 모달과 페이지 가능한 8개 고정 후보 카드를 갖는지 확인합니다.
        [Test]
        public void HeroAssignmentPrefabUsesReusableModalAndPagedCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroAssignmentPrefabPath);
            ProjectWI.Administration.WIAdministrationHeroAssignmentUGUIController assignment =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationHeroAssignmentUGUIController>();
            ProjectWI.Administration.WIAdministrationModalUGUIController modal =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>();
            SerializedObject serialized = new SerializedObject(assignment);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(assignment, Is.Not.Null);
            Assert.That(modal, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("candidateButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("candidatePortraits").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("candidateLabels").arraySize, Is.EqualTo(8));
        }

        // 인재 활동 프리팹이 공통 모달, 4열 2행 후보 카드와 고정 검색 도구막대를 갖는지 확인합니다.
        [Test]
        public void CharacterActivityPrefabUsesReusableModalAndFixedSteps()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterActivityPrefabPath);
            ProjectWI.Administration.WIAdministrationCharacterActivityUGUIController activity =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationCharacterActivityUGUIController>();
            ProjectWI.Administration.WIAdministrationModalUGUIController modal =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>();
            SerializedObject serialized = new SerializedObject(activity);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(activity, Is.Not.Null);
            Assert.That(modal, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardPortraits").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("activityButtons").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("toolbarRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("searchInput").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("filterButtons").arraySize, Is.EqualTo(3));
            Assert.That(serialized.FindProperty("sortButton").objectReferenceValue, Is.Not.Null);
        }

        // 특화 시설 프리팹이 공통 모달과 8개의 고정 선택 카드를 갖는지 확인합니다.
        [Test]
        public void SpecialFacilityPrefabUsesReusableModalAndFixedCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpecialFacilityPrefabPath);
            ProjectWI.Administration.WIAdministrationSpecialFacilityUGUIController facility =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationSpecialFacilityUGUIController>();
            SerializedObject serialized = new SerializedObject(facility);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(facility, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("optionButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("optionIcons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("optionLabels").arraySize, Is.EqualTo(8));
        }

        // 기본 시설 프리팹이 네 시설 기능 버튼과 의뢰·담당자용 8개 카드를 갖는지 확인합니다.
        [Test]
        public void BasicFacilityPrefabContainsFacilityGuideAndQuestCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasicFacilityPrefabPath);
            ProjectWI.Administration.WIAdministrationBasicFacilityUGUIController facility =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationBasicFacilityUGUIController>();
            SerializedObject serialized = new SerializedObject(facility);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(facility, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("facilityRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("castleHallButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("marketButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("trainingGroundButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("tavernButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
        }

        // 영지관 위임 프리팹이 후보 8개와 방침 5개, 예산·전환 컨트롤을 고정 배치하는지 확인합니다.
        [Test]
        public void DelegationPrefabContainsFixedGovernorAndPolicyControls()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DelegationPrefabPath);
            ProjectWI.Administration.WIAdministrationDelegationUGUIController delegation =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationDelegationUGUIController>();
            SerializedObject serialized = new SerializedObject(delegation);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(delegation, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("governorButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("policyButtons").arraySize, Is.EqualTo(5));
            Assert.That(serialized.FindProperty("toggleButton").objectReferenceValue, Is.Not.Null);
        }

        // 원정 프리팹이 전투단·대장·목표에 재사용되는 8개 카드와 새 편성 버튼을 갖는지 확인합니다.
        [Test]
        public void MarchPrefabContainsPagedCardsAndCreateArmyControl()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MarchPrefabPath);
            ProjectWI.Administration.WIAdministrationMarchUGUIController march =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationMarchUGUIController>();
            SerializedObject serialized = new SerializedObject(march);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(march, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardImages").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("createArmyButton").objectReferenceValue, Is.Not.Null);
        }

        // 성 상세 기록 프리팹이 고정 성 이미지와 스크롤 본문을 갖는지 확인합니다.
        [Test]
        public void CastleRecordPrefabContainsImageAndScrollableBody()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CastleRecordPrefabPath);
            ProjectWI.Administration.WIAdministrationCastleRecordUGUIController record =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationCastleRecordUGUIController>();
            SerializedObject serialized = new SerializedObject(record);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(record, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("castleImage").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("bodyLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("scrollRect").objectReferenceValue, Is.Not.Null);
        }

        // 캠페인 목표 프리팹이 정세·조건·진행도와 확인 버튼을 고정 배치하는지 확인합니다.
        [Test]
        public void ObjectivePrefabContainsFixedObjectiveSections()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ObjectivePrefabPath);
            ProjectWI.Administration.WIAdministrationObjectiveUGUIController objective =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationObjectiveUGUIController>();
            SerializedObject serialized = new SerializedObject(objective);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(objective, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("situationLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("descriptionLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("progressLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("confirmButton").objectReferenceValue, Is.Not.Null);
        }

        // 월간 보고 프리팹이 요약·운영·소식 카드와 페이지 가능한 3개 중요 결정 카드를 갖는지 확인합니다.
        [Test]
        public void MonthlyReportPrefabContainsScrollableBodyAndActionSlots()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonthlyReportPrefabPath);
            ProjectWI.Administration.WIAdministrationMonthlyReportUGUIController report =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationMonthlyReportUGUIController>();
            SerializedObject serialized = new SerializedObject(report);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(report, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("summaryLabels").arraySize, Is.EqualTo(5));
            Assert.That(serialized.FindProperty("resourceLabels").arraySize, Is.EqualTo(3));
            Assert.That(serialized.FindProperty("operationCards").arraySize, Is.EqualTo(4));
            Assert.That(serialized.FindProperty("newsCards").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("actionButtons").arraySize, Is.EqualTo(3));
            Assert.That(serialized.FindProperty("actionThumbnails").arraySize, Is.EqualTo(3));
            Assert.That(serialized.FindProperty("filterButtons").arraySize, Is.EqualTo(4));
            Assert.That(serialized.FindProperty("filterNormalSprite").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("filterSelectedSprite").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("confirmButton").objectReferenceValue, Is.Not.Null);
        }

        // 군사 프리팹이 전투·전투단용 고정 카드와 편성 버튼을 갖는지 확인합니다.
        [Test]
        public void MilitaryPrefabContainsFixedCardsAndCreateControl()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MilitaryPrefabPath);
            ProjectWI.Administration.WIAdministrationMilitaryUGUIController military =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationMilitaryUGUIController>();
            SerializedObject serialized = new SerializedObject(military);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(military, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("itemButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("createButton").objectReferenceValue, Is.Not.Null);
        }

        // 전역 영웅 프리팹이 페이지 가능한 8개 카드와 작위 화면 복귀 버튼을 갖는지 확인합니다.
        [Test]
        public void HeroesPrefabContainsPagedCardsAndTitleNavigation()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroesPrefabPath);
            ProjectWI.Administration.WIAdministrationHeroesUGUIController heroes =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationHeroesUGUIController>();
            SerializedObject serialized = new SerializedObject(heroes);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(heroes, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardPortraits").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("backButton").objectReferenceValue, Is.Not.Null);
        }

        // 외교 프리팹이 대상·명령에 재사용되는 8개 카드와 페이지·복귀 컨트롤을 갖는지 확인합니다.
        [Test]
        public void DiplomacyPrefabContainsPagedCommandCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiplomacyPrefabPath);
            ProjectWI.Administration.WIAdministrationDiplomacyUGUIController diplomacy =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationDiplomacyUGUIController>();
            SerializedObject serialized = new SerializedObject(diplomacy);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(diplomacy, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("backButton").objectReferenceValue, Is.Not.Null);
        }

        // 첩보 프리팹이 종류·담당자·대상에 재사용되는 8개 카드와 단계 복귀 컨트롤을 갖는지 확인합니다.
        [Test]
        public void SchemePrefabContainsPagedStepCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SchemePrefabPath);
            ProjectWI.Administration.WIAdministrationSchemeUGUIController scheme =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationSchemeUGUIController>();
            SerializedObject serialized = new SerializedObject(scheme);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(scheme, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardPortraits").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("backButton").objectReferenceValue, Is.Not.Null);
        }

        // 연구 프리팹이 연구·담당자에 재사용되는 8개 카드와 페이지·복귀 컨트롤을 갖는지 확인합니다.
        [Test]
        public void ResearchPrefabContainsPagedResearchCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResearchPrefabPath);
            ProjectWI.Administration.WIAdministrationResearchUGUIController research =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationResearchUGUIController>();
            SerializedObject serialized = new SerializedObject(research);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(research, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardPortraits").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("backButton").objectReferenceValue, Is.Not.Null);
        }

        // 진영 정세 프리팹이 대륙 진영용 8개 고정 읽기 카드 슬롯을 갖는지 확인합니다.
        [Test]
        public void FactionPrefabContainsFixedOverviewCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FactionPrefabPath);
            ProjectWI.Administration.WIAdministrationFactionUGUIController faction =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationFactionUGUIController>();
            SerializedObject serialized = new SerializedObject(faction);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(faction, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardLabels").arraySize, Is.EqualTo(8));
        }

        // 의회 프리팹이 월간 진영 방침 선택용 고정 카드 슬롯을 갖는지 확인합니다.
        [Test]
        public void CouncilPrefabContainsFixedPolicyCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CouncilPrefabPath);
            ProjectWI.Administration.WIAdministrationCouncilUGUIController council =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationCouncilUGUIController>();
            SerializedObject serialized = new SerializedObject(council);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(council, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardLabels").arraySize, Is.EqualTo(8));
        }

        // 선택 사건 프리팹이 사건 종류에 공용으로 쓰는 8개 고정 선택 카드를 갖는지 확인합니다.
        [Test]
        public void EventChoicePrefabContainsFixedChoiceCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EventChoicePrefabPath);
            ProjectWI.Administration.WIAdministrationEventChoiceUGUIController eventChoice =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationEventChoiceUGUIController>();
            SerializedObject serialized = new SerializedObject(eventChoice);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(eventChoice, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("choiceButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("choiceLabels").arraySize, Is.EqualTo(8));
        }

        // 턴 후속 프리팹이 전용 튜토리얼 내용과 두 개의 고정 선택 버튼을 갖는지 확인합니다.
        [Test]
        public void TurnFollowupPrefabContainsFixedChoiceCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurnFollowupPrefabPath);
            ProjectWI.Administration.WIAdministrationTurnFollowupUGUIController followup =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationTurnFollowupUGUIController>();
            SerializedObject serialized = new SerializedObject(followup);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(followup, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("choiceButtons").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("choiceLabels").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("choiceDescriptionLabels").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("tutorialContent").objectReferenceValue, Is.Not.Null);
        }

        // 턴 후속 정보 패널의 테두리와 모든 사용처가 9-Slice 설정을 유지하는지 확인합니다.
        [Test]
        public void TurnFollowupInfoPanelsUseSlicedSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(TurnFollowupInfoPanelPath) as TextureImporter;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurnFollowupPrefabPath);
            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            int slicedPanelCount = 0;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spriteBorder.sqrMagnitude, Is.GreaterThan(0f));
            foreach (Image image in images)
            {
                if (image.name == "DescriptionPanel" || image.name == "ChecklistPanel" || image.name == "MapCheck" || image.name == "CommandCheck")
                {
                    Assert.That(AssetDatabase.GetAssetPath(image.sprite), Is.EqualTo(TurnFollowupInfoPanelPath), image.name);
                    Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), image.gameObject.name);
                    slicedPanelCount += 1;
                }
            }
            Assert.That(slicedPanelCount, Is.EqualTo(4));
        }

        // 성 내정 기능 모달이 구형 종이 헤더와 흰색 버튼을 다시 사용하지 않는지 확인합니다.
        [Test]
        public void TerritoryCommandModalsUseDarkMetalVisuals()
        {
            string[] prefabPaths =
            {
                FocusProjectPrefabPath,
                HeroAssignmentPrefabPath,
                CharacterActivityPrefabPath,
                SpecialFacilityPrefabPath,
                BasicFacilityPrefabPath,
                DelegationPrefabPath,
                MarchPrefabPath,
                CastleRecordPrefabPath
            };
            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Transform panel = prefab.transform.Find("ModalRoot/ModalPanel");
                Image panelImage = panel.GetComponent<Image>();
                Image headerImage = panel.Find("Header").GetComponent<Image>();
                Button closeButton = panel.Find("Header/CloseButton").GetComponent<Button>();

                Assert.That(AssetDatabase.GetAssetPath(panelImage.sprite), Does.EndWith("administration_modal_shell_v1.png"), prefabPath);
                Assert.That(headerImage.enabled, Is.False, prefabPath);
                Assert.That(AssetDatabase.GetAssetPath(closeButton.image.sprite), Does.EndWith("turn_followup_close_button_v1.png"), prefabPath);
                foreach (Button button in prefab.GetComponentsInChildren<Button>(true))
                {
                    string spritePath = button.image.sprite == null ? string.Empty : AssetDatabase.GetAssetPath(button.image.sprite);
                    Assert.That(spritePath, Does.Not.EndWith("button_normal.png"), $"{prefabPath}/{button.name}");
                }
            }
        }

        // 시스템 프리팹이 설정과 저장에 공용으로 쓰는 8개 고정 카드 슬롯을 갖는지 확인합니다.
        [Test]
        public void SystemPrefabContainsFixedSettingsAndSaveCards()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SystemPrefabPath);
            ProjectWI.Administration.WIAdministrationSystemUGUIController system =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationSystemUGUIController>();
            SerializedObject serialized = new SerializedObject(system);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(system, Is.Not.Null);
            Assert.That(prefab.GetComponent<ProjectWI.Administration.WIAdministrationModalUGUIController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("cardButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("cardLabels").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("previousButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("nextButton").objectReferenceValue, Is.Not.Null);
        }
    }
}
