using ProjectWI.Administration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectWI.Systems
{
    public class WICampaignRuntimeService : MonoBehaviour
    {
        [SerializeField] private WIAdministrationDatabaseSO database;
        [SerializeField] private string strategySceneName = "MainScene";
        [SerializeField] private string battleSceneName = "BattleScene";
        [SerializeField] private bool autoSaveEnabled = true;

        public static WICampaignRuntimeService Instance { get; private set; }
        public WIAdministrationDatabaseSO Database => database;
        public WIAdministrationState State { get; private set; }
        public string PendingBattleSessionId { get; private set; }
        public bool AutoSaveEnabled => autoSaveEnabled;
        public bool HasCampaignStarted { get; private set; }
        public bool IsTestBattle { get; private set; }

        // 씬 전환 중에도 단일 캠페인 상태를 유지하는 서비스 인스턴스를 초기화합니다.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (database != null && State == null)
            {
                State = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            }
        }

        // 서비스 오브젝트가 제거될 때 정적 참조가 폐기된 인스턴스를 가리키지 않도록 정리합니다.
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 데이터베이스가 아직 지정되지 않은 경우 전달받은 데이터로 캠페인을 생성합니다.
        public WIAdministrationState GetOrCreateState(WIAdministrationDatabaseSO sourceDatabase)
        {
            if (database == null)
            {
                database = sourceDatabase;
            }
            if (State == null && database != null)
            {
                State = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            }
            return State;
        }

        // 선택한 전투 세션을 진행 상태로 바꾸고 전투 씬을 비동기로 불러옵니다.
        public bool StartBattle(string sessionId)
        {
            if (State == null || WIAdministrationTurnSystem.BeginRealTimeBattle(State, sessionId) == false)
            {
                return false;
            }
            PendingBattleSessionId = sessionId;
            SceneManager.LoadSceneAsync(battleSceneName);
            return true;
        }

        // 에디터 전투 테스트 랩이 편성한 임시 세션을 캠페인 결과와 분리해 실행합니다.
        public bool StartTestBattle(WIBattleSessionState session)
        {
            if (database == null || session == null || session.AttackerHeroIds.Count == 0 || session.DefenderHeroIds.Count == 0)
            {
                Debug.LogError("테스트 전투 시작 실패: 데이터베이스 또는 양측 편성이 없습니다.");
                return false;
            }
            // 테스트 전장 소속은 기본 시나리오 배치와 분리하고 일반 전투의 아군 공격 금지 규칙은 유지합니다.
            WIAdministrationState testState = WIAdministrationState.Create(database);
            WICastleRuntimeState battlefield = testState.GetCastle(session.CastleId);
            if (battlefield == null || database.GetFaction(session.AttackerFactionId) == null ||
                database.GetFaction(session.DefenderFactionId) == null ||
                session.AttackerFactionId == session.DefenderFactionId || session.Status != WIBattleSessionStatus.Pending ||
                Application.CanStreamedLevelBeLoaded(battleSceneName) == false)
            {
                Debug.LogError("테스트 전투 시작 실패: 전장·양측 세력·대기 상태 또는 BattleScene 빌드 등록을 확인하세요.");
                return false;
            }
            battlefield.FactionId = session.DefenderFactionId;
            State = testState;
            State.BattleSessions.Add(session);
            HasCampaignStarted = false;
            IsTestBattle = true;
            return StartBattle(session.SessionId);
        }

        // 전투 결과를 캠페인에 반영하고 전략 화면으로 비동기 복귀합니다.
        public bool CompleteBattle(
            WIBattleOutcome attackerOutcome,
            bool attackerRetreated = false,
            bool defenderRetreated = false)
        {
            if (State == null || string.IsNullOrEmpty(PendingBattleSessionId))
            {
                return false;
            }
            if (IsTestBattle)
            {
                PendingBattleSessionId = string.Empty;
                IsTestBattle = false;
                SceneManager.LoadSceneAsync(strategySceneName);
                return true;
            }
            WITurnSummary summary = State.LastMonthlyReport ?? new WITurnSummary();
            WIBattleSessionState session = State.BattleSessions.Find(item => item.SessionId == PendingBattleSessionId);
            if (session != null)
            {
                session.AttackerRetreated = attackerRetreated;
                session.DefenderRetreated = defenderRetreated;
            }
            bool applied = WIAdministrationTurnSystem.SubmitBattleResult(
                database, State, PendingBattleSessionId, attackerOutcome,
                WIBattleResolutionSource.RealTimeBattle, summary);
            if (applied == false)
            {
                return false;
            }
            State.LastMonthlyReport = summary;
            PendingBattleSessionId = string.Empty;
            AutoSave();
            SceneManager.LoadSceneAsync(strategySceneName);
            return true;
        }

        // 테스트나 새 캠페인 시작 시 보존 중인 상태를 새 상태로 교체합니다.
        public void ResetCampaign()
        {
            State = database == null ? null : WIAdministrationState.Create(
                database, WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            PendingBattleSessionId = string.Empty;
            HasCampaignStarted = false;
            IsTestBattle = false;
        }

        // 선택한 난이도로 새 캠페인 상태를 만들고 진행 중 상태로 전환합니다.
        public WIAdministrationState StartNewCampaign(WICampaignDifficulty difficulty,
            WICampaignVariant variant = WICampaignVariant.AresMain)
        {
            State = database == null ? null : WIAdministrationState.Create(database, difficulty, variant);
            PendingBattleSessionId = string.Empty;
            HasCampaignStarted = State != null;
            IsTestBattle = false;
            return State;
        }

        // 번호 슬롯에 현재 캠페인 상태를 저장합니다.
        public bool SaveSlot(int slot, out string error)
        {
            return WICampaignSaveSystem.SaveFile(GetSavePath(slot), State, out error);
        }

        // 번호 슬롯에서 캠페인을 불러와 현재 상태 인스턴스를 교체합니다.
        public bool LoadSlot(int slot, out string error)
        {
            if (WICampaignSaveSystem.LoadFile(GetSavePath(slot), out WIAdministrationState loaded, out error) == false)
            {
                return false;
            }
            State = loaded;
            State.EnsureRuntimeCastleMap(database);
            State.SynchronizeCharacterTraits(database);
            foreach (WICharacterRuntimeState character in State.Characters)
            {
                WIHeroDefinition definition = database.GetHero(character.HeroId);
                if (definition != null && definition.Grade == WICharacterGrade.Hero)
                {
                    character.BaseGrade = WICharacterGrade.Hero;
                }
            }
            PendingBattleSessionId = string.Empty;
            HasCampaignStarted = true;
            IsTestBattle = false;
            return true;
        }

        // 자동 저장이 활성화된 경우 전용 0번 슬롯에 현재 상태를 기록합니다.
        public bool AutoSave()
        {
            if (autoSaveEnabled == false || State == null)
            {
                return false;
            }
            return SaveSlot(0, out _);
        }

        // 슬롯에 저장 파일이 존재하는지 확인합니다.
        public bool HasSave(int slot)
        {
            return System.IO.File.Exists(GetSavePath(slot));
        }

        // 안전한 고정 번호 슬롯의 영속 저장 경로를 반환합니다.
        private string GetSavePath(int slot)
        {
            int safeSlot = Mathf.Clamp(slot, 0, 3);
            return System.IO.Path.Combine(Application.persistentDataPath, $"ProjectWI_Save_{safeSlot}.json");
        }
    }
}
