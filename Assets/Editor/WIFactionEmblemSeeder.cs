using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WIFactionEmblemSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        private static readonly Dictionary<string, string> EmblemPaths = new Dictionary<string, string>
        {
            { "avalon", "Assets/Art/Factions/Emblem_Avalon_V1.png" },
            { "valdor", "Assets/Art/Factions/Emblem_Valdor_V1.png" },
            { "ironheart", "Assets/Art/Factions/Emblem_Ironheart_V1.png" },
            { "sylvanroad", "Assets/Art/Factions/Emblem_Sylvanroad_V1.png" },
            { "necropolis", "Assets/Art/Factions/Emblem_Necropolis_V1.png" }
        };

        [MenuItem("ProjectWI/Data/Assign Faction Emblems")]
        // 세력 문장 PNG를 Sprite로 임포트하고 각 세력 데이터에 연결합니다.
        public static void AssignFactionEmblems()
        {
            foreach (string path in EmblemPaths.Values)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"세력 문장 임포터를 찾을 수 없습니다: {path}");
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"내정 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty factions = serializedDatabase.FindProperty("factions");
            for (int index = 0; index < factions.arraySize; index++)
            {
                SerializedProperty faction = factions.GetArrayElementAtIndex(index);
                string id = faction.FindPropertyRelative("id").stringValue;
                if (EmblemPaths.TryGetValue(id, out string path) == false)
                {
                    continue;
                }

                Sprite emblem = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                faction.FindPropertyRelative("emblem").objectReferenceValue = emblem;
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 세력 문장 {EmblemPaths.Count}종을 연결했습니다.");
        }
    }
}
