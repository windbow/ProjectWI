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
                if (envelope.Version < 0)
                {
                    error = $"지원하지 않는 저장 버전입니다: {envelope.Version}";
                    return false;
                }
                Normalize(envelope.Campaign);
                if (ValidateCampaign(envelope.Campaign, out error) == false)
                {
                    return false;
                }
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
            if (state == null)
            {
                error = "저장할 캠페인 데이터가 없습니다.";
                return false;
            }
            string temporaryPath = path + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory) == false)
                {
                    Directory.CreateDirectory(directory);
                }
                string json = Serialize(state);
                if (TryDeserialize(json, out _, out string validationError) == false)
                {
                    error = $"저장 전 검증 실패: {validationError}";
                    return false;
                }
                File.WriteAllText(temporaryPath, json);
                string backupPath = path + ".bak";
                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
                return true;
            }
            catch (Exception exception)
            {
                error = $"저장 실패: {exception.Message}";
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
                catch
                {
                    // 임시 파일 정리 실패는 원래 저장 결과와 오류 메시지를 덮어쓰지 않습니다.
                }
            }
        }

        // 지정한 파일의 JSON을 읽어 캠페인 상태로 복원합니다.
        public static bool LoadFile(string path, out WIAdministrationState state, out string error)
        {
            state = null;
            string backupPath = path + ".bak";
            if (File.Exists(path) == false && File.Exists(backupPath) == false)
            {
                error = "저장 파일이 없습니다.";
                return false;
            }
            try
            {
                string primaryError = "주 저장 파일이 없습니다.";
                if (File.Exists(path) && TryDeserialize(File.ReadAllText(path), out state, out primaryError))
                {
                    error = string.Empty;
                    return true;
                }
                if (File.Exists(backupPath) && TryDeserialize(File.ReadAllText(backupPath), out state, out string backupError))
                {
                    error = $"주 저장을 불러오지 못해 백업을 복구했습니다. 원인: {primaryError}";
                    return true;
                }
                error = File.Exists(backupPath)
                    ? $"주 저장과 백업을 모두 복구하지 못했습니다. 주 저장: {primaryError}"
                    : primaryError;
                return false;
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
            state.PendingRegionalEvents = state.PendingRegionalEvents ?? new System.Collections.Generic.List<WIPendingRegionalEvent>();
            state.CompletedRegionalEventIds = state.CompletedRegionalEventIds ?? new System.Collections.Generic.List<string>();
            state.PendingOccupationEvents = state.PendingOccupationEvents ?? new System.Collections.Generic.List<WIPendingOccupationEvent>();
            state.OccupationPolicyHistory = state.OccupationPolicyHistory ?? new System.Collections.Generic.List<string>();
            state.PendingRecruitmentEvents = state.PendingRecruitmentEvents ?? new System.Collections.Generic.List<WIPendingRecruitmentEvent>();
            state.PendingHeroPromotionIds = state.PendingHeroPromotionIds ?? new System.Collections.Generic.List<string>();
            state.CharacterTransfers = state.CharacterTransfers ?? new System.Collections.Generic.List<WICharacterTransferState>();
            state.SchemeIntel = state.SchemeIntel ?? new System.Collections.Generic.List<WISchemeIntelState>();
            state.SchemeMissions = state.SchemeMissions ?? new System.Collections.Generic.List<WISchemeMissionState>();
            state.CompletedTutorialIds = state.CompletedTutorialIds ?? new System.Collections.Generic.List<string>();
            state.CompletedCampaignObjectiveIds = state.CompletedCampaignObjectiveIds ?? new System.Collections.Generic.List<string>();
            state.Factions.RemoveAll(item => item == null);
            state.DiplomaticRelations.RemoveAll(item => item == null);
            state.Characters.RemoveAll(item => item == null);
            state.Relationships.RemoveAll(item => item == null);
            state.Castles.RemoveAll(item => item == null);
            state.Armies.RemoveAll(item => item == null);
            state.BattleSessions.RemoveAll(item => item == null);
            state.CharacterTransfers.RemoveAll(item => item == null);
            foreach (WICharacterTransferState transfer in state.CharacterTransfers)
            {
                transfer.RouteCastleIds = transfer.RouteCastleIds ??
                    new System.Collections.Generic.List<string>();
            }
            if (state.LastMonthlyReport != null)
            {
                state.LastMonthlyReport.News = state.LastMonthlyReport.News ?? new System.Collections.Generic.List<string>();
                state.LastMonthlyReport.DelegationReports = state.LastMonthlyReport.DelegationReports ?? new System.Collections.Generic.List<string>();
                state.LastMonthlyReport.AIReasonReports = state.LastMonthlyReport.AIReasonReports ?? new System.Collections.Generic.List<string>();
            }
            foreach (WIFactionRuntimeState faction in state.Factions)
            {
                faction.CompletedResearchIds = faction.CompletedResearchIds ?? new System.Collections.Generic.List<string>();
            }
            foreach (WICastleRuntimeState castle in state.Castles)
            {
                if (castle == null) continue;
                castle.HeroIds = castle.HeroIds ?? new System.Collections.Generic.List<string>();
                castle.AdjacentCastleIds = castle.AdjacentCastleIds ?? new System.Collections.Generic.List<string>();
                castle.SpecialFacilityIds = castle.SpecialFacilityIds ?? new System.Collections.Generic.List<string>();
                castle.HeroLegacies = castle.HeroLegacies ?? new System.Collections.Generic.List<WIHeroLegacyState>();
                castle.CommemoratedHeroLegacies = castle.CommemoratedHeroLegacies ?? new System.Collections.Generic.List<WIHeroLegacyState>();
                castle.TavernQuests = castle.TavernQuests ?? new System.Collections.Generic.List<WITavernQuestState>();
            }
            foreach (WIArmyState army in state.Armies)
            {
                if (army != null) army.Members = army.Members ?? new System.Collections.Generic.List<WIArmyMemberState>();
            }
            foreach (WIBattleSessionState session in state.BattleSessions)
            {
                if (session == null) continue;
                session.AttackerHeroIds = session.AttackerHeroIds ?? new System.Collections.Generic.List<WIBattleParticipantState>();
                session.DefenderHeroIds = session.DefenderHeroIds ?? new System.Collections.Generic.List<WIBattleParticipantState>();
            }
            if (string.IsNullOrEmpty(state.PlayerFactionId) && state.Factions.Count > 0)
            {
                state.PlayerFactionId = state.Factions[0].FactionId;
            }
            state.Month = Mathf.Clamp(state.Month <= 0 ? 1 : state.Month, 1, 12);
            state.Turn = Mathf.Max(1, state.Turn);
            state.NextArmyNumber = Mathf.Max(1, state.NextArmyNumber);
            state.NextBattleSessionNumber = Mathf.Max(1, state.NextBattleSessionNumber);
            state.EnsureDefaultDiplomacy();
        }

        // 복구 후에도 게임을 시작할 수 없는 핵심 목록과 플레이어 세력 참조를 거부합니다.
        private static bool ValidateCampaign(WIAdministrationState state, out string error)
        {
            if (state.CampaignResult != WICampaignResult.Ongoing)
            {
                error = string.Empty;
                return true;
            }
            if (state.Factions.Count == 0 || state.Castles.Count == 0 || state.Characters.Count == 0)
            {
                error = "필수 캠페인 목록(세력·성·인물)이 비어 있습니다.";
                return false;
            }
            if (state.Factions.Exists(faction => faction != null && faction.FactionId == state.PlayerFactionId) == false)
            {
                error = "플레이어 세력 참조가 저장 데이터에 없습니다.";
                return false;
            }
            error = string.Empty;
            return true;
        }
    }
}
