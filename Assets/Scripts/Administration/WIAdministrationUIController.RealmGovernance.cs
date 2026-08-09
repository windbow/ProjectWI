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
                ShowMessage("영지관 위임을 해제한 뒤 직접 중점 사업을 지정할 수 있습니다.");
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
                button.tooltip = $"예상 성과 {expectedGain} = 기본 {database.ProjectBalance.BaseGain} + 담당 적성 {relevantStat}/{database.ProjectBalance.StatDivisor} + 투자 {investmentBonus} + 특기 {traitBonus} + 전문 분야 {specialtyBonus} + 진영 방침 {policyBonus}\n기본·적성·투자·특기 합계는 {database.ProjectBalance.MinimumGain}~{database.ProjectBalance.MaximumGain} 범위로 제한된 뒤 전문 분야와 방침을 더합니다.";
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

            ShowMessage("다른 진영의 성은 정보를 열람할 수 있지만 영지 관리 명령은 내릴 수 없습니다.");
            return false;
        }

        // 이번 달 진영 전체 방침을 선택하는 의회 모달을 엽니다.
        private void OpenFactionPolicyModal()
        {
            VisualElement panel = CreateModal("이번 달 진영 방침");
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

        // 플레이어 진영의 유휴 인물 중 연구 담당자를 선택합니다.
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

    }
}
