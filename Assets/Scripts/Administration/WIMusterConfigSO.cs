using UnityEngine;

namespace ProjectWI.Administration
{
    [CreateAssetMenu(menuName = "ProjectWI/Muster Config")]
    public sealed class WIMusterConfigSO : ScriptableObject
    {
        // 일반 인물 한 명의 모집 비용과 성 규모별 월 모집 한도입니다.
        [SerializeField] private int goldPerCharacter = 60;
        [SerializeField] private int smallCastleMonthlyLimit = 1;
        [SerializeField] private int developedCastleMonthlyLimit = 2;
        // 이동 중 인원과 모집 예약까지 포함하는 세력 전체 인원 상한입니다.
        [SerializeField] private int factionBaseCapacity = 8;
        [SerializeField] private int capacityPerCastle = 4;
        [SerializeField] private int factionMaximumCapacity = 80;
        // AI의 월간 모집 성 수와 성별 방어 인원 목표입니다.
        [SerializeField] private int aiOrdersPerMonth = 2;
        [SerializeField] private int aiRearTarget = 3;
        [SerializeField] private int aiFrontTarget = 6;

        public int GoldPerCharacter => Mathf.Max(1, goldPerCharacter);
        public int SmallCastleMonthlyLimit => Mathf.Max(1, smallCastleMonthlyLimit);
        public int DevelopedCastleMonthlyLimit => Mathf.Max(1, developedCastleMonthlyLimit);
        public int FactionBaseCapacity => Mathf.Max(0, factionBaseCapacity);
        public int CapacityPerCastle => Mathf.Max(1, capacityPerCastle);
        public int FactionMaximumCapacity => Mathf.Max(1, factionMaximumCapacity);
        public int AIOrdersPerMonth => Mathf.Max(0, aiOrdersPerMonth);
        public int AIRearTarget => Mathf.Max(0, aiRearTarget);
        public int AIFrontTarget => Mathf.Max(0, aiFrontTarget);
    }
}
