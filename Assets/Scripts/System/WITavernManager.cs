using UnityEngine;
using ProjectWI.Data;

namespace ProjectWI.SubSystem
{
    /// <summary>
    /// 선술집에서의 캐릭터 영입 로직과 UI 연동을 관리하는 클래스입니다.
    /// </summary>
    public class WITavernManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject tavernPanel;

        [Header("Job Data Assets")]
        [SerializeField] private WIJobDataSO warriorData;
        [SerializeField] private WIJobDataSO mageData;

        /// <summary>
        /// 선술집 UI 창을 엽니다.
        /// </summary>
        public void OpenTavern()
        {
            if (tavernPanel != null)
            {
                tavernPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 선술집 UI 창을 닫습니다.
        /// </summary>
        public void CloseTavern()
        {
            if (tavernPanel != null)
            {
                tavernPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 전사 캐릭터를 영입합니다.
        /// </summary>
        public void RecruitWarrior()
        {
            if (WIPlayerData.Instance != null && warriorData != null)
            {
                WIPlayerData.Instance.RecruitAdventurer(warriorData);
            }
            else
            {
                Debug.LogWarning("[선술집] 데이터 또는 플레이어 데이터 인스턴스가 없습니다.");
            }
        }

        /// <summary>
        /// 마법사 캐릭터를 영입합니다.
        /// </summary>
        public void RecruitMage()
        {
            if (WIPlayerData.Instance != null && mageData != null)
            {
                WIPlayerData.Instance.RecruitAdventurer(mageData);
            }
            else
            {
                Debug.LogWarning("[선술집] 데이터 또는 플레이어 데이터 인스턴스가 없습니다.");
            }
        }
    }
}
