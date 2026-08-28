using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationCastleRecordUGUIController : WIAdministrationUGUIPanelController
    {
        [SerializeField] private WIAdministrationModalUGUIController modal;
        [SerializeField] private Image castleImage;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private ScrollRect scrollRect;

        // 성 상세 기록 UGUI 열기 요청을 구독합니다.
        private void OnEnable()
        {
            ResolveAdministrationController();
            if (administrationController != null)
            {
                administrationController.UGUICastleRecordRequested += Open;
            }
        }

        // 성 상세 기록 UGUI 열기 요청 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUICastleRecordRequested -= Open;
            }
        }

        // 현재 선택 성의 공개 가능한 상세 기록을 고정 이미지와 스크롤 본문에 표시합니다.
        private void Open()
        {
            if (administrationController.TryGetUGUICastleRecordSnapshot(out WIAdministrationCastleRecordSnapshot snapshot)
                == false)
            {
                return;
            }
            modal.Show(snapshot.CastleName + " 상세");
            castleImage.sprite = snapshot.CastleImage;
            castleImage.enabled = snapshot.CastleImage != null;
            bodyLabel.text = snapshot.Body;
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
