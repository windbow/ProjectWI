using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationSpecialFacilityUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Image[] optionIcons;
        [SerializeField] private TMP_Text[] optionLabels;

        private WIAdministrationSpecialFacilitySnapshot snapshot;

        // 고정된 시설 카드 버튼을 선택 기능에 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            for (int index = 0; index < optionButtons.Length; index += 1)
            {
                int captured = index;
                optionButtons[index].onClick.AddListener(() => Select(captured));
            }
        }

        // 특화 시설 선택 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUISpecialFacilityRequested += Open;
        }

        // 특화 시설 선택 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUISpecialFacilityRequested -= Open;
        }

        // 현재 성에서 선택 가능한 특화 시설을 표시합니다.
        private void Open()
        {
            modal.Show("특화 시설 선택");
            if (administrationController.TryGetUGUISpecialFacilitySnapshot(out snapshot, out string error) == false)
            {
                statusLabel.text = string.Empty;
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
                HideOptions();
                return;
            }
            messageLabel.gameObject.SetActive(false);
            statusLabel.text = $"{snapshot.CastleName} · 특화 시설 {snapshot.OccupiedSlots}/{snapshot.MaximumSlots} · 새 시설 하나를 선택하십시오.";
            for (int index = 0; index < optionButtons.Length; index += 1)
            {
                bool visible = index < snapshot.Options.Count;
                optionButtons[index].gameObject.SetActive(visible);
                if (visible == false) continue;
                WIAdministrationSpecialFacilityOptionSnapshot option = snapshot.Options[index];
                optionIcons[index].sprite = option.Icon;
                optionIcons[index].enabled = option.Icon != null;
                optionLabels[index].text = option.DisplayName + "\n" + option.Description;
            }
        }

        // 선택한 시설을 기존 성 상태에 추가하고 모달을 닫습니다.
        private void Select(int index)
        {
            if (snapshot == null || index >= snapshot.Options.Count) return;
            if (administrationController.SelectUGUISpecialFacility(snapshot.Options[index].FacilityId, out string error))
            {
                modal.Hide();
                return;
            }
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
        }

        // 선택할 수 없는 상태에서는 시설 카드를 모두 숨깁니다.
        private void HideOptions()
        {
            foreach (Button button in optionButtons) button.gameObject.SetActive(false);
        }
    }
}
