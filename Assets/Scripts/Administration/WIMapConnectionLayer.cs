using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWI.Administration
{
    public sealed class WIMapConnectionLayer : VisualElement
    {
        public readonly struct Connection
        {
            // 지도 연결선의 양 끝점과 표시 스타일을 저장합니다.
            public Connection(Vector2 start, Vector2 end, Color color, float width, bool frontline)
            {
                Start = start;
                End = end;
                Color = color;
                Width = width;
                Frontline = frontline;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public Color Color { get; }
            public float Width { get; }
            public bool Frontline { get; }
        }

        private readonly List<Connection> connections = new List<Connection>();

        // 연결선 레이어가 지도 입력을 가로채지 않도록 초기화합니다.
        public WIMapConnectionLayer()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.top = 0;
            style.bottom = 0;
            generateVisualContent += DrawConnections;
        }

        // 최신 성 소유권과 선택 상태로 연결선 목록을 교체합니다.
        public void SetConnections(IEnumerable<Connection> values)
        {
            connections.Clear();
            connections.AddRange(values);
            MarkDirtyRepaint();
        }

        // 정규화된 지도 좌표를 실제 픽셀 좌표로 바꿔 경로와 전선을 그립니다.
        private void DrawConnections(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            float width = contentRect.width;
            float height = contentRect.height;
            foreach (Connection connection in connections)
            {
                Vector2 start = new Vector2(connection.Start.x * width, connection.Start.y * height);
                Vector2 end = new Vector2(connection.End.x * width, connection.End.y * height);

                painter.BeginPath();
                painter.strokeColor = new Color(0.06f, 0.05f, 0.04f, 0.8f);
                painter.lineWidth = connection.Width + 2f;
                painter.MoveTo(start);
                painter.LineTo(end);
                painter.Stroke();

                painter.BeginPath();
                painter.strokeColor = connection.Color;
                painter.lineWidth = connection.Width;
                painter.MoveTo(start);
                painter.LineTo(end);
                painter.Stroke();

                if (connection.Frontline)
                {
                    Vector2 midpoint = (start + end) * 0.5f;
                    painter.BeginPath();
                    painter.fillColor = connection.Color;
                    painter.Arc(midpoint, 4.5f, Angle.Degrees(0f), Angle.Degrees(360f));
                    painter.Fill();
                }
            }
        }
    }
}
