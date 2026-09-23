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
                {"UI_SELECTION_SEARCH","이름·정보 검색","Search name or details"},
                {"UI_SELECTION_COMMON","일반","Common"},
                {"UI_SELECTION_HERO","영웅","Hero"},
                {"UI_SELECTION_IN_ARMY","편성 불가 · 전투단 소속","Unavailable · already in an army"},
                {"UI_SELECTION_BUSY","편성 불가 · 다른 임무 진행 중","Unavailable · assigned to another task"},
                {"UI_SELECTION_READY","편성 가능","Available for assignment"},
                {"UI_SELECTION_FATIGUE","피로 {0}","Fatigue {0}"},
                {"UI_SELECTION_INJURY","부상 {0}개월","Injured {0} months"},
                {"UI_SELECTION_DUTY","내정: {0} · 전투 병행","Administration: {0} · combat compatible"},
                {"UI_ARMY_LIST_STATUS","선택 {0}명 / 추가 가능 {1}명","Selected {0} / Capacity {1}"},
                {"UI_MARCH_LIST_STATUS","선택 {0}개 · 총 영향력 {1}","Selected {0} · Total influence {1}"},
                {"UI_SELECTION_ALL","전체","All"},
                {"UI_SELECTION_AVAILABLE","선택 가능","Available"},
                {"UI_SELECTION_NAME","이름순","Name"},
                {"UI_SELECTION_DEFAULT","기본순","Default"},
                {"UI_SELECTION_COUNT","{0} / {1}명","{0} / {1}"},
                {"UI_ADMIN_BASIC_OPERATION","기본 내정","Basic administration"},
                {"UI_ADMIN_OPERATION_HELP","영지관은 출전 중에도 내정을 지휘합니다. Common은 전투 전용입니다.","Governors lead administration even while deployed. Common characters serve in combat only."},
                {"UI_COMMON_COMBAT_ONLY","전투 전용 · 주둔 중 자동 훈련","Combat only · automatic training while stationed"},
                {"UI_AUTOMATIC_TRAINING_HINT","훈련은 자동입니다. 과로·부상 시 회복하며 출정 명령을 우선합니다.","Training is automatic. Fatigued or injured characters recover; marching takes priority."},
                {"REPORT_AUTOMATIC_TRAINING","자동 훈련 · {0}명 성장 · {1}명 회복","Automatic training · {0} trained · {1} recovered"},
                {"UI_ADMIN_TRAIT_REQUIRED","내정 특성을 가진 영웅만 담당할 수 있습니다. Common은 전투 전용입니다.","Requires a Hero with Administration. Common characters serve in combat only."},
                {"UI_ADMIN_OPERATION_START","기본 운영 시작","Start administration"},
                {"UI_ADMIN_OPERATION_STOP","기본 운영 중지","Stop administration"},
                {"UI_ADMIN_GOVERNOR_OPTIONAL","영지관 선택 배치","Optional governor"},
                {"UI_ADMIN_OPERATION_STATUS","기본 운영 · {0} · 월 {1}G","Administration · {0} · {1}G/month"},
                {"UI_ADMIN_OPERATION_PREVIEW","예상 계획 · {0} · {1} · 성과 +{2} · 비용 {3}G · 1개월 · {4}","Plan · {0} · {1} · gain +{2} · cost {3}G · 1 month · {4}"},
                {"UI_ADMIN_OCCUPATION_PROGRESS","점령 안정화 · 남은 {0}개월 · 월 질서 +{1} · 인원 잔류 불필요","Stabilization · {0} months left · order +{1}/month · no personnel required"},
                {"UI_BATTLE_REINFORCEMENT_PENDING","증원 예정 · {0} · {1:0}초 후 합류","Reinforcements · {0} · arriving in {1:0}s"},
                {"UI_BATTLE_REINFORCEMENT_ARRIVED","증원 합류 · {0}","Reinforcements arrived · {0}"},
                {"UI_GROUP_MARCH_TITLE","출정 전투단 선택","Select Marching Armies"},
                {"UI_GROUP_MARCH_ROUTE","{0} → {1} · 함께 출정할 전투단을 선택하세요.","{0} → {1} · Select armies to march together."},
                {"UI_GROUP_MARCH_STATUS","선택 {0}개 · 총 영향력 {1} · {2}/{3}쪽","Selected {0} · Total influence {1} · Page {2}/{3}"},
                {"UI_GROUP_MARCH_CONFIRM","선택한 {0}개 전투단 출정","March with {0} selected armies"},
                {"UI_GROUP_MARCH_ARMY","인원 {0}/{1}명 · 전력 {2}","Members {0}/{1} · Power {2}"},
                {"UI_GROUP_MARCH_INVALID","출정할 수 없습니다. 같은 성의 대기 전투단, 목표와의 교전 상태, 총 영향력을 확인하세요.","Cannot march. Check that armies are ready in the same castle, the target is at war, and total influence is sufficient."},
                {"UI_TAVERN_DIRECT_TITLE","선술집 · 인재실과 의뢰","Tavern · Talent Office and Quests"},
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
            // 업무 특성은 영웅 등급에서만 사용할 수 있도록 기존 설명도 맞춥니다.
            var traits = serialized.FindProperty("traitDefinitions");
            for (int index = 0; index < traits.arraySize; index++)
            {
                var trait = traits.GetArrayElementAtIndex(index);
                var type = (WITraitType)trait.FindPropertyRelative("traitType").intValue;
                if (type == WITraitType.Administration)
                {
                    var description = trait.FindPropertyRelative("description");
                    description.FindPropertyRelative("korean").stringValue = "영웅 등급에서 영지관과 성 중점 사업을 담당합니다. 출정 중에도 업무를 유지합니다.";
                    description.FindPropertyRelative("english").stringValue = "Heroes can govern and manage castle projects, including while deployed.";
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
