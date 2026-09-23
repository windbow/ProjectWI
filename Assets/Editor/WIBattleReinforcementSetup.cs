using ProjectWI.Battle;
using UnityEditor;

namespace ProjectWI.EditorTools
{
    public static class WIBattleReinforcementSetup
    {
        // 전투 설정 에셋에 증원 시간 기준을 저장해 실행 전에 편집할 수 있게 합니다.
        [MenuItem("WI/Data/Apply Battle Reinforcement Timing")]
        public static void Apply()
        {
            var config = AssetDatabase.LoadAssetAtPath<WIBattleConfigSO>("Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset");
            var serialized = new SerializedObject(config);
            serialized.FindProperty("reinforcementSecondsPerMonth").floatValue = 15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(config);
            WIActivityCopyUtility.Apply();
        }
    }
}
