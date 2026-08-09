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

    }
}
