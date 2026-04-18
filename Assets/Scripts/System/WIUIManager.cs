using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ProjectWI.SubSystem
{
    public class WIUIManager : MonoBehaviour
    {
        public static WIUIManager Instance { get; private set; }
        
        [SerializeField] private ScrollRect mainScrollRect;
        [SerializeField] private Button[] menuButtons;
        
        [Header("Combat")]
        [SerializeField] private GameObject combatPage;
        [SerializeField] private Button btnDungeon1;
        [SerializeField] private Button btnCombatBack;
        
        private float[] pagePositions = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
        private Coroutine scrollCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            for (int i = 0; i < menuButtons.Length; i++)
            {
                int index = i;
                if (menuButtons[i] != null)
                {
                    menuButtons[i].onClick.AddListener(() => OnMenuButtonClicked(index));
                }
            }

            if (btnDungeon1 != null)
            {
                btnDungeon1.onClick.AddListener(() => {
                    if (WIPartySelectionManager.Instance != null)
                    {
                        WIPartySelectionManager.Instance.OpenPartyPanel();
                    }
                    else
                    {
                        ShowCombatPage();
                    }
                });
            }
            
            if (btnCombatBack != null)
            {
                btnCombatBack.onClick.AddListener(HideCombatPage);
            }
        }

        private void OnMenuButtonClicked(int index)
        {
            if (index < 0 || index >= pagePositions.Length)
            {
                return;
            }
            
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
            }
            scrollCoroutine = StartCoroutine(SmoothScroll(pagePositions[index]));
        }

        private IEnumerator SmoothScroll(float targetPosition)
        {
            float time = 0;
            float duration = 0.25f;
            float startPosition = mainScrollRect.horizontalNormalizedPosition;

            while (time < duration)
            {
                time += Time.deltaTime;
                mainScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, time / duration);
                yield return null;
            }

            mainScrollRect.horizontalNormalizedPosition = targetPosition;
        }

        public void ShowCombatPage()
        {
            if (combatPage != null)
            {
                combatPage.SetActive(true);
                var session = WIBattleManager.Instance?.GetSession(1);
                if (session != null && WIBattleSubSystem.Instance != null)
                {
                    WIBattleSubSystem.Instance.BindSession(session);
                }
            }
        }

        public void HideCombatPage()
        {
            if (combatPage != null)
            {
                combatPage.SetActive(false);
                if (WIBattleSubSystem.Instance != null)
                {
                    WIBattleSubSystem.Instance.BindSession(null);
                }
            }
        }
    }
}
