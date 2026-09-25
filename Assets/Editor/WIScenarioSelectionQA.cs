using System.Reflection;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIScenarioSelectionQA
    {
        // 실제 프리팹의 시나리오 화면을 열며 저장 파일을 변경하지 않습니다.
        [MenuItem("WI/QA/Scenario/Show")]
        public static void Show()
        {
            var title = Object.FindFirstObjectByType<WICampaignTitleUGUIController>(FindObjectsInactive.Include);
            title.ShowCampaignStart();
            title.GetComponent<Canvas>().sortingOrder = 30000;
        }

        // 실제 프리 시나리오 버튼의 이벤트를 실행합니다.
        [MenuItem("WI/QA/Scenario/Select Free")]
        public static void SelectFree()
        {
            Click("variantButtons", 1);
        }

        // 실제 다음 버튼으로 설정 화면에 진입합니다.
        [MenuItem("WI/QA/Scenario/Next")]
        public static void Next()
        {
            Click("newCampaignButton", -1);
        }

        // 실제 이전 버튼으로 선택 화면에 복귀합니다.
        [MenuItem("WI/QA/Scenario/Back")]
        public static void Back()
        {
            Click("backButton", -1);
        }

        // 캡처 파일을 프로젝트 검증 폴더에 직접 기록합니다.
        [MenuItem("WI/QA/Scenario/Capture")]
        public static void Capture()
        {
            System.IO.Directory.CreateDirectory("GameDocuments/DesignAuditEvidence");
            ScreenCapture.CaptureScreenshot("GameDocuments/DesignAuditEvidence/ScenarioSelection-Current.png");
        }

        // 직렬화된 버튼을 찾아 클릭 이벤트를 호출합니다.
        private static void Click(string field, int index)
        {
            var title = Object.FindFirstObjectByType<WICampaignTitleUGUIController>(FindObjectsInactive.Include);
            var value = typeof(WICampaignTitleUGUIController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(title);
            var button = index < 0 ? (Button)value : ((Button[])value)[index];
            button.onClick.Invoke();
        }
    }
}
