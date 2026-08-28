using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMapUGUIController : WIAdministrationUGUIPanelController
    {
        [SerializeField] private Button[] castleButtons;
        [SerializeField] private Image[] castleMarkers;
        [SerializeField] private TMP_Text[] castleLabels;
        [SerializeField] private string[] castleIds;
        [SerializeField] private WIAdministrationMapConnectionGraphic connectionGraphic;
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
            ResolveAdministrationController();

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
            ResolveAdministrationController();

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
            if (administrationController.TryGetUGUIWorldSnapshot(out WIAdministrationWorldSnapshot snapshot)
                == false)
            {
                return;
            }

            int count = Mathf.Min(castleButtons.Length, snapshot.MapNodes.Count);
            if (connectionGraphic != null)
            {
                connectionGraphic.SetConnections(snapshot.MapConnections);
            }
            for (int index = 0; index < count; index += 1)
            {
                WIAdministrationMapNodeSnapshot node = snapshot.MapNodes[index];
                castleButtons[index].image.rectTransform.anchorMin = node.Position;
                castleButtons[index].image.rectTransform.anchorMax = node.Position;
                castleLabels[index].text = node.DisplayName;
                castleMarkers[index].sprite = node.CastleImage != null
                    ? node.CastleImage
                    : ResolveMarker(node.FactionId);
                castleMarkers[index].color = node.Selected ? selectedColor : normalColor;
                castleMarkers[index].rectTransform.localScale = node.Selected
                    ? new Vector3(1.65f, 1.65f, 1f)
                    : Vector3.one;
                castleLabels[index].fontSize = node.Selected ? 15f : 12f;
                castleLabels[index].color = node.Selected
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(232, 236, 238, 255);
                castleButtons[index].image.color = Color.clear;
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
