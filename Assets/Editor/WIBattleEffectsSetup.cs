using ProjectWI.Battle;
using UnityEditor;
using UnityEngine;

namespace ProjectWI.EditorTools
{
    public static class WIBattleEffectsSetup
    {
        // 제작된 네 프리팹을 전투 설정에 연결하고 기존 밸런스 값은 유지합니다.
        [MenuItem("WI/Effects/Connect Temporary Battle Effects")]
        public static void Connect()
        {
            var config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            var serialized = new SerializedObject(config);
            string[] fields = { "slashEffectPrefab", "arrowEffectPrefab", "magicEffectPrefab", "hitEffectPrefab" };
            string[] names = { "Slash", "Arrow", "Magic", "Hit" };
            for (int i = 0; i < fields.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Battle/Effects/Temporary/WI_Temp_" + names[i] + ".prefab");
                if (prefab == null || prefab.GetComponent<ParticleSystem>() == null)
                {
                    throw new System.InvalidOperationException("전투 이펙트 프리팹 누락: " + names[i]);
                }
                serialized.FindProperty(fields[i]).objectReferenceValue = prefab;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("검격·화살·마법·피격 이펙트를 BattleConfig에 연결했습니다.");
        }
    }
}
