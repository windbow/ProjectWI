using System.Collections;
using System.IO;
using ProjectWI.Administration;
using ProjectWI.Battle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectWI.Systems
{
    public sealed class WIWindowsBuildSmokeTest : MonoBehaviour
    {
        private const string CommandLineFlag = "-wi-smoke-test";
        private enum SmokePhase { Preparing, WaitingBattle, WaitingStrategy, Finished }

        private SmokePhase phase;
        private WICampaignRuntimeService runtime;
        private string smokePath;
        private int expectedTurn;
        private int expectedCastleCount;
        private float startedAt;

        // 전용 명령줄 인자가 있을 때만 실제 Player 환경의 자동 검증기를 생성합니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateWhenRequested()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), CommandLineFlag) < 0) return;
            GameObject runner = new GameObject(nameof(WIWindowsBuildSmokeTest));
            DontDestroyOnLoad(runner);
            runner.AddComponent<WIWindowsBuildSmokeTest>();
        }

        // 런타임 서비스 초기화를 기다린 뒤 새 캠페인·턴·저장·불러오기와 전투 씬 포함 여부를 검증합니다.
        private IEnumerator Start()
        {
            yield return null;
            startedAt = Time.realtimeSinceStartup;
            smokePath = Path.Combine(Application.persistentDataPath, "ProjectWI_BuildSmoke.json");
            try
            {
                runtime = WICampaignRuntimeService.Instance;
                if (runtime == null || runtime.Database == null)
                    throw new System.InvalidOperationException("캠페인 런타임 또는 데이터베이스가 없습니다.");
                if (Application.CanStreamedLevelBeLoaded("MainScene") == false ||
                    Application.CanStreamedLevelBeLoaded("BattleScene") == false)
                    throw new System.InvalidOperationException("빌드에 필요한 전략 또는 전투 씬이 없습니다.");

                WIAdministrationState state = runtime.StartNewCampaign(WICampaignDifficulty.Standard);
                if (state == null) throw new System.InvalidOperationException("새 캠페인을 만들지 못했습니다.");
                WIAdministrationTurnSystem.ExecuteTurn(runtime.Database, state);
                expectedTurn = state.Turn;
                expectedCastleCount = state.Castles.Count;
                if (WICampaignSaveSystem.SaveFile(smokePath, state, out string saveError) == false)
                    throw new System.InvalidOperationException($"저장 실패: {saveError}");
                if (WICampaignSaveSystem.LoadFile(smokePath, out WIAdministrationState loaded, out string loadError) == false)
                    throw new System.InvalidOperationException($"불러오기 실패: {loadError}");
                if (loaded.Turn != expectedTurn || loaded.Castles.Count != state.Castles.Count)
                    throw new System.InvalidOperationException("저장 왕복 후 핵심 캠페인 상태가 일치하지 않습니다.");

                WIBattleSessionState battleSession = CreateTestBattleSession();
                if (runtime.StartTestBattle(battleSession) == false)
                    throw new System.InvalidOperationException("테스트 전투 진입을 시작하지 못했습니다.");
                phase = SmokePhase.WaitingBattle;
            }
            catch (System.Exception exception)
            {
                Fail(exception);
            }
        }

        // 비동기 씬 전환이 끝난 프레임에서 전투 초기화와 전략 화면 복귀를 순서대로 확인합니다.
        private void Update()
        {
            if (phase == SmokePhase.Preparing || phase == SmokePhase.Finished) return;
            if (Time.realtimeSinceStartup - startedAt > 30f)
            {
                Fail(new System.TimeoutException($"통합 검증 시간 초과: {phase}"));
                return;
            }
            try
            {
                if (phase == SmokePhase.WaitingBattle && SceneManager.GetActiveScene().name == "BattleScene")
                {
                    WIBattleRuntimeController battle = FindFirstObjectByType<WIBattleRuntimeController>();
                    if (battle == null || battle.Runtime == null || battle.Runtime.Characters.Count != 2) return;
                    battle.SimulateStep(0.1f);
                    if (runtime.CompleteBattle(WIBattleOutcome.Victory) == false)
                        throw new System.InvalidOperationException("전투 결과 복귀를 시작하지 못했습니다.");
                    phase = SmokePhase.WaitingStrategy;
                }
                else if (phase == SmokePhase.WaitingStrategy && SceneManager.GetActiveScene().name == "MainScene")
                {
                    if (FindFirstObjectByType<WIAdministrationUIController>() == null)
                        throw new System.InvalidOperationException("전투 후 전략 UI를 복구하지 못했습니다.");
                    Pass();
                }
            }
            catch (System.Exception exception)
            {
                Fail(exception);
            }
        }

        // 성공 결과와 핵심 캠페인 수치를 기록하고 임시 파일을 정리한 뒤 정상 종료합니다.
        private void Pass()
        {
            phase = SmokePhase.Finished;
            CleanupSmokeFiles();
            Debug.Log($"[WI_BUILD_SMOKE_PASS] 턴 {expectedTurn} · 성 {expectedCastleCount} · " +
                      "저장/불러오기 · 전투 진입/복귀 정상");
            Application.Quit(0);
        }

        // 실패 원인을 로그에 남기고 임시 파일을 정리한 뒤 오류 코드로 종료합니다.
        private void Fail(System.Exception exception)
        {
            phase = SmokePhase.Finished;
            CleanupSmokeFiles();
            Debug.LogError($"[WI_BUILD_SMOKE_FAIL] {exception}");
            Application.Quit(1);
        }

        // 스모크 테스트가 만든 주 저장·백업·임시 파일만 제거합니다.
        private void CleanupSmokeFiles()
        {
            if (string.IsNullOrEmpty(smokePath)) return;
            if (File.Exists(smokePath)) File.Delete(smokePath);
            if (File.Exists(smokePath + ".bak")) File.Delete(smokePath + ".bak");
            if (File.Exists(smokePath + ".tmp")) File.Delete(smokePath + ".tmp");
        }

        // 실제 전투 씬과 임시 도형 인물 두 명을 초기화할 최소 테스트 세션을 구성합니다.
        private static WIBattleSessionState CreateTestBattleSession()
        {
            WIBattleSessionState session = new WIBattleSessionState
            {
                SessionId = "windows_integration_smoke",
                CastleId = "castle_00",
                AttackerFactionId = "avalon",
                DefenderFactionId = "valdor",
                PlayerInvolved = true,
                Status = WIBattleSessionStatus.Pending
            };
            session.AttackerHeroIds.Add(new WIBattleParticipantState
            {
                HeroId = "ares", ArmyId = "smoke_allies", Role = WIUnitRole.Melee
            });
            session.DefenderHeroIds.Add(new WIBattleParticipantState
            {
                HeroId = "lyria", ArmyId = "smoke_enemies", Role = WIUnitRole.Ranged
            });
            return session;
        }
    }
}
