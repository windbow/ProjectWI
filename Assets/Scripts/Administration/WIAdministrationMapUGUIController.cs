using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMapUGUIController : MonoBehaviour
    {
        [SerializeField] private WIAdministrationUIController administrationController;
        [SerializeField] private Button[] castleButtons;
        [SerializeField] private Image[] castleMarkers;
        [SerializeField] private TMP_Text[] castleLabels;
        [SerializeField] private string[] castleIds;
        [SerializeField] private Sprite avalonMarker;
        [SerializeField] private Sprite valdorMarker;
        [SerializeField] private Sprite ironheartMarker;
        [SerializeField] private Sprite sylvanroadMarker;
        [SerializeField] private Sprite necropolisMarker;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color32(255, 214, 84, 255);

        // 고정 배치된 60개 성 노드에 기존 성 선택 기능을 연결합니다.
        private void Awake()
        {
            if (administrationController == null)
            {
                administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            }

            int count = Mathf.Min(castleButtons.Length, castleIds.Length);
            for (int index = 0; index < count; index += 1)
            {
                string castleId = castleIds[index];
                castleButtons[index].onClick.AddListener(() => administrationController.OpenCastleFromUGUI(castleId));
            }
        }

        // 월드 상태 변경을 구독하고 지도 노드를 갱신합니다.
        private void OnEnable()
        {
            if (administrationController == null)
            {
                administrationController = FindFirstObjectByType<WIAdministrationUIController>();
            }

            if (administrationController != null)
            {
                administrationController.UGUIWorldChanged += Refresh;
                Refresh();
            }
        }

        // 월드 상태 변경 구독을 해제합니다.
        private void OnDisable()
        {
            if (administrationController != null)
            {
                administrationController.UGUIWorldChanged -= Refresh;
            }
        }

        // 캠페인 스냅샷의 성 이름·진영·선택 상태를 각 지도 노드에 반영합니다.
        private void Refresh()
        {
            if (administrationController.TryGetUGUIWorldSnapshot(out WIAdministrationWorldSnapshot snapshot) == false)
            {
                return;
            }

            int count = Mathf.Min(castleButtons.Length, snapshot.MapNodes.Count);
            for (int index = 0; index < count; index += 1)
            {
                WIAdministrationMapNodeSnapshot node = snapshot.MapNodes[index];
                castleLabels[index].text = node.DisplayName;
                castleMarkers[index].sprite = ResolveMarker(node.FactionId);
                castleMarkers[index].color = node.Selected ? selectedColor : normalColor;
                castleButtons[index].image.color = node.Selected
                    ? new Color32(255, 214, 84, 42)
                    : Color.clear;
            }
        }

        // 진영 ID에 맞는 성채 마커 스프라이트를 반환합니다.
        private Sprite ResolveMarker(string factionId)
        {
            switch (factionId)
            {
                case "avalon": return avalonMarker;
                case "valdor": return valdorMarker;
                case "sylvanroad": return sylvanroadMarker;
                case "necropolis": return necropolisMarker;
                default: return ironheartMarker;
            }
        }
    }
}
