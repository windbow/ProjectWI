using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ProjectWI.SubSystem
{
    public class WIBattleSubSystem : MonoBehaviour
    {
        [Header("Characters")]
        public GameObject advSpot;
        public GameObject monSpot;
        
        private Text advHpText;
        private Text monHpText;
        
        [Header("Gauges")]
        public Slider advGauge;
        public Slider monGauge;
        
        [Header("Logs")]
        public RectTransform logContent;
        public GameObject logTextPrefab;
        
        private WIBattleSession boundSession;
        public static WIBattleSubSystem Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (advSpot != null)
                advHpText = advSpot.GetComponentInChildren<Text>(true);
            if (monSpot != null)
                monHpText = monSpot.GetComponentInChildren<Text>(true);
            
            if (logTextPrefab != null)
                logTextPrefab.SetActive(false); // 템플릿용 비활성화
        }

        public void BindSession(WIBattleSession session)
        {
            if (boundSession != null)
            {
                boundSession.OnLogAdded -= HandleLogAdded;
            }
            
            boundSession = session;
            if (boundSession != null)
            {
                SetCharactersActive(true);
                boundSession.OnLogAdded += HandleLogAdded;
                
                // 기존 로그 청소
                if (logContent != null)
                {
                    for (int i = 0; i < logContent.childCount; i++)
                    {
                        Transform child = logContent.GetChild(i);
                        if (child.gameObject != logTextPrefab)
                            Destroy(child.gameObject);
                    }
                }
                
                // 새 세션 데이터 복원
                foreach (string log in boundSession.GetLogs())
                {
                    HandleLogAdded(log);
                }
            }
            else
            {
                SetCharactersActive(false);
            }
        }

        private void SetCharactersActive(bool active)
        {
            if (advSpot != null && advSpot.transform.childCount > 0)
                advSpot.transform.GetChild(0).gameObject.SetActive(active);
                
            if (monSpot != null && monSpot.transform.childCount > 0)
                monSpot.transform.GetChild(0).gameObject.SetActive(active);
        }

        private void Update()
        {
            if (boundSession == null) return;

            if (advHpText != null)
                advHpText.text = $"HP: {boundSession.AdvCurrentHp}/{boundSession.AdvMaxHp}";
            if (monHpText != null)
                monHpText.text = $"HP: {boundSession.MonCurrentHp}/{boundSession.MonMaxHp}";

            if (advGauge != null)
                advGauge.value = boundSession.AdvTurnProgress / 100f;
            if (monGauge != null)
                monGauge.value = boundSession.MonTurnProgress / 100f;
        }

        private void HandleLogAdded(string logMsg)
        {
            if (logTextPrefab == null || logContent == null) return;
            
            GameObject newLog = Instantiate(logTextPrefab, logContent);
            newLog.SetActive(true);
            newLog.GetComponent<Text>().text = logMsg;
        }
        
        private void OnDestroy()
        {
            if (boundSession != null)
            {
                boundSession.OnLogAdded -= HandleLogAdded;
            }
        }
    }
}
