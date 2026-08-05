using System;
using System.IO;
using NUnit.Framework;
using ProjectWI.Administration;
using ProjectWI.Systems;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Tests.Editor
{
    public class WICampaignSaveRecoveryTests
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        // 빈 문자열과 잘린 JSON이 상태를 일부 생성하지 않고 명확한 오류로 거부되는지 검증합니다.
        [Test]
        public void TryDeserialize_EmptyAndTruncatedJson_AreRejected()
        {
            Assert.IsFalse(WICampaignSaveSystem.TryDeserialize("   ", out WIAdministrationState empty, out string emptyError));
            Assert.IsNull(empty);
            StringAssert.Contains("비어", emptyError);

            Assert.IsFalse(WICampaignSaveSystem.TryDeserialize("{\"Version\":1,\"Campaign\":{", out WIAdministrationState broken, out string brokenError));
            Assert.IsNull(broken);
            Assert.IsFalse(string.IsNullOrEmpty(brokenError));
        }

        // 세력·성·인물 핵심 목록이 비어 있는 저장은 실행 불가능한 상태로 복구하지 않는지 검증합니다.
        [Test]
        public void TryDeserialize_EmptyCampaignCollections_AreRejected()
        {
            WICampaignSaveEnvelope envelope = new WICampaignSaveEnvelope { Campaign = new WIAdministrationState() };
            string json = JsonUtility.ToJson(envelope);

            Assert.IsFalse(WICampaignSaveSystem.TryDeserialize(json, out _, out string error));
            StringAssert.Contains("필수 캠페인 목록", error);
        }

        // 지원하는 구버전 저장의 누락 선택 목록과 잘못된 월·턴 값을 안전한 기본값으로 복구하는지 검증합니다.
        [Test]
        public void TryDeserialize_LegacyVersion_RepairsOptionalCollectionsAndCounters()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            WIAdministrationState legacy = WIAdministrationState.Create(database);
            legacy.Month = 0;
            legacy.Turn = 0;
            legacy.CompletedTutorialIds = null;
            legacy.SchemeMissions = null;
            legacy.GetCastle("castle_00").HeroLegacies = null;
            WICampaignSaveEnvelope envelope = new WICampaignSaveEnvelope { Version = 0, Campaign = legacy };

            Assert.IsTrue(WICampaignSaveSystem.TryDeserialize(JsonUtility.ToJson(envelope), out WIAdministrationState restored, out string error), error);
            Assert.AreEqual(1, restored.Month);
            Assert.AreEqual(1, restored.Turn);
            Assert.IsNotNull(restored.CompletedTutorialIds);
            Assert.IsNotNull(restored.SchemeMissions);
            Assert.IsNotNull(restored.GetCastle("castle_00").HeroLegacies);
        }

        // 손상된 주 저장 대신 직전 정상 백업을 복원하고 복구 사실을 호출자에게 알리는지 검증합니다.
        [Test]
        public void LoadFile_CorruptedPrimary_RestoresPreviousBackup()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(DatabasePath);
            string path = Path.Combine(Application.temporaryCachePath, $"ProjectWI_SaveRecovery_{Guid.NewGuid():N}.json");
            try
            {
                WIAdministrationState first = WIAdministrationState.Create(database);
                first.Gold = 1111;
                Assert.IsTrue(WICampaignSaveSystem.SaveFile(path, first, out string firstError), firstError);
                WIAdministrationState second = WIAdministrationState.Create(database);
                second.Gold = 2222;
                Assert.IsTrue(WICampaignSaveSystem.SaveFile(path, second, out string secondError), secondError);
                File.WriteAllText(path, "{broken");

                Assert.IsTrue(WICampaignSaveSystem.LoadFile(path, out WIAdministrationState restored, out string recoveryMessage), recoveryMessage);
                Assert.AreEqual(1111, restored.Gold);
                StringAssert.Contains("백업", recoveryMessage);
            }
            finally
            {
                DeleteIfExists(path);
                DeleteIfExists(path + ".bak");
                DeleteIfExists(path + ".tmp");
            }
        }

        // 캠페인 상태가 없는 저장 요청이 기존 슬롯을 빈 파일로 덮어쓰지 않도록 거부되는지 검증합니다.
        [Test]
        public void SaveFile_NullCampaign_IsRejected()
        {
            string path = Path.Combine(Application.temporaryCachePath, $"ProjectWI_NullSave_{Guid.NewGuid():N}.json");
            try
            {
                Assert.IsFalse(WICampaignSaveSystem.SaveFile(path, null, out string error));
                Assert.IsFalse(File.Exists(path));
                StringAssert.Contains("캠페인", error);
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        // 테스트가 만든 정확한 임시 파일만 정리합니다.
        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
