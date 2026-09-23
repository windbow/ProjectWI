namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 모든 진영의 월간 처리 순서를 조율하고 한 턴의 결과를 반환합니다.
        public static WITurnSummary ExecuteTurn(WIAdministrationDatabaseSO database, WIAdministrationState state)
        {
            WITurnSummary summary = new WITurnSummary();
            state.NormalizeCharacterDuties();
            PlanMusterPolicies(database, state);
            PlanAIMuster(database, state);
            summary.GoldSpent = state.PendingPlayerGoldSpent;
            state.PendingPlayerGoldSpent = 0;
            PrepareStandingOrders(database, state, summary);
            ResolveAutomaticCharacterTraining(database, state, summary);
            PrepareTurn(database, state, summary);
            ApplyCastleTurns(database, state, summary, out int appliedPlayerGold,
                out int appliedPlayerMana, out int appliedPlayerInfluence);
            ResolveMonthlySystems(database, state, summary);
            CompleteTurn(database, state, summary, appliedPlayerGold, appliedPlayerMana, appliedPlayerInfluence);

            return summary;
        }

        // 성별 월간 처리 전에 진영 제거, AI 연구와 AI 영입 계획을 준비합니다.
        private static void PrepareTurn(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            ResolveFactionEliminations(database, state, summary);
            ResolveAISpecialFacilityChoices(database, state);
            PlanAIResearch(database, state, summary);
            PlanFreeScenarioAIRecruitment(database, state, summary);
        }

        // 모든 성의 사업 배정과 수입, 사업 결과를 순서대로 적용합니다.
        private static void ApplyCastleTurns(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary,
            out int appliedPlayerGold,
            out int appliedPlayerMana,
            out int appliedPlayerInfluence)
        {
            appliedPlayerGold = 0;
            appliedPlayerMana = 0;
            appliedPlayerInfluence = 0;
            foreach (WICastleRuntimeState castleState in state.Castles)
            {
                WICastleDefinition castle = database.GetCastle(castleState.CastleId);
                WIFactionDefinition faction = database.GetFaction(castleState.FactionId);
                WIFactionRuntimeState factionState = state.GetFactionState(castleState.FactionId);
                if (castle == null || faction == null || factionState == null)
                {
                    continue;
                }

                if (faction.PlayerFaction)
                {
                    AssignDelegatedProject(database, state, castleState, summary);
                }
                else
                {
                    AssignAIProject(database, state, castleState, faction, factionState, summary);
                }

                WITurnSummary income = new WITurnSummary();
                AddCastleIncome(castle, castleState, income);
                ApplyResearchIncomeBonus(database, factionState, income);
                factionState.Gold += income.GoldGained;
                factionState.ManaCrystal += income.ManaGained;
                factionState.Influence += income.InfluenceGained;
                if (faction.PlayerFaction)
                {
                    summary.GoldGained += income.GoldGained;
                    summary.ManaGained += income.ManaGained;
                    summary.InfluenceGained += income.InfluenceGained;
                    appliedPlayerGold += income.GoldGained;
                    appliedPlayerMana += income.ManaGained;
                    appliedPlayerInfluence += income.InfluenceGained;
                }

                ResolveCastleProject(database, state, castleState, summary);
            }
        }

        // 성 처리 이후의 인물, 군사, AI, 외교, 사건 시스템을 정해진 순서로 실행합니다.
        private static void ResolveMonthlySystems(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary)
        {
            ResolveFactionResearch(database, state, summary);
            ResolveFacilityPassives(database, state, summary);
            ResolveCharacterActivities(database, state, summary);
            CreateRelationshipEventCandidates(database, state, summary);
            ResolveTavernQuests(database, state, summary);
            ResolveCharacterTransfers(database, state, summary);
            ResolveMusterOrders(database, state, summary);
            ResolveArmyReorganization(database, state, summary);
            ResolveCapturedCharacters(database, state, summary);
            ResolveCommonCharacterReturns(database, state, summary);
            ResolveArmyMovement(database, state, summary);
            ResolveStrategicBattles(database, state, summary);
            ResolveFactionEliminations(database, state, summary);
            ResolveArmyTraining(database, state, summary);
            ResolveOccupationStability(database, state, summary);
            RefreshInvasionWarnings(database, state);
            EnsureAIWarPressure(database, state, summary);
            PlanAIActions(database, state, summary);
            ResolvePromotionCandidates(database, state, summary);
            ResolveDiplomaticTurn(database, state, summary);
            WISchemeSystem.AdvanceMonth(state);
            WISchemeSystem.ResolveScheduledMissions(database, state, summary);
            WISchemeSystem.ScheduleAISchemes(database, state, summary);
            WICampaignObjectiveSystem.Evaluate(database, state, summary);
            WIRegionalEventSystem.CreateCandidate(database, state, summary);
        }

        // 월간 결과를 플레이어 상태와 달력에 반영하고 캠페인 종료 조건을 평가합니다.
        private static void CompleteTurn(
            WIAdministrationDatabaseSO database,
            WIAdministrationState state,
            WITurnSummary summary,
            int appliedPlayerGold,
            int appliedPlayerMana,
            int appliedPlayerInfluence)
        {
            state.Gold += summary.GoldGained - appliedPlayerGold;
            state.ManaCrystal += summary.ManaGained - appliedPlayerMana;
            state.Influence += summary.InfluenceGained - appliedPlayerInfluence;
            state.LastMonthlyReport = summary;
            state.Turn += 1;
            state.Month += 1;
            if (state.Month > 12)
            {
                state.Month = 1;
                state.Year += 1;
            }

            WICampaignResultSystem.Evaluate(database, state);
        }
    }
}
