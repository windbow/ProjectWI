using UnityEngine;

namespace ProjectWI.Battle
{
    public class WIBattleCharacterView : MonoBehaviour
    {
        private const int CharacterSortingBase = 1000;
        private const float CharacterSortingPrecision = 100f;
        private WIBattleCharacterState state;
        private SpriteRenderer spriteRenderer;
        // 원본 Sprite가 오른쪽을 바라보는지 여부로 반전 기준을 정합니다.
        private bool spriteFacesRight;
        private SpriteRenderer groundShadowRenderer;
        // 진영 색으로 칠하는 발밑 표시입니다.
        private SpriteRenderer sideMarkerRenderer;
        // 바인딩 시 정한 인물 이미지 기본 색입니다.
        private Color baseColor = Color.white;
        // 퇴각 중 이미지에 곱하는 색입니다.
        private Color routingTint = Color.white;
        private Material defaultMaterial;
        private Material farOutlineMaterial;
        private Transform healthFill;
        private GameObject focusMarker;
        private GameObject selectionMarker;
        private GameObject healthBackground;
        private GameObject characterLabelObject;
        private TextMesh characterLabel;
        // 걷기·공격·피격·쓰러짐 연출을 적용하는 인물 그림 자식입니다. 발밑 표시와 체력 바는 움직이지 않습니다.
        private Transform bodyTransform;
        // 연출 수치를 읽는 전투 설정입니다.
        private WIBattleConfigSO feelConfig;
        // 직전 표시 프레임의 체력·공격 대기시간·위치입니다. 변화로 피격·공격·이동을 감지합니다.
        private int lastHealth;
        private float lastCooldown;
        private Vector2 lastDisplayPosition;
        // 진행 중인 연출의 남은 시간입니다.
        private float flashRemaining;
        private float lungeRemaining;
        private float deathElapsed;
        private float spawnElapsed = float.MaxValue;
        // 걸음 위상과 이동 중 가중치(0~1)입니다.
        private float walkPhase;
        private float walkWeight;
        // 선택 표시 맥동 시간과 원래 크기입니다.
        private float pulseTime;
        private Vector3 selectionMarkerBaseScale = Vector3.one;

        public string HeroId => state?.HeroId;
        // 표시 중인 인물의 분대 번호입니다.
        public int SquadId => state == null ? 0 : state.SquadId;

        // 전투 캐릭터 상태를 표시 오브젝트와 연결하고 임시 진영 색상을 적용합니다.
        public void Bind(WIBattleCharacterState characterState, WIBattleConfigSO config, Sprite battleSprite)
        {
            state = characterState;
            feelConfig = config;
            bodyTransform = transform.Find("Body");
            spriteRenderer = bodyTransform == null ? null : bodyTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("전투 캐릭터 프리팹에 Body 자식 SpriteRenderer가 없습니다.", this);
                return;
            }
            BindGroundShadow();
            spriteFacesRight = config.BattleSpriteFacesRight;
            defaultMaterial = config.CharacterDefaultMaterial;
            farOutlineMaterial = config.CharacterFarOutlineMaterial;
            spriteRenderer.sprite = battleSprite != null
                ? battleSprite
                : config.PlaceholderSprite;
            if (spriteRenderer.sprite == null)
            {
                Debug.LogError("전투 캐릭터 대체 Sprite가 BattleConfig에 연결되지 않았습니다.", config);
            }
            baseColor = battleSprite != null
                ? Color.white
                : state.Side == WIBattleSide.Attacker
                    ? config.AttackerPlaceholderColor
                    : config.DefenderPlaceholderColor;
            spriteRenderer.color = baseColor;
            routingTint = config.RoutingTint;
            if (sideMarkerRenderer != null)
            {
                Color sideColor = state.Side == WIBattleSide.Attacker
                    ? config.AttackerPlaceholderColor
                    : config.DefenderPlaceholderColor;
                sideColor.a = config.SideMarkerAlpha;
                sideMarkerRenderer.color = sideColor;
            }
            else
            {
                Debug.LogError("전투 캐릭터 프리팹에 SideMarker SpriteRenderer가 없습니다.", this);
            }
            spriteRenderer.sortingOrder = 10;
            transform.localScale = battleSprite == null
                ? Vector3.one * config.PlaceholderCharacterSize
                : Vector3.one * config.BattleSpriteScale;
            BindPrefabVisuals(config);
            lastHealth = state.Health;
            lastCooldown = state.CooldownRemaining;
            lastDisplayPosition = state.Position;
            Refresh(1f, 0f);
        }

        // 전투 중 합류한 인물이 튀어나오듯 등장하도록 등장 연출을 시작합니다.
        public void PlaySpawn()
        {
            spawnElapsed = 0f;
        }

        // 프리팹에 미리 배치된 공용 접지 그림자를 찾아 캐릭터 깊이 정렬과 연결합니다.
        private void BindGroundShadow()
        {
            Transform shadowTransform = transform.Find("GroundShadow");
            groundShadowRenderer = shadowTransform == null
                ? null
                : shadowTransform.GetComponent<SpriteRenderer>();
            Transform markerTransform = transform.Find("SideMarker");
            sideMarkerRenderer = markerTransform == null
                ? null
                : markerTransform.GetComponent<SpriteRenderer>();
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
            if (selectionMarker != null)
            {
                selectionMarkerBaseScale = selectionMarker.transform.localScale;
            }
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

        // 틱 사이 보간 비율을 적용한 런타임 좌표와 생존 상태를 반영하고, deltaTime만큼 연출을 진행합니다.
        // 히트 스톱 중에는 deltaTime 0을 받아 연출도 멈춥니다.
        public void Refresh(float interpolationAlpha = 1f, float deltaTime = 0f)
        {
            if (state == null || spriteRenderer == null)
            {
                return;
            }
            Vector2 displayPosition = state.GetDisplayPosition(interpolationAlpha);
            transform.position = new Vector3(displayPosition.x, displayPosition.y, 0f);
            spriteRenderer.flipX = state.FacingRight != spriteFacesRight;
            spriteRenderer.sortingOrder = CharacterSortingBase - Mathf.RoundToInt(displayPosition.y * CharacterSortingPrecision);
            if (groundShadowRenderer != null)
            {
                groundShadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 2;
            }
            if (sideMarkerRenderer != null)
            {
                sideMarkerRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            }
            if (healthFill != null)
            {
                float ratio = state.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)state.Health / state.MaxHealth);
                healthFill.localScale = new Vector3(ratio, 1f, 1f);
            }
            DetectFeelTriggers(displayPosition, deltaTime);
            if (state.IsAlive == false)
            {
                AnimateDefeat(deltaTime);
                return;
            }
            AnimateBody(deltaTime);
        }

        // 체력 감소·공격 대기시간 재설정·위치 변화를 감지해 피격·공격·걷기 연출을 시작합니다.
        private void DetectFeelTriggers(Vector2 displayPosition, float deltaTime)
        {
            if (state.Health < lastHealth)
            {
                flashRemaining = feelConfig.HitFlashDuration;
            }
            if (state.CooldownRemaining > lastCooldown + 0.2f && UsesMeleeLunge() == true)
            {
                lungeRemaining = feelConfig.AttackLungeDuration;
            }
            if (deltaTime > 0f)
            {
                float speed = Vector2.Distance(displayPosition, lastDisplayPosition) / deltaTime;
                float targetWeight = speed > 0.2f ? 1f : 0f;
                walkWeight = Mathf.MoveTowards(walkWeight, targetWeight, deltaTime * 6f);
                walkPhase += deltaTime * feelConfig.WalkBobFrequency * Mathf.PI * Mathf.Clamp(speed / 2.5f, 0.6f, 1.6f);
            }
            lastHealth = state.Health;
            lastCooldown = state.CooldownRemaining;
            lastDisplayPosition = displayPosition;
        }

        // 걷기 흔들림·기울기, 공격 내딛기, 피격 번쩍임·눌림, 등장 튀어나옴을 몸 그림에 적용합니다.
        private void AnimateBody(float deltaTime)
        {
            flashRemaining = Mathf.Max(0f, flashRemaining - deltaTime);
            lungeRemaining = Mathf.Max(0f, lungeRemaining - deltaTime);
            if (spawnElapsed < float.MaxValue)
            {
                spawnElapsed += deltaTime;
            }
            pulseTime += deltaTime;

            float facing = state.FacingRight == true ? 1f : -1f;
            float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * feelConfig.WalkBobHeight * walkWeight;
            float tilt = Mathf.Sin(walkPhase) * feelConfig.WalkTiltDegrees * walkWeight;
            float lungeProgress = 1f - lungeRemaining / feelConfig.AttackLungeDuration;
            float lunge = lungeRemaining > 0f ? Mathf.Sin(lungeProgress * Mathf.PI) * feelConfig.AttackLungeDistance : 0f;
            Vector3 rootScale = transform.localScale;
            float scaleX = Mathf.Max(0.01f, rootScale.x);
            float scaleY = Mathf.Max(0.01f, rootScale.y);
            bodyTransform.localPosition = new Vector3(facing * lunge / scaleX, bob / scaleY, 0f);
            bodyTransform.localRotation = Quaternion.Euler(0f, 0f, -tilt * facing);

            float flashT = flashRemaining / feelConfig.HitFlashDuration;
            float squash = feelConfig.HitSquashAmount * flashT;
            float pop = 1f;
            if (spawnElapsed < feelConfig.SpawnPopDuration)
            {
                pop = EaseOutBack(spawnElapsed / feelConfig.SpawnPopDuration);
            }
            bodyTransform.localScale = new Vector3((1f + squash) * pop, (1f - squash) * pop, 1f);

            Color color = state.IsRouting == true ? baseColor * routingTint : baseColor;
            spriteRenderer.color = Color.Lerp(color, feelConfig.HitFlashColor * color, flashT);

            if (selectionMarker != null && selectionMarker.activeSelf == true)
            {
                float pulse = 1f + Mathf.Sin(pulseTime * feelConfig.SelectionPulseSpeed) * feelConfig.SelectionPulseAmount;
                selectionMarker.transform.localScale = selectionMarkerBaseScale * pulse;
            }
            gameObject.SetActive(true);
        }

        // 쓰러진 인물은 뒤로 넘어지며 흐려지고, 전장을 벗어난 인물은 흐려지며 사라집니다. 끝나면 비활성화합니다.
        private void AnimateDefeat(float deltaTime)
        {
            deathElapsed += deltaTime;
            float progress = Mathf.Clamp01(deathElapsed / feelConfig.DeathFallDuration);
            SetOverlaysHidden();
            if (state.Escaped == false)
            {
                float facing = state.FacingRight == true ? 1f : -1f;
                float fall = EaseOutQuad(Mathf.Clamp01(progress * 1.6f));
                bodyTransform.localRotation = Quaternion.Euler(0f, 0f, fall * feelConfig.DeathFallDegrees * facing);
            }
            Color color = spriteRenderer.color;
            color.a = 1f - EaseOutQuad(progress);
            spriteRenderer.color = color;
            if (progress >= 1f)
            {
                gameObject.SetActive(false);
            }
        }

        // 쓰러짐 연출 중 발밑 표시·체력 바·마커를 숨깁니다.
        private void SetOverlaysHidden()
        {
            if (sideMarkerRenderer != null)
            {
                sideMarkerRenderer.enabled = false;
            }
            if (groundShadowRenderer != null)
            {
                groundShadowRenderer.enabled = false;
            }
            if (healthBackground != null)
            {
                healthBackground.SetActive(false);
            }
            if (focusMarker != null)
            {
                focusMarker.SetActive(false);
            }
            if (selectionMarker != null)
            {
                selectionMarker.SetActive(false);
            }
        }

        // 근접 역할처럼 공격 때 앞으로 내딛는 인물인지 반환합니다.
        private bool UsesMeleeLunge()
        {
            return state.Role != ProjectWI.Administration.WIUnitRole.Ranged &&
                state.Role != ProjectWI.Administration.WIUnitRole.Magic &&
                state.Role != ProjectWI.Administration.WIUnitRole.Support;
        }

        // 끝에서 살짝 넘쳤다 돌아오는 튀어나옴 곡선입니다.
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        // 빠르게 시작해 천천히 끝나는 곡선입니다.
        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
