using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // 군사 목록에서 출발 성·대기 인물·목적지를 순서대로 선택하도록 기존 이동 규칙을 표시합니다.
        private bool BuildCharacterTransferPanel(string mode, string context,
            WIAdministrationMilitarySnapshot snapshot, out string error)
        {
            error = string.Empty;
            snapshot.Title = database.GetText("UI_TRANSFER_TITLE");
            snapshot.Summary = database.GetText("UI_TRANSFER_HINT");
            if (mode == "transfer-castles")
            {
                foreach (var castle in state.Castles.Where(item => item.FactionId == state.PlayerFactionId))
                {
                    snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
                    {
                        Kind = "transfer-castle", Id = castle.CastleId,
                        Title = database.GetCastle(castle.CastleId).DisplayName.Get(database.UseEnglish),
                        Description = string.Format(database.GetText("UI_TRANSFER_RESIDENTS"),
                            WIAdministrationTurnSystem.GetCastleResidentHeroIds(state, castle).Count)
                    });
                }
                return true;
            }
            if (mode == "transfer-actors")
            {
                var castle = state.GetCastle(context);
                if (castle == null || castle.FactionId != state.PlayerFactionId)
                {
                    error = database.GetText("UI_TRANSFER_UNAVAILABLE");
                    return false;
                }
                snapshot.Summary = database.GetCastle(context).DisplayName.Get(database.UseEnglish) + "\n" +
                    database.GetText("UI_TRANSFER_ACTOR_HINT");
                foreach (string id in WIAdministrationTurnSystem.GetCastleResidentHeroIds(state, castle))
                {
                    var hero = database.GetHero(id);
                    if (hero == null)
                    {
                        continue;
                    }
                    bool available = state.IsCharacterBusy(id) == false && castle.GovernorHeroId != id;
                    snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
                    {
                        Kind = "transfer-actor", Id = id, Title = hero.DisplayName.Get(database.UseEnglish),
                        Description = database.GetText(available ? "UI_TRANSFER_READY" : "UI_TRANSFER_BUSY"),
                        Interactable = available
                    });
                }
                if (snapshot.Items.Count == 0)
                {
                    snapshot.Summary += "\n" + database.GetText("UI_TRANSFER_NO_ACTORS");
                }
                return true;
            }
            if (TryGetUGUICharacterTransferTargets(context, out var targets, out error) == false)
            {
                return false;
            }
            snapshot.Summary = database.GetHero(context).DisplayName.Get(database.UseEnglish) + "\n" +
                database.GetText("UI_TRANSFER_DESTINATION_HINT");
            foreach (var target in targets.Candidates)
            {
                snapshot.Items.Add(new WIAdministrationMilitaryItemSnapshot
                {
                    Kind = "transfer-target", Id = target.HeroId, Title = target.DisplayName,
                    Description = target.Summary, Interactable = target.Interactable
                });
            }
            return true;
        }
    }
}
