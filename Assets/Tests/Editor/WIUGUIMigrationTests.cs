using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectWI.Tests.Editor
{
    public class WIUGUIMigrationTests
    {
        private const string CampaignTitlePrefabPath = "Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab";
        private const string WorldPrefabPath = "Assets/Prefabs/Administration/WIAdministrationWorldUGUI.prefab";
        private const string TerritoryPrefabPath = "Assets/Prefabs/Administration/WIAdministrationTerritoryUGUI.prefab";
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
        private const string SystemPrefabPath = "Assets/Prefabs/Administration/WIAdministrationSystemUGUI.prefab";
        private const string AdministrationRuntimePrefabPath = "Assets/Prefabs/Administration/WIAdministrationUI.prefab";

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

        // 등록된 완성 UGUI 프리팹이 부트스트랩 초기화에서 생성되는지 확인합니다.
        [Test]
        public void ScreenBootstrapInstantiatesRegisteredPrefabs()
        {
            GameObject root = new GameObject("BootstrapTestRoot");
            GameObject first = new GameObject("FirstScreenPrefab");
            GameObject second = new GameObject("SecondScreenPrefab");
            ProjectWI.Administration.WIAdministrationUGUIScreenBootstrap bootstrap =
                root.AddComponent<ProjectWI.Administration.WIAdministrationUGUIScreenBootstrap>();
            SerializedObject serialized = new SerializedObject(bootstrap);
            SerializedProperty screens = serialized.FindProperty("screenPrefabs");
            screens.arraySize = 2;
            screens.GetArrayElementAtIndex(0).objectReferenceValue = first;
            screens.GetArrayElementAtIndex(1).objectReferenceValue = second;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            typeof(ProjectWI.Administration.WIAdministrationUGUIScreenBootstrap)
                .GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(bootstrap, null);

            Assert.That(root.transform.childCount, Is.EqualTo(2));
            Assert.That(root.transform.GetChild(0).gameObject.activeSelf, Is.True);
            Assert.That(root.transform.GetChild(1).gameObject.activeSelf, Is.True);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(root);
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

        // 영지 프리팹이 화면 전용 컨트롤러와 고정 영웅·시설·명령 슬롯을 갖는지 확인합니다.
        [Test]
        public void TerritoryPrefabContainsFixedSlotsAndDedicatedController()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TerritoryPrefabPath);
            ProjectWI.Administration.WIAdministrationTerritoryUGUIController controller =
                prefab.GetComponent<ProjectWI.Administration.WIAdministrationTerritoryUGUIController>();
            SerializedObject serialized = new SerializedObject(controller);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<EventSystem>(true), Is.Empty);
            Assert.That(serialized.FindProperty("heroSlots").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("facilitySlots").arraySize, Is.EqualTo(2));
            Assert.That(serialized.FindProperty("commandButtons").arraySize, Is.EqualTo(8));
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

        // 인재 활동 프리팹이 공통 모달과 8개 후보 카드, 5개 고정 활동 버튼을 갖는지 확인합니다.
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
            Assert.That(serialized.FindProperty("activityButtons").arraySize, Is.EqualTo(6));
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

        // 기본 시설 프리팹이 고정 안내와 의뢰·담당자용 8개 카드를 갖는지 확인합니다.
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

        // 월간 보고 프리팹이 스크롤 본문과 페이지 가능한 6개 사건·전투 버튼을 갖는지 확인합니다.
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
            Assert.That(serialized.FindProperty("bodyLabel").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("scrollRect").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("actionButtons").arraySize, Is.EqualTo(6));
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

        // 턴 후속 프리팹이 결과와 튜토리얼용 고정 선택 카드를 갖는지 확인합니다.
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
            Assert.That(serialized.FindProperty("choiceButtons").arraySize, Is.EqualTo(8));
            Assert.That(serialized.FindProperty("choiceLabels").arraySize, Is.EqualTo(8));
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
