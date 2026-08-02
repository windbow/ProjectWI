using System;
using System.IO;
using ProjectWI.Administration;
using UnityEngine;

namespace ProjectWI.Systems
{
    [Serializable]
    public class WICampaignSaveEnvelope
    {
        public int Version = WICampaignSaveSystem.CurrentVersion;
        public string SavedAtUtc;
        public WIAdministrationState Campaign;
    }

    public static class WICampaignSaveSystem
    {
        public const int CurrentVersion = 1;

        // 캠페인 상태를 버전과 저장 시각을 포함한 JSON 문자열로 직렬화합니다.
        public static string Serialize(WIAdministrationState state, bool prettyPrint = true)
        {
            if (state == null)
            {
                return string.Empty;
            }
            WICampaignSaveEnvelope envelope = new WICampaignSaveEnvelope
            {
                SavedAtUtc = DateTime.UtcNow.ToString("O"),
                Campaign = state
            };
            return JsonUtility.ToJson(envelope, prettyPrint);
        }

        // 저장 JSON을 검사하고 현재 버전의 캠페인 상태로 역직렬화합니다.
        public static bool TryDeserialize(string json, out WIAdministrationState state, out string error)
        {
            state = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "저장 데이터가 비어 있습니다.";
                return false;
            }
            try
            {
                WICampaignSaveEnvelope envelope = JsonUtility.FromJson<WICampaignSaveEnvelope>(json);
                if (envelope == null || envelope.Campaign == null)
                {
                    error = "캠페인 데이터가 없습니다.";
                    return false;
                }
                if (envelope.Version > CurrentVersion)
                {
                    error = $"지원하지 않는 미래 저장 버전입니다: {envelope.Version}";
                    return false;
                }
                Normalize(envelope.Campaign);
                state = envelope.Campaign;
                return true;
            }
            catch (Exception exception)
            {
                error = $"저장 데이터 해석 실패: {exception.Message}";
                return false;
            }
        }

        // 저장 슬롯 JSON을 임시 파일에 먼저 기록한 뒤 대상 파일로 교체합니다.
        public static bool SaveFile(string path, WIAdministrationState state, out string error)
        {
            error = string.Empty;
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory) == false)
                {
                    Directory.CreateDirectory(directory);
                }
                string temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, Serialize(state));
                File.Copy(temporaryPath, path, true);
                File.Delete(temporaryPath);
                return true;
            }
            catch (Exception exception)
            {
                error = $"저장 실패: {exception.Message}";
                return false;
            }
        }

        // 지정한 파일의 JSON을 읽어 캠페인 상태로 복원합니다.
        public static bool LoadFile(string path, out WIAdministrationState state, out string error)
        {
            state = null;
            if (File.Exists(path) == false)
            {
                error = "저장 파일이 없습니다.";
                return false;
            }
            try
            {
                return TryDeserialize(File.ReadAllText(path), out state, out error);
            }
            catch (Exception exception)
            {
                error = $"불러오기 실패: {exception.Message}";
                return false;
            }
        }

        // 이전 버전이나 누락 필드에서 필수 컬렉션과 플레이어 세력 ID를 복구합니다.
        private static void Normalize(WIAdministrationState state)
        {
            state.Factions = state.Factions ?? new System.Collections.Generic.List<WIFactionRuntimeState>();
            state.DiplomaticRelations = state.DiplomaticRelations ?? new System.Collections.Generic.List<WIDiplomaticRelationState>();
            state.Characters = state.Characters ?? new System.Collections.Generic.List<WICharacterRuntimeState>();
            state.Relationships = state.Relationships ?? new System.Collections.Generic.List<WIRelationshipState>();
            state.Castles = state.Castles ?? new System.Collections.Generic.List<WICastleRuntimeState>();
            state.Armies = state.Armies ?? new System.Collections.Generic.List<WIArmyState>();
            state.BattleSessions = state.BattleSessions ?? new System.Collections.Generic.List<WIBattleSessionState>();
            state.PendingProjectEvents = state.PendingProjectEvents ?? new System.Collections.Generic.List<WIPendingProjectEvent>();
            state.PendingLegacyChoices = state.PendingLegacyChoices ?? new System.Collections.Generic.List<WIPendingLegacyChoice>();
            state.PendingRelationshipEvents = state.PendingRelationshipEvents ?? new System.Collections.Generic.List<WIPendingRelationshipEvent>();
            state.CompletedRelationshipEventKeys = state.CompletedRelationshipEventKeys ?? new System.Collections.Generic.List<string>();
            state.PendingRecruitmentEvents = state.PendingRecruitmentEvents ?? new System.Collections.Generic.List<WIPendingRecruitmentEvent>();
            state.PendingHeroPromotionIds = state.PendingHeroPromotionIds ?? new System.Collections.Generic.List<string>();
            state.CharacterTransfers = state.CharacterTransfers ?? new System.Collections.Generic.List<WICharacterTransferState>();
            state.SchemeIntel = state.SchemeIntel ?? new System.Collections.Generic.List<WISchemeIntelState>();
            state.CompletedTutorialIds = state.CompletedTutorialIds ?? new System.Collections.Generic.List<string>();
            foreach (WIFactionRuntimeState faction in state.Factions)
            {
                faction.CompletedResearchIds = faction.CompletedResearchIds ?? new System.Collections.Generic.List<string>();
            }
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                castle.HeroLegacies = castle.HeroLegacies ?? new System.Collections.Generic.List<WIHeroLegacyState>();
                castle.CommemoratedHeroLegacies = castle.CommemoratedHeroLegacies ?? new System.Collections.Generic.List<WIHeroLegacyState>();
            }
            if (string.IsNullOrEmpty(state.PlayerFactionId) && state.Factions.Count > 0)
            {
                state.PlayerFactionId = state.Factions[0].FactionId;
            }
            state.EnsureDefaultDiplomacy();
        }
    }
}
