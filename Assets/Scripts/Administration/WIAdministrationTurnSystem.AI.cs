using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {



        // 한 턴의 AI 판단 근거를 진영·분야별 한 건으로 제한해 월간 보고에 추가합니다.
        public static void AddAIReasonReport(WITurnSummary summary, string factionId, string factionName,
            string category, string reason)
        {
            if (summary == null)
            {
                return;
            }
            summary.AIReasonReports = summary.AIReasonReports ?? new List<string>();
            if (summary.AIReasonReports.Count >= 12)
            {
                return;
            }
            string prefix = $"[AI 판단] {factionId} · {category} ·";
            if (summary.AIReasonReports.Any(item => item.StartsWith(prefix)))
            {
                return;
            }
            summary.AIReasonReports.Add($"{prefix} {factionName} · {reason}");
        }

        // AI 성향을 사업과 군사 판단에 사용하는 짧은 한국어 근거로 변환합니다.
        private static string GetAIStrategyReason(WIAIStrategy strategy)
        {
            switch (strategy)
            {
                case WIAIStrategy.Development: return "개발";
                case WIAIStrategy.Defense: return "방어";
                case WIAIStrategy.Aggressive: return "공세";
                case WIAIStrategy.Scheme: return "모략";
                default: return "번영";
            }
        }

        // 난이도의 AI 후보 범위 안에서 턴과 고정 소금값으로 재현 가능한 선택 순위를 반환합니다.
        public static int GetAICandidateIndex(WIAdministrationDatabaseSO database, WIAdministrationState state,
            int candidateCount, int salt)
        {
            if (candidateCount <= 1)
            {
                return 0;
            }
            int window = Mathf.Clamp(database.GetDifficulty(state.Difficulty)?.AICandidateWindow ?? 2, 1, candidateCount);
            return Mathf.Abs(state.Turn + salt) % window;
        }

        // 현재 난이도가 AI에게 허용하는 상위 후보 범위를 반환합니다.
        public static int GetAICandidateWindow(WIAdministrationDatabaseSO database, WIAdministrationState state,
            int candidateCount)
        {
            return Mathf.Clamp(database.GetDifficulty(state.Difficulty)?.AICandidateWindow ?? 2, 1, Mathf.Max(1, candidateCount));
        }
    }
}
