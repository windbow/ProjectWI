using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ProjectWI.SubSystem
{
    public class WIPartySelectionManager : MonoBehaviour
    {
        public static WIPartySelectionManager Instance { get; private set; }

        [SerializeField] private GameObject selectionPanel;
        private List<WIAdventurerInstance> selectedMembers = new List<WIAdventurerInstance>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void OpenPartyPanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }
            selectedMembers.Clear();
        }

        public void ClosePartyPanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
        }

        public void ConfirmAndStartBattle()
        {
            var playerRecruited = WIPlayerData.Instance.RecruitedAdventurers;
            
            if (playerRecruited == null || playerRecruited.Count == 0)
            {
                Debug.LogWarning("영입된 모험가가 없습니다!");
                return;
            }

            selectedMembers.Clear();
            for (int i = 0; i < playerRecruited.Count && i < 2; i++)
            {
                selectedMembers.Add(playerRecruited[i]);
            }

            WIPlayerData.Instance.SetParty(selectedMembers);
            
            if (WIBattleManager.Instance != null)
            {
                WIBattleManager.Instance.StartBattle(1, selectedMembers);
            }

            ClosePartyPanel();
            
            if (WIUIManager.Instance != null)
            {
                WIUIManager.Instance.ShowCombatPage();
            }
        }
    }
}
