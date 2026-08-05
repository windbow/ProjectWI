using UnityEngine;

namespace ProjectWI.Battle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WIBattleCharacterView : MonoBehaviour
    {
        private WIBattleCharacterState state;
        private SpriteRenderer spriteRenderer;
        private Transform healthFill;
        private GameObject focusMarker;
        private GameObject selectionMarker;

        public string HeroId => state?.HeroId;

        // 전투 캐릭터 상태를 표시 오브젝트와 연결하고 임시 진영 색상을 적용합니다.
        public void Bind(WIBattleCharacterState characterState, WIBattleConfigSO config)
        {
            state = characterState;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = config.PlaceholderSprite != null
                ? config.PlaceholderSprite
                : state.Grade == ProjectWI.Administration.WICharacterGrade.Hero
                    ? WIBattlePlaceholderSprites.GetCircle()
                    : WIBattlePlaceholderSprites.GetSquare();
            spriteRenderer.color = state.Side == WIBattleSide.Attacker
                ? config.AttackerPlaceholderColor
                : config.DefenderPlaceholderColor;
            spriteRenderer.sortingOrder = 10;
            transform.localScale = Vector3.one * config.PlaceholderCharacterSize;
            CreateHealthBar();
            CreateLabel();
            CreateFocusMarker();
            CreateSelectionMarker();
            Refresh();
        }

        // 집중 공격 대상으로 지정된 인물을 둘러쌀 반투명 표적 원을 생성합니다.
        private void CreateFocusMarker()
        {
            focusMarker = new GameObject("FocusTargetMarker");
            focusMarker.transform.SetParent(transform, false);
            focusMarker.transform.localScale = Vector3.one * 1.55f;
            SpriteRenderer markerRenderer = focusMarker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = WIBattlePlaceholderSprites.GetCircle();
            markerRenderer.color = new Color(1f, 0.82f, 0.12f, 0.32f);
            markerRenderer.sortingOrder = 9;
            focusMarker.SetActive(false);
        }

        // 현재 인물이 집중 공격 표적인지에 따라 금색 표적 표시를 켜거나 끕니다.
        public void SetFocused(bool focused)
        {
            if (focusMarker != null) focusMarker.SetActive(focused && state != null && state.IsAlive);
        }

        // 마우스로 선택한 인물을 구분할 청록색 선택 원을 생성합니다.
        private void CreateSelectionMarker()
        {
            selectionMarker = new GameObject("SelectionMarker");
            selectionMarker.transform.SetParent(transform, false);
            selectionMarker.transform.localScale = Vector3.one * 1.3f;
            SpriteRenderer markerRenderer = selectionMarker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = WIBattlePlaceholderSprites.GetCircle();
            markerRenderer.color = new Color(0.15f, 0.95f, 0.95f, 0.26f);
            markerRenderer.sortingOrder = 8;
            selectionMarker.SetActive(false);
        }

        // 현재 인물이 마우스로 선택되었는지에 따라 청록색 표시를 갱신합니다.
        public void SetSelected(bool selected)
        {
            if (selectionMarker != null) selectionMarker.SetActive(selected && state != null && state.IsAlive);
        }

        // 캐릭터 위에 현재 체력을 표시하는 배경과 전경 막대를 생성합니다.
        private void CreateHealthBar()
        {
            GameObject background = new GameObject("HealthBackground");
            background.transform.SetParent(transform, false);
            background.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            background.transform.localScale = new Vector3(0.9f, 0.11f, 1f);
            SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = WIBattlePlaceholderSprites.GetSquare();
            backgroundRenderer.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            backgroundRenderer.sortingOrder = 11;

            GameObject fill = new GameObject("HealthFill");
            fill.transform.SetParent(background.transform, false);
            fill.transform.localPosition = new Vector3(-0.5f, 0f, -0.01f);
            SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = WIBattlePlaceholderSprites.GetSquare();
            fillRenderer.color = new Color(0.25f, 0.9f, 0.3f, 1f);
            fillRenderer.sortingOrder = 12;
            healthFill = fill.transform;
        }

        // 캐릭터 아래에 영웅·일반 등급, 이름과 역할을 표시합니다.
        private void CreateLabel()
        {
            GameObject labelObject = new GameObject("CharacterLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, -0.72f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = $"{(state.Grade == ProjectWI.Administration.WICharacterGrade.Hero ? "영웅" : "일반")} {state.DisplayName}\n{state.Role}";
            label.anchor = TextAnchor.UpperCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 32;
            label.color = Color.white;
            label.GetComponent<MeshRenderer>().sortingOrder = 13;
        }

        // 런타임 좌표와 생존 상태를 Transform과 렌더러에 반영합니다.
        public void Refresh()
        {
            if (state == null) return;
            transform.position = new Vector3(state.Position.x, state.Position.y, 0f);
            if (healthFill != null)
            {
                float ratio = state.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)state.Health / state.MaxHealth);
                healthFill.localScale = new Vector3(ratio, 1f, 1f);
            }
            gameObject.SetActive(state.IsAlive);
        }
    }
}
