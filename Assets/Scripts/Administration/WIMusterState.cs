using System;
using System.Collections.Generic;

namespace ProjectWI.Administration
{
    [Serializable]
    public sealed class WIMusterOrder
    {
        // 비용을 낸 소유 세력과 합류할 전투단, 예약한 기존 인물 식별자입니다.
        public string FactionId;
        public string ArmyId;
        public List<string> HeroIds = new List<string>();
        // 취소 시 실제 지불액만 반환하며 완료는 다음 월 처리에서 한 번 실행합니다.
        public int GoldPaid;
        public int OrderedTurn;
    }

    [Serializable]
    public sealed class WIMusterPolicy
    {
        // 성 소유권이 바뀌면 이전 세력의 자동 충원 지시를 해제합니다.
        public string FactionId;
        public string ArmyId;
        public WIUnitRole Role = WIUnitRole.Melee;
        public bool Enabled;
        // 전투단 전체 목표 인원과 한 달에 쓸 수 있는 모병 예산입니다.
        public int TargetSize;
        public int MonthlyBudget;
    }
}
