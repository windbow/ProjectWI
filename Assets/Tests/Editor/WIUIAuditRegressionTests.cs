using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Tests.Editor
{
    public class WIUIAuditRegressionTests
    {
        // 초상과 본문이 함께 있는 후보 카드의 영역이 다시 겹치지 않는지 확인합니다.
        [TestCase("Heroes")]
        [TestCase("CharacterActivity")]
        [TestCase("HeroAssignment")]
        [TestCase("Delegation")]
        public void CandidatePortraitAndTextHaveSeparateRegions(string screen)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Administration/WIAdministration" + screen + "UGUI.prefab");
            Transform cards = prefab.GetComponentsInChildren<Transform>(true).First(item => item.name == "CandidateCards");
            foreach (Transform card in cards)
            {
                Image portrait = card.GetComponentsInChildren<Image>(true).FirstOrDefault(item => item.name == "Portrait");
                if (portrait == null)
                {
                    continue;
                }
                Assert.That(portrait.preserveAspect, Is.True, card.name);
                foreach (TMP_Text label in card.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.That(label.rectTransform.anchorMin.x, Is.GreaterThan(portrait.rectTransform.anchorMax.x), card.name);
                    Assert.That(label.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis), card.name);
                }
            }
        }

        // 월간 보고의 고정 좌표 직계 요소가 모달 테두리 밖으로 배치되지 않는지 확인합니다.
        [Test]
        public void MonthlyReportFixedWidgetsRemainInsidePanel()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Administration/WIAdministrationMonthlyReportUGUI.prefab");
            RectTransform panel = prefab.GetComponentsInChildren<RectTransform>(true).First(item => item.name == "ModalPanel");
            Vector3[] corners = new Vector3[4];
            foreach (RectTransform child in panel)
            {
                child.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 point = panel.InverseTransformPoint(corner);
                    Assert.That(point.x, Is.InRange(panel.rect.xMin - 1f, panel.rect.xMax + 1f), child.name);
                    Assert.That(point.y, Is.InRange(panel.rect.yMin - 1f, panel.rect.yMax + 1f), child.name);
                }
            }
        }

        // 반복 지시가 활동 단계의 표시 상태를 따라가며 이동 버튼과 분리되는지 확인합니다.
        [Test]
        public void RepeatOrderBelongsToActivityStep()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Administration/WIAdministrationCharacterActivityUGUI.prefab");
            RectTransform repeat = prefab.GetComponentsInChildren<RectTransform>(true).First(item => item.name == "RepeatOrderButton");
            Assert.That(repeat.parent.name, Is.EqualTo("ActivityRoot"));
            Assert.That(repeat.anchoredPosition.y + repeat.rect.height * .5f, Is.LessThan(0));
        }

        // 전투 스킬 UI가 동적 버튼 증식 없이 저장된 네 슬롯과 페이지 이동으로 구성되는지 확인합니다.
        [Test]
        public void BattleSkillSlotsAreAuthoredAndBounded()
        {
            string layout = File.ReadAllText("Assets/UI/Battle/WIBattleHUD.uxml");
            Assert.That(System.Text.RegularExpressions.Regex.Matches(layout, "name=\"skill-slot-[0-9]+\"").Count, Is.EqualTo(4));
            Assert.That(layout, Does.Contain("name=\"skill-previous\""));
            Assert.That(layout, Does.Contain("name=\"skill-next\""));
            Assert.That(File.ReadAllText("Assets/Scripts/Battle/WIBattleHUDController.cs"), Does.Not.Contain("new Button("));
        }
    }
}
