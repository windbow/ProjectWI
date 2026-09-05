using System.Collections.Generic;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 인물 등급의 UI 표시명을 반환합니다.
        private static string GetGradeDisplayName(WICharacterGrade grade)
        {
            return grade == WICharacterGrade.Hero ? "영웅" : "일반";
        }

        // 인물이 가진 특기 목록을 한국어 UI 문자열로 조합합니다.
        private string GetTraitDisplayText(WIHeroDefinition hero)
        {
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
