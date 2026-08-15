using System.Collections;
using UnityEngine;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public enum WIAdministrationShortcutAction
    {
        None, CloseModal, ReturnToGlobal, Military, Heroes, Diplomacy, Scheme, Research,
        Faction, Council, MonthlyReport, EndTurn
    }

    public partial class WIAdministrationUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationDatabaseSO database;

        private WIAdministrationState state;
        private WICastleRuntimeState selectedCastle;

        // 캠페인 상태를 준비하고 UGUI 화면에 초기 상태를 전달합니다.
        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError("전략 데이터베이스가 할당되지 않았습니다.");
                enabled = false;
                return;
            }

            WICampaignRuntimeService campaignService = WICampaignRuntimeService.Instance;
            state = campaignService == null
                ? WIAdministrationState.Create(database)
                : campaignService.GetOrCreateState(database);
            SelectInitialCastle();
            RefreshAll();
            if (campaignService != null && campaignService.HasCampaignStarted &&
                WIAdministrationTurnSystem.HasUnresolvedPlayerBattles(state))
            {
                StartCoroutine(OpenPendingBattleReportUGUIAfterInitialization());
            }
        }

        // 모든 UGUI 프리팹의 구독이 준비된 다음 미결 전투가 포함된 월간 보고를 엽니다.
        private IEnumerator OpenPendingBattleReportUGUIAfterInitialization()
        {
            yield return null;
            UGUIMonthlyReportRequested?.Invoke();
        }

        // 현재 캠페인 상태가 바뀌었음을 UGUI 화면들에 알립니다.
        private void RefreshAll()
        {
            NotifyUGUIWorldChanged();
        }

        // UID 또는 직접 작성한 문장을 공통 UGUI 안내 화면에 표시합니다.
        private void ShowMessage(string uid)
        {
            string message = database.GetText(uid);
            if (string.IsNullOrEmpty(message) || message == uid)
            {
                message = uid;
            }
            ShowUGUIMessage("안내", message);
        }

        // 이전 모달 종료 호출을 UGUI 화면 복원 처리로 연결합니다.
        private void CloseModal()
        {
            ResumeUGUIAfterLegacyModal();
        }

        // 키와 화면 상태만으로 실행할 전략 명령을 결정합니다.
        public static WIAdministrationShortcutAction ResolveShortcutAction(KeyCode keyCode, bool modalOpen,
            bool castleOpen, bool campaignStartOpen, bool textInputFocused)
        {
            if (keyCode == KeyCode.Escape)
            {
                if (modalOpen)
                {
                    return WIAdministrationShortcutAction.CloseModal;
                }
                if (castleOpen)
                {
                    return WIAdministrationShortcutAction.ReturnToGlobal;
                }
                return WIAdministrationShortcutAction.None;
            }
            if (campaignStartOpen || modalOpen || castleOpen || textInputFocused)
            {
                return WIAdministrationShortcutAction.None;
            }
            if (keyCode == KeyCode.M) return WIAdministrationShortcutAction.Military;
            if (keyCode == KeyCode.H) return WIAdministrationShortcutAction.Heroes;
            if (keyCode == KeyCode.D) return WIAdministrationShortcutAction.Diplomacy;
            if (keyCode == KeyCode.S) return WIAdministrationShortcutAction.Scheme;
            if (keyCode == KeyCode.R) return WIAdministrationShortcutAction.Research;
            if (keyCode == KeyCode.G) return WIAdministrationShortcutAction.Faction;
            if (keyCode == KeyCode.C) return WIAdministrationShortcutAction.Council;
            if (keyCode == KeyCode.L) return WIAdministrationShortcutAction.MonthlyReport;
            if (keyCode == KeyCode.T) return WIAdministrationShortcutAction.EndTurn;
            return WIAdministrationShortcutAction.None;
        }
    }
}
