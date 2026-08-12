using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationSystemUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private TMP_Text[] cardLabels;

        private int page;

        // 고정 시스템 카드와 페이지 이동 버튼에 선택 처리를 연결합니다.
        private void Awake()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int capturedIndex = index;
                cardButtons[index].onClick.AddListener(() => SelectCard(capturedIndex));
            }
        }

        // 시스템 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            if (administrationController == null) administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            if (administrationController != null) administrationController.UGUISystemRequested += Open;
        }

        // 시스템 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null) administrationController.UGUISystemRequested -= Open;
        }

        // 시스템 설정 화면을 첫 페이지부터 표시합니다.
        private void Open()
        {
            page = 0;
            modal.Show("시스템 · 저장 및 불러오기");
            Refresh();
        }

        // 설정 또는 저장 페이지로 이동합니다.
        private void ChangePage(int direction)
        {
            page = Mathf.Clamp(page + direction, 0, 1);
            Refresh();
        }

        // 현재 페이지의 고정 카드 문구와 사용 가능 상태를 갱신합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUISystemSnapshot(out WIAdministrationSystemSnapshot snapshot) == false)
            {
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = "시스템 정보를 불러올 수 없습니다.";
                return;
            }

            statusLabel.text = snapshot.AutoSaveEnabled ? "턴 종료 자동 저장 · 사용" : "턴 종료 자동 저장 · 미사용";
            messageLabel.gameObject.SetActive(false);
            pageLabel.text = page == 0 ? "설정  1 / 2" : "저장  2 / 2";
            previousButton.interactable = page > 0;
            nextButton.interactable = page < 1;
            string[] labels = page == 0
                ? new[]
                {
                    $"언어\n{(snapshot.UseEnglish ? "English" : "한국어")}",
                    $"화면 모드\n{(snapshot.Fullscreen ? "전체 화면" : "창 모드")}",
                    $"전체 음량\n{Mathf.RoundToInt(snapshot.MasterVolume * 100f)}%",
                    $"음악 음량\n{Mathf.RoundToInt(snapshot.MusicVolume * 100f)}%",
                    $"효과음 음량\n{Mathf.RoundToInt(snapshot.SfxVolume * 100f)}%",
                    $"목표 프레임\n{snapshot.TargetFrameRate} FPS",
                    "설정 적용\n변경값 저장",
                    "기본값 복원\n설정 초기화"
                }
                : new[]
                {
                    "슬롯 1 저장", snapshot.HasSave1 ? "슬롯 1 불러오기" : "슬롯 1 비어 있음",
                    "슬롯 2 저장", snapshot.HasSave2 ? "슬롯 2 불러오기" : "슬롯 2 비어 있음",
                    "슬롯 3 저장", snapshot.HasSave3 ? "슬롯 3 불러오기" : "슬롯 3 비어 있음",
                    snapshot.HasAutoSave ? "자동 저장 불러오기" : "자동 저장 없음", "설정 페이지로 이동"
                };
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                cardLabels[index].text = labels[index];
                cardButtons[index].interactable = page == 0 || IsSaveCardEnabled(snapshot, index);
            }
        }

        // 저장 페이지에서 비어 있는 불러오기 카드만 비활성화합니다.
        private bool IsSaveCardEnabled(WIAdministrationSystemSnapshot snapshot, int index)
        {
            if (index == 1) return snapshot.HasSave1;
            if (index == 3) return snapshot.HasSave2;
            if (index == 5) return snapshot.HasSave3;
            if (index == 6) return snapshot.HasAutoSave;
            return true;
        }

        // 현재 페이지의 선택 카드를 기존 설정 또는 저장 기능으로 전달합니다.
        private void SelectCard(int index)
        {
            if (page == 0)
            {
                if (index < 6)
                {
                    administrationController.ChangeUGUISystemSetting(index);
                    Refresh();
                    return;
                }
                modal.Hide();
                if (index == 6) administrationController.ApplyUGUISystemSettings();
                else administrationController.ResetUGUISystemSettings();
                return;
            }

            if (index == 7)
            {
                page = 0;
                Refresh();
                return;
            }
            int slot = index == 6 ? 0 : index / 2 + 1;
            modal.Hide();
            if (index < 6 && index % 2 == 0) administrationController.SaveUGUICampaignSlot(slot);
            else administrationController.LoadUGUICampaignSlot(slot);
        }
    }
}
