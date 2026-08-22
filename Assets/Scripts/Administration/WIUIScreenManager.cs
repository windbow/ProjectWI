using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIUIScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] screenPrefabs;

        // 등록된 완성 UGUI 프리팹을 생성하고 각 화면이 자체 표시 상태와 이벤트 구독을 초기화하게 합니다.
        private void Awake()
        {
            foreach (GameObject prefab in screenPrefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                GameObject screen = Instantiate(prefab, transform);
#if UNITY_EDITOR
                // 플레이마다 다시 생성되는 전체 화면 Canvas 루트만 Scene 피킹에서 제외하고 자식 UI 선택은 유지합니다.
                UnityEditor.SceneVisibilityManager.instance.DisablePicking(screen, false);
#endif
            }
        }
    }
}
