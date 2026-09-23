using System.Reflection;
using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.EditorTools
{
    public static class WICharacterSelectionListQA
    {
        // 실제 군사 화면의 대장 목록을 열어 초상·스크롤·고정 하단 버튼을 확인합니다.
        [MenuItem("WI/QA/Preview/Character Rows Military")]
        public static void Military()
        {
            if (Application.isPlaying == false)
            {
                return;
            }
            var controller = Object.FindFirstObjectByType<WIAdministrationUIController>();
            controller.OpenMilitaryUGUIForQA();
            var panel = Object.FindFirstObjectByType<WIAdministrationMilitaryUGUIController>(FindObjectsInactive.Include);
            typeof(WIAdministrationMilitaryUGUIController).GetMethod("Push", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(panel, new object[] { "commanders", "castle_00", 0 });
        }
    }
}
