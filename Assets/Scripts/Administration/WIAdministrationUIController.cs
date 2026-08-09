using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public enum WIAdministrationShortcutAction
    {
        None, CloseModal, ReturnToGlobal, Military, Heroes, Diplomacy, Scheme, Research,
        Faction, Council, MonthlyReport, EndTurn
    }

    [RequireComponent(typeof(UIDocument))]
    public partial class WIAdministrationUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationDatabaseSO database;

        private WIAdministrationState state;
        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement map;
        private VisualElement mapBackground;
        private VisualElement factionEmblem;
        private VisualElement castleBackground;
        private VisualElement governorPortrait;
        private WIMapConnectionLayer mapConnectionLayer;
        private VisualElement globalView;
        private VisualElement castleView;
        private VisualElement globalViewHost;
        private VisualElement castleViewHost;
        private VisualElement modalLayer;
        private VisualElement campaignStartLayer;
        private VisualElement difficultyOptions;
        private VisualElement variantOptions;
        private Label factionLabel;
        private Label dateLabel;
        private Label turnDescription;
        private Label goldLabel;
        private Label manaLabel;
        private Label influenceLabel;
        private Label factionLegendLabel;
        private Label castleTitle;
        private Label castleInfo;
        private Label prosperityLabel;
        private Label technologyLabel;
        private Label stabilityLabel;
        private Label defenseLabel;
        private Label projectStatusLabel;
        private Label globalCastleName;
        private Label globalCastleOwner;
        private Label globalCastleStats;
        private Label globalCastleHeroes;
        private VisualElement globalCastleImage;
        private VisualElement globalCastleHeroCards;
        private readonly List<VisualElement> globalHeroCards = new List<VisualElement>();
        private readonly List<VisualElement> globalHeroPortraits = new List<VisualElement>();
        private readonly List<Label> globalHeroNames = new List<Label>();
        private VisualElement objectiveProgressFill;
        private Label rightObjectiveProgress;
        private Label rightMonthlyNews;
        private Button battleAlertButton;
        private VisualElement heroSlots;
        private VisualElement specialFacilitySlots;
        private readonly List<VisualElement> castleHeroSlotElements = new List<VisualElement>();
        private readonly List<VisualElement> castleHeroSlotImages = new List<VisualElement>();
        private readonly List<Label> castleHeroSlotCaptions = new List<Label>();
        private readonly List<VisualElement> castleFacilitySlotElements = new List<VisualElement>();
        private readonly List<VisualElement> castleFacilitySlotImages = new List<VisualElement>();
        private readonly List<Label> castleFacilitySlotCaptions = new List<Label>();
        private Button specialFacilityButton;
        private Button objectiveButton;
        private WICastleRuntimeState selectedCastle;
        private readonly Dictionary<string, Button> castleButtons = new Dictionary<string, Button>();
        private readonly Dictionary<WICampaignDifficulty, Button> difficultyButtons = new Dictionary<WICampaignDifficulty, Button>();
        private readonly Dictionary<WICampaignVariant, Button> variantButtons = new Dictionary<WICampaignVariant, Button>();
        private bool mapNodesBound;
        private WICampaignDifficulty selectedDifficulty = WICampaignDifficulty.Standard;
        private WICampaignVariant selectedVariant = WICampaignVariant.Classic;

        // UI 문서와 버튼 이벤트를 초기화합니다.
        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement;
            if (database == null)
            {
                Debug.LogError("전략 데이터베이스가 할당되지 않았습니다.");
                enabled = false;
                return;
            }

            WICampaignRuntimeService campaignService = WICampaignRuntimeService.Instance;
            state = campaignService == null
                ? WIAdministrationState.Create(database)
                : campaignService.GetOrCreateState(database);
            CacheElements();
            BindGlobalButtons();
            BindKeyboardShortcuts();
            BuildMap();
            RefreshAll();
            SelectInitialCastle();
            BuildCampaignStartScreen();
            campaignStartLayer.style.display = campaignService == null || campaignService.HasCampaignStarted == false
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            if (campaignService != null && campaignService.HasCampaignStarted &&
                WIAdministrationTurnSystem.HasUnresolvedPlayerBattles(state))
            {
                OpenMonthlyReportModal();
            }
        }

        // 자주 사용하는 UI 요소를 이름으로 캐시합니다.
        private void CacheElements()
        {
            map = root.Q<VisualElement>("map");
            mapBackground = root.Q<VisualElement>("map-background-placeholder");
            factionEmblem = root.Q<VisualElement>("faction-emblem");
            castleBackground = root.Q<VisualElement>("castle-background-placeholder");
            governorPortrait = root.Q<VisualElement>("governor-portrait-placeholder");
            globalView = root.Q<VisualElement>("global-view");
            castleView = root.Q<VisualElement>("castle-view");
            globalViewHost = root.Q<VisualElement>("global-view-host");
            castleViewHost = root.Q<VisualElement>("castle-view-host");
            modalLayer = root.Q<VisualElement>("modal-layer");
            campaignStartLayer = root.Q<VisualElement>("campaign-start-layer");
            difficultyOptions = root.Q<VisualElement>("difficulty-options");
            variantOptions = root.Q<VisualElement>("variant-options");
            factionLabel = root.Q<Label>("faction-label");
            dateLabel = root.Q<Label>("date-label");
            turnDescription = root.Q<Label>("turn-description");
            goldLabel = root.Q<Label>("gold-label");
            manaLabel = root.Q<Label>("mana-label");
            influenceLabel = root.Q<Label>("influence-label");
            factionLegendLabel = root.Q<Label>("faction-legend-copy");
            castleTitle = root.Q<Label>("castle-title");
            castleInfo = root.Q<Label>("castle-info");
            prosperityLabel = root.Q<Label>("prosperity-label");
            technologyLabel = root.Q<Label>("technology-label");
            stabilityLabel = root.Q<Label>("stability-label");
            defenseLabel = root.Q<Label>("defense-label");
            projectStatusLabel = root.Q<Label>("project-status-label");
            globalCastleName = root.Q<Label>("global-castle-name");
            globalCastleOwner = root.Q<Label>("global-castle-owner");
            globalCastleStats = root.Q<Label>("global-castle-stats");
            globalCastleHeroes = root.Q<Label>("global-castle-heroes");
            globalCastleImage = root.Q<VisualElement>("global-castle-image");
            globalCastleHeroCards = root.Q<VisualElement>("global-castle-hero-cards");
            objectiveProgressFill = root.Q<VisualElement>("objective-progress-fill");
            rightObjectiveProgress = root.Q<Label>("right-objective-progress");
            rightMonthlyNews = root.Q<Label>("right-monthly-news");
            battleAlertButton = root.Q<Button>("battle-alert-button");
            heroSlots = root.Q<VisualElement>("hero-slots");
            specialFacilitySlots = root.Q<VisualElement>("special-facility-slots");
            CacheFixedSlotElements();
            specialFacilityButton = root.Q<Button>("special-facility-button");
            objectiveButton = root.Q<Button>("objective-button");
            ApplyVisualAssets();
        }

        // UXML에 미리 배치된 영웅 카드와 성 슬롯을 목록으로 캐시합니다.
        private void CacheFixedSlotElements()
        {
            for (int index = 0; index < 4; index += 1)
            {
                globalHeroCards.Add(root.Q<VisualElement>($"global-hero-card-{index}"));
                globalHeroPortraits.Add(root.Q<VisualElement>($"global-hero-portrait-{index}"));
                globalHeroNames.Add(root.Q<Label>($"global-hero-name-{index}"));
            }

            for (int index = 0; index < 8; index += 1)
            {
                castleHeroSlotElements.Add(root.Q<VisualElement>($"hero-slot-{index}"));
                castleHeroSlotImages.Add(root.Q<VisualElement>($"hero-slot-image-{index}"));
                castleHeroSlotCaptions.Add(root.Q<Label>($"hero-slot-caption-{index}"));
            }

            for (int index = 0; index < 2; index += 1)
            {
                castleFacilitySlotElements.Add(root.Q<VisualElement>($"facility-slot-{index}"));
                castleFacilitySlotImages.Add(root.Q<VisualElement>($"facility-slot-image-{index}"));
                castleFacilitySlotCaptions.Add(root.Q<Label>($"facility-slot-caption-{index}"));
            }
        }

        // ScriptableObject에 지정된 실제 지도 이미지를 전역 지도 배경에 적용합니다.
        private void ApplyVisualAssets()
        {
            if (mapBackground == null || database.GlobalMapImage == null)
            {
                return;
            }

            mapBackground.style.backgroundImage = new StyleBackground(database.GlobalMapImage);
            Label placeholder = mapBackground.Q<Label>();
            if (placeholder != null)
            {
                placeholder.style.display = DisplayStyle.None;
            }
        }

        // 전역 메뉴와 성 액션 버튼에 동작을 연결합니다.
        private void BindGlobalButtons()
        {
            root.Q<Button>("turn-button").clicked += BeginTurn;
            root.Q<Button>("back-to-map-button").clicked += ShowGlobalView;
            specialFacilityButton.clicked += OpenSpecialFacilityModal;
            root.Q<Button>("focus-project-button").clicked += OpenFocusProjectModal;
            root.Q<Button>("march-button").clicked += OpenCastleMarchModal;
            root.Q<Button>("assign-button").clicked += OpenHeroAssignmentModal;
            root.Q<Button>("character-activity-button").clicked += OpenCharacterActivityModal;
            root.Q<Button>("basic-facility-button").clicked += OpenBasicFacilityModal;
            root.Q<Button>("heroes-button").clicked += OpenHeroListModal;
            root.Q<Button>("military-button").clicked += OpenMilitaryModal;
            root.Q<Button>("diplomacy-button").clicked += OpenDiplomacyModal;
            root.Q<Button>("scheme-button").clicked += OpenSchemeModal;
            root.Q<Button>("research-button").clicked += OpenResearchModal;
            root.Q<Button>("faction-button").clicked += OpenFactionOverviewModal;
            root.Q<Button>("castle-record-button").clicked += OpenCastleRecordModal;
            root.Q<Button>("delegation-button").clicked += OpenDelegationModal;
            root.Q<Button>("council-button").clicked += OpenFactionPolicyModal;
            root.Q<Button>("monthly-report-button").clicked += OpenMonthlyReportModal;
            root.Q<Button>("information-button").clicked += () => ShowMessage("대륙 지도에서 성을 선택하면 영지 관리 화면으로 이동합니다.");
            root.Q<Button>("system-button").clicked += OpenSystemModal;
            root.Q<Button>("new-campaign-button").clicked += StartNewCampaign;
            root.Q<Button>("continue-campaign-button").clicked += ContinueAutoSave;
            root.Q<Button>("capital-manage-button").clicked += OpenGlobalSummaryCastle;
            battleAlertButton.clicked += OpenMonthlyReportModal;
            objectiveButton.clicked += () => ShowCurrentObjective(false);
        }

        // 좌측 전역 요약 패널이 가리키는 플레이어 성을 영지 관리 화면으로 엽니다.
        private void OpenGlobalSummaryCastle()
        {
            WICastleRuntimeState castle = GetGlobalSummaryCastle();
            if (castle != null) SelectCastle(castle.CastleId);
        }

        // 현재 선택 성 또는 플레이어의 첫 소유 성을 전역 요약 대상으로 반환합니다.
        private WICastleRuntimeState GetGlobalSummaryCastle()
        {
            if (selectedCastle != null && selectedCastle.FactionId == state.PlayerFactionId) return selectedCastle;
            return state.Castles.FirstOrDefault(castle => castle.FactionId == state.PlayerFactionId);
        }

        // 전략 화면에 PC 단축키를 연결하고 버튼에 키 안내를 표시합니다.
        private void BindKeyboardShortcuts()
        {
            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(HandleKeyboardShortcut, TrickleDown.TrickleDown);
            SetShortcutHint("military-button", "군사  M", "M · 군사 화면");
            SetShortcutHint("heroes-button", "영웅  H", "H · 전체 인물 목록");
            SetShortcutHint("diplomacy-button", "외교  D", "D · 외교 화면");
            SetShortcutHint("scheme-button", "첩보  S", "S · 첩보 화면");
            SetShortcutHint("research-button", "연구  R", "R · 연구 화면");
            SetShortcutHint("faction-button", "통치  G", "G · 진영 통치 화면");
            SetShortcutHint("council-button", "의회  C", "C · 진영 방침 화면");
            SetShortcutHint("monthly-report-button", "월간 보고  L", "L · 월간 보고서");
            SetShortcutHint("turn-button", "다음 턴  T", "T · 다음 턴 진행");
            root.Focus();
        }

        // 버튼 문구와 툴팁에 단축키를 함께 기록합니다.
        private void SetShortcutHint(string elementName, string text, string tooltip)
        {
            Button button = root.Q<Button>(elementName);
            if (button == null) return;
            button.text = text;
            button.tooltip = tooltip;
        }

        // 현재 UI 계층의 우선순위에 맞춰 키 입력을 모달·화면·전역 명령으로 전달합니다.
        private void HandleKeyboardShortcut(KeyDownEvent keyboardEvent)
        {
            bool modalOpen = modalLayer.resolvedStyle.display != DisplayStyle.None;
            bool castleOpen = castleView.resolvedStyle.display != DisplayStyle.None;
            bool campaignStartOpen = campaignStartLayer.resolvedStyle.display != DisplayStyle.None;
            bool textInputFocused = root.focusController?.focusedElement is TextField;
            WIAdministrationShortcutAction action = ResolveShortcutAction(keyboardEvent.keyCode, modalOpen,
                castleOpen, campaignStartOpen, textInputFocused);
            if (action == WIAdministrationShortcutAction.None) return;

            switch (action)
            {
                case WIAdministrationShortcutAction.CloseModal: CloseModal(); break;
                case WIAdministrationShortcutAction.ReturnToGlobal: ShowGlobalView(); break;
                case WIAdministrationShortcutAction.Military: OpenMilitaryModal(); break;
                case WIAdministrationShortcutAction.Heroes: OpenHeroListModal(); break;
                case WIAdministrationShortcutAction.Diplomacy: OpenDiplomacyModal(); break;
                case WIAdministrationShortcutAction.Scheme: OpenSchemeModal(); break;
                case WIAdministrationShortcutAction.Research: OpenResearchModal(); break;
                case WIAdministrationShortcutAction.Faction: OpenFactionOverviewModal(); break;
                case WIAdministrationShortcutAction.Council: OpenFactionPolicyModal(); break;
                case WIAdministrationShortcutAction.MonthlyReport: OpenMonthlyReportModal(); break;
                case WIAdministrationShortcutAction.EndTurn: BeginTurn(); break;
            }
            keyboardEvent.StopImmediatePropagation();
        }

        // 키와 화면 상태만으로 실행할 전략 명령을 결정해 입력 우선순위를 일관되게 유지합니다.
        public static WIAdministrationShortcutAction ResolveShortcutAction(KeyCode keyCode, bool modalOpen,
            bool castleOpen, bool campaignStartOpen, bool textInputFocused)
        {
            if (keyCode == KeyCode.Escape)
            {
                if (modalOpen) return WIAdministrationShortcutAction.CloseModal;
                if (castleOpen) return WIAdministrationShortcutAction.ReturnToGlobal;
                return WIAdministrationShortcutAction.None;
            }
            if (campaignStartOpen || modalOpen || castleOpen || textInputFocused) return WIAdministrationShortcutAction.None;
            if (keyCode == KeyCode.M) return WIAdministrationShortcutAction.Military;
            if (keyCode == KeyCode.H) return WIAdministrationShortcutAction.Heroes;
            if (keyCode == KeyCode.D) return WIAdministrationShortcutAction.Diplomacy;
            if (keyCode == KeyCode.S) return WIAdministrationShortcutAction.Scheme;
            if (keyCode == KeyCode.R) return WIAdministrationShortcutAction.Research;
            if (keyCode == KeyCode.G) return WIAdministrationShortcutAction.Faction;
            if (keyCode == KeyCode.C) return WIAdministrationShortcutAction.Council;
            if (keyCode == KeyCode.L) return WIAdministrationShortcutAction.MonthlyReport;
            if (keyCode == KeyCode.T) return WIAdministrationShortcutAction.EndTurn;
            return WIAdministrationShortcutAction.None;
        }

        // 전역 HUD와 지도 뱃지를 최신 상태로 갱신합니다.
        private void RefreshAll()
        {
            WIFactionDefinition playerFaction = null;
            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.PlayerFaction == true)
                {
                    playerFaction = faction;
                    break;
                }
            }

            string factionName = playerFaction == null
                ? database.GetText("UI_GAME_TITLE")
                : playerFaction.DisplayName.Get(database.UseEnglish);
            factionLabel.text = TruncateLabel(factionName, 18);
            factionLabel.tooltip = factionName;
            ApplyBackgroundSprite(factionEmblem, playerFaction == null ? null : playerFaction.Emblem);
            dateLabel.text = $"{state.Year}년 {state.Month:00}월 · 제 {state.Turn}턴";
            WITurnSummary forecast = WIAdministrationTurnSystem.GetFactionMonthlyIncome(database, state, state.PlayerFactionId);
            bool depleted = state.Gold <= 0 || state.ManaCrystal <= 0 || state.Influence <= 0;
            turnDescription.text = depleted
                ? $"자원 고갈 · 다음 턴 기본 수입 G+{forecast.GoldGained} / M+{forecast.ManaGained} / I+{forecast.InfluenceGained}로 회복할 수 있습니다."
                : $"진영 방침: {GetFactionPolicyDisplayName(state.FactionPolicy)} · {state.Month:00}월 명령을 검토하십시오.";
            goldLabel.text = $"금화\n{FormatHudNumber(state.Gold, database.UseEnglish)} (+{FormatHudNumber(forecast.GoldGained, database.UseEnglish)})";
            manaLabel.text = $"마나\n{FormatHudNumber(state.ManaCrystal, database.UseEnglish)} (+{FormatHudNumber(forecast.ManaGained, database.UseEnglish)})";
            influenceLabel.text = $"영향력\n{FormatHudNumber(state.Influence, database.UseEnglish)} (+{FormatHudNumber(forecast.InfluenceGained, database.UseEnglish)})";
            int ownedCastleCount = state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId);
            goldLabel.tooltip = BuildResourceCalculationTooltip("금화", state.Gold, forecast.GoldGained, ownedCastleCount,
                "성별 번영 × 성 규모 × 질서 효율 + 전문 분야·시설·연구");
            manaLabel.tooltip = BuildResourceCalculationTooltip("마나", state.ManaCrystal, forecast.ManaGained, ownedCastleCount,
                "성별 기술 × 성 규모 + 전문 분야·마법 시설·연구");
            influenceLabel.tooltip = BuildResourceCalculationTooltip("영향력", state.Influence, forecast.InfluenceGained, ownedCastleCount,
                "성별 질서 × 성 규모 + 전문 분야·연구");
            Button turnButton = root.Q<Button>("turn-button");
            int unresolvedBattleCount = state.BattleSessions.Count(session =>
                session.PlayerInvolved && session.Status != WIBattleSessionStatus.Resolved);
            turnButton.SetEnabled(unresolvedBattleCount == 0);
            turnButton.text = unresolvedBattleCount == 0 ? "다음 턴  T" : $"전투 해결 필요 · {unresolvedBattleCount}건";
            turnButton.tooltip = unresolvedBattleCount == 0
                ? "T · 다음 턴 진행"
                : "플레이어가 참가하는 모든 전투를 끝내야 다음 턴으로 진행할 수 있습니다.";
            WICampaignObjectiveDefinition objective = WICampaignObjectiveSystem.GetCurrent(database, state);
            if (objective == null)
            {
                objectiveButton.text = state.CampaignResult == WICampaignResult.Victory
                    ? "캠페인 목표 완료 · 대륙 통일"
                    : "모든 등록 목표 완료";
                objectiveButton.SetEnabled(false);
            }
            else
            {
                int progress = WICampaignObjectiveSystem.GetProgress(state, objective);
                objectiveButton.text = $"목표 · {objective.Title.Get(database.UseEnglish)}   {progress}/{objective.TargetValue}";
                objectiveButton.SetEnabled(true);
            }
            RefreshGlobalSidePanels(objective, unresolvedBattleCount);
            foreach (KeyValuePair<string, Button> pair in castleButtons)
            {
                WICastleRuntimeState castle = state.GetCastle(pair.Key);
                WICastleDefinition definition = database.GetCastle(pair.Key);
                WIFactionDefinition owner = database.GetFaction(castle.FactionId);
                ApplyMapNodeFactionClass(pair.Value, castle.FactionId);
                bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, castle);
                bool military = WIInformationVisibility.CanViewMilitaryDetails(state, state.PlayerFactionId, castle);
                string eventBadge = detailed && castle.InvasionWarning ? " !" : string.Empty;
                string occupationBadge = detailed && castle.OccupationUnrestMonths > 0 ? " ⚑" : string.Empty;
                int armyCount = state.Armies.FindAll(army => army.CurrentCastleId == pair.Key).Count;
                string battleBadge = military && state.Armies.Exists(army => army.CurrentCastleId == pair.Key && army.AwaitingBattle) ? " ⚔" : string.Empty;
                string detailText = detailed ? $"{castle.HeroIds.Count}H · {armyCount}전투단" : military ? "전투 접촉 · 전력 확인 가능" : "정보 미확보";
                string castleName = definition.DisplayName.Get(database.UseEnglish);
                string factionCode = GetFactionAccessibilityCode(castle.FactionId);
                Label nameplate = pair.Value.Q<Label>(className: "castle-node-name");
                if (nameplate != null)
                {
                    nameplate.text = $"{TruncateLabel(castleName, 9)}{eventBadge}{battleBadge}{occupationBadge}";
                }
                pair.Value.tooltip = $"[{factionCode}] {owner?.DisplayName.Get(database.UseEnglish) ?? castle.FactionId}\n{castleName}\n{detailText}";
            }
            RefreshFactionLegend();
            RefreshMapConnections();

            if (selectedCastle != null && castleView.resolvedStyle.display == DisplayStyle.Flex)
            {
                string selectedCastleId = selectedCastle.CastleId;
                SelectCastle(selectedCastleId);
            }
        }

        // 시안형 전역 화면의 좌측 성 현황과 우측 목표·전투 알림을 현재 상태로 갱신합니다.
        private void RefreshGlobalSidePanels(WICampaignObjectiveDefinition objective, int unresolvedBattleCount)
        {
            WICastleRuntimeState castle = GetGlobalSummaryCastle();
            if (castle != null)
            {
                WICastleDefinition definition = database.GetCastle(castle.CastleId);
                WIFactionDefinition owner = database.GetFaction(castle.FactionId);
                globalCastleName.text = definition?.DisplayName.Get(database.UseEnglish) ?? castle.CastleId;
                globalCastleOwner.text = $"{castle.CastleSize} · {owner?.DisplayName.Get(database.UseEnglish) ?? castle.FactionId}";
                globalCastleStats.text = $"번영 {castle.Prosperity}  ·  기술 {castle.Technology}\n질서 {castle.Stability}  ·  방어 {castle.Defense}";
                int armyCount = state.Armies.Count(army => army.CurrentCastleId == castle.CastleId);
                globalCastleHeroes.text = $"주둔 영웅 {castle.HeroIds.Count}명  ·  주둔 전투단 {armyCount}개";
                ApplyBackgroundSprite(globalCastleImage, definition?.CastleImage);
                RefreshGlobalHeroCards(castle);
            }

            if (objective == null)
            {
                rightObjectiveProgress.text = state.CampaignResult == WICampaignResult.Victory
                    ? "대륙 통일 목표를 달성했습니다."
                    : "현재 진행 중인 목표가 없습니다.";
            }
            else
            {
                int progress = WICampaignObjectiveSystem.GetProgress(state, objective);
                rightObjectiveProgress.text = $"진행 {progress}/{objective.TargetValue}\n{objective.Description.Get(database.UseEnglish)}";
            }
            int objectiveProgress = objective == null || objective.TargetValue <= 0
                ? state.CampaignResult == WICampaignResult.Victory ? 100 : 0
                : Mathf.Clamp(Mathf.RoundToInt(WICampaignObjectiveSystem.GetProgress(state, objective) * 100f / objective.TargetValue), 0, 100);
            if (objectiveProgressFill != null) objectiveProgressFill.style.width = Length.Percent(objectiveProgress);

            battleAlertButton.text = unresolvedBattleCount > 0
                ? $"전투 발생 {unresolvedBattleCount}건 · 확인"
                : "현재 전투 없음";
            battleAlertButton.SetEnabled(unresolvedBattleCount > 0 || state.LastMonthlyReport != null);
            battleAlertButton.EnableInClassList("danger", unresolvedBattleCount > 0);
            string latestNews = state.LastMonthlyReport?.News?.LastOrDefault();
            rightMonthlyNews.text = string.IsNullOrEmpty(latestNews)
                ? "새로운 월간 보고가 없습니다."
                : $"최근 소식\n{latestNews}";
        }

        // 좌측 성 요약 패널에 최대 네 명의 주둔 영웅 초상 카드와 이름을 표시합니다.
        private void RefreshGlobalHeroCards(WICastleRuntimeState castle)
        {
            for (int index = 0; index < globalHeroCards.Count; index += 1)
            {
                bool occupied = index < castle.HeroIds.Count;
                globalHeroCards[index].style.display = occupied ? DisplayStyle.Flex : DisplayStyle.None;
                if (occupied == false)
                {
                    ClearBackgroundSprite(globalHeroPortraits[index]);
                    globalHeroNames[index].text = string.Empty;
                    continue;
                }

                WIHeroDefinition hero = database.GetHero(castle.HeroIds[index]);
                ApplyBackgroundSprite(globalHeroPortraits[index], hero == null ? null : hero.Portrait);
                globalHeroNames[index].text = hero == null
                    ? castle.HeroIds[index]
                    : TruncateLabel(hero.DisplayName.Get(database.UseEnglish), 6);
            }
        }

        // UID에 해당하는 단순 안내 모달을 표시합니다.
        private void ShowMessage(string uid)
        {
            string message = database.GetText(uid);
            if (string.IsNullOrEmpty(message) || message == uid)
            {
                message = uid;
            }

            VisualElement panel = CreateModal(message);
            panel.Add(new Label(message));
        }

        // 담당 가능한 인물이 없을 때 턴 진행과 인물 복귀 방법을 공통으로 안내합니다.
        private void AddNoIdleCharacterGuidance(VisualElement panel)
        {
            panel.Add(new Label("현재 명령을 맡길 대기 인물이 없습니다."));
            panel.Add(new Label("진행 중인 사업·연구·활동·의뢰·이동은 다음 턴에 처리됩니다. 전투단 인물은 전투단을 해산하거나 구성원에서 제외해야 다시 명령할 수 있습니다."));
        }

        // 공통 선택 모달을 데이터 목록으로 구성합니다.
        private void ShowChoiceModal<T>(string title, IReadOnlyList<T> values, System.Action<T> onSelected)
        {
            VisualElement panel = CreateModal(title);
            foreach (T value in values)
            {
                Button button = new Button(() => onSelected(value));
                if (value is WISpecialFacilityDefinition facility)
                {
                    button.text = $"{facility.DisplayName.Get(database.UseEnglish)} · {facility.Description.Get(database.UseEnglish)}";
                }
                else if (value is WIHeroDefinition hero)
                {
                    button.text = hero.DisplayName.Get(database.UseEnglish);
                }
                else if (value is WICastleDefinition castle)
                {
                    button.text = castle.DisplayName.Get(database.UseEnglish);
                }

                panel.Add(button);
            }
        }

        // 공통 모달 배경과 패널을 생성합니다.
        private VisualElement CreateModal(string title)
        {
            return CreateModalLayout(title, false, out _);
        }

        // 월간 보고처럼 스크롤 본문 아래에 고정 행동 영역이 필요한 모달을 생성합니다.
        private VisualElement CreateModalWithFooter(string title, out VisualElement footer)
        {
            return CreateModalLayout(title, true, out footer);
        }

        // 공통 모달의 고정 헤더, 스크롤 본문과 선택적 하단 행동 영역을 구성합니다.
        private VisualElement CreateModalLayout(string title, bool includeFooter, out VisualElement footer)
        {
            CloseModal();
            modalLayer.style.display = DisplayStyle.Flex;
            VisualElement panel = new VisualElement();
            panel.AddToClassList("modal-panel");
            panel.AddToClassList("generated-panel-background");
            VisualElement header = new VisualElement();
            header.AddToClassList("modal-header");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("modal-title");
            header.Add(titleLabel);
            Button close = new Button(CloseModal);
            close.text = database.GetText("UI_CLOSE");
            close.AddToClassList("modal-close");
            header.Add(close);
            panel.Add(header);
            ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.name = "modal-scroll-view";
            scrollView.AddToClassList("modal-scroll-view");
            scrollView.contentContainer.AddToClassList("modal-scroll-content");
            panel.Add(scrollView);
            ScrollView footerScrollView = new ScrollView(ScrollViewMode.Vertical);
            footer = footerScrollView.contentContainer;
            if (includeFooter)
            {
                footerScrollView.AddToClassList("modal-fixed-footer");
                footer.AddToClassList("modal-fixed-footer-content");
                panel.Add(footerScrollView);
            }
            modalLayer.Add(panel);
            return scrollView.contentContainer;
        }

        // 현재 열린 모달을 닫습니다.
        private void CloseModal()
        {
            modalLayer.Clear();
            modalLayer.style.display = DisplayStyle.None;
        }
    }
}
