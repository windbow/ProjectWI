using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using ProjectWI.Administration;
using ProjectWI.Battle;
namespace ProjectWI.Tests.Editor
{
    public class WICardiaDefenseDiagnosticTests
    {
        // 새 메인 캠페인의 카르디아 초기 상태와 공격 도착 시 참가자 누락을 진단합니다.
        [TestCase(0)]
        [TestCase(6)]
        [TestCase(12)]
        public void TraceCardiaDefense(int waitMonths)
        {
            var db=AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var state=WIAdministrationState.Create(db,WICampaignDifficulty.Standard,WICampaignVariant.AresMain);
            var log=new StringBuilder();
            Record(state,log);
            for(int month=0;month<waitMonths;month++)
            {
                WIAdministrationTurnSystem.ExecuteTurn(db,state);
                Record(state,log);
            }
            var origin=state.Castles.First(c=>c.FactionId==state.PlayerFactionId && c.AdjacentCastleIds.Contains("castle_04"));
            string actor=origin.HeroIds.First(id=>state.IsCharacterBusy(id)==false);
            var army=WIAdministrationTurnSystem.CreateArmy(db,state,origin,actor);
            bool march=WIAdministrationTurnSystem.BeginArmyMarch(db,state,army,"castle_04");
            log.AppendLine($"원정 {origin.CastleId}: {march}");
            Assert.IsTrue(march);
            for(int month=0;month<3;month++)
            {
                WIAdministrationTurnSystem.ExecuteTurn(db,state);
                Record(state,log);
                if(state.BattleSessions.Any(b=>b.CastleId=="castle_04"))
                {
                    break;
                }
            }
            var config=AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            foreach(var battle in state.BattleSessions.Where(b=>b.CastleId=="castle_04"))
            {
                var runtime=WIBattleRuntimeBuilder.Build(config,db,battle);
                log.AppendLine($"전투 {battle.SessionId}: 수비전력 {battle.DefenderPowerSnapshot}, 참가자 {battle.DefenderHeroIds.Count}, 실제생성 {runtime.Characters.Count(c=>c.Side==WIBattleSide.Defender)}");
                log.AppendLine($"첫 스텝 결과 {WIBattleSimulation.Step(config,runtime,1f/30f)}");
            }
            Directory.CreateDirectory("GameDocuments/DesignAuditEvidence");
            File.WriteAllText($"GameDocuments/DesignAuditEvidence/Cardia-{waitMonths}.txt",log.ToString(),new UTF8Encoding(false));
        }
        // 성 등록 인물과 카르디아에 머무르거나 이동하는 전투단을 기록합니다.
        private static void Record(WIAdministrationState state,StringBuilder log)
        {
            var castle=state.GetCastle("castle_04");
            log.AppendLine($"턴{state.Turn} 카르디아 {castle.FactionId}: 인물[{string.Join(",",castle.HeroIds)}], 방어{castle.Defense}");
            foreach(var army in state.Armies.Where(a=>a.CurrentCastleId=="castle_04" || a.OriginCastleId=="castle_04" || a.TargetCastleId=="castle_04"))
            {
                log.AppendLine($"{army.ArmyId} {army.FactionId}: {army.CurrentCastleId}->{army.TargetCastleId}, 이동{army.IsMoving}, 작전가능{army.IsOperational}, 인원{army.Members.Count}");
            }
        }
    }
}