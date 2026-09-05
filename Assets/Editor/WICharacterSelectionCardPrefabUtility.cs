using System;
using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WICharacterSelectionCardPrefabUtility
    {
        private const string CardSpritePath =
            "Assets/Resources/UI/Generated/CharacterActivity/character_activity_hero_card_v2.png";
        private const string DividerSpritePath =
            "Assets/Resources/UI/Generated/CharacterActivity/character_activity_divider_v1.png";
        private const string PreviousButtonSpritePath =
            "Assets/Resources/UI/Generated/CharacterActivity/character_activity_previous_button_v1.png";
        private const string DividerObjectName = "CharacterInfoDivider";

        // 캐릭터 전용 버튼 배열을 가진 UGUI 프리팹과 직렬화 필드를 정의합니다.
        private static readonly (string Path, Type ControllerType, string ButtonField)[] Targets =
        {
            ("Assets/Prefabs/Administration/WIAdministrationHeroAssignmentUGUI.prefab",
                typeof(WIAdministrationHeroAssignmentUGUIController), "candidateButtons"),
            ("Assets/Prefabs/Administration/WIAdministrationFocusProjectUGUI.prefab",
                typeof(WIAdministrationFocusProjectUGUIController), "managerButtons"),
            ("Assets/Prefabs/Administration/WIAdministrationDelegationUGUI.prefab",
                typeof(WIAdministrationDelegationUGUIController), "governorButtons"),
            ("Assets/Prefabs/Administration/WIAdministrationCharacterActivityUGUI.prefab",
                typeof(WIAdministrationCharacterActivityUGUIController), "cardButtons"),
            ("Assets/Prefabs/Administration/WIAdministrationHeroesUGUI.prefab",
                typeof(WIAdministrationHeroesUGUIController), "cardButtons"),
            ("Assets/Prefabs/Administration/WIAdministrationResearchUGUI.prefab",
                typeof(WIAdministrationResearchUGUIController), "cardButtons"),
            ("Assets/Prefabs/Administration/WIAdministrationSchemeUGUI.prefab",
                typeof(WIAdministrationSchemeUGUIController), "cardButtons")
        };

        // 캐릭터를 카드로 선택하는 모든 고정 UGUI 프리팹에 공용 프레임과 구분선을 적용합니다.
        [MenuItem("WI/UI/Apply Character Selection Card Visuals")]
        public static void ApplyCharacterSelectionCardVisuals()
        {
            ConfigureSharedSpriteImports();
            int changedCardCount = 0;
            foreach ((string path, Type controllerType, string buttonField) in Targets)
            {
                changedCardCount += ApplyToPrefab(path, controllerType, buttonField);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"캐릭터 선택 UGUI 7종의 카드 {changedCardCount}개에 공용 프레임과 구분선을 적용했습니다.");
        }

        // 두 공용 이미지가 Sprite/Single과 카드 9-Slice 규칙으로 임포트되도록 설정합니다.
        public static void ConfigureSharedSpriteImports()
        {
            ConfigureSpriteImport(CardSpritePath, new Vector4(12f, 12f, 12f, 12f));
            ConfigureSpriteImport(DividerSpritePath, Vector4.zero);
            ConfigureSpriteImport(PreviousButtonSpritePath, new Vector4(20f, 20f, 20f, 20f));
        }

        // 버튼 하나의 배경과 우측 정보 구분선을 공용 카드 규격으로 맞춥니다.
        public static void ApplyCardVisual(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image background = button.targetGraphic as Image;
            if (background == null)
            {
                background = button.GetComponent<Image>();
            }

            if (background != null)
            {
                background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }

            Transform existing = button.transform.Find(DividerObjectName);
            GameObject dividerObject = existing == null
                ? new GameObject(DividerObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                : existing.gameObject;
            dividerObject.layer = button.gameObject.layer;
            dividerObject.transform.SetParent(button.transform, false);
            dividerObject.transform.SetSiblingIndex(0);

            RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0.56f, 0.43f);
            dividerRect.anchorMax = new Vector2(0.96f, 0.43f);
            dividerRect.pivot = new Vector2(0.5f, 0.5f);
            dividerRect.anchoredPosition = Vector2.zero;
            dividerRect.sizeDelta = new Vector2(0f, 10f);
            dividerRect.localScale = Vector3.one;

            Image dividerImage = dividerObject.GetComponent<Image>();
            dividerImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DividerSpritePath);
            dividerImage.type = Image.Type.Simple;
            dividerImage.preserveAspect = false;
            dividerImage.raycastTarget = false;
            dividerImage.color = Color.white;
        }

        // 지정 프리팹의 직렬화된 캐릭터 버튼 배열에 공용 카드 비주얼을 저장합니다.
        private static int ApplyToPrefab(string path, Type controllerType, string buttonField)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Component controller = root.GetComponent(controllerType);
                if (controller == null)
                {
                    Debug.LogError($"캐릭터 카드 적용 실패 · 컨트롤러 없음 · {path}");
                    return 0;
                }

                SerializedObject serialized = new SerializedObject(controller);
                SerializedProperty buttons = serialized.FindProperty(buttonField);
                if (buttons == null || buttons.isArray == false)
                {
                    Debug.LogError($"캐릭터 카드 적용 실패 · 버튼 배열 없음 · {path} · {buttonField}");
                    return 0;
                }

                int changedCount = 0;
                for (int index = 0; index < buttons.arraySize; index += 1)
                {
                    Button button = buttons.GetArrayElementAtIndex(index).objectReferenceValue as Button;
                    if (button == null)
                    {
                        continue;
                    }

                    ApplyCardVisual(button);
                    changedCount += 1;
                }

                ApplyPreviousButtonVisual(serialized.FindProperty("previousButton"));
                ApplyPreviousButtonVisual(serialized.FindProperty("backButton"));
                ApplyPreviousButtonVisual(serialized.FindProperty("managerBackButton"));

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return changedCount;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 이전 단계로 돌아가는 기존 버튼에 무문자 9-Slice 배경만 적용합니다.
        private static void ApplyPreviousButtonVisual(SerializedProperty buttonProperty)
        {
            if (buttonProperty == null)
            {
                return;
            }

            Button button = buttonProperty.objectReferenceValue as Button;
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image == null)
            {
                return;
            }

            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PreviousButtonSpritePath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        // PNG를 투명 UI Sprite로 임포트하고 선택적인 9-Slice 테두리를 저장합니다.
        private static void ConfigureSpriteImport(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"캐릭터 카드 Sprite를 찾을 수 없습니다. · {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
