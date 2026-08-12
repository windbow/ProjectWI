using System.Collections.Generic;
using ProjectWI.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public class WICampaignTitleUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationDatabaseSO database;
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private Button[] difficultyButtons;
        [SerializeField] private TMP_Text[] difficultyLabels;
        [SerializeField] private Button[] variantButtons;
        [SerializeField] private TMP_Text[] variantLabels;
        [SerializeField] private Button newCampaignButton;
        [SerializeField] private Button continueCampaignButton;
        [SerializeField] private Color normalCardColor = new Color32(17, 27, 35, 255);
        [SerializeField] private Color selectedCardColor = new Color32(233, 237, 240, 255);
        [SerializeField] private Color normalTextColor = new Color32(220, 227, 231, 255);
        [SerializeField] private Color selectedTextColor = new Color32(238, 242, 244, 255);

        private readonly List<WICampaignDifficultyDefinition> difficultyDefinitions = new List<WICampaignDifficultyDefinition>();
        private readonly List<WICampaignVariantDefinition> variantDefinitions = new List<WICampaignVariantDefinition>();
        private WICampaignDifficulty selectedDifficulty = WICampaignDifficulty.Standard;
        private WICampaignVariant selectedVariant = WICampaignVariant.Classic;

        // 외부 QA 시작이나 저장 불러오기처럼 다른 경로에서 캠페인이 시작되어도 타이틀을 닫습니다.
        private void OnEnable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIWorldChanged += HideWhenCampaignStarted;
            }
        }

        // 월드 상태 변경 알림 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIWorldChanged -= HideWhenCampaignStarted;
            }
        }

        // 고정 배치된 UGUI 카드에 데이터와 클릭 동작을 연결합니다.
        private void Awake()
        {
            if (administrationController == null)
            {
                administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            }

            if (database == null || administrationController == null)
            {
                Debug.LogError("UGUI 캠페인 타이틀 화면에 데이터베이스 또는 행정 UI 컨트롤러가 연결되지 않았습니다.");
                enabled = false;
                return;
            }

            administrationController.UGUIWorldChanged -= HideWhenCampaignStarted;
            administrationController.UGUIWorldChanged += HideWhenCampaignStarted;

            BindDifficultyCards();
            BindVariantCards();
            newCampaignButton.onClick.AddListener(StartNewCampaign);
            continueCampaignButton.onClick.AddListener(ContinueCampaign);

            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            continueCampaignButton.interactable = service != null && service.HasSave(0);
            SelectDifficulty(WICampaignDifficulty.Standard);
            SelectVariant(WICampaignVariant.Classic);

            if (service != null && service.HasCampaignStarted)
            {
                gameObject.SetActive(false);
            }
        }

        // ScriptableObject 난이도 정의를 세 개의 고정 UGUI 카드에 표시합니다.
        private void BindDifficultyCards()
        {
            difficultyDefinitions.Clear();
            difficultyDefinitions.AddRange(database.DifficultyDefinitions);
            int count = Mathf.Min(difficultyButtons.Length, difficultyDefinitions.Count);
            for (int index = 0; index < count; index += 1)
            {
                int capturedIndex = index;
                WICampaignDifficultyDefinition definition = difficultyDefinitions[index];
                difficultyLabels[index].text = $"{definition.DisplayName.Get(database.UseEnglish)}\n\n{FormatDescription(definition.Description.Get(database.UseEnglish))}";
                difficultyButtons[index].onClick.AddListener(() => SelectDifficulty(difficultyDefinitions[capturedIndex].Difficulty));
            }
        }

        // ScriptableObject 시작 조건 정의를 세 개의 고정 UGUI 카드에 표시합니다.
        private void BindVariantCards()
        {
            variantDefinitions.Clear();
            variantDefinitions.AddRange(database.CampaignVariants);
            int count = Mathf.Min(variantButtons.Length, variantDefinitions.Count);
            for (int index = 0; index < count; index += 1)
            {
                int capturedIndex = index;
                WICampaignVariantDefinition definition = variantDefinitions[index];
                variantLabels[index].text = $"{definition.DisplayName.Get(database.UseEnglish)}\n{FormatDescription(definition.Description.Get(database.UseEnglish))}";
                variantButtons[index].onClick.AddListener(() => SelectVariant(variantDefinitions[capturedIndex].Variant));
            }
        }

        // 카드 설명을 마침표 단위로 줄바꿈합니다.
        private static string FormatDescription(string description)
        {
            return string.IsNullOrWhiteSpace(description) ? string.Empty : description.Replace(". ", ".\n");
        }

        // 선택 난이도와 카드 강조 상태를 변경합니다.
        private void SelectDifficulty(WICampaignDifficulty difficulty)
        {
            selectedDifficulty = difficulty;
            for (int index = 0; index < difficultyButtons.Length; index += 1)
            {
                bool selected = index < difficultyDefinitions.Count && difficultyDefinitions[index].Difficulty == difficulty;
                ApplyCardState(difficultyButtons[index], difficultyLabels[index], selected);
            }
        }

        // 선택 시작 조건과 카드 강조 상태를 변경합니다.
        private void SelectVariant(WICampaignVariant variant)
        {
            selectedVariant = variant;
            for (int index = 0; index < variantButtons.Length; index += 1)
            {
                bool selected = index < variantDefinitions.Count && variantDefinitions[index].Variant == variant;
                ApplyCardState(variantButtons[index], variantLabels[index], selected);
            }
        }

        // 선택 여부에 맞춰 카드 배경과 글자색을 적용합니다.
        private void ApplyCardState(Button button, TMP_Text label, bool selected)
        {
            button.image.color = selected ? selectedCardColor : normalCardColor;
            label.color = selected ? selectedTextColor : normalTextColor;
        }

        // 선택한 설정으로 기존 캠페인 시작 흐름을 호출하고 UGUI 타이틀을 닫습니다.
        private void StartNewCampaign()
        {
            administrationController.BeginCampaign(selectedDifficulty, selectedVariant);
            gameObject.SetActive(false);
        }

        // 기존 자동 저장 불러오기 흐름을 호출하고 성공한 경우 UGUI 타이틀을 닫습니다.
        private void ContinueCampaign()
        {
            administrationController.ContinueCampaignFromAutoSave();
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            if (service != null && service.HasCampaignStarted)
            {
                gameObject.SetActive(false);
            }
        }

        // 캠페인 시작 상태가 확인되면 UGUI 타이틀 화면을 숨깁니다.
        private void HideWhenCampaignStarted()
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            if (service != null && service.HasCampaignStarted)
            {
                gameObject.SetActive(false);
            }
        }

        // 캠페인 결과에서 시작 화면으로 돌아올 때 타이틀 프리팹을 다시 표시합니다.
        public void ShowCampaignStart()
        {
            gameObject.SetActive(true);
        }
    }
}
