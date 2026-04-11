using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WIUIManager : MonoBehaviour
{
    [SerializeField] private ScrollRect mainScrollRect;
    [SerializeField] private Button[] menuButtons;
    
    [Header("Combat")]
    [SerializeField] private GameObject combatPage;
    [SerializeField] private Button btnDungeon1;
    [SerializeField] private Button btnCombatBack;
    
    private float[] pagePositions = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
    private Coroutine scrollCoroutine;

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
            btnDungeon1.onClick.AddListener(ShowCombatPage);
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
            var session = ProjectWI.SubSystem.WIBattleManager.Instance?.GetSession(1);
            if (session != null && ProjectWI.SubSystem.WIBattleSubSystem.Instance != null)
            {
                ProjectWI.SubSystem.WIBattleSubSystem.Instance.BindSession(session);
            }
        }
    }

    public void HideCombatPage()
    {
        if (combatPage != null)
        {
            combatPage.SetActive(false);
            if (ProjectWI.SubSystem.WIBattleSubSystem.Instance != null)
            {
                ProjectWI.SubSystem.WIBattleSubSystem.Instance.BindSession(null);
            }
        }
    }
}
