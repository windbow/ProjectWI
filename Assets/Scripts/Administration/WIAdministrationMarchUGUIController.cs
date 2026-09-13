using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMarchUGUIController : WIAdministrationUGUIPanelController
    {
        private const int PageSize = 8;
        private enum PageMode { Army, Commander, Target }
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardImages;
        [SerializeField] private TMP_Text[] cardLabels;
        [SerializeField] private Button createArmyButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageLabel;

        private WIAdministrationMarchSnapshot snapshot;
        private PageMode pageMode;
        private string selectedArmyId;
        private int pageIndex;

        // 고정 카드와 새 전투단·페이지 버튼을 원정 단계에 연결합니다.
        private void Awake()
        {
            ResolveAdministrationController();
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int captured = index;
                cardButtons[index].onClick.AddListener(() => SelectCard(captured));
            }
            createArmyButton.onClick.AddListener(OpenCommanders);
            previousButton.onClick.AddListener(() => ChangePage(-1));
            nextButton.onClick.AddListener(() => ChangePage(1));
        }

        // 원정 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUIMarchRequested += Open;
            }
        }

        // 원정 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIMarchRequested -= Open;
            }
        }

        // 현재 성의 출정 가능한 전투단을 표시합니다.
        private void Open()
        {
            modal.Show("원정 전투단 선택");
            pageMode = PageMode.Army;
            pageIndex = 0;
            if (administrationController.TryGetUGUIMarchArmies(out snapshot, out string error)
                == false)
            {
                snapshot = new WIAdministrationMarchSnapshot();
                messageLabel.gameObject.SetActive(true);
                messageLabel.text = error;
            }
            else messageLabel.gameObject.SetActive(false);
            statusLabel.text = "주둔 전투단을 선택하거나 새 전투단을 편성하십시오.";
            createArmyButton.gameObject.SetActive(true);
            RefreshCards();
        }

        // 새 전투단의 대장으로 지정할 대기 인물을 표시합니다.
        private void OpenCommanders()
        {
            pageMode = PageMode.Commander;
            pageIndex = 0;
            modal.SetTitle("새 전투단 · 대장 선택");
            createArmyButton.gameObject.SetActive(false);
            if (administrationController.TryGetUGUIMarchCommanders(out snapshot, out string error)
                == false)
            {
                ShowError(error);
                return;
            }
            statusLabel.text = snapshot.CastleName + " · 대장을 선택하면 새 전투단을 편성합니다.";
            messageLabel.gameObject.SetActive(false);
            RefreshCards();
        }

        // 선택한 전투단이 이동하거나 원정할 인접 성을 표시합니다.
        private void OpenTargets(string armyId)
        {
            selectedArmyId = armyId;
            pageMode = PageMode.Target;
            pageIndex = 0;
            modal.SetTitle("이동 / 원정 목표");
            createArmyButton.gameObject.SetActive(false);
            if (administrationController.TryGetUGUIMarchTargets(armyId, out snapshot, out string error)
                == false)
            {
                ShowError(error);
                return;
            }
            statusLabel.text = "같은 진영 성은 이동, 교전 중인 진영 성은 영향력 20을 사용해 원정합니다.";
            messageLabel.gameObject.SetActive(false);
            RefreshCards();
        }

        // 현재 단계의 스냅샷을 8개 고정 카드와 페이지 컨트롤에 반영합니다.
        private void RefreshCards()
        {
            int count = snapshot?.Options.Count ?? 0;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)PageSize));
            pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            for (int index = 0; index < cardButtons.Length; index += 1)
            {
                int optionIndex = pageIndex * PageSize + index;
                bool visible = optionIndex < count;
                cardButtons[index].gameObject.SetActive(visible);
                if (visible == false)
                {
                    continue;
                }
                WIAdministrationMarchOptionSnapshot option = snapshot.Options[optionIndex];
                cardImages[index].sprite = option.Image;
                cardImages[index].enabled = option.Image != null;
                cardLabels[index].text = option.DisplayName + "\n" + option.Summary;
                cardButtons[index].interactable = option.Interactable;
            }
            previousButton.interactable = pageIndex > 0;
            nextButton.interactable = pageIndex + 1 < pageCount;
            previousButton.gameObject.SetActive(pageCount > 1);
            nextButton.gameObject.SetActive(pageCount > 1);
            pageLabel.gameObject.SetActive(pageCount > 1);
            pageLabel.text = $"{pageIndex + 1} / {pageCount}";
        }

        // 현재 단계에 따라 전투단·대장·목표 카드 선택을 처리합니다.
        private void SelectCard(int cardIndex)
        {
            int optionIndex = pageIndex * PageSize + cardIndex;
            if (snapshot == null || optionIndex >= snapshot.Options.Count)
            {
                return;
            }
            WIAdministrationMarchOptionSnapshot option = snapshot.Options[optionIndex];
            if (pageMode == PageMode.Army)
            {
                OpenTargets(option.Id);
                return;
            }
            if (pageMode == PageMode.Commander)
            {
                if (administrationController.CreateUGUIMarchArmy(option.Id, out string armyId, out string error)
                    == false)
                {
                    ShowError(error);
                    return;
                }
                OpenTargets(armyId);
                return;
            }
            if (administrationController.BeginUGUIArmyMarch(selectedArmyId, option.Id, out string marchError))
            {
                modal.Hide();
                return;
            }
            ShowError(marchError);
        }

        // 카드 페이지를 지정한 방향으로 변경합니다.
        private void ChangePage(int direction) { pageIndex += direction; RefreshCards(); }

        // 현재 단계의 오류 문구를 표시합니다.
        private void ShowError(string error)
        {
            messageLabel.gameObject.SetActive(true);
            messageLabel.text = error;
            foreach (Button button in cardButtons) button.gameObject.SetActive(false);
        }
    }
}
