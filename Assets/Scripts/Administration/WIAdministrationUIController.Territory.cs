using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 주둔 인물 및 특화 시설 슬롯을 현재 성 상태에 맞게 갱신합니다.
        private void RefreshCastleSlots(WICastleDefinition castle, WICastleRuntimeState castleState)
        {
            int heroSlotCount = Mathf.Min(castleState.GetHeroSlotCount(), castleHeroSlotElements.Count);
            for (int index = 0; index < castleHeroSlotElements.Count; index += 1)
            {
                VisualElement slot = castleHeroSlotElements[index];
                VisualElement portrait = castleHeroSlotImages[index];
                Label caption = castleHeroSlotCaptions[index];
                bool available = index < heroSlotCount;
                slot.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
                if (available == false)
                {
                    continue;
                }

                if (index < castleState.HeroIds.Count)
                {
                    string heroId = castleState.HeroIds[index];
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    WIHeroDefinition hero = database.GetHero(heroId);
                    string activity = character == null || character.Activity == WICharacterActivityType.None
                        ? "대기"
                        : character.Activity.ToString();
                    slot.EnableInClassList("empty-slot", false);
                    portrait.style.display = DisplayStyle.Flex;
                    ApplyBackgroundSprite(portrait, hero == null ? null : hero.Portrait);
                    caption.text = hero == null
                        ? heroId
                        : $"{hero.DisplayName.Get(database.UseEnglish)}\n{activity}";
                }
                else
                {
                    slot.EnableInClassList("empty-slot", true);
                    ClearBackgroundSprite(portrait);
                    portrait.style.display = DisplayStyle.None;
                    caption.text = database.GetText("UI_EMPTY_HERO");
                }
            }

            int facilitySlotCount = Mathf.Min(castleState.GetSpecialFacilitySlotCount(), castleFacilitySlotElements.Count);
            for (int index = 0; index < castleFacilitySlotElements.Count; index += 1)
            {
                VisualElement slot = castleFacilitySlotElements[index];
                VisualElement icon = castleFacilitySlotImages[index];
                Label caption = castleFacilitySlotCaptions[index];
                bool available = index < facilitySlotCount;
                slot.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
                if (available == false)
                {
                    continue;
                }

                if (index < castleState.SpecialFacilityIds.Count)
                {
                    WISpecialFacilityDefinition facility = database.GetSpecialFacility(castleState.SpecialFacilityIds[index]);
                    slot.EnableInClassList("empty-slot", false);
                    icon.style.display = DisplayStyle.Flex;
                    ApplyBackgroundSprite(icon, facility == null ? null : facility.Icon);
                    caption.text = facility == null
                        ? castleState.SpecialFacilityIds[index]
                        : facility.DisplayName.Get(database.UseEnglish);
                }
                else
                {
                    slot.EnableInClassList("empty-slot", true);
                    ClearBackgroundSprite(icon);
                    icon.style.display = DisplayStyle.None;
                    caption.text = "확장 완료 시 선택";
                }
            }
        }

        // Sprite가 있으면 UI 요소의 배경에 적용하고 없으면 빈 슬롯 상태를 유지합니다.
        private void ApplyBackgroundSprite(VisualElement element, Sprite sprite)
        {
            if (element == null)
            {
                return;
            }

            if (sprite == null)
            {
                ClearBackgroundSprite(element);
                return;
            }

            element.style.backgroundImage = new StyleBackground(sprite);
            Label placeholder = element.Q<Label>();
            if (placeholder != null)
            {
                placeholder.style.display = DisplayStyle.None;
            }
        }

        // 재사용 슬롯에 이전 데이터의 이미지가 남지 않도록 배경 이미지를 제거합니다.
        private static void ClearBackgroundSprite(VisualElement element)
        {
            if (element != null)
            {
                element.style.backgroundImage = StyleKeyword.None;
            }
        }

        // 선택한 성의 전체 정보를 확인하는 상세 모달을 엽니다.
        private void OpenCastleRecordModal()
        {
            if (selectedCastle == null)
            {
                return;
            }

            WICastleDefinition castle = database.GetCastle(selectedCastle.CastleId);
            VisualElement panel = CreateModal($"{castle.DisplayName.Get(database.UseEnglish)} 상세");
            bool detailed = WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, selectedCastle);
            if (detailed == false)
            {
                panel.Add(new Label("소유 진영과 지형 외 상세 정보가 확인되지 않았습니다."));
                panel.Add(new Label("첩보 메뉴에서 조사를 성공시키면 일정 기간 성 수치·주둔·시설 정보를 볼 수 있습니다."));
                if (WIInformationVisibility.CanViewMilitaryDetails(state, state.PlayerFactionId, selectedCastle))
                {
                    panel.Add(new Label("현재 전투 접촉으로 전투 세션의 양측 전력만 확인할 수 있습니다."));
                }
                return;
            }
            panel.Add(new Label($"규모: {selectedCastle.CastleSize} / 지형: {castle.TerrainTrait.Get(database.UseEnglish)} / 특산: {castle.Specialty.Get(database.UseEnglish)}"));
            panel.Add(new Label($"전문 분야 효과 · {GetSpecialtyEffectDescription(castle)}"));
            panel.Add(new Label($"번영 {selectedCastle.Prosperity} · 기술 {selectedCastle.Technology} · 질서 {selectedCastle.Stability} · 방어 {selectedCastle.Defense}"));
            panel.Add(new Label($"주둔 인물 {selectedCastle.HeroIds.Count}/{selectedCastle.GetHeroSlotCount()} · 특화 시설 {selectedCastle.SpecialFacilityIds.Count}/{selectedCastle.GetSpecialFacilitySlotCount()}"));
            if (selectedCastle.OccupationUnrestMonths > 0)
            {
                panel.Add(new Label($"점령 불안 · {selectedCastle.OccupationUnrestMonths}개월 · 영지관과 주둔 전투단 필요"));
            }
            foreach (WIHeroLegacyState legacy in selectedCastle.HeroLegacies)
            {
                panel.Add(new Label($"영웅의 흔적 · {legacy.DisplayName} · {GetProjectDisplayName(legacy.ProjectType)} +{legacy.Bonus}"));
                if (string.IsNullOrEmpty(legacy.Description) == false) panel.Add(new Label(legacy.Description));
            }
            foreach (WIHeroLegacyState legacy in selectedCastle.CommemoratedHeroLegacies)
            {
                panel.Add(new Label($"기념 기록 · {legacy.DisplayName} · 효과 없음"));
            }
            foreach (WIArmyState army in state.Armies)
            {
                if (army.CurrentCastleId == selectedCastle.CastleId)
                {
                    panel.Add(new Label($"주둔 전투단 · {army.DisplayName} · {army.Members.Count}명 · {army.Proficiency} · 보급 {army.Supply}"));
                }
            }
        }

        // 성 전문 분야의 실제 적용 효과를 UI 문장으로 반환합니다.
        private string GetSpecialtyEffectDescription(WICastleDefinition castle)
        {
            switch (castle.SpecialtyEffectType)
            {
                case WICastleSpecialtyEffectType.ProjectGain:
                    return $"{GetProjectDisplayName(castle.SpecialtyProjectType)} 성과 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.GoldIncome:
                    return $"월간 금화 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.ManaIncome:
                    return $"월간 마나 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.InfluenceIncome:
                    return $"월간 영향력 +{castle.SpecialtyEffectValue}";
                case WICastleSpecialtyEffectType.DefensePower:
                    return $"성 방어 전투력 +{castle.SpecialtyEffectValue}";
                default:
                    return "효과 없음";
            }
        }

        // 성 확장으로 획득한 슬롯에 배치할 특화 시설을 선택합니다.
        private void OpenSpecialFacilityModal()
        {
            if (EnsureSelectedCastleManageable() == false)
            {
                return;
            }

            if (selectedCastle.PendingSpecialFacilityChoice == false)
            {
                ShowMessage("특화 시설은 성 확장 사업을 완료할 때 선택할 수 있습니다.");
                return;
            }

            List<WISpecialFacilityDefinition> candidates = new List<WISpecialFacilityDefinition>();
            foreach (WISpecialFacilityDefinition facility in database.SpecialFacilities)
            {
                if (selectedCastle.SpecialFacilityIds.Contains(facility.Id) == false)
                {
                    candidates.Add(facility);
                }
            }

            ShowChoiceModal("특화 시설 선택", candidates, facility =>
            {
                selectedCastle.SpecialFacilityIds.Add(facility.Id);
                selectedCastle.PendingSpecialFacilityChoice = false;
                CloseModal();
                SelectCastle(selectedCastle.CastleId);
            });
        }

    }
}
