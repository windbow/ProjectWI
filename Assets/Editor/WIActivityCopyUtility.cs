using UnityEditor;
using ProjectWI.Administration;
namespace ProjectWI.EditorTools
{
    public static class WIActivityCopyUtility
    {
        // 기존 데이터베이스의 배치·활동 안내 문자열만 지정 UID로 저장합니다.
        [MenuItem("WI/UI/Clarify Activity Labels")]
        public static void Apply()
        {
            var asset=AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>("Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            var serialized=new SerializedObject(asset);
            var list=serialized.FindProperty("uiStrings");
            string[,] entries={
                {"UI_ARMY_MEMBER_TITLE","전투단원 선택","Select Members"},
                {"UI_ARMY_MULTI_HINT","인물을 여러 명 선택한 뒤 한 번에 편성합니다.","Select multiple characters and confirm."},
                {"UI_ARMY_MULTI_STATUS","선택 {0}명 / 추가 가능 {1}명 · {2}/{3}쪽","Selected {0} / Available {1} · Page {2}/{3}"},
                {"UI_ARMY_MULTI_CONFIRM","선택한 {0}명 편성","Assign {0} selected"},
                {"UI_ARMY_BATCH_INVALID","선택 인원이나 활동 상태가 변경되었습니다. 편성 가능 인원을 다시 확인하세요.","Selection or availability changed. Check capacity and retry."},
                {"UI_ASSIGN_RESIDENT_TITLE","미배치 인물 배치","Assign Unplaced Characters"},
                {"UI_ASSIGN_RESIDENT_HINT","어느 성에도 배치되지 않은 인물을 이 성에 배치합니다. 다른 성의 인물은 개인 활동의 이동을 이용하세요.","Assign characters without a castle here. Use personal activity transfer for characters in another castle."},
                {"UI_ACTIVITY_ACTOR_TITLE","{0} · 활동 선택","{0} · Choose Activity"},
                {"UI_ACTIVITY_SOCIAL_TARGET","{0} · 교류할 상대 선택","{0} · Choose Social Partner"},
                {"UI_ACTIVITY_RECRUIT_TARGET","{0} · 영입할 인재 선택","{0} · Choose Recruitment Target"},
                {"UI_ACTIVITY_TARGET_HINT","담당자는 {0}입니다. 활동을 함께할 상대를 선택하세요.","{0} is the assigned character. Choose the other participant."}
            };
            for(int row=0;row<entries.GetLength(0);row++)
            {
                SerializedProperty item=null;
                for(int i=0;i<list.arraySize;i++)
                {
                    var candidate=list.GetArrayElementAtIndex(i);
                    if(candidate.FindPropertyRelative("uid").stringValue==entries[row,0])
                    {
                        item=candidate;
                        break;
                    }
                }
                if(item==null)
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    item=list.GetArrayElementAtIndex(list.arraySize-1);
                }
                item.FindPropertyRelative("uid").stringValue=entries[row,0];
                item.FindPropertyRelative("korean").stringValue=entries[row,1];
                item.FindPropertyRelative("english").stringValue=entries[row,2];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}