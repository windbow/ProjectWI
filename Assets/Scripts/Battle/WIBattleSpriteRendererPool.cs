using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    public sealed class WIBattleSpriteRendererPool
    {
        // 새 표시 오브젝트가 필요할 때 복제할 원본 프리팹입니다.
        private readonly GameObject prefab;

        // 풀에서 관리하는 표시 오브젝트의 고정 부모입니다.
        private readonly Transform root;

        // 사용을 마치고 다음 요청을 기다리는 렌더러입니다.
        private readonly Stack<SpriteRenderer> inactiveRenderers = new Stack<SpriteRenderer>();

        // 현재 전투 화면에서 사용 중인 렌더러 수입니다.
        private int activeCount;

        // 프리팹 참조가 살아 있고 SpriteRenderer를 포함하는지 예외 없이 확인합니다.
        public bool IsValid
        {
            get
            {
                try
                {
                    return prefab != null && prefab.GetComponent<SpriteRenderer>() != null;
                }
                catch (MissingReferenceException)
                {
                    return false;
                }
            }
        }
        public int ActiveCount => activeCount;
        public int InactiveCount => inactiveRenderers.Count;

        // 원본 프리팹과 생성된 인스턴스를 정리할 부모를 연결합니다.
        public WIBattleSpriteRendererPool(GameObject sourcePrefab, Transform instanceRoot)
        {
            prefab = sourcePrefab;
            root = instanceRoot;
        }

        // 비활성 인스턴스를 우선 재사용하고 부족할 때만 프리팹을 새로 생성합니다.
        public SpriteRenderer Acquire()
        {
            if (IsValid == false)
            {
                return null;
            }

            SpriteRenderer renderer = inactiveRenderers.Count > 0
                ? inactiveRenderers.Pop()
                : Object.Instantiate(prefab, root).GetComponent<SpriteRenderer>();
            renderer.gameObject.SetActive(true);
            activeCount += 1;
            return renderer;
        }

        // 사용을 마친 렌더러를 초기 상태로 되돌려 다음 요청에서 재사용합니다.
        public void Release(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.gameObject.activeSelf == false)
            {
                return;
            }

            renderer.transform.SetParent(root, false);
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = Vector3.one;
            renderer.color = Color.white;
            renderer.gameObject.SetActive(false);
            inactiveRenderers.Push(renderer);
            activeCount = Mathf.Max(0, activeCount - 1);
        }
    }
}
