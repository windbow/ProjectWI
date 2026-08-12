using System.Collections.Generic;
using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // QA에서 전역 진영 정세 UGUI를 바로 표시합니다.
        public void OpenFactionUGUIForQA()
        {
            OpenCastlePreviewForQA();
            UGUIFactionRequested?.Invoke();
        }

        // 대륙의 5대 진영 공개 정보를 읽기 전용 카드 스냅샷으로 구성합니다.
        public bool TryGetUGUIFactionSnapshot(out WIAdministrationFactionSnapshot snapshot)
        {
            snapshot = null;
            if (state == null || database == null) return false;
            snapshot = new WIAdministrationFactionSnapshot
            {
                Summary = $"제 {state.Turn}턴 · 대륙 {database.Factions.Count}개 진영\n플레이어 자원은 공개하고 다른 진영은 영토·성향·외교 관계만 표시합니다."
            };
            WIFactionDefinition playerFaction = database.GetFaction(state.PlayerFactionId);
            foreach (WIFactionDefinition faction in database.Factions)
            {
                int castleCount = state.Castles.Count(castle => castle.FactionId == faction.Id);
                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                string status = factionState?.Eliminated == true ? $"멸망 · 제 {factionState.EliminatedTurn}턴" : "존속";
                string economy = faction.PlayerFaction && factionState != null
                    ? $"\n금화 {factionState.Gold:N0} · 마나 {factionState.ManaCrystal:N0} · 영향력 {factionState.Influence:N0} · 방침 {GetFactionPolicyDisplayName(factionState.Policy)}"
                    : string.Empty;
                List<string> relationships = new();
                foreach (WIStartingRelationshipDefinition relationship in database.StartingRelationships.Where(item => item.FactionId == faction.Id))
                {
                    WIHeroDefinition first = database.GetHero(relationship.FirstHeroId);
                    WIHeroDefinition second = database.GetHero(relationship.SecondHeroId);
                    if (first == null || second == null) continue;
                    string level = relationship.Level == WIRelationshipLevel.Conflict ? "갈등" : "친애";
                    relationships.Add($"{first.DisplayName.Get(database.UseEnglish)} ↔ {second.DisplayName.Get(database.UseEnglish)} · {level}");
                }
                if (faction.Id != state.PlayerFactionId && factionState?.Eliminated != true)
                {
                    WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(state.PlayerFactionId, faction.Id);
                    relationships.Insert(0, playerFaction.DisplayName.Get(database.UseEnglish) + "과 " + GetDiplomaticStatusDisplayName(relation.Status));
                }
                string relationshipText = relationships.Count == 0 ? "주요 관계 기록 없음" : string.Join("\n", relationships.Take(3));
                snapshot.Cards.Add(new WIAdministrationFactionCardSnapshot
                {
                    Title = faction.DisplayName.Get(database.UseEnglish) + (faction.PlayerFaction ? " · 플레이어" : string.Empty),
                    Description = $"영토 {castleCount}성 · 성향 {GetAIStrategyDisplayName(faction.AIStrategy)} · {status}{economy}\n{relationshipText}"
                });
            }
            return true;
        }
    }
}
