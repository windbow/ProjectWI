using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.SubSystem
{
    /// <summary>
    /// 전투 화면에 배치된 캐릭터 슬롯 하나를 담당하는 컴포넌트입니다.
    /// Bind()로 캐릭터를 연결하면 이벤트(OnHpChanged, OnBattleTick) 발생 시 Slider를 갱신합니다.
    /// Update() 폴링 없이 이벤트 기반으로 동작합니다.
    /// </summary>
    public class WIBattleCharacterSlot : MonoBehaviour
    {
        [Header("UI References")]
        /// <summary>캐릭터 이름을 표시하는 Text</summary>
        public Text nameText;
        /// <summary>HP 비율을 표시하는 Slider (HpSlider)</summary>
        public Slider hpSlider;
        /// <summary>행동력 비율을 표시하는 Slider (ActionSlider)</summary>
        public Slider actionSlider;
        /// <summary>스킬 아이콘 3개 배열 (SkillIcon_0 ~ SkillIcon_2)</summary>
        public Image[] skillIcons = new Image[3];

        /// <summary>현재 이 슬롯에 바인딩된 캐릭터</summary>
        private WICharacterBase boundCharacter;
        /// <summary>행동력 비율 계산을 위해 참조하는 전투 세션</summary>
        private WIBattleSession boundSession;

        /// <summary>
        /// Inspector에 연결된 참조가 없을 경우 자식 이름으로 자동 탐색합니다.
        /// </summary>
        private void Awake()
        {
            if (nameText == null || hpSlider == null || actionSlider == null)
            {
                Text[] texts = GetComponentsInChildren<Text>(true);
                foreach (Text t in texts)
                {
                    if (t.gameObject.name == "NameText")
                    {
                        nameText = t;
                    }
                }

                Slider[] sliders = GetComponentsInChildren<Slider>(true);
                foreach (Slider s in sliders)
                {
                    if (s.gameObject.name == "HpSlider")
                    {
                        hpSlider = s;
                    }
                    else if (s.gameObject.name == "ActionSlider")
                    {
                        actionSlider = s;
                    }
                }

                Image[] images = GetComponentsInChildren<Image>(true);
                foreach (Image img in images)
                {
                    if (img.gameObject.name == "SkillIcon_0")
                    {
                        skillIcons[0] = img;
                    }
                    else if (img.gameObject.name == "SkillIcon_1")
                    {
                        skillIcons[1] = img;
                    }
                    else if (img.gameObject.name == "SkillIcon_2")
                    {
                        skillIcons[2] = img;
                    }
                }
            }
        }

        /// <summary>
        /// 캐릭터와 전투 세션을 슬롯에 연결합니다.
        /// HP 이벤트와 틱 이벤트를 구독하여 이벤트 발생 시 UI를 갱신합니다.
        /// </summary>
        public void Bind(WICharacterBase character, WIBattleSession session, bool isAdventurer)
        {
            boundCharacter = character;
            boundSession   = session;

            // 배경 색상
            Image bg = GetComponent<Image>();
            if (bg != null)
            {
                bg.color = isAdventurer
                    ? new Color(0.25f, 0.35f, 0.65f, 0.55f)
                    : new Color(0.65f, 0.25f, 0.25f, 0.55f);
            }

            // 이름
            if (nameText != null)
            {
                nameText.text = character.Name;
            }

            // HP Slider 초기값 및 Fill 색상
            if (hpSlider != null)
            {
                SetSliderFillColor(hpSlider, isAdventurer
                    ? new Color(0.15f, 0.82f, 0.28f, 1f)
                    : new Color(0.90f, 0.28f, 0.22f, 1f));
                hpSlider.value = character.MaxHp > 0
                    ? (float)character.CurrentHp / character.MaxHp
                    : 0f;
            }

            // Action Slider 초기값
            if (actionSlider != null)
            {
                actionSlider.value = 0f;
            }

            // 스킬 아이콘 색상
            if (character.Job != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (skillIcons[i] == null)
                    {
                        continue;
                    }

                    bool hasSkill = i < character.Job.Skills.Length && character.Job.Skills[i] != null;
                    if (hasSkill)
                    {
                        skillIcons[i].color = isAdventurer
                            ? new Color(0.3f, 0.6f, 1.0f, 1f)
                            : new Color(1.0f, 0.42f, 0.3f, 1f);
                    }
                    else
                    {
                        skillIcons[i].color = new Color(0.22f, 0.22f, 0.22f, 0.6f);
                    }
                }
            }

            // 이벤트 구독
            boundCharacter.OnHpChanged  += OnHpChanged;
            boundSession.OnBattleTick   += OnBattleTick;
        }

        /// <summary>
        /// 슬롯 연결을 해제하고 이벤트 구독을 취소합니다.
        /// </summary>
        public void Unbind()
        {
            if (boundCharacter != null)
            {
                boundCharacter.OnHpChanged -= OnHpChanged;
                boundCharacter = null;
            }

            if (boundSession != null)
            {
                boundSession.OnBattleTick -= OnBattleTick;
                boundSession = null;
            }
        }

        /// <summary>
        /// 바인딩된 캐릭터의 HP가 0 이하인지 반환합니다.
        /// </summary>
        public bool IsDead()
        {
            return boundCharacter != null && boundCharacter.CurrentHp <= 0;
        }

        /// <summary>
        /// WICharacterBase.OnHpChanged 이벤트 수신 시 HP Slider를 갱신합니다.
        /// </summary>
        private void OnHpChanged()
        {
            if (hpSlider == null || boundCharacter == null)
            {
                return;
            }

            hpSlider.value = boundCharacter.MaxHp > 0
                ? (float)boundCharacter.CurrentHp / boundCharacter.MaxHp
                : 0f;
        }

        /// <summary>
        /// WIBattleSession.OnBattleTick 이벤트 수신 시 행동력 Slider를 갱신합니다.
        /// </summary>
        private void OnBattleTick()
        {
            if (actionSlider == null || boundCharacter == null || boundSession == null)
            {
                return;
            }

            actionSlider.value = boundSession.GetActionValueRatio(boundCharacter);
        }

        /// <summary>
        /// Slider의 Fill 이미지 색상을 설정합니다.
        /// fillRect → Fill Area 자식의 Image 컴포넌트에 색상을 적용합니다.
        /// </summary>
        private void SetSliderFillColor(Slider slider, Color color)
        {
            if (slider.fillRect == null)
            {
                return;
            }

            Image fillImg = slider.fillRect.GetComponent<Image>();
            if (fillImg != null)
            {
                fillImg.color = color;
            }
        }
    }
}
