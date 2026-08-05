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
    public class WIAdministrationUIController : MonoBehaviour
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
        private Label rightObjectiveProgress;
        private Label rightMonthlyNews;
        private Button battleAlertButton;
        private VisualElement heroSlots;
        private VisualElement specialFacilitySlots;
        private Button specialFacilityButton;
        private Button objectiveButton;
        private WICastleRuntimeState selectedCastle;
        private readonly Dictionary<string, Button> castleButtons = new Dictionary<string, Button>();
        private readonly Dictionary<WICampaignDifficulty, Button> difficultyButtons = new Dictionary<WICampaignDifficulty, Button>();
        private readonly Dictionary<WICampaignVariant, Button> variantButtons = new Dictionary<WICampaignVariant, Button>();
        private WICampaignDifficulty selectedDifficulty = WICampaignDifficulty.Standard;
        private WICampaignVariant selectedVariant = WICampaignVariant.Classic;

        // UI 문서와 버튼 이벤트를 초기화합니다.
        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement;
            if (database == null)
            {
                Debug.LogError("내정 데이터베이스가 할당되지 않았습니다.");
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
            rightObjectiveProgress = root.Q<Label>("right-objective-progress");
            rightMonthlyNews = root.Q<Label>("right-monthly-news");
            battleAlertButton = root.Q<Button>("battle-alert-button");
            heroSlots = root.Q<VisualElement>("hero-slots");
            specialFacilitySlots = root.Q<VisualElement>("special-facility-slots");
            specialFacilityButton = root.Q<Button>("special-facility-button");
            objectiveButton = root.Q<Button>("objective-button");
            ApplyVisualAssets();
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
            root.Q<Button>("information-button").clicked += () => ShowMessage("대륙 지도에서 성을 선택하면 성 내정 화면으로 이동합니다.");
            root.Q<Button>("system-button").clicked += OpenSystemModal;
            root.Q<Button>("new-campaign-button").clicked += StartNewCampaign;
            root.Q<Button>("continue-campaign-button").clicked += ContinueAutoSave;
            root.Q<Button>("capital-manage-button").clicked += OpenGlobalSummaryCastle;
            battleAlertButton.clicked += OpenMonthlyReportModal;
            objectiveButton.clicked += () => ShowCurrentObjective(false);
        }

        // 좌측 전역 요약 패널이 가리키는 플레이어 성을 내정 화면으로 엽니다.
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
            SetShortcutHint("heroes-button", "인사  H", "H · 전체 인물 목록");
            SetShortcutHint("diplomacy-button", "외교  D", "D · 외교 화면");
            SetShortcutHint("scheme-button", "계략  S", "S · 계략 화면");
            SetShortcutHint("research-button", "연구  R", "R · 연구 화면");
            SetShortcutHint("faction-button", "통치  G", "G · 세력 통치 화면");
            SetShortcutHint("council-button", "평정  C", "C · 세력 방침 화면");
            SetShortcutHint("monthly-report-button", "월보  L", "L · 월간 보고서");
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

        // ScriptableObject 난이도 정의를 PC용 선택 카드로 구성합니다.
        private void BuildCampaignStartScreen()
        {
            difficultyOptions.Clear();
            difficultyButtons.Clear();
            foreach (WICampaignDifficultyDefinition definition in database.DifficultyDefinitions)
            {
                Button button = new Button(() => SelectDifficulty(definition.Difficulty));
                button.text = $"{definition.DisplayName.Get(database.UseEnglish)}\n\n{definition.Description.Get(database.UseEnglish)}";
                button.AddToClassList("difficulty-card");
                difficultyOptions.Add(button);
                difficultyButtons[definition.Difficulty] = button;
            }
            SelectDifficulty(WICampaignDifficulty.Standard);
            BuildCampaignVariantOptions();

            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            Button continueButton = root.Q<Button>("continue-campaign-button");
            continueButton.SetEnabled(service != null && service.HasSave(0));
        }

        // ScriptableObject의 반복 플레이 시작 조건을 선택 카드로 구성합니다.
        private void BuildCampaignVariantOptions()
        {
            variantOptions.Clear();
            variantButtons.Clear();
            foreach (WICampaignVariantDefinition definition in database.CampaignVariants)
            {
                Button button = new Button(() => SelectCampaignVariant(definition.Variant));
                button.text = $"{definition.DisplayName.Get(database.UseEnglish)}\n{definition.Description.Get(database.UseEnglish)}";
                button.AddToClassList("variant-card");
                variantOptions.Add(button);
                variantButtons[definition.Variant] = button;
            }
            SelectCampaignVariant(WICampaignVariant.Classic);
        }

        // 선택한 시작 변형 카드의 강조 상태를 갱신합니다.
        private void SelectCampaignVariant(WICampaignVariant variant)
        {
            selectedVariant = variant;
            foreach (KeyValuePair<WICampaignVariant, Button> pair in variantButtons)
                pair.Value.EnableInClassList("selected", pair.Key == variant);
        }

        // 선택한 난이도 카드의 강조 상태를 갱신합니다.
        private void SelectDifficulty(WICampaignDifficulty difficulty)
        {
            selectedDifficulty = difficulty;
            foreach (KeyValuePair<WICampaignDifficulty, Button> pair in difficultyButtons)
            {
                pair.Value.EnableInClassList("selected", pair.Key == difficulty);
            }
        }

        // 선택 난이도로 런타임 상태를 교체하고 새 캠페인을 시작합니다.
        private void StartNewCampaign()
        {
            BeginCampaign(selectedDifficulty, selectedVariant);
        }

        // 지정 난이도로 새 캠페인을 시작해 시작 화면을 닫습니다.
        public void BeginCampaign(WICampaignDifficulty difficulty,
            WICampaignVariant variant = WICampaignVariant.Classic)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            state = service == null
                ? WIAdministrationState.Create(database, difficulty, variant)
                : service.StartNewCampaign(difficulty, variant);
            RebuildCampaignView();
            campaignStartLayer.style.display = DisplayStyle.None;
            if (ShowCampaignResult() == false)
            {
                ShowCurrentObjective(true);
            }
        }

        // 현재 캠페인 목표의 시작 정세, 조건, 진행도와 보상을 표시합니다.
        private void ShowCurrentObjective(bool showTutorialAfterClose)
        {
            WICampaignObjectiveDefinition objective = WICampaignObjectiveSystem.GetCurrent(database, state);
            if (objective == null)
            {
                ShowMessage("현재 등록된 다음 캠페인 목표가 없습니다.");
                return;
            }

            VisualElement panel = CreateModal(objective.Title.Get(database.UseEnglish));
            Label situation = new Label(objective.Situation.Get(database.UseEnglish));
            situation.AddToClassList("objective-modal-copy");
            panel.Add(situation);
            Label description = new Label(objective.Description.Get(database.UseEnglish));
            description.AddToClassList("objective-modal-copy");
            panel.Add(description);
            int progress = WICampaignObjectiveSystem.GetProgress(state, objective);
            Label progressLabel = new Label($"진행 {progress}/{objective.TargetValue} · 보상 G {objective.RewardGold} / M {objective.RewardMana} / I {objective.RewardInfluence}");
            progressLabel.AddToClassList("objective-modal-progress");
            panel.Add(progressLabel);
            Button confirm = new Button(() =>
            {
                CloseModal();
                if (showTutorialAfterClose) ShowCurrentTutorial();
            });
            confirm.text = "목표 확인";
            panel.Add(confirm);
        }

        // 자동 저장 슬롯을 불러와 캠페인 화면으로 진입합니다.
        private void ContinueAutoSave()
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            string error = string.Empty;
            if (service == null || service.LoadSlot(0, out error) == false)
            {
                ShowMessage(string.IsNullOrEmpty(error) ? "자동 저장을 불러올 수 없습니다." : error);
                return;
            }
            state = service.State;
            RebuildCampaignView();
            campaignStartLayer.style.display = DisplayStyle.None;
            if (string.IsNullOrEmpty(error) == false)
            {
                ShowMessage(error);
                return;
            }
            if (ShowCampaignResult() == false)
            {
                ShowCurrentTutorial();
            }
        }

        // 교체된 캠페인 상태를 지도·HUD·초기 선택 성에 다시 연결합니다.
        private void RebuildCampaignView()
        {
            selectedCastle = null;
            BuildMap();
            RefreshAll();
            SelectInitialCastle();
        }

        // 데이터에 정의된 모든 성을 지도 노드로 생성합니다.
        private void BuildMap()
        {
            map.Clear();
            castleButtons.Clear();
            mapConnectionLayer = new WIMapConnectionLayer();
            mapConnectionLayer.name = "map-connection-layer";
            map.Add(mapConnectionLayer);
            foreach (WICastleDefinition castle in database.Castles)
            {
                WICastleRuntimeState castleState = state.GetCastle(castle.Id);
                WIFactionDefinition faction = database.GetFaction(castleState.FactionId);
                Button node = new Button(() => SelectCastle(castle.Id));
                node.name = $"castle-{castle.Id}";
                node.AddToClassList("castle-node");
                string castleName = castle.DisplayName.Get(database.UseEnglish);
                string factionCode = GetFactionAccessibilityCode(castleState.FactionId);
                node.text = $"{factionCode}·{TruncateLabel(castleName, 9)}";
                node.tooltip = $"[{factionCode}] {faction?.DisplayName.Get(database.UseEnglish) ?? castleState.FactionId}\n{castleName}";
                node.style.left = Length.Percent(castle.NormalizedMapPosition.x * 100f);
                node.style.top = Length.Percent(castle.NormalizedMapPosition.y * 100f);
                if (faction != null)
                {
                    node.style.backgroundColor = faction.Color;
                }

                map.Add(node);
                castleButtons.Add(castle.Id, node);
            }
            RefreshMapConnections();
        }

        // 인접 성 관계를 중복 없이 경로로 만들고 세력 경계를 전선 색상으로 표시합니다.
        private void RefreshMapConnections()
        {
            if (mapConnectionLayer == null)
            {
                return;
            }

            List<WIMapConnectionLayer.Connection> connections = new List<WIMapConnectionLayer.Connection>();
            HashSet<string> visited = new HashSet<string>();
            foreach (WICastleDefinition castle in database.Castles)
            {
                WICastleRuntimeState castleState = state.GetCastle(castle.Id);
                foreach (string adjacentId in castle.AdjacentCastleIds)
                {
                    string edgeId = string.CompareOrdinal(castle.Id, adjacentId) < 0
                        ? $"{castle.Id}|{adjacentId}"
                        : $"{adjacentId}|{castle.Id}";
                    if (visited.Add(edgeId) == false)
                    {
                        continue;
                    }

                    WICastleDefinition adjacent = database.GetCastle(adjacentId);
                    WICastleRuntimeState adjacentState = state.GetCastle(adjacentId);
                    if (adjacent == null || castleState == null || adjacentState == null)
                    {
                        continue;
                    }

                    bool frontline = castleState.FactionId != adjacentState.FactionId;
                    bool selectedRoute = selectedCastle != null &&
                        (selectedCastle.CastleId == castle.Id || selectedCastle.CastleId == adjacentId);
                    WIFactionDefinition owner = database.GetFaction(castleState.FactionId);
                    Color ownerColor = owner == null ? Color.gray : owner.Color;
                    Color routeColor = frontline
                        ? new Color(0.86f, 0.22f, 0.12f, 0.95f)
                        : new Color(ownerColor.r, ownerColor.g, ownerColor.b, 0.72f);
                    if (selectedRoute)
                    {
                        routeColor = new Color(1f, 0.82f, 0.25f, 1f);
                    }

                    connections.Add(new WIMapConnectionLayer.Connection(
                        castle.NormalizedMapPosition,
                        adjacent.NormalizedMapPosition,
                        routeColor,
                        selectedRoute ? 4f : (frontline ? 3f : 1.5f),
                        frontline,
                        selectedRoute));
                }
            }
            mapConnectionLayer.SetConnections(connections);
        }

        // 플레이어가 소유한 첫 번째 성을 초기 선택값으로만 저장하고 대륙 화면을 유지합니다.
        private void SelectInitialCastle()
        {
            foreach (WICastleRuntimeState castleState in state.Castles)
            {
                WIFactionDefinition faction = database.GetFaction(castleState.FactionId);
                if (faction != null && faction.PlayerFaction == true)
                {
                    selectedCastle = castleState;
                    ShowGlobalView();
                    return;
                }
            }
        }

        // 선택한 성의 상세 정보를 갱신하고 성 내정 화면으로 전환합니다.
        private void SelectCastle(string castleId)
        {
            selectedCastle = state.GetCastle(castleId);
            WICastleDefinition castle = database.GetCastle(castleId);
            bool ownCastle = WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle);
            bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, selectedCastle);
            WIFactionDefinition owner = database.GetFaction(selectedCastle.FactionId);
            string ownerName = owner == null ? selectedCastle.FactionId : owner.DisplayName.Get(database.UseEnglish);
            string accessText = ownCastle ? "직접 관리 영지" : $"관찰 전용 · {ownerName} 소유";
            ApplyBackgroundSprite(castleBackground, castle.CastleImage);
            WIHeroDefinition governor = detailed ? database.GetHero(selectedCastle.GovernorHeroId) : null;
            if (detailed == false)
            {
                governorPortrait.style.backgroundImage = StyleKeyword.None;
                Label governorPlaceholder = governorPortrait.Q<Label>();
                if (governorPlaceholder != null) governorPlaceholder.style.display = DisplayStyle.Flex;
            }
            ApplyBackgroundSprite(governorPortrait, governor == null ? null : governor.Portrait);
            castleTitle.text = castle.DisplayName.Get(database.UseEnglish);
            string terrainName = castle.TerrainTrait == null ? string.Empty : castle.TerrainTrait.Get(database.UseEnglish);
            string ownerCode = GetFactionAccessibilityCode(selectedCastle.FactionId);
            castleInfo.text = detailed
                ? $"[{ownerCode}] {accessText} · {selectedCastle.CastleSize} · {terrainName} · 인물 {selectedCastle.HeroIds.Count}/{selectedCastle.GetHeroSlotCount()}"
                : $"[{ownerCode}] {accessText} · {terrainName} · 상세 정보 미확보";
            prosperityLabel.text = detailed ? $"번영 {selectedCastle.Prosperity} · {GetCastleStatusName(selectedCastle.Prosperity)}" : "번영 ??";
            technologyLabel.text = detailed ? $"기술 {selectedCastle.Technology} · {GetCastleStatusName(selectedCastle.Technology)}" : "기술 ??";
            stabilityLabel.text = detailed ? $"치안 {selectedCastle.Stability} · {GetCastleStatusName(selectedCastle.Stability)}" : "치안 ??";
            defenseLabel.text = detailed ? $"방어 {selectedCastle.Defense} · {GetCastleStatusName(selectedCastle.Defense)}" : "방어 ??";
            prosperityLabel.tooltip = detailed ? "번영 · 금화 수입의 기본값입니다. 성 규모와 치안 효율을 곱해 계산합니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            technologyLabel.tooltip = detailed ? "기술 · 마나 수입과 연구 조건에 사용합니다. 성 규모가 높을수록 월간 마나가 증가합니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            stabilityLabel.tooltip = detailed ? "치안 · 금화 수입 효율과 영향력 수입을 높이고 적 계략 성공률을 낮춥니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            defenseLabel.tooltip = detailed ? "방어 · 공성전 자동 판정과 수비 전력에 반영됩니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            projectStatusLabel.text = detailed
                ? selectedCastle.DelegatedToGovernor && selectedCastle.ActiveProject == null
                    ? $"태수 위임 · {GetGovernorPolicyDisplayName(selectedCastle.GovernorPolicy)} · 월 {selectedCastle.GovernorMonthlyBudget}G"
                    : GetProjectStatusText(selectedCastle.ActiveProject)
                : "계략의 조사를 성공하면 일정 기간 상세 정보가 공개됩니다.";
            if (detailed) RefreshCastleSlots(castle, selectedCastle);
            else RefreshHiddenCastleSlots();
            foreach (Button command in root.Query<Button>(className: "castle-command").ToList())
            {
                command.SetEnabled(ownCastle || command.name == "castle-record-button");
            }
            specialFacilityButton.SetEnabled(ownCastle && selectedCastle.PendingSpecialFacilityChoice);
            ShowCastleView();
        }

        // 미조사 적 성의 주둔 인물과 특화 시설 슬롯을 비공개 안내로 대체합니다.
        private void RefreshHiddenCastleSlots()
        {
            heroSlots.Clear();
            heroSlots.Add(new Label("주둔 인물 정보 미확보"));
            specialFacilitySlots.Clear();
            specialFacilitySlots.Add(new Label("시설 정보 미확보"));
        }

        // 대륙 전략 화면을 표시하고 성 내정 화면을 숨깁니다.
        private void ShowGlobalView()
        {
            RefreshMapConnections();
            globalView.style.display = DisplayStyle.Flex;
            castleView.style.display = DisplayStyle.None;
        }

        // 선택한 성의 내정 화면을 표시하고 대륙 전략 화면을 숨깁니다.
        private void ShowCastleView()
        {
            globalView.style.display = DisplayStyle.None;
            castleView.style.display = DisplayStyle.Flex;
        }

        // 외부 UI나 검증 도구에서 지정한 성의 내정 화면으로 이동합니다.
        public void OpenCastle(string castleId)
        {
            if (state == null || database.GetCastle(castleId) == null)
            {
                return;
            }

            SelectCastle(castleId);
            Debug.Log($"성 화면 전환: {castleId} · 전역 {globalView.style.display.value} · 성 {castleView.style.display.value}");
        }

        // 해상도 QA에서 모달 없이 전역 지도 화면을 즉시 표시합니다.
        public void OpenGlobalPreviewForQA()
        {
            BeginCampaign(WICampaignDifficulty.Standard, WICampaignVariant.Classic);
            CloseModal();
            ShowGlobalView();
            RefreshAll();
        }

        // 해상도 QA에서 아발론 소유 성 화면을 모달 없이 즉시 표시합니다.
        public void OpenCastlePreviewForQA()
        {
            BeginCampaign(WICampaignDifficulty.Standard, WICampaignVariant.Classic);
            CloseModal();
            SelectCastle("castle_00");
            RefreshAll();
        }

        // 긴 이름과 큰 숫자를 실제 저장 데이터 변경 없이 전역 화면에 주입해 레이아웃을 검증합니다.
        public void OpenLongContentGlobalPreviewForQA()
        {
            OpenGlobalPreviewForQA();
            ApplyLongContentStressLabels(false);
        }

        // 긴 이름과 큰 숫자를 실제 저장 데이터 변경 없이 성 화면에 주입해 레이아웃을 검증합니다.
        public void OpenLongContentCastlePreviewForQA()
        {
            OpenCastlePreviewForQA();
            ApplyLongContentStressLabels(true);
        }

        // 공통 버튼의 기본·호버·선택·비활성·위험 상태를 한 화면에서 비교합니다.
        public void OpenInteractionStatePreviewForQA()
        {
            OpenGlobalPreviewForQA();
            VisualElement panel = CreateModal("마우스 상호작용 상태 QA");
            AddInteractionStateSample(panel, "기본", "기본 명령", string.Empty, true);
            AddInteractionStateSample(panel, "호버", "마우스 호버", "qa-hover-state", true);
            AddInteractionStateSample(panel, "선택", "현재 선택됨", "qa-selected-state", true);
            AddInteractionStateSample(panel, "비활성", "조건 미충족", "qa-disabled-state", false);
            AddInteractionStateSample(panel, "위험", "진격 / 후퇴", "qa-danger-state", true);
            Label note = new Label("백색 반전은 탐색·선택, 낮은 회색은 사용 불가, 적갈색은 결과가 위험한 명령에만 사용합니다.");
            note.AddToClassList("qa-state-note");
            panel.Add(note);
        }

        // 대표 자원·성 수치·사업·계략의 툴팁 문장을 한 화면에서 비교합니다.
        public void OpenCalculationTooltipPreviewForQA()
        {
            OpenCastlePreviewForQA();
            VisualElement panel = CreateModal("계산 근거 툴팁 QA");
            panel.Add(CreateCalculationSample("금화 수입", BuildResourceCalculationTooltip("금화", 1200, 67, 3,
                "성별 번영 × 성 규모 × 치안 효율 + 전문 분야·시설·연구")));
            panel.Add(CreateCalculationSample("성 수치", "치안 50 · 금화 효율 75% · 영향력과 계략 방어에 반영"));
            panel.Add(CreateCalculationSample("사업 성과", "예상 8 = 기본 3 + 담당 적성 50/20 + 집중 투자 2 + 특기 1 + 전문 분야 0 + 방침 0"));
            panel.Add(CreateCalculationSample("계략 확률", "성공 55% = 기본 60 + 지력/2 - 치안/2 - 방첩 20\n미조사 대상은 정확한 치안과 최종 확률을 공개하지 않습니다."));
            panel.Add(CreateCalculationSample("외교 명령", "확정 명령 · 비용을 충족하면 무작위 판정 없이 관계가 한 단계 변합니다."));
        }

        // 모든 세력색과 경로색을 회색으로 낮춰 코드·기호만으로 지도를 읽을 수 있는지 검증합니다.
        public void OpenColorVisionPreviewForQA()
        {
            OpenGlobalPreviewForQA();
            foreach (KeyValuePair<string, Button> pair in castleButtons)
            {
                string factionId = state.GetCastle(pair.Key)?.FactionId;
                float shade = GetFactionAccessibilityCode(factionId) == "AV" ? 0.72f :
                              GetFactionAccessibilityCode(factionId) == "VD" ? 0.32f :
                              GetFactionAccessibilityCode(factionId) == "IH" ? 0.46f :
                              GetFactionAccessibilityCode(factionId) == "SY" ? 0.58f : 0.22f;
                pair.Value.style.backgroundColor = new Color(shade, shade, shade, 1f);
            }
            mapConnectionLayer?.SetColorVisionQaMode(true);
            turnDescription.text = "저채도 QA · 세력 코드, 전선 ×, 선택 연결 ◎로 판독하십시오.";
        }

        // 툴팁 QA용 제목과 계산 문장을 흑백 정보 카드로 구성합니다.
        private static VisualElement CreateCalculationSample(string title, string calculation)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("calculation-sample");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("calculation-title");
            Label detailLabel = new Label(calculation);
            detailLabel.AddToClassList("calculation-detail");
            row.Add(titleLabel);
            row.Add(detailLabel);
            return row;
        }

        // 상태 견본 한 행을 생성해 동일한 크기와 텍스트 조건에서 색상 차이를 비교합니다.
        private static void AddInteractionStateSample(VisualElement panel, string labelText, string buttonText,
            string stateClass, bool enabledState)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("qa-state-row");
            Label label = new Label(labelText);
            label.AddToClassList("qa-state-label");
            Button button = new Button { text = buttonText };
            button.AddToClassList("qa-state-button");
            if (string.IsNullOrEmpty(stateClass) == false) button.AddToClassList(stateClass);
            button.SetEnabled(enabledState);
            row.Add(label);
            row.Add(button);
            panel.Add(row);
        }

        // QA 화면에 최악 조건의 한글·영문 문구와 7자리 자원 값을 표시합니다.
        private void ApplyLongContentStressLabels(bool castleScreen)
        {
            factionLabel.text = "아발론 북부 변경 재건 연합왕국";
            factionLabel.tooltip = "Kingdom of the United Northern Avalon Reconstruction Frontier";
            goldLabel.text = $"금화\n{FormatHudNumber(9876543, database.UseEnglish)} (+{FormatHudNumber(654321, database.UseEnglish)})";
            manaLabel.text = $"마나\n{FormatHudNumber(7654321, database.UseEnglish)} (+{FormatHudNumber(543210, database.UseEnglish)})";
            influenceLabel.text = $"영향력\n{FormatHudNumber(5432109, database.UseEnglish)} (+{FormatHudNumber(321098, database.UseEnglish)})";
            goldLabel.tooltip = "금화 9,876,543 · 다음 턴 +654,321";
            manaLabel.tooltip = "마나 7,654,321 · 다음 턴 +543,210";
            influenceLabel.tooltip = "영향력 5,432,109 · 다음 턴 +321,098";

            int index = 0;
            foreach (Button node in castleButtons.Values)
            {
                string stressName = index++ % 2 == 0
                    ? "북부 변경의 영원한 별빛 수호 대성채"
                    : "Citadel of the Everlasting Northern Starlight Frontier";
                node.text = $"{TruncateLabel(stressName, 12)}\n99H · 99부대";
                node.tooltip = stressName;
                if (index >= 4) break;
            }

            if (castleScreen)
            {
                const string longCastleName = "북부 변경의 영원한 별빛을 수호하는 아발론 왕립 대성채";
                castleTitle.text = TruncateLabel(longCastleName, 26);
                castleTitle.tooltip = longCastleName;
                castleInfo.text = "직접 관리 영지 · Metropolitan Stronghold · 서부 빛바랜 호반 변경지대 · 인물 99/99";
                castleInfo.tooltip = castleInfo.text;
                foreach (Label caption in root.Query<Label>(className: "slot-caption").ToList())
                {
                    caption.text = "알렉산드리아 폰 에버라이트 변경백\n장기 원정 임무 준비 중";
                    caption.tooltip = caption.text;
                }
            }
        }

        // 제한 폭 UI에서 원문을 툴팁으로 보존하면서 표시 문자열을 안전하게 축약합니다.
        public static string TruncateLabel(string value, int maxCharacters)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxCharacters) return value ?? string.Empty;
            return value.Substring(0, Mathf.Max(1, maxCharacters - 1)) + "…";
        }

        // 큰 HUD 숫자를 언어별 짧은 단위로 바꿔 자원 칩의 폭을 안정적으로 유지합니다.
        public static string FormatHudNumber(int value, bool useEnglish)
        {
            long absolute = Math.Abs((long)value);
            string sign = value < 0 ? "-" : string.Empty;
            if (useEnglish)
            {
                if (absolute >= 1000000) return $"{sign}{absolute / 1000000d:0.#}M";
                if (absolute >= 1000) return $"{sign}{absolute / 1000d:0.#}K";
            }
            else
            {
                if (absolute >= 100000000) return $"{sign}{absolute / 100000000d:0.#}억";
                if (absolute >= 10000) return $"{sign}{absolute / 10000d:0.#}만";
            }
            return value.ToString("N0");
        }

        // HUD 자원의 현재값·예상 증가량·소유 성 수와 계산식을 일관된 툴팁 문장으로 만듭니다.
        public static string BuildResourceCalculationTooltip(string resourceName, int current, int monthlyGain,
            int castleCount, string basis)
        {
            return $"{resourceName} {current:N0}\n다음 턴 예상 +{monthlyGain:N0} · 소유 성 {castleCount}개\n근거: {basis}";
        }

        // 세력색을 볼 수 없는 상황에서도 사용할 고유 영문 코드를 반환합니다.
        public static string GetFactionAccessibilityCode(string factionId)
        {
            switch (factionId)
            {
                case "avalon": return "AV";
                case "valdor": return "VD";
                case "ironheart": return "IH";
                case "sylvanroad": return "SY";
                case "necropolis": return "NC";
                default: return "??";
            }
        }

        // 현재 세력 목록과 경로 기호를 색상 없이 읽을 수 있는 범례 문장으로 갱신합니다.
        private void RefreshFactionLegend()
        {
            if (factionLegendLabel == null) return;
            List<string> entries = new List<string>();
            foreach (WIFactionDefinition faction in database.Factions)
            {
                entries.Add($"[{GetFactionAccessibilityCode(faction.Id)}] {faction.DisplayName.Get(database.UseEnglish)}");
            }
            factionLegendLabel.text = string.Join("   ", entries.Take(2)) + "\n" +
                                      string.Join("   ", entries.Skip(2).Take(2)) + "\n" +
                                      string.Join(string.Empty, entries.Skip(4)) +
                                      "\n— 이동   × 전선   ◎ 선택 연결";
        }

        // 성 수치에 대응하는 간결한 상태명을 반환합니다.
        private string GetCastleStatusName(int value)
        {
            if (value < 25)
            {
                return "낙후";
            }

            if (value < 50)
            {
                return "보통";
            }

            if (value < 75)
            {
                return "발전";
            }

            return "번성";
        }

        // 현재 중점 사업의 담당자와 투자 상태를 표시할 문자열로 만듭니다.
        private string GetProjectStatusText(WICastleProjectState project)
        {
            if (project == null)
            {
                return "이번 달 중점 사업이 지정되지 않았습니다.";
            }

            WIHeroDefinition manager = database.GetHero(project.ManagerHeroId);
            string managerName = manager == null
                ? "담당자 없음"
                : manager.DisplayName.Get(database.UseEnglish);
            string result = $"{GetProjectDisplayName(project.ProjectType)} · {managerName} · {GetInvestmentDisplayName(project.Investment)}";
            return project.Delegated
                ? $"태수 위임 · {result} · 예상 +{project.ExpectedGain} · {project.GoldCost}G"
                : result;
        }

        // 중점 사업과 투자 단계를 선택하는 모달을 엽니다.
        private void OpenFocusProjectModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            if (selectedCastle.ActiveProject != null)
            {
                ShowMessage("UI_PROJECT_ALREADY_ASSIGNED");
                return;
            }

            if (selectedCastle.DelegatedToGovernor)
            {
                ShowMessage("태수 위임을 해제한 뒤 직접 중점 사업을 지정할 수 있습니다.");
                return;
            }

            VisualElement panel = CreateModal("이번 달 중점 사업");
            foreach (WICastleProjectType projectType in System.Enum.GetValues(typeof(WICastleProjectType)))
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("research-row");
                Label nameLabel = new Label(GetProjectDisplayName(projectType));
                row.Add(nameLabel);

                Button basicButton = new Button(() => OpenProjectManagerModal(projectType, WIProjectInvestment.Basic));
                basicButton.text = $"기본 {WIAdministrationTurnSystem.GetProjectCost(database, projectType, WIProjectInvestment.Basic)}G";
                row.Add(basicButton);

                Button intensiveButton = new Button(() => OpenProjectManagerModal(projectType, WIProjectInvestment.Intensive));
                intensiveButton.text = $"집중 {WIAdministrationTurnSystem.GetProjectCost(database, projectType, WIProjectInvestment.Intensive)}G";
                row.Add(intensiveButton);
                panel.Add(row);
            }
        }

        // 선택한 사업을 맡길 주둔 영웅 선택 모달을 엽니다.
        private void OpenProjectManagerModal(WICastleProjectType projectType, WIProjectInvestment investment)
        {
            List<WIHeroDefinition> candidates = new List<WIHeroDefinition>();
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                if (hero != null && state.IsCharacterBusy(hero.Id) == false)
                {
                    candidates.Add(hero);
                }
            }

            if (candidates.Count == 0)
            {
                ShowMessage("이 성에 사업을 맡길 대기 인물이 없습니다. 진행 중인 임무는 다음 턴에 처리되며, 임무 완료 후 다시 배정할 수 있습니다.");
                return;
            }

            VisualElement panel = CreateModal("담당 인물 선택");
            foreach (WIHeroDefinition hero in candidates)
            {
                int expectedGain = WIAdministrationTurnSystem.GetExpectedProjectGain(database, projectType, hero, investment) +
                                   WIAdministrationTurnSystem.GetCastleSpecialtyProjectBonus(
                                       database.GetCastle(selectedCastle.CastleId), projectType);
                expectedGain += WIAdministrationTurnSystem.GetFactionPolicyBonus(database, state.FactionPolicy, projectType);
                Button button = new Button(() => AssignCastleProject(projectType, investment, hero));
                int traitBonus = WIAdministrationTurnSystem.GetProjectTraitBonus(database, projectType, hero);
                string traitText = traitBonus > 0 ? $" · 특기 적용 +{traitBonus}" : " · 특기 미적용";
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 예상 성과 +{expectedGain}{traitText}";
                int relevantStat = WIAdministrationTurnSystem.GetProjectRelevantStat(projectType, hero);
                int investmentBonus = investment == WIProjectInvestment.Intensive
                    ? database.ProjectBalance.IntensiveGainBonus : 0;
                int specialtyBonus = WIAdministrationTurnSystem.GetCastleSpecialtyProjectBonus(
                    database.GetCastle(selectedCastle.CastleId), projectType);
                int policyBonus = WIAdministrationTurnSystem.GetFactionPolicyBonus(database, state.FactionPolicy, projectType);
                button.tooltip = $"예상 성과 {expectedGain} = 기본 {database.ProjectBalance.BaseGain} + 담당 적성 {relevantStat}/{database.ProjectBalance.StatDivisor} + 투자 {investmentBonus} + 특기 {traitBonus} + 전문 분야 {specialtyBonus} + 세력 방침 {policyBonus}\n기본·적성·투자·특기 합계는 {database.ProjectBalance.MinimumGain}~{database.ProjectBalance.MaximumGain} 범위로 제한된 뒤 전문 분야와 방침을 더합니다.";
                panel.Add(button);
            }
        }

        // 비용을 지불하고 선택한 성에 월간 중점 사업을 지정합니다.
        private void AssignCastleProject(
            WICastleProjectType projectType,
            WIProjectInvestment investment,
            WIHeroDefinition manager)
        {
            if (projectType == WICastleProjectType.Expansion && CanStartExpansion(selectedCastle) == false)
            {
                ShowMessage("확장에는 성 규모에 맞는 번영과 기술이 필요하며 대형 성은 더 확장할 수 없습니다.");
                return;
            }

            int cost = WIAdministrationTurnSystem.GetProjectCost(database, projectType, investment);
            if (state.Gold < cost)
            {
                ShowMessage("UI_NOT_ENOUGH_RESOURCE");
                return;
            }

            state.Gold -= cost;
            state.PendingPlayerGoldSpent += cost;
            selectedCastle.ActiveProject = new WICastleProjectState
            {
                ProjectType = projectType,
                Investment = investment,
                ManagerHeroId = manager.Id,
                RemainingMonths = projectType == WICastleProjectType.Expansion
                    ? database.ProjectBalance.ExpansionDurationMonths
                    : 1
            };
            WITutorialSystem.Complete(state, "tutorial_project");
            CloseModal();
            SelectCastle(selectedCastle.CastleId);
            RefreshAll();
        }

        // 현재 성이 규모 확장 사업의 수치 조건을 만족하는지 확인합니다.
        private bool CanStartExpansion(WICastleRuntimeState castleState)
        {
            if (castleState.CastleSize == WICastleSize.Large || castleState.PendingSpecialFacilityChoice)
            {
                return false;
            }

            int requiredValue = castleState.CastleSize == WICastleSize.Small ? 50 : 70;
            return castleState.Prosperity >= requiredValue && castleState.Technology >= requiredValue;
        }

        // 사업 열거형을 UI에 표시할 한국어 이름으로 변환합니다.
        private string GetProjectDisplayName(WICastleProjectType projectType)
        {
            switch (projectType)
            {
                case WICastleProjectType.Prosperity:
                    return "번영 사업";
                case WICastleProjectType.Technology:
                    return "기술 사업";
                case WICastleProjectType.Stability:
                    return "안정 사업";
                case WICastleProjectType.Fortification:
                    return "요새 사업";
                case WICastleProjectType.Recruitment:
                    return "인재 사업";
                case WICastleProjectType.Training:
                    return "훈련 사업";
                case WICastleProjectType.Recovery:
                    return "회복 사업";
                case WICastleProjectType.Expansion:
                    return "확장 사업";
                default:
                    return projectType.ToString();
            }
        }

        // 투자 열거형을 UI에 표시할 한국어 이름으로 변환합니다.
        private string GetInvestmentDisplayName(WIProjectInvestment investment)
        {
            return investment == WIProjectInvestment.Intensive ? "집중 투자" : "기본 투자";
        }

        // 현재 선택한 성의 플레이어 소유권을 확인하고 잘못된 명령 진입을 차단합니다.
        private bool EnsureSelectedCastleManageable()
        {
            if (WIAdministrationTurnSystem.CanPlayerManageCastle(state, selectedCastle))
            {
                return true;
            }

            ShowMessage("다른 세력의 성은 정보를 열람할 수 있지만 내정 명령은 내릴 수 없습니다.");
            return false;
        }

        // 이번 달 세력 전체 방침을 선택하는 평정 모달을 엽니다.
        private void OpenFactionPolicyModal()
        {
            VisualElement panel = CreateModal("이번 달 세력 방침");
            foreach (WIFactionPolicy policy in System.Enum.GetValues(typeof(WIFactionPolicy)))
            {
                Button button = new Button(() =>
                {
                    state.FactionPolicy = policy;
                    CloseModal();
                    RefreshAll();
                });
                button.text = $"{GetFactionPolicyDisplayName(policy)} · {GetFactionPolicyDescription(policy)}";
                panel.Add(button);
            }
        }

        // 완료 연구, 진행 상태와 새 연구 후보를 표시합니다.
        private void OpenResearchModal()
        {
            WIFactionRuntimeState faction = state.GetPlayerFactionState();
            VisualElement panel = CreateModal("기술·마법 연구");
            if (string.IsNullOrEmpty(faction.ActiveResearchId) == false)
            {
                WIResearchDefinition active = database.GetResearch(faction.ActiveResearchId);
                panel.Add(new Label($"진행 중: {active.DisplayName.Get(database.UseEnglish)} · {faction.ResearchRemainingMonths}개월 · 담당 {database.GetHero(faction.ResearcherHeroId).DisplayName.Get(database.UseEnglish)}"));
            }
            foreach (WIResearchDefinition research in database.ResearchDefinitions)
            {
                if (faction.CompletedResearchIds.Contains(research.Id))
                {
                    panel.Add(new Label($"완료 · {research.DisplayName.Get(database.UseEnglish)} · {research.Description.Get(database.UseEnglish)}"));
                    continue;
                }
                Button button = new Button(() => OpenResearcherModal(research));
                button.text = $"{research.DisplayName.Get(database.UseEnglish)} · 마나 {research.ManaCost} · 기술 {research.RequiredTechnology} · {research.DurationMonths}개월";
                button.tooltip = $"비용: 마나 {research.ManaCost}\n조건: 보유 성 최고 기술 {research.RequiredTechnology} 이상, 진행 중 연구 없음\n기간: 기본 {research.DurationMonths}개월 · 담당 인물 지력에 따라 단축 가능\n효과: {research.Description.Get(database.UseEnglish)}";
                button.SetEnabled(string.IsNullOrEmpty(faction.ActiveResearchId));
                panel.Add(button);
            }
        }

        // 플레이어 세력의 유휴 인물 중 연구 담당자를 선택합니다.
        private void OpenResearcherModal(WIResearchDefinition research)
        {
            VisualElement panel = CreateModal($"연구 담당 · {research.DisplayName.Get(database.UseEnglish)}");
            bool hasCandidate = false;
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || state.IsCharacterBusy(character.HeroId)) continue;
                bool inPlayerFaction = state.Castles.Exists(castle =>
                    castle.FactionId == state.PlayerFactionId && castle.HeroIds.Contains(character.HeroId));
                if (inPlayerFaction == false) continue;
                hasCandidate = true;
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                Button button = new Button(() =>
                {
                    bool started = WIAdministrationTurnSystem.BeginResearch(
                        database, state, state.PlayerFactionId, research.Id, hero.Id);
                    if (started)
                    {
                        WITutorialSystem.Complete(state, "tutorial_research");
                    }
                    CloseModal();
                    RefreshAll();
                    ShowMessage(started ? "연구를 시작했습니다." : "연구 조건 또는 자원이 부족합니다.");
                });
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 지력 {hero.Intelligence}";
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 수동 저장, 불러오기와 자동 저장 상태를 표시하는 시스템 모달을 엽니다.
        private void OpenSystemModal()
        {
            VisualElement panel = CreateModal("시스템 · 저장 및 불러오기");
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            if (service == null)
            {
                panel.Add(new Label("캠페인 런타임 서비스를 찾을 수 없습니다."));
                return;
            }
            panel.Add(new Label($"턴 종료 자동 저장: {(service.AutoSaveEnabled ? "사용" : "미사용")}"));
            AddSettingsControls(panel);
            for (int slot = 1; slot <= 3; slot += 1)
            {
                int selectedSlot = slot;
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                Button saveButton = new Button(() => SaveCampaignSlot(selectedSlot));
                saveButton.text = $"슬롯 {slot} 저장";
                Button loadButton = new Button(() => LoadCampaignSlot(selectedSlot));
                loadButton.text = service.HasSave(slot) ? $"슬롯 {slot} 불러오기" : $"슬롯 {slot} 비어 있음";
                loadButton.SetEnabled(service.HasSave(slot));
                row.Add(saveButton);
                row.Add(loadButton);
                panel.Add(row);
            }
            Button autoLoad = new Button(() => LoadCampaignSlot(0));
            autoLoad.text = service.HasSave(0) ? "자동 저장 불러오기" : "자동 저장 없음";
            autoLoad.SetEnabled(service.HasSave(0));
            panel.Add(autoLoad);
        }

        // 시스템 모달에 언어, 오디오, 전체 화면과 목표 프레임 설정을 추가합니다.
        private void AddSettingsControls(VisualElement panel)
        {
            WISystemSettingsService settingsService = WISystemSettingsService.Instance;
            if (settingsService == null)
            {
                panel.Add(new Label("시스템 설정 서비스를 찾을 수 없습니다."));
                return;
            }
            WISystemSettingsState settings = settingsService.Settings;
            panel.Add(new Label("언어 · 화면 · 오디오"));
            Toggle language = new Toggle("English") { value = settings.UseEnglish };
            language.RegisterValueChangedCallback(evt => settings.UseEnglish = evt.newValue);
            panel.Add(language);
            Toggle fullscreen = new Toggle("전체 화면") { value = settings.Fullscreen };
            fullscreen.RegisterValueChangedCallback(evt => settings.Fullscreen = evt.newValue);
            panel.Add(fullscreen);
            Slider master = new Slider("전체 음량", 0f, 1f) { value = settings.MasterVolume };
            master.RegisterValueChangedCallback(evt => settings.MasterVolume = evt.newValue);
            panel.Add(master);
            Slider music = new Slider("음악 음량", 0f, 1f) { value = settings.MusicVolume };
            music.RegisterValueChangedCallback(evt => settings.MusicVolume = evt.newValue);
            panel.Add(music);
            Slider sfx = new Slider("효과음 음량", 0f, 1f) { value = settings.SfxVolume };
            sfx.RegisterValueChangedCallback(evt => settings.SfxVolume = evt.newValue);
            panel.Add(sfx);
            VisualElement frameRateRow = new VisualElement();
            frameRateRow.style.flexDirection = FlexDirection.Row;
            foreach (int frameRate in new[] { 30, 60, 120 })
            {
                int selectedFrameRate = frameRate;
                Button frameButton = new Button(() => settings.TargetFrameRate = selectedFrameRate);
                frameButton.text = $"{frameRate} FPS";
                frameRateRow.Add(frameButton);
            }
            panel.Add(frameRateRow);
            Button apply = new Button(() =>
            {
                settingsService.Apply();
                settingsService.Save();
                BuildMap();
                RefreshAll();
                CloseModal();
                ShowMessage(database.UseEnglish ? "Settings applied." : "설정을 적용했습니다.");
            });
            apply.text = "설정 적용";
            panel.Add(apply);
            Button reset = new Button(() =>
            {
                settingsService.ResetToDefaults();
                BuildMap();
                RefreshAll();
                CloseModal();
                ShowMessage("설정을 기본값으로 되돌렸습니다.");
            });
            reset.text = "기본값 복원";
            panel.Add(reset);
        }

        // 선택한 슬롯에 현재 캠페인을 저장하고 결과를 안내합니다.
        private void SaveCampaignSlot(int slot)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            string error = "캠페인 런타임 서비스를 찾을 수 없습니다.";
            bool saved = service != null && service.SaveSlot(slot, out error);
            CloseModal();
            ShowMessage(saved ? $"슬롯 {slot}에 저장했습니다." : error);
        }

        // 선택한 슬롯의 캠페인을 불러오고 지도와 HUD 참조를 새 상태로 갱신합니다.
        private void LoadCampaignSlot(int slot)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            string error = "캠페인 런타임 서비스를 찾을 수 없습니다.";
            if (service == null || service.LoadSlot(slot, out error) == false)
            {
                CloseModal();
                ShowMessage(error);
                return;
            }
            state = service.State;
            selectedCastle = null;
            BuildMap();
            SelectInitialCastle();
            CloseModal();
            RefreshAll();
            string loadedMessage = slot == 0 ? "자동 저장을 불러왔습니다." : $"슬롯 {slot}을 불러왔습니다.";
            ShowMessage(string.IsNullOrEmpty(error) ? loadedMessage : $"{loadedMessage}\n{error}");
        }

        // 각 세력의 공개 정보와 AI 운영 성향을 정세 창에 표시합니다.
        private void OpenFactionOverviewModal()
        {
            VisualElement panel = CreateModal("대륙 세력 정세");
            foreach (WIFactionDefinition faction in database.Factions)
            {
                int castleCount = state.Castles.FindAll(castle => castle.FactionId == faction.Id).Count;
                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                string economy = faction.PlayerFaction && factionState != null
                    ? $" · 금화 {factionState.Gold:N0} · 마나 {factionState.ManaCrystal:N0} · 영향력 {factionState.Influence:N0}"
                    : string.Empty;
                string status = factionState?.Eliminated == true ? $" · 멸망(제 {factionState.EliminatedTurn}턴)" : string.Empty;
                panel.Add(new Label($"{faction.DisplayName.Get(database.UseEnglish)} · 영토 {castleCount}성 · 성향 {GetAIStrategyDisplayName(faction.AIStrategy)}{status}{economy}"));
                foreach (WIStartingRelationshipDefinition relationship in database.StartingRelationships)
                {
                    if (relationship.FactionId != faction.Id) continue;
                    WIHeroDefinition first = database.GetHero(relationship.FirstHeroId);
                    WIHeroDefinition second = database.GetHero(relationship.SecondHeroId);
                    if (first == null || second == null) continue;
                    string level = relationship.Level == WIRelationshipLevel.Conflict ? "갈등" : "친애";
                    panel.Add(new Label($"  └ 주요 관계 · {first.DisplayName.Get(database.UseEnglish)} ↔ {second.DisplayName.Get(database.UseEnglish)} · {level}\n     {relationship.Context.Get(database.UseEnglish)}"));
                }
            }
        }

        // 플레이어 세력과 다른 세력의 현재 외교 상태를 목록으로 표시합니다.
        private void OpenDiplomacyModal()
        {
            VisualElement panel = CreateModal("외교");
            panel.Add(new Label("복잡한 점수 대신 현재 관계와 실행 가능한 명령만 표시합니다."));
            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.Id == state.PlayerFactionId)
                {
                    continue;
                }
                if (state.GetFactionState(faction.Id)?.Eliminated == true) continue;

                WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(state.PlayerFactionId, faction.Id);
                Button factionButton = new Button(() => OpenDiplomacyTargetModal(faction));
                factionButton.text = $"{faction.DisplayName.Get(database.UseEnglish)} · {GetDiplomaticStatusDisplayName(relation.Status)}";
                panel.Add(factionButton);
            }
        }

        // 조사·방첩·유언비어·인재 이간 중 실행할 계략을 선택합니다.
        private void OpenSchemeModal()
        {
            VisualElement panel = CreateModal("계략");
            panel.Add(new Label("지력이 높은 인물을 보내십시오. 적 성의 치안과 방첩이 성공률을 낮춥니다."));
            WIFactionRuntimeState faction = state.GetFactionState(state.PlayerFactionId);
            panel.Add(new Label($"보유 영향력 {faction.Influence:N0}"));
            foreach (WISchemeMissionState mission in state.SchemeMissions.Where(item => item.InitiatorFactionId == state.PlayerFactionId))
            {
                WISchemeDefinition activeScheme = database.GetScheme(mission.SchemeId);
                WIHeroDefinition activeAgent = database.GetHero(mission.AgentHeroId);
                WICastleDefinition activeTarget = database.GetCastle(mission.TargetCastleId);
                panel.Add(new Label($"진행 중 · {activeScheme?.DisplayName.Get(database.UseEnglish)} · " +
                    $"{activeAgent?.DisplayName.Get(database.UseEnglish)} → {activeTarget?.DisplayName.Get(database.UseEnglish)} · {mission.RemainingMonths}개월"));
            }
            foreach (WISchemeDefinition scheme in database.SchemeDefinitions)
            {
                Button button = new Button(() => OpenSchemeAgentModal(scheme));
                button.text = $"{scheme.DisplayName.Get(database.UseEnglish)} · 영향력 {scheme.InfluenceCost} · " +
                              $"기본 성공 {scheme.BaseSuccessChance}% · 기본 발각 {scheme.BaseDetectionChance}%\n" +
                              scheme.Description.Get(database.UseEnglish);
                button.SetEnabled(faction.Influence >= scheme.InfluenceCost);
                panel.Add(button);
            }
        }

        // 선택한 계략을 맡길 플레이어 세력의 유휴 인물을 표시합니다.
        private void OpenSchemeAgentModal(WISchemeDefinition scheme)
        {
            VisualElement panel = CreateModal($"{scheme.DisplayName.Get(database.UseEnglish)} · 담당 인물");
            bool hasCandidate = false;
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == state.PlayerFactionId))
            {
                foreach (string heroId in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false))
                {
                    WIHeroDefinition hero = database.GetHero(heroId);
                    if (hero == null)
                    {
                        continue;
                    }

                    hasCandidate = true;
                    Button button = new Button(() => OpenSchemeTargetCastleModal(scheme, hero));
                    button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 지력 {hero.Intelligence}";
                    panel.Add(button);
                }
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 계략 종류에 맞는 아군 또는 적 성을 대상 목록으로 표시합니다.
        private void OpenSchemeTargetCastleModal(WISchemeDefinition scheme, WIHeroDefinition agent)
        {
            VisualElement panel = CreateModal($"{scheme.DisplayName.Get(database.UseEnglish)} · 대상 성");
            bool ownTarget = scheme.SchemeType == WISchemeType.Counterintelligence;
            foreach (WICastleRuntimeState castleState in state.Castles.Where(item =>
                         ownTarget ? item.FactionId == state.PlayerFactionId : item.FactionId != state.PlayerFactionId))
            {
                WICastleDefinition castle = database.GetCastle(castleState.CastleId);
                Button button = new Button(() =>
                {
                    if (scheme.SchemeType == WISchemeType.Alienation)
                    {
                        OpenSchemeTargetHeroModal(scheme, agent, castleState);
                    }
                    else
                    {
                        ExecuteScheme(scheme, agent, castleState, null);
                    }
                });
                WISchemeIntelState intel = state.SchemeIntel.Find(item =>
                    item.ObserverFactionId == state.PlayerFactionId && item.TargetCastleId == castleState.CastleId);
                string defense = ownTarget ? $"방첩 {castleState.CounterintelligenceMonths}개월"
                    : intel == null ? "정보 미확보" : $"조사 정보 {intel.RemainingMonths}개월";
                button.text = $"{castle.DisplayName.Get(database.UseEnglish)} · {defense}";
                bool canRevealCalculation = ownTarget || intel != null;
                if (canRevealCalculation)
                {
                    int successChance = WISchemeSystem.CalculateSuccessChance(scheme, agent, castleState);
                    int counterPenalty = castleState.CounterintelligenceMonths > 0 ? 20 : 0;
                    button.tooltip = $"성공 {successChance}% = 기본 {scheme.BaseSuccessChance}% + 담당 지력 {agent.Intelligence}/2 - 치안 {castleState.Stability}/2 - 방첩 {counterPenalty}%\n발각은 기본 확률 + 치안/4 + 방첩 - 담당 지력/3으로 별도 판정합니다.";
                }
                else
                {
                    button.tooltip = $"정보 미확보 · 기본 성공 {scheme.BaseSuccessChance}%만 확인할 수 있습니다. 조사 성공 후 치안·방첩을 포함한 최종 확률이 공개됩니다.";
                }
                panel.Add(button);
            }
        }

        // 인재 이간의 대상 성에 주둔한 적 인물을 표시합니다.
        private void OpenSchemeTargetHeroModal(WISchemeDefinition scheme, WIHeroDefinition agent, WICastleRuntimeState castle)
        {
            VisualElement panel = CreateModal("인재 이간 · 대상 인물");
            if (WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, castle) == false)
            {
                panel.Add(new Label("주둔 인물 정보가 없습니다. 먼저 해당 성의 조사를 성공시키십시오."));
                return;
            }
            foreach (string heroId in castle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (hero == null || character == null)
                {
                    continue;
                }

                Button button = new Button(() => ExecuteScheme(scheme, agent, castle, hero));
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 충성 {GetLoyaltyDisplayName(character.LoyaltyState)}";
                panel.Add(button);
            }
        }

        // 계략 담당 인물을 한 달 임무에 배정하고 다음 턴 판정을 예약합니다.
        private void ExecuteScheme(WISchemeDefinition scheme, WIHeroDefinition agent, WICastleRuntimeState targetCastle, WIHeroDefinition targetHero)
        {
            bool scheduled = WISchemeSystem.TrySchedule(database, state, scheme.Id, state.PlayerFactionId,
                agent.Id, targetCastle.CastleId, targetHero?.Id, UnityEngine.Random.Range(0, 100), out string message);
            RefreshAll();
            ShowMessage(scheduled ? message + "\n담당 인물은 결과가 나올 때까지 다른 임무에 배정할 수 없습니다." : message);
        }

        // 충성 상태를 계략 UI용 한국어로 변환합니다.
        private string GetLoyaltyDisplayName(WILoyaltyState loyalty)
        {
            switch (loyalty)
            {
                case WILoyaltyState.Unsettled: return "동요";
                case WILoyaltyState.Danger: return "위험";
                default: return "안정";
            }
        }

        // 선택 세력의 관계 설명과 현재 가능한 외교 명령을 표시합니다.
        private void OpenDiplomacyTargetModal(WIFactionDefinition targetFaction)
        {
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(state.PlayerFactionId, targetFaction.Id);
            VisualElement panel = CreateModal($"외교 · {targetFaction.DisplayName.Get(database.UseEnglish)}");
            panel.Add(new Label(GetDiplomaticDescription(relation)));

            List<WICharacterRuntimeState> ourPrisoners = state.Characters.Where(item => item.Captured &&
                item.CapturedFromFactionId == state.PlayerFactionId && item.CaptorFactionId == targetFaction.Id).ToList();
            List<WICharacterRuntimeState> theirPrisoners = state.Characters.Where(item => item.Captured &&
                item.CapturedFromFactionId == targetFaction.Id && item.CaptorFactionId == state.PlayerFactionId).ToList();
            foreach (WICharacterRuntimeState prisoner in ourPrisoners)
            {
                WIHeroDefinition hero = database.GetHero(prisoner.HeroId);
                Button ransom = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.RansomPrisoner(database, state, state.PlayerFactionId, prisoner.HeroId),
                    targetFaction));
                ransom.text = $"포로 몸값 · {hero?.DisplayName.Get(database.UseEnglish) ?? prisoner.HeroId} · " +
                              $"금화 {database.PrisonerRansomGold}";
                ransom.SetEnabled(state.GetPlayerFactionState().Gold >= database.PrisonerRansomGold);
                panel.Add(ransom);
            }
            if (ourPrisoners.Count > 0 && theirPrisoners.Count > 0)
            {
                WICharacterRuntimeState ourPrisoner = ourPrisoners[0];
                WICharacterRuntimeState theirPrisoner = theirPrisoners[0];
                Button exchange = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.ExchangePrisoners(state, state.PlayerFactionId, targetFaction.Id,
                        ourPrisoner.HeroId, theirPrisoner.HeroId), targetFaction));
                exchange.text = $"포로 맞교환 · {database.GetHero(ourPrisoner.HeroId)?.DisplayName.Get(database.UseEnglish)} ↔ " +
                                database.GetHero(theirPrisoner.HeroId)?.DisplayName.Get(database.UseEnglish);
                panel.Add(exchange);
            }

            if (relation.Status == WIDiplomaticStatus.War || relation.Status == WIDiplomaticStatus.Neutral)
            {
                Button improve = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.ImproveDiplomaticRelations(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                improve.text = relation.Status == WIDiplomaticStatus.War
                    ? $"휴전 교섭 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} · 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}"
                    : $"친선 사절 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} · 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}";
                improve.tooltip = $"확정 명령 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} + 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}를 소비해 관계를 한 단계 개선합니다.";
                panel.Add(improve);
            }
            else if (relation.Status == WIDiplomaticStatus.Friendly)
            {
                Button pact = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.SignNonAggression(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                pact.text = $"불가침 협정 · 영향력 {WIAdministrationTurnSystem.NonAggressionInfluenceCost}";
                pact.tooltip = $"확정 명령 · 우호 관계에서 영향력 {WIAdministrationTurnSystem.NonAggressionInfluenceCost}를 소비합니다.";
                panel.Add(pact);
            }
            else if (relation.Status == WIDiplomaticStatus.NonAggression)
            {
                Button alliance = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.FormAlliance(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                alliance.text = $"동맹 체결 · 영향력 {WIAdministrationTurnSystem.AllianceInfluenceCost}";
                alliance.tooltip = $"확정 명령 · 불가침 관계에서 영향력 {WIAdministrationTurnSystem.AllianceInfluenceCost}를 소비합니다.";
                panel.Add(alliance);
            }
            else if (relation.Status == WIDiplomaticStatus.Alliance)
            {
                Button aid = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.RequestAllianceAid(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                aid.text = relation.AidCooldownMonths > 0
                    ? $"금화 원조 · {relation.AidCooldownMonths}개월 후 재요청"
                    : $"금화 원조 요청 · +{WIAdministrationTurnSystem.AllianceAidGold}";
                aid.SetEnabled(relation.AidCooldownMonths <= 0);
                aid.tooltip = relation.AidCooldownMonths > 0
                    ? $"사용 불가 · 재요청 대기 {relation.AidCooldownMonths}개월"
                    : $"확정 명령 · 동맹으로부터 금화 {WIAdministrationTurnSystem.AllianceAidGold}를 받고 재사용 대기시간이 적용됩니다.";
                panel.Add(aid);

                if (relation.JointAttackMonthsRemaining > 0)
                {
                    panel.Add(new Label($"공동 공격 진행 · {database.GetCastle(relation.JointAttackTargetCastleId)?.DisplayName.Get(database.UseEnglish)} · " +
                                        $"{relation.JointAttackMonthsRemaining}개월 남음"));
                }
                foreach (WICastleRuntimeState target in state.Castles.Where(item =>
                             item.FactionId != state.PlayerFactionId && item.FactionId != targetFaction.Id &&
                             WIAdministrationTurnSystem.AreFactionsAtWar(state, state.PlayerFactionId, item.FactionId) &&
                             WIAdministrationTurnSystem.AreFactionsAtWar(state, targetFaction.Id, item.FactionId))
                         .GroupBy(item => item.FactionId).Select(group => group.First()))
                {
                    Button jointAttack = new Button(() => ExecuteDiplomaticCommand(
                        WIAdministrationTurnSystem.ProposeJointAttack(database, state, state.PlayerFactionId,
                            targetFaction.Id, target.CastleId), targetFaction));
                    jointAttack.text = $"공동 공격 제안 · {database.GetCastle(target.CastleId).DisplayName.Get(database.UseEnglish)} · " +
                                       $"영향력 {database.JointAttackInfluenceCost} · {database.JointAttackDurationMonths}개월";
                    panel.Add(jointAttack);
                }
            }

            if (relation.Status != WIDiplomaticStatus.War)
            {
                Button war = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.DeclareWar(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                war.text = $"선전포고 · 영향력 {WIAdministrationTurnSystem.DeclareWarInfluenceCost}";
                war.tooltip = $"위험한 확정 명령 · 영향력 {WIAdministrationTurnSystem.DeclareWarInfluenceCost}를 소비하고 즉시 전쟁 상태가 됩니다.";
                panel.Add(war);
            }
        }

        // 외교 명령 결과를 반영하고 같은 대상의 최신 관계 화면을 다시 엽니다.
        private void ExecuteDiplomaticCommand(bool succeeded, WIFactionDefinition targetFaction)
        {
            RefreshAll();
            if (succeeded == false)
            {
                ShowMessage("외교 명령 조건이나 자원이 부족합니다.");
                return;
            }

            OpenDiplomacyTargetModal(targetFaction);
        }

        // 외교 상태를 UI용 한국어 이름으로 변환합니다.
        private string GetDiplomaticStatusDisplayName(WIDiplomaticStatus status)
        {
            switch (status)
            {
                case WIDiplomaticStatus.War: return "전쟁";
                case WIDiplomaticStatus.Friendly: return "우호";
                case WIDiplomaticStatus.NonAggression: return "불가침";
                case WIDiplomaticStatus.Alliance: return "동맹";
                default: return "중립";
            }
        }

        // 현재 관계와 다음 행동의 의미를 짧은 문장으로 설명합니다.
        private string GetDiplomaticDescription(WIDiplomaticRelationState relation)
        {
            switch (relation.Status)
            {
                case WIDiplomaticStatus.War: return "교전 중입니다. 적 성으로 출정할 수 있으며 휴전 교섭을 시도할 수 있습니다.";
                case WIDiplomaticStatus.Friendly: return "사절 교류가 안정되었습니다. 불가침 협정을 제안할 수 있습니다.";
                case WIDiplomaticStatus.NonAggression: return "서로 공격하지 않기로 합의했습니다. 동맹 체결을 제안할 수 있습니다.";
                case WIDiplomaticStatus.Alliance: return $"동맹 관계입니다. 금화 원조 재요청 대기 {relation.AidCooldownMonths}개월.";
                default: return "공식 협정이 없는 중립 관계입니다. 친선 사절을 파견할 수 있습니다.";
            }
        }

        // AI 성향을 정세 화면용 한국어 이름으로 변환합니다.
        private string GetAIStrategyDisplayName(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Development: return "개발";
                case WIAIStrategy.Defense: return "수비";
                case WIAIStrategy.Aggressive: return "공세";
                case WIAIStrategy.Scheme: return "모략";
                default: return "부국";
            }
        }

        // 선택한 성의 태수, 운영 방침과 월간 예산을 설정합니다.
        private void OpenDelegationModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("태수 위임 설정");
            panel.Add(new Label("태수 임명"));
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                Button governorButton = new Button(() =>
                {
                    if (WIAdministrationTurnSystem.AssignGovernor(state, selectedCastle.CastleId, hero.Id) == false)
                    {
                        ShowMessage("다른 임무를 수행 중인 인물은 태수로 임명할 수 없습니다.");
                        return;
                    }
                    CloseModal();
                    OpenDelegationModal();
                });
                governorButton.text = hero.Id == selectedCastle.GovernorHeroId
                    ? $"● {hero.DisplayName.Get(database.UseEnglish)}"
                    : hero.DisplayName.Get(database.UseEnglish);
                panel.Add(governorButton);
            }

            Button dismissGovernor = new Button(() =>
            {
                selectedCastle.GovernorHeroId = string.Empty;
                selectedCastle.DelegatedToGovernor = false;
                CloseModal();
                OpenDelegationModal();
            });
            dismissGovernor.text = "태수 해임";
            panel.Add(dismissGovernor);

            panel.Add(new Label("운영 방침"));
            foreach (WIGovernorPolicy policy in System.Enum.GetValues(typeof(WIGovernorPolicy)))
            {
                Button policyButton = new Button(() =>
                {
                    selectedCastle.GovernorPolicy = policy;
                    CloseModal();
                    OpenDelegationModal();
                });
                policyButton.text = policy == selectedCastle.GovernorPolicy
                    ? $"● {GetGovernorPolicyDisplayName(policy)}"
                    : GetGovernorPolicyDisplayName(policy);
                panel.Add(policyButton);
            }

            Button basicBudget = new Button(() => SetGovernorBudget(database.ProjectBalance.BasicCost));
            basicBudget.text = $"기본 예산 · 월 {database.ProjectBalance.BasicCost}G";
            panel.Add(basicBudget);
            Button intensiveBudget = new Button(() => SetGovernorBudget(database.ProjectBalance.IntensiveCost));
            intensiveBudget.text = $"집중 예산 · 월 {database.ProjectBalance.IntensiveCost}G";
            panel.Add(intensiveBudget);

            panel.Add(new Label(WIAdministrationTurnSystem.GetDelegationPreview(database, state, selectedCastle)));

            Button toggle = new Button(() =>
            {
                if (selectedCastle.DelegatedToGovernor == false && string.IsNullOrEmpty(selectedCastle.GovernorHeroId))
                {
                    ShowMessage("먼저 태수를 임명해야 합니다.");
                    return;
                }
                selectedCastle.DelegatedToGovernor = selectedCastle.DelegatedToGovernor == false;
                CloseModal();
                SelectCastle(selectedCastle.CastleId);
            });
            toggle.text = selectedCastle.DelegatedToGovernor ? "직접 관리로 전환" : "태수에게 위임";
            panel.Add(toggle);
        }

        // 태수에게 허용할 월간 사업 예산을 변경합니다.
        private void SetGovernorBudget(int budget)
        {
            selectedCastle.GovernorMonthlyBudget = budget;
            CloseModal();
            OpenDelegationModal();
        }

        // 최근 완료된 턴의 자원, 사업과 위임 결과를 월보로 표시합니다.
        private void OpenMonthlyReportModal()
        {
            if (state.LastMonthlyReport == null)
            {
                ShowMessage("아직 작성된 월보가 없습니다. 다음 턴을 진행하십시오.");
                return;
            }

            WITurnSummary report = state.LastMonthlyReport;
            VisualElement panel = CreateModalWithFooter("지난달 월보", out VisualElement battleFooter);
            panel.Add(new Label($"금화 +{report.GoldGained} / 지출 -{report.GoldSpent}"));
            panel.Add(new Label($"마나 +{report.ManaGained} / 영향력 +{report.InfluenceGained}"));
            foreach (string delegationReport in report.DelegationReports)
            {
                panel.Add(new Label(delegationReport));
            }

            if (report.AIReasonReports != null && report.AIReasonReports.Count > 0) panel.Add(new Label("AI 세력 판단 근거"));
            foreach (string aiReport in report.AIReasonReports ?? new List<string>())
            {
                panel.Add(new Label(aiReport));
            }

            foreach (string news in report.News)
            {
                panel.Add(new Label(news));
            }

            foreach (WIPendingProjectEvent pendingEvent in new List<WIPendingProjectEvent>(state.PendingProjectEvents))
            {
                Button eventButton = new Button(() => OpenProjectEventModal(pendingEvent));
                eventButton.text = $"선택 사건 · {pendingEvent.Title}";
                panel.Add(eventButton);
            }

            foreach (WIPendingRelationshipEvent pendingEvent in new List<WIPendingRelationshipEvent>(state.PendingRelationshipEvents))
            {
                WIRelationshipEventDefinition definition = database.GetRelationshipEvent(pendingEvent.EventId);
                if (definition == null) continue;
                Button eventButton = new Button(() => OpenRelationshipEventModal(pendingEvent));
                eventButton.text = $"관계 사건 · {definition.Title.Get(database.UseEnglish)}";
                panel.Add(eventButton);
            }

            foreach (WIPendingRegionalEvent pendingEvent in new List<WIPendingRegionalEvent>(state.PendingRegionalEvents))
            {
                WIRegionalEventDefinition definition = database.GetRegionalEvent(pendingEvent.EventId);
                if (definition == null) continue;
                Button eventButton = new Button(() => OpenRegionalEventModal(pendingEvent));
                eventButton.text = $"지역 사건 · {definition.Title.Get(database.UseEnglish)}";
                panel.Add(eventButton);
            }

            foreach (WIPendingOccupationEvent pendingEvent in new List<WIPendingOccupationEvent>(state.PendingOccupationEvents))
            {
                WICastleDefinition castle = database.GetCastle(pendingEvent.CastleId);
                Button eventButton = new Button(() => OpenOccupationEventModal(pendingEvent));
                eventButton.text = $"점령 통치 · {castle?.DisplayName.Get(database.UseEnglish) ?? pendingEvent.CastleId}";
                panel.Add(eventButton);
            }

            foreach (WIPendingRecruitmentEvent pendingEvent in new List<WIPendingRecruitmentEvent>(state.PendingRecruitmentEvents))
            {
                WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(pendingEvent.EventId);
                if (definition == null) continue;
                Button eventButton = new Button(() => OpenRecruitmentEventModal(pendingEvent));
                eventButton.text = $"등용 요구 · {definition.Title.Get(database.UseEnglish)}";
                panel.Add(eventButton);
            }

            foreach (WIPendingLegacyChoice legacyChoice in new List<WIPendingLegacyChoice>(state.PendingLegacyChoices))
            {
                Button legacyButton = new Button(() => ConfirmLegacyChoice(legacyChoice));
                legacyButton.text = $"영웅의 흔적 · {legacyChoice.Legacy.DisplayName}";
                panel.Add(legacyButton);
            }
            AddPendingBattleActions(battleFooter);
        }

        // 지역 사건의 배경과 ScriptableObject 선택 결과를 표시합니다.
        private void OpenRegionalEventModal(WIPendingRegionalEvent pendingEvent)
        {
            WIRegionalEventDefinition definition = database.GetRegionalEvent(pendingEvent.EventId);
            if (definition == null) return;
            VisualElement panel = CreateModal(definition.Title.Get(database.UseEnglish));
            panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            for (int index = 0; index < definition.Choices.Count; index += 1)
            {
                int selectedIndex = index;
                WIRegionalEventChoiceDefinition choice = definition.Choices[index];
                Button button = new Button(() =>
                {
                    if (WIRegionalEventSystem.Resolve(database, state, pendingEvent, selectedIndex, state.LastMonthlyReport))
                    {
                        CloseModal();
                        RefreshAll();
                    }
                });
                button.text = $"{choice.Label.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}";
                button.SetEnabled(WIRegionalEventSystem.CanChoose(state, choice));
                panel.Add(button);
            }
        }

        // 새 점령지의 통치 방침과 예상 자원·불안 결과를 표시합니다.
        private void OpenOccupationEventModal(WIPendingOccupationEvent pendingEvent)
        {
            WICastleDefinition castle = database.GetCastle(pendingEvent.CastleId);
            WIFactionDefinition defeatedFaction = database.GetFaction(pendingEvent.DefeatedFactionId);
            VisualElement panel = CreateModal($"점령 통치 · {castle?.DisplayName.Get(database.UseEnglish) ?? pendingEvent.CastleId}");
            panel.Add(new Label($"구 소유 세력 · {defeatedFaction?.DisplayName.Get(database.UseEnglish) ?? pendingEvent.DefeatedFactionId}\n점령 불안을 줄일 통치 방침을 선택하십시오."));
            for (int index = 0; index < database.OccupationChoices.Count; index += 1)
            {
                int selectedIndex = index;
                WIOccupationChoiceDefinition choice = database.OccupationChoices[index];
                Button button = new Button(() =>
                {
                    if (WIOccupationEventSystem.Resolve(database, state, pendingEvent, selectedIndex, state.LastMonthlyReport))
                    {
                        CloseModal();
                        RefreshAll();
                    }
                });
                button.text = $"{choice.Label.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)} · 불안 {choice.UnrestMonths}개월";
                button.SetEnabled(WIOccupationEventSystem.CanChoose(state, choice));
                panel.Add(button);
            }
        }

        // 등용 요구 사건의 조건과 선택 결과를 표시합니다.
        private void OpenRecruitmentEventModal(WIPendingRecruitmentEvent pendingEvent)
        {
            WIRecruitmentEventDefinition definition = database.GetRecruitmentEvent(pendingEvent.EventId);
            WIHeroDefinition candidate = database.GetHero(pendingEvent.CandidateHeroId);
            if (definition == null || candidate == null) return;
            VisualElement panel = CreateModal(definition.Title.Get(database.UseEnglish));
            panel.Add(new Label($"등용 대상 · {candidate.DisplayName.Get(database.UseEnglish)}"));
            panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            for (int index = 0; index < definition.Choices.Count; index += 1)
            {
                int selectedIndex = index;
                WIRecruitmentEventChoiceDefinition choice = definition.Choices[index];
                Button button = new Button(() => ResolveRecruitmentEventChoice(pendingEvent, selectedIndex));
                button.text = $"{choice.Label.Get(database.UseEnglish)} · {GetRecruitmentChoiceCondition(choice)} · {choice.ResultDescription.Get(database.UseEnglish)}";
                button.SetEnabled(WIAdministrationTurnSystem.CanChooseRecruitmentEventOption(database, state, pendingEvent, choice));
                panel.Add(button);
            }
        }

        // 등용 요구 선택 결과를 적용하고 화면을 갱신합니다.
        private void ResolveRecruitmentEventChoice(WIPendingRecruitmentEvent pendingEvent, int choiceIndex)
        {
            if (WIAdministrationTurnSystem.ResolveRecruitmentEvent(
                    database, state, pendingEvent, choiceIndex, state.LastMonthlyReport))
            {
                CloseModal();
                RefreshAll();
            }
        }

        // 등용 사건 선택지의 잠금 조건을 짧은 문장으로 반환합니다.
        private string GetRecruitmentChoiceCondition(WIRecruitmentEventChoiceDefinition choice)
        {
            List<string> conditions = new List<string>();
            if (choice.RequiredReputation > 0) conditions.Add($"명성 {choice.RequiredReputation}");
            if (choice.RequiredMerit > 0) conditions.Add($"공적 {choice.RequiredMerit}");
            if (choice.RequiredFactionCastleCount > 0) conditions.Add($"영토 {choice.RequiredFactionCastleCount}성");
            if (choice.RequiresNegotiator) conditions.Add("교섭가 특기");
            return conditions.Count == 0 ? "조건 없음" : string.Join(" · ", conditions);
        }

        // 관계 사건의 인물과 데이터 기반 선택지·예상 결과를 표시합니다.
        private void OpenRelationshipEventModal(WIPendingRelationshipEvent pendingEvent)
        {
            WIRelationshipEventDefinition definition = database.GetRelationshipEvent(pendingEvent.EventId);
            if (definition == null) return;
            WIHeroDefinition first = database.GetHero(pendingEvent.FirstHeroId);
            WIHeroDefinition second = database.GetHero(pendingEvent.SecondHeroId);
            VisualElement panel = CreateModal(definition.Title.Get(database.UseEnglish));
            panel.Add(new Label($"{first.DisplayName.Get(database.UseEnglish)} · {second.DisplayName.Get(database.UseEnglish)}"));
            panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            for (int index = 0; index < definition.Choices.Count; index += 1)
            {
                int selectedIndex = index;
                WIRelationshipEventChoiceDefinition choice = definition.Choices[index];
                Button choiceButton = new Button(() => ResolveRelationshipEventChoice(pendingEvent, selectedIndex));
                choiceButton.text = $"{choice.Label.Get(database.UseEnglish)} · {choice.ResultDescription.Get(database.UseEnglish)}";
                panel.Add(choiceButton);
            }
        }

        // 선택한 관계 사건 결과를 적용하고 화면을 갱신합니다.
        private void ResolveRelationshipEventChoice(WIPendingRelationshipEvent pendingEvent, int choiceIndex)
        {
            if (WIAdministrationTurnSystem.ResolveRelationshipEvent(
                    database, state, pendingEvent, choiceIndex, state.LastMonthlyReport))
            {
                CloseModal();
                RefreshAll();
            }
        }

        // 사업에서 발생한 사건의 안전, 과감, 영웅 선택지를 표시합니다.
        private void OpenProjectEventModal(WIPendingProjectEvent pendingEvent)
        {
            VisualElement panel = CreateModal(pendingEvent.Title);
            Button safe = new Button(() => ResolveProjectEvent(pendingEvent, 2, false));
            safe.text = "안전한 선택 · 관련 성 수치 +2";
            panel.Add(safe);
            Button bold = new Button(() => ResolveProjectEvent(pendingEvent, 4, true));
            bold.text = "과감한 선택 · 관련 성 수치 +4 / 치안 -2";
            panel.Add(bold);
            if (pendingEvent.HeroChoiceAvailable)
            {
                WIHeroDefinition hero = database.GetHero(pendingEvent.HeroId);
                Button heroChoice = new Button(() => ResolveProjectEvent(pendingEvent, 5, false));
                heroChoice.text = $"영웅 선택지 · {hero.DisplayName.Get(database.UseEnglish)}의 특기로 +5";
                panel.Add(heroChoice);
            }
        }

        // 사건 선택 결과를 성 수치에 적용하고 대기 목록에서 제거합니다.
        private void ResolveProjectEvent(WIPendingProjectEvent pendingEvent, int gain, bool boldRisk)
        {
            WICastleRuntimeState castle = state.GetCastle(pendingEvent.CastleId);
            ApplyEventStat(castle, pendingEvent.ProjectType, gain);
            if (pendingEvent.ProjectType == WICastleProjectType.Recruitment)
            {
                state.GetCharacter(pendingEvent.HeroId).Reputation += gain;
            }
            if (boldRisk)
            {
                castle.Stability = Mathf.Clamp(castle.Stability - 2, 0, 100);
            }

            state.PendingProjectEvents.Remove(pendingEvent);
            CloseModal();
            RefreshAll();
        }

        // 사건의 사업 종류에 맞는 성 수치를 증가시킵니다.
        private void ApplyEventStat(WICastleRuntimeState castle, WICastleProjectType projectType, int gain)
        {
            if (projectType == WICastleProjectType.Prosperity) castle.Prosperity = Mathf.Clamp(castle.Prosperity + gain, 0, 100);
            else if (projectType == WICastleProjectType.Technology) castle.Technology = Mathf.Clamp(castle.Technology + gain, 0, 100);
            else if (projectType == WICastleProjectType.Stability) castle.Stability = Mathf.Clamp(castle.Stability + gain, 0, 100);
            else if (projectType == WICastleProjectType.Fortification) castle.Defense = Mathf.Clamp(castle.Defense + gain, 0, 100);
        }

        // 대성공으로 생성된 영웅의 흔적을 성에 기록하거나 교체 대상을 선택합니다.
        private void ConfirmLegacyChoice(WIPendingLegacyChoice pendingChoice)
        {
            WICastleRuntimeState castle = state.GetCastle(pendingChoice.CastleId);
            if (castle.HeroLegacies.Count < 2)
            {
                WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pendingChoice);
                CloseModal();
                RefreshAll();
                return;
            }

            VisualElement panel = CreateModal("교체할 영웅의 흔적 선택");
            panel.Add(new Label($"새 흔적 · {pendingChoice.Legacy.DisplayName} · {GetProjectDisplayName(pendingChoice.Legacy.ProjectType)} +{pendingChoice.Legacy.Bonus}"));
            panel.Add(new Label(pendingChoice.Legacy.Description));
            foreach (WIHeroLegacyState oldLegacy in new List<WIHeroLegacyState>(castle.HeroLegacies))
            {
                Button replace = new Button(() =>
                {
                    WIAdministrationTurnSystem.InstallHeroLegacy(state, castle, pendingChoice, oldLegacy);
                    CloseModal();
                    RefreshAll();
                });
                replace.text = $"교체 · {oldLegacy.DisplayName} · {GetProjectDisplayName(oldLegacy.ProjectType)} +{oldLegacy.Bonus} → 기념 기록";
                panel.Add(replace);
            }
        }

        // 세력 방침의 UI 표시명을 반환합니다.
        private string GetFactionPolicyDisplayName(WIFactionPolicy policy)
        {
            string[] names = { "부국", "개발", "안정", "수비", "원정", "인재" };
            return names[(int)policy];
        }

        // 세력 방침의 핵심 효과 설명을 반환합니다.
        private string GetFactionPolicyDescription(WIFactionPolicy policy)
        {
            string[] descriptions =
            {
                "번영 사업 강화", "기술 사업 강화", "치안 사업 강화",
                "방어·회복 강화", "훈련과 원정 준비 강화", "탐색·등용 강화"
            };
            return descriptions[(int)policy];
        }

        // 태수 운영 방침의 UI 표시명을 반환합니다.
        private string GetGovernorPolicyDisplayName(WIGovernorPolicy policy)
        {
            string[] names = { "균형", "번영", "연구", "전선", "인재" };
            return names[(int)policy];
        }

        // 주둔 인물 및 특화 시설 슬롯을 현재 성 상태에 맞게 갱신합니다.
        private void RefreshCastleSlots(WICastleDefinition castle, WICastleRuntimeState castleState)
        {
            heroSlots.Clear();
            for (int index = 0; index < castleState.GetHeroSlotCount(); index += 1)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList("slot");
                slot.AddToClassList("hero-slot");
                Label caption = new Label();
                caption.AddToClassList("slot-caption");
                if (index < castleState.HeroIds.Count)
                {
                    string heroId = castleState.HeroIds[index];
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    WIHeroDefinition hero = database.GetHero(heroId);
                    string activity = character.Activity == WICharacterActivityType.None ? "대기" : character.Activity.ToString();
                    VisualElement portrait = new VisualElement();
                    portrait.AddToClassList("slot-image");
                    portrait.AddToClassList("hero-slot-image");
                    ApplyBackgroundSprite(portrait, hero.Portrait);
                    slot.Add(portrait);
                    caption.text = $"{hero.DisplayName.Get(database.UseEnglish)}\n{activity}";
                }
                else
                {
                    slot.AddToClassList("empty-slot");
                    caption.text = database.GetText("UI_EMPTY_HERO");
                }
                slot.Add(caption);
                heroSlots.Add(slot);
            }

            specialFacilitySlots.Clear();
            int facilitySlotCount = castleState.GetSpecialFacilitySlotCount();
            for (int index = 0; index < facilitySlotCount; index += 1)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList("slot");
                slot.AddToClassList("facility-slot");
                Label caption = new Label();
                caption.AddToClassList("slot-caption");
                if (index < castleState.SpecialFacilityIds.Count)
                {
                    WISpecialFacilityDefinition facility = database.GetSpecialFacility(castleState.SpecialFacilityIds[index]);
                    VisualElement icon = new VisualElement();
                    icon.AddToClassList("slot-image");
                    icon.AddToClassList("facility-slot-image");
                    ApplyBackgroundSprite(icon, facility.Icon);
                    slot.Add(icon);
                    caption.text = facility.DisplayName.Get(database.UseEnglish);
                }
                else
                {
                    slot.AddToClassList("empty-slot");
                    caption.text = "확장 완료 시 선택";
                }
                slot.Add(caption);
                specialFacilitySlots.Add(slot);
            }
        }

        // Sprite가 있으면 UI 요소의 배경에 적용하고 없으면 빈 슬롯 상태를 유지합니다.
        private void ApplyBackgroundSprite(VisualElement element, Sprite sprite)
        {
            if (element == null || sprite == null)
            {
                return;
            }

            element.style.backgroundImage = new StyleBackground(sprite);
            Label placeholder = element.Q<Label>();
            if (placeholder != null)
            {
                placeholder.style.display = DisplayStyle.None;
            }
        }

        // 선택한 성의 전체 정보를 확인하는 상세 모달을 엽니다.
        private void OpenCastleRecordModal()
        {
            if (selectedCastle == null)
            {
                return;
            }

            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            VisualElement panel = CreateModal($"{castle.DisplayName.Get(database.UseEnglish)} 상세");
            bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, selectedCastle);
            if (detailed == false)
            {
                panel.Add(new Label("소유 세력과 지형 외 상세 정보가 확인되지 않았습니다."));
                panel.Add(new Label("계략 메뉴에서 조사를 성공시키면 일정 기간 성 수치·주둔·시설 정보를 볼 수 있습니다."));
                if (WIInformationVisibility.CanViewMilitaryDetails(state, state.PlayerFactionId, selectedCastle))
                {
                    panel.Add(new Label("현재 전투 접촉으로 전투 세션의 양측 전력만 확인할 수 있습니다."));
                }
                return;
            }
            panel.Add(new Label($"규모: {selectedCastle.CastleSize} / 지형: {castle.TerrainTrait.Get(database.UseEnglish)} / 특산: {castle.Specialty.Get(database.UseEnglish)}"));
            panel.Add(new Label($"전문 분야 효과 · {GetSpecialtyEffectDescription(castle)}"));
            panel.Add(new Label($"번영 {selectedCastle.Prosperity} · 기술 {selectedCastle.Technology} · 치안 {selectedCastle.Stability} · 방어 {selectedCastle.Defense}"));
            panel.Add(new Label($"주둔 인물 {selectedCastle.HeroIds.Count}/{selectedCastle.GetHeroSlotCount()} · 특화 시설 {selectedCastle.SpecialFacilityIds.Count}/{selectedCastle.GetSpecialFacilitySlotCount()}"));
            if (selectedCastle.OccupationUnrestMonths > 0)
            {
                panel.Add(new Label($"점령 불안 · {selectedCastle.OccupationUnrestMonths}개월 · 태수와 주둔 부대 필요"));
            }
            foreach (WIHeroLegacyState legacy in selectedCastle.HeroLegacies)
            {
                panel.Add(new Label($"영웅의 흔적 · {legacy.DisplayName} · {GetProjectDisplayName(legacy.ProjectType)} +{legacy.Bonus}"));
                if (string.IsNullOrEmpty(legacy.Description) == false) panel.Add(new Label(legacy.Description));
            }
            foreach (WIHeroLegacyState legacy in selectedCastle.CommemoratedHeroLegacies)
            {
                panel.Add(new Label($"기념 기록 · {legacy.DisplayName} · 효과 없음"));
            }
            foreach (WIArmyState army in state.Armies)
            {
                if (army.CurrentCastleId == selectedCastle.CastleId)
                {
                    panel.Add(new Label($"주둔 부대 · {army.DisplayName} · {army.Members.Count}명 · {army.Proficiency} · 보급 {army.Supply}"));
                }
            }
        }

        // 성 전문 분야의 실제 적용 효과를 UI 문장으로 반환합니다.
        private string GetSpecialtyEffectDescription(WICastleDefinition castle)
        {
            switch (castle.SpecialtyEffectType)
            {
                case WICastleSpecialtyEffectType.ProjectGain:
                    return $"{GetProjectDisplayName(castle.SpecialtyProjectType)} 성과 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.GoldIncome:
                    return $"월간 금화 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.ManaIncome:
                    return $"월간 마나 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.InfluenceIncome:
                    return $"월간 영향력 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.DefensePower:
                    return $"성 방어 전투력 +{castle.SpecialtyEffectValue}";
                default:
                    return "효과 없음";
            }
        }

        // 성 확장으로 획득한 슬롯에 배치할 특화 시설을 선택합니다.
        private void OpenSpecialFacilityModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            if (selectedCastle.PendingSpecialFacilityChoice == false)
            {
                ShowMessage("특화 시설은 성 확장 사업을 완료할 때 선택할 수 있습니다.");
                return;
            }

            List<WISpecialFacilityDefinition> candidates = new List<WISpecialFacilityDefinition>();
            foreach (WISpecialFacilityDefinition facility in database.SpecialFacilities)
            {
                if (selectedCastle.SpecialFacilityIds.Contains(facility.Id) == false)
                {
                    candidates.Add(facility);
                }
            }

            ShowChoiceModal("특화 시설 선택", candidates, facility =>
            {
                selectedCastle.SpecialFacilityIds.Add(facility.Id);
                selectedCastle.PendingSpecialFacilityChoice = false;
                CloseModal();
                SelectCastle(selectedCastle.CastleId);
            });
        }

        // 현재 세력의 부대 목록과 새 부대 편성 진입점을 표시합니다.
        private void OpenMilitaryModal()
        {
            WITutorialSystem.Complete(state, "tutorial_military");
            VisualElement panel = CreateModal("군사 · 부대 목록");
            foreach (WIBattleSessionState session in state.BattleSessions.FindAll(item =>
                         item.Status != WIBattleSessionStatus.Resolved && item.PlayerInvolved))
            {
                Button battleButton = new Button(() => OpenBattleSessionModal(session));
                battleButton.text = $"전투 세션 · {database.GetCastle(session.CastleId).DisplayName.Get(database.UseEnglish)} · {session.Status}";
                panel.Add(battleButton);
            }
            foreach (WIArmyState army in state.Armies)
            {
                if (army.FactionId != state.PlayerFactionId) continue;
                Button armyButton = new Button(() => OpenArmyDetailModal(army));
                string status = army.AwaitingBattle ? "전투 대기" : (army.IsMoving ? $"이동 {army.RemainingTravelMonths}개월" : "주둔");
                armyButton.text = $"{army.DisplayName} · {army.Members.Count}/{WIAdministrationTurnSystem.GetRecommendedArmySize(database, army)} · {army.Proficiency} · 보급 {army.Supply} · {status}";
                panel.Add(armyButton);
            }

            Button create = new Button(OpenArmyCastleModal);
            create.text = "새 부대 편성";
            panel.Add(create);
        }

        // 실시간 전투에 전달될 참가 세력과 전력 스냅샷을 표시합니다.
        private void OpenBattleSessionModal(WIBattleSessionState session)
        {
            VisualElement panel = CreateModal($"전투 세션 {session.SessionId}");
            panel.Add(new Label($"전장: {database.GetCastle(session.CastleId).DisplayName.Get(database.UseEnglish)}"));
            panel.Add(new Label($"공격 세력: {database.GetFaction(session.AttackerFactionId).DisplayName.Get(database.UseEnglish)} · 전력 {session.AttackerPowerSnapshot}"));
            panel.Add(new Label($"수비 세력: {database.GetFaction(session.DefenderFactionId).DisplayName.Get(database.UseEnglish)} · 전력 {session.DefenderPowerSnapshot}"));
            panel.Add(new Label($"수비 부대 {session.DefenderArmyIds.Count}개 · 플레이어 참가 {(session.PlayerInvolved ? "예" : "아니오")} · 상태 {session.Status}"));
            if (session.PlayerInvolved && session.Status == WIBattleSessionStatus.Pending)
            {
                Button startBattle = new Button(() =>
                {
                    WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
                    if (service == null || service.StartBattle(session.SessionId) == false)
                    {
                        ShowMessage("전투 씬을 시작할 수 없습니다.");
                    }
                });
                startBattle.text = "전투 시작";
                panel.Add(startBattle);
            }
        }

        // 새 부대를 편성할 플레이어 소유 성을 선택합니다.
        private void OpenArmyCastleModal()
        {
            VisualElement panel = CreateModal("부대 편성 성 선택");
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                WIFactionDefinition faction = database.GetFaction(castle.FactionId);
                if (faction == null || faction.PlayerFaction == false)
                {
                    continue;
                }

                Button button = new Button(() => OpenArmyCommanderModal(castle));
                button.text = database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish);
                panel.Add(button);
            }
        }

        // 선택한 성의 대기 인물 중 새 부대의 대장을 선택합니다.
        private void OpenArmyCommanderModal(WICastleRuntimeState castle)
        {
            VisualElement panel = CreateModal("부대 대장 선택");
            bool hasCandidate = false;
            foreach (string heroId in castle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId))
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition hero = database.GetHero(heroId);
                Button button = new Button(() =>
                {
                    WIArmyState army = WIAdministrationTurnSystem.CreateArmy(database, state, castle, heroId);
                    CloseModal();
                    if (army != null) OpenArmyDetailModal(army);
                });
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 통솔 {hero.Leadership}";
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 부대 구성원, 역할, 숙련, 보급과 현재 위치를 표시합니다.
        private void OpenArmyDetailModal(WIArmyState army)
        {
            VisualElement panel = CreateModal(army.DisplayName);
            string strategicTarget = string.IsNullOrEmpty(army.StrategicTargetCastleId)
                ? "없음"
                : database.GetCastle(army.StrategicTargetCastleId).DisplayName.Get(database.UseEnglish);
            panel.Add(new Label($"위치: {database.GetCastle(army.CurrentCastleId).DisplayName.Get(database.UseEnglish)} · 숙련 {army.Proficiency} · 보급 {army.Supply}"));
            panel.Add(new Label($"전략 임무: {army.Mission} · 목표: {strategicTarget}"));
            if (army.ReorganizationMonths > 0)
            {
                panel.Add(new Label($"재편성 중 · 남은 기간 {army.ReorganizationMonths}개월"));
            }
            if (army.LastBattleOutcome != WIBattleOutcome.None)
            {
                panel.Add(new Label($"최근 전투: {army.LastBattleOutcome} · 전투력 {army.LastBattlePower}"));
            }
            foreach (WIArmyMemberState member in army.Members)
            {
                WIHeroDefinition hero = database.GetHero(member.HeroId);
                if (member.Role == WIUnitRole.Commander || army.IsMoving || army.AwaitingBattle)
                {
                    panel.Add(new Label($"{GetUnitRoleDisplayName(member.Role)} · {hero.DisplayName.Get(database.UseEnglish)}"));
                }
                else
                {
                    Button remove = new Button(() =>
                    {
                        WIAdministrationTurnSystem.RemoveArmyMember(state, army, member.HeroId);
                        CloseModal();
                        OpenArmyDetailModal(army);
                    });
                    remove.text = $"{GetUnitRoleDisplayName(member.Role)} · {hero.DisplayName.Get(database.UseEnglish)} · 부대에서 제외";
                    panel.Add(remove);
                }
            }

            if (army.IsMoving == false && army.AwaitingBattle == false)
            {
                Button addMember = new Button(() => OpenArmyRoleModal(army));
                addMember.text = "부대원 추가";
                panel.Add(addMember);
                Button march = new Button(() => OpenArmyTargetModal(army));
                march.text = "이동 / 출정";
                panel.Add(march);
                Button training = new Button(() =>
                {
                    WIAdministrationTurnSystem.ScheduleJointTraining(army);
                    CloseModal();
                    OpenArmyDetailModal(army);
                });
                training.text = army.JointTrainingScheduled ? "합동 훈련 예약됨" : "다음 달 합동 훈련";
                training.SetEnabled(army.JointTrainingScheduled == false);
                panel.Add(training);
                Button disband = new Button(() =>
                {
                    WIAdministrationTurnSystem.DisbandArmy(state, army);
                    CloseModal();
                    OpenMilitaryModal();
                });
                disband.text = "부대 해산";
                panel.Add(disband);
            }
            else if (army.AwaitingBattle)
            {
                panel.Add(new Label("적 성 외곽에서 전투 대기 중입니다. 4단계 전투 결과가 승리로 전달되면 점령 절차가 시작됩니다."));
            }
        }

        // 새 부대원에게 부여할 전투 역할을 선택합니다.
        private void OpenArmyRoleModal(WIArmyState army)
        {
            VisualElement panel = CreateModal("추가할 역할 선택");
            foreach (WIUnitRole role in System.Enum.GetValues(typeof(WIUnitRole)))
            {
                if (role == WIUnitRole.Commander)
                {
                    continue;
                }

                Button button = new Button(() => OpenArmyMemberModal(army, role));
                button.text = GetUnitRoleDisplayName(role);
                panel.Add(button);
            }
        }

        // 부대와 같은 성에 있는 대기 인물을 역할에 배치합니다.
        private void OpenArmyMemberModal(WIArmyState army, WIUnitRole role)
        {
            WICastleRuntimeState castle = state.GetCastle(army.CurrentCastleId);
            VisualElement panel = CreateModal($"{GetUnitRoleDisplayName(role)} 인물 선택");
            bool hasCandidate = false;
            foreach (string heroId in castle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId))
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition hero = database.GetHero(heroId);
                Button button = new Button(() =>
                {
                    WIAdministrationTurnSystem.AddArmyMember(database, state, army, heroId, role);
                    CloseModal();
                    OpenArmyDetailModal(army);
                });
                button.text = hero.DisplayName.Get(database.UseEnglish);
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 부대가 이동하거나 공격할 인접 성을 선택합니다.
        private void OpenArmyTargetModal(WIArmyState army)
        {
            WICastleDefinition origin = database.GetCastle(army.CurrentCastleId);
            VisualElement panel = CreateModal("이동 / 출정 목표");
            foreach (string targetId in origin.AdjacentCastleIds)
            {
                WICastleRuntimeState target = state.GetCastle(targetId);
                string action = target.FactionId == army.FactionId ? "이동" : "출정";
                Button button = new Button(() =>
                {
                    if (WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, targetId) == false)
                    {
                        ShowMessage(GetArmyMarchFailureMessage(army, target));
                        return;
                    }
                    CloseModal();
                    RefreshAll();
                });
                string cost = target.FactionId == army.FactionId ? string.Empty : " · 영향력 20";
                button.text = $"{action} · {database.GetCastle(targetId).DisplayName.Get(database.UseEnglish)}{cost}";
                panel.Add(button);
            }
        }

        // 출정 시작이 거부된 경우 현재 상태에서 가장 직접적인 해결 방법을 안내합니다.
        private string GetArmyMarchFailureMessage(WIArmyState army, WICastleRuntimeState target)
        {
            if (army == null || target == null) return "출정 정보를 확인할 수 없습니다.";
            if (army.IsMoving) return "이미 이동 중인 부대입니다.";
            if (army.AwaitingBattle) return "현재 전투 결과를 기다리는 부대입니다.";
            if (army.ReorganizationMonths > 0) return $"재편성 완료까지 {army.ReorganizationMonths}개월 남았습니다.";
            if (target.FactionId != army.FactionId)
            {
                if (WIAdministrationTurnSystem.AreFactionsAtWar(state, army.FactionId, target.FactionId) == false)
                    return "교전 중인 세력의 성에만 출정할 수 있습니다. 먼저 외교 관계를 확인하십시오.";
                WIFactionRuntimeState faction = state.GetFactionState(army.FactionId);
                if (faction == null || faction.Influence < 20)
                    return "출정에 필요한 영향력 20이 부족합니다.";
            }
            return "현재 경로로 출정할 수 없습니다. 인접 성과 부대 상태를 확인하십시오.";
        }

        // 선택한 성에 주둔한 부대 중 출정할 부대를 선택합니다.
        private void OpenCastleMarchModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("출정 부대 선택");
            bool found = false;
            foreach (WIArmyState army in state.Armies)
            {
                if (army.CurrentCastleId != selectedCastle.CastleId || army.IsMoving || army.AwaitingBattle)
                {
                    continue;
                }

                found = true;
                Button button = new Button(() => OpenArmyTargetModal(army));
                button.text = $"{army.DisplayName} · {army.Members.Count}명 · {army.Proficiency}";
                panel.Add(button);
            }

            if (found == false)
            {
                panel.Add(new Label("주둔 영웅은 먼저 부대로 편성해야 출정할 수 있습니다."));
                Button createArmy = new Button(() => OpenArmyCommanderModal(selectedCastle));
                createArmy.text = "이 성의 영웅으로 새 부대 편성";
                createArmy.tooltip = "대장을 선택한 뒤 부대원을 추가하고 이동 / 출정을 선택합니다.";
                panel.Add(createArmy);
            }
        }

        // 부대 역할의 한국어 표시명을 반환합니다.
        private string GetUnitRoleDisplayName(WIUnitRole role)
        {
            string[] names = { "대장", "전위", "근접", "원거리", "마법", "지원" };
            return names[(int)role];
        }

        // 모든 성이 기본으로 보유한 시설과 역할을 안내합니다.
        private void OpenBasicFacilityModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("기본 시설");
            panel.Add(new Label("성관 · 태수 임명과 성 운영"));
            panel.Add(new Label("시장 · 기본 거래와 영지 수입"));
            panel.Add(new Label("훈련소 · 개인 및 합동 훈련"));
            panel.Add(new Label("선술집 · 소문, 인재 단서와 월간 의뢰"));
            panel.Add(new Label("기본 시설은 건설하거나 강화하지 않습니다."));
            Button tavernQuests = new Button(OpenTavernQuestModal);
            tavernQuests.text = "선술집 월간 의뢰 확인";
            panel.Add(tavernQuests);
        }

        // 현재 성의 선술집에 게시된 월간 의뢰를 표시합니다.
        private void OpenTavernQuestModal()
        {
            VisualElement panel = CreateModal("선술집 월간 의뢰");
            if (selectedCastle.TavernQuests.Count == 0)
            {
                panel.Add(new Label("다음 달부터 새로운 의뢰가 게시됩니다."));
                return;
            }

            foreach (WITavernQuestState quest in selectedCastle.TavernQuests)
            {
                WITavernQuestDefinition definition = database.GetTavernQuest(quest.QuestType);
                if (definition == null) continue;
                Button button = new Button(() => OpenQuestHeroModal(quest));
                button.text = quest.Status == WIQuestStatus.Accepted
                    ? $"진행 중 · {definition.DisplayName.Get(database.UseEnglish)} · {quest.RemainingMonths}개월"
                    : $"{definition.DisplayName.Get(database.UseEnglish)} · {definition.DurationMonths}개월 · 금화 {definition.GoldReward} · 공적 {definition.MeritReward} · 명성 {definition.ReputationReward}";
                button.SetEnabled(quest.Status == WIQuestStatus.Available);
                panel.Add(button);
                panel.Add(new Label(definition.Description.Get(database.UseEnglish)));
            }
        }

        // 선술집 의뢰를 맡길 수 있는 주둔 인물을 선택합니다.
        private void OpenQuestHeroModal(WITavernQuestState quest)
        {
            VisualElement panel = CreateModal("의뢰 담당 인물 선택");
            WITavernQuestDefinition definition = database.GetTavernQuest(quest.QuestType);
            if (definition == null) return;
            panel.Add(new Label($"권장 적성 · {definition.Aptitude} {definition.AptitudeThreshold} 이상 · 달성 시 금화 +{definition.AptitudeBonusGold}"));
            bool hasCandidate = false;
            foreach (string heroId in selectedCastle.HeroIds)
            {
                if (state.IsCharacterBusy(heroId))
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition hero = database.GetHero(heroId);
                Button button = new Button(() =>
                {
                    quest.AssignedHeroId = heroId;
                    quest.Status = WIQuestStatus.Accepted;
                    quest.RemainingMonths = definition.DurationMonths;
                    CloseModal();
                    SelectCastle(selectedCastle.CastleId);
                });
                int aptitude = WIAdministrationTurnSystem.GetQuestAptitude(hero, definition.Aptitude);
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · {definition.Aptitude} {aptitude}{(aptitude >= definition.AptitudeThreshold ? " · 적합" : string.Empty)}";
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 현재 성에서 이번 달 개인 활동을 수행할 인물을 선택합니다.
        private void OpenCharacterActivityModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            VisualElement panel = CreateModal("인재 활동 · 인물 선택");
            bool hasCandidate = false;
            foreach (string heroId in selectedCastle.HeroIds)
            {
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                WIHeroDefinition hero = database.GetHero(heroId);
                if (character == null || hero == null || state.IsCharacterBusy(heroId) || character.InjuryMonths > 0)
                {
                    continue;
                }

                hasCandidate = true;
                Button button = new Button(() => OpenActivityTypeModal(character));
                button.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 피로 {character.Fatigue}";
                panel.Add(button);
                Button moveButton = new Button(() => OpenCharacterTransferModal(character));
                moveButton.text = $"{hero.DisplayName.Get(database.UseEnglish)} · 인접 성 이동";
                panel.Add(moveButton);
            }

            if (hasCandidate == false)
            {
                AddNoIdleCharacterGuidance(panel);
            }
        }

        // 선택 인물이 이동할 수 있는 같은 세력의 인접 성 목록을 표시합니다.
        private void OpenCharacterTransferModal(WICharacterRuntimeState character)
        {
            WICastleDefinition origin = database.GetCastle(selectedCastle.CastleId);
            VisualElement panel = CreateModal("인물 이동");
            bool hasDestination = false;
            foreach (string targetId in origin.AdjacentCastleIds)
            {
                WICastleRuntimeState target = state.GetCastle(targetId);
                if (target == null || target.FactionId != selectedCastle.FactionId)
                {
                    continue;
                }

                hasDestination = true;
                WICastleDefinition targetDefinition = database.GetCastle(targetId);
                Button button = new Button(() =>
                {
                    bool started = WIAdministrationTurnSystem.StartCharacterTransfer(
                        database, state, character.HeroId, targetId);
                    if (started == false)
                    {
                        ShowMessage("이동할 수 없습니다. 임무, 인접 경로와 목적지 슬롯을 확인하십시오.");
                        return;
                    }

                    CloseModal();
                    SelectCastle(selectedCastle.CastleId);
                    ShowMessage($"{targetDefinition.DisplayName.Get(database.UseEnglish)} 이동을 시작했습니다. 다음 달에 도착합니다.");
                });
                button.text = $"{targetDefinition.DisplayName.Get(database.UseEnglish)} · 인물 {target.HeroIds.Count}/{target.GetHeroSlotCount()}";
                panel.Add(button);
            }

            if (hasDestination == false)
            {
                panel.Add(new Label("이동 가능한 같은 세력의 인접 성이 없습니다."));
            }
        }

        // 선택한 인물에게 배정할 개인 활동 종류를 표시합니다.
        private void OpenActivityTypeModal(WICharacterRuntimeState character)
        {
            WIHeroDefinition hero = database.GetHero(character.HeroId);
            VisualElement panel = CreateModal($"{hero.DisplayName.Get(database.UseEnglish)} · 개인 활동");
            AddActivityButton(panel, character, WICharacterActivityType.Search, "탐색 · 재야 인재와 단서 발견");
            AddActivityButton(panel, character, WICharacterActivityType.Training, "훈련 · 경험과 공적 획득");
            AddActivityButton(panel, character, WICharacterActivityType.Rest, "휴식 · 피로와 부상 회복");

            Button socialize = new Button(() => OpenSocializeTargetModal(character));
            socialize.text = "교류 · 주둔 인물과 관계 개선";
            panel.Add(socialize);
            Button recruit = new Button(() => OpenRecruitTargetModal(character));
            recruit.text = "등용 · 발견한 인재 설득";
            panel.Add(recruit);
        }

        // 대상이 필요 없는 개인 활동 버튼을 만들어 모달에 추가합니다.
        private void AddActivityButton(
            VisualElement panel,
            WICharacterRuntimeState character,
            WICharacterActivityType activity,
            string label)
        {
            Button button = new Button(() => AssignCharacterActivity(character, activity, string.Empty));
            button.text = label;
            panel.Add(button);
        }

        // 교류할 같은 성의 주둔 인물을 선택합니다.
        private void OpenSocializeTargetModal(WICharacterRuntimeState character)
        {
            VisualElement panel = CreateModal("교류 대상 선택");
            foreach (string targetId in selectedCastle.HeroIds)
            {
                if (targetId == character.HeroId)
                {
                    continue;
                }

                WIHeroDefinition targetHero = database.GetHero(targetId);
                Button button = new Button(() => AssignCharacterActivity(character, WICharacterActivityType.Socialize, targetId));
                button.text = targetHero.DisplayName.Get(database.UseEnglish);
                panel.Add(button);
            }
        }

        // 발견했지만 아직 영입하지 않은 인재 중 등용 대상을 선택합니다.
        private void OpenRecruitTargetModal(WICharacterRuntimeState character)
        {
            VisualElement panel = CreateModal("등용 대상 선택");
            bool hasCandidate = false;
            foreach (WICharacterRuntimeState candidate in state.Characters)
            {
                if (candidate.Discovered == false || candidate.Recruited)
                {
                    continue;
                }

                hasCandidate = true;
                WIHeroDefinition targetHero = database.GetHero(candidate.HeroId);
                Button button = new Button(() => AssignCharacterActivity(character, WICharacterActivityType.Recruit, candidate.HeroId));
                string request = targetHero.RecruitmentRequest == null ? string.Empty : targetHero.RecruitmentRequest.Get(database.UseEnglish);
                button.text = $"{targetHero.DisplayName.Get(database.UseEnglish)} · 설득 {candidate.RecruitmentProgress}% · 필요 명성 {targetHero.RequiredReputation} · {request}";
                button.SetEnabled(character.Reputation >= targetHero.RequiredReputation);
                panel.Add(button);
            }

            if (hasCandidate == false)
            {
                panel.Add(new Label("먼저 탐색이나 인재 사업으로 인재를 발견해야 합니다."));
            }
        }

        // 선택한 인물에게 이번 달 개인 활동과 대상을 배정합니다.
        private void AssignCharacterActivity(
            WICharacterRuntimeState character,
            WICharacterActivityType activity,
            string targetHeroId)
        {
            character.Activity = activity;
            character.ActivityTargetHeroId = targetHeroId;
            CloseModal();
            SelectCastle(selectedCastle.CastleId);
        }

        // 인물 등급의 UI 표시명을 반환합니다.
        private string GetGradeDisplayName(WICharacterGrade grade)
        {
            return grade == WICharacterGrade.Hero ? "영웅" : "일반";
        }

        // 인물과 연결된 주요 시작 관계를 상대 이름과 단계 문구로 요약합니다.
        private string GetRelationshipSummary(string heroId)
        {
            List<string> entries = new List<string>();
            foreach (WIRelationshipState relationship in state.Relationships)
            {
                string counterpartId = relationship.FirstHeroId == heroId
                    ? relationship.SecondHeroId
                    : relationship.SecondHeroId == heroId ? relationship.FirstHeroId : string.Empty;
                if (string.IsNullOrEmpty(counterpartId)) continue;
                WIHeroDefinition counterpart = database.GetHero(counterpartId);
                if (counterpart == null) continue;
                string level = relationship.Level == WIRelationshipLevel.Conflict ? "갈등"
                    : relationship.Level == WIRelationshipLevel.Fondness ? "친애" : "보통";
                entries.Add($"{counterpart.DisplayName.Get(database.UseEnglish)}({level})");
            }

            return entries.Count == 0 ? string.Empty : $" · 관계 {string.Join(", ", entries)}";
        }

        // 미배치 영웅을 현재 성에 배치하는 모달을 엽니다.
        private void OpenHeroAssignmentModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            if (selectedCastle.HeroIds.Count >= selectedCastle.GetHeroSlotCount())
            {
                ShowMessage("현재 성 규모의 주둔 인물 슬롯이 가득 찼습니다.");
                return;
            }

            List<WIHeroDefinition> candidates = new List<WIHeroDefinition>();
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || state.IsCharacterBusy(character.HeroId))
                {
                    continue;
                }

                string heroId = character.HeroId;
                bool assigned = state.Castles.Exists(item => item.HeroIds.Contains(heroId));
                if (assigned == false)
                {
                    candidates.Add(database.GetHero(heroId));
                }
            }

            if (candidates.Count == 0)
            {
                ShowMessage("배치 가능한 대기 인물이 없습니다. 진행 중인 임무가 끝나거나 부대를 해산한 뒤 다시 시도하십시오.");
                return;
            }

            ShowChoiceModal(database.GetText("UI_ASSIGN_HERO"), candidates, hero =>
            {
                selectedCastle.HeroIds.Add(hero.Id);
                CloseModal();
                SelectCastle(selectedCastle.CastleId);
            });
        }

        // 영입된 전체 영웅 목록을 표시합니다.
        private void OpenHeroListModal()
        {
            VisualElement panel = CreateModal(database.GetText("UI_ALL_HEROES"));
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false)
                {
                    continue;
                }

                WIHeroDefinition hero = database.GetHero(character.HeroId);
                string activity = character.Activity == WICharacterActivityType.None ? "대기" : character.Activity.ToString();
                if (character.IsDead) activity = "사망";
                else if (character.Captured) activity = $"포로 · {database.GetFaction(character.CaptorFactionId)?.DisplayName.Get(database.UseEnglish)} · {character.CapturedMonthsRemaining}개월";
                WICharacterTransferState transfer = state.CharacterTransfers.Find(item => item.HeroId == character.HeroId);
                if (transfer != null)
                {
                    activity = $"이동 중 · {database.GetCastle(transfer.TargetCastleId).DisplayName.Get(database.UseEnglish)} · {transfer.RemainingMonths}개월";
                }
                WICharacterGrade effectiveGrade = character.PromotedToHero ? WICharacterGrade.Hero : character.BaseGrade;
                WIHeroClassDefinition classDefinition = database.GetHeroClass(hero.HeroClass);
                string classTendency = classDefinition == null
                    ? hero.HeroClass.ToString()
                    : $"{classDefinition.DisplayName.Get(database.UseEnglish)} · 주 {classDefinition.PrimaryStat} / 보조 {classDefinition.SecondaryStat} · 권장 {GetUnitRoleDisplayName(classDefinition.RecommendedRole)}";
                string relationships = GetRelationshipSummary(hero.Id);
                panel.Add(new Label($"[{GetGradeDisplayName(effectiveGrade)}] {hero.DisplayName.Get(database.UseEnglish)} · {classTendency} · 공적 {character.Merit} · 명성 {character.Reputation} · {character.LoyaltyState} · {activity}{relationships}"));
                panel.Add(new Label($"특기: {GetTraitDisplayText(hero)} · 피로 {character.Fatigue} · 부상 {character.InjuryMonths}개월"));
                if (state.PendingHeroPromotionIds.Contains(character.HeroId))
                {
                    Button promotionButton = new Button(() =>
                    {
                        bool promoted = WIAdministrationTurnSystem.PromoteCommonCharacter(database, state, character.HeroId);
                        CloseModal();
                        RefreshAll();
                        ShowMessage(promoted ? "영웅 승격을 확정했습니다." : "승격에 필요한 영향력이 부족합니다.");
                    });
                    promotionButton.text = $"영웅 승격 · 영향력 {database.PromotionInfluenceCost}";
                    panel.Add(promotionButton);
                }
                Button titleButton = new Button(() => OpenTitleModal(character));
                WITitleDefinition currentTitle = database.GetTitle(character.TitleId);
                titleButton.text = currentTitle == null
                    ? "작위 수여"
                    : $"현재 작위: {currentTitle.DisplayName.Get(database.UseEnglish)} · 변경";
                panel.Add(titleButton);
            }

            foreach (WICharacterRuntimeState candidate in state.Characters)
            {
                if (candidate.Discovered && candidate.Recruited == false)
                {
                    WIHeroDefinition hero = database.GetHero(candidate.HeroId);
                    panel.Add(new Label($"[발견 인재] {hero.DisplayName.Get(database.UseEnglish)} · 등용 설득 {candidate.RecruitmentProgress}%"));
                }
            }
        }

        // 인물의 공적과 세력 영향력으로 수여할 수 있는 작위를 표시합니다.
        private void OpenTitleModal(WICharacterRuntimeState character)
        {
            WIHeroDefinition hero = database.GetHero(character.HeroId);
            VisualElement panel = CreateModal($"작위 수여 · {hero.DisplayName.Get(database.UseEnglish)} · 공적 {character.Merit}");
            foreach (WITitleDefinition title in database.TitleDefinitions)
            {
                Button button = new Button(() =>
                {
                    bool awarded = WIAdministrationTurnSystem.AwardTitle(
                        database, state, state.PlayerFactionId, character.HeroId, title.Id);
                    CloseModal();
                    RefreshAll();
                    ShowMessage(awarded ? "작위를 수여했습니다." : "공적 또는 영향력이 부족합니다.");
                });
                button.text = $"{title.DisplayName.Get(database.UseEnglish)} · 공적 {title.RequiredMerit} · 영향력 {title.InfluenceCost} · 내정 +{title.ProjectBonus} · 전투 +{title.BattlePowerBonus}";
                panel.Add(button);
            }
        }

        // 인물이 가진 특기 목록을 한국어 UI 문자열로 조합합니다.
        private string GetTraitDisplayText(WIHeroDefinition hero)
        {
            List<string> names = new List<string>();
            foreach (WITraitType trait in hero.Traits)
            {
                WITraitDefinition definition = database.GetTrait(trait);
                if (definition != null)
                {
                    names.Add($"{definition.DisplayName.Get(database.UseEnglish)} · {definition.Description.Get(database.UseEnglish)}");
                }
            }

            return names.Count == 0 ? "없음" : string.Join(", ", names);
        }

        // 턴 연산 오버레이를 표시한 뒤 결과 요약을 엽니다.
        private void BeginTurn()
        {
            if (WIAdministrationTurnSystem.HasUnresolvedPlayerBattles(state))
            {
                OpenMonthlyReportModal();
                return;
            }
            StartCoroutine(ExecuteTurnRoutine());
        }

        // 한 프레임 동안 연산 안내를 노출하고 턴 결과를 UI에 반영합니다.
        private IEnumerator ExecuteTurnRoutine()
        {
            VisualElement panel = CreateModal(database.GetText("UI_TURN_PROCESSING"));
            panel.Add(new Label($"TURN {state.Turn}"));
            yield return null;
            WITurnSummary summary = WIAdministrationTurnSystem.ExecuteTurn(database, state);
            WICampaignRuntimeService.Instance?.AutoSave();
            CloseModal();
            RefreshAll();
            ShowTurnSummary(summary);
        }

        // 턴 결과의 자원과 소식을 요약 모달로 표시합니다.
        private void ShowTurnSummary(WITurnSummary summary)
        {
            VisualElement panel = CreateModalWithFooter(database.GetText("UI_TURN_SUMMARY"), out VisualElement battleFooter);
            panel.Add(new Label($"Gold +{summary.GoldGained}"));
            panel.Add(new Label($"Gold Spent -{summary.GoldSpent}"));
            panel.Add(new Label($"Mana +{summary.ManaGained}"));
            panel.Add(new Label($"Influence +{summary.InfluenceGained}"));
            foreach (string report in summary.DelegationReports)
            {
                panel.Add(new Label(report));
            }
            if (summary.AIReasonReports != null && summary.AIReasonReports.Count > 0) panel.Add(new Label("AI 세력 판단 근거"));
            foreach (string aiReport in summary.AIReasonReports ?? new List<string>())
            {
                panel.Add(new Label(aiReport));
            }
            foreach (string news in summary.News)
            {
                panel.Add(new Label(news));
            }
            AddPendingBattleActions(battleFooter);
            if (state.CampaignResult != WICampaignResult.Ongoing && state.CampaignResultAcknowledged == false)
            {
                AddCampaignResultContent(panel);
            }
            else
            {
                AddPendingTutorial(panel);
            }
        }

        // 턴 결과에서 플레이어가 참가할 대기 전투를 놓치지 않도록 즉시 진입 버튼을 표시합니다.
        private void AddPendingBattleActions(VisualElement panel)
        {
            List<WIBattleSessionState> pendingBattles = state.BattleSessions.FindAll(item =>
                item.PlayerInvolved && item.Status == WIBattleSessionStatus.Pending);
            panel.style.display = pendingBattles.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (pendingBattles.Count == 0) return;

            Label heading = new Label($"전투 발생 · {pendingBattles.Count}건");
            heading.AddToClassList("modal-section-heading");
            panel.Add(heading);
            foreach (WIBattleSessionState session in pendingBattles)
            {
                WICastleDefinition castle = database.GetCastle(session.CastleId);
                Button battleButton = new Button(() =>
                {
                    WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
                    if (service == null || service.StartBattle(session.SessionId) == false)
                    {
                        ShowMessage("전투를 시작할 수 없습니다. 군사 화면에서 전투 세션 상태를 확인하십시오.");
                    }
                });
                battleButton.text = $"전투 시작 · {castle?.DisplayName.Get(database.UseEnglish) ?? session.CastleId} · " +
                                    $"아군 {session.AttackerPowerSnapshot} / 적군 {session.DefenderPowerSnapshot}";
                battleButton.AddToClassList("danger");
                battleButton.tooltip = "실시간 전투 화면으로 이동합니다.";
                panel.Add(battleButton);
            }
        }

        // 저장에서 복원된 미확인 캠페인 결과를 독립 모달로 표시합니다.
        private bool ShowCampaignResult()
        {
            if (state.CampaignResult == WICampaignResult.Ongoing || state.CampaignResultAcknowledged)
            {
                return false;
            }
            WICampaignRuleDefinition rules = database.CampaignRules;
            WICampaignEndingDefinition ending = state.CampaignResult == WICampaignResult.Victory
                ? database.GetCampaignEnding(state.CampaignEnding) : null;
            string title = ending != null
                ? ending.Title.Get(database.UseEnglish)
                : state.CampaignResult == WICampaignResult.Victory
                    ? rules.VictoryTitle.Get(database.UseEnglish)
                    : rules.DefeatTitle.Get(database.UseEnglish);
            VisualElement panel = CreateModal(title);
            AddCampaignResultContent(panel);
            return true;
        }

        // 승리·패배 설명과 계속 보기·시작 화면 복귀 선택지를 구성합니다.
        private void AddCampaignResultContent(VisualElement panel)
        {
            WICampaignRuleDefinition rules = database.CampaignRules;
            WICampaignEndingDefinition ending = state.CampaignResult == WICampaignResult.Victory
                ? database.GetCampaignEnding(state.CampaignEnding) : null;
            string title = ending != null
                ? ending.Title.Get(database.UseEnglish)
                : state.CampaignResult == WICampaignResult.Victory
                    ? rules.VictoryTitle.Get(database.UseEnglish)
                    : rules.DefeatTitle.Get(database.UseEnglish);
            string description = ending != null
                ? ending.Description.Get(database.UseEnglish)
                : state.CampaignResult == WICampaignResult.Victory
                    ? rules.VictoryDescription.Get(database.UseEnglish)
                    : rules.DefeatDescription.Get(database.UseEnglish);
            panel.Add(new Label($"{title} · 판정 턴 {state.CampaignResultTurn}"));
            panel.Add(new Label(description));
            Button continueButton = new Button(() =>
            {
                state.CampaignResultAcknowledged = true;
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
            });
            continueButton.text = "계속 보기";
            panel.Add(continueButton);
            Button startButton = new Button(() =>
            {
                state.CampaignResultAcknowledged = true;
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
                campaignStartLayer.style.display = DisplayStyle.Flex;
            });
            startButton.text = "캠페인 시작 화면";
            panel.Add(startButton);
        }

        // 현재 월의 첫해 안내가 남아 있으면 독립 모달로 표시합니다.
        private void ShowCurrentTutorial()
        {
            WITutorialDefinition tutorial = WITutorialSystem.GetPending(database, state);
            if (tutorial == null)
            {
                return;
            }
            VisualElement panel = CreateModal(tutorial.Title.Get(database.UseEnglish));
            AddTutorialContent(panel, tutorial);
        }

        // 턴 결과 아래에 현재 월의 첫해 안내를 이어서 표시합니다.
        private void AddPendingTutorial(VisualElement panel)
        {
            WITutorialDefinition tutorial = WITutorialSystem.GetPending(database, state);
            if (tutorial == null)
            {
                return;
            }
            Label title = new Label(tutorial.Title.Get(database.UseEnglish));
            title.AddToClassList("modal-title");
            panel.Add(title);
            AddTutorialContent(panel, tutorial);
        }

        // 안내 본문과 확인·전체 건너뛰기 버튼을 구성합니다.
        private void AddTutorialContent(VisualElement panel, WITutorialDefinition tutorial)
        {
            panel.Add(new Label(tutorial.Description.Get(database.UseEnglish)));
            Button confirm = new Button(() =>
            {
                WITutorialSystem.Complete(state, tutorial.Id);
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
            });
            confirm.text = "안내 확인";
            panel.Add(confirm);
            Button skip = new Button(() =>
            {
                WITutorialSystem.SkipAll(state);
                WICampaignRuntimeService.Instance?.AutoSave();
                CloseModal();
            });
            skip.text = "첫해 안내 전체 건너뛰기";
            panel.Add(skip);
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
                : $"세력 방침: {GetFactionPolicyDisplayName(state.FactionPolicy)} · {state.Month:00}월 명령을 검토하십시오.";
            goldLabel.text = $"금화\n{FormatHudNumber(state.Gold, database.UseEnglish)} (+{FormatHudNumber(forecast.GoldGained, database.UseEnglish)})";
            manaLabel.text = $"마나\n{FormatHudNumber(state.ManaCrystal, database.UseEnglish)} (+{FormatHudNumber(forecast.ManaGained, database.UseEnglish)})";
            influenceLabel.text = $"영향력\n{FormatHudNumber(state.Influence, database.UseEnglish)} (+{FormatHudNumber(forecast.InfluenceGained, database.UseEnglish)})";
            int ownedCastleCount = state.Castles.Count(castle => castle.FactionId == state.PlayerFactionId);
            goldLabel.tooltip = BuildResourceCalculationTooltip("금화", state.Gold, forecast.GoldGained, ownedCastleCount,
                "성별 번영 × 성 규모 × 치안 효율 + 전문 분야·시설·연구");
            manaLabel.tooltip = BuildResourceCalculationTooltip("마나", state.ManaCrystal, forecast.ManaGained, ownedCastleCount,
                "성별 기술 × 성 규모 + 전문 분야·마법 시설·연구");
            influenceLabel.tooltip = BuildResourceCalculationTooltip("영향력", state.Influence, forecast.InfluenceGained, ownedCastleCount,
                "성별 치안 × 성 규모 + 전문 분야·연구");
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
                if (owner != null)
                {
                    pair.Value.style.backgroundColor = owner.Color;
                }
                bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, castle);
                bool military = WIInformationVisibility.CanViewMilitaryDetails(state, state.PlayerFactionId, castle);
                string eventBadge = detailed && castle.InvasionWarning ? " !" : string.Empty;
                string occupationBadge = detailed && castle.OccupationUnrestMonths > 0 ? " ⚑" : string.Empty;
                int armyCount = state.Armies.FindAll(army => army.CurrentCastleId == pair.Key).Count;
                string battleBadge = military && state.Armies.Exists(army => army.CurrentCastleId == pair.Key && army.AwaitingBattle) ? " ⚔" : string.Empty;
                string detailText = detailed ? $"{castle.HeroIds.Count}H · {armyCount}부대" : military ? "전투 접촉 · 전력 확인 가능" : "정보 미확보";
                string castleName = definition.DisplayName.Get(database.UseEnglish);
                string factionCode = GetFactionAccessibilityCode(castle.FactionId);
                pair.Value.text = $"{factionCode}·{TruncateLabel(castleName, 9)}\n{detailText}{eventBadge}{battleBadge}{occupationBadge}";
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
                globalCastleStats.text = $"번영 {castle.Prosperity}  ·  기술 {castle.Technology}\n치안 {castle.Stability}  ·  방어 {castle.Defense}";
                int armyCount = state.Armies.Count(army => army.CurrentCastleId == castle.CastleId);
                globalCastleHeroes.text = $"주둔 영웅 {castle.HeroIds.Count}명  ·  주둔 부대 {armyCount}개";
                ApplyBackgroundSprite(globalCastleImage, definition?.CastleImage);
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

            battleAlertButton.text = unresolvedBattleCount > 0
                ? $"전투 발생 {unresolvedBattleCount}건 · 확인"
                : "현재 전투 없음";
            battleAlertButton.SetEnabled(unresolvedBattleCount > 0 || state.LastMonthlyReport != null);
            battleAlertButton.EnableInClassList("danger", unresolvedBattleCount > 0);
            string latestNews = state.LastMonthlyReport?.News?.LastOrDefault();
            rightMonthlyNews.text = string.IsNullOrEmpty(latestNews)
                ? "새로운 월보가 없습니다."
                : $"최근 소식\n{latestNews}";
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
            panel.Add(new Label("진행 중인 사업·연구·활동·의뢰·이동은 다음 턴에 처리됩니다. 부대 인물은 부대를 해산하거나 구성원에서 제외해야 다시 명령할 수 있습니다."));
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

        // 월보처럼 스크롤 본문 아래에 고정 행동 영역이 필요한 모달을 생성합니다.
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
