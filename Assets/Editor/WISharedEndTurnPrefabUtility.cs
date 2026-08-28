using ProjectWI.Administration;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WISharedEndTurnPrefabUtility
    {
        private const string WorldPath = "Assets/Prefabs/Administration/WIAdministrationWorldUGUI.prefab";
        private const string TerritoryPath = "Assets/Prefabs/Administration/WIAdministrationTerritoryUGUI.prefab";
        private const string SharedPath = "Assets/Prefabs/Administration/WIAdministrationEndTurnUGUI.prefab";

        // 월드 다음 턴 버튼을 공용 프리팹으로 만들고 월드와 영지 화면에 동일 인스턴스로 연결합니다.
        [MenuItem("WI/UI/Rebuild Shared End Turn Button")]
        public static void Rebuild()
        {
            GameObject worldRoot = PrefabUtility.LoadPrefabContents(WorldPath);
            Transform source = worldRoot.transform.Find("WorldContent/CommandBar/EndTurnButton");
            if (source == null)
            {
                PrefabUtility.UnloadPrefabContents(worldRoot);
                Debug.LogError("월드 다음 턴 버튼 원본을 찾을 수 없습니다.");
                return;
            }

            if (source.GetComponent<WIAdministrationEndTurnUGUIController>() == null)
            {
                source.gameObject.AddComponent<WIAdministrationEndTurnUGUIController>();
            }
            PrefabUtility.SaveAsPrefabAsset(source.gameObject, SharedPath);
            PrefabUtility.UnloadPrefabContents(worldRoot);

            ReplaceButton(WorldPath, "WorldContent/CommandBar/EndTurnButton", "endTurn");
            ReplaceButton(TerritoryPath, "TerritoryContent/NextTurnButton", "endTurn");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 지정 화면의 기존 버튼을 공용 중첩 프리팹으로 교체하고 컨트롤러 참조를 갱신합니다.
        private static void ReplaceButton(string prefabPath, string buttonPath, string propertyName)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            Transform oldButton = root.transform.Find(buttonPath);
            Transform parent = oldButton.parent;
            int siblingIndex = oldButton.GetSiblingIndex();
            Object.DestroyImmediate(oldButton.gameObject);

            GameObject shared = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(shared, root.scene);
            instance.transform.SetParent(parent, false);
            instance.transform.SetSiblingIndex(siblingIndex);

            MonoBehaviour screenController = root.GetComponent<WIAdministrationWorldUGUIController>();
            if (screenController == null)
            {
                screenController = root.GetComponent<WIAdministrationTerritoryUGUIController>();
            }
            SerializedObject serializedController = new SerializedObject(screenController);
            serializedController.FindProperty(propertyName).objectReferenceValue =
                instance.GetComponent<WIAdministrationEndTurnUGUIController>();
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
