using System.Reflection;
using NUnit.Framework;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Tests.Editor
{
    public class WIScenarioSelectionTests
    {
        // 실제 저장 프리팹을 별도 인스턴스로 로드하여 화면 전환을 검증합니다.
        private GameObject root;
        private WICampaignTitleUGUIController controller;

        // 런타임 상태나 사용자 저장을 건드리지 않고 UI 바인딩만 준비합니다.
        [SetUp]
        public void SetUp()
        {
            root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab");
            controller = root.GetComponent<WICampaignTitleUGUIController>();
            Invoke("BindSelectionView");
            Invoke("BindVariantCards");
            Invoke("BindDifficultyCards");
            Invoke("SelectVariant", WICampaignVariant.AresMain);
            Invoke("ShowSettings", false);
        }

        // 검사 인스턴스를 저장하지 않고 해제합니다.
        [TearDown]
        public void TearDown()
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        // 다음·이전 이동에서 시나리오와 난이도가 보존되고 새 캠페인을 성급히 시작하지 않는지 확인합니다.
        [Test]
        public void NextAndBackPreserveSelections()
        {
            Get<Button[]>("variantButtons")[1].onClick.Invoke();
            Get<Button[]>("difficultyButtons")[2].onClick.Invoke();
            var difficulty = Get<WICampaignDifficulty>("selectedDifficulty");
            Invoke("AdvanceSelection");
            Assert.That(Get<GameObject>("settingsPage").activeSelf, Is.True);
            Assert.That(Get<GameObject>("scenarioPage").activeSelf, Is.False);
            Get<Button>("backButton").onClick.Invoke();
            Assert.That(Get<GameObject>("scenarioPage").activeSelf, Is.True);
            Assert.That(Get<WICampaignVariant>("selectedVariant"), Is.EqualTo(WICampaignVariant.Free));
            Assert.That(Get<WICampaignDifficulty>("selectedDifficulty"), Is.EqualTo(difficulty));
        }

        // 선택에 따라 삽화·목표·실제 시작 성이 함께 바뀌고 예약 슬롯은 비노출인지 확인합니다.
        [Test]
        public void VariantSelectionUpdatesDetailsAndHidesReserved()
        {
            var mainSprite = Get<Image>("detailArtwork").sprite;
            var mainCastle = Get<TMP_Text>("detailCastle").text;
            Get<Button[]>("variantButtons")[1].onClick.Invoke();
            Assert.That(Get<Image>("detailArtwork").sprite, Is.Not.Null.And.Not.EqualTo(mainSprite));
            Assert.That(Get<TMP_Text>("detailCastle").text, Is.Not.EqualTo(mainCastle));
            Assert.That(Get<TMP_Text>("detailObjective").text, Is.EqualTo("대륙의 모든 성 점령"));
            Assert.That(Get<Button[]>("variantButtons")[2].gameObject.activeSelf, Is.False);
            Invoke("SelectVariant", WICampaignVariant.Reserved);
            Assert.That(Get<WICampaignVariant>("selectedVariant"), Is.EqualTo(WICampaignVariant.Free));
        }

        // 화면 참조·한영 문자열·입력 영역 누락을 확인합니다.
        [Test]
        public void PrefabReferencesLocalizationAndRaycastsAreReady()
        {
            Assert.That(Invoke("ValidateSelectionView"), Is.True);
            var db = Get<WIAdministrationDatabaseSO>("database");
            foreach (string uid in Get<string[]>("fixedLabelUids"))
            {
                Assert.That(db.GetText(uid), Is.Not.Empty.And.Not.EqualTo(uid));
            }
            foreach (Button button in Get<Button[]>("variantButtons"))
            {
                Assert.That(button.image.raycastTarget, Is.True);
            }
            Assert.That(Get<Button>("newCampaignButton").image.raycastTarget, Is.True);
        }

        // 비공개 화면 명령을 테스트 인스턴스에서만 호출합니다.
        private object Invoke(string name, params object[] arguments)
        {
            return typeof(WICampaignTitleUGUIController).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, arguments);
        }

        // 직렬화된 요소와 선택 상태를 읽습니다.
        private T Get<T>(string name)
        {
            return (T)typeof(WICampaignTitleUGUIController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
        }
    }
}
