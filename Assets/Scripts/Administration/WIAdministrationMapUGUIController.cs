using System.Collections.Generic;
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
        [SerializeField] private Sprite rimgardMarker;
        [SerializeField] private Sprite valdorMarker;
        [SerializeField] private Sprite ironheartMarker;
        [SerializeField] private Sprite sylvanroadMarker;
        [SerializeField] private Sprite necropolisMarker;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color32(255, 214, 84, 255);

        // 해상도 변경 시 기존 성 이름의 겹침을 다시 계산하기 위한 지도 크기입니다.
        private Vector2 lastMapSize;

        // 지도 표시 크기가 달라졌을 때만 기존 이름표의 배치를 다시 계산합니다.
        private void LateUpdate()
        {
            if (castleButtons.Length == 0 || castleButtons[0] == null)
            {
                return;
            }
            RectTransform map = castleButtons[0].transform.parent as RectTransform;
            if (map != null && map.rect.size != lastMapSize)
            {
                ArrangeCastleLabels();
            }
        }

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
                    ? new Vector3(1.35f, 1.35f, 1f)
                    : Vector3.one;
                castleLabels[index].fontSize = node.Selected ? 17f : 14f;
                castleLabels[index].color = node.Selected
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(232, 236, 238, 255);
                castleButtons[index].image.color = Color.clear;
            }
            ArrangeCastleLabels();
        }

        // 기존 이름표를 가까운 빈 위치에 배치하여 다른 이름이나 성채 그림과 겹치지 않게 합니다.
        private void ArrangeCastleLabels()
        {
            if (castleButtons.Length == 0)
            {
                return;
            }
            RectTransform map = castleButtons[0].transform.parent as RectTransform;
            if (map == null || map.rect.width <= 0f || map.rect.height <= 0f)
            {
                return;
            }
            lastMapSize = map.rect.size;
            List<Rect> occupied = new List<Rect>();
            foreach (Image marker in castleMarkers)
            {
                Vector2 center = map.InverseTransformPoint(marker.rectTransform.position);
                Vector2 size = Vector2.Scale(marker.rectTransform.rect.size, marker.rectTransform.localScale);
                occupied.Add(new Rect(center - size * .5f, size));
            }
            for (int index = 0; index < castleLabels.Length; index += 1)
            {
                TMP_Text label = castleLabels[index];
                RectTransform rect = label.rectTransform;
                Vector2 origin = map.InverseTransformPoint(castleButtons[index].transform.position);
                Vector2 size = new Vector2(Mathf.Clamp(label.GetPreferredValues(label.text).x + 8f, 48f, 140f), 26f);
                Vector2 best = origin + new Vector2(0f, -28f);
                float bestScore = float.MaxValue;
                for (int candidate = 0; candidate < 65; candidate += 1)
                {
                    float angle = candidate == 0 ? -Mathf.PI * .5f : (candidate - 1) % 8 * Mathf.PI * .25f;
                    float radius = candidate == 0 ? 28f : 42f + (candidate - 1) / 8 * 12f;
                    Vector2 center = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    center.x = Mathf.Clamp(center.x, map.rect.xMin + size.x * .5f, map.rect.xMax - size.x * .5f);
                    center.y = Mathf.Clamp(center.y, map.rect.yMin + size.y * .5f, map.rect.yMax - size.y * .5f);
                    Rect bounds = new Rect(center - size * .5f, size);
                    float score = Vector2.Distance(origin, center);
                    foreach (Rect obstacle in occupied)
                    {
                        if (bounds.Overlaps(obstacle) == true)
                        {
                            score += 10000f;
                        }
                    }
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = center;
                    }
                }
                occupied.Add(new Rect(best - size * .5f, size));
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = size;
                rect.position = map.TransformPoint(best);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        // 진영 ID에 맞는 성채 마커 스프라이트를 반환합니다.
        private Sprite ResolveMarker(string factionId)
        {
            switch (factionId)
            {
                case "rimgard": return rimgardMarker;
                case "valdor": return valdorMarker;
                case "sylvanroad": return sylvanroadMarker;
                case "necropolis": return necropolisMarker;
                default: return ironheartMarker;
            }
        }
    }
}
