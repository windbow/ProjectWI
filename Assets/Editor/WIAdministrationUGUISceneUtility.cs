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
        private const string LegacyMainCanvasName = "MainCanvas";

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

        // 현재 씬의 UGUI 관리자 루트를 찾고 이전 이름의 루트가 있으면 정식 이름으로 통합합니다.
        private static GameObject GetOrCreateRoot()
        {
            List<WIUIScreenManager> managers = FindSceneScreenManagers();
            GameObject root = FindManagerRootByName(managers, SceneRootName);
            if (root == null)
            {
                root = FindManagerRootByName(managers, LegacySceneRootName);
            }
            if (root == null)
            {
                root = FindManagerRootByName(managers, LegacyMainCanvasName);
            }
            if (root == null && managers.Count > 0)
            {
                root = managers[0].gameObject;
            }
            if (root == null)
            {
                root = new GameObject(SceneRootName);
            }
            else
            {
                root.name = SceneRootName;
            }
            return root;
        }

        // 모든 UGUI 관리자의 프리팹 참조를 하나로 병합하고 중복 관리자 루트를 제거합니다.
        private static void ConfigureBootstrap(GameObject root, GameObject additionalPrefab)
        {
            WIUIScreenManager screenManager = root.GetComponent<WIUIScreenManager>();
            if (screenManager == null)
            {
                screenManager = root.AddComponent<WIUIScreenManager>();
            }
            SerializedObject serialized = new SerializedObject(screenManager);
            SerializedProperty screens = serialized.FindProperty("screenPrefabs");
            List<GameObject> prefabs = new List<GameObject>();
            List<WIUIScreenManager> managers = FindSceneScreenManagers();
            foreach (WIUIScreenManager manager in managers)
            {
                CollectRegisteredPrefabs(manager, prefabs);
                CollectChildPrefabSources(manager.transform, prefabs);
            }
            AddUnique(prefabs, additionalPrefab);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Administration" });
            List<string> prefabPaths = new List<string>();
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("UGUI.prefab"))
                {
                    prefabPaths.Add(path);
                }
            }
            prefabPaths.Sort();
            foreach (string path in prefabPaths)
            {
                AddUnique(prefabs, AssetDatabase.LoadAssetAtPath<GameObject>(path));
            }
            screens.arraySize = prefabs.Count;
            for (int index = 0; index < prefabs.Count; index += 1)
            {
                screens.GetArrayElementAtIndex(index).objectReferenceValue = prefabs[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            while (root.transform.childCount > 0)
            {
                Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            }
            foreach (WIUIScreenManager manager in managers)
            {
                if (manager != null && manager.gameObject != root)
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }
        }

        // 현재 활성 씬에 속한 UGUI 관리자만 수집합니다.
        private static List<WIUIScreenManager> FindSceneScreenManagers()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            WIUIScreenManager[] allManagers = Resources.FindObjectsOfTypeAll<WIUIScreenManager>();
            List<WIUIScreenManager> sceneManagers = new List<WIUIScreenManager>();
            foreach (WIUIScreenManager manager in allManagers)
            {
                if (manager != null && manager.gameObject.scene == activeScene)
                {
                    sceneManagers.Add(manager);
                }
            }
            return sceneManagers;
        }

        // 관리자 목록에서 지정한 이름의 루트를 찾습니다.
        private static GameObject FindManagerRootByName(List<WIUIScreenManager> managers, string rootName)
        {
            foreach (WIUIScreenManager manager in managers)
            {
                if (manager != null && manager.gameObject.name == rootName)
                {
                    return manager.gameObject;
                }
            }
            return null;
        }

        // 관리자가 직렬화해 둔 화면 프리팹 참조를 수집합니다.
        private static void CollectRegisteredPrefabs(WIUIScreenManager manager, List<GameObject> prefabs)
        {
            SerializedObject managerSerialized = new SerializedObject(manager);
            SerializedProperty managerScreens = managerSerialized.FindProperty("screenPrefabs");
            for (int index = 0; index < managerScreens.arraySize; index += 1)
            {
                AddUnique(prefabs, managerScreens.GetArrayElementAtIndex(index).objectReferenceValue as GameObject);
            }
        }

        // 관리자 아래에 남은 씬 인스턴스의 원본 프리팹을 수집합니다.
        private static void CollectChildPrefabSources(Transform managerRoot, List<GameObject> prefabs)
        {
            for (int index = 0; index < managerRoot.childCount; index += 1)
            {
                GameObject screen = managerRoot.GetChild(index).gameObject;
                AddUnique(prefabs, PrefabUtility.GetCorrespondingObjectFromSource(screen));
            }
        }

        // null과 중복을 제외하고 프리팹 참조를 목록에 추가합니다.
        private static void AddUnique(List<GameObject> prefabs, GameObject prefab)
        {
            if (prefab != null && prefabs.Contains(prefab) == false)
            {
                prefabs.Add(prefab);
            }
        }
    }
}
