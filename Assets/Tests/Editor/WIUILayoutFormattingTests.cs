using System.IO;
using NUnit.Framework;
using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWI.Tests.Editor
{
    public class WIUILayoutFormattingTests
    {
        // 분리된 영지 관리 UXML을 하나의 검증 문자열로 결합합니다.
        private static string ReadAdministrationLayout()
        {
            string[] paths = Directory.GetFiles("Assets/UI/Administration/Views", "*.uxml");
            System.Array.Sort(paths, System.StringComparer.Ordinal);
            string layout = File.ReadAllText("Assets/UI/Administration/WIAdministration.uxml");
            foreach (string path in paths)
            {
                layout += "\n" + File.ReadAllText(path);
            }

            return layout;
        }

        // 기능별 partial로 분리된 영지 관리 컨트롤러를 하나의 검증 문자열로 결합합니다.
        private static string ReadAdministrationController()
        {
            string[] paths = Directory.GetFiles("Assets/Scripts/Administration", "WIAdministrationUIController*.cs");
            System.Array.Sort(paths, System.StringComparer.Ordinal);
            string controller = string.Empty;
            foreach (string path in paths)
            {
                controller += "\n" + File.ReadAllText(path);
            }

            return controller;
        }

        // 루트 UXML이 분리된 화면 템플릿을 실제 VisualElement 트리로 조립하는지 검증합니다.
        [Test]
        public void AdministrationLayout_ComposesSeparatedTemplates()
        {
            VisualTreeAsset layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/UI/Administration/WIAdministration.uxml");
            Assert.IsNotNull(layout);

            TemplateContainer root = layout.CloneTree();
            Assert.IsNotNull(root.Q<VisualElement>("compact-hud"));
            Assert.IsNotNull(root.Q<VisualElement>("global-view"));
            Assert.IsNotNull(root.Q<VisualElement>("castle-view"));
            Assert.IsNotNull(root.Q<VisualElement>("modal-layer"));
            Assert.AreEqual(60, root.Query<Button>(className: "castle-node").ToList().Count);
        }

        // 긴 캠페인 목표 문장이 모달 폭 안에서 줄바꿈되도록 전용 스타일이 유지되는지 검증합니다.
        [Test]
        public void ObjectiveModal_LongCopyUsesWrappingStyle()
        {
            string stylesheet = System.IO.File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string controller = ReadAdministrationController();

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
            string controller = ReadAdministrationController();

            StringAssert.Contains("new ScrollView(ScrollViewMode.Vertical)", controller);
            StringAssert.Contains("return scrollView.contentContainer", controller);
            StringAssert.Contains(".modal-scroll-view", stylesheet);
            StringAssert.Contains("max-height:88%", stylesheet);
            StringAssert.Contains("overflow:hidden", stylesheet);
        }

        // 월간 보고 본문과 전투 행동 영역이 서로 다른 스크롤 영역으로 분리되는지 검증합니다.
        [Test]
        public void MonthlyReport_BattleActionsUseFixedFooter()
        {
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string controller = ReadAdministrationController();

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
                "hero_slot_card.png", "facility_slot_card.png",
                "map_castle_avalon.png", "map_castle_valdor.png", "map_castle_ironheart.png",
                "map_castle_sylvanroad.png", "map_castle_necropolis.png"
            };
            foreach (string assetName in assetNames)
            {
                Assert.IsTrue(File.Exists(Path.Combine("Assets/Resources/UI/Generated", assetName)), assetName);
            }

            string administrationUxml = ReadAdministrationLayout();
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

        // 영지 관리의 영웅과 특화 시설 목록이 분리된 스크롤 영역을 사용하는지 검증합니다.
        [Test]
        public void CastleSlots_UseSeparatedScrollableSections()
        {
            string layout = ReadAdministrationLayout();
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");

            StringAssert.Contains("slot-scroll hero-slot-scroll", layout);
            StringAssert.Contains("slot-scroll facility-slot-scroll", layout);
            StringAssert.Contains("name=\"hero-slots\"", layout);
            StringAssert.Contains("name=\"special-facility-slots\"", layout);
            StringAssert.Contains(".castle-slot-section", stylesheet);
            StringAssert.Contains(".slot-scroll { flex-grow:1; min-height:0; overflow:hidden; }", stylesheet);
            StringAssert.Contains(".facility-slot-grid .slot { width:100%", stylesheet);
        }

        // 이미지 버튼의 호버·클릭·비활성 상태가 단색 반전 대신 텍스처 틴트를 사용하는지 검증합니다.
        [Test]
        public void GeneratedButtons_UseImageAwareInteractionStates()
        {
            string administration = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string battle = File.ReadAllText("Assets/UI/Battle/WIBattleHUD.uss");

            StringAssert.Contains(".generated-normal:hover, .generated-primary:hover, .modal-panel Button:hover", administration);
            StringAssert.Contains("-unity-background-image-tint-color:#d9eaff", administration);
            StringAssert.Contains(".generated-danger:hover", administration);
            StringAssert.Contains("-unity-background-image-tint-color:#ffd1d1", administration);
            StringAssert.Contains(".generated-normal:disabled", administration);
            StringAssert.Contains(".generated-normal:hover, .generated-primary:hover, .skill-button:hover", battle);
            StringAssert.Contains("-unity-background-image-tint-color:rgb(217,234,255)", battle);
        }

        // 전략 지도와 영지 관리 화면이 시안형 성채 마커·좌우 패널·하단 관리 구조를 유지하는지 검증합니다.
        [Test]
        public void StrategyAndCastleViews_UseConceptLayoutAndCastleMarkers()
        {
            string layout = ReadAdministrationLayout();
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string controller = ReadAdministrationController();

            StringAssert.Contains("global-castle-hero-cards", layout);
            StringAssert.Contains("objective-progress-fill", layout);
            StringAssert.Contains("castle-overview-panel", layout);
            StringAssert.Contains(".castle-node-avalon .castle-node-marker", stylesheet);
            StringAssert.Contains("UI/Generated/map_castle_necropolis.png", stylesheet);
            StringAssert.Contains(".castle-overview-panel .governor-full", stylesheet);
            StringAssert.Contains("ApplyMapNodeFactionClass(node, castleState.FactionId)", controller);
            StringAssert.Contains("RefreshGlobalHeroCards(castle)", controller);
        }

        // 시안형 명조·고딕 폰트와 평면 버튼·명령 아이콘이 UXML/USS에 직접 연결되는지 검증합니다.
        [Test]
        public void StrategyTheme_UsesProjectFontsAndFlatCommandAssets()
        {
            string layout = ReadAdministrationLayout();
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");

            Assert.IsTrue(File.Exists("Assets/Fonts/NotoSerifKR-VariableFont_wght.ttf"));
            Assert.IsTrue(File.Exists("Assets/Fonts/NotoSansKR-VariableFont_wght.ttf"));
            StringAssert.Contains("NotoSansKR-VariableFont_wght.ttf", stylesheet);
            StringAssert.Contains("NotoSerifKR-VariableFont_wght.ttf", stylesheet);
            StringAssert.Contains(".app Label, .app Button, .app TextField, .app Toggle", stylesheet);
            StringAssert.Contains(".resource-chip, .side-panel-stats, .castle-stat", stylesheet);
            StringAssert.Contains("button_flat_normal.png", stylesheet);
            StringAssert.Contains("button_flat_primary.png", stylesheet);
            StringAssert.Contains("button_flat_danger.png", stylesheet);
            StringAssert.Contains("icon_flat_military.png", stylesheet);
            StringAssert.Contains("icon_flat_faction.png", stylesheet);
            StringAssert.Contains("hud_flat_gold.png", stylesheet);
            StringAssert.Contains("hud-resource-icon hud-date-icon", layout);
            StringAssert.Contains("generated-command-icon icon-faction", layout);
        }

        // 하단 전역 명령 8개는 D Type을 사용하고 다음 턴 버튼은 기존 B Type을 유지하는지 검증합니다.
        [Test]
        public void GlobalCommands_UseDTypeWhileTurnButtonKeepsBType()
        {
            string layout = ReadAdministrationLayout();
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");

            Assert.AreEqual(8, layout.Split(new[] { "class=\"global-command " }, System.StringSplitOptions.None).Length - 1);
            StringAssert.Contains("class=\"turn-button generated-primary\"", layout);
            StringAssert.Contains("background-image:url(\"project://database/Assets/Resources/UI/Generated/right_action_button.png\");", stylesheet);
            StringAssert.Contains("-unity-slice-left:28;", stylesheet);
            StringAssert.Contains("-unity-slice-right:28;", stylesheet);
            StringAssert.Contains("-unity-slice-top:20;", stylesheet);
            StringAssert.Contains("-unity-slice-bottom:20;", stylesheet);
        }

        // 전략 UI의 플레이어 표시 문구가 판타지 공식 용어를 사용하는지 검증합니다.
        [Test]
        public void AdministrationUI_UsesFantasyTerminology()
        {
            string layout = ReadAdministrationLayout();
            string controller = ReadAdministrationController();
            string combined = layout + controller;

            string[] retiredTerms = { "군주", "태수", "평정", "월보", "내정", "계략", "출정", "세력", "등용", "재야", "치안", "공적", "부대" };
            foreach (string retiredTerm in retiredTerms)
            {
                StringAssert.DoesNotContain(retiredTerm, combined, $"폐기 용어가 남았습니다: {retiredTerm}");
            }

            StringAssert.Contains("text=\"영웅\"", layout);
            StringAssert.Contains("text=\"첩보\"", layout);
            StringAssert.Contains("text=\"의회\"", layout);
            StringAssert.Contains("text=\"월간 보고\"", layout);
            StringAssert.Contains("text=\"영지관 위임\"", layout);
            StringAssert.Contains("text=\"진격 / 원정\"", layout);
        }

        // 우측 목표·알림 및 성 명령 패널이 생성 에셋과 읽기 쉬운 버튼 서체를 사용하는지 검증합니다.
        [Test]
        public void RightPanels_UseGeneratedFramesAndSansButtonFont()
        {
            string layout = ReadAdministrationLayout();
            string stylesheet = File.ReadAllText("Assets/UI/Administration/WIAdministration.uss");
            string battleStylesheet = File.ReadAllText("Assets/UI/Battle/WIBattleHUD.uss");

            Assert.IsTrue(File.Exists("Assets/Resources/UI/Generated/right_panel_header.png"));
            Assert.IsTrue(File.Exists("Assets/Resources/UI/Generated/right_panel_frame.png"));
            Assert.IsTrue(File.Exists("Assets/Resources/UI/Generated/right_objective_card.png"));
            Assert.IsTrue(File.Exists("Assets/Resources/UI/Generated/right_notice_row.png"));
            Assert.IsTrue(File.Exists("Assets/Resources/UI/Generated/right_danger_row.png"));
            Assert.IsTrue(File.Exists("Assets/Resources/UI/Generated/right_action_button.png"));
            Assert.IsTrue(File.Exists("Assets/Resources/UI/Generated/button_type_h.png"));
            StringAssert.Contains("side-panel-kicker right-panel-heading", layout);
            StringAssert.Contains("command-title right-panel-heading", layout);
            StringAssert.Contains("right_panel_frame.png", stylesheet);
            StringAssert.Contains("right_objective_card.png", stylesheet);
            StringAssert.Contains("right_danger_row.png", stylesheet);
            StringAssert.Contains(".generated-ornate-action", stylesheet);
            StringAssert.Contains("UI/Generated/button_type_h.png", stylesheet);
            StringAssert.Contains(".app Button, .generated-normal", stylesheet);
            StringAssert.Contains("-unity-text-outline-width:0", stylesheet);
            StringAssert.Contains("NotoSansKR-VariableFont_wght.ttf", battleStylesheet);
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
                "금화", 1234567, 890, 3, "번영 × 규모 × 질서");

            StringAssert.Contains("1,234,567", tooltip);
            StringAssert.Contains("+890", tooltip);
            StringAssert.Contains("소유 성 3개", tooltip);
            StringAssert.Contains("근거: 번영 × 규모 × 질서", tooltip);
        }

        // 첩보 성공률이 기본 확률과 담당 지력에서 대상 질서·방첩을 차감해 계산되는지 검증합니다.
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

        // 다섯 진영이 색상 없이도 중복되지 않는 고유 코드로 구분되는지 검증합니다.
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
