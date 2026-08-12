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
    }
}
