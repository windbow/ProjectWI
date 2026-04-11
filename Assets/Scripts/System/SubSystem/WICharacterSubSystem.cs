using UnityEngine;

// 캐릭터 스테이터스 등을 담당
namespace ProjectWI.SubSystem
{
    public class WICharacterSubSystem : MonoBehaviour
    {
        public static WICharacterSubSystem Instance { get; private set; }

        [Header("Game State")]
        // 체력
        public int HP = 1000;

        private void Awake()
        {
            
        }

        private void Start()
        {
        }

        private void Update()
        {
            
        }
    }
}
