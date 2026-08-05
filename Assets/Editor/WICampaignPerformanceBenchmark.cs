using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProjectWI.Editor
{
    public sealed class WICampaignPerformanceResult
    {
        public int Months;
        public double ElapsedMilliseconds;
        public long MemoryDeltaBytes;
        public int PendingCount;
        public int ArmyCount;
        public int BattleSessionCount;
        public int TransferCount;
        public int SchemeMissionCount;
        public int LastReportEntryCount;
        public int SaveBytes;
    }

    public static class WICampaignPerformanceBenchmark
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        [MenuItem("ProjectWI/Verification/Run Campaign Long Benchmark")]
        private static void RunFromMenu()
        {
            IReadOnlyList<WICampaignPerformanceResult> results = Run(new[] { 120, 240, 480 });
            Debug.Log("ProjectWI 장시간 캠페인 벤치마크");
            foreach (WICampaignPerformanceResult result in results)
            {
                Debug.Log($"[캠페인 벤치마크] {result.Months}개월 · {result.ElapsedMilliseconds:0.00}ms · " +
                          $"메모리 증감 {result.MemoryDeltaBytes / 1024d:0.0}KB · 대기 {result.PendingCount} · " +
                          $"부대 {result.ArmyCount} · 전투 {result.BattleSessionCount} · 이동 {result.TransferCount} · " +
                          $"계략 {result.SchemeMissionCount} · 월보 {result.LastReportEntryCount} · 저장 {result.SaveBytes}B");
            }
        }

        // 지정 개월마다 새 표준 캠페인을 실행해 시간·메모리·누적 컬렉션과 저장 크기를 측정합니다.
        public static IReadOnlyList<WICampaignPerformanceResult> Run(IReadOnlyList<int> monthCases)
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            if (database == null) throw new InvalidOperationException("내정 데이터베이스를 찾지 못했습니다.");
            List<WICampaignPerformanceResult> results = new List<WICampaignPerformanceResult>();
            foreach (int months in monthCases)
            {
                GC.Collect();
                long memoryBefore = GC.GetTotalMemory(true);
                WIAdministrationState state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard);
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int month = 0; month < months; month += 1)
                {
                    WIAdministrationTurnSystem.ExecuteTurn(database, state);
                }
                stopwatch.Stop();
                long memoryAfter = GC.GetTotalMemory(true);
                results.Add(CreateResult(state, months, stopwatch.Elapsed.TotalMilliseconds,
                    memoryAfter - memoryBefore));
            }
            return results;
        }

        // 장시간 실행 상태에서 무제한 증가 가능성이 있는 컬렉션과 저장 크기를 결과로 요약합니다.
        private static WICampaignPerformanceResult CreateResult(WIAdministrationState state, int months,
            double elapsedMilliseconds, long memoryDeltaBytes)
        {
            int pendingCount = state.PendingProjectEvents.Count + state.PendingLegacyChoices.Count +
                               state.PendingRelationshipEvents.Count + state.PendingRegionalEvents.Count +
                               state.PendingOccupationEvents.Count + state.PendingRecruitmentEvents.Count +
                               state.PendingHeroPromotionIds.Count;
            int reportCount = state.LastMonthlyReport == null ? 0 : state.LastMonthlyReport.News.Count +
                              state.LastMonthlyReport.DelegationReports.Count + state.LastMonthlyReport.AIReasonReports.Count;
            return new WICampaignPerformanceResult
            {
                Months = months,
                ElapsedMilliseconds = elapsedMilliseconds,
                MemoryDeltaBytes = memoryDeltaBytes,
                PendingCount = pendingCount,
                ArmyCount = state.Armies.Count,
                BattleSessionCount = state.BattleSessions.Count,
                TransferCount = state.CharacterTransfers.Count,
                SchemeMissionCount = state.SchemeMissions.Count,
                LastReportEntryCount = reportCount,
                SaveBytes = Encoding.UTF8.GetByteCount(WICampaignSaveSystem.Serialize(state, false))
            };
        }
    }
}
