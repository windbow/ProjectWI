using System.Linq;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using ProjectWI.Administration;

namespace ProjectWI.Tests.Editor
{
    public class WIGameDesignDiagnosticTests
    {
        // 동일한 메인 60개월 상태에서 실제 이동·편성·원정 명령의 대안을 비교합니다.
        [Explicit("수정 전 자동 정책의 6성 배치 전용 비교입니다. 현재 정책의 일반 회귀 검사로 실행하지 않습니다.")]
        [TestCase(false)]
        [TestCase(true)]
        public void CompareMainMidgameCommands(bool reinforce)
        {
            var database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var state = WIAdministrationState.Create(database,WICampaignDifficulty.Standard,WICampaignVariant.AresMain);
            WICampaignAutoPlayer.Run(database,state,WIAutoPlayerPolicy.Balanced,60);
            var report = new StringBuilder();
            report.AppendLine($"경제 시작: 금화 {state.Gold}, 번영합 {state.Castles.Where(c=>c.FactionId==state.PlayerFactionId).Sum(c=>c.Prosperity)}");
            foreach (var target in state.Castles.Where(c => c.FactionId != state.PlayerFactionId && c.AdjacentCastleIds.Any(id => state.GetCastle(id)?.FactionId == state.PlayerFactionId)))
            {
                var defenders = state.Armies.Where(a => a.FactionId == target.FactionId && a.CurrentCastleId == target.CastleId && a.IsOperational).ToList();
                report.AppendLine($"접경 {target.CastleId}: 전쟁 {WIAdministrationTurnSystem.AreFactionsAtWar(state,state.PlayerFactionId,target.FactionId)}, 수비 {WIAdministrationTurnSystem.GetCastleDefensePower(database,state,target,defenders)}");
            }
            if (reinforce)
            {
                foreach (var castle in state.Castles.Where(c => c.FactionId == state.PlayerFactionId).ToList())
                {
                    foreach (string id in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false && id != castle.GovernorHeroId && state.GetCharacter(id).BaseGrade == WICharacterGrade.Hero).ToList())
                    {
                        report.AppendLine($"이동조건 {id}: 거주 인원 {WIAdministrationTurnSystem.GetCastleResidentHeroIds(state,state.GetCastle("castle_31")).Count}/{state.GetCastle("castle_31").GetHeroSlotCount()}, 예약 {state.CharacterTransfers.Count(t=>t.TargetCastleId=="castle_31")}, 경로 {string.Join(",",WIAdministrationTurnSystem.GetCharacterTransferPath(state,castle.CastleId,"castle_31"))}");
                        bool moved = WIAdministrationTurnSystem.StartCharacterTransfer(database,state,id,"castle_31");
                        report.AppendLine($"이동 {id}@{castle.CastleId}: {moved}");
                    }
                }
                Assert.AreEqual(4,state.CharacterTransfers.Count,"메인의 후방 영웅 네 명을 예약해야 합니다.");
                Assert.IsTrue(state.IsCharacterBusy("ares"),"이동 중 인물은 내정에 사용할 수 없어야 합니다.");
                for (int month=0; month<8 && state.CharacterTransfers.Count>0; month++)
                {
                    WIAdministrationTurnSystem.ExecuteTurn(database,state);
                }
                Assert.IsEmpty(state.CharacterTransfers,"아군 경로 이동 후 모두 도착해야 합니다.");
                Assert.LessOrEqual(WIAdministrationTurnSystem.GetCastleResidentHeroIds(state,state.GetCastle("castle_31")).Count,state.GetCastle("castle_31").GetHeroSlotCount());
                foreach (var army in state.Armies.Where(a => a.FactionId == state.PlayerFactionId && a.CurrentCastleId == "castle_31" && a.IsOperational).ToList())
                {
                    foreach (string id in state.GetCastle("castle_31").HeroIds.Where(id => state.IsCharacterBusy(id) == false && id != state.GetCastle("castle_31").GovernorHeroId).ToList())
                    {
                        if (WIAdministrationTurnSystem.AddArmyMember(database,state,army,id,WIUnitRole.Melee))
                        {
                            report.AppendLine($"편성 {army.ArmyId}: {id}");
                        }
                    }
                }
            }
            report.AppendLine($"출발 전력 {state.Armies.Where(a => a.FactionId == state.PlayerFactionId && a.CurrentCastleId == "castle_31").Sum(a => WIAdministrationTurnSystem.GetArmyBattlePower(database,state,a))}, 턴 {state.Turn}");
            foreach (var army in state.Armies.Where(a => a.FactionId == state.PlayerFactionId && a.CurrentCastleId == "castle_31" && a.IsOperational).ToList())
            {
                bool started = WIAdministrationTurnSystem.BeginArmyMarch(database,state,army,"castle_29");
                if (started)
                {
                    army.StrategicTargetCastleId="castle_29";
                    army.Mission=WIArmyMission.Attack;
                }
                report.AppendLine($"원정 {army.ArmyId}: {started}");
            }
            for (int month=0;month<3;month++)
            {
                WIAdministrationTurnSystem.ExecuteTurn(database,state);
                if (state.BattleSessions.Any(b=>b.CastleId=="castle_29" && b.Status != WIBattleSessionStatus.Resolved))
                {
                    break;
                }
            }
            foreach (var battle in state.BattleSessions.Where(b=>b.CastleId=="castle_29"))
            {
                report.AppendLine($"전투 {battle.SessionId}: 공격 {battle.AttackerPowerSnapshot}, 수비 {battle.DefenderPowerSnapshot}, 상태 {battle.Status}");
                var config = AssetDatabase.LoadAssetAtPath<ProjectWI.Battle.WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
                var runtime = ProjectWI.Battle.WIBattleRuntimeBuilder.Build(config,database,battle);
                for (int tick=0;tick<18000 && runtime.Finished == false;tick++)
                {
                    ProjectWI.Battle.WIBattleSimulation.Step(config,runtime,1f/30f);
                }
                report.AppendLine($"무조작 전투: {runtime.AttackerOutcome}, 종료 {runtime.Finished}, 시간 {runtime.ElapsedSeconds}, 공격 전투불능 {runtime.Characters.Count(c=>c.Side==ProjectWI.Battle.WIBattleSide.Attacker && c.IsAlive==false)}/{runtime.Characters.Count(c=>c.Side==ProjectWI.Battle.WIBattleSide.Attacker)}");
            }
            Directory.CreateDirectory("GameDocuments/DesignAuditEvidence");
            File.WriteAllText($"GameDocuments/DesignAuditEvidence/MainAlternative-{reinforce}.txt",report.ToString(),new UTF8Encoding(false));
            Assert.IsNotNull(database);
        }

        // 원본 데이터에서 실행한 캠페인의 정체와 실제 명령으로 가능한 대안을 기록합니다.
        [TestCase(WICampaignVariant.AresMain, 24)]
        [TestCase(WICampaignVariant.AresMain, 60)]
        [TestCase(WICampaignVariant.AresMain, 120)]
        public void RecordExistingDataAlternatives(WICampaignVariant variant, int months)
        {
            var database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(
                "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var state = WIAdministrationState.Create(database, WICampaignDifficulty.Standard, variant);
            var metrics = WICampaignAutoPlayer.Run(database, state, WIAutoPlayerPolicy.Balanced, months);
            var report = new StringBuilder();
            report.AppendLine($"경제 시작: 금화 {state.Gold}, 번영합 {state.Castles.Where(c=>c.FactionId==state.PlayerFactionId).Sum(c=>c.Prosperity)}");
            report.AppendLine($"{variant} Balanced {months}: 성 {metrics.FinalPlayerCastleCount}, 원정 {metrics.MarchesStarted}, 승/패 {metrics.PlayerVictories}/{metrics.PlayerDefeats}, 선택 {metrics.DecisionsResolved}, 결과 {state.CampaignResult}, 발도르 잔여 {state.Castles.Count(item => item.FactionId == "valdor")}, 신규영입 {metrics.CharactersRecruited}");
            foreach (var castle in state.Castles.Where(item => item.FactionId == state.PlayerFactionId))
            {
                foreach (string id in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false))
                {
                    var character = state.GetCharacter(id);
                    report.AppendLine($"대기 {id}: 등급 {character.BaseGrade}, 내정 {WIAdministrationTurnSystem.IsAdministrationCapable(state,id)}, 영입 {WIAdministrationTurnSystem.CanRecruitTalent(state,id)}, 탐색가능 {WIAdministrationTurnSystem.CanPerformCharacterActivity(state,id,WICharacterActivityType.Search,database.Automation.TalentOfficeCapacity)}");
                }
            }
            if (variant == WICampaignVariant.Free)
            {
                foreach (var army in state.Armies.Where(item => item.FactionId == state.PlayerFactionId && item.IsOperational).ToList())
                {
                    int before = WIAdministrationTurnSystem.GetArmyBattlePower(database,state,army);
                    var castle = state.GetCastle(army.CurrentCastleId);
                    foreach (string id in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false && id != castle.GovernorHeroId).ToList())
                    {
                        if (WIAdministrationTurnSystem.AddArmyMember(database,state,army,id,WIUnitRole.Melee))
                        {
                            report.AppendLine($"추가 편성 성공 {army.ArmyId}: {id}");
                        }
                    }
                    report.AppendLine($"편성 전후 {army.ArmyId}: {before} -> {WIAdministrationTurnSystem.GetArmyBattlePower(database,state,army)}");
                }
                bool declared = WIAdministrationTurnSystem.DeclareWar(state,state.PlayerFactionId,"sylvanroad");
                report.AppendLine($"실반로드 선전포고 가능: {declared}");
                foreach (var army in state.Armies.Where(item => item.FactionId == state.PlayerFactionId && item.IsOperational).ToList())
                {
                    if (WIAdministrationTurnSystem.BeginArmyMarch(database,state,army,"castle_38"))
                    {
                        report.AppendLine($"대체 전선 원정 명령 성공: {army.ArmyId}");
                    }
                }
            }
            report.AppendLine($"목표 {metrics.FinalStrategicTargetCastleId}, 집결 {metrics.FinalStrategicAssemblyPower}, 필요 {metrics.FinalStrategicRequiredPower}, 전체전력 {metrics.FinalPlayerArmyPower}, 영향력 {metrics.FinalInfluence}, 전쟁 {metrics.FinalAtWarWithValdor}");
            foreach (var army in state.Armies.Where(item => item.FactionId == state.PlayerFactionId))
            {
                report.AppendLine($"전투단 {army.ArmyId}@{army.CurrentCastleId} -> {army.TargetCastleId}, 임무 {army.Mission}, 인원 {army.Members.Count}, 전력 {WIAdministrationTurnSystem.GetArmyBattlePower(database,state,army)}, 이동 {army.IsMoving}, 재편 {army.ReorganizationMonths}");
            }
            foreach (var trace in metrics.DecisionTraces.Where(item => item.Month > months - 12))
            {
                report.AppendLine($"{trace.Month}: {trace.ReasonCode} {trace.TargetCastleId} {trace.Description}");
            }
            Directory.CreateDirectory("GameDocuments/DesignAuditEvidence");
            File.WriteAllText($"GameDocuments/DesignAuditEvidence/{variant}-{months}.txt",report.ToString().Replace("\r\n","\n").Replace("\n","\r\n"),new UTF8Encoding(false));
            Assert.IsNotNull(database);
        }
    }
}