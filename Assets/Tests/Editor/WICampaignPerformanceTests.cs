using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectWI.Editor;

namespace ProjectWI.Tests.Editor
{
    public class WICampaignPerformanceTests
    {
        // 10·20·40년 캠페인이 시간·메모리·저장 크기와 누적 컬렉션 상한 안에서 완료되는지 검증합니다.
        [Test]
        public void LongCampaign_RemainsBoundedThroughFortyYears()
        {
            IReadOnlyList<WICampaignPerformanceResult> results =
                WICampaignPerformanceBenchmark.Run(new[] { 120, 240, 480 });

            Assert.AreEqual(3, results.Count);
            foreach (WICampaignPerformanceResult result in results)
            {
                Assert.Less(result.ElapsedMilliseconds, 10000d, $"{result.Months}개월 실행 시간 초과");
                Assert.Less(System.Math.Abs(result.MemoryDeltaBytes), 64L * 1024L * 1024L,
                    $"{result.Months}개월 관리 메모리 증감 초과");
                Assert.Less(result.SaveBytes, 2 * 1024 * 1024, $"{result.Months}개월 저장 크기 초과");
                Assert.LessOrEqual(result.PendingCount, 100, $"{result.Months}개월 대기 사건 누적 초과");
                Assert.LessOrEqual(result.LastReportEntryCount, 100, $"{result.Months}개월 월간 보고 누적 초과");
                Assert.LessOrEqual(result.BattleSessionCount, 100, $"{result.Months}개월 전투 기록 누적 초과");
                Assert.LessOrEqual(result.TransferCount, 100, $"{result.Months}개월 이동 기록 누적 초과");
                Assert.LessOrEqual(result.SchemeMissionCount, 100, $"{result.Months}개월 첩보 기록 누적 초과");
            }

            Assert.Less(results.Max(item => item.SaveBytes) - results.Min(item => item.SaveBytes), 64 * 1024,
                "진행 기간에 비례해 저장 데이터가 계속 증가합니다.");
        }
    }
}
