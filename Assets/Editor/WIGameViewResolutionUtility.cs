using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ProjectWI.Administration;

namespace ProjectWI.Editor
{
    public static class WIGameViewResolutionUtility
    {
        [MenuItem("WI/QA/Game View/1366x768")]
        private static void Set1366x768() => SetResolution(1366, 768);

        [MenuItem("WI/QA/Game View/1920x1080")]
        private static void Set1920x1080() => SetResolution(1920, 1080);

        [MenuItem("WI/QA/Game View/2560x1440")]
        private static void Set2560x1440() => SetResolution(2560, 1440);

        [MenuItem("WI/QA/Preview/Global Map")]
        private static void OpenGlobalPreview() => FindController()?.OpenGlobalPreviewForQA();

        [MenuItem("WI/QA/Preview/Rimgard Castle")]
        private static void OpenCastlePreview() => FindController()?.OpenCastlePreviewForQA();

        [MenuItem("WI/QA/Preview/Focus Project UGUI")]
        private static void OpenFocusProjectUGUIPreview() => FindController()?.OpenFocusProjectUGUIForQA();

        [MenuItem("WI/QA/Preview/Hero Assignment UGUI")]
        private static void OpenHeroAssignmentUGUIPreview() => FindController()?.OpenHeroAssignmentUGUIForQA();

        [MenuItem("WI/QA/Preview/Character Activity UGUI")]
        private static void OpenCharacterActivityUGUIPreview() => FindController()?.OpenCharacterActivityUGUIForQA();

        [MenuItem("WI/QA/Preview/Special Facility UGUI")]
        private static void OpenSpecialFacilityUGUIPreview() => FindController()?.OpenSpecialFacilityUGUIForQA();

        [MenuItem("WI/QA/Preview/Basic Facility UGUI")]
        private static void OpenBasicFacilityUGUIPreview() => FindController()?.OpenBasicFacilityUGUIForQA();

        [MenuItem("WI/QA/Preview/Delegation UGUI")]
        private static void OpenDelegationUGUIPreview() => FindController()?.OpenDelegationUGUIForQA();

        [MenuItem("WI/QA/Preview/March UGUI")]
        private static void OpenMarchUGUIPreview() => FindController()?.OpenMarchUGUIForQA();

        [MenuItem("WI/QA/Preview/Castle Record UGUI")]
        private static void OpenCastleRecordUGUIPreview() => FindController()?.OpenCastleRecordUGUIForQA();

        [MenuItem("WI/QA/Preview/Objective UGUI")]
        private static void OpenObjectiveUGUIPreview() => FindController()?.OpenObjectiveUGUIForQA();

        [MenuItem("WI/QA/Preview/Monthly Report UGUI")]
        private static void OpenMonthlyReportUGUIPreview() => FindController()?.OpenMonthlyReportUGUIForQA();

        [MenuItem("WI/QA/Preview/Military UGUI")]
        private static void OpenMilitaryUGUIPreview() => FindController()?.OpenMilitaryUGUIForQA();

        [MenuItem("WI/QA/Preview/Heroes UGUI")]
        private static void OpenHeroesUGUIPreview() => FindController()?.OpenHeroesUGUIForQA();

        [MenuItem("WI/QA/Preview/Diplomacy UGUI")]
        private static void OpenDiplomacyUGUIPreview() => FindController()?.OpenDiplomacyUGUIForQA();

        [MenuItem("WI/QA/Preview/Scheme UGUI")]
        private static void OpenSchemeUGUIPreview() => FindController()?.OpenSchemeUGUIForQA();

        [MenuItem("WI/QA/Preview/Research UGUI")]
        private static void OpenResearchUGUIPreview() => FindController()?.OpenResearchUGUIForQA();

        [MenuItem("WI/QA/Preview/Faction UGUI")]
        private static void OpenFactionUGUIPreview() => FindController()?.OpenFactionUGUIForQA();

        [MenuItem("WI/QA/Preview/Council UGUI")]
        private static void OpenCouncilUGUIPreview() => FindController()?.OpenCouncilUGUIForQA();

        [MenuItem("WI/QA/Preview/System UGUI")]
        private static void OpenSystemUGUIPreview()
        {
            WIAdministrationUIController controller = FindController();
            if (controller == null) return;
            controller.OpenGlobalPreviewForQA();
            controller.OpenUGUISystem();
        }

        [MenuItem("WI/QA/Preview/Advance Turn UGUI")]
        private static void AdvanceTurnUGUIPreview() => FindController()?.AdvanceTurnUGUIForQA();

        [MenuItem("WI/QA/Preview/New Campaign Onboarding UGUI")]
        private static void OpenNewCampaignOnboardingUGUIPreview()
        {
            WIAdministrationUIController controller = FindController();
            if (controller == null) return;
            controller.BeginCampaign(WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WICampaignTitleUGUIController title = UnityEngine.Object.FindFirstObjectByType<WICampaignTitleUGUIController>(FindObjectsInactive.Include);
            if (title != null) title.gameObject.SetActive(false);
        }

        [MenuItem("WI/QA/Preview/Common Message UGUI")]
        private static void OpenCommonMessageUGUIPreview() => FindController()?.OpenMessageUGUIForQA();

        [MenuItem("WI/QA/Capture/Current Game View")]
        private static void CaptureCurrentGameView()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("Game View 캡처는 플레이 모드에서 실행해야 합니다.");
                return;
            }
            const string path = "Assets/Screenshots/UI_Current_Preview.png";
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log($"Game View 캡처 요청 · {path}");
        }

        // 플레이 모드에서 내정 UI 컨트롤러를 찾아 QA 화면 전환에 사용합니다.
        private static WIAdministrationUIController FindController()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("UI 미리보기는 플레이 모드에서 실행해야 합니다.");
                return null;
            }
            WIAdministrationUIController controller = UnityEngine.Object.FindFirstObjectByType<WIAdministrationUIController>();
            if (controller == null) Debug.LogError("내정 UI 컨트롤러를 찾지 못했습니다.");
            return controller;
        }

        // Unity 내부 Game View 크기 목록에 고정 해상도를 추가하고 즉시 선택합니다.
        private static void SetResolution(int width, int height)
        {
            Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            MethodInfo getGroup = sizesType.GetMethod("GetGroup");
            object group = getGroup?.Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });
            if (group == null || sizeType == null || gameViewType == null)
            {
                Debug.LogError("Game View 해상도 목록을 찾지 못했습니다.");
                return;
            }

            MethodInfo getTotalCount = group.GetType().GetMethod("GetTotalCount");
            MethodInfo getSize = group.GetType().GetMethod("GetGameViewSize");
            int count = (int)getTotalCount.Invoke(group, null);
            int selectedIndex = -1;
            for (int index = 0; index < count; index += 1)
            {
                object existing = getSize.Invoke(group, new object[] { index });
                int existingWidth = (int)sizeType.GetProperty("width")?.GetValue(existing);
                int existingHeight = (int)sizeType.GetProperty("height")?.GetValue(existing);
                if (existingWidth == width && existingHeight == height)
                {
                    selectedIndex = index;
                    break;
                }
            }

            if (selectedIndex < 0)
            {
                object fixedKind = Enum.Parse(sizeKindType, "FixedResolution");
                ConstructorInfo constructor = sizeType.GetConstructor(new[]
                    { sizeKindType, typeof(int), typeof(int), typeof(string) });
                object newSize = constructor.Invoke(new object[] { fixedKind, width, height, $"WI {width}x{height}" });
                group.GetType().GetMethod("AddCustomSize")?.Invoke(group, new[] { newSize });
                selectedIndex = (int)getTotalCount.Invoke(group, null) - 1;
            }

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            PropertyInfo selectedSize = gameViewType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selectedSize?.SetValue(gameView, selectedIndex);
            gameView.Repaint();
            Debug.Log($"Game View 해상도 전환 · {width}x{height}");
        }
    }
}
