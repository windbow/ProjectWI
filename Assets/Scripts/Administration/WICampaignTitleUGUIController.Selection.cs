using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public partial class WICampaignTitleUGUIController
    {
        // 편집 시점에 저장한 선택 화면 및 난이도 화면 참조입니다.
        [SerializeField] private GameObject scenarioPage;
        [SerializeField] private GameObject settingsPage;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text pageTitle;
        [SerializeField] private TMP_Text pageSubtitle;
        [SerializeField] private TMP_Text footerHint;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailSubtitle;
        [SerializeField] private TMP_Text detailStory;
        [SerializeField] private TMP_Text detailFaction;
        [SerializeField] private TMP_Text detailProtagonist;
        [SerializeField] private TMP_Text detailCastle;
        [SerializeField] private TMP_Text detailObjective;
        [SerializeField] private TMP_Text settingsSummary;
        [SerializeField] private Image detailArtwork;
        [SerializeField] private Image[] variantArtwork;
        [SerializeField] private TMP_Text[] variantSubtitles;
        [SerializeField] private TMP_Text[] variantBadges;
        [SerializeField] private TMP_Text[] fixedLabels;
        [SerializeField] private string[] fixedLabelUids;
        // 현재 단계만 변경하며 선택 난이도와 시나리오는 이전 이동에도 보존합니다.
        private bool showingSettings;

        // 누락된 UI를 동적 생성하지 않고 프리팹 수정이 필요한 오류로 보고합니다.
        private bool ValidateSelectionView()
        {
            bool valid = scenarioPage != null && settingsPage != null && backButton != null &&
                detailArtwork != null && detailTitle != null && detailSubtitle != null &&
                detailStory != null && detailFaction != null && detailProtagonist != null &&
                detailCastle != null && detailObjective != null && settingsSummary != null &&
                pageTitle != null && pageSubtitle != null && footerHint != null && nextLabel != null &&
                newCampaignButton != null && continueCampaignButton != null &&
                variantArtwork.Length == variantButtons.Length &&
                variantSubtitles.Length == variantButtons.Length && variantBadges.Length == variantButtons.Length &&
                fixedLabels.Length == fixedLabelUids.Length;
            if (valid == false)
            {
                Debug.LogError("시나리오 선택 프리팹 참조가 누락되었습니다. 에디터에서 프리팹을 수정하세요.", this);
            }
            return valid;
        }

        // 고정 문구를 DB 문자열 UID로 연결하고 이전 버튼을 구독합니다.
        private void BindSelectionView()
        {
            for (int index = 0; index < fixedLabels.Length; index += 1)
            {
                fixedLabels[index].text = database.GetText(fixedLabelUids[index]);
            }
            backButton.onClick.AddListener(() => ShowSettings(false));
        }

        // 선택 정의의 삽화와 서사, 실제 시작 성과 플레이어 세력을 표시합니다.
        private void RefreshSelectionDetails()
        {
            WICampaignVariantDefinition definition = variantDefinitions.Find(item => item.Variant == selectedVariant);
            if (definition == null)
            {
                return;
            }
            detailArtwork.sprite = definition.SelectionArtwork;
            detailTitle.text = database.GetText(definition.SelectionTitleUid);
            detailSubtitle.text = database.GetText(definition.SelectionSubtitleUid);
            detailStory.text = database.GetText(definition.SelectionStoryUid);
            detailObjective.text = database.GetText(definition.SelectionObjectiveUid);
            detailProtagonist.text = database.GetText(definition.SelectionProtagonistUid);
            WICastleDefinition castle = database.GetCastle(definition.PlayerStartingCastleId);
            detailCastle.text = castle != null ? castle.DisplayName.Get(database.UseEnglish) : string.Empty;
            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.PlayerFaction == true)
                {
                    detailFaction.text = faction.DisplayName.Get(database.UseEnglish);
                    break;
                }
            }
            settingsSummary.text = detailTitle.text;
        }

        // 미리 제작된 두 페이지의 활성 상태와 하단 명령만 전환합니다.
        private void ShowSettings(bool show)
        {
            showingSettings = show;
            scenarioPage.SetActive(show == false);
            settingsPage.SetActive(show);
            backButton.gameObject.SetActive(show);
            continueCampaignButton.gameObject.SetActive(show == false);
            pageTitle.text = database.GetText(show ? "UI_SCENARIO_SETTINGS" : "UI_SCENARIO_TITLE");
            pageSubtitle.text = database.GetText(show ? "UI_SCENARIO_SETTINGS_HINT" : "UI_SCENARIO_SUBTITLE");
            footerHint.text = database.GetText(show ? "UI_SCENARIO_START_HINT" : "UI_SCENARIO_NEXT_HINT");
            nextLabel.text = database.GetText(show ? "UI_SCENARIO_START" : "UI_SCENARIO_NEXT");
        }

        // 첫 클릭은 난이도 선택으로 이동하며 두 번째 단계에서만 캠페인을 시작합니다.
        private void AdvanceSelection()
        {
            if (showingSettings == false)
            {
                ShowSettings(true);
                return;
            }
            StartNewCampaign();
        }
    }
}
