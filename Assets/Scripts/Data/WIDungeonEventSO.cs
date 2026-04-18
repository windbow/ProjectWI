using UnityEngine;

namespace ProjectWI.Data
{
    public enum WIDungeonEventType
    {
        None,
        Treasure,
        Story,
        MonsterEncounter
    }

    [CreateAssetMenu(fileName = "NewDungeonEvent", menuName = "WIData/DungeonEventData", order = 1)]
    public class WIDungeonEventSO : ScriptableObject
    {
        [Header("Event Settings")]
        public string id;
        public WIDungeonEventType eventType = WIDungeonEventType.None;
        
        [TextArea(2, 4)]
        public string logMessage = "아무일도 일어나지 않았습니다.";
        
        [Tooltip("이 이벤트가 등장할 가중치 (높을 수록 자주 호출됨)")]
        [Range(1, 100)]
        public int weight = 10;

        [Header("Rewards (If Applicable)")]
        public int rewardGold = 0;
        public int rewardExp = 0;
    }
}