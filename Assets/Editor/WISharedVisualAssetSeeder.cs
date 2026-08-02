using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectWI.Administration;

namespace ProjectWI.Editor
{
    public static class WISharedVisualAssetSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        private static readonly Dictionary<string, string> PortraitPaths = new Dictionary<string, string>
        {
            { "ares", "Assets/Art/Characters/Portrait_ares_V1.png" },
            { "lyria", "Assets/Art/Characters/Portrait_lyria_V1.png" },
            { "brom", "Assets/Art/Characters/Portrait_brom_V1.png" },
            { "selene", "Assets/Art/Characters/Portrait_selene_V1.png" },
            { "kael", "Assets/Art/Characters/Portrait_kael_V1.png" },
            { "morrigan", "Assets/Art/Characters/Portrait_morrigan_V1.png" },
            { "theron", "Assets/Art/Characters/Portrait_theron_V1.png" },
            { "elwyn", "Assets/Art/Characters/Portrait_elwyn_V1.png" },
            { "common_gareth", "Assets/Art/Characters/Portrait_common_gareth_V1.png" },
            { "common_mira", "Assets/Art/Characters/Portrait_common_mira_V1.png" },
            { "common_thane", "Assets/Art/Characters/Portrait_common_thane_V1.png" },
            { "common_nym", "Assets/Art/Characters/Portrait_common_nym_V1.png" },
            { "common_raska", "Assets/Art/Characters/Portrait_common_raska_V1.png" },
            { "common_veil", "Assets/Art/Characters/Portrait_common_veil_V1.png" }
        };

        private static readonly Dictionary<string, string> FacilityPaths = new Dictionary<string, string>
        {
            { "grand_forge", "Assets/Art/Facilities/Facility_grand_forge_V1.png" },
            { "mage_tower", "Assets/Art/Facilities/Facility_mage_tower_V1.png" },
            { "grand_market", "Assets/Art/Facilities/Facility_grand_market_V1.png" },
            { "knightly_order", "Assets/Art/Facilities/Facility_knightly_order_V1.png" },
            { "adventurers_guild", "Assets/Art/Facilities/Facility_adventurers_guild_V1.png" },
            { "sanctuary", "Assets/Art/Facilities/Facility_sanctuary_V1.png" },
            { "spy_outpost", "Assets/Art/Facilities/Facility_spy_outpost_V1.png" },
            { "embassy", "Assets/Art/Facilities/Facility_embassy_V1.png" }
        };

        [MenuItem("ProjectWI/Data/Assign Shared Visual Assets")]
        // 일반 성 전경, 전체 인물 초상화와 특화 시설 아이콘을 Sprite로 임포트해 데이터베이스에 연결합니다.
        public static void AssignSharedVisualAssets()
        {
            List<string> paths = new List<string>(PortraitPaths.Values);
            paths.AddRange(FacilityPaths.Values);
            paths.AddRange(new[]
            {
                "Assets/Art/Castles/Common/Castle_Common_ValdorCity_V1.png",
                "Assets/Art/Castles/Common/Castle_Common_ValdorFortress_V1.png",
                "Assets/Art/Castles/Common/Castle_Common_Ironheart_V1.png",
                "Assets/Art/Castles/Common/Castle_Common_Sylvanroad_V1.png",
                "Assets/Art/Castles/Common/Castle_Common_Necropolis_V1.png"
            });
            foreach (string path in paths)
            {
                ImportAsSprite(path);
            }

            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            SerializedObject serializedDatabase = new SerializedObject(database);
            AssignMappedSprites(serializedDatabase.FindProperty("heroes"), "portrait", PortraitPaths);
            AssignMappedSprites(serializedDatabase.FindProperty("specialFacilities"), "icon", FacilityPaths);
            AssignCastleSprites(serializedDatabase.FindProperty("castles"));
            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 공용 전경 55성, 초상화 {PortraitPaths.Count}종, 시설 아이콘 {FacilityPaths.Count}종을 연결했습니다.");
        }

        [MenuItem("ProjectWI/Verification/Open Common Castle Preview")]
        // 플레이 모드에서 일반 성 전경과 인물 카드가 있는 성 화면을 엽니다.
        public static void OpenCommonCastlePreview()
        {
            if (Application.isPlaying == false)
            {
                Debug.LogWarning("일반 성 미리보기는 플레이 모드에서 실행해야 합니다.");
                return;
            }

            WIAdministrationUIController controller = Object.FindFirstObjectByType<WIAdministrationUIController>();
            if (controller == null)
            {
                Debug.LogError("내정 UI 컨트롤러를 찾을 수 없습니다.");
                return;
            }

            controller.OpenCastle("castle_02");
            Debug.Log("ProjectWI 일반 성 PC 미리보기를 열었습니다.");
        }

        // PNG를 UI용 단일 Sprite로 설정하고 밉맵을 비활성화합니다.
        private static void ImportAsSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"이미지 임포터를 찾을 수 없습니다: {path}");
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        // ID와 경로 사전을 사용해 배열 요소의 Sprite 필드를 연결합니다.
        private static void AssignMappedSprites(SerializedProperty items, string spriteField, Dictionary<string, string> paths)
        {
            for (int index = 0; index < items.arraySize; index++)
            {
                SerializedProperty item = items.GetArrayElementAtIndex(index);
                string id = item.FindPropertyRelative("id").stringValue;
                if (paths.TryGetValue(id, out string path))
                {
                    item.FindPropertyRelative(spriteField).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
        }

        // 수도 전경은 유지하고 나머지 성에 세력별 공용 전경을 배정합니다.
        private static void AssignCastleSprites(SerializedProperty castles)
        {
            for (int index = 0; index < castles.arraySize; index++)
            {
                SerializedProperty castle = castles.GetArrayElementAtIndex(index);
                SerializedProperty image = castle.FindPropertyRelative("castleImage");
                if (image.objectReferenceValue != null)
                {
                    continue;
                }

                string factionId = castle.FindPropertyRelative("factionId").stringValue;
                string id = castle.FindPropertyRelative("id").stringValue;
                string path = factionId switch
                {
                    "ironheart" => "Assets/Art/Castles/Common/Castle_Common_Ironheart_V1.png",
                    "sylvanroad" => "Assets/Art/Castles/Common/Castle_Common_Sylvanroad_V1.png",
                    "necropolis" => "Assets/Art/Castles/Common/Castle_Common_Necropolis_V1.png",
                    _ => int.Parse(id.Substring(id.Length - 2)) % 3 == 0
                        ? "Assets/Art/Castles/Common/Castle_Common_ValdorFortress_V1.png"
                        : "Assets/Art/Castles/Common/Castle_Common_ValdorCity_V1.png"
                };
                image.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
    }
}
