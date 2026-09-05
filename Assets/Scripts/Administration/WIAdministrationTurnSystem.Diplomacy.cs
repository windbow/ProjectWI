using System.Linq;
using UnityEngine;

namespace ProjectWI.Administration
{
    public static partial class WIAdministrationTurnSystem
    {
        // 친선 비용을 지불해 전쟁을 끝내거나 중립 관계를 우호로 개선합니다.
        public static bool ImproveDiplomaticRelations(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            int influenceCost = Mathf.Max(0, ImproveRelationsInfluenceCost - GetEmbassyDiscount(state, initiatorFactionId));
            if (initiator == null || relation == null || initiator.Gold < ImproveRelationsGoldCost ||
                initiator.Influence < influenceCost ||
                relation.Status != WIDiplomaticStatus.War && relation.Status != WIDiplomaticStatus.Neutral)
            {
                return false;
            }

            initiator.Gold -= ImproveRelationsGoldCost;
            initiator.Influence -= influenceCost;
            relation.Status = relation.Status == WIDiplomaticStatus.War
                ? WIDiplomaticStatus.Neutral
                : WIDiplomaticStatus.Friendly;
            return true;
        }

        // 우호 진영과 영향력을 사용해 불가침 협정을 체결합니다.
        public static bool SignNonAggression(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            int influenceCost = Mathf.Max(0, NonAggressionInfluenceCost - GetEmbassyDiscount(state, initiatorFactionId));
            if (initiator == null || relation == null || relation.Status != WIDiplomaticStatus.Friendly ||
                initiator.Influence < influenceCost)
            {
                return false;
            }

            initiator.Influence -= influenceCost;
            relation.Status = WIDiplomaticStatus.NonAggression;
            return true;
        }

        // 불가침 진영과 영향력을 사용해 동맹을 체결합니다.
        public static bool FormAlliance(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            int influenceCost = Mathf.Max(0, AllianceInfluenceCost - GetEmbassyDiscount(state, initiatorFactionId));
            if (initiator == null || relation == null || relation.Status != WIDiplomaticStatus.NonAggression ||
                initiator.Influence < influenceCost)
            {
                return false;
            }

            initiator.Influence -= influenceCost;
            relation.Status = WIDiplomaticStatus.Alliance;
            relation.AidCooldownMonths = 0;
            return true;
        }

        // 영향력을 지불하고 협정을 파기해 대상 진영에 선전포고합니다.
        public static bool DeclareWar(WIAdministrationState state, string initiatorFactionId, string targetFactionId)
        {
            WIFactionRuntimeState initiator = state.GetFactionState(initiatorFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(initiatorFactionId, targetFactionId);
            if (initiator == null || relation == null || relation.Status == WIDiplomaticStatus.War ||
                initiator.Influence < DeclareWarInfluenceCost)
            {
                return false;
            }

            initiator.Influence -= DeclareWarInfluenceCost;
            relation.Status = WIDiplomaticStatus.War;
            relation.AidCooldownMonths = 0;
            return true;
        }

        // 동맹국에서 금화 원조를 받고 6개월 재요청 대기 시간을 설정합니다.
        public static bool RequestAllianceAid(WIAdministrationState state, string requesterFactionId, string allyFactionId)
        {
            WIFactionRuntimeState requester = state.GetFactionState(requesterFactionId);
            WIFactionRuntimeState ally = state.GetFactionState(allyFactionId);
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(requesterFactionId, allyFactionId);
            if (requester == null || ally == null || relation == null || relation.Status != WIDiplomaticStatus.Alliance ||
                relation.AidCooldownMonths > 0 || ally.Gold < AllianceAidGold)
            {
                return false;
            }

            ally.Gold -= AllianceAidGold;
            requester.Gold += AllianceAidGold;
            relation.AidCooldownMonths = 6;
            return true;
        }

        // 원소속 진영이 몸값을 지불해 상대 진영이 억류한 포로 한 명을 즉시 귀환시킵니다.
        public static bool RansomPrisoner(WIAdministrationDatabaseSO database, WIAdministrationState state,
            string requesterFactionId, string prisonerHeroId)
        {
            WICharacterRuntimeState prisoner = state.GetCharacter(prisonerHeroId);
            WIFactionRuntimeState requester = state.GetFactionState(requesterFactionId);
            WIFactionRuntimeState captor = state.GetFactionState(prisoner?.CaptorFactionId);
            WIResourceType resourceType = prisoner != null && prisoner.RansomRequested
                ? prisoner.RansomResourceType : WIResourceType.Gold;
            int amount = prisoner != null && prisoner.RansomRequested
                ? Mathf.Max(1, prisoner.RansomAmount) : database.PrisonerRansomGold;
            if (prisoner == null || requester == null || captor == null || prisoner.Captured == false ||
                prisoner.CapturedFromFactionId != requesterFactionId ||
                GetFactionResource(requester, resourceType) < amount)
            {
                return false;
            }

            AddFactionResource(requester, resourceType, -amount);
            AddFactionResource(captor, resourceType, amount);
            return ReleaseCapturedCharacter(state, prisoner);
        }

        // 대사관 보유 진영의 외교 영향력 할인량을 반환합니다.
        private static int GetEmbassyDiscount(WIAdministrationState state, string factionId)
        {
            return HasFactionFacility(state, factionId, "embassy") ? FacilityEmbassyCostDiscount : 0;
        }

        // 진영의 지정 자원 보유량을 반환합니다.
        private static int GetFactionResource(WIFactionRuntimeState faction, WIResourceType resourceType)
        {
            if (resourceType == WIResourceType.ManaCrystal)
            {
                return faction.ManaCrystal;
            }

            if (resourceType == WIResourceType.Influence)
            {
                return faction.Influence;
            }

            return faction.Gold;
        }

        // 진영의 지정 자원을 증감합니다.
        private static void AddFactionResource(WIFactionRuntimeState faction, WIResourceType resourceType, int amount)
        {
            if (resourceType == WIResourceType.ManaCrystal)
            {
                faction.ManaCrystal += amount;
                return;
            }

            if (resourceType == WIResourceType.Influence)
            {
                faction.Influence += amount;
                return;
            }

            faction.Gold += amount;
        }

        // 서로 상대 진영이 억류한 두 포로를 비용 없이 맞교환해 각 원소속 성으로 귀환시킵니다.
        public static bool ExchangePrisoners(WIAdministrationState state, string firstFactionId, string secondFactionId,
            string firstPrisonerHeroId, string secondPrisonerHeroId)
        {
            WICharacterRuntimeState first = state.GetCharacter(firstPrisonerHeroId);
            WICharacterRuntimeState second = state.GetCharacter(secondPrisonerHeroId);
            if (first == null || second == null || first.Captured == false || second.Captured == false ||
                first.CapturedFromFactionId != firstFactionId || first.CaptorFactionId != secondFactionId ||
                second.CapturedFromFactionId != secondFactionId || second.CaptorFactionId != firstFactionId)
            {
                return false;
            }

            return ReleaseCapturedCharacter(state, first) && ReleaseCapturedCharacter(state, second);
        }

        // 동맹 양측이 모두 교전 중인 제3진영의 성을 제한 기간 공동 공격 목표로 지정합니다.
        public static bool ProposeJointAttack(WIAdministrationDatabaseSO database, WIAdministrationState state,
            string proposerFactionId, string allyFactionId, string targetCastleId)
        {
            WIFactionRuntimeState proposer = state.GetFactionState(proposerFactionId);
            WIDiplomaticRelationState alliance = state.GetOrCreateDiplomaticRelation(proposerFactionId, allyFactionId);
            WICastleRuntimeState target = state.GetCastle(targetCastleId);
            if (proposer == null || alliance == null || target == null || alliance.Status != WIDiplomaticStatus.Alliance ||
                target.FactionId == proposerFactionId || target.FactionId == allyFactionId ||
                AreFactionsAtWar(state, proposerFactionId, target.FactionId) == false ||
                AreFactionsAtWar(state, allyFactionId, target.FactionId) == false ||
                proposer.Influence < database.JointAttackInfluenceCost)
            {
                return false;
            }

            proposer.Influence -= database.JointAttackInfluenceCost;
            alliance.JointAttackTargetCastleId = targetCastleId;
            alliance.JointAttackMonthsRemaining = database.JointAttackDurationMonths;
            return true;
        }

        // 두 진영이 현재 전쟁 상태인지 확인합니다.
        public static bool AreFactionsAtWar(WIAdministrationState state, string firstFactionId, string secondFactionId)
        {
            WIDiplomaticRelationState relation = state.GetOrCreateDiplomaticRelation(firstFactionId, secondFactionId);
            return relation != null && relation.Status == WIDiplomaticStatus.War;
        }

        // 외교 원조 대기 시간을 줄이고 AI 진영의 제한적인 관계 개선을 처리합니다.
        private static void ResolveDiplomaticTurn(WIAdministrationDatabaseSO database, WIAdministrationState state, WITurnSummary summary)
        {
            foreach (WIDiplomaticRelationState relation in state.DiplomaticRelations)
            {
                relation.AidCooldownMonths = Mathf.Max(0, relation.AidCooldownMonths - 1);
                relation.JointAttackMonthsRemaining = Mathf.Max(0, relation.JointAttackMonthsRemaining - 1);
                if (relation.JointAttackMonthsRemaining == 0)
                {
                    relation.JointAttackTargetCastleId = string.Empty;
                }
            }

            if (state.Turn % 4 != 0)
            {
                return;
            }

            foreach (WIFactionDefinition faction in database.Factions.Where(item => item.PlayerFaction == false))
            {
                if (faction.AIStrategy == WIAIStrategy.Aggressive)
                {
                    continue;
                }

                WIFactionRuntimeState factionState = state.GetFactionState(faction.Id);
                if (factionState == null || factionState.Eliminated)
                {
                    continue;
                }
                WIDiplomaticRelationState relation = state.DiplomaticRelations.FirstOrDefault(item =>
                    (item.FirstFactionId == faction.Id || item.SecondFactionId == faction.Id) &&
                    item.Status == WIDiplomaticStatus.Neutral);
                if (relation == null || factionState.Influence < 10)
                {
                    continue;
                }

                factionState.Influence -= 10;
                relation.Status = WIDiplomaticStatus.Friendly;
                string counterpartId = relation.FirstFactionId == faction.Id ? relation.SecondFactionId : relation.FirstFactionId;
                summary.News.Add($"대륙 정세 · {faction.DisplayName.Get(database.UseEnglish)}와 {database.GetFaction(counterpartId).DisplayName.Get(database.UseEnglish)}의 관계가 우호로 개선됨");
                AddAIReasonReport(summary, faction.Id, faction.DisplayName.Get(database.UseEnglish), "외교",
                    $"중립 관계를 개선해 고립을 완화 · 영향력 10 사용 · 대상 {database.GetFaction(counterpartId).DisplayName.Get(database.UseEnglish)}");
                break;
            }
        }
    }
}
