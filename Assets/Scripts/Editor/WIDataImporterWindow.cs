#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using ProjectWI.Data;

namespace ProjectWI.Editor
{
    public enum ImportDataType
    {
        DungeonData,
        DungeonEvent,
        MonsterData,
        JobData,
        SkillData,
    }

    public class WIDataImporterWindow : EditorWindow
    {
        private ImportDataType selectedType = ImportDataType.DungeonEvent;
        private TextAsset dataFile; // CSV 또는 TSV 파일 할당용
        private Vector2 scrollPos;

        [MenuItem("WI Tools/Universal Data Importer")]
        public static void ShowWindow()
        {
            GetWindow<WIDataImporterWindow>("통합 데이터 임포터");
        }

        private void OnGUI()
        {
            GUILayout.Label("기획 데이터 (CSV/TSV) 자동 변환기", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            selectedType = (ImportDataType)EditorGUILayout.EnumPopup("데이터 종류 선택", selectedType);
            EditorGUILayout.Space();

            GUILayout.Label("유니티 Project 창에 임포트된 CSV 또는 TSV 파일을 아래에 드래그하여 할당하세요:");
            dataFile = (TextAsset)EditorGUILayout.ObjectField("데이터 파일 (CSV/TSV)", dataFile, typeof(TextAsset), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("SO 에셋 생성 / 덮어쓰기 (Generate)", GUILayout.Height(40)))
            {
                if (dataFile == null)
                {
                    EditorUtility.DisplayDialog("경고", "데이터 파일이 할당되지 않았습니다.", "확인");
                    return;
                }
                ParseAndGenerate(dataFile.text);
            }

            EditorGUILayout.Space(20);
            GUILayout.Label("유니티 에셋 -> CSV 추출", EditorStyles.boldLabel);
            
            if (GUILayout.Button("현재 선택 타입 SO 전체 -> CSV로 내보내기 (Export)", GUILayout.Height(40)))
            {
                ExportData();
            }
        }

        private void ParseAndGenerate(string rawText)
        {
            string[] rows = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length <= 1)
            {
                EditorUtility.DisplayDialog("알림", "헤더(머리글) 외에 유효한 데이터 줄이 없습니다.", "확인");
                return;
            }

            int successCount = 0;

            try
            {
                // 데이터 형식이 TSV인지 CSV인지 판별 (첫 줄 기준)
                bool isTsv = rows[0].Contains("\t");

                for (int i = 1; i < rows.Length; i++)
                {
                    // 파싱 로직 분기
                    string[] columns = isTsv ? rows[i].Split('\t') : SplitCsvLine(rows[i]);
                    
                    // 빈 줄 무시
                    if (columns.Length < 2 || string.IsNullOrWhiteSpace(columns[0])) continue;

                    string id = columns[0].Trim();

                    switch (selectedType)
                    {
                        case ImportDataType.DungeonEvent:
                            successCount += ParseDungeonEvent(id, columns) ? 1 : 0;
                            break;
                        case ImportDataType.MonsterData:
                            successCount += ParseMonsterData(id, columns) ? 1 : 0;
                            break;
                        case ImportDataType.JobData:
                            successCount += ParseJobData(id, columns) ? 1 : 0;
                            break;
                        case ImportDataType.DungeonData:
                            successCount += ParseDungeonData(id, columns) ? 1 : 0;
                            break;
                        case ImportDataType.SkillData:
                            successCount += ParseSkillData(id, columns) ? 1 : 0;
                            break;
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("성공", $"{successCount}개의 에셋이 성공적으로 처리되었습니다.", "확인");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("오류", "데이터 파싱 중 오류가 발생했습니다. 양식이 맞는지 확인해주세요.\n\n" + ex.Message, "확인");
            }
        }

        private void ExportData()
        {
            string defaultFolder = "Assets/Data/Excel";
            EnsureFolderExists(defaultFolder);

            string path = EditorUtility.SaveFilePanel("Export to CSV", defaultFolder, selectedType.ToString() + "_Export.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int count = 0;

            try
            {
                switch (selectedType)
                {
                    case ImportDataType.DungeonEvent:
                        sb.AppendLine("ID,EventType,LogMessage,Weight,RewardGold,RewardExp");
                        foreach (string guid in AssetDatabase.FindAssets("t:WIDungeonEventSO"))
                        {
                            var so = AssetDatabase.LoadAssetAtPath<WIDungeonEventSO>(AssetDatabase.GUIDToAssetPath(guid));
                            if (so == null) continue;
                            sb.AppendLine($"{EscapeCsv(so.id)},{EscapeCsv(so.eventType.ToString())},{EscapeCsv(so.logMessage)},{so.weight},{so.rewardGold},{so.rewardExp}");
                            count++;
                        }
                        break;
                    case ImportDataType.MonsterData:
                        sb.AppendLine("ID,MonsterName,MaxHp,Speed,AttackPower,RewardGold,RewardExp");
                        foreach (string guid in AssetDatabase.FindAssets("t:WIMonsterDataSO"))
                        {
                            var so = AssetDatabase.LoadAssetAtPath<WIMonsterDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                            if (so == null) continue;
                            sb.AppendLine($"{EscapeCsv(so.id)},{EscapeCsv(so.monsterName)},{so.maxHp},{so.speed},{so.attackPower},{so.killRewardGold},{so.killRewardExp}");
                            count++;
                        }
                        break;
                    case ImportDataType.JobData:
                        sb.AppendLine("ID,JobName,Description,BaseHp,BaseSpeed,BaseAttack");
                        foreach (string guid in AssetDatabase.FindAssets("t:WIJobDataSO"))
                        {
                            var so = AssetDatabase.LoadAssetAtPath<WIJobDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                            if (so == null) continue;
                            sb.AppendLine($"{EscapeCsv(so.id)},{EscapeCsv(so.jobName)},{EscapeCsv(so.description)},{so.baseHp},{so.baseSpeed},{so.baseAttack}");
                            count++;
                        }
                        break;
                    case ImportDataType.DungeonData:
                        sb.AppendLine("DungeonID,DungeonName");
                        foreach (string guid in AssetDatabase.FindAssets("t:WIDungeonDataSO"))
                        {
                            var so = AssetDatabase.LoadAssetAtPath<WIDungeonDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                            if (so == null) continue;
                            sb.AppendLine($"{so.dungeonId},{EscapeCsv(so.dungeonName)}");
                            count++;
                        }
                        break;
                    case ImportDataType.SkillData:
                        sb.AppendLine("ID,SkillName,Description,Cooldown,DamageMultiplier,ManaCost");
                        foreach (string guid in AssetDatabase.FindAssets("t:WISkillDataSO"))
                        {
                            var so = AssetDatabase.LoadAssetAtPath<WISkillDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                            if (so == null) continue;
                            sb.AppendLine($"{EscapeCsv(so.id)},{EscapeCsv(so.skillName)},{EscapeCsv(so.description)},{so.cooldown},{so.damageMultiplier},{so.manaCost}");
                            count++;
                        }
                        break;
                }

                // UTF-8 with BOM for Excel compatibility
                File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
                EditorUtility.DisplayDialog("성공", $"{count}개의 에셋을 CSV로 내보냈습니다.\n\n저장 경로:\n{path}", "확인");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("오류", "내보내기 중 문제가 발생했습니다.\n\n" + ex.Message, "확인");
            }
        }

        // ==========================================
        // [1] 던전 이벤트 파서
        // 엑셀 포맷: ID | EventType | LogMessage | Weight | RewardGold | RewardExp
        // ==========================================
        private bool ParseDungeonEvent(string id, string[] cols)
        {
            if (cols.Length < 6) return false;

            string folderPath = "Assets/Data/ScriptableObject/Events";
            EnsureFolderExists(folderPath);

            string assetPath = $"{folderPath}/{id}.asset";
            WIDungeonEventSO asset = AssetDatabase.LoadAssetAtPath<WIDungeonEventSO>(assetPath);

            bool isNew = false;
            if (asset == null)
            {
                asset = CreateInstance<WIDungeonEventSO>();
                isNew = true;
            }

            asset.id = id;
            if (Enum.TryParse(cols[1], true, out WIDungeonEventType type)) asset.eventType = type;
            asset.logMessage = cols[2];
            if (int.TryParse(cols[3], out int w)) asset.weight = w;
            if (int.TryParse(cols[4], out int g)) asset.rewardGold = g;
            if (int.TryParse(cols[5], out int e)) asset.rewardExp = e;

            if (isNew) AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            return true;
        }

        // ==========================================
        // [2] 직업 데이터 파서
        // 엑셀 포맷: ID | JobName | Description | BaseHp | BaseSpeed | BaseAttack
        // ==========================================
        private bool ParseJobData(string id, string[] cols)
        {
            if (cols.Length < 6) return false;

            string folderPath = "Assets/Data/ScriptableObject/Jobs";
            EnsureFolderExists(folderPath);

            string assetPath = $"{folderPath}/{id}.asset";
            WIJobDataSO asset = AssetDatabase.LoadAssetAtPath<WIJobDataSO>(assetPath);

            bool isNew = false;
            if (asset == null)
            {
                asset = CreateInstance<WIJobDataSO>();
                isNew = true;
            }

            asset.id = id;
            asset.jobName = cols[1];
            asset.description = cols[2];
            if (float.TryParse(cols[3], out float hp)) asset.baseHp = hp;
            if (float.TryParse(cols[4], out float spd)) asset.baseSpeed = spd;
            if (float.TryParse(cols[5], out float atk)) asset.baseAttack = atk;

            if (isNew) AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            return true;
        }

        // ==========================================
        // [3] 몬스터 데이터 파서
        // 엑셀 포맷: ID | MonsterName | MaxHp | Speed | AttackPower | RewardGold | RewardExp
        // ==========================================
        private bool ParseMonsterData(string id, string[] cols)
        {
            if (cols.Length < 7) return false;

            string folderPath = "Assets/Data/ScriptableObject/Monsters";
            EnsureFolderExists(folderPath);

            string assetPath = $"{folderPath}/{id}.asset";
            WIMonsterDataSO asset = AssetDatabase.LoadAssetAtPath<WIMonsterDataSO>(assetPath);

            bool isNew = false;
            if (asset == null)
            {
                asset = CreateInstance<WIMonsterDataSO>();
                isNew = true;
            }

            asset.id = id;
            asset.monsterName = cols[1];
            if (float.TryParse(cols[2], out float hp)) asset.maxHp = hp;
            if (float.TryParse(cols[3], out float spd)) asset.speed = spd;
            if (float.TryParse(cols[4], out float atk)) asset.attackPower = atk;
            if (int.TryParse(cols[5], out int g)) asset.killRewardGold = g;
            if (int.TryParse(cols[6], out int e)) asset.killRewardExp = e;

            if (isNew) AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            return true;
        }

        // ==========================================
        // [4] 던전 데이터 파서
        // 엑셀 포맷: DungeonID | DungeonName (기타 List는 에디터에서 수동으로 세팅 권장)
        // ==========================================
        private bool ParseDungeonData(string id, string[] cols)
        {
            if (cols.Length < 2) return false;

            string folderPath = "Assets/Data/ScriptableObject/Dungeons";
            EnsureFolderExists(folderPath);

            string assetPath = $"{folderPath}/{id}.asset";
            WIDungeonDataSO asset = AssetDatabase.LoadAssetAtPath<WIDungeonDataSO>(assetPath);

            bool isNew = false;
            if (asset == null)
            {
                asset = CreateInstance<WIDungeonDataSO>();
                isNew = true;
            }

            if (int.TryParse(id, out int dId)) asset.dungeonId = dId;
            asset.dungeonName = cols[1];

            if (isNew) AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            return true;
        }

        // ==========================================
        // [5] 스킬 데이터 파서
        // 엑셀 포맷: ID | SkillName | Description | Cooldown | DamageMultiplier | ManaCost
        // ==========================================
        private bool ParseSkillData(string id, string[] cols)
        {
            if (cols.Length < 6) return false;

            string folderPath = "Assets/Data/ScriptableObject/Skills";
            EnsureFolderExists(folderPath);

            string assetPath = $"{folderPath}/{id}.asset";
            WISkillDataSO asset = AssetDatabase.LoadAssetAtPath<WISkillDataSO>(assetPath);

            bool isNew = false;
            if (asset == null)
            {
                asset = CreateInstance<WISkillDataSO>();
                isNew = true;
            }

            asset.id = id;
            asset.skillName = cols[1];
            asset.description = cols[2];
            if (float.TryParse(cols[3], out float cd)) asset.cooldown = cd;
            if (float.TryParse(cols[4], out float dm)) asset.damageMultiplier = dm;
            if (int.TryParse(cols[5], out int mc)) asset.manaCost = mc;

            if (isNew) AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            return true;
        }

        // ==========================================
        // 유틸리티 함수
        // ==========================================

        /// <summary>
        /// CSV의 경우 큰따옴표("") 안의 쉼표(,)는 무시하고 분할하는 커스텀 파서
        /// </summary>
        private string[] SplitCsvLine(string line)
        {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            string current = "";
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && inQuotes == false)
                {
                    result.Add(current.Trim('\"').Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current.Trim('\"').Trim());
            return result.ToArray();
        }

        /// <summary>
        /// CSV 내보낼 때 쉼표나 개행문자가 포함되어 있으면 큰따옴표로 감싸는 유틸리티
        /// </summary>
        private string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n") || text.Contains("\r"))
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            return text;
        }

        private void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string currPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string parent = currPath;
                    currPath += "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(currPath))
                    {
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    }
                }
            }
        }
    }
}
#endif
