using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIAdministrationModalVisualUtility
    {
        private static readonly string[] AdditionalPrefabPaths =
        {
            "Assets/Prefabs/Administration/WIAdministrationMarchUGUI.prefab",
            "Assets/Prefabs/Administration/WIAdministrationCastleRecordUGUI.prefab"
        };
        private const string NormalButtonPath = "Assets/Resources/UI/Generated/button_flat_normal.png";
        private const string PrimaryButtonPath = "Assets/Resources/UI/Generated/button_flat_primary.png";
        private const string CloseButtonPath = "Assets/Resources/UI/Generated/turn_followup_close_button_v1.png";
        private const string ShellPath = "Assets/Resources/UI/Generated/administration_modal_shell_v1.png";

        // 성 내정 기능 모달에 공통 냉색 금속 아트와 버튼 상태를 적용합니다.
        public static void Apply(GameObject root)
        {
            ConfigureShellImporter();
            ConfigureSpriteBorder(NormalButtonPath, new Vector4(8f, 8f, 8f, 8f));
            ConfigureSpriteBorder(PrimaryButtonPath, new Vector4(8f, 8f, 8f, 8f));

            Transform panel = root.transform.Find("ModalRoot/ModalPanel");
            if (panel == null)
            {
                return;
            }

            ConfigurePanel(panel);
            ConfigureHeader(panel.Find("Header"));
            ConfigureButtons(root);
        }

        // 현재 프리팹의 본문 배치를 유지하면서 누락된 두 화면의 외곽만 공용 UI로 교체합니다.
        [MenuItem("WI/UI/Apply Common Shell to March and Castle Record")]
        public static void ApplyToAdditionalPrefabs()
        {
            foreach (string prefabPath in AdditionalPrefabPaths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("진격/출정과 성 상세 프리팹의 본문 배치를 유지하고 공용 모달 셸을 적용했습니다.");
        }

        // 모달 전체에 공용 외곽 프레임과 빈 본문 셸을 적용합니다.
        private static void ConfigurePanel(Transform panel)
        {
            Image image = panel.GetComponent<Image>();
            if (image == null)
            {
                return;
            }
            image.sprite = LoadSprite(ShellPath);
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }

        // 공용 셸에 포함된 헤더 위에 제목과 전용 닫기 아이콘만 배치합니다.
        private static void ConfigureHeader(Transform header)
        {
            if (header == null)
            {
                return;
            }
            Image headerImage = header.GetComponent<Image>();
            headerImage.enabled = false;

            TMP_Text title = header.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.color = new Color32(235, 239, 242, 255);
                title.fontSize = 27f;
                RectTransform titleRect = title.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.08f, 0.12f);
                titleRect.anchorMax = new Vector2(0.78f, 0.88f);
                titleRect.offsetMin = Vector2.zero;
                titleRect.offsetMax = Vector2.zero;
            }

            Button close = header.Find("CloseButton")?.GetComponent<Button>();
            if (close == null)
            {
                return;
            }
            close.image.sprite = LoadSprite(CloseButtonPath);
            close.image.type = Image.Type.Simple;
            close.image.color = Color.white;
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.91f, 0.18f);
            closeRect.anchorMax = new Vector2(0.97f, 0.82f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            TMP_Text closeLabel = close.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null)
            {
                closeLabel.text = string.Empty;
                closeLabel.gameObject.SetActive(false);
            }
        }

        // 공용 셸을 UI용 무압축 단일 Sprite로 설정합니다.
        private static void ConfigureShellImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(ShellPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }
            bool requiresImport = importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (requiresImport == false)
            {
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // 구형 흰색 버튼을 일반·주요 역할에 맞는 평면 금속 버튼으로 교체합니다.
        private static void ConfigureButtons(GameObject root)
        {
            Sprite normal = LoadSprite(NormalButtonPath);
            Sprite primary = LoadSprite(PrimaryButtonPath);
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.transform.parent != null && button.transform.parent.name == "Header")
                {
                    continue;
                }
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage == null)
                {
                    continue;
                }
                string spritePath = buttonImage.sprite == null ? string.Empty : AssetDatabase.GetAssetPath(buttonImage.sprite);
                string spriteName = buttonImage.sprite == null ? string.Empty : buttonImage.sprite.name;
                bool isKnownNormalButton = button.name == "CreateArmyButton" ||
                    button.name == "PreviousButton" || button.name == "NextButton";
                if (spritePath.EndsWith("button_normal.png") || spriteName.StartsWith("button_normal") ||
                    isKnownNormalButton)
                {
                    buttonImage.sprite = normal;
                    buttonImage.type = Image.Type.Sliced;
                }
                else if (spritePath.EndsWith("button_primary.png") || spriteName.StartsWith("button_primary"))
                {
                    buttonImage.sprite = primary;
                    buttonImage.type = Image.Type.Sliced;
                }
                else
                {
                    continue;
                }
                buttonImage.color = Color.white;
                button.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color32(220, 232, 244, 255);
                colors.pressedColor = new Color32(160, 181, 205, 255);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color32(95, 102, 112, 150);
                button.colors = colors;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.color = new Color32(235, 239, 242, 255);
                }
            }
        }

        // 공용 버튼 Sprite의 9-Slice Border를 빌더 재실행 시에도 유지합니다.
        private static void ConfigureSpriteBorder(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteBorder == border)
            {
                return;
            }
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        // 메인 또는 서브 에셋 여부와 관계없이 경로의 Sprite를 불러옵니다.
        private static Sprite LoadSprite(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
            return null;
        }
    }
}
