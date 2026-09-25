using System.Linq;

namespace ProjectWI.Administration
{
    public partial class WIAdministrationUIController
    {
        // QA에서 전역 영웅 UGUI를 바로 표시합니다.
        public void OpenHeroesUGUIForQA()
        {
            OpenCastlePreviewForQA();
            RaiseUGUIScreenRequest<WIAdministrationHeroesUGUIController>(() => UGUIHeroesRequested);
        }

        // 영웅 목록 또는 선택 영웅의 승격·작위 카드를 구성합니다.
        public bool TryGetUGUIHeroesPanel(string mode, string heroId,
            out WIAdministrationHeroesSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = string.Empty;
            if (state == null || database == null)
            {
                error = "영웅 정보를 불러올 수 없습니다.";
                return false;
            }
            snapshot = new WIAdministrationHeroesSnapshot();
            if (mode == "titles") BuildHeroTitleCards(snapshot, heroId);
            else BuildHeroListCards(snapshot);
            return true;
        }

        // 영웅 승격 또는 작위 수여를 기존 게임 상태에 적용합니다.
        public bool ExecuteUGUIHeroAction(string action, string heroId, string targetId, out string error)
        {
            error = string.Empty;
            bool succeeded;
            if (action == "promote")
            {
                succeeded = WIAdministrationTurnSystem.PromoteCommonCharacter(database, state, heroId);
                if (succeeded == false) error = "승격에 필요한 영향력이 부족합니다.";
            }
            else if (action == "title")
            {
                succeeded = WIAdministrationTurnSystem.AwardTitle(database, state, state.PlayerFactionId, heroId, targetId);
                if (succeeded == false) error = "공훈 또는 영향력이 부족하거나 이미 보유한 작위입니다.";
            }
            else
            {
                error = "지원하지 않는 영웅 명령입니다.";
                return false;
            }
            if (succeeded) RefreshAll();
            return succeeded;
        }

        // 플레이어 소속 인물만 목록 카드와 인원 수에 반영합니다.
        private void BuildHeroListCards(WIAdministrationHeroesSnapshot snapshot)
        {
            var allies = state.Characters.Where(IsPlayerPersonnel).ToList();
            snapshot.Title = database.GetText("UI_ALL_HEROES");
            snapshot.Summary = $"영입 영웅 {allies.Count}명 · 영웅을 선택하면 승격과 작위를 관리합니다.";
            foreach (WICharacterRuntimeState character in allies)
            {
                WIHeroDefinition hero = database.GetHero(character.HeroId);
                WICharacterGrade grade = character.PromotedToHero ? WICharacterGrade.Hero : character.BaseGrade;
                string activity = GetUGUIHeroActivity(character);
                WITitleDefinition title = database.GetTitle(character.TitleId);
                string titleName = title == null ? "작위 없음" : title.DisplayName.Get(database.UseEnglish);
                snapshot.Cards.Add(new WIAdministrationHeroCardSnapshot
                {
                    Action = "hero", Id = character.HeroId,
                    Title = $"[{GetGradeDisplayName(grade)}] {hero.DisplayName.Get(database.UseEnglish)}",
                    Description = $"{GetTraitDisplayText(hero)}\n{hero.HeroClass} · 공훈 {character.Merit} · 명성 {character.Reputation}\n{titleName} · {activity}",
                    Portrait = hero.Portrait
                });
            }
        }

        // 성·전투단·이동 및 포로 이전 소속으로 살아 있는 아군 인사 대상을 판정합니다.
        private bool IsPlayerPersonnel(WICharacterRuntimeState character)
        {
            if (character == null || character.Recruited == false || character.IsDead == true)
            {
                return false;
            }
            if (string.IsNullOrEmpty(character.JoinedEnemyFactionId) == false)
            {
                return character.JoinedEnemyFactionId == state.PlayerFactionId;
            }
            if (character.Captured == true)
            {
                return character.CapturedFromFactionId == state.PlayerFactionId;
            }
            var army = state.Armies.FirstOrDefault(item => item.Members.Any(member => member.HeroId == character.HeroId));
            if (army != null)
            {
                return army.FactionId == state.PlayerFactionId;
            }
            var transfer = state.CharacterTransfers.Find(item => item.HeroId == character.HeroId);
            if (transfer != null)
            {
                return state.GetCastle(transfer.OriginCastleId)?.FactionId == state.PlayerFactionId;
            }
            return state.Castles.Any(item => item.FactionId == state.PlayerFactionId && item.HeroIds.Contains(character.HeroId));
        }

        // 선택 영웅의 상태와 이용 가능한 승격·작위 카드를 구성합니다.
        private void BuildHeroTitleCards(WIAdministrationHeroesSnapshot snapshot, string heroId)
        {
            WICharacterRuntimeState character = state.GetCharacter(heroId);
            WIHeroDefinition hero = database.GetHero(heroId);
            if (character == null || hero == null) return;
            snapshot.Title = "영웅 관리 · " + hero.DisplayName.Get(database.UseEnglish);
            snapshot.Summary = $"공훈 {character.Merit} · 명성 {character.Reputation} · 충성 {character.LoyaltyState}\n" +
                $"{database.GetText("UI_CHARACTER_TRAITS")} · {GetTraitDisplayText(hero)}\n전투 특성 · {GetBattleTraitDisplayText(hero)}";
            if (state.PendingHeroPromotionIds.Contains(heroId))
            {
                snapshot.Cards.Add(new WIAdministrationHeroCardSnapshot
                {
                    Action = "promote", Id = heroId, Title = "영웅 승격",
                    Description = $"영향력 {database.PromotionInfluenceCost} · 일반 인물을 영웅으로 승격", Portrait = hero.Portrait
                });
            }
            foreach (WITitleDefinition title in database.TitleDefinitions)
            {
                bool current = character.TitleId == title.Id;
                snapshot.Cards.Add(new WIAdministrationHeroCardSnapshot
                {
                    Action = "title", Id = title.Id,
                    Title = (current ? "● " : string.Empty) + title.DisplayName.Get(database.UseEnglish),
                    Description = $"공훈 {title.RequiredMerit} · 영향력 {title.InfluenceCost}\n영지 관리 +{title.ProjectBonus} · 전투 +{title.BattlePowerBonus}",
                    Interactable = current == false
                });
            }
        }

        // 영웅의 사망·포로·이동·개인 활동 상태를 화면용 문장으로 반환합니다.
        private string GetUGUIHeroActivity(WICharacterRuntimeState character)
        {
            if (character.IsDead) return "사망";
            if (character.Captured)
            {
                return $"포로 · {database.GetFaction(character.CaptorFactionId)?.DisplayName.Get(database.UseEnglish)} · {character.CapturedMonthsRemaining}개월";
            }
            WICharacterTransferState transfer = state.CharacterTransfers.Find(item => item.HeroId == character.HeroId);
            if (transfer != null) return $"이동 중 · {database.GetCastle(transfer.TargetCastleId).DisplayName.Get(database.UseEnglish)} · {transfer.RemainingMonths}개월";
            return character.Activity == WICharacterActivityType.None ? "대기" : character.Activity.ToString();
        }
    }
}
