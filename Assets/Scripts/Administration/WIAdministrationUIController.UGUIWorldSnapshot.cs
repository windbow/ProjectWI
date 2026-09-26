using System.Collections.Generic;
using System.Linq;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 이행 중인 UGUI 월드 화면에 현재 게임 상태를 읽기 전용 스냅샷으로 전달합니다.
        public bool TryGetUGUIWorldSnapshot(out WIAdministrationWorldSnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null)
            {
                return false;
            }

            WIFactionDefinition playerFaction = database.Factions.FirstOrDefault(faction => faction.PlayerFaction == true);
            WITurnSummary forecast = WIAdministrationTurnSystem.GetFactionMonthlyIncome(database, state, state.PlayerFactionId);
            WICampaignObjectiveDefinition objective = WICampaignObjectiveSystem.GetCurrent(database, state);
            WICastleRuntimeState castle = GetGlobalSummaryCastle();
            int unresolvedBattleCount = state.BattleSessions.Count(session =>
                session.PlayerInvolved && session.Status != WIBattleSessionStatus.Resolved);

            int objectiveProgress = objective == null ? 0 : WICampaignObjectiveSystem.GetProgress(state, objective);
            int objectiveTarget = objective == null ? 0 : objective.TargetValue;
            string latestNews = state.LastMonthlyReport?.News?.LastOrDefault();
            WICastleDefinition castleDefinition = castle == null ? null : database.GetCastle(castle.CastleId);
            WIFactionDefinition castleOwner = castle == null ? null : database.GetFaction(castle.FactionId);
            int armyCount = castle == null ? 0 : state.Armies.Count(army => army.CurrentCastleId == castle.CastleId);

            snapshot = new WIAdministrationWorldSnapshot
            {
                CampaignStarted = WICampaignRuntimeService.Instance != null && WICampaignRuntimeService.Instance.HasCampaignStarted,
                WorldVisible = uguiWorldVisible
                    && uguiSuspendedForLegacyModal == false
                    && uguiCampaignTitleVisible == false,
                FactionName = playerFaction == null ? database.GetText("UI_GAME_TITLE") : playerFaction.DisplayName.Get(database.UseEnglish),
                Date = $"{state.Year}년 {state.Month:00}월",
                TurnDescription = $"진영 방침: {GetFactionPolicyDisplayName(state.FactionPolicy)} · {state.Month:00}월 명령을 검토하십시오.",
                Gold = $"금화  {FormatHudNumber(state.Gold, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.GoldGained, database.UseEnglish)}/월)</color></size>",
                Mana = $"마나  {FormatHudNumber(state.ManaCrystal, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.ManaGained, database.UseEnglish)}/월)</color></size>",
                Influence = $"영향력  {FormatHudNumber(state.Influence, database.UseEnglish)}  <size=75%><color=#AEB4B8>(+{FormatHudNumber(forecast.InfluenceGained, database.UseEnglish)}/월)</color></size>",
                CastleName = castleDefinition?.DisplayName.Get(database.UseEnglish) ?? castle?.CastleId ?? string.Empty,
                CastleOwner = castle == null ? string.Empty : $"{database.GetCastleSizeName(castle.CastleSize)} · {castleOwner?.DisplayName.Get(database.UseEnglish) ?? castle.FactionId}",
                CastleStats = castle == null ? string.Empty : $"번영 {castle.Prosperity}  ·  기술 {castle.Technology}\n질서 {castle.Stability}  ·  방어 {castle.Defense}",
                CastleHeroes = castle == null ? string.Empty : $"주둔 영웅 {castle.HeroIds.Count}명  ·  주둔 전투단 {armyCount}개",
                MapImage = database.GlobalMapImage,
                CastleImage = castleDefinition?.CastleImage,
                ObjectiveTitle = objective == null ? "현재 목표 없음" : objective.Title.Get(database.UseEnglish),
                ObjectiveProgress = objective == null
                    ? "현재 진행 중인 목표가 없습니다."
                    : $"진행 {objectiveProgress}/{objectiveTarget}\n{objective.Description.Get(database.UseEnglish)}",
                ObjectiveProgressNormalized = objectiveTarget <= 0 ? 0f : UnityEngine.Mathf.Clamp01((float)objectiveProgress / objectiveTarget),
                BattleAlert = unresolvedBattleCount > 0 ? $"전투 발생 {unresolvedBattleCount}건 · 확인" : "현재 전투 없음",
                HasBattleAlert = unresolvedBattleCount > 0 || state.LastMonthlyReport != null,
                MonthlyNews = string.IsNullOrEmpty(latestNews) ? "새로운 월간 보고가 없습니다." : $"최근 소식\n{latestNews}",
                CanEndTurn = unresolvedBattleCount == 0,
                EndTurnText = unresolvedBattleCount == 0 ? "다음 턴" : $"전투 해결 필요 · {unresolvedBattleCount}건"
            };
            if (castle != null)
            {
                WIHeroDefinition governor = database.GetHero(castle.GovernorHeroId);
                snapshot.CastleDetailTitles.AddRange(new[] { "영지관", "번영", "기술", "질서", "방어", "주둔 전투단" });
                snapshot.CastleDetailValues.Add(governor?.DisplayName.Get(database.UseEnglish) ?? "미배치");
                snapshot.CastleDetailValues.Add(castle.Prosperity.ToString());
                snapshot.CastleDetailValues.Add(castle.Technology.ToString());
                snapshot.CastleDetailValues.Add(castle.Stability.ToString());
                snapshot.CastleDetailValues.Add($"{castle.Defense}/100");
                snapshot.CastleDetailValues.Add($"{armyCount}개");
                foreach (string heroId in castle.HeroIds.Take(4))
                {
                    WIHeroDefinition hero = database.GetHero(heroId);
                    WICharacterRuntimeState character = state.GetCharacter(heroId);
                    snapshot.CastleHeroCards.Add(new WIAdministrationWorldHeroSnapshot
                    {
                        DisplayName = hero?.DisplayName.Get(database.UseEnglish) ?? heroId,
                        LevelText = $"Lv.{1 + (character?.Experience ?? 0) / 100}",
                        Portrait = hero?.Portrait
                    });
                }
            }
            foreach (WICastleDefinition definition in database.Castles)
            {
                WICastleRuntimeState castleState = state.GetCastle(definition.Id);
                WIFactionDefinition owner = castleState == null ? null : database.GetFaction(castleState.FactionId);
                snapshot.MapNodes.Add(new WIAdministrationMapNodeSnapshot
                {
                    CastleId = definition.Id,
                    DisplayName = definition.DisplayName.Get(database.UseEnglish),
                    FactionId = castleState?.FactionId ?? definition.FactionId,
                    CastleImage = definition.MapMarkerImage,
                    Position = castleState == null
                        ? new UnityEngine.Vector2(definition.NormalizedMapPosition.x, 1f - definition.NormalizedMapPosition.y)
                        : new UnityEngine.Vector2(castleState.NormalizedMapPosition.x, 1f - castleState.NormalizedMapPosition.y),
                    Selected = selectedCastle != null && selectedCastle.CastleId == definition.Id,
                    Tooltip = $"{owner?.DisplayName.Get(database.UseEnglish) ?? castleState?.FactionId}\n{definition.DisplayName.Get(database.UseEnglish)}"
                });
            }
            HashSet<string> visitedConnections = new HashSet<string>();
            foreach (WICastleDefinition definition in database.Castles)
            {
                WICastleRuntimeState originState = state.GetCastle(definition.Id);
                foreach (string adjacentId in originState.AdjacentCastleIds)
                {
                    string connectionId = string.CompareOrdinal(definition.Id, adjacentId) < 0
                        ? $"{definition.Id}|{adjacentId}"
                        : $"{adjacentId}|{definition.Id}";
                    if (visitedConnections.Add(connectionId) == false)
                    {
                        continue;
                    }

                    WICastleDefinition adjacent = database.GetCastle(adjacentId);
                    WICastleRuntimeState adjacentState = state.GetCastle(adjacentId);
                    if (originState == null || adjacent == null || adjacentState == null)
                    {
                        continue;
                    }

                    bool frontline = originState.FactionId != adjacentState.FactionId;
                    bool selectedRoute = selectedCastle != null &&
                        (selectedCastle.CastleId == definition.Id || selectedCastle.CastleId == adjacentId);
                    WIFactionDefinition owner = database.GetFaction(originState.FactionId);
                    UnityEngine.Color ownerColor = owner?.Color ?? UnityEngine.Color.gray;
                    UnityEngine.Color32 routeColor = selectedRoute
                        ? new UnityEngine.Color32(184, 226, 255, 245)
                        : frontline
                            ? new UnityEngine.Color32(190, 58, 52, 165)
                            : new UnityEngine.Color32(
                                (byte)(ownerColor.r * 170f), (byte)(ownerColor.g * 170f),
                                (byte)(ownerColor.b * 170f), 92);
                    snapshot.MapConnections.Add(new WIAdministrationMapConnectionSnapshot
                    {
                        Start = new UnityEngine.Vector2(originState.NormalizedMapPosition.x,
                            1f - originState.NormalizedMapPosition.y),
                        End = new UnityEngine.Vector2(adjacentState.NormalizedMapPosition.x,
                            1f - adjacentState.NormalizedMapPosition.y),
                        Color = routeColor,
                        Frontline = frontline,
                        Selected = selectedRoute
                    });
                }
            }
            return true;
        }

    }
}
