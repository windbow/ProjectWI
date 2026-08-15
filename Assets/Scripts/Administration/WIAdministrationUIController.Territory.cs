namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 성 전문 분야의 실제 적용 효과를 UGUI 표시 문장으로 반환합니다.
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
    }
}
