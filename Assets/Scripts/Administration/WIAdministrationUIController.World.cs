using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // UXML에 미리 배치된 성 노드에 데이터와 선택 이벤트를 연결합니다.
        private void BuildMap()
        {
            castleButtons.Clear();
            mapConnectionLayer = map.Q<WIMapConnectionLayer>("map-connection-layer");
            foreach (WICastleDefinition castle in database.Castles)
            {
                WICastleRuntimeState castleState = state.GetCastle(castle.Id);
                WIFactionDefinition faction = database.GetFaction(castleState.FactionId);
                Button node = map.Q<Button>($"castle-{castle.Id}");
                if (node == null)
                {
                    Debug.LogError($"UXML 지도 노드를 찾을 수 없습니다: {castle.Id}");
                    continue;
                }

                if (mapNodesBound == false)
                {
                    string castleId = castle.Id;
                    node.clicked += () => SelectCastle(castleId);
                }

                ApplyMapNodeFactionClass(node, castleState.FactionId);
                string castleName = castle.DisplayName.Get(database.UseEnglish);
                string factionCode = GetFactionAccessibilityCode(castleState.FactionId);
                Label nameplate = node.Q<Label>(className: "castle-node-name");
                if (nameplate != null)
                {
                    nameplate.text = TruncateLabel(castleName, 9);
                }
                node.tooltip = $"[{factionCode}] {faction?.DisplayName.Get(database.UseEnglish) ?? castleState.FactionId}\n{castleName}";
                node.style.left = Length.Percent(castle.NormalizedMapPosition.x * 100f);
                node.style.top = Length.Percent(castle.NormalizedMapPosition.y * 100f);
                castleButtons.Add(castle.Id, node);
            }
            mapNodesBound = true;
            RefreshMapConnections();
        }

        // 성 소유 진영에 맞는 성채·깃발 이미지 클래스를 지도 노드에 적용합니다.
        private static void ApplyMapNodeFactionClass(VisualElement node, string factionId)
        {
            string[] factionIds = { "avalon", "valdor", "ironheart", "sylvanroad", "necropolis" };
            foreach (string id in factionIds) node.RemoveFromClassList($"castle-node-{id}");
            string resolved = factionIds.Contains(factionId) ? factionId : "ironheart";
            node.AddToClassList($"castle-node-{resolved}");
        }

        // 인접 성 관계를 중복 없이 경로로 만들고 진영 경계를 전선 색상으로 표시합니다.
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

        // 선택한 성의 상세 정보를 갱신하고 영지 관리 화면으로 전환합니다.
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
            stabilityLabel.text = detailed ? $"질서 {selectedCastle.Stability} · {GetCastleStatusName(selectedCastle.Stability)}" : "질서 ??";
            defenseLabel.text = detailed ? $"방어 {selectedCastle.Defense} · {GetCastleStatusName(selectedCastle.Defense)}" : "방어 ??";
            prosperityLabel.tooltip = detailed ? "번영 · 금화 수입의 기본값입니다. 성 규모와 질서 효율을 곱해 계산합니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            technologyLabel.tooltip = detailed ? "기술 · 마나 수입과 연구 조건에 사용합니다. 성 규모가 높을수록 월간 마나가 증가합니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            stabilityLabel.tooltip = detailed ? "질서 · 금화 수입 효율과 영향력 수입을 높이고 적 첩보 성공률을 낮춥니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            defenseLabel.tooltip = detailed ? "방어 · 공성전 자동 판정과 수비 전력에 반영됩니다." : "정보 미확보 · 조사 또는 동맹 정보가 필요합니다.";
            projectStatusLabel.text = detailed
                ? selectedCastle.DelegatedToGovernor && selectedCastle.ActiveProject == null
                    ? $"영지관 위임 · {GetGovernorPolicyDisplayName(selectedCastle.GovernorPolicy)} · 월 {selectedCastle.GovernorMonthlyBudget}G"
                    : GetProjectStatusText(selectedCastle.ActiveProject)
                : "첩보의 조사를 성공하면 일정 기간 상세 정보가 공개됩니다.";
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
            for (int index = 0; index < castleHeroSlotElements.Count; index += 1)
            {
                bool first = index == 0;
                castleHeroSlotElements[index].style.display = first ? DisplayStyle.Flex : DisplayStyle.None;
                castleHeroSlotElements[index].EnableInClassList("empty-slot", true);
                ClearBackgroundSprite(castleHeroSlotImages[index]);
                castleHeroSlotImages[index].style.display = DisplayStyle.None;
                castleHeroSlotCaptions[index].text = "주둔 인물 정보 미확보";
            }

            for (int index = 0; index < castleFacilitySlotElements.Count; index += 1)
            {
                bool first = index == 0;
                castleFacilitySlotElements[index].style.display = first ? DisplayStyle.Flex : DisplayStyle.None;
                castleFacilitySlotElements[index].EnableInClassList("empty-slot", true);
                ClearBackgroundSprite(castleFacilitySlotImages[index]);
                castleFacilitySlotImages[index].style.display = DisplayStyle.None;
                castleFacilitySlotCaptions[index].text = "시설 정보 미확보";
            }
        }

        // 대륙 전략 화면을 표시하고 영지 관리 화면을 숨깁니다.
        private void ShowGlobalView()
        {
            RefreshMapConnections();
            globalViewHost.style.display = DisplayStyle.Flex;
            castleViewHost.style.display = DisplayStyle.None;
            globalView.style.display = DisplayStyle.Flex;
            castleView.style.display = DisplayStyle.None;
        }

        // 선택한 성의 영지 관리 화면을 표시하고 대륙 전략 화면을 숨깁니다.
        private void ShowCastleView()
        {
            globalViewHost.style.display = DisplayStyle.None;
            castleViewHost.style.display = DisplayStyle.Flex;
            globalView.style.display = DisplayStyle.None;
            castleView.style.display = DisplayStyle.Flex;
        }

        // 외부 UI나 검증 도구에서 지정한 성의 영지 관리 화면으로 이동합니다.
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

        // 대표 자원·성 수치·사업·첩보의 툴팁 문장을 한 화면에서 비교합니다.
        public void OpenCalculationTooltipPreviewForQA()
        {
            OpenCastlePreviewForQA();
            VisualElement panel = CreateModal("계산 근거 툴팁 QA");
            panel.Add(CreateCalculationSample("금화 수입", BuildResourceCalculationTooltip("금화", 1200, 67, 3,
                "성별 번영 × 성 규모 × 질서 효율 + 전문 분야·시설·연구")));
            panel.Add(CreateCalculationSample("성 수치", "질서 50 · 금화 효율 75% · 영향력과 첩보 방어에 반영"));
            panel.Add(CreateCalculationSample("사업 성과", "예상 8 = 기본 3 + 담당 적성 50/20 + 집중 투자 2 + 특기 1 + 전문 분야 0 + 방침 0"));
            panel.Add(CreateCalculationSample("첩보 확률", "성공 55% = 기본 60 + 지력/2 - 질서/2 - 방첩 20\n미조사 대상은 정확한 질서와 최종 확률을 공개하지 않습니다."));
            panel.Add(CreateCalculationSample("외교 명령", "확정 명령 · 비용을 충족하면 무작위 판정 없이 관계가 한 단계 변합니다."));
        }

        // 모든 진영색과 경로색을 회색으로 낮춰 코드·기호만으로 지도를 읽을 수 있는지 검증합니다.
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
            turnDescription.text = "저채도 QA · 진영 코드, 전선 ×, 선택 연결 ◎로 판독하십시오.";
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
                node.text = $"{TruncateLabel(stressName, 12)}\n99H · 99전투단";
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

        // 진영색을 볼 수 없는 상황에서도 사용할 고유 영문 코드를 반환합니다.
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

        // 현재 진영 목록과 경로 기호를 색상 없이 읽을 수 있는 범례 문장으로 갱신합니다.
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
                ? $"영지관 위임 · {result} · 예상 +{project.ExpectedGain} · {project.GoldCost}G"
                : result;
        }

    }
}
