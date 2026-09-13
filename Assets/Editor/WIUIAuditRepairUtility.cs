using System.IO;
using System.Text;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWI.EditorTools
{
    public static class WIUIAuditRepairUtility
    {
        private const string Generated = "Assets/Resources/UI/Generated/";

        // QA 중 현재 맨 위의 기존 모달을 닫아 다음 화면 점검을 준비합니다.
        [MenuItem("WI/UI/Audit/Close Topmost")]
        public static void CloseTopmost() => ProjectWI.Administration.WIAdministrationModalUGUIController.TryHideTopmost();

        // 현재 테스트 세션의 자동 저장을 끄고 실제 턴 결과 UI를 점검합니다.
        [MenuItem("WI/UI/Audit/Advance Without Saving")]
        public static void AdvanceWithoutSaving()
        {
            if (Application.isPlaying == false)
            {
                return;
            }
            var service = ProjectWI.Systems.WICampaignRuntimeService.Instance;
            SerializedObject serialized = new SerializedObject(service);
            serialized.FindProperty("autoSaveEnabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEngine.Object.FindFirstObjectByType<ProjectWI.Administration.WIAdministrationUIController>().AdvanceTurnUGUIForQA();
        }

        // 점검한 월간 보고 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Monthly Report")]
        public static void RepairMonthlyReport() => RepairPrefab("WIAdministrationMonthlyReportUGUI");

        // 점검한 인사 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Heroes")]
        public static void RepairHeroes() => RepairPrefab("WIAdministrationHeroesUGUI");

        // 점검한 개인 활동 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Activity")]
        public static void RepairActivity() => RepairPrefab("WIAdministrationCharacterActivityUGUI");

        // 점검한 원정 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair March")]
        public static void RepairMarch() => RepairPrefab("WIAdministrationMarchUGUI");
        // 점검한 외교 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Diplomacy")]
        public static void RepairDiplomacy() => RepairPrefab("WIAdministrationDiplomacyUGUI");
        // 점검한 연구 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Research")]
        public static void RepairResearch() => RepairPrefab("WIAdministrationResearchUGUI");
        // 점검한 계략 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Scheme")]
        public static void RepairScheme() => RepairPrefab("WIAdministrationSchemeUGUI");
        // 점검한 세력 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Faction")]
        public static void RepairFaction() => RepairPrefab("WIAdministrationFactionUGUI");
        // 점검한 의회 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Council")]
        public static void RepairCouncil() => RepairPrefab("WIAdministrationCouncilUGUI");
        // 점검한 설정 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair System")]
        public static void RepairSystem() => RepairPrefab("WIAdministrationSystemUGUI");
        // 점검한 군사 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Military")]
        public static void RepairMilitary() => RepairPrefab("WIAdministrationMilitaryUGUI");
        // 점검한 사건 선택 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Event")]
        public static void RepairEvent() => RepairPrefab("WIAdministrationEventChoiceUGUI");
        // 점검한 성 내정 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Territory")]
        public static void RepairTerritory() => RepairPrefab("WIAdministrationTerritoryUGUI");
        // 점검한 첫해 안내 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Tutorial")]
        public static void RepairTutorial() => RepairPrefab("WIAdministrationTurnFollowupUGUI");
        // 점검한 기본 시설 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Basic Facility")]
        public static void RepairBasicFacility() => RepairPrefab("WIAdministrationBasicFacilityUGUI");
        // 점검한 특화 시설 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Special Facility")]
        public static void RepairSpecialFacility() => RepairPrefab("WIAdministrationSpecialFacilityUGUI");
        // 점검한 인사 배치 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Assignment")]
        public static void RepairAssignment() => RepairPrefab("WIAdministrationHeroAssignmentUGUI");
        // 점검한 성 상세 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Record")]
        public static void RepairRecord() => RepairPrefab("WIAdministrationCastleRecordUGUI");
        // 점검한 중점 사업 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Focus")]
        public static void RepairFocus() => RepairPrefab("WIAdministrationFocusProjectUGUI");
        // 점검한 위임 화면 한 개를 백업 후 보정합니다.
        [MenuItem("WI/UI/Audit/Repair Delegation")]
        public static void RepairDelegation() => RepairPrefab("WIAdministrationDelegationUGUI");

        // 명시된 프리팹 하나만 백업하고 보정하며 다른 에셋은 저장하지 않습니다.
        private static void RepairPrefab(string name)
        {
            string path = "Assets/Prefabs/Administration/" + name + ".prefab";
            Directory.CreateDirectory("Temp/UIRepair/Backups");
            string backup = "Temp/UIRepair/Backups/" + name + ".prefab";
            if (File.Exists(backup) == false)
            {
                File.Copy(path, backup);
            }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RepairCommon(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("프리팹 한 개 보정 완료: " + path + " / 원본: " + backup);
        }

        // 이름과 기존 구성으로 확인한 공통 모달 및 개별 화면을 보정합니다.
        internal static void RepairCommon(GameObject root)
        {
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            RectTransform panel = rects.FirstOrDefault(item => item.name == "ModalPanel");
            bool monthly = root.name.Contains("MonthlyReport");
            bool tutorial = root.name.Contains("TurnFollowup");
            bool objective = root.name.Contains("Objective");
            bool activity = root.name.Contains("CharacterActivity");
            if (panel != null && monthly == false && tutorial == false && objective == false)
            {
                Center(panel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1720f, 940f));
                SetImage(panel.GetComponent<Image>(), "administration_modal_shell_v1.png");
            }
            foreach (RectTransform rect in rects)
            {
                Image image = rect.GetComponent<Image>();
                TMP_Text text = rect.GetComponent<TMP_Text>();
                if (image != null && (rect.name == "Header" || rect.name == "HeaderPanel"))
                {
                    image.sprite = null;
                    image.color = Color.clear;
                    image.raycastTarget = false;
                    image.enabled = false;
                    SerializedObject serializedImage = new SerializedObject(image);
                    serializedImage.FindProperty("m_Sprite").objectReferenceValue = null;
                    serializedImage.ApplyModifiedPropertiesWithoutUndo();
                }
                if (rect.name == "Header" && monthly == false && objective == false)
                {
                    Center(rect, new Vector2(.5f, 1f), new Vector2(0, -74), new Vector2(1580, 76));
                }
                if (rect.name == "CloseButton" && panel != null && objective == false && tutorial == false)
                {
                    RectTransform header = rects.First(item => item.name == "Header");
                    rect.SetParent(header, false);
                    Center(rect, new Vector2(1f, .5f), new Vector2(monthly ? 285 : -35, 0), new Vector2(60, 60));
                    SetImage(image, "turn_followup_close_button_v1.png");
                    foreach (TMP_Text label in rect.GetComponentsInChildren<TMP_Text>(true))
                    {
                        label.text = string.Empty;
                    }
                }
                if (text != null && rect.parent != null && rect.parent.name == "Header")
                {
                    Anchors(rect, new Vector2(.04f, .1f), new Vector2(.88f, .9f));
                    text.color = new Color32(235, 240, 244, 255);
                    text.fontSize = 30;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                }
                if (panel != null && monthly == false && objective == false && tutorial == false &&
                    (rect.name == "SlotStatus" || rect.name == "StatusLabel" || rect.name == "ContextLabel"))
                {
                    Center(rect, new Vector2(.5f, 1f), new Vector2(0, -126), new Vector2(1480, 30));
                    if (text != null)
                    {
                        text.fontSize = 20;
                        text.textWrappingMode = TextWrappingModes.NoWrap;
                        text.overflowMode = TextOverflowModes.Ellipsis;
                    }
                }
                if (image != null && (rect.name.Contains("Portrait") || rect.name == "Thumbnail" || rect.name == "Picture"))
                {
                    image.preserveAspect = true;
                }
                if (root.name.Contains("Territory") && rect.name == "Command-6")
                {
                    SetImage(image, "strategy_command_button_v2.png");
                }
                if (root.name.Contains("Territory") && rect.name == "Caption" && rect.parent.name.StartsWith("HeroSlot"))
                {
                    Anchors(rect, new Vector2(.06f, .02f), new Vector2(.94f, .27f));
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 10;
                    text.fontSizeMax = 12;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                }
                if (rect.name == "CandidateCards" && monthly == false)
                {
                    bool delegation = root.name.Contains("Delegation");
                    Center(rect, new Vector2(.5f, .5f), new Vector2(0, activity ? -48 : -20), new Vector2(1510, activity ? 560 : 620));
                    if (delegation)
                    {
                        Center(rect, new Vector2(.5f, 1), new Vector2(0, -350), new Vector2(1510, 320));
                    }
                    int index = 0;
                    foreach (Transform child in rect)
                    {
                        Button button = child.GetComponent<Button>();
                        if (button == null)
                        {
                            continue;
                        }
                        RepairCard(button, index++, activity ? 132 : 146);
                        if (delegation)
                        {
                            int item = index - 1;
                            Center((RectTransform)button.transform, new Vector2(.5f, 1), new Vector2(-568 + item % 4 * 378, -73 - item / 4 * 156), new Vector2(362, 146));
                        }
                    }
                }
                if (rect.name == "PreviousButton" || rect.name == "NextButton" || rect.name == "PageLabel")
                {
                    if (monthly == false && panel != null)
                    {
                        float x = rect.name == "PreviousButton" ? -570 : rect.name == "NextButton" ? 570 : 0;
                        Center(rect, new Vector2(.5f, 0), new Vector2(x, 70), new Vector2(210, 52));
                        if (image != null)
                        {
                            SetImage(image, "bg_type_a.png");
                        }
                    }
                }
                if (activity && rect.name == "CandidateToolbar")
                {
                    Center(rect, new Vector2(.5f, 1), new Vector2(0, -206), new Vector2(1480, 60));
                }
                if (activity && rect.name == "ActivityRoot")
                {
                    Anchors(rect, new Vector2(.20f, .25f), new Vector2(.80f, .72f));
                }
                if (rect.name.Contains("Repeat") && activity)
                {
                    RectTransform activityRoot = rects.First(item => item.name == "ActivityRoot");
                    rect.SetParent(activityRoot, false);
                    Center(rect, new Vector2(.5f, 0), new Vector2(0, -75), new Vector2(350, 54));
                }
                if (tutorial && image != null && (rect.name == "DescriptionPanel" || rect.name == "ChecklistPanel" || rect.name == "MapCheck" || rect.name == "CommandCheck"))
                {
                    SetImage(image, "bg_type_a.png");
                }
                if (root.name.Contains("CastleRecord") && rect.name == "CastleImage")
                {
                    Anchors(rect, new Vector2(.06f, .40f), new Vector2(.43f, .77f));
                }
                if (root.name.Contains("CastleRecord") && rect.name == "RecordScroll")
                {
                    Anchors(rect, new Vector2(.47f, .14f), new Vector2(.94f, .77f));
                }
            }
            if (monthly)
            {
                foreach (RectTransform rect in rects)
                {
                    if (rect.anchorMin == rect.anchorMax)
                    {
                        rect.pivot = new Vector2(.5f, .5f);
                    }
                }
                TMP_Text decision = rects.First(item => item.name == "DecisionTitle").GetComponent<TMP_Text>();
                decision.text = "결정 필요";
                Center(decision.rectTransform, new Vector2(.5f, 1), new Vector2(440, -180), new Vector2(820, 42));
                Center(rects.First(item => item.name == "RightSection"), new Vector2(.5f, 1), new Vector2(440, -530), new Vector2(850, 650));
                TMP_Text title = rects.First(item => item.name == "Header").GetComponentInChildren<TMP_Text>();
                Center(title.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(540, 60));
                title.alignment = TextAlignmentOptions.Center;
                foreach (Image item in root.GetComponentsInChildren<Image>(true))
                {
                    if (item.name.StartsWith("Summary-") || item.name.StartsWith("Filter-") || item.name == "ConfirmButton" || item.name == "ActionVisual" || item.name == "PreviousButton" || item.name == "NextButton")
                    {
                        item.type = Image.Type.Simple;
                    }
                }
            }
            if (root.name.Contains("FocusProject"))
            {
                Center(rects.First(item => item.name == "RepeatOrderButton"), new Vector2(.5f, 0), new Vector2(0, 114), new Vector2(350, 48));
                RectTransform message = rects.First(item => item.name == "Message");
                Center(message, new Vector2(.5f, 0), new Vector2(0, 68), new Vector2(1480, 52));
                foreach (RectTransform page in rects.Where(item => item.name == "ProjectPage" || item.name == "ManagerPage"))
                {
                    Anchors(page, new Vector2(.05f, .18f), new Vector2(.95f, .79f));
                }
                int managerIndex = 0;
                foreach (RectTransform manager in rects.Where(item => item.name.StartsWith("Manager-") && item.GetComponent<Button>() != null))
                {
                    RepairCard(manager.GetComponent<Button>(), managerIndex++, 118);
                    manager.anchoredPosition += new Vector2(0, -64);
                }
            }
            if (root.name.Contains("CastleRecord"))
            {
                Center(panel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1720, 650));
                Anchors(rects.First(item => item.name == "CastleImage"), new Vector2(.06f, .18f), new Vector2(.43f, .68f));
                Anchors(rects.First(item => item.name == "RecordScroll"), new Vector2(.47f, .16f), new Vector2(.94f, .68f));
            }
        }

        // 기존 후보 카드를 두 열 네 행으로 정렬하고 초상과 본문 영역을 분리합니다.
        private static void RepairCard(Button button, int index, float height)
        {
            RectTransform rect = (RectTransform)button.transform;
            Center(rect, new Vector2(.5f, 1), new Vector2(index % 2 == 0 ? -382 : 382, -height / 2 - index / 2 * (height + 10)), new Vector2(742, height));
            SetImage(button.GetComponent<Image>(), "bg_type_a.png");
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                Anchors(label.rectTransform, new Vector2(.23f, .08f), new Vector2(.97f, .92f));
                label.color = new Color32(232, 238, 242, 255);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontSize = 21;
                label.enableAutoSizing = true;
                label.fontSizeMin = 17;
                label.fontSizeMax = 21;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
            foreach (Image image in button.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Portrait")
                {
                    Anchors(image.rectTransform, new Vector2(.025f, .07f), new Vector2(.21f, .93f));
                    image.preserveAspect = true;
                }
                if (image.name == "CharacterInfoDivider")
                {
                    image.gameObject.SetActive(false);
                }
            }
        }

        // 기존 Image에 준비된 단일 Sprite를 다시 연결합니다.
        private static void SetImage(Image image, string file)
        {
            if (image == null)
            {
                return;
            }
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Generated + file);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        // 중앙 기준으로 고정 크기의 기존 RectTransform을 배치합니다.
        private static void Center(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        // 부모에 대한 비율로 기존 RectTransform의 콘텐츠 영역을 배치합니다.
        private static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        // 저장된 UI 프리팹의 좌표와 이미지 연결을 읽기 전용으로 기록합니다.
        [MenuItem("WI/UI/Audit/Dump Layout")]
        public static void DumpLayout()
        {
            StringBuilder output = new StringBuilder();
            foreach (string path in Directory.GetFiles("Assets/Prefabs/Administration", "*.prefab"))
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                output.AppendLine("\nPREFAB " + root.name);
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                {
                    Image image = rect.GetComponent<Image>();
                    TMP_Text label = rect.GetComponent<TMP_Text>();
                    output.AppendLine(FullPath(rect, root.transform) + " | anchor=" + rect.anchorMin + "/" + rect.anchorMax + " pivot=" + rect.pivot + " pos=" + rect.anchoredPosition + " size=" + rect.sizeDelta +
                        (image == null ? "" : " image=" + (image.sprite == null ? "NULL" : AssetDatabase.GetAssetPath(image.sprite)) + " color=" + image.color) +
                        (label == null ? "" : " font=" + label.fontSize + " color=" + label.color + " text=" + label.text.Replace("\n", " / ")));
                }
            }
            Directory.CreateDirectory("Temp/UIRepair");
            File.WriteAllText("Temp/UIRepair/layout.txt", output.ToString());
            Debug.Log("UI 좌표 기록: Temp/UIRepair/layout.txt");
        }

        // 루트부터 대상까지의 하이어라키 경로를 반환합니다.
        private static string FullPath(Transform node, Transform root)
        {
            return node == root ? node.name : FullPath(node.parent, root) + "/" + node.name;
        }
    }
}
