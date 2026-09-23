using System.Linq;
using ProjectWI.Systems;
using UnityEngine;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // QA에서 전역 첩보 UGUI를 바로 표시합니다.
        public void OpenSchemeUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationSchemeUGUIController>(() => UGUISchemeRequested);
        }

        // 첩보 종류·담당자·대상 성·대상 인물 단계의 카드를 구성합니다.
        public bool TryGetUGUISchemePanel(string mode, string schemeId, string agentId, string castleId,
            out WIAdministrationSchemeSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (state == null || database == null)
            {
                error = "첩보 정보를 불러올 수 없습니다.";
                return false;
            }
            snapshot = new WIAdministrationSchemeSnapshot();
            switch (mode)
            {
                case "schemes": BuildSchemeDefinitions(snapshot); break;
                case "agents": BuildSchemeAgents(snapshot, schemeId); break;
                case "castles": BuildSchemeCastles(snapshot, schemeId, agentId); break;
                case "heroes": BuildSchemeHeroes(snapshot, schemeId, agentId, castleId); break;
                default: error = "알 수 없는 첩보 화면입니다."; return false;
            }
            return true;
        }

        // 선택한 첩보 임무를 한 달 임무로 예약합니다.
        public bool ScheduleUGUIScheme(string schemeId, string agentId, string castleId, string heroId, out string error)
        {
            bool scheduled = WISchemeSystem.TrySchedule(database, state, schemeId, state.PlayerFactionId,
                agentId, castleId, string.IsNullOrEmpty(heroId) ? null : heroId, Random.Range(0, 100), out string message);
            error = scheduled ? string.Empty : message;
            if (scheduled) RefreshAll();
            return scheduled;
        }

        // 진행 중 임무와 실행 가능한 첩보 종류를 첫 화면 카드로 구성합니다.
        private void BuildSchemeDefinitions(WIAdministrationSchemeSnapshot snapshot)
        {
            WIFactionRuntimeState faction = state.GetFactionState(state.PlayerFactionId);
            snapshot.Title = "첩보";
            snapshot.Summary = $"보유 영향력 {faction.Influence:N0}\n지력이 높은 인물일수록 성공률이 높고 적 성의 질서와 방첩이 이를 낮춥니다.";
            foreach (WISchemeMissionState mission in state.SchemeMissions.Where(item => item.InitiatorFactionId == state.PlayerFactionId))
            {
                WISchemeDefinition scheme = database.GetScheme(mission.SchemeId);
                WIHeroDefinition agent = database.GetHero(mission.AgentHeroId);
                WICastleDefinition target = database.GetCastle(mission.TargetCastleId);
                snapshot.Cards.Add(new WIAdministrationSchemeCardSnapshot
                {
                    Action = "mission", Title = "진행 중 · " + scheme?.DisplayName.Get(database.UseEnglish),
                    Description = $"{agent?.DisplayName.Get(database.UseEnglish)} → {target?.DisplayName.Get(database.UseEnglish)} · {mission.RemainingMonths}개월",
                    Portrait = agent?.Portrait, Interactable = false
                });
            }
            foreach (WISchemeDefinition scheme in database.SchemeDefinitions)
            {
                snapshot.Cards.Add(new WIAdministrationSchemeCardSnapshot
                {
                    Action = "scheme", Id = scheme.Id,
                    Title = scheme.DisplayName.Get(database.UseEnglish) + $" · 영향력 {scheme.InfluenceCost}",
                    Description = $"기본 성공 {scheme.BaseSuccessChance}% · 발각 {scheme.BaseDetectionChance}%\n{scheme.Description.Get(database.UseEnglish)}",
                    Interactable = faction.Influence >= scheme.InfluenceCost
                });
            }
        }

        // 플레이어 성에 있는 대기 인물을 첩보 담당자 카드로 구성합니다.
        private void BuildSchemeAgents(WIAdministrationSchemeSnapshot snapshot, string schemeId)
        {
            WISchemeDefinition scheme = database.GetScheme(schemeId);
            snapshot.Title = scheme.DisplayName.Get(database.UseEnglish) + " · 담당 인물";
            snapshot.Summary = "다른 임무가 없는 인물 중 지력이 높은 담당자를 선택하십시오.";
            foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId == state.PlayerFactionId))
            {
                foreach (string heroId in castle.HeroIds.Where(id => state.IsCharacterBusy(id) == false &&
                    WIAdministrationTurnSystem.CanPerformScheme(state, id)))
                {
                    WIHeroDefinition hero = database.GetHero(heroId);
                    if (hero == null) continue;
                    snapshot.Cards.Add(new WIAdministrationSchemeCardSnapshot
                    {
                        Action = "agent", Id = heroId, Title = hero.DisplayName.Get(database.UseEnglish),
                        Description = $"지력 {hero.Intelligence} · {database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish)}",
                        Portrait = hero.Portrait
                    });
                }
            }
            snapshot.Cards = snapshot.Cards.OrderByDescending(item => database.GetHero(item.Id)?.Intelligence ?? 0).ToList();
        }

        // 첩보 종류에 맞는 아군 또는 적 성과 공개 가능한 계산 근거를 구성합니다.
        private void BuildSchemeCastles(WIAdministrationSchemeSnapshot snapshot, string schemeId, string agentId)
        {
            WISchemeDefinition scheme = database.GetScheme(schemeId);
            WIHeroDefinition agent = database.GetHero(agentId);
            bool ownTarget = scheme.SchemeType == WISchemeType.Counterintelligence;
            snapshot.Title = scheme.DisplayName.Get(database.UseEnglish) + " · 대상 성";
            snapshot.Summary = $"담당 {agent.DisplayName.Get(database.UseEnglish)} · 지력 {agent.Intelligence}";
            foreach (WICastleRuntimeState castleState in state.Castles.Where(item => ownTarget ? item.FactionId == state.PlayerFactionId : item.FactionId != state.PlayerFactionId))
            {
                WISchemeIntelState intel = state.SchemeIntel.Find(item => item.ObserverFactionId == state.PlayerFactionId && item.TargetCastleId == castleState.CastleId);
                bool revealed = ownTarget || intel != null;
                string detail;
                if (revealed)
                {
                    int success = WISchemeSystem.CalculateSuccessChance(scheme, agent, castleState);
                    detail = $"성공 {success}% · 질서 {castleState.Stability} · 방첩 {castleState.CounterintelligenceMonths}개월";
                }
                else
                {
                    detail = $"정보 미확보 · 기본 성공 {scheme.BaseSuccessChance}%";
                }
                snapshot.Cards.Add(new WIAdministrationSchemeCardSnapshot
                {
                    Action = scheme.SchemeType == WISchemeType.Alienation ? "castle-heroes" : "execute-castle",
                    Id = castleState.CastleId,
                    Title = database.GetCastle(castleState.CastleId).DisplayName.Get(database.UseEnglish),
                    Description = detail
                });
            }
        }

        // 인재 이간 대상 성의 공개된 주둔 인물을 카드로 구성합니다.
        private void BuildSchemeHeroes(WIAdministrationSchemeSnapshot snapshot, string schemeId, string agentId, string castleId)
        {
            WICastleRuntimeState castle = state.GetCastle(castleId);
            snapshot.Title = "인재 이간 · 대상 인물";
            snapshot.Summary = database.GetCastle(castleId).DisplayName.Get(database.UseEnglish);
            if (WIInformationVisibility.CanViewCastleDetails(state, state.PlayerFactionId, castle) == false) return;
            foreach (string heroId in castle.HeroIds)
            {
                WIHeroDefinition hero = database.GetHero(heroId);
                WICharacterRuntimeState character = state.GetCharacter(heroId);
                if (hero == null || character == null) continue;
                snapshot.Cards.Add(new WIAdministrationSchemeCardSnapshot
                {
                    Action = "execute-hero", Id = heroId, Title = hero.DisplayName.Get(database.UseEnglish),
                    Description = "충성 " + GetLoyaltyDisplayName(character.LoyaltyState), Portrait = hero.Portrait
                });
            }
        }
    }
}
