using UnityEngine;

namespace ProjectWI.Data
{
    [CreateAssetMenu(fileName = "NewGameSettingData", menuName = "WIData", order = 0)]
    public class WIGameSettingDataSO : ScriptableObject
    {
        [Header("Base Stats")]
        // 전투 타이머
        public float combatInterval;
        // 탐험 타이머
        public float exploreInterval;
    }
}