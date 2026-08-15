using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectWI.Administration;
using System.Collections.Generic;

namespace ProjectWI.EditorTools
{
    public static class WIAdministrationUGUISceneUtility
    {
        public const string SceneRootName = "UGUI Screen Bootstrap";
        private const string LegacySceneRootName = "UGUI Screens (Prefab Instances)";

        // 생성된 UGUI 프리팹 에셋을 플레이용 부트스트랩에 등록합니다.
        public static GameObject InstantiateUnderSceneRoot(GameObject prefab)
        {
            GameObject root = GetOrCreateRoot();
            ConfigureBootstrap(root, prefab);
            return null;
        }

        // 현재 MainScene의 UGUI 인스턴스를 프리팹 참조 부트스트랩으로 변환합니다.
        [MenuItem("WI/UI/Configure UGUI Scene Visibility")]
        public static void ConfigureSceneVisibility()
        {
            GameObject root = GetOrCreateRoot();
            ConfigureBootstrap(root, null);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("UGUI 화면 인스턴스를 제거하고 런타임 프리팹 부트스트랩으로 정리했습니다.");
        }

        // UGUI 정리 루트를 찾거나 새로 생성합니다.
        private static GameObject GetOrCreateRoot()
        {
            GameObject root = GameObject.Find(SceneRootName);
            if (root != null) return root;
            root = GameObject.Find(LegacySceneRootName);
            if (root != null)
            {
                root.name = SceneRootName;
                return root;
            }
            return new GameObject(SceneRootName);
        }

        // 기존 씬 인스턴스의 원본과 신규 프리팹을 수집하고 씬 자식을 제거합니다.
        private static void ConfigureBootstrap(GameObject root, GameObject additionalPrefab)
        {
            WIUIScreenManager screenManager = root.GetComponent<WIUIScreenManager>();
            if (screenManager == null) screenManager = root.AddComponent<WIUIScreenManager>();
            SerializedObject serialized = new SerializedObject(screenManager);
            SerializedProperty screens = serialized.FindProperty("screenPrefabs");
            List<GameObject> prefabs = new List<GameObject>();
            for (int index = 0; index < screens.arraySize; index += 1)
            {
                AddUnique(prefabs, screens.GetArrayElementAtIndex(index).objectReferenceValue as GameObject);
            }
            for (int index = 0; index < root.transform.childCount; index += 1)
            {
                GameObject screen = root.transform.GetChild(index).gameObject;
                AddUnique(prefabs, PrefabUtility.GetCorrespondingObjectFromSource(screen));
            }
            AddUnique(prefabs, additionalPrefab);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Administration" });
            List<string> prefabPaths = new List<string>();
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("UGUI.prefab")) prefabPaths.Add(path);
            }
            prefabPaths.Sort();
            foreach (string path in prefabPaths)
                AddUnique(prefabs, AssetDatabase.LoadAssetAtPath<GameObject>(path));
            screens.arraySize = prefabs.Count;
            for (int index = 0; index < prefabs.Count; index += 1)
                screens.GetArrayElementAtIndex(index).objectReferenceValue = prefabs[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            while (root.transform.childCount > 0)
                Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
        }

        // null과 중복을 제외하고 프리팹 참조를 목록에 추가합니다.
        private static void AddUnique(List<GameObject> prefabs, GameObject prefab)
        {
            if (prefab != null && prefabs.Contains(prefab) == false) prefabs.Add(prefab);
        }
    }
}
