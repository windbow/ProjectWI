using UnityEngine;
using System.Collections.Generic;

namespace ProjectWI.Data
{
    [CreateAssetMenu(fileName = "NewDungeonData", menuName = "WIData/DungeonData", order = 1)]
    public class WIDungeonDataSO : ScriptableObject
    {
        public int dungeonId;
        public string dungeonName;
        
        [Header("Event Pool")]
        [Tooltip("던전 탐험 시 등장할 이벤트들의 모음")]
        public List<WIDungeonEventSO> eventPool;
        
        public WIDungeonEventSO RollRandomEvent()
        {
            if (eventPool == null || eventPool.Count == 0) return null;
            
            int totalWeight = 0;
            foreach (var evt in eventPool)
            {
                if (evt != null) totalWeight += evt.weight;
            }
            
            if (totalWeight <= 0) return eventPool[0];
            
            // Random value between 0 and totalWeight - 1
            int randomRoll = UnityEngine.Random.Range(0, totalWeight);
            
            int currentWeight = 0;
            foreach (var evt in eventPool)
            {
                if (evt == null) continue;
                
                currentWeight += evt.weight;
                if (randomRoll < currentWeight)
                {
                    return evt;
                }
            }
            
            return eventPool[0]; // Fallback
        }
    }
}