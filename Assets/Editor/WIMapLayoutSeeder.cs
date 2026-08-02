using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.Editor
{
    public static class WIMapLayoutSeeder
    {
        private const string DatabasePath = "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset";

        private static readonly Dictionary<string, Vector2> CastlePositions = new Dictionary<string, Vector2>
        {
            { "castle_00", new Vector2(0.18f, 0.48f) },

            { "castle_01", new Vector2(0.27f, 0.43f) }, { "castle_02", new Vector2(0.36f, 0.38f) },
            { "castle_03", new Vector2(0.46f, 0.34f) }, { "castle_04", new Vector2(0.57f, 0.32f) },
            { "castle_05", new Vector2(0.68f, 0.31f) }, { "castle_06", new Vector2(0.78f, 0.33f) },
            { "castle_07", new Vector2(0.86f, 0.38f) }, { "castle_08", new Vector2(0.89f, 0.46f) },
            { "castle_09", new Vector2(0.86f, 0.54f) },
            { "castle_10", new Vector2(0.25f, 0.52f) }, { "castle_11", new Vector2(0.34f, 0.48f) },
            { "castle_12", new Vector2(0.44f, 0.44f) }, { "castle_13", new Vector2(0.54f, 0.42f) },
            { "castle_14", new Vector2(0.64f, 0.41f) }, { "castle_15", new Vector2(0.74f, 0.43f) },
            { "castle_16", new Vector2(0.81f, 0.48f) }, { "castle_17", new Vector2(0.80f, 0.57f) },
            { "castle_18", new Vector2(0.72f, 0.61f) }, { "castle_19", new Vector2(0.62f, 0.61f) },
            { "castle_20", new Vector2(0.28f, 0.60f) }, { "castle_21", new Vector2(0.38f, 0.57f) },
            { "castle_22", new Vector2(0.48f, 0.54f) }, { "castle_23", new Vector2(0.57f, 0.51f) },
            { "castle_24", new Vector2(0.66f, 0.50f) }, { "castle_25", new Vector2(0.70f, 0.56f) },

            { "castle_26", new Vector2(0.72f, 0.23f) }, { "castle_27", new Vector2(0.63f, 0.20f) },
            { "castle_28", new Vector2(0.54f, 0.18f) }, { "castle_29", new Vector2(0.45f, 0.19f) },
            { "castle_30", new Vector2(0.25f, 0.29f) }, { "castle_31", new Vector2(0.32f, 0.23f) },
            { "castle_32", new Vector2(0.40f, 0.16f) }, { "castle_33", new Vector2(0.49f, 0.12f) },
            { "castle_34", new Vector2(0.59f, 0.13f) }, { "castle_35", new Vector2(0.68f, 0.16f) },
            { "castle_36", new Vector2(0.77f, 0.20f) }, { "castle_37", new Vector2(0.83f, 0.26f) },

            { "castle_38", new Vector2(0.16f, 0.39f) }, { "castle_39", new Vector2(0.11f, 0.46f) },
            { "castle_40", new Vector2(0.12f, 0.55f) }, { "castle_41", new Vector2(0.18f, 0.58f) },
            { "castle_42", new Vector2(0.23f, 0.64f) }, { "castle_43", new Vector2(0.18f, 0.69f) },
            { "castle_44", new Vector2(0.11f, 0.66f) }, { "castle_45", new Vector2(0.13f, 0.75f) },
            { "castle_46", new Vector2(0.21f, 0.78f) }, { "castle_47", new Vector2(0.29f, 0.74f) },
            { "castle_48", new Vector2(0.31f, 0.66f) }, { "castle_49", new Vector2(0.27f, 0.52f) },

            { "castle_50", new Vector2(0.36f, 0.75f) }, { "castle_51", new Vector2(0.43f, 0.80f) },
            { "castle_52", new Vector2(0.51f, 0.84f) }, { "castle_53", new Vector2(0.60f, 0.87f) },
            { "castle_54", new Vector2(0.70f, 0.86f) }, { "castle_55", new Vector2(0.79f, 0.82f) },
            { "castle_56", new Vector2(0.84f, 0.75f) }, { "castle_57", new Vector2(0.76f, 0.71f) },
            { "castle_58", new Vector2(0.65f, 0.73f) }, { "castle_59", new Vector2(0.54f, 0.76f) }
        };

        [MenuItem("ProjectWI/Data/Assign Geographic Castle Layout")]
        // 60개 성을 대륙 지형과 세력권에 맞는 정규화 좌표로 배치합니다.
        public static void AssignGeographicCastleLayout()
        {
            Object database = AssetDatabase.LoadMainAssetAtPath(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"내정 데이터베이스를 찾을 수 없습니다: {DatabasePath}");
                return;
            }

            SerializedObject serializedDatabase = new SerializedObject(database);
            SerializedProperty castles = serializedDatabase.FindProperty("castles");
            int assignedCount = 0;
            for (int index = 0; index < castles.arraySize; index++)
            {
                SerializedProperty castle = castles.GetArrayElementAtIndex(index);
                string id = castle.FindPropertyRelative("id").stringValue;
                if (CastlePositions.TryGetValue(id, out Vector2 position) == false)
                {
                    Debug.LogWarning($"지도 좌표가 정의되지 않은 성입니다: {id}");
                    continue;
                }

                castle.FindPropertyRelative("normalizedMapPosition").vector2Value = position;
                assignedCount++;
            }

            AssignGeographicConnections(castles);

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"ProjectWI 성 지도 좌표 {assignedCount}개를 지형 기반으로 갱신했습니다.");
        }

        // 각 세력 내부에 최소 연결 도로망과 보조 도로를 만들고 국경 관문만 별도로 연결합니다.
        private static void AssignGeographicConnections(SerializedProperty castles)
        {
            Dictionary<string, SerializedProperty> castleProperties = new Dictionary<string, SerializedProperty>();
            Dictionary<string, string> factionByCastle = new Dictionary<string, string>();
            for (int index = 0; index < castles.arraySize; index++)
            {
                SerializedProperty castle = castles.GetArrayElementAtIndex(index);
                string id = castle.FindPropertyRelative("id").stringValue;
                castleProperties[id] = castle;
                factionByCastle[id] = castle.FindPropertyRelative("factionId").stringValue;
            }

            Dictionary<string, HashSet<string>> connections = CastlePositions.Keys
                .ToDictionary(id => id, _ => new HashSet<string>());
            foreach (IGrouping<string, string> factionGroup in factionByCastle.GroupBy(pair => pair.Value, pair => pair.Key))
            {
                ConnectFactionRoads(factionGroup.ToList(), connections);
            }

            Connect("castle_00", "castle_01", connections);
            Connect("castle_00", "castle_38", connections);
            Connect("castle_03", "castle_29", connections);
            Connect("castle_20", "castle_49", connections);
            Connect("castle_19", "castle_57", connections);
            Connect("castle_47", "castle_50", connections);
            Connect("castle_30", "castle_38", connections);

            foreach (KeyValuePair<string, SerializedProperty> pair in castleProperties)
            {
                SerializedProperty adjacentIds = pair.Value.FindPropertyRelative("adjacentCastleIds");
                string[] orderedConnections = connections[pair.Key].OrderBy(id => id).ToArray();
                adjacentIds.arraySize = orderedConnections.Length;
                for (int index = 0; index < orderedConnections.Length; index++)
                {
                    adjacentIds.GetArrayElementAtIndex(index).stringValue = orderedConnections[index];
                }
            }
        }

        // 한 세력의 모든 성을 최소 신장 트리로 연결한 뒤 각 성에 가까운 보조 도로를 하나 이상 추가합니다.
        private static void ConnectFactionRoads(IReadOnlyList<string> castleIds, Dictionary<string, HashSet<string>> connections)
        {
            if (castleIds.Count < 2)
            {
                return;
            }

            HashSet<string> connected = new HashSet<string> { castleIds[0] };
            while (connected.Count < castleIds.Count)
            {
                (string from, string to, float distance) best = (null, null, float.MaxValue);
                foreach (string from in connected)
                {
                    foreach (string to in castleIds.Where(id => connected.Contains(id) == false))
                    {
                        float distance = Vector2.SqrMagnitude(CastlePositions[from] - CastlePositions[to]);
                        if (distance < best.distance)
                        {
                            best = (from, to, distance);
                        }
                    }
                }

                Connect(best.from, best.to, connections);
                connected.Add(best.to);
            }

            foreach (string castleId in castleIds)
            {
                string nearest = castleIds
                    .Where(otherId => otherId != castleId && connections[castleId].Contains(otherId) == false)
                    .OrderBy(otherId => Vector2.SqrMagnitude(CastlePositions[castleId] - CastlePositions[otherId]))
                    .FirstOrDefault();
                if (nearest != null && connections[castleId].Count < 3)
                {
                    Connect(castleId, nearest, connections);
                }
            }
        }

        // 두 성의 인접 관계를 양방향으로 등록합니다.
        private static void Connect(string firstId, string secondId, Dictionary<string, HashSet<string>> connections)
        {
            connections[firstId].Add(secondId);
            connections[secondId].Add(firstId);
        }
    }
}
