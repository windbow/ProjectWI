using UnityEngine;

namespace ProjectWI.Administration
{
    public sealed class WIUIScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] screenPrefabs;

        // 등록된 완성 UGUI 프리팹을 플레이 시작 때 정리 루트 아래에 생성합니다.
        private void Awake()
        {
            foreach (GameObject prefab in screenPrefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                GameObject screen = Instantiate(prefab, transform);
                screen.SetActive(false);
            }
        }
    }
}
