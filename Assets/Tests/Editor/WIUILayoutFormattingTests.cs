using System.IO;
using NUnit.Framework;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WIUILayoutFormattingTests
    {
        // 긴 캠페인 목표 문장이 모달 폭 안에서 줄바꿈되도록 전용 스타일이 유지되는지 검증합니다.
        [Test]
        public void ObjectiveModal_LongCopyUsesWrappingStyle()
        {
            string stylesheet = System.IO.File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string controller = System.IO.File.ReadAllText("Assets/Scripts/Administration/WIAdministrationUIController.cs");

            StringAssert.Contains(".objective-modal-copy", stylesheet);
            StringAssert.Contains("white-space:normal", stylesheet);
            StringAssert.Contains("AddToClassList(\"objective-modal-copy\")", controller);
            StringAssert.Contains("AddToClassList(\"objective-modal-progress\")", controller);
        }

        // 모든 공통 모달의 일반 문구와 버튼이 폭 안에서 줄바꿈되는 기본 규칙을 검증합니다.
        [Test]
        public void CommonModal_LabelsAndButtonsUseWrappingStyle()
        {
            string stylesheet = System.IO.File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");

            StringAssert.Contains(".modal-panel Label,.modal-panel Button", stylesheet);
            StringAssert.Contains("white-space:normal", stylesheet);
            StringAssert.Contains("max-width:100%", stylesheet);
        }

        // 공통 모달이 제목·닫기 버튼을 고정하고 긴 본문만 세로 스크롤하도록 구성되는지 검증합니다.
        [Test]
        public void CommonModal_LongContentUsesVerticalScrollView()
        {
            string stylesheet = System.IO.File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string controller = System.IO.File.ReadAllText("Assets/Scripts/Administration/WIAdministrationUIController.cs");

            StringAssert.Contains("new ScrollView(ScrollViewMode.Vertical)", controller);
            StringAssert.Contains("return scrollView.contentContainer", controller);
            StringAssert.Contains(".modal-scroll-view", stylesheet);
            StringAssert.Contains("max-height:88%", stylesheet);
            StringAssert.Contains("overflow:hidden", stylesheet);
        }

        // 월보 본문과 전투 행동 영역이 서로 다른 스크롤 영역으로 분리되는지 검증합니다.
        [Test]
        public void MonthlyReport_BattleActionsUseFixedFooter()
        {
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string controller = File.ReadAllText("Assets/Scripts/Administration/WIAdministrationUIController.cs");

            StringAssert.Contains("CreateModalWithFooter", controller);
            StringAssert.Contains("AddPendingBattleActions(battleFooter)", controller);
            StringAssert.Contains("modal-fixed-footer", stylesheet);
            StringAssert.Contains("전투 해결 필요", controller);
        }

        // 전투 장면이 좁은 포인트 광원 대신 전장 전체를 비추는 글로벌 2D 광원을 사용하는지 검증합니다.
        [Test]
        public void BattleScene_UsesGlobalLightForFullVisibility()
        {
            string battleScene = File.ReadAllText("Assets/Scenes/BattleScene.unity");

            StringAssert.Contains("m_Name: Global Light 2D", battleScene);
            StringAssert.Contains("m_LightType: 4", battleScene);
            StringAssert.Contains("m_Intensity: 1.1", battleScene);
            StringAssert.Contains("m_ShadowsEnabled: 0", battleScene);
        }

        // 콘셉트 시안의 청회색 지휘실 테마가 전략 UI와 전투 HUD에 함께 유지되는지 검증합니다.
        [Test]
        public void StrategyConceptTheme_StylesPanelsButtonsAndBattleHud()
        {
            string administration = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string battle = File.ReadAllText("Assets/UI/Battle/WIBattleHUD.uss");

            StringAssert.Contains("--wi-panel:#0d151d", administration);
            StringAssert.Contains(".modal-header { min-height:64px", administration);
            StringAssert.Contains(".turn-button { width:190px", administration);
            StringAssert.Contains(".modal-fixed-footer { max-height:225px", administration);
            StringAssert.Contains("전략 화면 V2와 동일한 청회색", battle);
            StringAssert.Contains("background-color: rgb(34, 75, 109)", battle);
        }

        // ImageGen으로 제작한 아이콘·버튼·팝업 이미지가 런타임 UI 연결 경로에 모두 존재하는지 검증합니다.
        [Test]
        public void GeneratedUIArtwork_IsPresentAndConnected()
        {
            string[] assetNames =
            {
                "button_normal.png", "button_primary.png", "button_danger.png", "popup_panel.png",
                "popup_header.png",
                "icon_military.png", "icon_heroes.png", "icon_diplomacy.png", "icon_scheme.png",
                "icon_research.png", "icon_council.png", "icon_report.png", "icon_turn.png",
                "castle_stat_prosperity.png", "castle_stat_technology.png",
                "castle_stat_stability.png", "castle_stat_defense.png",
                "hero_slot_card.png", "facility_slot_card.png"
            };
            foreach (string assetName in assetNames)
            {
                Assert.IsTrue(File.Exists(Path.Combine("Assets/Resources/UI/Generated", assetName)), assetName);
            }

            string administrationUxml = File.ReadAllText("Assets/UI/Administration/WIAdministration.uxml");
            string administrationUss = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string battleUxml = File.ReadAllText("Assets/UI/Battle/WIBattleHUD.uxml");
            StringAssert.Contains("class=\"global-command generated-normal\"", administrationUxml);
            StringAssert.Contains("class=\"generated-command-icon icon-military\"", administrationUxml);
            StringAssert.Contains("project://database/Assets/Resources/UI/Generated/popup_panel.png", administrationUss);
            StringAssert.Contains("class=\"command-button generated-normal\"", battleUxml);
            StringAssert.Contains("castle-summary-panel", administrationUxml);
            StringAssert.Contains("campaign-summary-panel", administrationUxml);
            StringAssert.Contains("castle-stat-prosperity", administrationUxml);
            StringAssert.Contains("UI/Generated/castle_stat_prosperity.png", administrationUss);
            StringAssert.Contains("UI/Generated/hero_slot_card.png", administrationUss);
            StringAssert.Contains("UI/Generated/facility_slot_card.png", administrationUss);
        }

        // 밝은 팝업 헤더의 제목 대비와 축소 해상도 메뉴 아이콘의 절대 배치 규칙을 검증합니다.
        [Test]
        public void GeneratedUIArtwork_HeaderTitleAndCommandIconsRemainLegible()
        {
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");

            StringAssert.Contains(".modal-panel .modal-header .modal-title { color:#10161b; }", stylesheet);
            StringAssert.Contains(".global-command .generated-command-icon { position:absolute", stylesheet);
            StringAssert.Contains(".global-command { padding-left:31px", stylesheet);
            StringAssert.Contains(".turn-button .generated-command-icon { left:7px", stylesheet);
            StringAssert.Contains("UI/Generated/popup_header.png", stylesheet);
        }

        // 생성 버튼과 패널의 모서리가 크기 변경에도 늘어나지 않도록 9-Slice 규칙을 검증합니다.
        [Test]
        public void GeneratedUIArtwork_UsesNineSliceAndEqualCampaignButtonHeight()
        {
            string administration = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string battle = File.ReadAllText("Assets/UI/Battle/WIBattleHUD.uss");

            StringAssert.Contains(".generated-normal, .generated-primary, .generated-danger, .modal-panel Button, .modal-close", administration);
            StringAssert.Contains("-unity-slice-left:28", administration);
            StringAssert.Contains(".campaign-start-button, .campaign-continue-button", administration);
            StringAssert.Contains("height:50px", administration);
            StringAssert.Contains("-unity-slice-left:28", battle);
        }

        // 긴 이름은 지정 폭에 맞춰 생략 기호를 포함한 길이로 축약되는지 검증합니다.
        [Test]
        public void TruncateLabel_LongName_PreservesMaximumLength()
        {
            string result = WIAdministrationUIController.TruncateLabel("북부 변경의 영원한 별빛 수호 대성채", 12);

            Assert.AreEqual(12, result.Length);
            StringAssert.EndsWith("…", result);
        }

        // 큰 한국어 자원 값은 만 단위로 줄이고 작은 값은 정확한 숫자를 유지하는지 검증합니다.
        [Test]
        public void FormatHudNumber_Korean_CompactsLargeValues()
        {
            Assert.AreEqual("987.7만", WIAdministrationUIController.FormatHudNumber(9876543, false));
            Assert.AreEqual("9,876", WIAdministrationUIController.FormatHudNumber(9876, false));
        }

        // 영문 UI의 큰 자원 값은 M·K 단위와 음수 부호를 유지하는지 검증합니다.
        [Test]
        public void FormatHudNumber_English_UsesCompactSuffixes()
        {
            Assert.AreEqual("9.9M", WIAdministrationUIController.FormatHudNumber(9876543, true));
            Assert.AreEqual("-654.3K", WIAdministrationUIController.FormatHudNumber(-654321, true));
        }

        // 행정과 전투 UI가 호버·비활성·위험 상태 규칙을 모두 선언하는지 검증합니다.
        [Test]
        public void InteractionStyles_DefineRequiredStates()
        {
            string administration = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string battle = File.ReadAllText("Assets/UI/Battle/WIBattleHUD.uss");

            StringAssert.Contains(".global-command:hover", administration);
            StringAssert.Contains(".castle-command:disabled", administration);
            StringAssert.Contains(".qa-danger-state", administration);
            StringAssert.Contains(".command-button:hover", battle);
            StringAssert.Contains(".command-button:disabled", battle);
            StringAssert.Contains(".retreat:hover", battle);
        }

        // ESC는 열린 모달을 먼저 닫고 그다음 성 화면에서 전역으로 복귀하는지 검증합니다.
        [Test]
        public void ResolveShortcutAction_Escape_UsesNavigationPriority()
        {
            Assert.AreEqual(WIAdministrationShortcutAction.CloseModal,
                WIAdministrationUIController.ResolveShortcutAction(KeyCode.Escape, true, true, false, false));
            Assert.AreEqual(WIAdministrationShortcutAction.ReturnToGlobal,
                WIAdministrationUIController.ResolveShortcutAction(KeyCode.Escape, false, true, false, false));
            Assert.AreEqual(WIAdministrationShortcutAction.None,
                WIAdministrationUIController.ResolveShortcutAction(KeyCode.Escape, false, false, false, false));
        }

        // 전역 단축키는 정상 화면에서만 실행되고 모달·성·텍스트 입력 중에는 차단되는지 검증합니다.
        [Test]
        public void ResolveShortcutAction_GlobalCommands_RespectInputContext()
        {
            Assert.AreEqual(WIAdministrationShortcutAction.Military,
                WIAdministrationUIController.ResolveShortcutAction(KeyCode.M, false, false, false, false));
            Assert.AreEqual(WIAdministrationShortcutAction.EndTurn,
                WIAdministrationUIController.ResolveShortcutAction(KeyCode.T, false, false, false, false));
            Assert.AreEqual(WIAdministrationShortcutAction.None,
                WIAdministrationUIController.ResolveShortcutAction(KeyCode.T, true, false, false, false));
            Assert.AreEqual(WIAdministrationShortcutAction.None,
                WIAdministrationUIController.ResolveShortcutAction(KeyCode.D, false, false, false, true));
        }

        // 자원 툴팁이 현재값, 다음 턴 증가량, 소유 성 수와 계산 근거를 모두 포함하는지 검증합니다.
        [Test]
        public void BuildResourceCalculationTooltip_IncludesDecisionEvidence()
        {
            string tooltip = WIAdministrationUIController.BuildResourceCalculationTooltip(
                "금화", 1234567, 890, 3, "번영 × 규모 × 치안");

            StringAssert.Contains("1,234,567", tooltip);
            StringAssert.Contains("+890", tooltip);
            StringAssert.Contains("소유 성 3개", tooltip);
            StringAssert.Contains("근거: 번영 × 규모 × 치안", tooltip);
        }

        // 계략 성공률이 기본 확률과 담당 지력에서 대상 치안·방첩을 차감해 계산되는지 검증합니다.
        [Test]
        public void CalculateSchemeSuccessChance_UsesVisibleFactors()
        {
            WISchemeDefinition scheme = new WISchemeDefinition();
            WIHeroDefinition agent = new WIHeroDefinition();
            WICastleRuntimeState target = new WICastleRuntimeState { Stability = 40, CounterintelligenceMonths = 1 };
            typeof(WISchemeDefinition).GetField("baseSuccessChance", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)?.SetValue(scheme, 60);
            typeof(WIHeroDefinition).GetField("intelligence", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)?.SetValue(agent, 80);

            Assert.AreEqual(60, WISchemeSystem.CalculateSuccessChance(scheme, agent, target));
        }

        // 다섯 세력이 색상 없이도 중복되지 않는 고유 코드로 구분되는지 검증합니다.
        [Test]
        public void FactionAccessibilityCodes_AreUniqueAndStable()
        {
            string[] codes =
            {
                WIAdministrationUIController.GetFactionAccessibilityCode("avalon"),
                WIAdministrationUIController.GetFactionAccessibilityCode("valdor"),
                WIAdministrationUIController.GetFactionAccessibilityCode("ironheart"),
                WIAdministrationUIController.GetFactionAccessibilityCode("sylvanroad"),
                WIAdministrationUIController.GetFactionAccessibilityCode("necropolis")
            };

            CollectionAssert.AllItemsAreUnique(codes);
            CollectionAssert.DoesNotContain(codes, "??");
            Assert.AreEqual("AV", codes[0]);
            Assert.AreEqual("NC", codes[4]);
        }
    }
}
