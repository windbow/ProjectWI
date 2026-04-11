using UnityEngine;
using System.Collections.Generic;

namespace ProjectWI.SubSystem
{
    public class WIBattleManager : MonoBehaviour
    {
        public static WIBattleManager Instance { get; private set; }
        
        private Dictionary<int, WIBattleSession> activeSessions = new Dictionary<int, WIBattleSession>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 테스트용: 1번 던전 백그라운드 할당
            CreateSession(1, "초심자의 숲 (Dungeon 1)");
        }

        public WIBattleSession CreateSession(int dungeonId, string name)
        {
            if (activeSessions.ContainsKey(dungeonId)) return activeSessions[dungeonId];
            
            WIBattleSession newSession = new WIBattleSession(name);
            activeSessions.Add(dungeonId, newSession);
            return newSession;
        }

        public WIBattleSession GetSession(int dungeonId)
        {
            if (activeSessions.ContainsKey(dungeonId))
            {
                return activeSessions[dungeonId];
            }
            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach(var kvp in activeSessions)
            {
                kvp.Value.Tick(dt);
            }
        }
    }
}
