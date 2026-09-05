using System;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 플레이어가 소유한 첫 번째 성을 초기 선택값으로 저장하고 월드 UGUI를 표시합니다.
        private void SelectInitialCastle()
        {
            selectedCastle = null;
            foreach (WICastleRuntimeState castleState in state.Castles)
            {
                WIFactionDefinition faction = database.GetFaction(castleState.FactionId);
                if (faction != null && faction.PlayerFaction)
                {
                    selectedCastle = castleState;
                    break;
                }
            }
            ShowGlobalView();
        }

        // 선택 성을 변경하고 영지 UGUI를 표시합니다.
        private void SelectCastle(string castleId)
        {
            selectedCastle = state.GetCastle(castleId);
            if (selectedCastle == null)
            {
                return;
            }
            ActivateUGUITerritoryVisibility();
            NotifyUGUIWorldChanged();
        }

        // 월드 UGUI를 복원합니다.
        private void ShowGlobalView()
        {
            RestoreUGUIWorldVisibility();
        }

        // 월드 요약 카드가 가리키는 플레이어 영지를 엽니다.
        private void OpenGlobalSummaryCastle()
        {
            WICastleRuntimeState castle = GetGlobalSummaryCastle();
            if (castle != null)
            {
                SelectCastle(castle.CastleId);
            }
        }

        // 현재 선택 성 또는 플레이어가 소유한 첫 성을 월드 요약 대상으로 반환합니다.
        private WICastleRuntimeState GetGlobalSummaryCastle()
        {
            if (selectedCastle != null && selectedCastle.FactionId == state.PlayerFactionId)
            {
                return selectedCastle;
            }
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                if (castle.FactionId == state.PlayerFactionId)
                {
                    return castle;
                }
            }
            return null;
        }

        // 외부 UI나 검증 도구에서 지정한 성의 영지 UGUI로 이동합니다.
        public void OpenCastle(string castleId)
        {
            if (state == null || database.GetCastle(castleId) == null)
            {
                return;
            }
            SelectCastle(castleId);
            Debug.Log($"UGUI 성 화면 전환: {castleId}");
        }

        // QA에서 표준 캠페인 상태의 월드 UGUI를 표시합니다.
        public void OpenGlobalPreviewForQA()
        {
            BeginCampaignForQA(WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            ShowGlobalView();
            RefreshAll();
        }

        // QA에서 현재 시나리오의 플레이어 소유 성을 선택하고 영지 UGUI를 표시합니다.
        public void OpenCastlePreviewForQA()
        {
            BeginCampaignForQA(WICampaignDifficulty.Standard, WICampaignVariant.AresMain);
            WICastleRuntimeState previewCastle = state.Castles
                .Where(castle => castle.FactionId == state.PlayerFactionId && castle.HeroIds.Count > 0)
                .FirstOrDefault() ?? state.Castles.FirstOrDefault(castle => castle.FactionId == state.PlayerFactionId);
            if (previewCastle != null)
            {
                SelectCastle(previewCastle.CastleId);
            }
            RefreshAll();
        }

        // 긴 표시명을 지정 길이로 줄이고 말줄임표를 붙입니다.
        public static string TruncateLabel(string value, int maxCharacters)
        {
            return string.IsNullOrEmpty(value) || value.Length <= maxCharacters
                ? value
                : value.Substring(0, Math.Max(1, maxCharacters - 1)) + "…";
        }

        // HUD 수치를 현재 언어에 맞는 축약 문자열로 반환합니다.
        public static string FormatHudNumber(int value, bool useEnglish)
        {
            double absolute = Math.Abs((double)value);
            string sign = value < 0 ? "-" : string.Empty;
            if (useEnglish)
            {
                if (absolute >= 1000000d) return sign + (absolute / 1000000d).ToString("0.#") + "M";
                if (absolute >= 100000d) return sign + (absolute / 1000d).ToString("0.#") + "K";
                return value.ToString("N0");
            }
            if (absolute >= 10000d) return sign + (absolute / 10000d).ToString("0.#") + "만";
            return value.ToString("N0");
        }

        // 자원 현재값과 월 수입의 계산 근거를 툴팁 문장으로 조합합니다.
        public static string BuildResourceCalculationTooltip(string resourceName, int current, int monthlyGain,
            int castleCount, string formula)
        {
            return $"{resourceName} 현재 {current:N0}\n다음 턴 예상 +{monthlyGain:N0}\n소유 성 {castleCount}개\n근거: {formula}";
        }

        // 진영 ID를 색상 외에도 구분 가능한 접근성 코드로 변환합니다.
        public static string GetFactionAccessibilityCode(string factionId)
        {
            switch (factionId)
            {
                case "avalon": return "AV";
                case "valdor": return "VD";
                case "ironheart": return "IH";
                case "sylvanroad": return "SY";
                case "necropolis": return "NC";
                default: return "--";
            }
        }

        // 성 수치에 대응하는 간결한 상태명을 반환합니다.
        private string GetCastleStatusName(int value)
        {
            if (value < 25) return "낙후";
            if (value < 50) return "보통";
            if (value < 75) return "발전";
            return "번성";
        }

        // 현재 중점 사업의 담당자와 투자 상태를 UGUI 표시 문자열로 만듭니다.
        private string GetProjectStatusText(WICastleProjectState project)
        {
            if (project == null)
            {
                return "이번 달 중점 사업이 지정되지 않았습니다.";
            }
            WIHeroDefinition manager = database.GetHero(project.ManagerHeroId);
            string managerName = manager == null ? "담당자 없음" : manager.DisplayName.Get(database.UseEnglish);
            string result = $"{GetProjectDisplayName(project.ProjectType)} · {managerName} · {GetInvestmentDisplayName(project.Investment)}";
            return project.Delegated
                ? $"영지관 위임 · {result} · 예상 +{project.ExpectedGain} · {project.GoldCost}G"
                : result;
        }
    }
}
