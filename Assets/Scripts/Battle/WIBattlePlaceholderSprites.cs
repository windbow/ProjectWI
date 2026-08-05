using UnityEngine;

namespace ProjectWI.Battle
{
    public static class WIBattlePlaceholderSprites
    {
        private static Sprite square;
        private static Sprite circle;
        private static Sprite arenaGrid;

        // 흰색 정사각형 스프라이트를 한 번 생성해 일반 인물과 UI 막대에 재사용합니다.
        public static Sprite GetSquare()
        {
            if (square != null) return square;
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64];
            for (int index = 0; index < pixels.Length; index += 1) pixels[index] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            texture.name = "BattlePlaceholderSquare";
            square = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            return square;
        }

        // 투명 배경의 흰색 원형 스프라이트를 한 번 생성해 영웅 표시에 사용합니다.
        public static Sprite GetCircle()
        {
            if (circle != null) return circle;
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y += 1)
            {
                for (int x = 0; x < size; x += 1)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= size * 0.46f ? Color.white : Color.clear);
                }
            }
            texture.Apply();
            texture.name = "BattlePlaceholderCircle";
            circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circle;
        }

        // 전장 크기에 맞춰 어두운 바탕과 밝은 격자선을 가진 임시 맵 스프라이트를 생성합니다.
        public static Sprite GetArenaGrid()
        {
            if (arenaGrid != null) return arenaGrid;
            const int width = 180;
            const int height = 100;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color background = new Color(0.1f, 0.13f, 0.12f, 1f);
            Color grid = new Color(0.31f, 0.38f, 0.31f, 1f);
            Color center = new Color(0.55f, 0.45f, 0.2f, 1f);
            for (int y = 0; y < height; y += 1)
            {
                for (int x = 0; x < width; x += 1)
                {
                    bool gridLine = x % 20 == 0 || y % 20 == 0;
                    bool centerLine = x == width / 2;
                    texture.SetPixel(x, y, centerLine ? center : gridLine ? grid : background);
                }
            }
            texture.Apply();
            texture.name = "BattlePlaceholderArenaGrid";
            arenaGrid = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 10f);
            return arenaGrid;
        }
    }
}
