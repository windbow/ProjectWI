using System.Linq;
using ProjectWI.Systems;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // QA에서 전역 연구 UGUI를 바로 표시합니다.
        public void OpenResearchUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIResearchRequested?.Invoke();
        }

        // 연구 목록 또는 선택 연구의 담당자 카드를 구성합니다.
        public bool TryGetUGUIResearchPanel(string mode, string researchId,
            out WIAdministrationResearchSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (state == null || database == null)
            {
                error = "연구 정보를 불러올 수 없습니다.";
                return false;
            }
            snapshot = new WIAdministrationResearchSnapshot();
            if (mode == "researchers") BuildResearcherCards(snapshot, researchId);
            else BuildResearchCards(snapshot);
            return true;
        }

        // 선택 연구와 담당자를 기존 연구 시스템에 전달해 연구를 시작합니다.
        public bool BeginUGUIResearch(string researchId, string heroId, out string error)
        {
            bool started = WIAdministrationTurnSystem.BeginResearch(database, state, state.PlayerFactionId, researchId, heroId);
            error = started ? string.Empty : "선행 연구, 기술, 마나 또는 담당자 조건이 부족합니다.";
            if (started)
            {
                WITutorialSystem.Complete(state, "tutorial_research");
                RefreshAll();
            }
            return started;
        }

        // 완료·진행·미완료 연구와 시작 조건을 카드로 구성합니다.
        private void BuildResearchCards(WIAdministrationResearchSnapshot snapshot)
        {
            WIFactionRuntimeState faction = state.GetPlayerFactionState();
            int maximumTechnology = state.Castles.Where(castle => castle.FactionId == state.PlayerFactionId)
                .Select(castle => castle.Technology).DefaultIfEmpty(0).Max();
            snapshot.Title = "기술·마법 연구";
            snapshot.Summary = $"마나 {faction.ManaCrystal:N0} · 최고 기술 {maximumTechnology}\n완료 {faction.CompletedResearchIds.Count}건";
            if (string.IsNullOrEmpty(faction.ActiveResearchId) == false)
            {
                WIResearchDefinition active = database.GetResearch(faction.ActiveResearchId);
                WIHeroDefinition researcher = database.GetHero(faction.ResearcherHeroId);
                snapshot.Cards.Add(new WIAdministrationResearchCardSnapshot
                {
                    Action = "active", Id = active.Id,
                    Title = "진행 중 · " + active.DisplayName.Get(database.UseEnglish),
                    Description = $"{faction.ResearchRemainingMonths}개월 · 담당 {researcher?.DisplayName.Get(database.UseEnglish)}\n{active.Description.Get(database.UseEnglish)}",
                    Portrait = researcher?.Portrait, Interactable = false
                });
            }
            foreach (WIResearchDefinition research in database.ResearchDefinitions)
            {
                if (faction.CompletedResearchIds.Contains(research.Id))
                {
                    snapshot.Cards.Add(new WIAdministrationResearchCardSnapshot
                    {
                        Action = "completed", Id = research.Id,
                        Title = "완료 · " + research.DisplayName.Get(database.UseEnglish),
                        Description = research.Description.Get(database.UseEnglish), Interactable = false
                    });
                    continue;
                }
                bool prerequisite = string.IsNullOrEmpty(research.PrerequisiteResearchId) || faction.CompletedResearchIds.Contains(research.PrerequisiteResearchId);
                bool available = string.IsNullOrEmpty(faction.ActiveResearchId) && faction.ManaCrystal >= research.ManaCost &&
                    maximumTechnology >= research.RequiredTechnology && prerequisite;
                string prerequisiteText = prerequisite ? "선행 충족" : "선행 연구 필요";
                snapshot.Cards.Add(new WIAdministrationResearchCardSnapshot
                {
                    Action = "research", Id = research.Id,
                    Title = research.DisplayName.Get(database.UseEnglish),
                    Description = $"마나 {research.ManaCost} · 기술 {research.RequiredTechnology} · {research.DurationMonths}개월 · {prerequisiteText}\n{research.Description.Get(database.UseEnglish)}",
                    Interactable = available
                });
            }
        }

        // 플레이어 진영의 대기 인물을 지력 순 연구 담당자 카드로 구성합니다.
        private void BuildResearcherCards(WIAdministrationResearchSnapshot snapshot, string researchId)
        {
            WIResearchDefinition research = database.GetResearch(researchId);
            snapshot.Title = "연구 담당 · " + research.DisplayName.Get(database.UseEnglish);
            snapshot.Summary = $"마나 {research.ManaCost} · 기술 {research.RequiredTechnology} · 기본 {research.DurationMonths}개월\n지력이 높은 대기 인물을 선택하십시오.";
            foreach (WICharacterRuntimeState character in state.Characters)
            {
                if (character.Recruited == false || state.IsCharacterBusy(character.HeroId) ||
                    WIAdministrationTurnSystem.IsAdministrationCapable(state, character.HeroId) == false) continue;
                bool inPlayerFaction = state.Castles.Exists(castle => castle.FactionId == state.PlayerFactionId && castle.HeroIds.Contains(character.HeroId));
                if (inPlayerFaction == false) continue;
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                snapshot.Cards.Add(new WIAdministrationResearchCardSnapshot
                {
                    Action = "researcher", Id = hero.Id, Title = hero.DisplayName.Get(database.UseEnglish),
                    Description = $"지력 {hero.Intelligence} · 정무 {hero.Politics}", Portrait = hero.Portrait
                });
            }
            snapshot.Cards = snapshot.Cards.OrderByDescending(card => database.GetHero(card.Id)?.Intelligence ?? 0).ToList();
        }
    }
}
