using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectWI.Administration;
namespace ProjectWI.Tests.Editor
{
    public class WIArmyBatchSelectionTests
    {
        // 실제 메인 시작 인물로 일괄 편성의 성공과 잘못된 후보가 포함된 경우의 무변경을 확인합니다.
        [TestCase(false)]
        [TestCase(true)]
        public void BatchSelectionValidatesAllBeforeAssigning(bool invalid)
        {
            var database=AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var state=WIAdministrationState.Create(database,WICampaignDifficulty.Standard,WICampaignVariant.AresMain);
            var castle=state.Castles.First(c=>c.FactionId==state.PlayerFactionId);
            var available=castle.HeroIds.Where(id=>state.IsCharacterBusy(id)==false).ToList();
            Assert.GreaterOrEqual(available.Count,3);
            var army=WIAdministrationTurnSystem.CreateArmy(database,state,castle,available[0]);
            var selected=available.Skip(1).Take(2).ToList();
            if(invalid)
            {
                selected.Add("missing-character");
            }
            var host=new GameObject("WIArmyBatchTest");
            host.SetActive(false);
            try
            {
                var controller=host.AddComponent<WIAdministrationUIController>();
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(WIAdministrationUIController).GetField("state",flags).SetValue(controller,state);
                typeof(WIAdministrationUIController).GetField("database",flags).SetValue(controller,database);
                bool result=controller.AssignUGUIArmyMembers(army.ArmyId,selected,out string error);
                Assert.AreEqual(invalid==false,result,error);
                Assert.AreEqual(invalid?1:3,army.Members.Count);
                if(invalid==false)
                {
                    Assert.IsTrue(selected.All(id=>army.Members.Any(m=>m.HeroId==id)));
                    Assert.AreEqual(1,army.Members.Count(m=>m.Role==WIUnitRole.Commander));
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}