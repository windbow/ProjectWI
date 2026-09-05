using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectWI.Administration;

namespace ProjectWI.EditorTools
{
    public static class WIFacilityPrefabUtility
    {
        private const string BasicFacilityPrefabPath =
            "Assets/Prefabs/Administration/WIAdministrationBasicFacilityUGUI.prefab";
        private const string TerritoryPrefabPath =
            "Assets/Prefabs/Administration/WIAdministrationTerritoryUGUI.prefab";

        // 기본 시설 안내 목록을 실제 시설 기능으로 진입하는 고정 버튼 네 개로 구성합니다.
        [MenuItem("ProjectWI/UI/Rebuild Basic Facility Actions")]
        public static void RebuildBasicFacilityActions()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BasicFacilityPrefabPath);
            try
            {
                WIAdministrationBasicFacilityUGUIController controller =
                    root.GetComponent<WIAdministrationBasicFacilityUGUIController>();
                SerializedObject serialized = new SerializedObject(controller);
                Button tavernButton = serialized.FindProperty("tavernButton").objectReferenceValue as Button;
                GameObject facilityRoot = serialized.FindProperty("facilityRoot").objectReferenceValue as GameObject;

                Button castleHallButton = GetOrCloneButton(tavernButton, "CastleHallButton",
                    "성관 · 영지관 임명과 성 운영", 0.58f, 0.72f);
                Button marketButton = GetOrCloneButton(tavernButton, "MarketButton",
                    "시장 · 영지 수입과 성 기록", 0.40f, 0.54f);
                Button trainingGroundButton = GetOrCloneButton(tavernButton, "TrainingGroundButton",
                    "훈련소 · 개인 및 합동 훈련", 0.22f, 0.36f);
                ConfigureButton(tavernButton, "TavernQuestButton",
                    "선술집 · 소문, 인재 단서와 월간 의뢰", 0.04f, 0.18f);

                foreach (TMP_Text label in facilityRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.transform.IsChildOf(castleHallButton.transform) ||
                        label.transform.IsChildOf(marketButton.transform) ||
                        label.transform.IsChildOf(trainingGroundButton.transform) ||
                        label.transform.IsChildOf(tavernButton.transform))
                    {
                        continue;
                    }
                    label.gameObject.SetActive(false);
                }

                serialized.FindProperty("castleHallButton").objectReferenceValue = castleHallButton;
                serialized.FindProperty("marketButton").objectReferenceValue = marketButton;
                serialized.FindProperty("trainingGroundButton").objectReferenceValue = trainingGroundButton;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BasicFacilityPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            RebuildTerritoryFacilityHotspots();
        }

        // 성 전경 위에 기준 시안처럼 네 기본 시설의 고정 진입점을 배치합니다.
        private static void RebuildTerritoryFacilityHotspots()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TerritoryPrefabPath);
            try
            {
                WIAdministrationTerritoryUGUIController controller =
                    root.GetComponent<WIAdministrationTerritoryUGUIController>();
                SerializedObject serialized = new SerializedObject(controller);
                Button source = (serialized.FindProperty("commandButtons").GetArrayElementAtIndex(0)
                    .objectReferenceValue as Button);
                Image castleBackground = serialized.FindProperty("castleBackground").objectReferenceValue as Image;
                Transform parent = castleBackground.transform.parent;

                Button castleHall = GetOrCloneHotspot(source, parent, "CastleHallFacilityHotspot",
                    "성관", new Vector2(0.56f, 0.68f));
                Button market = GetOrCloneHotspot(source, parent, "MarketFacilityHotspot",
                    "시장", new Vector2(0.33f, 0.43f));
                Button training = GetOrCloneHotspot(source, parent, "TrainingFacilityHotspot",
                    "훈련소", new Vector2(0.68f, 0.48f));
                Button tavern = GetOrCloneHotspot(source, parent, "TavernFacilityHotspot",
                    "선술집", new Vector2(0.29f, 0.60f));

                SerializedProperty buttons = serialized.FindProperty("basicFacilityButtons");
                buttons.arraySize = 4;
                buttons.GetArrayElementAtIndex(0).objectReferenceValue = castleHall;
                buttons.GetArrayElementAtIndex(1).objectReferenceValue = market;
                buttons.GetArrayElementAtIndex(2).objectReferenceValue = training;
                buttons.GetArrayElementAtIndex(3).objectReferenceValue = tavern;

                SerializedProperty actions = serialized.FindProperty("basicFacilityActions");
                actions.arraySize = 4;
                actions.GetArrayElementAtIndex(0).enumValueIndex =
                    (int)WIAdministrationTerritoryCommand.Delegation;
                actions.GetArrayElementAtIndex(1).enumValueIndex =
                    (int)WIAdministrationTerritoryCommand.CastleRecord;
                actions.GetArrayElementAtIndex(2).enumValueIndex =
                    (int)WIAdministrationTerritoryCommand.CharacterActivity;
                actions.GetArrayElementAtIndex(3).enumValueIndex =
                    (int)WIAdministrationTerritoryCommand.BasicFacility;

                SerializedProperty facilitySlots = serialized.FindProperty("facilitySlots");
                SerializedProperty facilitySlotButtons = serialized.FindProperty("facilitySlotButtons");
                facilitySlotButtons.arraySize = facilitySlots.arraySize;
                for (int index = 0; index < facilitySlots.arraySize; index += 1)
                {
                    GameObject slot = facilitySlots.GetArrayElementAtIndex(index).objectReferenceValue as GameObject;
                    Button slotButton = slot.GetComponent<Button>();
                    if (slotButton == null)
                    {
                        slotButton = slot.AddComponent<Button>();
                    }
                    slotButton.targetGraphic = slot.GetComponent<Image>();
                    facilitySlotButtons.GetArrayElementAtIndex(index).objectReferenceValue = slotButton;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, TerritoryPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 같은 프리팹 내부에 이미 존재하면 재사용하고 없으면 기준 버튼을 복제합니다.
        private static Button GetOrCloneButton(Button source, string name, string label, float minY, float maxY)
        {
            Transform existing = source.transform.parent.Find(name);
            Button button = existing == null
                ? Object.Instantiate(source, source.transform.parent)
                : existing.GetComponent<Button>();
            ConfigureButton(button, name, label, minY, maxY);
            return button;
        }

        // 시설 버튼의 이름, 고정 배치와 표시 문구를 기준 시안형 목록에 맞춥니다.
        private static void ConfigureButton(Button button, string name, string label, float minY, float maxY)
        {
            button.name = name;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, minY);
            rect.anchorMax = new Vector2(0.88f, maxY);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            text.text = label;
            text.fontSize = 20f;
            text.textWrappingMode = TextWrappingModes.Normal;
            button.gameObject.SetActive(true);
        }

        // 기존 명령 버튼 스타일을 재사용해 성 전경 위 시설 핫스팟을 고정 배치합니다.
        private static Button GetOrCloneHotspot(Button source, Transform parent, string name, string label,
            Vector2 anchor)
        {
            Transform existing = parent.Find(name);
            Button button = existing == null
                ? Object.Instantiate(source, parent)
                : existing.GetComponent<Button>();
            button.name = name;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(150f, 46f);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            text.text = label;
            text.fontSize = 18f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            button.gameObject.SetActive(true);
            return button;
        }
    }
}
