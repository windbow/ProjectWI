using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace ProjectWI.SubSystem
{
    public class WIBattleSubSystem : MonoBehaviour
    {
        [Header("Characters")]
        /// <summary>아군(모험가) UI 슬롯들이 배치된 부모 레이아웃 객체</summary>
        public GameObject advSpot;
        /// <summary>적군(몬스터) UI 슬롯들이 배치된 부모 레이아웃 객체</summary>
        public GameObject monSpot;

        /// <summary>씬에 미리 생성된 아군 슬롯의 WIBattleCharacterSlot 컴포넌트 풀 (최대 8개)</summary>
        private List<WIBattleCharacterSlot> advSlots = new List<WIBattleCharacterSlot>();
        /// <summary>씬에 미리 생성된 적군 슬롯의 WIBattleCharacterSlot 컴포넌트 풀 (최대 5개)</summary>
        private List<WIBattleCharacterSlot> monSlots = new List<WIBattleCharacterSlot>();

        [Header("Gauges")]
        /// <summary>탐험 진척도 또는 전투 턴 대기시간을 표시할 게이지 슬라이더</summary>
        public Slider advGauge;
        /// <summary>(현재 단일 게이지 구조로 미사용) 적군용 게이지 슬라이더</summary>
        public Slider monGauge;

        [Header("Logs")]
        /// <summary>로그 텍스트들이 세로로 쌓일 뷰포트 내 Content 레이아웃</summary>
        public RectTransform logContent;
        /// <summary>새 로그가 들어올 때 복제하여 사용할 텍스트 UI 프리팹</summary>
        public GameObject logTextPrefab;

        /// <summary>현재 UI가 관측 중인 던전 런타임 세션</summary>
        private WIDungeonSession boundSession;
        /// <summary>이전 프레임의 던전 상태 - 탐험↔전투 전환 감지용</summary>
        private WIDungeonState lastKnownState = WIDungeonState.Exploring;
        /// <summary>외부에서 접근하기 위한 싱글톤 인스턴스</summary>
        public static WIBattleSubSystem Instance { get; private set; }

        /// <summary>
        /// 싱글톤 등록 및 advSpot/monSpot 자식들에서 WIBattleCharacterSlot을 캐싱하고 전부 비활성화합니다.
        /// Awake에서 처리하는 이유: WIUIManager가 SetActive(true) 직후 같은 프레임에 BindSession()을 호출하므로
        /// Start()로 미루면 슬롯 준비 전에 RebuildCharacterUI()가 실행됩니다.
        /// </summary>
        private void Awake()
        {
            Instance = this;

            if (advSpot != null)
            {
                foreach (Transform child in advSpot.transform)
                {
                    WIBattleCharacterSlot slot = child.GetComponent<WIBattleCharacterSlot>();
                    if (slot != null)
                    {
                        advSlots.Add(slot);
                    }
                    child.gameObject.SetActive(false);
                }
            }

            if (monSpot != null)
            {
                foreach (Transform child in monSpot.transform)
                {
                    WIBattleCharacterSlot slot = child.GetComponent<WIBattleCharacterSlot>();
                    if (slot != null)
                    {
                        monSlots.Add(slot);
                    }
                    child.gameObject.SetActive(false);
                }
            }

            if (logTextPrefab != null)
            {
                logTextPrefab.SetActive(false);
            }
        }

        /// <summary>
        /// 특정 던전 세션을 UI에 바인딩합니다.
        /// 기존 세션을 해제하고, 로그를 복원하며, 캐릭터 슬롯을 갱신합니다.
        /// null을 전달하면 UI 연결을 해제합니다.
        /// </summary>
        public void BindSession(WIDungeonSession session)
        {
            if (boundSession != null)
            {
                boundSession.OnDungeonLogAdded -= HandleLogAdded;
            }

            boundSession = session;

            if (boundSession != null)
            {
                boundSession.OnDungeonLogAdded += HandleLogAdded;

                if (logContent != null)
                {
                    for (int i = logContent.childCount - 1; i >= 0; i--)
                    {
                        Transform child = logContent.GetChild(i);
                        if (child.gameObject != logTextPrefab)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }

                RebuildCharacterUI();

                foreach (string log in boundSession.GetLogs())
                {
                    HandleLogAdded(log);
                }
            }
            else
            {
                ClearCharacterUI();
            }
        }

        /// <summary>
        /// 세션의 아군 수만큼 슬롯을 활성화하고 Bind()합니다.
        /// 몬스터는 던전이 전투 상태(Battling)일 때만 표시합니다.
        /// </summary>
        private void RebuildCharacterUI()
        {
            ClearCharacterUI();

            if (boundSession == null || boundSession.BattlePhase == null)
            {
                return;
            }

            lastKnownState = boundSession.CurrentState;

            var adventurers = boundSession.BattlePhase.Adventurers;
            for (int i = 0; i < adventurers.Count && i < advSlots.Count; i++)
            {
                advSlots[i].gameObject.SetActive(true);
                advSlots[i].Bind(adventurers[i], boundSession.BattlePhase, true);
            }

            if (boundSession.CurrentState == WIDungeonState.Battling)
            {
                ShowMonsterSlots();
            }
        }

        /// <summary>
        /// 몬스터 슬롯을 활성화하고 Bind()합니다. 전투 진입 시 호출됩니다.
        /// </summary>
        private void ShowMonsterSlots()
        {
            if (boundSession == null || boundSession.BattlePhase == null)
            {
                return;
            }

            var monsters = boundSession.BattlePhase.Monsters;
            for (int i = 0; i < monsters.Count && i < monSlots.Count; i++)
            {
                monSlots[i].gameObject.SetActive(true);
                monSlots[i].Bind(monsters[i], boundSession.BattlePhase, false);
            }
        }

        /// <summary>
        /// 모든 몬스터 슬롯을 Unbind하고 비활성화합니다. 탐험 복귀 시 호출됩니다.
        /// </summary>
        private void HideMonsterSlots()
        {
            foreach (WIBattleCharacterSlot slot in monSlots)
            {
                if (slot.gameObject.activeSelf)
                {
                    slot.Unbind();
                    slot.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 모든 아군/적군 슬롯을 Unbind하고 비활성화합니다.
        /// </summary>
        private void ClearCharacterUI()
        {
            foreach (WIBattleCharacterSlot slot in advSlots)
            {
                slot.Unbind();
                slot.gameObject.SetActive(false);
            }
            foreach (WIBattleCharacterSlot slot in monSlots)
            {
                slot.Unbind();
                slot.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 탐험↔전투 상태 전환을 감지하여 몬스터 슬롯을 토글하고,
        /// 개별 사망한 몬스터 슬롯을 비활성화하며, 게이지를 갱신합니다.
        /// HP/행동력 바 갱신은 각 WIBattleCharacterSlot의 Update()가 담당합니다.
        /// </summary>
        private void Update()
        {
            if (boundSession == null || boundSession.BattlePhase == null)
            {
                return;
            }

            // 탐험↔전투 상태 전환 감지
            if (lastKnownState != boundSession.CurrentState)
            {
                lastKnownState = boundSession.CurrentState;
                if (boundSession.CurrentState == WIDungeonState.Battling)
                {
                    ShowMonsterSlots();
                }
                else
                {
                    HideMonsterSlots();
                }
            }

            // 개별 몬스터 사망 감지 → 해당 슬롯만 비활성화
            if (boundSession.CurrentState == WIDungeonState.Battling)
            {
                for (int i = 0; i < monSlots.Count; i++)
                {
                    if (false == monSlots[i].gameObject.activeSelf)
                    {
                        continue;
                    }

                    if (monSlots[i].IsDead())
                    {
                        monSlots[i].Unbind();
                        monSlots[i].gameObject.SetActive(false);
                    }
                }
            }

            // 던전 탐험/전투 게이지 갱신
            if (advGauge != null)
            {
                advGauge.value = boundSession.GetExploreProgressRatio();
            }

            if (monGauge != null && monGauge.gameObject.activeSelf)
            {
                monGauge.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 전투 이벤트가 발생했을 때 배틀 로그에 텍스트를 추가합니다.
        /// 로그가 100개를 초과하면 가장 오래된 항목을 삭제하여 부하를 방지합니다.
        /// </summary>
        private void HandleLogAdded(string logMsg)
        {
            if (logTextPrefab == null || logContent == null)
            {
                return;
            }

            GameObject newLog = Instantiate(logTextPrefab, logContent);
            newLog.SetActive(true);
            newLog.GetComponent<Text>().text = logMsg;

            int activeChildCount = 0;
            for (int i = 0; i < logContent.childCount; i++)
            {
                if (logContent.GetChild(i).gameObject.activeSelf)
                {
                    activeChildCount++;
                }
            }

            if (activeChildCount > 100)
            {
                for (int i = 0; i < logContent.childCount; i++)
                {
                    Transform child = logContent.GetChild(i);
                    if (child.gameObject != logTextPrefab)
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }

            StartCoroutine(ScrollToBottom());
        }

        /// <summary>
        /// 새 로그 추가 후 다음 프레임에 스크롤을 최하단으로 이동시킵니다.
        /// </summary>
        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();

            ScrollRect scrollRect = logContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnDestroy()
        {
            if (boundSession != null)
            {
                boundSession.OnDungeonLogAdded -= HandleLogAdded;
            }
        }
    }
}
