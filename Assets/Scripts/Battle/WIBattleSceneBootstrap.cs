using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEngine;

namespace ProjectWI.Battle
{
    public class WIBattleSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private WIBattleRuntimeController battleController;
        [SerializeField] private WIBattleHUDController hudController;

        // 캠페인 서비스의 대기 세션을 전투 런타임으로 변환하고 결과 복귀 이벤트를 연결합니다.
        private void Start()
        {
            WICampaignRuntimeService service = WICampaignRuntimeService.Instance;
            if (service == null || service.State == null)
            {
                Debug.LogError("전투 씬에 전달된 캠페인 상태가 없습니다.");
                return;
            }
            WIBattleSessionState session = service.State.BattleSessions.Find(item =>
                item.SessionId == service.PendingBattleSessionId && item.Status == WIBattleSessionStatus.InProgress);
            if (session == null)
            {
                Debug.LogError("진행할 전투 세션을 찾을 수 없습니다.");
                return;
            }
            WIBattleSide playerSide = session.AttackerFactionId == service.State.PlayerFactionId
                ? WIBattleSide.Attacker
                : WIBattleSide.Defender;
            hudController.SetPlayerSide(playerSide);
            battleController.BattleFinished += OnBattleFinished;
            battleController.Initialize(service.Database, session);
        }

        // 전투 런타임 승패를 캠페인 서비스에 전달해 전략 씬으로 복귀시킵니다.
        private void OnBattleFinished(WIBattleOutcome outcome)
        {
            battleController.BattleFinished -= OnBattleFinished;
            WICampaignRuntimeService.Instance.CompleteBattle(outcome);
        }
    }
}
