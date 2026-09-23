using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 공통 선택 행에서 마스터 데이터의 인물 초상을 조회합니다.
        public UnityEngine.Sprite GetSelectionPortrait(string heroId)
        {
            return database.GetHero(heroId)?.Portrait;
        }

        // 편성 불가 사유를 목록에 남겨 후보가 보이지 않는 이유를 설명합니다.
        private string GetMilitarySelectionStatus(string heroId)
        {
            if (state.Armies.Any(army => army.Members.Any(member => member.HeroId == heroId)) == true)
            {
                return database.GetText("UI_SELECTION_IN_ARMY");
            }
            return database.GetText(state.IsCharacterBusy(heroId, ignoreAdministration: true)
                ? "UI_SELECTION_BUSY" : "UI_SELECTION_READY");
        }

        // 선택 행에서 등급·직업·피로와 현재 내정·전투단 배치를 함께 안내합니다.
        public string GetSelectionCharacterSummary(string heroId)
        {
            var hero = database.GetHero(heroId);
            var character = state.GetCharacter(heroId);
            if (hero == null || character == null)
            {
                return string.Empty;
            }
            var parts = new List<string>
            {
                database.GetText(state.IsCommonCharacter(heroId) ? "UI_SELECTION_COMMON" : "UI_SELECTION_HERO"),
                database.GetHeroClass(hero.HeroClass)?.DisplayName.Get(database.UseEnglish) ?? string.Empty,
                string.Format(database.GetText("UI_SELECTION_FATIGUE"), character.Fatigue)
            };
            if (character.InjuryMonths > 0)
            {
                parts.Add(string.Format(database.GetText("UI_SELECTION_INJURY"), character.InjuryMonths));
            }
            var duty = state.Castles.FirstOrDefault(castle => castle.GovernorHeroId == heroId ||
                castle.ActiveProject?.ManagerHeroId == heroId);
            if (duty != null)
            {
                parts.Add(string.Format(database.GetText("UI_SELECTION_DUTY"),
                    database.GetCastle(duty.CastleId).DisplayName.Get(database.UseEnglish)));
            }
            var army = state.Armies.FirstOrDefault(item => item.Members.Any(member => member.HeroId == heroId));
            if (army != null)
            {
                parts.Add(army.DisplayName);
            }
            return string.Join(" · ", parts);
        }

        // 인물 등급의 UI 표시명을 반환합니다.
        private static string GetGradeDisplayName(WICharacterGrade grade)
        {
            return grade == WICharacterGrade.Hero ? "영웅" : "일반";
        }

        // 인물이 가진 특기 목록을 한국어 UI 문자열로 조합합니다.
        private string GetTraitDisplayText(WIHeroDefinition hero)
        {
            if (state.IsCommonCharacter(hero.Id) == true)
            {
                return database.GetText("UI_COMMON_COMBAT_ONLY");
            }
            List<string> names = new List<string>();
            foreach (WITraitType trait in hero.Traits)
            {
                WITraitDefinition definition = database.GetTrait(trait);
                if (definition != null)
                {
                    names.Add($"{definition.DisplayName.Get(database.UseEnglish)} · {definition.Description.Get(database.UseEnglish)}");
                }
            }
            return names.Count == 0 ? "없음" : string.Join(", ", names);
        }

        // 좁은 선택 카드에서는 설명을 제외하고 특성 이름만 간결하게 조합합니다.
        private string GetTraitNameText(WIHeroDefinition hero)
        {
            if (state.IsCommonCharacter(hero.Id) == true)
            {
                return database.GetText("UI_COMMON_COMBAT_ONLY");
            }
            List<string> names = new List<string>();
            foreach (WITraitType trait in hero.Traits)
            {
                WITraitDefinition definition = database.GetTrait(trait);
                if (definition != null)
                {
                    names.Add(definition.DisplayName.Get(database.UseEnglish));
                }
            }
            return names.Count == 0 ? "특성 없음" : string.Join(", ", names);
        }

        // 인물의 전투 운명 특성을 효과 설명과 함께 UI 문자열로 조합합니다.
        private static string GetBattleTraitDisplayText(WIHeroDefinition hero)
        {
            List<string> names = new List<string>();
            foreach (WIBattleTraitType trait in hero.BattleTraits)
            {
                if (trait == WIBattleTraitType.Survivor)
                {
                    names.Add("생존가 · 사망 확률 감소");
                }
                else if (trait == WIBattleTraitType.Elusive)
                {
                    names.Add("탈출가 · 포로 확률 감소");
                }
                else if (trait == WIBattleTraitType.Unyielding)
                {
                    names.Add("불굴 · 적 진영 합류 불가");
                }
            }
            return names.Count == 0 ? "없음" : string.Join(", ", names);
        }
    }
}
