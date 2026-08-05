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

        [MenuItem("WI/QA/Preview/Avalon Castle")]
        private static void OpenCastlePreview() => FindController()?.OpenCastlePreviewForQA();

        [MenuItem("WI/QA/Preview/Long Content Global")]
        private static void OpenLongContentGlobalPreview() => FindController()?.OpenLongContentGlobalPreviewForQA();

        [MenuItem("WI/QA/Preview/Long Content Castle")]
        private static void OpenLongContentCastlePreview() => FindController()?.OpenLongContentCastlePreviewForQA();

        [MenuItem("WI/QA/Preview/Interaction States")]
        private static void OpenInteractionStatePreview() => FindController()?.OpenInteractionStatePreviewForQA();

        [MenuItem("WI/QA/Preview/Calculation Tooltips")]
        private static void OpenCalculationTooltipPreview() => FindController()?.OpenCalculationTooltipPreviewForQA();

        [MenuItem("WI/QA/Preview/Color Vision")]
        private static void OpenColorVisionPreview() => FindController()?.OpenColorVisionPreviewForQA();

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
