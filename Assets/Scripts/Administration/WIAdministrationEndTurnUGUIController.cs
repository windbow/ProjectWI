using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationEndTurnUGUIController : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        // 공용 다음 턴 버튼의 내부 참조를 준비합니다.
        private void Awake()
        {
            ResolveReferences();
        }

        // 공용 버튼을 행정 UI의 다음 턴 기능과 연결합니다.
        public void Initialize(WIAdministrationUIController administrationController)
        {
            ResolveReferences();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
                administrationController.ExecuteUGUIShortcut(WIAdministrationShortcutAction.EndTurn));
        }

        // 다음 턴 가능 상태와 표시 문구를 함께 적용합니다.
        public void Apply(bool interactable, string text)
        {
            ResolveReferences();
            button.interactable = interactable;
            label.text = text;
        }

        // 프리팹에 고정 배치된 버튼과 라벨 참조를 확인합니다.
        private void ResolveReferences()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }
        }
    }
}
