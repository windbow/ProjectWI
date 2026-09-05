using UnityEngine;

namespace ProjectWI.Battle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WIBattleCharacterView : MonoBehaviour
    {
        private const int CharacterSortingBase = 1000;
        private const float CharacterSortingPrecision = 100f;
        private WIBattleCharacterState state;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer groundShadowRenderer;
        private Material defaultMaterial;
        private Material farOutlineMaterial;
        private Transform healthFill;
        private GameObject focusMarker;
        private GameObject selectionMarker;
        private GameObject healthBackground;
        private GameObject characterLabelObject;
        private TextMesh characterLabel;

        public string HeroId => state?.HeroId;

        // 전투 캐릭터 상태를 표시 오브젝트와 연결하고 임시 진영 색상을 적용합니다.
        public void Bind(WIBattleCharacterState characterState, WIBattleConfigSO config, Sprite battleSprite)
        {
            state = characterState;
            spriteRenderer = GetComponent<SpriteRenderer>();
            BindGroundShadow();
            defaultMaterial = config.CharacterDefaultMaterial;
            farOutlineMaterial = config.CharacterFarOutlineMaterial;
            spriteRenderer.sprite = battleSprite != null
                ? battleSprite
                : config.PlaceholderSprite;
            if (spriteRenderer.sprite == null)
            {
                Debug.LogError("전투 캐릭터 대체 Sprite가 BattleConfig에 연결되지 않았습니다.", config);
            }
            spriteRenderer.color = battleSprite != null
                ? Color.white
                : state.Side == WIBattleSide.Attacker
                    ? config.AttackerPlaceholderColor
                    : config.DefenderPlaceholderColor;
            spriteRenderer.sortingOrder = 10;
            transform.localScale = battleSprite == null
                ? Vector3.one * config.PlaceholderCharacterSize
                : Vector3.one * config.BattleSpriteScale;
            BindPrefabVisuals(config);
            Refresh();
        }

        // 프리팹에 미리 배치된 공용 접지 그림자를 찾아 캐릭터 깊이 정렬과 연결합니다.
        private void BindGroundShadow()
        {
            Transform shadowTransform = transform.Find("GroundShadow");
            groundShadowRenderer = shadowTransform == null
                ? null
                : shadowTransform.GetComponent<SpriteRenderer>();
        }

        // 원거리 C 단계에서만 외곽선 머티리얼을 공유 적용합니다.
        public void SetFarOutlineEnabled(bool enabled)
        {
            if (spriteRenderer == null)
            {
                return;
            }
            Material targetMaterial = enabled == true ? farOutlineMaterial : defaultMaterial;
            if (targetMaterial != null && spriteRenderer.sharedMaterial != targetMaterial)
            {
                spriteRenderer.sharedMaterial = targetMaterial;
            }
        }

        // 프리팹에 미리 배치된 마커, 체력 바와 인물 라벨을 찾아 표시값을 연결합니다.
        private void BindPrefabVisuals(WIBattleConfigSO config)
        {
            focusMarker = BindMarker("FocusTargetMarker", new Color(1f, 0.82f, 0.12f, 0.32f), 9);
            selectionMarker = BindMarker("SelectionMarker", new Color(0.15f, 0.95f, 0.95f, 0.26f), 8);
            BindHealthBar(config.ShowCharacterHealthBars);
            BindCharacterLabel(config.ShowCharacterLabels);
        }

        // 프리팹 마커의 색상과 정렬 순서를 설정하고 비활성 상태로 반환합니다.
        private GameObject BindMarker(string childName, Color color, int sortingOrder)
        {
            Transform markerTransform = transform.Find(childName);
            if (markerTransform == null)
            {
                return null;
            }

            SpriteRenderer markerRenderer = markerTransform.GetComponent<SpriteRenderer>();
            if (markerRenderer != null)
            {
                markerRenderer.color = color;
                markerRenderer.sortingOrder = sortingOrder;
            }

            markerTransform.gameObject.SetActive(false);
            return markerTransform.gameObject;
        }

        // 현재 인물이 집중 공격 표적인지에 따라 금색 표적 표시를 켜거나 끕니다.
        public void SetFocused(bool focused)
        {
            if (focusMarker != null)
            {
                focusMarker.SetActive(focused && state != null && state.IsAlive);
            }
        }

        // 현재 인물이 마우스로 선택되었는지에 따라 청록색 표시를 갱신합니다.
        public void SetSelected(bool selected)
        {
            if (selectionMarker != null)
            {
                selectionMarker.SetActive(selected && state != null && state.IsAlive);
            }
        }

        // 프리팹 체력 바의 Sprite와 색상, 표시 여부를 설정합니다.
        private void BindHealthBar(bool visible)
        {
            Transform backgroundTransform = transform.Find("HealthBackground");
            healthBackground = backgroundTransform?.gameObject;
            healthFill = backgroundTransform?.Find("HealthFill");
            SpriteRenderer backgroundRenderer = backgroundTransform?.GetComponent<SpriteRenderer>();
            SpriteRenderer fillRenderer = healthFill?.GetComponent<SpriteRenderer>();
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = new Color(0.08f, 0.08f, 0.08f, 1f);
                backgroundRenderer.sortingOrder = 11;
            }
            if (fillRenderer != null)
            {
                fillRenderer.color = new Color(0.25f, 0.9f, 0.3f, 1f);
                fillRenderer.sortingOrder = 12;
            }
            if (healthBackground != null)
            {
                healthBackground.SetActive(visible);
            }
        }

        // 프리팹 인물 라벨에 등급, 이름과 역할을 설정합니다.
        private void BindCharacterLabel(bool visible)
        {
            Transform labelTransform = transform.Find("CharacterLabel");
            characterLabelObject = labelTransform?.gameObject;
            characterLabel = labelTransform?.GetComponent<TextMesh>();
            if (characterLabel != null)
            {
                characterLabel.text = $"{(state.Grade == ProjectWI.Administration.WICharacterGrade.Hero ? "영웅" : "일반")} {state.DisplayName}\n{state.Role}";
                characterLabel.anchor = TextAnchor.UpperCenter;
                characterLabel.alignment = TextAlignment.Center;
                characterLabel.characterSize = 0.12f;
                characterLabel.fontSize = 32;
                characterLabel.color = Color.white;
                characterLabel.GetComponent<MeshRenderer>().sortingOrder = 13;
            }
            if (characterLabelObject != null)
            {
                characterLabelObject.SetActive(visible);
            }
        }

        // 런타임 좌표와 생존 상태를 Transform과 렌더러에 반영합니다.
        public void Refresh()
        {
            if (state == null)
            {
                return;
            }
            transform.position = new Vector3(state.Position.x, state.Position.y, 0f);
            spriteRenderer.sortingOrder = CharacterSortingBase - Mathf.RoundToInt(state.Position.y * CharacterSortingPrecision);
            if (groundShadowRenderer != null)
            {
                groundShadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            }
            if (healthFill != null)
            {
                float ratio = state.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)state.Health / state.MaxHealth);
                healthFill.localScale = new Vector3(ratio, 1f, 1f);
            }
            gameObject.SetActive(state.IsAlive);
        }
    }
}
