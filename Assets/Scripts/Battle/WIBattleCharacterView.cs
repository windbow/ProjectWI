using UnityEngine;

namespace ProjectWI.Battle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WIBattleCharacterView : MonoBehaviour
    {
        private WIBattleCharacterState state;
        private SpriteRenderer spriteRenderer;

        // 전투 캐릭터 상태를 표시 오브젝트와 연결하고 임시 진영 색상을 적용합니다.
        public void Bind(WIBattleCharacterState characterState, Sprite placeholderSprite)
        {
            state = characterState;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = placeholderSprite;
            spriteRenderer.color = state.Side == WIBattleSide.Attacker
                ? new Color(0.75f, 0.2f, 0.18f)
                : new Color(0.18f, 0.42f, 0.75f);
            Refresh();
        }

        // 런타임 좌표와 생존 상태를 Transform과 렌더러에 반영합니다.
        public void Refresh()
        {
            if (state == null) return;
            transform.position = new Vector3(state.Position.x, state.Position.y, 0f);
            gameObject.SetActive(state.IsAlive);
        }
    }
}
