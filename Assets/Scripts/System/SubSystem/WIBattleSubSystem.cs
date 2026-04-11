using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace ProjectWI.SubSystem
{
    public class WIBattleSubSystem : MonoBehaviour
    {
        [Header("Characters")]
        public GameObject advSpot;
        public GameObject monSpot;
        
        private GameObject advTemplate;
        private GameObject monTemplate;
        
        private List<Text> advHpTexts = new List<Text>();
        private List<Text> monHpTexts = new List<Text>();
        
        [Header("Gauges")]
        public Slider advGauge;
        public Slider monGauge;
        
        [Header("Logs")]
        public RectTransform logContent;
        public GameObject logTextPrefab;
        
        private WIDungeonSession boundSession;
        public static WIBattleSubSystem Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Scene에 배치된 첫 번째 자식 객체들을 다중 UI 복제를 위한 '템플릿(원형)' 프리팹으로 취급하여 캐싱한 후 숨깁니다.
        /// 전투 기록 템플릿도 마찬가지로 캐싱 및 비활성화합니다.
        /// </summary>
        private void Start()
        {
            if (advSpot != null && advSpot.transform.childCount > 0)
            {
                advTemplate = advSpot.transform.GetChild(0).gameObject;
                advTemplate.SetActive(false);
            }
            if (monSpot != null && monSpot.transform.childCount > 0)
            {
                monTemplate = monSpot.transform.GetChild(0).gameObject;
                monTemplate.SetActive(false);
            }
            
            if (logTextPrefab != null)
                logTextPrefab.SetActive(false); 
        }

        /// <summary>
        /// 눈앞의 시야(Battle Page UI)에 표시할 특정 던전의 전투 세션을 바인딩합니다.
        /// 기존 세션 연결을 해제하고, 새로운 세션의 밀린 로그를 채우고, 캐릭터 수만큼 UI 그리드를 재생산합니다.
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
                
                // Clear old logs
                if (logContent != null)
                {
                    for (int i = 0; i < logContent.childCount; i++)
                    {
                        Transform child = logContent.GetChild(i);
                        if (child.gameObject != logTextPrefab)
                            Destroy(child.gameObject);
                    }
                }
                
                RebuildCharacterUI();

                // Restore logs
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
        /// 바인딩된 전투 세션의 개별 모험가와 몬스터 숫자만큼 캐릭터 HP UI 프리팹 템플릿을 복제(Instantiate)하여 화면에 늘려놓습니다.
        /// </summary>
        private void RebuildCharacterUI()
        {
            ClearCharacterUI();
            
            if (boundSession == null || boundSession.BattlePhase == null) return;

            if (advTemplate != null)
            {
                foreach (var adv in boundSession.BattlePhase.Adventurers)
                {
                    GameObject go = Instantiate(advTemplate, advSpot.transform);
                    go.SetActive(true);
                    Text t = go.GetComponentInChildren<Text>();
                    if (t != null) advHpTexts.Add(t);
                }
            }

            if (monTemplate != null)
            {
                foreach (var mon in boundSession.BattlePhase.Monsters)
                {
                    GameObject go = Instantiate(monTemplate, monSpot.transform);
                    go.SetActive(true);
                    Text t = go.GetComponentInChildren<Text>();
                    if (t != null) monHpTexts.Add(t);
                }
            }
        }

        /// <summary>
        /// 동적으로 복제되었던 모든 UI 자식 객체들을 파괴하고 리스트를 비웁니다. 템플릿 원형(Template)은 건드리지 않습니다.
        /// </summary>
        private void ClearCharacterUI()
        {
            if (advSpot != null)
            {
                foreach (Transform child in advSpot.transform)
                {
                    if (child.gameObject != advTemplate) Destroy(child.gameObject);
                }
            }
            if (monSpot != null)
            {
                foreach (Transform child in monSpot.transform)
                {
                    if (child.gameObject != monTemplate) Destroy(child.gameObject);
                }
            }
            advHpTexts.Clear();
            monHpTexts.Clear();
        }

        /// <summary>
        /// 매 프레임 UI 상에서 바인딩된 백그라운드 세션의 최신 체력 및 단일 게이지 진행률 렌더링 수치를 가져와 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (boundSession == null || boundSession.BattlePhase == null) return;

            for (int i = 0; i < boundSession.BattlePhase.Adventurers.Count && i < advHpTexts.Count; i++)
            {
                var ch = boundSession.BattlePhase.Adventurers[i];
                advHpTexts[i].text = $"{ch.Name}\nHP: {ch.CurrentHp}/{ch.MaxHp}";
            }

            for (int i = 0; i < boundSession.BattlePhase.Monsters.Count && i < monHpTexts.Count; i++)
            {
                var ch = boundSession.BattlePhase.Monsters[i];
                monHpTexts[i].text = $"{ch.Name}\nHP: {ch.CurrentHp}/{ch.MaxHp}";
            }

            if (advGauge != null)
                advGauge.value = boundSession.GetExploreProgressRatio();
            if (monGauge != null && monGauge.gameObject.activeSelf)
                monGauge.gameObject.SetActive(false);
        }

        /// <summary>
        /// 전투 중에 스킬 시전, 회피 등의 이벤트가 발생할 때 호출되어 배틀 로그에 텍스트 프리팹을 붙여 시각적으로 표시합니다.
        /// 쌓인 로그가 100개를 초과할 경우 가장 오래된 로그 UI를 삭제하여 부하를 방지합니다.
        /// </summary>
        private void HandleLogAdded(string logMsg)
        {
            if (logTextPrefab == null || logContent == null) return;
            
            GameObject newLog = Instantiate(logTextPrefab, logContent);
            newLog.SetActive(true);
            newLog.GetComponent<Text>().text = logMsg;

            // UI 오브젝트 최적화 (100개 초과 시 오래된 텍스트 삭제)
            int activeChildCount = 0;
            for (int i = 0; i < logContent.childCount; i++)
            {
                if (logContent.GetChild(i).gameObject.activeSelf)
                    activeChildCount++;
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
            
            // 자동 스크롤 코루틴 실행
            StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            // 프레임이 끝날 때까지 대기하여 UI 레이아웃 크기 리빌딩이 완료되도록 함
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
