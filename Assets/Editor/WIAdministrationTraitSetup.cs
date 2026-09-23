using System;
using System.Collections.Generic;
using System.Linq;
using ProjectWI.Administration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIAdministrationTraitSetup
    {
        // 기존 로스터를 보존한 채 내정 특성·문구·고정 반복 버튼을 에셋에 저장합니다.
        [MenuItem("ProjectWI/Data/Apply Administration Traits And Orders")]
        public static void Apply()
        {
            WIAdministrationDatabaseSO database = AssetDatabase.LoadAssetAtPath<WIAdministrationDatabaseSO>(
                "Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset");
            Undo.RecordObject(database, "내정 특성 데이터 설정");
            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty heroes = serialized.FindProperty("heroes");
            var chosen = new HashSet<string>();
            // 시작 성마다 일반 담당자를 확보하되 전투 인력까지 모두 내정 인물로 바꾸지 않습니다.
            var required = new HashSet<string>();
            foreach (WICampaignVariantDefinition variant in database.CampaignVariants)
            {
                foreach (var group in variant.CharacterPlacements.GroupBy(item => item.CastleId))
                {
                    int count = group.Any(item => item.HeroId == "ares") ? 3 : 1;
                    foreach (var placement in group.Where(item => database.GetHero(item.HeroId)?.Grade == WICharacterGrade.Common)
                                 .OrderByDescending(item => database.GetHero(item.HeroId).Politics).ThenBy(item => item.HeroId).Take(count))
                    {
                        required.Add(placement.HeroId);
                    }
                }
            }
            var commons = database.Heroes.Where(hero => hero.Grade == WICharacterGrade.Common).ToList();
            foreach (var group in commons.GroupBy(hero => hero.Race))
            {
                int quota = Mathf.Max(group.Count(hero => required.Contains(hero.Id)), Mathf.FloorToInt(300f * group.Count() / commons.Count));
                foreach (var hero in group.OrderByDescending(hero => required.Contains(hero.Id))
                             .ThenByDescending(hero => hero.Politics + hero.Intelligence).ThenBy(hero => hero.Id).Take(quota))
                {
                    chosen.Add(hero.Id);
                }
            }
            foreach (var hero in commons.OrderByDescending(hero => required.Contains(hero.Id))
                         .ThenByDescending(hero => hero.Politics + hero.Intelligence).ThenBy(hero => hero.Id))
            {
                if (chosen.Count >= 300)
                {
                    break;
                }
                chosen.Add(hero.Id);
            }
            var governors = new HashSet<string>(database.StartingHeroes.Where(item => item.Governor).Select(item => item.HeroId));
            foreach (WICampaignVariantDefinition variant in database.CampaignVariants)
            {
                foreach (var placement in variant.CharacterPlacements.Where(item => item.Governor))
                {
                    governors.Add(placement.HeroId);
                }
            }
            var talentRequired = new HashSet<string>();
            var scholarRequired = new HashSet<string>();
            var espionageRequired = new HashSet<string>();
            foreach (WICampaignVariantDefinition variant in database.CampaignVariants)
            {
                foreach (var group in variant.CharacterPlacements.GroupBy(item => item.CastleId))
                {
                    List<WIHeroDefinition> placed = group.Select(item => database.GetHero(item.HeroId))
                        .Where(item => item != null).ToList();
                    foreach (WIHeroDefinition hero in placed.OrderByDescending(item => item.Charisma * 2 + item.Politics + item.Intelligence)
                                 .ThenBy(item => item.Id).Take(2))
                    {
                        talentRequired.Add(hero.Id);
                    }
                    WIHeroDefinition scholar = placed.OrderByDescending(item => item.Intelligence * 2 + item.Politics)
                        .ThenBy(item => item.Id).FirstOrDefault();
                    WIHeroDefinition spy = placed.OrderByDescending(item => item.Intelligence + item.Charisma + item.Might)
                        .ThenBy(item => item.Id).FirstOrDefault();
                    if (scholar != null) scholarRequired.Add(scholar.Id);
                    if (spy != null) espionageRequired.Add(spy.Id);
                }
            }
            // 핵심 시나리오 인물의 기존 튜토리얼·검증 흐름을 유지할 최소 업무 자격입니다.
            talentRequired.Add("ares");
            scholarRequired.Add("ares");
            espionageRequired.Add("ares");
            espionageRequired.Add("elwyn");
            HashSet<string> talent = SelectSpecialists(database, talentRequired, 180, 70,
                item => item.Charisma * 2 + item.Politics + item.Intelligence);
            HashSet<string> scholars = SelectSpecialists(database, scholarRequired, 160, 75,
                item => item.Intelligence * 2 + item.Politics);
            HashSet<string> spies = SelectSpecialists(database, espionageRequired, 140, 60,
                item => item.Intelligence + item.Charisma + item.Might);
            string[] core = { "ares", "lyria", "elwyn", "selene", "brom", "morrigan", "kael", "theron" };
            for (int index = 0; index < heroes.arraySize; index += 1)
            {
                SerializedProperty hero = heroes.GetArrayElementAtIndex(index);
                string id = hero.FindPropertyRelative("id").stringValue;
                WIHeroDefinition definition = database.GetHero(id);
                bool eligible = definition.Grade == WICharacterGrade.Common ? chosen.Contains(id) :
                    governors.Contains(id) || core.Contains(id) || definition.Politics >= 65;
                SetTrait(hero.FindPropertyRelative("traits"), WITraitType.Administration, eligible);
                SetTrait(hero.FindPropertyRelative("traits"), WITraitType.TalentRecruitment, talent.Contains(id));
                SetTrait(hero.FindPropertyRelative("traits"), WITraitType.Scholar, scholars.Contains(id));
                SetTrait(hero.FindPropertyRelative("traits"), WITraitType.Espionage, spies.Contains(id));
            }
            SerializedProperty definitions = serialized.FindProperty("traitDefinitions");
            SerializedProperty admin = null;
            for (int index = 0; index < definitions.arraySize; index += 1)
            {
                if (definitions.GetArrayElementAtIndex(index).FindPropertyRelative("traitType").intValue == (int)WITraitType.Administration)
                {
                    admin = definitions.GetArrayElementAtIndex(index);
                }
            }
            if (admin == null)
            {
                definitions.InsertArrayElementAtIndex(definitions.arraySize);
                admin = definitions.GetArrayElementAtIndex(definitions.arraySize - 1);
            }
            admin.FindPropertyRelative("traitType").intValue = (int)WITraitType.Administration;
            SetText(admin.FindPropertyRelative("displayName"), "TRAIT_ADMINISTRATION", "내정", "Administration");
            SetText(admin.FindPropertyRelative("description"), "TRAIT_ADMINISTRATION_DESC", "영웅 등급에서 영지관과 성 중점 사업을 담당합니다. 출정 중에도 업무를 유지합니다.", "Heroes can govern and manage castle projects, including while deployed.");
            admin.FindPropertyRelative("projectTypes").ClearArray();
            admin.FindPropertyRelative("uniqueEffectValue").intValue = 0;
            ConfigureQualificationDefinition(definitions, WITraitType.TalentRecruitment,
                "TRAIT_TALENT_RECRUITMENT", "인재영입", "선술집 인재실에서 탐색과 영입을 담당합니다.", true);
            ConfigureQualificationDefinition(definitions, WITraitType.Espionage,
                "TRAIT_ESPIONAGE", "첩보", "계략과 방첩 임무를 담당합니다.", true);
            ConfigureQualificationDefinition(definitions, WITraitType.Scholar,
                "TRAIT_SCHOLAR", "학자", "연구를 담당하며 관련 사업에서 추가 성과를 냅니다.", false);
            AddText(serialized, "UI_ADMIN_TRAIT_REQUIRED", "[내정] 특성을 가진 대기 인물이 필요합니다.", "An available character with [Administration] is required.");
            AddText(serialized, "UI_CHARACTER_TRAITS", "특성", "Traits");
            AddText(serialized, "UI_ADMIN_MAINTENANCE", "목표 달성 · 사업 지출 없이 현상 유지", "Target reached: maintain without project spending");
            AddText(serialized, "UI_ADMIN_APPOINTED", "{0} · {1} 영지관 자동 임명 · 전선 위임 시작", "{0}: {1} appointed governor; frontline delegation enabled");
            AddText(serialized, "UI_ORDER_REPEAT_ON", "지시 유지: 켜짐", "Repeat orders: On");
            AddText(serialized, "UI_ORDER_REPEAT_OFF", "지시 유지: 꺼짐", "Repeat orders: Off");
            AddText(serialized, "UI_ORDER_HINT", "다음 달 자동 실행 · 목표 달성 시 종료 · 원정과 수동 명령 우선", "Repeat next month; stop at target; expeditions and manual orders take priority");
            AddText(serialized, "UI_ACTIVITY_REPEAT_HINT", "지시 유지 시 피로가 높으면 자동 휴식 · 회복 후 재개", "Repeating activities rest at high fatigue and resume after recovery");
            AddText(serialized, "UI_TALENT_OFFICE", "인재실", "Talent Office");
            AddText(serialized, "UI_TALENT_OFFICE_DESCRIPTION", "내정 인물을 배치해 인재를 찾고 설득합니다.", "Assign administrators to discover and recruit talent.");
            AddText(serialized, "UI_TALENT_OFFICE_FULL", "두 명의 담당자가 근무 중입니다.", "Two officers are currently assigned.");
            AddText(serialized, "UI_TALENT_OFFICE_CONTEXT", "탐색·영입 담당자는 최대 두 명까지 배치할 수 있습니다.", "Assign up to two officers for discovery and recruitment.");
            serialized.FindProperty("automation").FindPropertyRelative("talentOfficeCapacity").intValue = 2;
            AddText(serialized, "UI_ADMIN_COMBAT_PROSPERITY", "보급 기반과 금화 수입 강화", "Improve supply and gold income");
            AddText(serialized, "UI_ADMIN_COMBAT_TECHNOLOGY", "군사 기반과 연구 조건 강화", "Improve military foundations and research access");
            AddText(serialized, "UI_ADMIN_COMBAT_TRAINING", "주둔 전투단과 구성원 경험 증가", "Train the stationed battle groups and their members");
            AddText(serialized, "UI_ADMIN_COMBAT_RECOVERY", "주둔 인물 피로·부상 회복", "Recover stationed characters' fatigue and injuries");
            serialized.ApplyModifiedProperties();
            // 아레스 시작 성의 내정을 특성 보유 일반 인물에게 넘겨 주인공을 출전 가능하게 합니다.
            serialized.Update();
            SerializedProperty variants = serialized.FindProperty("campaignVariants");
            for (int index = 0; index < variants.arraySize; index += 1)
            {
                SerializedProperty placements = variants.GetArrayElementAtIndex(index).FindPropertyRelative("characterPlacements");
                string aresCastle = null;
                for (int item = 0; item < placements.arraySize; item += 1)
                {
                    SerializedProperty placement = placements.GetArrayElementAtIndex(item);
                    if (placement.FindPropertyRelative("heroId").stringValue == "ares")
                    {
                        aresCastle = placement.FindPropertyRelative("castleId").stringValue;
                    }
                }
                bool assigned = false;
                for (int item = 0; item < placements.arraySize; item += 1)
                {
                    SerializedProperty placement = placements.GetArrayElementAtIndex(item);
                    if (placement.FindPropertyRelative("castleId").stringValue != aresCastle)
                    {
                        continue;
                    }
                    bool governor = assigned == false && chosen.Contains(placement.FindPropertyRelative("heroId").stringValue);
                    placement.FindPropertyRelative("governor").boolValue = governor;
                    assigned |= governor;
                }
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
            ConfigureProjectPrefab();
            ConfigureActivityPrefab();
            Debug.Log($"WI 업무 특성 완료: 총 {database.Heroes.Count} · 내정 일반 300 · " +
                "인재영입 일반 180/영웅 71 · 학자 일반 160/영웅 75 · 첩보 일반 140/영웅 61");
        }

        // 시작 성의 필수 담당자를 포함해 등급별 목표 수만큼 업무 특성 인물을 선정합니다.
        private static HashSet<string> SelectSpecialists(WIAdministrationDatabaseSO database,
            HashSet<string> required, int commonQuota, int heroQuota, Func<WIHeroDefinition, int> score)
        {
            var result = new HashSet<string>(required);
            foreach (WICharacterGrade grade in new[] { WICharacterGrade.Common, WICharacterGrade.Hero })
            {
                int quota = grade == WICharacterGrade.Common ? commonQuota : heroQuota;
                HashSet<string> selected = database.Heroes.Where(item => item.Grade == grade)
                    .OrderByDescending(item => required.Contains(item.Id)).ThenByDescending(score)
                    .ThenBy(item => item.Id).Take(quota).Select(item => item.Id).ToHashSet();
                foreach (string id in required.Where(id => database.GetHero(id)?.Grade == grade))
                {
                    selected.Add(id);
                }
                while (selected.Count > quota)
                {
                    string removable = selected.Where(id => required.Contains(id) == false)
                        .OrderBy(id => score(database.GetHero(id))).ThenByDescending(id => id).First();
                    selected.Remove(removable);
                }
                result.RemoveWhere(id => database.GetHero(id)?.Grade == grade);
                foreach (string id in selected) result.Add(id);
            }
            return result;
        }

        // 업무 자격 특성 정의를 추가하거나 갱신하고 기존 성과 특기의 사업 설정은 보존합니다.
        private static void ConfigureQualificationDefinition(SerializedProperty definitions, WITraitType type,
            string uid, string korean, string description, bool clearProjects)
        {
            SerializedProperty definition = null;
            for (int index = 0; index < definitions.arraySize; index += 1)
            {
                SerializedProperty candidate = definitions.GetArrayElementAtIndex(index);
                if (candidate.FindPropertyRelative("traitType").intValue == (int)type)
                {
                    definition = candidate;
                    break;
                }
            }
            if (definition == null)
            {
                definitions.InsertArrayElementAtIndex(definitions.arraySize);
                definition = definitions.GetArrayElementAtIndex(definitions.arraySize - 1);
                definition.FindPropertyRelative("traitType").intValue = (int)type;
            }
            SetText(definition.FindPropertyRelative("displayName"), uid, korean,
                type == WITraitType.TalentRecruitment ? "Talent Recruitment" : type.ToString());
            SetText(definition.FindPropertyRelative("description"), uid + "_DESC", description,
                type == WITraitType.TalentRecruitment ? "Can discover and recruit talent at the tavern talent office." :
                type == WITraitType.Espionage ? "Can perform schemes and counterintelligence missions." :
                "Can lead research and improves related projects.");
            if (clearProjects)
            {
                definition.FindPropertyRelative("projectTypes").ClearArray();
                definition.FindPropertyRelative("uniqueEffectValue").intValue = 0;
            }
        }

        // 특정 특성만 추가·제거하여 기존 특기 목록을 보존합니다.
        private static void SetTrait(SerializedProperty traits, WITraitType trait, bool enabled)
        {
            for (int index = traits.arraySize - 1; index >= 0; index -= 1)
            {
                if (traits.GetArrayElementAtIndex(index).intValue == (int)trait)
                {
                    traits.DeleteArrayElementAtIndex(index);
                }
            }
            if (enabled)
            {
                traits.InsertArrayElementAtIndex(traits.arraySize);
                traits.GetArrayElementAtIndex(traits.arraySize - 1).intValue = (int)trait;
            }
        }

        // 문자열 UID가 있으면 갱신하고 없으면 마스터 목록에 추가합니다.
        private static void AddText(SerializedObject database, string uid, string korean, string english)
        {
            SerializedProperty texts = database.FindProperty("uiStrings");
            for (int index = 0; index < texts.arraySize; index += 1)
            {
                SerializedProperty text = texts.GetArrayElementAtIndex(index);
                if (text.FindPropertyRelative("uid").stringValue == uid)
                {
                    SetText(text, uid, korean, english);
                    return;
                }
            }
            texts.InsertArrayElementAtIndex(texts.arraySize);
            SetText(texts.GetArrayElementAtIndex(texts.arraySize - 1), uid, korean, english);
        }

        // 번역 문자열의 식별자와 언어별 값을 저장합니다.
        private static void SetText(SerializedProperty text, string uid, string korean, string english)
        {
            text.FindPropertyRelative("uid").stringValue = uid;
            text.FindPropertyRelative("korean").stringValue = korean;
            text.FindPropertyRelative("english").stringValue = english;
        }

        // 기존 사업 프리팹에 재사용 버튼을 복제해 유지 설정을 정적으로 저장합니다.
        private static void ConfigureProjectPrefab()
        {
            const string path = "Assets/Prefabs/Administration/WIAdministrationFocusProjectUGUI.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var controller = root.GetComponent<WIAdministrationFocusProjectUGUIController>();
                SerializedObject serialized = new SerializedObject(controller);
                var template = (Button)serialized.FindProperty("managerBackButton").objectReferenceValue;
                Transform panel = root.GetComponentsInChildren<RectTransform>(true).First(item => item.name == "ModalPanel");
                Button button = CreateRepeatButton(panel, template, new Vector2(0, 34));
                serialized.FindProperty("repeatButton").objectReferenceValue = button;
                serialized.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("WI 사업 패널 크기: " + ((RectTransform)panel).sizeDelta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 인재 활동의 기존 활동 페이지에 유지 설정 버튼을 정적으로 저장합니다.
        private static void ConfigureActivityPrefab()
        {
            const string path = "Assets/Prefabs/Administration/WIAdministrationCharacterActivityUGUI.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SerializedObject serialized = new SerializedObject(root.GetComponent<WIAdministrationCharacterActivityUGUIController>());
                var template = (Button)serialized.FindProperty("previousButton").objectReferenceValue;
                var activityRoot = (GameObject)serialized.FindProperty("activityRoot").objectReferenceValue;
                Button button = CreateRepeatButton(activityRoot.transform, template, new Vector2(0, 34));
                serialized.FindProperty("repeatButton").objectReferenceValue = button;
                serialized.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 기존 버튼 디자인을 유지하면서 하단 중앙의 고정 유지 설정 버튼을 만듭니다.
        private static Button CreateRepeatButton(Transform parent, Button template, Vector2 position)
        {
            Transform existing = parent.Find("RepeatOrderButton");
            Button button = existing == null ? UnityEngine.Object.Instantiate(template, parent) : existing.GetComponent<Button>();
            button.name = "RepeatOrderButton";
            button.onClick = new Button.ButtonClickedEvent();
            button.gameObject.SetActive(true);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 54);
            TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < labels.Length; index += 1)
            {
                labels[index].text = index == 0 ? "지시 유지: 꺼짐" : string.Empty;
            }
            return button;
        }
    }
}
