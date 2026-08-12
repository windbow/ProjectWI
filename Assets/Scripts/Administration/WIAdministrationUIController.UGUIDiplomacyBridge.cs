using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // QA에서 전역 외교 UGUI를 바로 표시합니다.
        public void OpenDiplomacyUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIDiplomacyRequested?.Invoke();
        }

        // 외교 대상 진영 또는 선택 진영의 실행 명령 카드를 구성합니다.
        public bool TryGetUGUIDiplomacyPanel(string mode, string factionId,
            out WIAdministrationDiplomacySnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (state == null || database == null)
            {
                error = "외교 정보를 불러올 수 없습니다.";
                return false;
            }
            snapshot = new WIAdministrationDiplomacySnapshot();
            if (mode == "detail") BuildDiplomacyCommands(snapshot, factionId);
            else BuildDiplomacyFactions(snapshot);
            return true;
        }

        // 선택한 외교 명령을 기존 게임 시스템에 적용합니다.
        public bool ExecuteUGUIDiplomacyAction(string action, string factionId, string id, out string error)
        {
            error = string.Empty;
            bool succeeded;
            switch (action)
            {
                case "ransom": succeeded = WIAdministrationTurnSystem.RansomPrisoner(database, state, state.PlayerFactionId, id); break;
                case "exchange":
                    string[] prisonerIds = id.Split('|');
                    succeeded = prisonerIds.Length == 2 && WIAdministrationTurnSystem.ExchangePrisoners(
                        state, state.PlayerFactionId, factionId, prisonerIds[0], prisonerIds[1]);
                    break;
                case "improve": succeeded = WIAdministrationTurnSystem.ImproveDiplomaticRelations(state, state.PlayerFactionId, factionId); break;
                case "pact": succeeded = WIAdministrationTurnSystem.SignNonAggression(state, state.PlayerFactionId, factionId); break;
                case "alliance": succeeded = WIAdministrationTurnSystem.FormAlliance(state, state.PlayerFactionId, factionId); break;
                case "aid": succeeded = WIAdministrationTurnSystem.RequestAllianceAid(state, state.PlayerFactionId, factionId); break;
                case "joint": succeeded = WIAdministrationTurnSystem.ProposeJointAttack(database, state, state.PlayerFactionId, factionId, id); break;
                case "war": succeeded = WIAdministrationTurnSystem.DeclareWar(state, state.PlayerFactionId, factionId); break;
                default: error = "지원하지 않는 외교 명령입니다."; return false;
            }
            if (succeeded == false)
            {
                error = "외교 명령 조건이나 자원이 부족합니다.";
                return false;
            }
            RefreshAll();
            return true;
        }

        // 존속 중인 다른 진영과 현재 관계를 카드로 변환합니다.
        private void BuildDiplomacyFactions(WIAdministrationDiplomacySnapshot snapshot)
        {
            WIFactionRuntimeState player = state.GetPlayerFactionState();
            snapshot.Title = "외교";
            snapshot.Summary = $"금화 {player.Gold:N0} · 영향력 {player.Influence:N0}\n복잡한 점수 대신 현재 관계와 실행 가능한 명령만 표시합니다.";
            foreach (WIFactionDefinition faction in database.Factions)
            {
                if (faction.Id == state.PlayerFactionId || state.GetFactionState(faction.Id)?.Eliminated == true) continue;
                WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(state.PlayerFactionId, faction.Id);
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot
                {
                    Action = "faction", Id = faction.Id,
                    Title = faction.DisplayName.Get(database.UseEnglish),
                    Description = GetDiplomaticStatusDisplayName(relation.Status) + " · " + GetDiplomaticDescription(relation)
                });
            }
        }

        // 선택 진영과 가능한 포로·관계·동맹·전쟁 명령을 카드로 구성합니다.
        private void BuildDiplomacyCommands(WIAdministrationDiplomacySnapshot snapshot, string factionId)
        {
            WIFactionDefinition target = database.GetFaction(factionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(state.PlayerFactionId, factionId);
            WIFactionRuntimeState player = state.GetPlayerFactionState();
            snapshot.Title = "외교 · " + target.DisplayName.Get(database.UseEnglish);
            snapshot.Summary = $"{GetDiplomaticStatusDisplayName(relation.Status)} · {GetDiplomaticDescription(relation)}\n금화 {player.Gold:N0} · 영향력 {player.Influence:N0}";
            var ourPrisoners = state.Characters.Where(item => item.Captured && item.CapturedFromFactionId == state.PlayerFactionId && item.CaptorFactionId == factionId).ToList();
            var theirPrisoners = state.Characters.Where(item => item.Captured && item.CapturedFromFactionId == factionId && item.CaptorFactionId == state.PlayerFactionId).ToList();
            foreach (WICharacterRuntimeState prisoner in ourPrisoners)
            {
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot
                {
                    Action = "ransom", Id = prisoner.HeroId,
                    Title = "포로 몸값 · " + (database.GetHero(prisoner.HeroId)?.DisplayName.Get(database.UseEnglish) ?? prisoner.HeroId),
                    Description = $"금화 {database.PrisonerRansomGold}", Interactable = player.Gold >= database.PrisonerRansomGold
                });
            }
            if (ourPrisoners.Count > 0 && theirPrisoners.Count > 0)
            {
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot
                {
                    Action = "exchange", Id = ourPrisoners[0].HeroId + "|" + theirPrisoners[0].HeroId,
                    Title = "포로 맞교환",
                    Description = database.GetHero(ourPrisoners[0].HeroId).DisplayName.Get(database.UseEnglish) + " ↔ " + database.GetHero(theirPrisoners[0].HeroId).DisplayName.Get(database.UseEnglish)
                });
            }
            if (relation.Status == WIDiplomaticStatus.War || relation.Status == WIDiplomaticStatus.Neutral)
            {
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot { Action = "improve", Title = relation.Status == WIDiplomaticStatus.War ? "휴전 교섭" : "친선 사절", Description = $"금화 {WIAdministrationTurnSystem.ImproveRelationsGoldCost} · 영향력 {WIAdministrationTurnSystem.ImproveRelationsInfluenceCost}" });
            }
            else if (relation.Status == WIDiplomaticStatus.Friendly)
            {
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot { Action = "pact", Title = "불가침 협정", Description = $"영향력 {WIAdministrationTurnSystem.NonAggressionInfluenceCost}" });
            }
            else if (relation.Status == WIDiplomaticStatus.NonAggression)
            {
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot { Action = "alliance", Title = "동맹 체결", Description = $"영향력 {WIAdministrationTurnSystem.AllianceInfluenceCost}" });
            }
            else if (relation.Status == WIDiplomaticStatus.Alliance)
            {
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot { Action = "aid", Title = "금화 원조 요청", Description = relation.AidCooldownMonths > 0 ? $"{relation.AidCooldownMonths}개월 후 재요청" : $"금화 +{WIAdministrationTurnSystem.AllianceAidGold}", Interactable = relation.AidCooldownMonths <= 0 });
                foreach (WICastleRuntimeState castle in state.Castles.Where(item => item.FactionId != state.PlayerFactionId && item.FactionId != factionId && WIAdministrationTurnSystem.AreFactionsAtWar(state, state.PlayerFactionId, item.FactionId) && WIAdministrationTurnSystem.AreFactionsAtWar(state, factionId, item.FactionId)).GroupBy(item => item.FactionId).Select(group => group.First()))
                {
                    snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot { Action = "joint", Id = castle.CastleId, Title = "공동 공격 제안", Description = database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish) + $" · 영향력 {database.JointAttackInfluenceCost} · {database.JointAttackDurationMonths}개월" });
                }
            }
            if (relation.Status != WIDiplomaticStatus.War)
            {
                snapshot.Cards.Add(new WIAdministrationDiplomacyCardSnapshot { Action = "war", Title = "선전포고", Description = $"위험 명령 · 영향력 {WIAdministrationTurnSystem.DeclareWarInfluenceCost}" });
            }
        }
    }
}
