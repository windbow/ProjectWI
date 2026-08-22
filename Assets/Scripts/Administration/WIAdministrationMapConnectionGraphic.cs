using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.Administration
{
    public sealed class WIAdministrationMapConnectionGraphic : MaskableGraphic
    {
        private readonly List<WIAdministrationMapConnectionSnapshot> connections = new List<WIAdministrationMapConnectionSnapshot>();

        // 연결선 레이어가 성 버튼의 포인터 입력을 가로채지 않도록 초기화합니다.
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        // 최신 지도 연결 상태를 복사하고 하나의 UGUI 메시를 다시 생성합니다.
        public void SetConnections(IReadOnlyList<WIAdministrationMapConnectionSnapshot> values)
        {
            connections.Clear();
            if (values != null)
            {
                connections.AddRange(values);
            }
            SetVerticesDirty();
        }

        // 모든 경로를 그림자·중심선·선택 광택 순서의 단일 메시로 그립니다.
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect area = rectTransform.rect;
            foreach (WIAdministrationMapConnectionSnapshot connection in connections)
            {
                Vector2 start = NormalizedToLocal(area, connection.Start);
                Vector2 end = NormalizedToLocal(area, connection.End);
                TrimEndpoints(ref start, ref end, connection.Selected ? 23f : 18f);

                if (connection.Selected)
                {
                    AddLine(vertexHelper, start, end, 10f, new Color32(92, 189, 255, 34));
                    AddLine(vertexHelper, start, end, 6f, new Color32(166, 225, 255, 72));
                }

                AddLine(vertexHelper, start, end, connection.Frontline ? 6f : 5f,
                    connection.Selected ? new Color32(4, 8, 12, 220) : new Color32(4, 8, 12, 150));
                AddLine(vertexHelper, start, end, connection.Selected ? 3.2f : connection.Frontline ? 2.6f : 1.8f,
                    connection.Color);

                Vector2 midpoint = (start + end) * 0.5f;
                if (connection.Frontline)
                {
                    AddCross(vertexHelper, midpoint, connection.Selected ? 6f : 4.5f,
                        connection.Selected ? new Color32(238, 246, 255, 245) : new Color32(224, 72, 64, 230));
                }
                else if (connection.Selected)
                {
                    AddDiamond(vertexHelper, midpoint, 5f, new Color32(224, 244, 255, 245));
                }
            }
        }

        // 정규화된 지도 좌표를 RectTransform 로컬 좌표로 변환합니다.
        private static Vector2 NormalizedToLocal(Rect area, Vector2 normalized)
        {
            return new Vector2(area.xMin + normalized.x * area.width, area.yMin + normalized.y * area.height);
        }

        // 연결선이 성 마커 중앙을 덮지 않도록 양 끝을 일정 거리만큼 줄입니다.
        private static void TrimEndpoints(ref Vector2 start, ref Vector2 end, float padding)
        {
            Vector2 direction = end - start;
            float distance = direction.magnitude;
            if (distance <= padding * 2f)
            {
                return;
            }
            direction /= distance;
            start += direction * padding;
            end -= direction * padding;
        }

        // 두 점을 잇는 사각형을 추가해 일정 두께의 선을 만듭니다.
        private static void AddLine(VertexHelper helper, Vector2 start, Vector2 end, float width, Color32 color)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude <= 0.01f)
            {
                return;
            }
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            AddQuad(helper, start - normal, start + normal, end + normal, end - normal, color);
        }

        // 전선의 중앙에 작은 교차 표식을 추가합니다.
        private static void AddCross(VertexHelper helper, Vector2 center, float radius, Color32 color)
        {
            AddLine(helper, center + new Vector2(-radius, -radius), center + new Vector2(radius, radius), 2f, color);
            AddLine(helper, center + new Vector2(-radius, radius), center + new Vector2(radius, -radius), 2f, color);
        }

        // 선택 경로 중앙에 작은 마름모 표식을 추가합니다.
        private static void AddDiamond(VertexHelper helper, Vector2 center, float radius, Color32 color)
        {
            AddQuad(helper, center + Vector2.left * radius, center + Vector2.up * radius,
                center + Vector2.right * radius, center + Vector2.down * radius, color);
        }

        // 지정한 네 꼭짓점으로 UGUI 메시 사각형을 추가합니다.
        private static void AddQuad(VertexHelper helper, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            int startIndex = helper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = a;
            helper.AddVert(vertex);
            vertex.position = b;
            helper.AddVert(vertex);
            vertex.position = c;
            helper.AddVert(vertex);
            vertex.position = d;
            helper.AddVert(vertex);
            helper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            helper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }
    }
}
