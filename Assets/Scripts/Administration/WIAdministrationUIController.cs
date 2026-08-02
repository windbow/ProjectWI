using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
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
        private Label factionLabel;
        private Label dateLabel;
        private Label turnDescription;
        private Label goldLabel;
        private Label manaLabel;
        private Label influenceLabel;
        private Label castleTitle;
        private Label castleInfo;
        private Label prosperityLabel;
        private Label technologyLabel;
        private Label stabilityLabel;
        private Label defenseLabel;
        private Label projectStatusLabel;
        private VisualElement heroSlots;
        private VisualElement specialFacilitySlots;
        private Button specialFacilityButton;
        private WICastleRuntimeState selectedCastle;
        private readonly Dictionary<string, Button> castleButtons = new Dictionary<string, Button>();
        private readonly Dictionary<WICampaignDifficulty, Button> difficultyButtons = new Dictionary<WICampaignDifficulty, Button>();
        private WICampaignDifficulty selectedDifficulty = WICampaignDifficulty.Standard;

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
            BuildMap();
            RefreshAll();
            SelectInitialCastle();
            BuildCampaignStartScreen();
            campaignStartLayer.style.display = campaignService == null || campaignService.HasCampaignStarted == false
                ? DisplayStyle.Flex
                : DisplayStyle.None;
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
            factionLabel = root.Q<Label>("faction-label");
            dateLabel = root.Q<Label>("date-label");
            turnDescription = root.Q<Label>("turn-description");
            goldLabel = root.Q<Label>("gold-label");
            manaLabel = root.Q<Label>("mana-label");
            influenceLabel = root.Q<Label>("influence-label");
            castleTitle = root.Q<Label>("castle-title");
            castleInfo = root.Q<Label>("castle-info");
            prosperityLabel = root.Q<Label>("prosperity-label");
            technologyLabel = root.Q<Label>("technology-label");
            stabilityLabel = root.Q<Label>("stability-label");
            defenseLabel = root.Q<Label>("defense-label");
            projectStatusLabel = root.Q<Label>("project-status-label");
            heroSlots = root.Q<VisualElement>("hero-slots");
            specialFacilitySlots = root.Q<VisualElement>("special-facility-slots");
            specialFacilityButton = root.Q<Button>("special-facility-button");
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

            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            Button continueButton = root.Q<Button>("continue-campaign-button");
            continueButton.SetEnabled(service != null && service.HasSave(0));
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
            BeginCampaign(selectedDifficulty);
        }

        // 지정 난이도로 새 캠페인을 시작해 시작 화면을 닫습니다.
        public void BeginCampaign(WICampaignDifficulty difficulty)
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            state = service == null
                ? WIAdministrationState.Create(database, difficulty)
                : service.StartNewCampaign(difficulty);
            RebuildCampaignView();
            campaignStartLayer.style.display = DisplayStyle.None;
            if (ShowCampaignResult() == false)
            {
                ShowCurrentTutorial();
            }
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
                node.text = castle.DisplayName.Get(database.UseEnglish);
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
                        frontline));
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
            ApplyBackgroundSprite(castleBackground, castle.CastleImage);
            WIHeroDefinition governor = database.GetHero(selectedCastle.GovernorHeroId);
            ApplyBackgroundSprite(governorPortrait, governor == null ? null : governor.Portrait);
            castleTitle.text = castle.DisplayName.Get(database.UseEnglish);
            string terrainName = castle.TerrainTrait == null ? string.Empty : castle.TerrainTrait.Get(database.UseEnglish);
            castleInfo.text = $"{selectedCastle.CastleSize} · {terrainName} · 인물 {selectedCastle.HeroIds.Count}/{selectedCastle.GetHeroSlotCount()}";
            prosperityLabel.text = $"번영 {selectedCastle.Prosperity} · {GetCastleStatusName(selectedCastle.Prosperity)}";
            technologyLabel.text = $"기술 {selectedCastle.Technology} · {GetCastleStatusName(selectedCastle.Technology)}";
            stabilityLabel.text = $"치안 {selectedCastle.Stability} · {GetCastleStatusName(selectedCastle.Stability)}";
            defenseLabel.text = $"방어 {selectedCastle.Defense} · {GetCastleStatusName(selectedCastle.Defense)}";
            projectStatusLabel.text = selectedCastle.DelegatedToGovernor && selectedCastle.ActiveProject == null
                ? $"태수 위임 · {GetGovernorPolicyDisplayName(selectedCastle.GovernorPolicy)} · 월 {selectedCastle.GovernorMonthlyBudget}G"
                : GetProjectStatusText(selectedCastle.ActiveProject);
            RefreshCastleSlots(castle, selectedCastle);
            specialFacilityButton.SetEnabled(selectedCastle.PendingSpecialFacilityChoice);
            ShowCastleView();
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
            if (selectedCastle == null)
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
            ShowMessage(slot == 0 ? "자동 저장을 불러왔습니다." : $"슬롯 {slot}을 불러왔습니다.");
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
                panel.Add(new Label($"{faction.DisplayName.Get(database.UseEnglish)} · 영토 {castleCount}성 · 성향 {GetAIStrategyDisplayName(faction.AIStrategy)}{economy}"));
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
            foreach (WISchemeDefinition scheme in database.SchemeDefinitions)
            {
                Button button = new Button(() => OpenSchemeAgentModal(scheme));
                button.text = $"{scheme.DisplayName.Get(database.UseEnglish)} · 영향력 {scheme.InfluenceCost}\n{scheme.Description.Get(database.UseEnglish)}";
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
                string defense = ownTarget ? $"방첩 {castleState.CounterintelligenceMonths}개월" : $"공개 치안 {castleState.Stability}";
                button.text = $"{castle.DisplayName.Get(database.UseEnglish)} · {defense}";
                panel.Add(button);
            }
        }

        // 인재 이간의 대상 성에 주둔한 적 인물을 표시합니다.
        private void OpenSchemeTargetHeroModal(WISchemeDefinition scheme, WIHeroDefinition agent, WICastleRuntimeState castle)
        {
            VisualElement panel = CreateModal("인재 이간 · 대상 인물");
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

        // 계략 성공 판정을 실행하고 조사 성공 시 확보한 성 정보를 결과에 포함합니다.
        private void ExecuteScheme(WISchemeDefinition scheme, WIHeroDefinition agent, WICastleRuntimeState targetCastle, WIHeroDefinition targetHero)
        {
            WISchemeResult result = WISchemeSystem.Execute(database, state, scheme.Id, state.PlayerFactionId,
                agent.Id, targetCastle.CastleId, targetHero?.Id, UnityEngine.Random.Range(0, 100));
            RefreshAll();
            string detail = result.Message;
            if (result.Executed)
            {
                detail += $"\n성공 확률 {result.SuccessChance}%";
            }
            if (result.Succeeded && scheme.SchemeType == WISchemeType.Investigation)
            {
                detail += $"\n번영 {targetCastle.Prosperity} · 기술 {targetCastle.Technology} · 치안 {targetCastle.Stability} · 방어 {targetCastle.Defense}";
                detail += $"\n주둔 인물 {targetCastle.HeroIds.Count}명 · 방첩 {targetCastle.CounterintelligenceMonths}개월";
            }
            ShowMessage(detail);
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

            if (relation.Status == WIDiplomaticStatus.War || relation.Status == WIDiplomaticStatus.Neutral)
            {
                Button improve = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.ImproveDiplomaticRelations(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                improve.text = relation.Status == WIDiplomaticStatus.War
                    ? $"휴전 교섭 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} · 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}"
                    : $"친선 사절 · 금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} · 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}";
                panel.Add(improve);
            }
            else if (relation.Status == WIDiplomaticStatus.Friendly)
            {
                Button pact = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.SignNonAggression(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                pact.text = $"불가침 협정 · 영향력 {WIAdministrationTurnSystem.NonAggressionInfluenceCost}";
                panel.Add(pact);
            }
            else if (relation.Status == WIDiplomaticStatus.NonAggression)
            {
                Button alliance = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.FormAlliance(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                alliance.text = $"동맹 체결 · 영향력 {WIAdministrationTurnSystem.AllianceInfluenceCost}";
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
                panel.Add(aid);
            }

            if (relation.Status != WIDiplomaticStatus.War)
            {
                Button war = new Button(() => ExecuteDiplomaticCommand(
                    WIAdministrationTurnSystem.DeclareWar(state, state.PlayerFactionId, targetFaction.Id),
                    targetFaction));
                war.text = $"선전포고 · 영향력 {WIAdministrationTurnSystem.DeclareWarInfluenceCost}";
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
            if (selectedCastle == null)
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
            VisualElement panel = CreateModal("지난달 월보");
            panel.Add(new Label($"금화 +{report.GoldGained} / 지출 -{report.GoldSpent}"));
            panel.Add(new Label($"마나 +{report.ManaGained} / 영향력 +{report.InfluenceGained}"));
            foreach (string delegationReport in report.DelegationReports)
            {
                panel.Add(new Label(delegationReport));
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
            if (selectedCastle == null)
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
            foreach (WIBattleSessionState session in state.BattleSessions.FindAll(item => item.Status != WIBattleSessionStatus.Resolved))
            {
                Button battleButton = new Button(() => OpenBattleSessionModal(session));
                battleButton.text = $"전투 세션 · {database.GetCastle(session.CastleId).DisplayName.Get(database.UseEnglish)} · {session.Status}";
                panel.Add(battleButton);
            }
            foreach (WIArmyState army in state.Armies)
            {
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
                    WIAdministrationTurnSystem.BeginArmyMarch(database, state, army, targetId);
                    CloseModal();
                    RefreshAll();
                });
                button.text = $"{action} · {database.GetCastle(targetId).DisplayName.Get(database.UseEnglish)}";
                panel.Add(button);
            }
        }

        // 선택한 성에 주둔한 부대 중 출정할 부대를 선택합니다.
        private void OpenCastleMarchModal()
        {
            if (selectedCastle == null)
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
                panel.Add(new Label("이 성에서 출정할 수 있는 부대가 없습니다."));
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
            if (selectedCastle == null)
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

        // 미배치 영웅을 현재 성에 배치하는 모달을 엽니다.
        private void OpenHeroAssignmentModal()
        {
            if (selectedCastle == null)
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
                WICharacterTransferState transfer = state.CharacterTransfers.Find(item => item.HeroId == character.HeroId);
                if (transfer != null)
                {
                    activity = $"이동 중 · {database.GetCastle(transfer.TargetCastleId).DisplayName.Get(database.UseEnglish)} · {transfer.RemainingMonths}개월";
                }
                WICharacterGrade effectiveGrade = character.PromotedToHero ? WICharacterGrade.Hero : character.BaseGrade;
                panel.Add(new Label($"[{GetGradeDisplayName(effectiveGrade)}] {hero.DisplayName.Get(database.UseEnglish)} · {hero.HeroClass} · 공적 {character.Merit} · 명성 {character.Reputation} · {character.LoyaltyState} · {activity}"));
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
            VisualElement panel = CreateModal(database.GetText("UI_TURN_SUMMARY"));
            panel.Add(new Label($"Gold +{summary.GoldGained}"));
            panel.Add(new Label($"Gold Spent -{summary.GoldSpent}"));
            panel.Add(new Label($"Mana +{summary.ManaGained}"));
            panel.Add(new Label($"Influence +{summary.InfluenceGained}"));
            foreach (string report in summary.DelegationReports)
            {
                panel.Add(new Label(report));
            }
            foreach (string news in summary.News)
            {
                panel.Add(new Label(news));
            }
            if (state.CampaignResult != WICampaignResult.Ongoing && state.CampaignResultAcknowledged == false)
            {
                AddCampaignResultContent(panel);
            }
            else
            {
                AddPendingTutorial(panel);
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
            string title = state.CampaignResult == WICampaignResult.Victory
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
            string title = state.CampaignResult == WICampaignResult.Victory
                ? rules.VictoryTitle.Get(database.UseEnglish)
                : rules.DefeatTitle.Get(database.UseEnglish);
            string description = state.CampaignResult == WICampaignResult.Victory
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

            factionLabel.text = playerFaction == null
                ? database.GetText("UI_GAME_TITLE")
                : playerFaction.DisplayName.Get(database.UseEnglish);
            ApplyBackgroundSprite(factionEmblem, playerFaction == null ? null : playerFaction.Emblem);
            dateLabel.text = $"{state.Year}년 {state.Month:00}월 · 제 {state.Turn}턴";
            WITurnSummary forecast = WIAdministrationTurnSystem.GetFactionMonthlyIncome(database, state, state.PlayerFactionId);
            bool depleted = state.Gold <= 0 || state.ManaCrystal <= 0 || state.Influence <= 0;
            turnDescription.text = depleted
                ? $"자원 고갈 · 다음 턴 기본 수입 G+{forecast.GoldGained} / M+{forecast.ManaGained} / I+{forecast.InfluenceGained}로 회복할 수 있습니다."
                : $"세력 방침: {GetFactionPolicyDisplayName(state.FactionPolicy)} · {state.Month:00}월 명령을 검토하십시오.";
            goldLabel.text = $"금화\n{state.Gold:N0} (+{forecast.GoldGained})";
            manaLabel.text = $"마나\n{state.ManaCrystal:N0} (+{forecast.ManaGained})";
            influenceLabel.text = $"영향력\n{state.Influence:N0} (+{forecast.InfluenceGained})";
            foreach (KeyValuePair<string, Button> pair in castleButtons)
            {
                WICastleRuntimeState castle = state.GetCastle(pair.Key);
                WICastleDefinition definition = database.GetCastle(pair.Key);
                WIFactionDefinition owner = database.GetFaction(castle.FactionId);
                if (owner != null)
                {
                    pair.Value.style.backgroundColor = owner.Color;
                }
                string eventBadge = castle.InvasionWarning ? " !" : string.Empty;
                string occupationBadge = castle.OccupationUnrestMonths > 0 ? " ⚑" : string.Empty;
                int armyCount = state.Armies.FindAll(army => army.CurrentCastleId == pair.Key).Count;
                string battleBadge = state.Armies.Exists(army => army.CurrentCastleId == pair.Key && army.AwaitingBattle) ? " ⚔" : string.Empty;
                pair.Value.text = $"{definition.DisplayName.Get(database.UseEnglish)}\n{castle.HeroIds.Count}H · {armyCount}부대{eventBadge}{battleBadge}{occupationBadge}";
            }
            RefreshMapConnections();

            if (selectedCastle != null && castleView.resolvedStyle.display == DisplayStyle.Flex)
            {
                string selectedCastleId = selectedCastle.CastleId;
                SelectCastle(selectedCastleId);
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
            CloseModal();
            modalLayer.style.display = DisplayStyle.Flex;
            VisualElement panel = new VisualElement();
            panel.AddToClassList("modal-panel");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("modal-title");
            panel.Add(titleLabel);
            Button close = new Button(CloseModal);
            close.text = database.GetText("UI_CLOSE");
            close.AddToClassList("modal-close");
            panel.Add(close);
            modalLayer.Add(panel);
            return panel;
        }

        // 현재 열린 모달을 닫습니다.
        private void CloseModal()
        {
            modalLayer.Clear();
            modalLayer.style.display = DisplayStyle.None;
        }
    }
}
