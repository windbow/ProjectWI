# ProjectWI 씬 및 에셋 구조

## 성 내정 공용 모달 셸

- `Assets/Editor/WIAdministrationModalVisualUtility.cs`: 공용 셸, 제목 배치, X 닫기 버튼 및 공통 버튼 상태를 성 내정 모달에 적용하는 Editor 전용 조립 유틸리티입니다.
- `Assets/Resources/UI/Generated/administration_modal_shell_v1.png`: 얇은 은회색 외곽, 중앙 상·하단 장식, 빈 헤더와 흑청색 본문으로 구성된 공용 Simple Sprite입니다.
- 공용 셸 사용 프리팹은 `WIAdministrationFocusProjectUGUI`, `WIAdministrationHeroAssignmentUGUI`, `WIAdministrationCharacterActivityUGUI`, `WIAdministrationSpecialFacilityUGUI`, `WIAdministrationBasicFacilityUGUI`, `WIAdministrationDelegationUGUI`, `WIAdministrationMarchUGUI`, `WIAdministrationCastleRecordUGUI`입니다.
- `WIAdministrationObjectiveUGUI`는 화면별 수동 배치를 보존하기 위해 공용 셸에서 제외하며 `objective_modal_frame_v1.png`을 유지합니다.

## Windows 패키징 도구

- `Assets/Editor/WIWindowsPackageBuilder.cs`: `ProjectWI/Build/Package Windows Test Build` 메뉴를 제공하는 Editor 전용 패키징 도구입니다.
- 입력 씬은 `EditorBuildSettings.scenes`에서 활성화되고 실제 파일이 존재하는 항목을 순서대로 사용합니다.
- 중간 빌드는 `Builds/Windows`, 전달용 ZIP은 `Builds/Packages`에 생성됩니다.
- 빌드 대상은 `StandaloneWindows64`, 옵션은 Development와 LZ4 압축입니다.
- 삭제와 출력 경로는 모두 프로젝트 루트 내부인지 검사하며 Player 런타임에는 패키징 코드가 포함되지 않습니다.

## 턴 후속 공통 UGUI

- `Assets/Prefabs/Administration/WIAdministrationTurnFollowupUGUI.prefab`: 턴 처리, 캠페인 결과, 첫해 튜토리얼과 공통 안내를 표시하는 전용 고정 UGUI입니다.
- `turn_followup_frame_v1.png`: 무문자 흑청색 헤더와 본문을 포함하는 전체 금속 프레임입니다.
- `turn_followup_info_panel_v1.png`: 설명·체크 행에 재사용하는 투명 배경의 냉색 금속 정보 패널입니다. 과도한 상하 투명 여백을 제거한 1955×509 원본에 사방 48px Border를 적용하며 프리팹의 4개 사용처는 모두 Sliced Image입니다.
- `turn_followup_header_emblem_v1.png`: 상단 설명 패널 전용 210×256 방패형 소형 나침반 Sprite입니다.
- `turn_followup_map_compass_v1.png`: 하단 체크 패널 전용 768×680 대륙 지도·원형 좌표·가느다란 나침반 결합 Sprite입니다.
- `turn_followup_check_badge_v1.png`: 튜토리얼 완료 상태를 표시하는 청색 체크 방패 Sprite입니다.
- `turn_followup_button_normal_v1.png`, `turn_followup_button_primary_v1.png`: 턴 후속 화면 전용 보조·주요 선택 버튼 Sprite이며 중앙 장식 보존을 위해 Border 0, Simple 타입으로 표시합니다.
- `choiceLabels`: Snapshot 선택 문자열의 첫 줄만 표시하는 버튼 내부 23pt 제목입니다. `choiceDescriptionLabels`는 줄바꿈 이후 내용을 버튼 위 16pt 설명으로 표시합니다.
- `turn_followup_choice_divider_v1.png`: 선택 설명 위에 배치하는 얇은 금속선·중앙 다이아 일체형 Sprite입니다.
- `turn_followup_close_button_v1.png`: 금속 프레임과 X를 모두 포함한 닫기 버튼 완성형 Sprite입니다. 별도 TMP X를 사용하지 않습니다.
- `WIAdministrationTurnFollowupUGUIController`는 `Tutorial` 모드에서만 `TutorialContent`를 활성화하고 Snapshot의 최대 두 선택지를 우측 주요 버튼과 좌측 보조 버튼에 연결합니다.

## 캠페인 목표 상세 UGUI

- `Assets/Prefabs/Administration/WIAdministrationObjectiveUGUI.prefab`: 목표 제목·현재 상황·달성 조건·진행 상황·보상을 표시하는 고정 UGUI입니다.
- `objective_confirm_button_v1.png`: 확인 버튼 전용 무문자 청남색 금속 배경입니다.
- `objective_progress_track_v1.png`, `objective_progress_fill_v1.png`: 빈 진행도 레일과 런타임 Filled Image용 청색 Fill입니다.
- `objective_reward_strip_v1.png`: 금화 G·마나 M·영향력 I 아이콘을 포함한 3칸 보상 프레임입니다. 수치는 `RewardGold`, `RewardMana`, `RewardInfluence` TMP 라벨이 별도로 표시합니다.

전투씬 캐릭터·지면·환경물·머티리얼·라이팅의 현재 설정과 제작 절차는 `GameDocuments/BattleSceneAssetSettingsGuide.md`를 단일 기준 문서로 사용합니다.

- `WIHiddenBattleGrid`: 전투 배경과 분리된 숨은 사각 좌표 유틸리티입니다. 현재 Cell Width 0.6, Cell Height 0.3이며 상하좌우와 네 대각선의 8방향 이웃을 제공합니다. 좌표는 캐릭터 발 위치와 이동 목적지 예약에만 사용하고 격자 선은 생성하지 않습니다.
- `WIBattleCharacterState.GridColumn/GridRow`: 현재 점유 셀입니다. `GridDestinationColumn/GridDestinationRow`와 `HasGridDestination`은 이동 중 목적지 셀을 예약해 다른 캐릭터가 같은 셀을 선택하지 못하게 합니다.
- 근접 공격 접근은 목표 주변 여덟 셀 중 빈 셀을 사용하며 실제 명중은 기존 월드 거리 판정입니다. 투사체와 광역 스킬도 월드 좌표 판정을 유지합니다.

- `Assets/Art/Characters/Ares/Ares_Battle_1WU_A_OutlineBake_V1.png`: 현재 A 근거리 줌 기준 전투용 아레스 Sprite입니다. 외곽 실루엣에 `#10141A` 3단계 그라데이션 선을 직접 베이크했으며 최종 190×256px, 256 PPU와 Scale 1에서 외곽선 포함 높이가 정확히 1월드 유닛입니다. Bilinear, Mipmap 활성, 무압축으로 임포트하며 현재 500명 밀도 테스트의 공용 Sprite입니다.

## 1유닛 캐릭터 축척 기준 완성형 전장 V6

- `Assets/Art/Battle/Effects/WI_CharacterShadow_Oval_V1.png`: 모든 전투 캐릭터에 재사용하는 256×128 RGBA 접지 그림자 Sprite입니다. 중심 최대 알파는 약 51%이고 RGB는 중립 흑회색이며 256 PPU 기준 기본 월드 폭은 1유닛입니다. `WIBattleCharacter.prefab/GroundShadow`에 연결되어 본체보다 Sorting Order 1 낮게 표시됩니다.
- `Assets/Art/Characters/Ares/Ares_Battle_Unit_V1.png`: 기존 원화와 비슷한 약 7등신 비율의 256×384 전장용 정지 Sprite 후보입니다. 실제 알파 높이는 328px이고 328 PPU에서 약 1유닛으로 표시되며 현재 캐릭터 데이터에는 아직 연결하지 않습니다.
- `Assets/Prefabs/Battle/WIBattleCharacterScaleArenaV6.prefab`: 현재 전투 설정에 연결된 전장 프리팹입니다. 약 1유닛 캐릭터 축척을 기준으로 하며 정적 환경물과 접지 그림자는 배경에 함께 굽습니다.
- `GroundChunkTL/TR/BL/BR`: `CharacterScaleArenaV6/Battle_CompleteArena_CharacterScale_V6_4K.png` 3840×2160 마스터를 재샘플링 없이 나눈 1920×1080 4청크입니다. 106.6667 PPU에서 각 18×10.125, 전체 36×20.25 월드 크기이며 재조립 픽셀 완전 일치를 확인했습니다.
- V4 지면은 중앙 집결지의 미세 자갈 밀도를 낮추고 상단 진입로에서 좌우 하단으로 갈라지는 길을 명확히 했습니다. 풀·돌 디테일은 외곽으로 집중하며 방향성 조명과 그림자가 없는 중립 대낮 알베도입니다.
- `Tent_1_NeutralDay_V2.png`, `Tent_3_NeutralDay_V2.png`: V4 지면용 중립 대낮 천막입니다. 원본 실루엣과 알파를 유지하면서 흰 캔버스를 회갈색으로 낮추고 스튜디오 후광을 제거했으며 현재 V4 프리팹의 `TentLeft`, `TentRight`가 참조합니다.
- `Tent_1_Shadow_V1.png`, `Tent_3_Shadow_V1.png`: 천막 전용 384×192 접지 그림자입니다. 최대 알파 약 20%의 중립 흑회색 Unlit Sprite이며 V4 프리팹에서 각 천막의 `GroundShadow` 자식으로 배치됩니다. Sorting Order는 -105입니다.
- `WIBattleCharacterView`는 `1000 - RoundToInt(worldY × 100)`으로 SpriteRenderer Sorting Order를 갱신합니다. 월드 Y가 작은, 화면 아래쪽 캐릭터가 더 큰 정렬값을 받아 앞에 표시됩니다.
- `WI_BattleCharacter_Unlit.mat`: A·B 줌에서 사용하는 일반 URP 2D Sprite Unlit 공용 머티리얼이며 캐릭터 프리팹의 기본값입니다.
- 구형 런타임 외곽선 비교 재질과 셰이더는 현재 데이터에서 참조되지 않아 `Assets/TrashAsset/Art/Battle/OutlineExperiment`로 이동했습니다. 현재 캐릭터 외곽선은 전투용 Sprite에 미리 합성합니다.
- `WI_BattleGround_Unlit.mat`: 지면 알베도의 색을 방향성 조명 없이 확인하기 위한 URP 2D Sprite Unlit 머티리얼입니다. 향후 노멀맵 검증 시 지면 Sprite를 Lit 머티리얼로 교체하고 Secondary Texture를 연결합니다.
- `WI_BattleConfig.showCharacterLabels`, `showCharacterHealthBars`: 순수 맵과 캐릭터 실루엣을 검증하기 위해 현재 모두 `false`입니다. 전투 상태 계산에는 영향을 주지 않고 캐릭터 주변 이름·등급·역할 TextMesh와 HP SpriteRenderer 생성만 생략합니다.
- 성문은 약 12.77×6.39유닛, 텐트는 약 3.27×3.69유닛, 망루는 약 3.38×4.51유닛, 방책은 약 3.04×3.04유닛 범위로 표시됩니다.
- `WI_BattleConfig`의 배경 범위는 36×20.25이며 카메라는 A=6, B=8, C=10 세 단계입니다. `characterDefaultMaterial`과 `characterFarOutlineMaterial`은 현재 동일한 기본 Unlit 머티리얼을 참조합니다.

Unity Editor 전용 `ProjectWI`·`WI` 메뉴 구조와 각 기능 설명은 `GameDocuments/UnityToolMenuManual.md`에 기록합니다.

## 공통 UI 버튼 에셋

- `objective_modal_frame_v1.png`: 1511×1041 목표 상세 전용 무문자 배경 Sprite입니다. 청회색 금속 외곽, 상단 헤더, 상황·조건·진행·보상 구획만 포함하며 Mipmap 비활성·무압축 Single Sprite로 사용합니다.
- `WIAdministrationObjectiveSnapshot`: 목표 제목·상황·조건 외에 `Progress`, `Reward`, `ProgressNormalized`를 제공해 진행 수치, 보상 문구와 Fill 이미지를 각각 갱신합니다.

- `WICampaignTitleUGUI`: 난이도·시작 조건 카드의 선택 상태는 `WICampaignTitleUGUIController.ApplyCardState`가 일반/주요 Sprite를 교체해 표시합니다. `ContinueCampaignButton`은 Single `button_normal` 메인 Sprite를 직접 참조합니다.

- `Assets/Resources/UI/Generated/button_primary.png`: 512×128 공통 주요 버튼 Sprite입니다. TextureImporter와 `button_primary_0`의 9-Slice border는 좌우 28px·상하 24px이며 UI 크기 변경 시 외곽 장식을 보존합니다.

## 과거 전장 아트 보관

- V1~V5와 요새 청크 실험 프리팹은 현재 설정에서 참조되지 않아 삭제했습니다.
- 해당 실험에서만 사용한 PNG·JPG 61개는 삭제하지 않고 `Assets/TrashAsset/Art/Battle` 아래에 기존 상대 경로와 GUID를 유지해 보관합니다.
- 현재 전장은 `Assets/Prefabs/Battle/WIBattleCharacterScaleArenaV6.prefab`과 `CharacterScaleArenaV6`의 네 청크를 사용합니다.
- `Assets/Art/UI/Battle/Battle_Top_Status_Frame_V1.png`: 목표, 아군·적군 수와 턴 영역을 가진 투명 상단 프레임입니다.
- `Assets/Art/UI/Battle/Battle_Character_Info_Frame_V1.png`: 초상 슬롯과 이름·HP·MP 영역을 가진 투명 캐릭터 정보 프레임입니다.
- `Assets/Art/UI/Battle/Battle_Command_Bar_Frame_V1.png`: 4개 명령 버튼 슬롯과 좌우 장식을 가진 투명 프레임입니다.
- `Assets/Art/UI/Battle/Battle_Skill_Bar_Frame_V1.png`: 4개 정사각형 스킬 슬롯을 가진 투명 프레임입니다.
- `Assets/Art/UI/Battle/Battle_UI_Atlas_Transparent_V1.png`: 위 네 프레임을 한 화면에 보관한 투명 원본 시트이며, 크로마 재처리 원본은 `GameDocuments/UIConcepts/Battle_UI_Atlas_Chroma_V1.png`입니다.

## BattleScene 2D 라이팅

- `Global Light 2D`: 백색, 강도 1의 전역광이며 캐릭터와 Lit 환경물의 중립색을 유지합니다.
- `Battle Lighting/Arena Key Light 2D`, `Arena Fill Light 2D`: 향후 노멀맵 검증용으로 씬에 보존하지만 현재 비활성 상태입니다.
- 현재 지면은 `WI_BattleGround_Unlit.mat`을 사용하며 방향성 조명과 그림자를 받지 않습니다. 별도 노멀맵과 `ShadowCaster2D`는 아직 구성하지 않았습니다.

## 시스템 UGUI 연결

- `WIAdministrationSystemSnapshot`은 `WISystemSettingsService.Settings`, `WICampaignRuntimeService.AutoSaveEnabled`, 저장 슬롯 0~3 존재 여부를 읽어 고정 UGUI에 전달합니다.
- 설정 변경은 기존 `WISystemSettingsState`에 반영하며 적용 시 `Apply()`와 `Save()`를 호출합니다. 캠페인 저장과 불러오기는 기존 `SaveSlot`·`LoadSlot` API를 사용합니다.
- `WIAdministrationModalUGUIController`는 활성 인스턴스를 정적 집합으로 관리해 전역 단축키 차단 여부와 Escape로 닫을 최상위 Canvas를 제공합니다. 닫힌 모달은 컨트롤러 루트와 이벤트 구독은 유지하되 루트 `Canvas`와 `GraphicRaycaster`를 비활성화하며, `Show`와 `Hide`가 표시·입력 상태를 함께 전환합니다. 실제 명령 키 판정은 `WIAdministrationWorldUGUIController`가 Input System으로 처리합니다.
- `WIAdministrationUIController` 초기화 시 미결 전투가 발견되면 `OpenPendingBattleReportUGUIAfterInitialization` 코루틴이 한 프레임 대기한 뒤 `UGUIMonthlyReportRequested`를 발생시킵니다.
- 캠페인 시작 데이터는 UGUI 타이틀 컨트롤러가 `WIAdministrationDatabaseSO.DifficultyDefinitions`와 `CampaignVariants`를 읽어 프리팹의 고정 카드 배열에 표시합니다. 선택 결과만 `BeginCampaign`으로 전달되며 행정 컨트롤러는 카드 오브젝트를 생성하지 않습니다.
- UGUI 지도 노드의 식별자·버튼·마커 배열은 `WIAdministrationWorldUGUI.prefab`에 직렬화됩니다. 소유 진영, 선택 상태와 정보 공개 문구는 `TryGetUGUIMapSnapshot`을 통해 갱신하며 기존 UI Toolkit `BuildMap()`은 실제 UGUI 초기화·캠페인 재구성·설정·불러오기 경로에서 호출하지 않습니다.
- `WIUIScreenManager`에는 캠페인 타이틀, 월드, 영지와 기능 모달을 합친 23개 프리팹 에셋 참조가 직렬화됩니다. 씬 자식은 저장하지 않고 `Awake()`에서 완성 프리팹을 생성합니다.

## 1. MainScene 하이어라키

- `Main Camera`: 2D 전략 화면 렌더링 카메라입니다.
- `Global Light 2D`: 2D 렌더러의 전역 조명입니다.
- `EventSystem`: PC와 모바일 UI 입력을 처리합니다.
- `UGUI Screen Bootstrap`: 단일 `WIUIScreenManager`로서 23개 완성 UGUI 화면 프리팹 참조를 보관하고 플레이 시작 시 인스턴스를 구성합니다. 편집 모드에는 화면 자식이 저장되지 않으며 별도 `MainCanvas` 관리자는 존재하지 않습니다.
- `ManagerObjects`: 전략 게임 시스템 오브젝트의 루트입니다.
  - `AdministrationUI`: 영지 관리 UI 프리팹 인스턴스입니다.

## 2. AdministrationUI

- `WIAdministrationUI.prefab`은 `UIDocument` 없이 `WIAdministrationUIController`만 유지하며 캠페인 상태와 기능별 UGUI 이벤트·스냅샷을 연결합니다.
- 인물·시설 UGUI 데이터는 `WIAdministrationUIController.UGUIBridge` 계열에서 기존 인물·성·시설 ScriptableObject 및 런타임 상태를 스냅샷으로 변환합니다.
- `WIAdministrationUIController.Characters.cs`에는 등급과 특기 표시 문자열 헬퍼만 남고 UI Toolkit 모달 생성 코드는 없습니다.
- `WIAdministrationUIController.RealmGovernance.cs`에는 `AssignCastleProject`, 확장 가능 조건, 사업·투자 표시와 선택 성 관리 권한 검사만 남습니다. 연구·의회·시스템의 상태 변경은 `UGUIResearchBridge`, `UGUICouncilBridge`, `UGUISystemBridge`가 기존 시스템 API에 전달합니다.
- `WIAdministrationUIController.RealmRelations.cs`에는 UGUI 표시용 문자열 변환 헬퍼만 남고 UI Toolkit 화면 및 명령 실행 코드는 없습니다. 외교 실행은 `ExecuteUGUIDiplomacyAction`, 첩보 예약은 `ScheduleUGUIScheme`, 진영 정세 변환은 `TryGetUGUIFactionOverview` 계열 브리지에서 담당합니다.
- `WIAdministrationUIController.RealmReports.cs`에는 UGUI 표시용 조건·방침 문자열 헬퍼만 남고 UI Toolkit 보고·사건·위임 화면 생성 코드는 없습니다. 사건 결과는 `ExecuteUGUIEventChoice`, 위임 변경은 `ExecuteUGUIDelegationAction`, 보고 표시는 `TryGetUGUIMonthlyReport` 계열 API가 기존 상태와 시스템을 사용합니다.
- `WIAdministrationUIController.Territory.cs`의 UI Toolkit 성 기록·시설 선택 모달은 제거됐습니다. 성 기록은 `TryGetUGUICastleRecord`, 시설 선택은 `TryGetUGUISpecialFacilitySelection`과 `ChooseUGUISpecialFacility`가 기존 성·시설 상태를 사용합니다.
- `WIAdministrationUIController.Turn.cs`에는 미결 전투 차단과 UGUI 턴 실행 코루틴만 남습니다. 결과·튜토리얼·메시지 스냅샷 및 선택 처리는 `WIAdministrationUIController.UGUITurnFlowBridge.cs`와 `WIAdministrationTurnFollowupUGUIController`가 담당합니다.
- `WIAdministrationUIController.cs`는 `MonoBehaviour` 상태 브리지이며 직렬화된 행정 데이터베이스, 캠페인 상태 초기화, 선택 성, UGUI 갱신·안내 연결만 보유합니다. UI Toolkit 컴포넌트나 시각 트리 필드는 없습니다.
- `WIAdministrationUIController.World.cs`는 선택 성 전환, QA용 기본 월드/성 상태 진입과 UGUI 표시 문자열 헬퍼만 보유합니다. 60개 지도 노드와 월드 HUD는 `WIAdministrationWorldUGUI.prefab`과 `WIAdministrationMapUGUIController`가 담당합니다.
- 비전투 런타임 스크립트에는 `UnityEngine.UIElements`, `UIDocument`, `VisualTreeAsset`, `PanelSettings` 의존성이 남아 있지 않습니다. 레거시 `WIAdministrationWorldView.uxml`의 지도 연결선 자리도 일반 `VisualElement`이며 전용 `WIMapConnectionLayer` 형식은 삭제됐습니다.
- `WIUIScreenManager`는 등록된 완성 UGUI 프리팹 인스턴스의 루트를 활성 상태로 생성해 컨트롤러의 이벤트 구독을 유지합니다. 캠페인 타이틀만 최초 Canvas가 활성화되고, 월드·영지·공통 모달 Canvas 및 GraphicRaycaster는 실제 표시 조건이 될 때만 활성화됩니다.
- Unity Editor 플레이 모드에서는 `WIUIScreenManager`가 매번 새로 생성되는 23개 화면 Clone의 루트만 `SceneVisibilityManager.DisablePicking(screen, false)`로 피킹 차단합니다. 자식 UI는 피킹 가능하므로 Scene View에서 실제 패널·버튼을 직접 선택할 수 있으며 Player 빌드에는 이 처리가 포함되지 않습니다.
- `WIHeroDefinition.battleSprite`는 인물별 선택 전투 전신 Sprite입니다. 현재 밀도 테스트에서는 `Assets/Art/Characters/Ares/Ares_Battle_1WU_A_OutlineBake_V1.png`가 공용 연결되며 `WIBattleCharacterView`는 값이 있으면 흰색 원본 Sprite를, 없으면 기존 진영색 플레이스홀더를 표시합니다. 아레스 UI 초상화는 고해상도 원본에서 얼굴 중심으로 추출한 `Ares_Portrait_Face_V1.png`를 사용합니다.
- `WIBattleCameraController.FrameCombatants`는 전체 참가자의 초기 좌표 범위와 2~60명 밀도를 함께 계산해 직교 카메라 시작 크기를 결정합니다. `WI_BattleConfig.cameraMaximumZoom`은 12이며 60명 전투는 최대 줌아웃을 사용합니다.
  - `Views/WIAdministrationHud.uxml`: 상단 HUD
  - `Views/WIAdministrationWorldView.uxml`: 전략 지도와 전역 명령
  - `Views/WIAdministrationTerritoryView.uxml`: 선택 영지 관리
  - `Views/WIAdministrationOverlays.uxml`: 캠페인 시작과 모달 레이어
  - `WIAdministrationPanelSettings.asset`
- `WIAdministrationUIController`
  - 전략 데이터베이스 참조
  - 캠페인 런타임 상태 생성
  - UXML에 고정 배치된 60개 지도 노드와 HUD 데이터 갱신
  - 성 선택 및 Bottom Sheet
  - 영웅 배치, 기본 시설 안내, 특화 시설 선택

## 2-1. UGUI 캠페인 타이틀

- `Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab`
  - `Canvas`, `CanvasScaler`, `GraphicRaycaster`
  - `Backdrop`: 전체 화면 차광
  - `CampaignPanel`: `popup_panel.png` 기반 중앙 패널
  - 난이도 카드 3개와 시작 조건 카드 3개
  - 새 캠페인 및 자동 저장 이어하기 버튼
- `WICampaignTitleUGUIController`
  - ScriptableObject 문구를 고정 카드에 표시
  - 선택 카드 강조 상태 관리
  - 기존 `WIAdministrationUIController`의 캠페인 시작·불러오기 흐름 호출
  - 성 상태 표시와 월간 중점 사업 지정
  - 진영 의회, 영지관 위임과 월간 보고 표시
  - 인물 현황과 월간 개인 활동 배정
  - 전투단 생성, 역할 편성, 이동과 원정
  - 합동 훈련, 편성 변경, 해산과 점령 안정
  - 턴 실행과 결과 표시

## 2-2. 성 내정 기능 모달 공통 아트

- `WIAdministrationFocusProjectUGUI`, `WIAdministrationHeroAssignmentUGUI`, `WIAdministrationCharacterActivityUGUI`, `WIAdministrationSpecialFacilityUGUI`, `WIAdministrationBasicFacilityUGUI`, `WIAdministrationDelegationUGUI`는 동일한 냉색 금속 모달 규칙을 사용합니다.
- `WIAdministrationModalVisualUtility`가 `bg_type_d` 본문 패널, `button_flat_normal` 헤더·일반 버튼, `button_flat_primary` 주요 버튼, `turn_followup_close_button_v1` 닫기 버튼과 버튼 상태 색을 적용합니다.
- 각 화면의 빌더가 저장 직전에 공통 스타일을 다시 적용하므로 기반 프리팹을 복제하거나 메뉴에서 재생성해도 구형 종이 헤더와 흰 버튼이 다시 저장되지 않습니다.

## 3. 프리팹

- `Assets/Prefabs/Administration/WIAdministrationUI.prefab`
  - 영지 관리 화면의 재사용 루트
  - UXML에 고정된 60개 성 노드에 ScriptableObject 좌표·이름·소유 진영을 바인딩
  - 전역 4개 영웅 카드, 성 화면 8개 영웅 슬롯과 2개 특화 시설 슬롯을 고정 보유
  - 지도, 문장, 영지관 초상화는 빈 이미지 슬롯 제공
- `Assets/Prefabs/Battle/WIBattleCharacter.prefab`
  - SpriteRenderer와 `WIBattleCharacterView`로 구성된 교체 가능한 빈 전투 캐릭터 표시 프리팹

## 4. 데이터 에셋

- `Assets/Resources/UI/Generated/bg_type_a.png`
  - `Panel_BackGround.png`의 바깥 체크무늬 영역을 제거하고 모서리를 투명 처리한 1587×508 RGBA 배경 Sprite
  - Background A Type으로 분류하며 좌·우·상·하 3px 9-Slice 사용
  - 모서리 사선 깊이 2px, 단일 2px 금속 외곽선 사용
  - 알파 투명도 사용, 밉맵 비활성, Clamp, 무압축 임포트 설정
  - 행정 화면의 HUD·지도 보조 패널·명령 바·성 정보 패널·공통 모달과 전투 HUD 정보 패널의 공통 배경이며 캠페인 첫 화면은 기존 `popup_panel.png` 유지

- `Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset`
  - 전역 지도, 진영 문장, 인물 초상화, 성 전경과 시설 아이콘 Sprite 참조
  - 진영 5개
  - 성 60개와 인접 경로
  - 고유 등급 100명과 일반 등급 400명(현재 자동 검증용 대규모 로스터)
  - 특화 시설 8개
  - 시작 자원과 시작 배치
  - 핵심 인물 19명의 시작 배치와 나머지 481명의 탐색·영입 대기 상태
  - 턴 규칙
  - UI 문자열
  - 성 규모와 번영/기술/안정/방어 초기값
  - 전략 방어 전력 가중치: 성 방어 300%, 질서 50%, 주둔 인물 무력 60%
  - 영웅 통솔 능력치
  - 인물 등급과 특기
  - 인재별 영입 요구와 필요 명성
  - 연구 5개와 작위 4개
  - 일반 인물 영웅 승격 기준: 공훈 80, 명성 30, 영향력 30과 전투 승리 성취
- `Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset`
  - 전장 크기, 진형 간격, 기본 체력·마나와 능력치 환산값
  - 이동 속도, 근접·원거리 사거리, 공격 간격과 기본 피해
  - 전진·사수·집중 명령 배율과 영웅별 액티브 스킬 목록
  - 섬멸·제한 시간 방어·중앙 거점 점령의 전투 목표 3종과 제한 시간·점유 시간·반경
  - 추후 교체할 전투 캐릭터 임시 Sprite 슬롯
- `Assets/Data/ScriptableObject/System/WI_SystemSettingsConfig.asset`
  - 전체·음악·효과음 기본 음량
  - 기본 전체 화면, 목표 FPS와 표시 언어

## 5. 코드

- `WIAdministrationDatabaseSO.cs`: 전략 마스터 데이터 정의
- `WIAdministrationState.cs`: 캠페인 런타임 상태
  - 개별 인물 이동 예약과 통합 임무 점유 상태
- `WIAdministrationTurnSystem.cs`: 영지 관리 턴, 인물 활동, 의뢰와 전투단 이동 계산
  - 연구 시작·완료·진영 효과, 작위, 영웅 승격, 영지관 임명과 개별 인물 이동 계산
  - 실제 전쟁 접경 위협도와 ScriptableObject 설정 기반 AI 전선 활성화
- `WICampaignAutoPlayer.cs`: 성장형·균형형·공세형 캠페인 자동 진행과 재미 검증 지표 수집
- `WICampaignAutoTestLabWindow.cs`: 단일 조합 또는 정책 3종 × 난이도 3종 캠페인 완주와 결과 내보내기 에디터 도구
- `WICampaignAutoTestLab.uxml`, `WICampaignAutoTestLab.uss`: 자동 테스트 랩의 고정 입력·결과 9행 레이아웃과 스타일
- `WIFunValidationAutomationTests.cs`: 세 정책 24·60·120개월, AI 전선, 전투 목표와 첫 12개월 사건 밀도 회귀 검증
- `WICampaignAutoTestLabTests.cs`: 캠페인 종료 정지, UXML 고정 결과 행과 보고서 변환 검증
- 일반 인물 데이터는 현재 `WIMassCharacterSeeder`의 500명 결정론적 재구성과 Character Data Viewer의 직접 편집 경로로 관리합니다. 과거 소규모 `WICommonRosterSeeder`는 제거했습니다.
- `WIAdministrationUIController.cs`: 행정 데이터와 캠페인 상태를 기능별 UGUI 프리팹에 연결하는 런타임 브리지
- `WIAdministrationUIController.*.cs`: 캠페인·지도·영지·인물·군사·진영 관계·보고·턴 기능별 partial 컨트롤러
- `WIAdministrationMapUGUIController.cs`: 프리팹에 고정 배치된 60개 성 노드의 소유·선택 상태와 입력 갱신
- `Assets/Resources/UI/Generated/map_castle_*.png`: 5대 진영의 성채·깃발 지도 마커
- `Assets/Art/Characters/Battle_MedievalSwordsman_Test_V2.png`: 약 4.5~5등신 양손 검 전투 준비 자세의 투명 배경 단일 프레임 테스트 스프라이트(전투 데이터 미연결)
- `Assets/generated/sprites/medieval-swordsman-idle/sprite-sheet-alpha.png`: 중세 검사 4프레임 공격 애니메이션 투명 아틀라스. 프레임 좌표와 8 FPS 비반복 재생 정보는 같은 폴더의 `manifest.json`이 소유하며 현재 전투 데이터에는 연결하지 않음
- `Assets/generated/sprites/medieval-swordsman-attack-v2/sprite-sheet-alpha.png`: 관절 포즈 가이드로 높은 당김·앞발 내딛기·대각선 타격·낮은 후속 자세를 고정한 개선 공격 아틀라스. 4프레임 좌표와 8 FPS 비반복 정보는 같은 폴더의 `manifest.json`이 소유하며 현재 권장 검토 후보이나 전투 데이터에는 연결하지 않음
- `Assets/Art/Characters/Character_MedievalSwordsman_Test_V1.png`: 일러스트 방향 비교를 위해 보존한 미사용 초기 테스트 이미지
- `Assets/Resources/UI/Generated/icon_flat_*.png`: 군사·영웅·외교·첩보·연구·통치·의회·월간 보고·다음 턴 명령 아이콘
- `Assets/Resources/UI/Generated/hud_flat_*.png`: 날짜·금화·마나·영향력 HUD 아이콘
- `Assets/Resources/UI/Generated/button_flat_*.png`: 일반·선택·위험 9-Slice 버튼 배경. A(`normal`)와 B(`primary`)는 2px 투명 모서리와 단일 2px 테두리의 동일 형상이며 B만 푸른 색조를 사용함
- `Assets/Resources/UI/Generated/bg_type_a.png`: 전역 지도 좌측 성 요약 패널과 오른쪽 목표·알림 분할 패널을 포함한 주요 공통 패널의 3px 9-Slice 배경
- `Assets/Resources/UI/Generated/bg_type_b.png`: 초기 검은 금속 패널의 테두리를 원본 대비 약 1/2로 줄인 1587×508 강조 패널 배경. `.bg-type-b` 클래스와 상하좌우 16px 9-Slice를 사용함
- `Assets/Resources/UI/Generated/bg_type_c.png`: 외곽 석재 프레임 없이 청동 테두리, 대각 모서리와 어두운 중앙 면만 가진 1587×508 강조 패널 배경. `.bg-type-c` 클래스와 좌우 72·상하 40px 9-Slice를 사용함
- `Assets/Resources/UI/Generated/bg_type_d.png`: 전략 화면 알림 영역용 512×640 세로형 공통 패널 배경. 얇은 은회색 금속 프레임과 중립적인 흑청색 질감 면으로 구성하며 문구·아이콘·행 장식은 포함하지 않음. 프레임과 연결되지 않은 생성 노이즈 69픽셀을 제거했으며 `.bg-type-d` 클래스와 상하좌우 16px 9-Slice를 사용함. 정리 전 원본은 `Assets/TrashAsset/UI/Generated/bg_type_d_noisy_original.png`에 보관함
- `Assets/Resources/UI/Generated/popup_header.png`: 은색 금속 테두리를 기존 약 1/4 두께로 줄인 1024×297 공통 모달 헤더. Unity Sprite 영역도 전체 1024×297로 설정하며 좌우 20·상하 10 슬라이스를 사용함
- `Tools/UIAssetSources/popup_header_selected_source.png`: 검은 바깥 여백을 제거한 1774×515 선택 원본
- `GameDocuments/ButtonTypeGuide.md`: 현행 버튼 이미지의 A~H Type 공식 별칭, 사용처와 업무 지시 기준
- 전역 지도 `다음 턴`: `generated-ornate-action` 클래스와 `#turn-button` 전용 규칙으로 `button_type_h.png`를 적용. 240×72 표시 크기에서 9-Slice 없이 `scale-to-fit`으로 원형을 보존하며 `NotoSerifKR` 굵은 명조체를 사용함
- 전역 지도 하단 일반 명령 버튼: `button_flat_normal.png`와 3px 9-Slice를 사용하며 아이콘·문자·단축키 요소는 유지함
- `GameDocuments/FantasyTerminologyGuide.md`: 삼국지식 이전 표현과 판타지 공식 표시 용어의 대응표, 성·영지 구분 및 내부 식별자 유지 기준
- `GameDocuments/UIReferences/ProjectWI_ButtonTypeCatalog.png`: 기존 A~G 버튼 에셋 7종을 모은 시각 카탈로그
- `Assets/Resources/UI/Generated/button_type_h.png`: 왼쪽 화살촉 장식과 은·황동 이중 테두리를 갖춘 H Type 대표 진행 버튼 배경
- `Assets/Resources/UI/Generated/right_panel_*.png`, `right_objective_card.png`, `right_notice_row.png`, `right_danger_row.png`, `right_action_button.png`: 전략·성 화면 우측 정보 패널과 하단 전역 명령(군사~월간 보고)에 사용하며, `right_action_button.png`는 모서리 절삭을 최소화한 직사각 금속 D Type 9-Slice 에셋
- `Assets/Fonts/NotoSerifKR-VariableFont_wght.ttf`: 제목과 본문용 한글 명조 서체
- `Assets/Fonts/NotoSansKR-VariableFont_wght.ttf`: 숫자·작은 명패·버튼용 한글 고딕 서체
- `WIBattleRuntimeBuilder.cs`: 전투 세션 참가자를 역할별 초기 진형으로 변환
- `WIBattleSimulation.cs`: 표적 탐색, 이동, 공격과 승패 계산
- `WIBattleRuntimeController.cs`: 전투 씬 오브젝트와 런타임 연결
- `WIBattleCharacterView.cs`: 빈 Sprite 기반 캐릭터 표시와 상태 동기화
- `WIBattleHUDController.cs`: 전투 상태, 전투단 명령과 영웅 스킬 버튼 연결
- `WIBattleSceneBootstrap.cs`: 영속 캠페인에서 전투 세션을 받아 결과를 반환
- `WICampaignRuntimeService.cs`: 씬 전환 사이 캠페인 상태와 대기 전투 ID 보존
- `WIAdministrationUIController.Campaign.cs`: 난이도·시작 조건 선택 카드 구성과 마침표 단위 설명 줄바꿈
- `WICampaignSaveSystem.cs`: 버전 저장 봉투, JSON 직렬화와 안전한 파일 입출력
- `WISystemSettingsConfigSO.cs`: 언어, 화면, 프레임과 오디오 기본값
- `WISystemSettingsService.cs`: 사용자 설정 로드, 적용과 PlayerPrefs 영속화

## 6. 전투 씬

- `Assets/Scenes/BattleScene.unity`
  - Main Camera, Global Light 2D 유지
  - `BattleRuntime`에 전투 설정과 캐릭터 프리팹 연결
  - `BattleRoot` 아래에 세션 참가 캐릭터를 런타임 생성
  - 전투 종료 시 결과 API 호출 후 `MainScene`으로 복귀

## 7. UI 후속 작업 문서

- `GameDocuments/UIHandoffReport.md`: 전략 지도·영지 관리 목표 시안, 현재 UXML/USS 구조, 재사용 에셋, 후속 수정 순서와 완료 기준
- `GameDocuments/UIConcepts/ProjectWI_Strategy_UI_Concept_V1.png`: 전략 지도 목표 시안
- `GameDocuments/UIConcepts/ProjectWI_Castle_Administration_UI_Concept_V1.png`: 영지 관리 목표 시안
# UGUI 표시 데이터

- 월드 UGUI는 기존 `WIAdministrationDatabaseSO`, `WIAdministrationState`, `WICampaignObjectiveSystem`, `WIAdministrationTurnSystem`의 데이터를 읽기 전용 `WIAdministrationWorldSnapshot`으로 변환하여 표시합니다.
- UGUI 전용 마스터 데이터나 중복 밸런스 데이터는 추가하지 않았습니다.
- 지도 노드의 ID·이름·진영·선택 상태는 기존 60개 `WICastleDefinition`과 `WICastleRuntimeState`를 `WIAdministrationMapNodeSnapshot`으로 변환해 표시합니다.
- 영지 화면은 선택된 `WICastleRuntimeState`와 성·영웅·시설 정의를 `WIAdministrationTerritorySnapshot` 및 슬롯 스냅샷으로 변환합니다. HUD용 진영·연월·금화·마나·영향력, 영지관 이름, 월 수입도 같은 읽기 전용 스냅샷에서 제공하며 UI 전용 영웅·시설 데이터는 복제하지 않습니다.
- `WIAdministrationTerritoryUGUI.prefab/TerritoryContent/TopHUD`: 월드 프리팹의 `WorldContent/TopHUD`와 동일한 92px 공통 상단 HUD입니다. 동일한 직계 오브젝트 순서, RectTransform, Image/TMP 설정과 월보·의회·연구·설정 버튼 동작을 사용하며 `TerritoryTopHudMatchesWorldTopHud` 검사로 구조 차이를 방지합니다.
- `Assets/Resources/UI/Generated/territory_bottom_panel_v1.png`: 성 내정 화면 하단의 주둔 영웅·특화 시설·이번 달 중점 영역을 감싸는 512×171 무문자 공통 프레임. 상하좌우 18px 9-Slice를 사용합니다.
- `Assets/Resources/UI/Generated/territory_back_button_v1.png`: 성 내정 화면의 `대륙 지도` 전용 512×142 무문자 버튼 프레임. 좌우 12px·상하 10px 9-Slice로 얇은 외곽선과 작은 장식을 보존합니다.
- `WIAdministrationTerritoryUGUI.prefab`: 상단 HUD, 좌측 성 현황, 중앙 성 전경, 우측 8개 내정 명령, 하단 영웅 4칸·시설 2칸·중점 카드와 대륙 지도/다음 턴 버튼을 고정 배치합니다.
- 중점 사업 선택 화면은 `WIAdministrationFocusProjectSnapshot`과 담당자 스냅샷을 사용하며, 비용과 예상 성과는 기존 사업 밸런스 데이터 및 계산 시스템에서 즉시 산출합니다.
- 영웅 배치 화면은 기존 `WICharacterRuntimeState`, 성별 `HeroIds`, 인물 정의를 `WIAdministrationHeroAssignmentSnapshot`으로 변환하며 별도 인물 데이터를 만들지 않습니다.
- 인재 활동 화면은 기존 `WICharacterRuntimeState.Activity`, `ActivityTargetHeroId`, 피로·부상·명성·발견 상태를 `WIAdministrationCharacterActivitySnapshot`으로 변환합니다. 탐색·교류·영입·훈련·휴식 결과 계산은 기존 `WIAdministrationTurnSystem`의 월말 처리를 그대로 사용합니다.
- 특화 시설 선택 화면은 `WIAdministrationDatabaseSO.SpecialFacilities`, 성의 `SpecialFacilityIds`, `PendingSpecialFacilityChoice`를 `WIAdministrationSpecialFacilitySnapshot`으로 변환하며 UI 전용 시설 데이터는 추가하지 않습니다.
- 기본 시설 화면의 선술집 의뢰는 성의 `TavernQuests`와 `WITavernQuestDefinition`을 `WIAdministrationBasicFacilitySnapshot`으로 변환합니다. 담당자 적성은 기존 `WIAdministrationTurnSystem.GetQuestAptitude`로 계산합니다.
- 영지관 위임 화면은 성의 `GovernorHeroId`, `GovernorPolicy`, `GovernorMonthlyBudget`, `DelegatedToGovernor`를 `WIAdministrationDelegationSnapshot`으로 변환합니다. 예상 사업은 기존 `WIAdministrationTurnSystem.GetDelegationPreview` 결과를 표시합니다.
- 원정 화면은 기존 `WIArmyState`, 성 인접 경로와 진영 관계를 `WIAdministrationMarchSnapshot`으로 변환합니다. 새 전투단은 `CreateArmy`, 이동·원정은 `BeginArmyMarch`를 호출하며 별도 전투단 데이터를 만들지 않습니다.
- 성 상세 기록 화면은 선택된 성 정의와 런타임 상태를 `WIAdministrationCastleRecordSnapshot`으로 변환합니다. 상세 공개 여부는 기존 `WIInformationVisibility` 판정을 그대로 사용합니다.
- 캠페인 목표 상세 화면은 `WICampaignObjectiveSystem.GetCurrent`와 `GetProgress` 결과를 `WIAdministrationObjectiveSnapshot`으로 변환하며 목표 마스터 데이터를 복제하지 않습니다.
- 월간 보고 화면은 `LastMonthlyReport`, 미결 선택 사건 컬렉션과 `BattleSessions`를 `WIAdministrationMonthlyReportSnapshot`으로 변환합니다. 사건 선택은 기존 모달을 유지하고 전투는 `WICampaignRuntimeService.StartBattle`에 연결합니다.
- 군사 화면은 `BattleSessions`, 플레이어 소유 `WIArmyState`, 성 인접 경로와 대기 영웅을 단계별 `WIAdministrationMilitarySnapshot`으로 변환합니다. 편성·단원 관리·훈련·해산·원정은 기존 `WIAdministrationTurnSystem`을 호출하며 UI 전용 군사 데이터는 복제하지 않습니다.
- 영웅 전역 화면은 `WICharacterRuntimeState`와 `WIHeroDefinition`, `WITitleDefinition`을 `WIAdministrationHeroesSnapshot` 카드로 변환합니다. 승격과 작위 수여는 기존 `PromoteCommonCharacter`, `AwardTitle`을 호출하며 영웅·작위 데이터를 중복 생성하지 않습니다.
- MainScene의 `UGUI Screen Bootstrap`에 있는 유일한 `WIUIScreenManager` 컴포넌트는 UGUI 프리팹 에셋 참조만 보관합니다. 각 화면은 `Assets/Prefabs/Administration`의 독립 프리팹이며 `WIAdministrationUGUISceneUtility`가 빌더 재생성 시 참조 목록을 갱신합니다. 같은 컴포넌트를 가진 이전 이름 또는 중복 루트가 발견되면 참조를 이 관리자에 병합하고 중복 루트를 제거합니다.
- `Assets/Prefabs/Administration/WIAdministrationUI.prefab`의 루트 컴포넌트는 `Transform`과 `WIAdministrationUIController`뿐입니다. `UIDocument` 없이 캠페인 상태 초기화와 UGUI 이벤트·스냅샷 브리지를 제공합니다.
- 군사 UGUI 상태 변경은 `ExecuteUGUIMilitaryAction`이 기존 `WIAdministrationTurnSystem` API를 호출합니다. 레거시 군사 partial에는 UI 생성 코드 없이 `GetArmyMarchFailureMessage`와 `GetUnitRoleDisplayName` 헬퍼만 남습니다.
- 외교 화면은 `WIDiplomaticRelationState`, 진영 런타임 자원, 포로 상태와 공동 공격 후보를 `WIAdministrationDiplomacySnapshot`으로 변환합니다. 모든 외교 명령은 기존 `WIAdministrationTurnSystem`의 확정 명령 API를 호출하며 관계 데이터를 복제하지 않습니다.
- 첩보 화면은 `WISchemeDefinition`, `WISchemeMissionState`, `WISchemeIntelState`, 대기 인물과 성 상태를 `WIAdministrationSchemeSnapshot`으로 변환합니다. 성공률은 기존 `WISchemeSystem.CalculateSuccessChance`, 임무 예약은 `TrySchedule`을 사용하며 UI 전용 첩보 데이터를 추가하지 않습니다.
- 연구 화면은 `WIResearchDefinition`, `WIFactionRuntimeState`의 완료·진행 상태, 플레이어 성 기술과 대기 인물을 `WIAdministrationResearchSnapshot`으로 변환합니다. 연구 시작은 기존 `WIAdministrationTurnSystem.BeginResearch`를 호출하며 연구 마스터 데이터를 복제하지 않습니다.
- 진영 정세 화면은 `WIFactionDefinition`, `WIFactionRuntimeState`, 성 소유권, 외교 상태와 시작 인물 관계를 `WIAdministrationFactionSnapshot`으로 변환합니다. 다른 진영의 비공개 자원은 포함하지 않으며 읽기 전용 카드만 사용합니다.
- 의회 화면은 `WIFactionPolicy` 6종과 플레이어 `WIFactionRuntimeState.Policy`를 `WIAdministrationCouncilSnapshot`으로 변환합니다. 방침 선택은 별도 UI 데이터를 만들지 않고 기존 `WIAdministrationState.FactionPolicy` 속성에 저장하여 월말 사업 보너스 계산에서 그대로 사용합니다.
- 선택 사건 화면은 월간 보고의 `WIAdministrationReportActionType`과 각 대기 사건 목록 위치를 `WIAdministrationEventChoiceSnapshot`으로 변환합니다. 선택 결과는 `WIRegionalEventSystem`, `WIOccupationEventSystem`, `WIAdministrationTurnSystem`의 기존 판정 API와 `InstallHeroLegacy`에 전달하며 UI 전용 사건 결과 데이터는 만들지 않습니다.
- 턴 후속 화면은 `WIAdministrationState.CampaignResult`, 캠페인 엔딩 정의와 `WITutorialSystem.GetPending` 결과를 `WIAdministrationTurnFollowupSnapshot`으로 변환합니다. 캠페인 결과 확인 상태와 튜토리얼 완료·전체 건너뛰기는 기존 캠페인 상태에 저장하고 자동 저장하며, 턴 결과 본문은 중복 데이터 없이 기존 월간 보고 스냅샷을 사용합니다.
- 캠페인 시작 후속 흐름은 기존 `WICampaignObjectiveSystem.GetCurrent` 결과를 목표 UGUI로 표시하고 `WIAdministrationModalUGUIController.Closed`를 통해 튜토리얼로 연결합니다. 자동 저장 오류와 목표 누락 안내는 `WIAdministrationTurnFollowupMode.Message` 스냅샷을 사용하며 별도의 메시지 마스터 데이터를 추가하지 않습니다.
- 인물 이동 대상 화면은 출발 성의 `AdjacentCastleIds`, 같은 진영 여부, 목적지 `HeroIds`, `CharacterTransfers` 예약 수를 `WIAdministrationCharacterActivitySnapshot` 카드로 변환합니다. 확정 시 기존 `WIAdministrationTurnSystem.StartCharacterTransfer`를 호출하므로 UI 전용 이동 상태나 별도 경로 데이터는 추가하지 않습니다.
- 공통 `ShowMessage`는 `WIAdministrationDatabaseSO.GetText`로 UID를 번역한 뒤 `WIAdministrationTurnFollowupMode.Message`와 제목·본문 문자열로 변환합니다. 기존 호출부의 오류·성공 결과 데이터는 변경하지 않고 UI Toolkit 모달 생성만 제거했습니다.
- 전투 HUD 이미지 연결은 `WIBattleHUD.uss`의 `battle-top-status-frame`, `battle-character-info-frame`, `battle-command-frame`, `battle-skill-frame` 클래스에 고정되어 있습니다. `battle-status`, `selection-info`, `command-feedback`, `skill-buttons` 및 네 명령 버튼의 이름은 런타임 컨트롤러 계약이므로 유지합니다.
- `WIBattleConfigSO.battleSpriteScale`: 전투용 캐릭터 Sprite의 공통 Transform 배율입니다. 현재 `1`이며 전투 이미지가 없는 플레이스홀더에는 적용되지 않습니다. 실제 캐릭터 크기는 전투 Sprite의 PPU 규격으로 관리합니다.
- `WIBattleConfigSO.arenaBackgroundSize`: 배경 Sprite 전용 월드 표시 크기입니다. 현재 `57.024×32.076`이며 `arenaSize` 18×10의 이동·진형·충돌 판정에는 영향을 주지 않습니다.
- `WIBattleConfigSO.cameraMinimumZoom`, `cameraMiddleZoom`, `cameraMaximumZoom`: A/B/C 고정 줌의 직교 크기이며 현재 각각 `6`, `8`, `10`입니다.
- `WI_BattleConfig.arenaBackground`: 현재 `Battle_FortressField_V4_4K` Sprite를 참조합니다. V1~V3는 비교와 복구용으로 유지합니다.
- `WI_BattleConfig.arenaBackground`: 현재 `Battle_FortressField_V5_4K` Sprite를 폴백 배경으로 참조합니다. V1~V4는 `Assets/TrashAsset/Art/Battle`로 이동했습니다.
- `WI_BattleConfig.arenaPrefab`: 현재 완성형 청크 전장 `WIBattleCharacterScaleArenaV6.prefab`을 참조합니다. 값이 있으면 런타임은 완성 프리팹을 사용하고, 값이 없을 때만 `arenaBackground` 단일 Sprite 폴백을 사용합니다.
- `WIBattleCameraController.GetClampedPosition`: 카메라 이동 가능 범위 계산에 `ArenaBackgroundSize`를 사용합니다. 전투 판정 경계인 `ArenaSize`와 시각적 대형 맵 탐색 범위를 분리합니다.
- `Ares_Battle_FullBody_V1`: 보존하는 고해상도 원본입니다. 전투에서는 이 파일을 직접 사용하지 않고 여기서 파생한 `Ares_Battle_1WU_A_OutlineBake_V1`을 사용합니다.
- `Ares_Battle_1WU_A_OutlineBake_V1`: 바깥 실루엣 외곽선을 포함해 190×256px로 정규화한 현재 전투용 Sprite입니다. Transform Scale 1, 256 PPU에서 약 0.742×1월드 단위로 표시하며 Mipmap 활성, Bilinear·무압축·Alpha Is Transparency를 사용합니다.
- `WIHeroDefinition.battleSprite`: 현재 30명 이상 전투 배치 R&D를 위해 전체 500명이 `Ares_Battle_1WU_A_OutlineBake_V1`을 임시 공유합니다. 이는 개별 캐릭터 이미지가 준비되기 전의 테스트 데이터이며 전용 배정·해제 에디터 메뉴로 관리합니다.
- `WIBattleTestLabWindow.StartThirtyVsThirtyBattleDensityTest`: 아레스와 커먼급 29명을 아군에, 별도 영웅과 중복 없는 커먼급 29명을 적군에 배정해 기존 테스트 세션 경로를 실행합니다. 검증 캡처는 `Assets/Screenshots/Battle_30v30_Density_FirstAttempt.png`와 `Battle_30v30_Density_ZoomOut_NoHUD.png`입니다.
- `Battle_Ground_NeutralDay_V2_4K`: `GroundExperiment_V2`에 보존된 4096×4096 실험용 중립 지면 Sprite입니다. 확대 상태의 지면 세부 검증을 위한 단일 화면 후보이며 현재 `WI_BattleConfig`와 전장 Prefab에서는 참조하지 않습니다. 반복 경계가 검증되지 않았으므로 타일 데이터로 취급하지 않습니다.
- `button_normal`: 공통 일반 UGUI 버튼 Sprite입니다. Single Sprite Border는 `{left: 28, bottom: 28, right: 28, top: 28}`이며 연결 Image는 Sliced를 사용합니다.
- `button_primary`: 공통 주요 UGUI 버튼 Sprite입니다. Border는 `{left: 28, bottom: 24, right: 28, top: 24}`이며 연결 Image는 Sliced를 사용합니다.
- `WIAdministrationWorldUGUI.prefab`: 전략 화면의 고정 UGUI 프리팹입니다. `WIAdministrationWorldUGUIBuilder`가 시안 기반 5영역 레이아웃과 60개 성 버튼을 편집기 시점에 생성하며 런타임에는 새 UI를 동적으로 만들지 않습니다.
- `strategy_avalon_crest_v1`: `Assets/Resources/UI/Generated`에 저장된 아발론 문장 Sprite입니다. 상단 진영 및 선택 성 패널에서 재사용하며 투명 배경, 중립 청회색 금속 색조를 사용합니다.
- `WIAdministrationWorldSnapshot.CastleHeroCards`: 선택 성의 주둔 영웅 중 최대 네 명의 표시 이름, 경험 기반 레벨 문자열, 초상 Sprite를 전달하는 읽기 전용 목록입니다.
- `WIAdministrationWorldSnapshot.MapConnections`: 인접 성 두 곳의 정규화 좌표, 표시색, 적대 전선 여부와 선택 경로 여부를 전달합니다. `WIAdministrationMapConnectionGraphic`이 이 목록을 하나의 UGUI 메시로 렌더링합니다.
- `WIAdministrationWorldSnapshot.CastleDetailRows`: 좌측 선택 성 패널의 영지관, 번영, 기술, 질서, 방어, 주둔 전투단 문자열을 순서대로 전달하는 읽기 전용 목록입니다. 원본 값은 `WICastleRuntimeState`와 현재 성에 위치한 `WIArmyState`에서 가져옵니다.
- `strategy_side_panel_v1`: 좌우 전략 정보 패널 공용 프레임입니다. 하단 일반 명령은 `strategy_command_button_v2`, 턴 진행은 `strategy_next_turn_button_v3`, 좌측 영웅 초상 슬롯은 `strategy_hero_card_v1`을 사용합니다.
- `strategy_top_settings_v1`: 전략 상단 우측 설정 메뉴의 512px 투명 톱니 Sprite입니다. `WIAdministrationWorldUGUI.prefab/WorldContent/TopHUD/TopIcon4`에서 표시하고 같은 영역의 `TopSystemButton`이 `OpenUGUISystem`을 호출합니다.
- `WIAdministrationWorldUGUI.prefab/WorldContent/CommandBar`: 112px 높이의 전략 하단 명령 영역입니다. 군사·인사·외교·계략·연구·평정·월보 7개 버튼과 `strategy_next_turn_button_v3` 기반 다음 턴 버튼으로 구성되며 설정은 상단 톱니 버튼에서 엽니다.
- `strategy_command_button_v2`: 시안 기반의 무문자 흑청색 일반 명령 프레임입니다. 512px 후처리 원본과 `{left:34,bottom:24,right:34,top:24}` Border를 사용합니다.
- `strategy_next_turn_button_v2`: 시안 기반의 무문자 남청색 다음 턴 프레임입니다. 양끝 금속 창날 장식을 보존하도록 `{left:92,bottom:24,right:92,top:24}` Border를 사용합니다.
- `strategy_next_turn_button_v3`: 새 하단 기준 시안의 비대칭 다음 턴 프레임입니다. 좌측 이중 화살촉과 우측 절삭 모서리를 가지며 `{left:88,bottom:24,right:42,top:24}` Border를 사용합니다.
