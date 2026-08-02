using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WIVisualAssetSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";
        private static readonly Dictionary<string, string> CastleImagePaths = new Dictionary<string, string>
        {
            { "castle_00", "Assets/Art/Castles/Castle_Avalon_V1.png" },
            { "castle_01", "Assets/Art/Castles/Castle_Valdor_V1.png" },
            { "castle_26", "Assets/Art/Castles/Castle_Ironhold_V1.png" },
            { "castle_38", "Assets/Art/Castles/Castle_Sylvarion_V1.png" },
            { "castle_50", "Assets/Art/Castles/Castle_Necropolis_V1.png" }
        };

        [MenuItem("ProjectWI/Data/Assign Visual Assets")]
        // 제작된 시각 에셋을 알맞은 ScriptableObject 데이터에 연결합니다.
        public static void AssignVisualAssets()
        {
            foreach (string path in CastleImagePaths.Values)
            {
                ConfigureSprite(path);
            }

            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"내정 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty castles = serializedDatabase.FindProperty("castles");
            for (int index = 0; index < castles.arraySize; index++)
            {
                SerializedProperty castle = castles.GetArrayElementAtIndex(index);
                string id = castle.FindPropertyRelative("id").stringValue;
                if (CastleImagePaths.TryGetValue(id, out string path) == false)
                {
                    continue;
                }

                castle.FindPropertyRelative("castleImage").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 5대 세력 수도 전경 {CastleImagePaths.Count}종을 연결했습니다.");
        }

        // UI 배경 이미지를 단일 Sprite로 임포트하도록 설정합니다.
        private static void ConfigureSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"시각 에셋 임포터를 찾을 수 없습니다: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
