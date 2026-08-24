# ProjectWI 데이터 편집 매뉴얼

> R&D 중간 이미지와 생성 원본은 활성 데이터 폴더에서 분리해 `Assets/TrashAsset`에 보관하며, 현재 ScriptableObject가 참조하는 에셋만 활성 경로에 유지합니다.

현재 기획 기준은 `GameDesign.md`이며 전략 게임 데이터는 ScriptableObject에서 편집합니다. 방치형 던전 데이터는 사용하지 않습니다.

## 성 내정 공용 모달 셸

- `Assets/Resources/UI/Generated/administration_modal_shell_v1.png`은 성 내정 기능 모달 8종이 함께 쓰는 무문자 외곽 셸입니다.
- 셸에는 외곽 금속 프레임, 빈 제목 띠, 어두운 본문 배경만 둡니다. 화면별 문구·목록·버튼·장식은 각 프리팹의 자식 요소로 유지합니다.
- `WIAdministrationObjectiveUGUI`는 사용자가 직접 조정한 이미지·텍스트 배치를 보존하기 위해 이 공용 셸 적용 대상에서 제외합니다.
- 공용 외곽과 버튼 디자인은 각 완성 프리팹에 직접 저장합니다. 프리팹을 코드로 열어 공용 디자인을 다시 적용하고 저장하는 Editor 유틸리티는 사용하지 않습니다.

## Windows 테스트 패키징

- Unity 상단 메뉴 `ProjectWI > Build > Package Windows Test Build`를 실행합니다.
- 실행할 때마다 기존 `Builds/Windows`를 지우고 현재 Build Settings의 활성 씬으로 Windows x86-64 Development 빌드를 새로 만듭니다.
- 실행 파일은 `Builds/Windows/ProjectWI.exe`이며, 전달용 ZIP은 `Builds/Packages/ProjectWI-Windows-v버전-날짜시간.zip`에 생성됩니다.
- ZIP에는 실행 파일뿐 아니라 `ProjectWI_Data`, `UnityPlayer.dll`, Mono 런타임 등 실행에 필요한 Windows 빌드 폴더 전체가 포함됩니다.
- 빌드 실패 시 ZIP을 만들지 않으며 Console에 `[WI_PACKAGE_FAIL]`을 기록합니다. 성공 시 `[WI_PACKAGE_SUCCESS]` 로그와 완료 창을 표시하고 ZIP 위치를 엽니다.
- 자동 검증이 필요하면 새 빌드 후 `ProjectWI.exe -batchmode -nographics -wi-smoke-test -logFile SmokeTest.log`를 실행합니다.

## 턴 후속 UI 에셋 편집

- 턴 처리·캠페인 결과·튜토리얼·일반 안내는 `Assets/Prefabs/Administration/WIAdministrationTurnFollowupUGUI.prefab`을 공용으로 사용합니다.
- 전용 이미지는 `Assets/Resources/UI/Generated/turn_followup_*_v1.png`이며, 프레임·정보 패널·지도 나침반·완료 체크 배지로 분리되어 있습니다.
- 이미지에는 문구를 굽지 않습니다. 제목·설명·선택 문구는 `WIAdministrationTurnFollowupSnapshot`에서 TMP 라벨로 전달합니다.
- 튜토리얼의 기능 선택 순서는 0번 안내 확인, 1번 전체 건너뛰기입니다. 화면에서는 주요 행동인 0번을 오른쪽 청색 버튼, 보조 행동인 1번을 왼쪽 흑청색 버튼에 표시합니다.
- `TutorialContent`는 튜토리얼 모드에서만 활성화합니다. 캠페인 결과와 일반 안내에 튜토리얼 체크 문구를 재사용하지 않습니다.
- 턴 후속 선택 버튼은 전용 `turn_followup_button_normal_v1.png`, `turn_followup_button_primary_v1.png`을 원본 약 4:1 비율의 `Image.Type.Simple`로 사용합니다. 중앙 다이아와 모서리 장식이 있는 이미지이므로 9-Slice를 적용하지 않습니다.
- 설명 패널·튜토리얼 체크 패널·체크 행은 `turn_followup_info_panel_v1.png`을 사용합니다. 해당 Sprite는 장식 바깥 투명 여백을 4px만 둔 1955×509 이미지이며 사방 48px Border와 `Image.Type.Sliced`를 유지해야 합니다. 빌더 재실행 시에도 같은 설정이 적용됩니다.
- 선택 문자열의 첫 줄은 버튼 제목, 줄바꿈 뒤 둘째 줄은 버튼 위 설명으로 표시됩니다. 제목과 설명을 다시 한 TMP 안에 합치지 않습니다.
- 선택 설명 위 장식은 `turn_followup_choice_divider_v1.png`을 좌우에 각각 사용합니다. 중앙 다이아를 별도 오브젝트로 다시 만들지 않습니다.
- 닫기 버튼은 X까지 포함된 `turn_followup_close_button_v1.png` 단일 Sprite입니다. 닫기 TMP 문자는 비활성 상태를 유지합니다.
- 상단 설명 아이콘은 방패형 `turn_followup_header_emblem_v1.png`을 사용하고, 하단 튜토리얼 장식은 지도형 `turn_followup_map_compass_v1.png`을 사용합니다. 서로 역할과 표시 크기가 다르므로 한 이미지로 통합하지 않습니다.
- 두 장식은 각각 210×256과 768×680, 무압축, Mipmap 비활성, Bilinear가 기준입니다. 원본 크기를 무작정 키우지 말고 실제 표시 크기에 맞춘 사전 축소본을 유지합니다.

## 캠페인 목표 UI 에셋 편집

- 목표 상세 프레임과 전용 요소는 `Assets/Resources/UI/Generated/objective_*_v1.png`에 있습니다.
- `objective_confirm_button_v1.png`은 문구가 없는 버튼 배경이며 `목표 확인` 문구는 프리팹의 TMP에서 관리합니다.
- 진행도는 `objective_progress_track_v1.png` 위에 `objective_progress_fill_v1.png`을 Filled Image로 겹칩니다. 목표 수치를 바꿀 때 이미지를 다시 만들지 않고 `ProgressNormalized` 계산을 유지합니다.
- `objective_reward_strip_v1.png`에는 G/M/I 아이콘과 빈 프레임만 포함됩니다. 실제 보상 값은 목표 ScriptableObject의 `RewardGold`, `RewardMana`, `RewardInfluence`에서 각각 표시됩니다.

전투씬 에셋의 설정값과 제작·검증 절차는 `GameDocuments/BattleSceneAssetSettingsGuide.md`를 우선 기준으로 사용합니다. 아래 과거 하이브리드 청크 항목은 비교·복구용 기록이며 현재 전장 기준이 아닙니다.

- 숨은 8방향 사각 좌표망은 `WI_BattleConfig`의 `Use Hidden Grid`로 전환합니다. 현재 `Grid Cell Width=0.6`, `Grid Cell Height=0.3`, `Grid Arrival Distance=0.015`이며 기본 전투 화면에는 선을 표시하지 않습니다.
- 캐릭터 밀도를 조절할 때 Sprite Scale을 변경하지 말고 먼저 `Grid Cell Width`와 `Grid Cell Height`를 조정합니다. Cell Height를 낮추면 행 간격이 줄어 몸체 겹침이 커집니다.

- A 근거리 기준 아레스 전투 Sprite는 `Ares_Battle_1WU_A_OutlineBake_V1.png`입니다. 외곽선을 포함한 가시 실루엣 높이 256px, PPU 256, Transform Scale 1을 한 세트로 유지해야 정확히 1월드 유닛입니다. 파일에 투명 여백이나 외곽선을 추가하면 최종 가시 높이를 다시 256px로 정규화합니다.

- 공용 캐릭터 접지 그림자는 `Assets/Art/Battle/Effects/WI_CharacterShadow_Oval_V1.png`입니다. 기본 크기는 256×128, 256 PPU이며 캐릭터 체형에 따라 Transform X Scale만 조정합니다. 강도를 높일 때 Sprite 자체를 중복 배치하지 말고 SpriteRenderer 색상 알파를 조정합니다.
- 접지 그림자는 `WIBattleCharacter.prefab`의 미리 배치된 `GroundShadow` 자식에서 관리합니다. 런타임에 새 오브젝트를 만들지 않으며 `WIBattleCharacterView`가 본체의 Y 깊이 정렬보다 1 낮은 순서를 자동 적용합니다.

## 과거 전장 아트 보관

- V1~V5와 요새 청크 실험 전장 프리팹 및 전용 생성 코드는 제거했습니다.
- 실험에 사용한 이미지 원본과 청크는 `Assets/TrashAsset/Art/Battle`에 기존 상대 경로와 GUID를 유지해 보관합니다.
- 현재 편집 대상은 `Assets/Prefabs/Battle/WIBattleCharacterScaleArenaV6.prefab`입니다.

## 전투 2D 라이팅 편집

- 씬: `Assets/Scenes/BattleScene.unity`
- `Global Light 2D`는 중립 백색 전역광이며 현재 강도는 1입니다.
- `Battle Lighting/Arena Key Light 2D`와 `Arena Fill Light 2D`는 향후 노멀맵 검증용으로 보존하지만 현재 비활성입니다.
- 현재 지면은 Unlit 머티리얼을 사용하며 아레스와 환경물에는 전용 노멀맵이 없습니다.
- 화면 전체에 노란색이나 주황색 색조를 입히지 않으며, 그림자는 대형 장애물에 `ShadowCaster2D`를 별도로 배치하기 전까지 비활성 상태를 유지합니다.

## UGUI 캠페인 타이틀 편집

- 프리팹: `Assets/Prefabs/Administration/WICampaignTitleUGUI.prefab`
- MainScene의 `WICampaignTitleUGUI`는 위 프리팹 인스턴스이며 Canvas Scaler는 1920×1080 기준입니다.
- `CampaignPanel` 아래의 타이틀, 설명, 난이도 카드, 시작 조건 카드와 버튼은 모두 고정 RectTransform이므로 Scene View 또는 Prefab Mode에서 직접 이동·크기 조정할 수 있습니다.
- 카드 개수는 난이도 3개와 시작 조건 3개로 고정되어 있으며 표시 문구는 `WI_AdministrationDatabase.asset`의 정의에서 갱신됩니다.
- 버튼 이미지는 현재 고정 크기에서 원본 비율을 사용하는 `Image Type = Simple`입니다.
- 입력은 MainScene의 기존 `EventSystem`과 `InputSystemUIInputModule`을 공유합니다. 프리팹 안에 EventSystem을 추가하지 않습니다.

## UI 배경 에셋

- 목표 상세 모달은 `Assets/Resources/UI/Generated/objective_modal_frame_v1.png`을 전용 배경으로 사용합니다. 이미지에는 문구·수치·버튼이 포함되지 않으며 `WIAdministrationObjectiveUGUI.prefab`의 TMP와 Image가 제목, 현재 상황, 달성 조건, 진행도, 보상을 표시합니다. 디자인 변경은 해당 프리팹을 Prefab Mode에서 직접 편집합니다.

- 캠페인 난이도·시작 조건 카드는 미선택 시 `button_normal`, 선택 시 `button_primary` Sprite를 사용합니다. 원본 이미지 색을 유지하기 위해 Image Tint로 선택색을 만들지 않습니다. `ContinueCampaignButton`은 `button_normal`, `NewCampaignButton`은 `button_primary`를 사용합니다.

- 공통 주요 버튼 `Assets/Resources/UI/Generated/button_primary.png`은 512×128 Sprite이며 9-Slice border는 좌우 28px·상하 24px입니다. 버튼 크기를 변경할 때 `Simple` 스트레치 대신 `Sliced`를 사용해 외곽 장식 두께를 유지합니다.

- Background A Type: `Assets/Resources/UI/Generated/bg_type_a.png`
- Background B Type: `Assets/Resources/UI/Generated/bg_type_b.png`
- Background C Type: `Assets/Resources/UI/Generated/bg_type_c.png`
- Background D Type: `Assets/Resources/UI/Generated/bg_type_d.png`
- Background G Type: `Assets/Resources/UI/Generated/bg_type_g.png` — 567×292 가로형 무문자 정보 패널입니다. Unity에서는 Sprite/Single, Mipmap 비활성, Bilinear, Clamp, 기본 플랫폼 무압축과 사방 10px 9-Slice를 사용합니다. UI Image에 적용할 때 Type을 `Sliced`로 지정하면 패널 크기가 달라져도 얇은 외곽 프레임을 보존할 수 있습니다.
- 원본 가운데 패널만 1587×508로 크롭하고 모서리 바깥 영역을 투명 처리한 RGBA Sprite입니다.
- UI Toolkit에서 크기를 변경할 때 좌·우·상·하 3px 9-Slice를 유지합니다.
- 모서리는 2px만 사선으로 잘라 거의 직사각형이며 외곽에는 단일 2px 금속 테두리만 사용합니다.
- 최소 표시 크기는 6×6px보다 크게 사용합니다.
- 캠페인 첫 화면은 기존 `popup_panel.png`를 유지하며 Background A Type 적용 대상에서 제외합니다.
- 현재 행정 UI의 주요 패널 컨테이너와 전투 HUD 정보 패널은 이 에셋을 직접 참조하며, 버튼·진행바·이미지 슬롯·상태 선택 카드는 각 용도의 기존 스타일을 유지합니다.

## 1. 전략 데이터베이스

에셋: `Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset`

- `Factions`: 진영 이름, 색상, 플레이어 여부, AI 성향과 초기 금화·마나·영향력
- `Castles`: 60개 성의 소유 진영, 지도 좌표, 인접 성, 규모와 번영·기술·질서·방어
- `Heroes`: 등급, 종족, 클래스, 특기, 통솔·무력·지력·정치·매력과 영입 조건
- `HeroClassDefinitions`: 12개 클래스의 표시명, 설명, 주·보조 능력치와 권장 전투 역할
- `CampaignObjectives`: 캠페인 목표의 시작 정세, 조건, 대상 성, 목표 수치와 완료 보상
- `StartingRelationships`: 진영별 핵심 인물 두 명, 시작 관계 단계와 관계 배경
- `RegionalEventDefinitions`: 원래 진영, 대상 수도, 최소 턴과 두 선택지의 자원·성 수치 변화
- `OccupationChoices`: 점령 직후 회유·군정·현지 자치의 비용, 성 수치와 불안 기간
- `FactionEliminationNarratives`: 다섯 진영의 마지막 영토 상실 제목과 설명
- `CampaignEndings`: 점령 통치 이력으로 판정하는 화합·군정 통일 결말의 제목과 설명
- `CampaignVariants`: 시작 영토 또는 핵심 관계만 변경하는 반복 플레이 시작 조건

시작 변형은 `Classic`, `BorderGarrison`, `DividedCourt` 세 종류입니다. 국경 수비대는 `castle_01`을 아발론 소유로 바꾸며, 분열된 궁정은 아레스·알덴의 친애를 갈등으로 덮어씁니다. 난이도와 시작 자원은 변형과 무관합니다.

캠페인 시작 화면은 940px 폭과 최소 650px 높이의 고정 패널을 사용합니다. 캠페인 소개는 제목 장식 아래에서 난이도 제목 바로 위에 배치하고, 난이도·시작 조건 카드의 설명은 여러 문장일 때 마침표 뒤에서 줄을 바꿉니다. 난이도 안내 문구는 하단 프레임 안쪽 여백을 확보합니다.

점령 통치 선택 ID는 저장 상태의 이력에 누적됩니다. 회유와 현지 자치 합계가 군정보다 같거나 많으면 `Concord`, 군정이 더 많으면 `Dominion` 결말입니다. 이력이 없는 구버전 캠페인은 화합 결말을 사용합니다.

플레이어가 적 성을 점령하면 구 소유 진영을 기억하는 점령 통치 사건이 생성됩니다. 선택 전 기본 상태는 질서 최대 20·불안 3개월이며 선택에 따라 비용과 안정화 방향이 달라집니다. AI 점령은 선택 사건 없이 기본 규칙을 사용합니다.

지역 사건은 아발론·발도르·아이언하트·실바니아·네크로폴리스의 대표 수도에 하나씩 있습니다. 플레이어가 해당 성을 소유하고 최소 턴을 넘기면 미발생 사건 한 건이 월간 보고에 등록되며, 완료 ID를 저장해 반복 발생을 막습니다. 자원 비용을 감당할 수 없는 선택지는 비활성화됩니다.

새 캠페인은 5개 진영에 핵심 관계 한 쌍씩을 생성합니다. 아레스·알덴과 브롬·테인은 친애, 리리아·가레스·모리건·님·테론·베일은 갈등으로 시작합니다. 두 인물이 플레이어 소유의 같은 성에 있으면 관계 단계에 맞는 선택 사건 후보가 생성됩니다.

첫 목표 `avalon_restore_capital`은 아발론 성의 번영을 시작값 45에서 50 이상으로 높이는 조건입니다. 완료 시 금화 200, 마나 50, 영향력 20을 지급하며 완료 ID는 저장 데이터에 유지됩니다.

목표는 데이터 배열 순서대로 수도 재건, 아발론 영토 3성 확보, 발도르 멸망, 60성 대륙 통일로 진행됩니다. 각 단계는 `CastleProsperity`, `PlayerCastleCount`, `FactionEliminated`, `ContinentalUnification` 조건을 사용하며 선행 달성 상태도 현재 단계가 되면 판정합니다.
- `Starting Heroes`: 시작 성, 영웅과 영지관 여부
- `Special Facilities`: 확장 시 선택하는 특화 시설 이름, 설명과 교체용 아이콘
- `UI Strings`: UID별 한국어·영어 문구

성관, 시장, 훈련소와 선술집은 모든 성의 기본 기능이며 자유 건설 목록에 넣지 않습니다. 특화 시설은 성 확장 완료 시 선택합니다.

60개 성의 정식 한글·영문 이름, 지형 특성과 전문 분야는 `AdministrationDesign.md`의 성 표를 기준으로 합니다. `ProjectWI/Data/Seed Castle Names And Lore` 메뉴는 이 표를 읽어 `Display Name`, `Terrain Trait`, `Specialty`와 고유 UID를 영지 관리 ScriptableObject에 기록합니다.

전문 분야의 실제 효과는 각 성의 `Specialty Effect Type`, `Specialty Project Type`, `Specialty Effect Value`에서 편집합니다. 공통 유형은 사업 성과, 월간 금화, 월간 마나, 월간 영향력, 전략 방어의 5종이며 현재 60개 성은 각각 18·9·11·12·10개로 분류되어 있습니다. 사업 성과형은 지정 사업에 +2, 금화형은 월 +12, 마나형은 월 +8, 영향력형은 월 +3, 방어형은 전략 방어 전력 +20을 적용합니다. 플레이어와 AI는 같은 값을 사용하며 별도 저장값 없이 마스터 데이터에서 매 턴 계산합니다.

## 2. 진영 경제와 AI

각 진영은 독립된 금화·마나·영향력을 보유합니다. 사업은 금화 100, 적대 원정은 영향력 20을 실제로 소비하며 AI에 숨은 자원을 지급하지 않습니다. AI 복수 전투단에는 해당 진영 소속 유휴 지휘관이 필요합니다.

AI 성향은 부국, 개발, 수비, 공세와 모략입니다. 전선 판단에는 **실제로 전쟁 중인 진영의 인접 성 수**, 접근 중인 적 전투단, 성 방어와 질서를 사용합니다. 중립·우호·불가침·동맹 진영의 접경은 적대 위협으로 계산하지 않습니다.

`AI War Pressure Interval Months`는 장기 평화 상태를 다시 평가하는 주기이며, `AI Minimum Active War Fronts`는 유지할 최소 활성 전선 수입니다. 평가 시점에 활성 전선이 부족하면 접경 규모와 양측 AI 공세 성향을 점수화해 인접 AI 진영 사이에 신규 전쟁을 생성합니다. 기본값은 18개월과 2개 전선입니다. 일괄적인 초반 공격 금지는 동시 침공 구조를 깨고 균형형·공세형 플레이어의 조기 멸망을 유발해 채택하지 않았습니다.

`Difficulty Definitions`에는 여유·표준·도전 난이도의 표시 문구와 `AI Candidate Window`를 저장합니다. 난이도는 플레이어·AI 자원이나 성공률에 보너스를 주지 않습니다. 여유는 상위 3개, 표준은 상위 2개, 도전은 최상위 1개 담당 후보 중에서 AI가 선택하며 선택한 난이도는 캠페인 저장 상태에 포함됩니다.

## 3. 영지 관리와 인물 런타임

중점 사업, 진영 방침, 영지관 위임, 인물 활동, 관계, 공훈, 명성, 피로와 부상은 캠페인 런타임 상태입니다. 결과 수치는 담당 인물 능력치와 투자 등급을 반영하며 0~100 범위로 제한합니다.

전투 포로 기준은 전략 데이터베이스의 `Capture Power Margin`, 억류 기간은 `Capture Duration Months`에서 편집합니다. 현재는 전력 차이 50 이상의 대패에서 질서 있는 후퇴를 하지 못한 패배 전투단원 한 명이 3개월간 포로가 됩니다. 일반 인물을 우선하며 같은 조건에서는 피로가 높은 인물을 선택합니다. 포로는 전투단과 성에서 제거되고 모든 임무에 배정할 수 없으며 기간 후 원래 진영으로 귀환합니다.

`Permanent Death Enabled`는 현재 꺼져 있습니다. 고유·일반 인물 로스터와 계승 규칙이 확장되기 전까지 전투 불능으로 인한 영구 사망은 발생하지 않습니다.

전투 후 수치는 `Battle Victory Merit/Experience/Fatigue`, `Battle Defeat Experience/Fatigue`, `Orderly Retreat Fatigue`, `Battle Injury Power Margin/Months`에서 편집합니다. 현재 승리는 공훈 +10·경험 +15·피로 +15, 일반 패배는 경험 +8·피로 +25이며 전력 차이 30 이상이면 부상 1개월, 질서 있는 후퇴는 경험 +8·피로 +12와 부상·포로 방지입니다. `Battle Bond Victory Threshold`는 같은 전투단 인물의 관계 발전에 필요한 공동 승리 횟수이며 현재 2회입니다.

`Relationship Event Definitions`는 갈등·친애 관계 단계에서 발생할 사건의 제목, 설명과 두 선택지를 저장합니다. 각 선택지는 관계 단계 이동, 두 인물의 공훈과 피로 변화를 가집니다. 같은 플레이어 성에 머무는 인물 쌍을 월말에 검사하며, 완료 키를 저장해 같은 사건과 인물 조합이 반복 발생하지 않게 합니다.

## 4. 전투단과 전투 세션

전투단은 병사가 아닌 `[영웅]`과 `[일반]` 인물로 구성합니다. 대장, 전위, 근접, 원거리, 마법과 지원 역할을 사용합니다. 이동, 숙련, 보급, 점령 불안, 후퇴와 재편성도 캠페인 상태에 저장합니다.

전투 발생 시 `WIBattleSessionState`에 전장 성, 공격·수비 진영, 참가 전투단과 인물, 양측 전력 스냅샷을 저장합니다. 전략 자동 판정과 실시간 전투는 동일한 `SubmitBattleResult()` API를 사용하며 완료된 세션에는 결과를 다시 제출할 수 없습니다.

## 5. 실시간 전투 설정

에셋: `Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset`

- 전장 크기와 진형 열·행 간격
- 기본 체력·마나와 무력·지력 환산값
- 이동 속도, 근접·원거리 사거리, 공격 간격과 기본 피해
- 전진 속도 배율, 위치 사수 피해 감소율, 집중 공격 피해 배율
- 인물 최소 간격과 충돌 해소 강도
- 근접 밀치기 거리와 위치 사수 밀치기 저항
- 진형 이탈 후 복귀 속도
- `Hero Skills`: 영웅 ID, 표시명, 범위 피해·아군 회복·지휘 강화 유형, 마나 비용, 위력, 범위와 재사용 대기시간
- `Placeholder Sprite`: 실제 캐릭터 이미지가 준비되면 교체하는 빈 슬롯
- 임시 캐릭터·발사체 크기, 공격 효과 지속 시간과 양 진영 임시 색상
- 실제 발사체 속도·수명·충돌 반경, 아군 오발 사용 여부와 발사자 주변 안전 거리
- 전투 카메라 이동·휠 확대 속도, 최소·최대 배율과 캐릭터 클릭 반경

에디터의 `ProjectWI/Tools/Battle Test Lab`은 이 데이터베이스의 전체 인물 원본과 전투 설정을 그대로 사용합니다. 테스트 편성은 임시 전투 세션으로만 생성되며 ScriptableObject와 캠페인 저장 데이터를 수정하지 않습니다.

`ProjectWI/Verification/Run Battle Performance Benchmark`는 20·40·60명 전투를 각각 600스텝 실행합니다. 목표와 최신 측정 결과는 `BattlePerformanceReport.md`에서 관리합니다.

`Placeholder Sprite`가 비어 있으면 영웅은 원형, 일반 인물은 사각형 절차형 스프라이트를 자동 생성합니다. 전장 배경도 격자 도형으로 생성하므로 외부 아트가 없어도 전투 위치·진형·공격 효과를 확인할 수 있습니다.

원거리·마법·지원 역할은 사거리 안에서 발사체를 생성하며 피해는 발사 시점이 아니라 이동 선분이 인물과 충돌한 시점에 적용됩니다. 기본 설정은 아군 오발 비활성화입니다. `Projectile Friendly Fire Enabled`를 켜면 안전 거리를 지난 발사체가 같은 진영과 충돌할 수 있습니다.

고유 영웅 8명은 각각 하나의 액티브 스킬을 가집니다. 범위 피해는 범위 안 적군, 회복과 지휘는 범위 안 아군만 대상으로 합니다. 지휘 스킬의 `Power`는 아군의 일반 공격 대기시간 감소량으로 사용하며 모든 스킬은 마나와 재사용 대기시간을 검사합니다. `Skill Visual Duration`은 적색 피해·녹색 회복·금색 지휘 범위 도형의 유지 시간입니다.

## 6. 전략·전투 씬 전환

`MainScene`의 `CampaignRuntime`은 `DontDestroyOnLoad`로 캠페인 상태와 진행할 전투 세션 ID를 보존합니다. 플레이어 참가 전투는 `Pending` 상태를 유지하며 군사 화면의 `전투 시작` 버튼으로 `BattleScene`에 진입합니다. 전투 종료 시 같은 세션에 결과를 제출하고 `MainScene`으로 복귀합니다.

두 씬은 `ProjectSettings/EditorBuildSettings.asset`에 등록되어 있어야 합니다.

## 7. UI와 프리팹 연결

- 영지 관리 프리팹: `Assets/Prefabs/Administration/WIAdministrationUI.prefab`
- 전투 캐릭터 프리팹: `Assets/Prefabs/Battle/WIBattleCharacter.prefab`
- 영지 관리 UI 조립 루트: `Assets/UI/Administration/WIAdministration.uxml`
- 영지 관리 화면 템플릿: `Assets/UI/Administration/Views/`의 HUD·전략 지도·영지 관리·오버레이 UXML
- 전투 HUD: `Assets/UI/Battle/WIBattleHUD.uxml`
- 공통 Panel Settings: `Assets/UI/Administration/WIAdministrationPanelSettings.asset`

이미지 슬롯은 실제 에셋이 준비될 때 Sprite 또는 UI Toolkit 배경 이미지로 교체합니다.

`WIAdministration.uxml`에는 화면 내용 전체를 직접 넣지 않고 네 개의 화면 템플릿을 조립하는 구조만 둡니다. 화면 요소를 수정할 때는 `Views`의 해당 UXML을 열고, 동작을 수정할 때는 `WIAdministrationUIController`의 기능명과 일치하는 partial 파일을 우선 확인합니다.

## 8. 저장 파일

저장 파일은 `Application.persistentDataPath` 아래 `ProjectWI_Save_0.json`부터 `ProjectWI_Save_3.json`까지 생성합니다. 0번은 턴 종료 및 전투 복귀 자동 저장이고 1~3번은 시스템 메뉴의 수동 슬롯입니다. JSON 최상위에는 저장 버전, UTC 저장 시각과 캠페인 상태가 들어갑니다. 저장 시 `.tmp` 파일을 먼저 완성한 뒤 대상 슬롯을 교체합니다.

현재 버전보다 높은 저장 파일과 손상된 JSON은 불러오지 않습니다. 컬렉션이 누락된 이전 버전 데이터는 비어 있는 목록으로 정규화합니다.

## 9. 시스템 설정

기본값은 `Assets/Data/ScriptableObject/System/WI_SystemSettingsConfig.asset`에서 편집합니다. 전체·음악·효과음 음량, 전체 화면, 목표 FPS와 기본 표시 언어를 설정할 수 있습니다. 사용자가 적용한 값은 캠페인 슬롯과 별도로 `ProjectWI_SystemSettings_V1` PlayerPrefs JSON에 저장되어 모든 캠페인에서 공유됩니다.

현재 전체 음량은 `AudioListener.volume`에 즉시 적용합니다. 음악과 효과음 값은 개별 볼륨 그룹 데이터로 보존하며 실제 AudioMixer가 연결되는 단계에서 각 그룹에 적용합니다.

## 10. 연구 데이터

전략 데이터베이스의 `Research Definitions`에서 연구 ID, 이름·설명, 마나 비용, 필요 성 기술, 기간, 효과 유형·수치와 선행 연구 ID를 설정합니다. 연구 효과는 금화 비율, 성별 마나·영향력 수입, 중점 사업 성과와 전투단 전투력 중 하나입니다. 연구 담당자는 플레이어 진영 성에 있는 유휴 인물이어야 하며 연구 기간 동안 다른 임무를 맡을 수 없습니다.

AI도 같은 연구 목록, 기술 조건, 선행 연구와 진영 마나를 사용합니다. 부국·개발·수비·공세·모략 성향에 맞는 효과를 우선하지만 조건을 충족하지 못하면 시작하지 않습니다.

## 11. 작위 데이터

`Title Definitions`에서 작위 ID, 이름, 필요 공훈, 영향력 비용, 영지 사업 보너스와 전투단 전투력 보너스를 설정합니다. 작위는 해당 진영 소속으로 영입된 인물에게만 수여할 수 있으며 수여 시 충성 상태가 안정으로 회복됩니다.

## 12. 일반 인물과 영웅 승격

`Heroes` 목록은 고유 영웅뿐 아니라 `Grade`가 `Common`인 일반 인물도 함께 관리합니다. 현재 가레스, 미라, 테인, 님, 라스카, 베일 6명이 등록되어 있으며 초상화 슬롯은 실제 이미지 교체 전까지 비어 있습니다. 일반 인물은 고유 액티브 스킬을 갖지 않지만 탐색, 영입, 영지 관리 배치와 전투단 편성에는 영웅과 같은 인물 단위로 참여합니다.

승격 기준은 데이터베이스의 `Promotion Required Merit`, `Promotion Required Reputation`, `Promotion Influence Cost`에서 조정합니다. 현재 기본값은 공훈 80, 명성 30, 영향력 30입니다. 일반 인물이 참가한 전투에서 승리해 특별 성취를 얻고 수치 조건을 충족하면 다음 턴 결과에 승격 후보로 등록됩니다. 승격을 확정하면 원본 등급은 보존한 채 런타임의 `Promoted To Hero`가 활성화되어 저장·불러오기 후에도 영웅으로 표시됩니다.

## 13. 인물 이동과 영지관

개별 인물 이동은 `Character Transfers`에 출발 성, 목적지 성과 남은 개월을 저장합니다. 같은 진영의 인접 성만 선택할 수 있고 목적지의 인물 슬롯에는 진행 중인 이동 예약도 포함됩니다. 기본 이동 기간은 한 달이며 이동 중에는 연구, 사업, 개인 활동, 의뢰와 전투단에 중복 배정할 수 없습니다.

영지관은 해당 성에 주둔한 유휴 인물만 임명할 수 있습니다. 영지관직은 상시 직책이므로 연구 등 다른 월간 임무를 맡을 수 있지만, 그달의 위임 사업은 실행되지 않습니다. 개인 이동을 하려면 먼저 영지관을 해임해야 하며 영지관이 포함된 전투단이 원정하면 영지관직과 위임 설정이 자동 해제됩니다.

AI 진영은 장기 캠페인에서 영지관과 전투단 대장이 같은 한 명에게 영구 점유되지 않도록 각 수도에 일반 인물 한 명을 추가 배치합니다. 발도르에는 가레스, 아이언하트에는 테인, 실반로드에는 님, 네크로폴리스에는 베일이 시작 인물로 배치되며 영지관은 기존 고유 영웅이 유지됩니다.

## 14. 시각 자산

전략 데이터베이스의 `Global Map Image`에는 전략 지도 전체 배경 Sprite를 지정합니다. 현재 `Assets/Art/Maps/ProjectWI_GlobalMap_V1.png`가 연결되어 있습니다. 노드와 연결선은 이미지와 별개로 정규화 좌표를 사용하므로 지도 이미지를 교체해도 전략 데이터는 유지됩니다.

현재 성 좌표와 인접 경로는 `ProjectWI/Data/Assign Geographic Castle Layout` 편집기 메뉴로 다시 생성할 수 있습니다. 아이언하트는 북부 산악, 실반로드는 서부 숲, 발도르는 중앙·동부, 네크로폴리스는 남부 화산 지대에 배치되며, 진영 내부는 근거리 도로망으로 연결되고 진영 간 이동은 지정된 국경 관문을 통합니다.

진영의 `Emblem`, 인물의 `Portrait`, 성의 `Castle Image`, 특화 시설의 `Icon`도 같은 데이터베이스에서 관리합니다. Sprite가 지정되면 전역 HUD 문장, 성 전경, 영지관 영역, 주둔 인물 슬롯과 시설 슬롯에 자동 적용됩니다. 값이 비어 있으면 기존 빈 이미지 슬롯이 유지됩니다.

현재 5개 진영의 `Emblem`에는 `Assets/Art/Factions` 아래 투명 PNG 문장이 연결되어 있습니다. 아발론은 왕관을 쓴 백사자와 마나 결정, 발도르는 검을 쥔 흑수리, 아이언하트는 산맥과 전쟁망치, 실반로드는 초승달 세계수, 네크로폴리스는 일식과 영혼불꽃 해골을 상징으로 사용합니다.

5대 진영 수도 `castle_00`, `castle_01`, `castle_26`, `castle_38`, `castle_50`의 `Castle Image`에는 `Assets/Art/Castles` 아래 전용 전경이 연결되어 있습니다. 이후 제작하는 일반 성 전경도 같은 필드에 단일 Sprite로 지정합니다.

나머지 55개 성에는 `Assets/Art/Castles/Common`의 진영·지형별 공용 전경이 연결되어 있습니다. 초기 핵심 인물 초상화는 `Assets/Art/Characters`, 특화 시설 8종의 아이콘은 `Assets/Art/Facilities`에서 관리합니다. 현재 500명 검증용 로스터에서 초상화가 비어 있는 인물은 UI의 빈 이미지 슬롯을 사용하며 추후 Sprite 참조만 연결하면 교체됩니다. 원본 생성 아틀라스는 재가공을 위해 `Assets/Art/SourceAtlases`에 보존합니다.

현재 `WIHeroClass`는 마검사·수호자·성기사·검성·궁수·암살자·대마법사·사제·드루이드·전략가·연금술사·흑마도사의 12종입니다. 현재 데이터베이스에는 고유 등급 100명과 일반 등급 400명, 총 500명이 정의되어 있습니다. 이 수량은 자동 검증과 대규모 로스터 편집을 위한 기계적 확장 상태이며, 서사·관계·특기 차별화와 초상화는 별도 수동 큐레이션 대상입니다.

각 클래스는 `HeroClassDefinitions`에서 주 능력치, 보조 능력치와 권장 전투 역할을 조정합니다. 전투 데이터의 `ClassSkills`에는 피해·회복·지휘 계열 공용 기술 12종이 있으며 일반 인물은 해당 기술을, 고유 영웅은 `HeroSkills`의 전용 기술을 우선 사용합니다.

## 15. 외교 상태

캠페인의 `Diplomatic Relations`는 모든 진영 쌍에 대해 전쟁, 중립, 우호, 불가침, 동맹 중 하나의 상태를 저장합니다. 신규 캠페인은 아발론과 발도르만 전쟁 상태로 시작하고 나머지 관계는 중립으로 시작합니다. 구버전 저장 파일은 불러올 때 누락된 진영 관계를 자동으로 보충합니다.

## 16. 첩보 데이터

`Scheme Definitions`에는 조사, 방첩, 유언비어와 인재 이간의 영향력 비용, 기본 성공률, 효과량, 지속 개월, 기본 발각률과 실패 시 발각 가산치를 설정합니다. 성공률은 담당 인물의 지력에 올라가고 대상 성의 질서와 방첩 상태에 내려가며 10~90% 범위로 제한됩니다. 발각률은 대상 성 질서·방첩과 실패 가산치에 올라가고 담당 인물 지력에 내려가며 5~90% 범위로 제한됩니다. 아군 성에서 수행하는 방첩은 판정 없이 성공하고 발각되지 않습니다.

모든 첩보는 영향력을 출발 시 소비하고 `Scheme Missions`에 1개월 임무로 저장합니다. 담당 인물은 판정 전까지 다른 사업·연구·이동·전투단·활동에 배정할 수 없으며, 다음 턴에 저장된 판정값으로 성공과 발각을 각각 해결한 뒤 자동 복귀합니다. 발각된 적대 첩보는 동맹→불가침→우호→중립 순서로 외교 관계를 한 단계 악화시키며 이미 전쟁 또는 중립이면 상태를 유지합니다. 조사 정보와 방첩의 남은 기간은 캠페인 상태에 저장되고 매월 감소합니다. 유언비어는 적 성 질서를 낮추며 인재 이간은 적 인물의 충성을 안정, 동요, 위험 순서로 흔듭니다. 모략 성향 AI도 같은 예약·비용·점유·성공·발각 규칙을 사용합니다.

친선 및 휴전 교섭은 금화 100과 영향력 15, 불가침은 영향력 25, 동맹은 영향력 40, 선전포고는 영향력 10을 사용합니다. 동맹 원조는 금화 200을 실제 진영 경제 사이에서 이동시키고 6개월의 재요청 대기 시간을 가집니다. 다른 진영의 성으로 원정하려면 두 진영이 전쟁 상태여야 합니다.

포로 몸값은 포로 발생 직후 생성되는 교환 요청을 사용합니다. 기본 금화 요구액은 데이터베이스의 `Prisoner Ransom Gold`(현재 150G)이며, 전투 판정에 따라 같은 가치 기준의 마나를 요구할 수도 있습니다. 원소속 진영이 요구 자원을 지불하면 포획 진영으로 실제 이전되고 포로는 즉시 원소속 성으로 귀환합니다. 양측이 서로 포로를 보유하면 자원 없이 한 명씩 맞교환할 수 있습니다. 공동 공격은 동맹 양측이 모두 교전 중인 제3진영의 성만 대상으로 하며 `Joint Attack Influence Cost` 20과 `Joint Attack Duration Months` 3을 사용합니다. 약속의 대상과 남은 기간은 외교 관계 상태에 저장되고 매월 감소합니다.

전투 운명 확률은 난이도 정의의 `Battle Death Chance`, `Battle Capture Chance`, `Battle Defection Chance`에서 편집합니다. 남은 확률은 후퇴입니다. 여유 난이도는 세 값이 모두 0이라 항상 후퇴하고, 인물의 `Battle Traits`에 지정하는 생존가(`Survivor`)는 사망 가중치를 1/4로, 탈출가(`Elusive`)는 포로 가중치를 1/4로 줄이며 불굴(`Unyielding`)은 적 합류를 금지합니다.

## 17. 자원 고갈과 월 수입

진영의 다음 달 기본 수입은 보유 성별 수입과 완료 연구 보너스의 합계입니다. 성 수입은 번영·성 규모·안정도로 금화를, 기술·성 규모로 마나를, 안정도·성 규모로 영향력을 계산합니다. 금화와 영향력은 성마다 최소 1을 보장하며 현재 초기 성 데이터에서는 마나도 양수이므로 자원 0 상태가 영구적인 진행 불능으로 이어지지 않습니다.

`WIAdministrationTurnSystem.GetFactionMonthlyIncome`은 실제 턴 처리와 같은 계산을 사용해 HUD 예상치를 제공합니다. 모든 비용 명령은 잔액을 먼저 검사하며 부족하면 실행하지 않아 자원이 음수가 되지 않습니다.

## 18. 인물 임무 점유

`WIAdministrationState.IsCharacterBusy`는 진행 중 사업, 개인 활동, 선술집 의뢰, 첩보 임무, 전투단 소속, 연구와 이동을 하나의 점유 규칙으로 검사합니다. 점유 중인 인물은 다른 담당 후보 목록에서 제외되며 시스템 API도 중복 연구·이동·전투단 배정을 거부합니다.

개인 활동은 월간 턴 처리 후 대기 상태로 돌아옵니다. 사업·연구·의뢰·이동은 각 남은 기간과 완료 규칙에 따라 해제되고, 전투단 소속은 전투단 해산 또는 구성원 제외 전까지 유지됩니다. 영지관직 자체는 월간 점유로 계산하지 않지만 그달 위임 사업과 다른 임무가 겹치면 위임 사업은 실행되지 않습니다.

AI 판단 근거는 최근 `WITurnSummary.AIReasonReports`에만 저장합니다. 사업·연구·군사·외교·첩보 분야별로 같은 진영의 보고는 한 턴에 한 건만 남기고 전체 12건으로 제한하여 장기 캠페인에서 기록이 누적되지 않습니다. 연구 보고는 선택 후보와 차순위의 성향 적합 점수를 함께 기록하며, 사업과 군사는 취약 수치·전선 위협도·공동 공격 여부를 구체적인 수치 또는 대상으로 설명합니다.

`WICampaignAutoPlayer`는 재미 검증용 에디터 자동 실행기입니다. 성장형·균형형·공세형 정책으로 24·60·120개월을 진행하며 대기 전투와 사업·관계·지역·점령·영입·영웅 흔적 선택을 모두 해결합니다. 이 실행기는 전투 수, 승패, 원정, 소유권 변화, 최종 영토와 미해결 항목을 기록하지만 감정적 재미 점수를 판정하지 않습니다.

UI 후속 작업은 `GameDocuments/UIHandoffReport.md`와 `GameDocuments/UIConcepts`의 전략 지도·영지 관리 시안 두 장을 기준으로 합니다. UI 계층은 UXML에 정적으로 유지하고 C#에서는 데이터와 Sprite만 바인딩합니다. 기존 생성 이미지는 삭제하지 않습니다.

캠페인 난이도의 `AI Candidate Window`는 AI가 점수순 상위 몇 후보까지 검토하는지를 결정합니다. 여유는 3, 표준은 2, 도전은 1이며 자원·수입·비용 보너스는 제공하지 않습니다. 사업 담당자, 연구, 인접 공격 성, 첩보 대상이 공통 선택 함수를 사용하고 턴과 고정 소금값으로 항상 재현 가능한 결과를 냅니다. 도전은 항상 1순위, 표준과 여유는 각 후보 범위 안에서 상황에 따라 차순위를 선택할 수 있습니다.

진영 상태의 `Eliminated`와 `Eliminated Turn`은 영토가 0개가 된 최초 턴을 저장합니다. 멸망 시 진행 연구, 잔존 전투단과 전투 세션, 발신 첩보, 공동 공격과 원조 대기 상태를 정리하고 소속 인물은 비영입 상태로 전환합니다. 멸망 진영이 억류하던 타 진영 포로는 원소속으로 즉시 귀환합니다. 멸망 처리는 턴 시작과 전투 해결 직후 검사하되 최초 한 번만 월간 보고 사건을 만듭니다.

전투에서 전투단장이 포로가 되면 남은 전투단원 중 통솔이 가장 높은 인물이 즉시 전투단장을 승계합니다. 남은 전투단원이 없으면 해당 전투단은 해산되어 지휘관 없는 전투단이 장기 상태에 남지 않습니다.

## 19. 첫해 안내 데이터

전략 데이터베이스의 `Tutorial Definitions`에서 안내 ID, 노출 월, 한글·영문 제목과 본문을 설정합니다. 현재 1·2·3·6·12월에 대륙, 영지 관리, 중점 사업, 연구와 군사 안내가 등록되어 있습니다. 런타임의 `Completed Tutorial Ids`와 `Tutorial Skipped`가 확인·선행 실행·전체 건너뛰기 상태를 저장하며 구버전 저장 파일은 불러올 때 빈 목록으로 복구됩니다.

## 20. 캠페인 승패 데이터

전략 데이터베이스의 `Campaign Rules`에서 대륙 전체 성 점유 승리, 플레이어 영토 소멸 패배 사용 여부와 한글·영문 결과 문구를 설정합니다. 현재 기본값은 두 조건 모두 활성화입니다. `WICampaignResultSystem`은 턴 종료 후 진행 중인 캠페인만 한 번 판정합니다.

런타임에는 `Campaign Result`, 최초 `Campaign Result Turn`과 `Campaign Result Acknowledged`를 저장합니다. 결과가 난 뒤 계속 보기를 선택해도 승패를 다시 판정하거나 덮어쓰지 않으며 저장·불러오기 후 동일하게 유지됩니다.

## 21. 중점 사업 밸런스 데이터

전략 데이터베이스의 `Project Balance`에서 기본·집중 비용, 확장 기본 비용, 기본 성과, 능력치 나눗수, 집중·특기·방침 보너스, 최소·최대 성과와 확장 기간을 설정합니다. 현재 값은 일반 기본 100G, 일반 집중 180G, 확장 기본 600G, 기본 성과 4, 능력치 20당 +1, 집중 +4, 특기 +2, 방침 +2, 성과 범위 4~16, 확장 3개월입니다.

확장 집중 비용은 일반 집중/기본 비용 비율을 적용해 1,080G로 계산합니다. 집중 투자는 기본보다 한 달 성과가 높지만 금화당 성과는 조금 낮게 설정했습니다. 수동 사업 지출은 `Pending Player Gold Spent`에 누적되어 다음 턴 월간 보고의 실제 지출로 기록됩니다.

## 22. 특기 데이터

`Trait Definitions`에서 특기 열거형, 한글·영문 이름과 설명, 적용 중점 사업 목록을 설정합니다. 적용 사업을 맡으면 `Project Balance`의 특기 보너스인 +2가 예상 성과와 실제 결과에 반영되고 관련 사업 사건의 영웅 선택지가 열립니다.

현재 농정가·상인은 번영, 건축가는 요새·확장, 질서관은 안정, 학자는 기술, 교섭가는 인재, 의사는 회복, 교관은 훈련 사업에 연결됩니다. 데이터 검사에서 상인 보유자가 없던 문제를 수정해 일반 인물 미라에게 상인 특기를 배정했습니다.

각 특기는 일치 사업의 성과 +2와 별도의 고유 결과를 가집니다. 농정가는 주둔 전투단 보급 회복, 상인은 금화 50, 건축가는 공사비 15% 환급, 질서관은 방첩 2개월, 학자는 진행 연구 1개월 단축, 교섭가는 영향력 10, 의사는 부상 1개월·피로 10 회복, 교관은 주둔 전투단 숙련 경험 15를 제공합니다. 수치는 `Trait Definitions > Unique Effect Value`에서 조정합니다.

`Hero Legacy Definitions`에는 번영·기술·안정·요새·인재·훈련·회복·확장 8개 사업별 영웅의 흔적 이름, 설명과 사업 성과 보너스를 저장합니다. 성에는 활성 흔적을 두 개까지 유지합니다. 세 번째 흔적을 선택하면 교체한 흔적은 `Commemorated Hero Legacies`로 이동하여 역사 기록은 남지만 효과 계산에서는 제외됩니다.

`Recruitment Event Definitions`는 신뢰 증명, 공훈 증명과 진영의 미래 요구 사건을 저장합니다. 인물의 설득 진척이 100%가 되어도 즉시 합류하지 않고 해당 인물의 `Recruitment Event Id` 사건을 해결해야 합니다. 선택지는 담당자의 명성·공훈, 플레이어 영토 수 또는 교섭가 특기를 조건으로 사용하며, 영입 성공 시 담당자의 공훈·명성·피로 결과를 적용합니다.

`Tavern Quest Definitions`는 호위·토벌·탐색·중재·방첩 5종의 기간, 권장 능력치와 기준, 금화·공훈·명성 보상, 피로 비용과 적성 보너스를 저장합니다. 호위는 번영, 토벌과 중재는 안정, 탐색은 기술과 미발견 인재 단서, 방첩은 방첩 유지 기간을 제공합니다. `Remaining Months`로 여러 달 의뢰 진행도를 저장합니다.

## 24. 전략 지도와 영지 관리 UI 에셋

전략 지도 성 노드는 `WIAdministration.uxml`에 `castle-castle_00`부터 `castle-castle_59`까지 고정 배치됩니다. 성 이름, 좌표, 소유 진영과 선택 이벤트는 런타임 데이터에 따라 기존 요소에 바인딩되며 UI 요소 자체를 런타임에 생성하지 않습니다. 노드 위치는 UI Builder의 `left`와 `top` 또는 성 ScriptableObject의 `Normalized Map Position`으로 조정합니다.

진영별 성채·깃발 이미지는 `Assets/Resources/UI/Generated/map_castle_avalon.png`, `map_castle_valdor.png`, `map_castle_ironheart.png`, `map_castle_sylvanroad.png`, `map_castle_necropolis.png`입니다. USS의 `castle-node-진영ID` 클래스가 해당 이미지를 선택합니다. 원본 재가공 파일은 `Tools/UIAssetSources/map_castle_marker_atlas_clean.png`에 보관합니다.

전역 좌측 패널의 주둔 영웅 카드는 4개, 영지 관리 화면의 주둔 영웅 슬롯은 8개, 특화 시설 슬롯은 2개가 UXML에 고정 배치되어 있습니다. 컨트롤러는 현재 데이터에 맞춰 표시 여부, 문구와 Sprite만 갱신합니다. 성 화면의 영웅 목록과 특화 시설 목록은 별도의 세로 ScrollView이므로 한 목록의 길이가 다른 목록의 레이아웃을 밀어내지 않습니다.

전투 캐릭터 화풍 검증용 중세 검사 스프라이트는 `Assets/Art/Characters/Battle_MedievalSwordsman_Test_V2.png`입니다. 약 4.5~5등신, 양손 검 전투 준비 자세, 작은 화면에서 읽히는 단순 색 덩어리와 투명 배경을 사용합니다. 현재 전투 캐릭터 데이터에는 연결하지 않은 단일 프레임 테스트 에셋입니다. 이전 `Character_MedievalSwordsman_Test_V1.png`은 일러스트 방향이어서 전투 에셋으로 사용하지 않습니다.

중세 검사 공격 애니메이션 테스트 런은 `Assets/generated/sprites/medieval-swordsman-idle`에서 관리합니다. 게임 입력은 `sprite-sheet-alpha.png`이며 `manifest.json`의 `frame_layout.rows.attack`에 기록된 512×512 사각형 4개를 순서대로 재생합니다. 재생 속도는 8 FPS, 비반복이며 현재 전투 데이터에는 연결하지 않았습니다. `raw/attack.png`는 생성 중간 산출물이고 실제 런타임에서는 투명 아틀라스와 manifest를 사용합니다.

동작 개선 후보는 `Assets/generated/sprites/medieval-swordsman-attack-v2`입니다. `references/motion-guides/attack.png`가 높은 당김, 앞발 내딛기, 대각선 타격, 낮은 후속 자세의 관절·검 궤적 기준을 소유합니다. 런타임 사용 방식은 동일하게 `sprite-sheet-alpha.png`와 `manifest.json`을 함께 사용하며 8 FPS 비반복 4프레임입니다. 기존 공격 시트는 비교용으로 보존하며 현재 권장 검토 대상은 v2입니다.

전체 UI 제목·본문·입력 문구는 `Assets/Fonts/NotoSerifKR-VariableFont_wght.ttf`를 기본으로 사용합니다. 자원 수치, 성 수치, 지도 명패, 작은 슬롯 문구와 모든 버튼은 작은 크기에서도 획이 선명한 `Assets/Fonts/NotoSansKR-VariableFont_wght.ttf`를 사용하며 버튼 텍스트 외곽선은 적용하지 않습니다. 두 글꼴은 SIL Open Font License이며 각각의 라이선스 원문을 같은 폴더의 `OFL-NotoSerifKR.txt`, `OFL-NotoSansKR.txt`에 보관합니다.

시안형 명령 아이콘은 `icon_flat_*.png`, 상단 날짜·자원 아이콘은 `hud_flat_*.png`, 버튼 타입은 `button_flat_normal.png`, `button_flat_primary.png`, `button_flat_danger.png`입니다. A/B 버튼은 동일한 768×128 규격과 형상을 사용하며 B는 푸른 색조만 다릅니다. 두 버튼은 2px 투명 모서리와 단일 2px 테두리로 구성하고 USS에서 상하좌우 3의 9-Slice를 사용합니다. 전역 지도 좌측 성 요약 패널과 오른쪽의 독립된 목표·알림 패널을 포함한 공통 패널 배경은 `bg_type_a.png`와 상하좌우 3의 9-Slice를 사용합니다. 조금 더 강조가 필요한 패널에는 원본 대비 약 1/2 두께의 `bg_type_b.png`를 `.bg-type-b` 클래스로 지정하며 상하좌우 16의 9-Slice를 사용합니다. 청동 테두리·대각 모서리와 어두운 중앙 면만 사용하는 `bg_type_c.png`는 `.bg-type-c` 클래스로 지정하며 좌우 72·상하 40의 9-Slice를 사용합니다. 세로형 알림 패널에는 얇은 은회색 금속 프레임과 흑청색 질감 면으로 구성된 512×640 `bg_type_d.png`를 `.bg-type-d` 클래스로 지정하며 상하좌우 16px 9-Slice를 사용합니다. D Type에는 제목·아이콘·행 구분선이 합성되어 있지 않아 알림 외의 세로형 정보 패널에도 재사용할 수 있습니다. 프레임 바깥은 완전 투명하며 고립 알파 픽셀은 허용하지 않습니다. 정리 전 원본은 `Assets/TrashAsset/UI/Generated/bg_type_d_noisy_original.png`에 보관합니다.

공통 모달 헤더는 사용자가 선택한 은색 금속 프레임 이미지를 바탕으로 테두리를 기존 약 1/4 두께로 얇게 재작성한 `popup_header.png`를 사용합니다. 에셋과 Unity Sprite 표시 영역은 1024×297이며, 공통 모달에서 최소 80px 높이와 좌우 20·상하 10의 9-Slice로 표시합니다. 가공 전 선택 원본은 `Tools/UIAssetSources/popup_header_selected_source.png`, 얇은 프레임 생성 원본은 `Tools/UIAssetSources/popup_header_thin_imagegen_source.png`에 보존합니다.

버튼 업무 지시용 공식 별칭은 `GameDocuments/ButtonTypeGuide.md`를 기준으로 합니다. 전략·영지 관리의 기본·주요·위험 버튼은 A·B·C Type, 하단 전역 명령(군사~월간 보고)과 우측 패널 독립 행동 버튼은 D Type, 전투 HUD의 기본·주요·위험 버튼은 E·F·G Type, 넓은 대표 진행 행동은 H Type입니다. H Type은 `generated-ornate-action` USS 클래스로 연결하며 좌 112·우 32·상하 32의 9-Slice를 사용합니다. 타입명은 문서상 별칭이며 실제 에셋 파일명은 Unity 참조 보존을 위해 유지합니다.

전역 지도의 `다음 턴` 버튼은 H Type(`button_type_h.png`)을 사용하고 버튼 문자는 `NotoSerifKR` 굵은 명조체로 표시합니다. 다른 공통 버튼 스타일에 덮이지 않도록 `#turn-button` 전용 규칙에서 이미지를 직접 지정합니다. 실제 표시 크기는 240×72이며 비대칭 화살촉 장식을 원형 그대로 보존하기 위해 이 버튼에서는 9-Slice를 사용하지 않고 `scale-to-fit`으로 표시합니다. H Type의 일반 제작 기준 슬라이스 정보는 `ButtonTypeGuide.md`에 유지합니다.

전역 지도 하단의 `군사`부터 `월간 보고`까지 명령 버튼은 `button_flat_normal.png`와 상하좌우 3의 9-Slice를 사용합니다. 버튼 내부 아이콘과 단축키 표기는 별도 요소로 유지합니다.

플레이어 표시 용어는 `GameDocuments/FantasyTerminologyGuide.md`를 기준으로 합니다. 진영·통치자·영지 관리·영지관·의회·월간 보고·영웅·첩보·원정·전투단·영입·방랑·질서·공훈을 공식 한국어로 사용합니다. 저장 호환성을 위해 `Faction`, `Governor`, `Scheme`, `Army`, `Recruitment`, `Stability` 등 영문 클래스·필드·열거형과 데이터 ID는 유지합니다. `성`은 물리적 지도 거점, `영지`는 관리 단위와 관리 행위를 뜻합니다.

우측 목표·알림 및 영지 관리 명령 패널은 `right_panel_header.png`, `right_panel_frame.png`, `right_objective_card.png`, `right_notice_row.png`, `right_danger_row.png`, `right_action_button.png`를 사용합니다. D Type인 `right_action_button.png`는 거의 직사각형인 흑청색 금속 패널과 작은 내부 리벳을 사용하며 텍스트와 아이콘이 없는 768×256 투명 PNG입니다. 모두 `WIAdministration.uxml`의 고정 요소에 USS로 직접 연결되며 좌우·상하 9-Slice로 확대하므로 모서리 두께가 변하지 않습니다. D Type의 ImageGen 크로마 원본은 `Tools/UIAssetSources/right_action_button_replacement_chroma.png`에 보존합니다.

## 25. 500명 로스터와 전략 자동 검증

`ProjectWI > Data > Seed Mass Character Roster (100 Heroes, 400 Commons)`는 고정 시드로 영웅 100명과 일반 인물 400명을 다시 생성합니다. 아레스·리리아 등 핵심 영웅 8명과 핵심 일반 인물 6명은 원본 능력치, 특기, 초상화와 액티브 스킬 자격을 덮어써 보존합니다. 새 캠페인에는 진영별 관계와 초기 명령에 필요한 핵심 인물 19명만 배치하며 나머지 481명은 탐색·영입 후보로 남깁니다.

전략 데이터베이스의 `전략 전투 전력 균형`에서 `Castle Defense Power Percent`, `Castle Stability Power Percent`, `Garrison Hero Power Percent`를 편집합니다. 현재 값은 성 방어 300%, 질서 50%, 주둔 인물 무력 60%입니다. 성 자체 방어 비중을 높이고 주둔 인물 비중을 제한하여 전투단 원정 전후 방어력이 급격히 바뀌는 현상을 줄입니다. 전략 자동 판정은 동률일 때 수비 성공으로 처리합니다.

AI는 전투단을 생성한 뒤 성에 최소 한 명을 남기고 성향에 따라 2~4명의 인물을 편성합니다. 지휘관만 있는 단독 원정은 허용하지 않으며, 복수 전투단은 각 전투단 인원과 성 예비 인물을 충족할 때만 생성됩니다. `WICampaignAutoPlayer`도 같은 예비 규칙을 사용하고 평균 피로 60 이하, 균형형 예상 전력 125% 이상, 공격형 115% 이상일 때만 공격 후보를 선택합니다. 상호 침공으로 회전 전투가 될 적 이동 전투단의 전력도 예상치에 합산하며, 공격형 정책은 첫 두 달 동안 편성과 적 이동을 확인한 뒤 원정을 시작합니다.

자동 검증은 첫 12개월에 해결한 선택 사건 수를 월별로 기록합니다. 현재 균형형 기준은 `2, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0`이며 한 달 최대 2건, 두 달 이상에 분산, 총 2건 이상을 기준으로 검사합니다. 이 수치는 기계적인 과밀·공백 방지 기준이며 사건의 감정적 재미는 수동 플레이에서 별도로 평가합니다.

## 26. 캠페인 자동 테스트 랩

유니티 상단 메뉴 `ProjectWI > Tools > Campaign Auto Test Lab`에서 컴퓨터가 캠페인을 결말 또는 지정한 최대 개월까지 자동 진행하는 도구를 엽니다. 기본 전략 데이터베이스, 여유·표준·도전 난이도, 성장형·균형형·공세형 정책과 최대 개월을 선택합니다. `정책 3종 × 난이도 3종 전체 비교`를 켜면 아홉 조합을 연속 실행하고, 끄면 선택한 한 조합만 실행합니다.

결과 표에는 진행 개월, 승리·패배·진행 중 상태, 최종 플레이어 성 수, 전투와 승패, 원정, 소유권 변화와 해결한 선택 수를 표시합니다. `CSV 내보내기`와 `Markdown 내보내기`는 현재 결과를 UTF-8 without BOM 파일로 저장합니다. 화면의 입력과 결과 행 아홉 개는 `Assets/Editor/UI/WICampaignAutoTestLab.uxml`에 고정 배치되어 UI Builder에서 확인할 수 있습니다.

자동 플레이는 현재 결정론적 전략 로직을 사용하므로 같은 데이터·난이도·정책은 같은 결과를 재현합니다. 이 도구는 캠페인 정체, 조기 멸망, 과도한 전승과 선택 차단을 찾기 위한 것이며 사람의 긴장감·성취감·클릭 피로 평가는 수동 플레이로 진행합니다.
적 성의 소유 진영과 지형은 기본 공개하지만 번영·기술·질서·방어·주둔·시설·전투단 수는 조사 정보가 있는 동안만 공개합니다. 동맹 성은 조사 없이 상세 정보를 공유합니다. 플레이어 참가 미결 전투는 해당 성의 영지 관리 정보가 아니라 전투 세션의 군사 정보만 공개합니다. `Scheme Intel`의 관찰 진영·대상 성·남은 개월은 캠페인 저장에 포함되고 월말에 0이 되면 제거됩니다.

## 23. 캐릭터 데이터 뷰어 (Editor Tool)

유니티 상단 메뉴 `ProjectWI > Viewer > Character Data Viewer`에서 `WI_AdministrationDatabase.asset`에 등록된 캐릭터(고유 영웅 8명 및 일반 인물 6명) 데이터를 엑셀(Excel) 스타일의 그리드 테이블 형태로 일괄 조회하고 편집할 수 있습니다.

- **검색 및 필터링**: ID/한글/영문 이름 텍스트 검색 및 등급 필터(전체/영웅/일반) 제공
- **컬럼 정렬**: ID, 이름, 등급, 종족, 클래스, 5대 능력치(통솔, 무력, 지력, 정치, 매력), 충성도, 필요 명성 컬럼 헤더 클릭 시 오름차순/내림차순 정렬
- **인라인 셀 편집**: 이름, 등급, 종족, 클래스, 능력치 수치, 충성도, 스킬 활성화, 초상화 Sprite를 그리드 내에서 즉시 수정 가능
- **데이터 저장 & Undo/Redo**: 유니티 `SerializedObject` 연동으로 수정 내용의 Undo/Redo 지원, '새 캐릭터 추가' 및 '데이터베이스 저장' 버튼 제공
# UGUI 월드 화면 이행

- 플레이 중 각 화면 프리팹 루트는 이벤트 구독을 위해 활성 상태를 유지합니다. 숨겨진 화면은 `Canvas.enabled=false`, `GraphicRaycaster.enabled=false`로 전환되므로 Scene 선택과 게임 입력을 가로막지 않으며, 내부 `contentRoot` 또는 `modalRoot`가 표시될 때만 Canvas도 함께 활성화됩니다.
- 플레이 때 생성된 `~~~UGUI(Clone)` 루트는 에디터 코드가 자동으로 Scene Picking을 차단합니다. 이 설정은 루트에만 적용되고 자식은 포함하지 않으므로 수동 `Alt + Picking` 조작 없이도 실제 자식 UI를 Scene View에서 선택할 수 있습니다.
- `WIAdministrationWorldUGUIController`는 데이터를 직접 생성하거나 변경하지 않고 `WIAdministrationWorldSnapshot`을 표시합니다.
- 선택 성 상세의 `CastleDetailRow1~6`은 왼쪽 제목 전용 TMP이며, `CastleDetailValue1~6`은 오른쪽 값 전용 TMP입니다. 제목과 값 사이를 공백 문자로 맞추지 않으며 `CastleDetailTitles`, `CastleDetailValues` 배열의 같은 인덱스를 한 행으로 표시합니다.
- 월드 화면의 군사·영웅·외교·첩보·연구·통치·의회·월간 보고·다음 턴 버튼은 이행용 `WIAdministrationUIController.UGUIBridge`를 통해 기존 게임 기능을 호출합니다.
- 실제 게임 화면은 완성 UGUI 프리팹을 사용합니다. 이전 UI Toolkit UXML은 참고 자료로 남아 있으나 런타임 `UIDocument`에는 연결되지 않습니다.
- 월드 지도 성 노드는 런타임 생성하지 않으며 프리팹에 60개가 고정 배치됩니다. 위치는 `WICastleDefinition.NormalizedMapPosition`, 표시 이름과 초기 진영은 행정 데이터베이스를 기준으로 생성하고 런타임 소유 진영은 스냅샷으로 갱신합니다.
- 영지 UGUI는 `WIAdministrationTerritorySnapshot`을 통해 선택 성 정보를 읽습니다. 화면은 전략 HUD와 동일한 상단 진영·연월·자원 표시, 좌측 성 현황, 중앙 대형 성 전경, 우측 명령, 하단 요약 트레이로 구성됩니다. 하단에는 주둔 영웅 앞쪽 4명, 특화 시설 최대 2개와 이번 달 중점을 표시하고, 명령 버튼 8개는 프리팹에 고정 배치됩니다. 성 규모·정보 공개·관리 가능 상태에 따라 슬롯과 버튼이 표시 또는 비활성화됩니다.
- 영지 UGUI의 `TopHUD`는 월드 UGUI의 `TopHUD`를 기준으로 동일하게 유지합니다. 1920×1080 기준 높이는 92px이며 배경·하단 금속선·진영 문장·날짜 및 세 자원·자원 아이콘·세로 구분선·우측 월보/의회/연구/설정 아이콘과 투명 클릭 영역의 구조, 앵커, 오프셋, 폰트 크기와 정렬을 일치시킵니다. 월보·의회·연구·설정 버튼도 두 화면에서 같은 전역 UGUI 기능을 실행합니다.
- 성 내정의 중점 사업·인사 배치·인재 활동·특화 시설·기본 시설·태수 위임 모달은 공통 냉색 금속 스타일을 사용합니다. 본문은 `bg_type_d`, 헤더와 일반 버튼은 `button_flat_normal`, 주요 행동은 `button_flat_primary`, 닫기는 X가 포함된 `turn_followup_close_button_v1`을 사용합니다. 고정 디자인은 각 프리팹에 저장하며 런타임 코드에서 위치나 Sprite를 교체하지 않습니다.
- 성 내정 하단 공통 트레이는 `Assets/Resources/UI/Generated/territory_bottom_panel_v1.png`입니다. 문구·아이콘·카드가 합성되지 않은 512×171 투명 PNG이며 냉색 흑청 질감, 얇은 은회색·저채도 청동 프레임을 사용합니다. Unity에서는 Sprite/Single, 무압축, Mipmap 비활성, Clamp, Bilinear와 상하좌우 18px 9-Slice를 사용합니다.
- 성 내정의 `대륙 지도` 이동 버튼은 공용 명령 버튼을 확대하지 않고 `Assets/Resources/UI/Generated/territory_back_button_v1.png`를 사용합니다. 512×142 무문자 투명 PNG의 얇은 은회색 이중선과 작은 모서리·중앙 장식으로 구성되며, Unity에서는 좌우 12px·상하 10px 9-Slice를 사용합니다.
- 영웅 배치·인재 활동·기본 시설·영웅 목록은 각 UGUI 프리팹과 전용 브리지만 사용하며 레거시 UI Toolkit 인물·시설 모달은 호출하지 않습니다.
- 중점 사업 UGUI는 8종 사업의 기본/집중 투자 선택 후 대기 영웅을 담당자로 선택하는 2단계 고정 화면입니다. 사업 비용·예상 성과·특기·전문 분야·진영 방침 계산은 기존 `WIAdministrationTurnSystem`을 사용합니다.
- 영웅 배치 UGUI는 영입되었고 다른 성에 배치되지 않았으며 임무·전투단에 참여하지 않는 인물을 후보로 표시합니다. 한 페이지에 8개의 고정 카드를 사용하며 이전/다음 버튼으로 전체 후보를 탐색합니다.
- 인재 활동 UGUI는 현재 성의 대기·비부상 인물을 선택한 뒤 탐색·교류·영입·훈련·휴식을 지정합니다. 교류는 같은 성의 다른 주둔 인물, 영입은 발견된 미영입 인물을 대상으로 하며 필요 명성 미달 후보는 비활성화됩니다.
- 특화 시설 선택 UGUI는 성 확장 사업 완료로 `PendingSpecialFacilityChoice`가 활성화된 경우에만 열립니다. 현재 성에 없는 시설 하나를 선택하면 빈 특화 시설 슬롯에 배치되고 선택 권한이 해제됩니다.
- 기본 시설 UGUI는 성관·시장·훈련소·선술집의 역할을 안내합니다. 선술집 월간 의뢰에서는 진행 기간과 보상, 설명을 확인하고 대기 중인 주둔 인물의 권장 적성을 비교해 담당자를 배정합니다.
- 영지관 위임 UGUI에서는 주둔 인물을 영지관으로 임명하고 균형·번영·연구·전선·인재 방침과 기본·집중 월 예산을 선택합니다. 예상 자동 사업은 기존 계산 결과로 표시되며 위임 전에는 반드시 영지관이 필요합니다.
- 영지 원정 UGUI에서는 현재 성의 출정 가능한 전투단을 고른 뒤 인접 목표를 선택합니다. 전투단이 없으면 대기 주둔 인물을 대장으로 새 전투단을 생성할 수 있으며, 같은 진영 성은 무료 이동하고 교전 중인 적 성 원정은 영향력 20을 사용합니다.
- 성 상세 기록 UGUI는 성 이미지와 규모·지형·특산·전문 분야·영지 수치·주둔·시설·영웅 흔적·전투단 기록을 스크롤 본문으로 표시합니다. 미조사 적 성은 소유 진영과 지형 외 정보가 숨겨집니다.
- 캠페인 목표 UGUI는 전역 화면 오른쪽 목표 카드를 누르면 열리며 현재 목표의 시작 정세, 달성 조건, 진행 수치와 금화·마나·영향력 보상을 표시합니다.
- 월간 보고 UGUI는 하단 월간 보고 버튼과 오른쪽 전투 알림에서 열립니다. 지난달 자원·위임·AI 판단·뉴스를 스크롤로 확인하고, 오른쪽 행동 목록에서 선택 사건을 처리하거나 미결 전투를 시작합니다.
- 군사 UGUI는 하단 군사 버튼에서 열리며 미결 플레이어 전투와 플레이어 전투단의 인원·숙련·보급·상태를 최대 8개 고정 카드로 표시합니다. 같은 프리팹에서 전투 시작, 편성 성·대장·역할·단원 선택, 단원 제외, 합동 훈련, 해산, 인접 성 이동·원정을 단계별로 처리합니다.
- 영웅 UGUI는 하단 영웅 버튼에서 열리며 영입 영웅과 발견 인재를 페이지 가능한 8개 카드로 표시합니다. 영웅 카드를 선택하면 공훈·명성·충성·특기를 확인하고 승격 대기 인물의 영웅 승격 또는 조건을 충족한 작위 수여를 처리할 수 있습니다.
- `MainScene`에는 개별 UGUI 화면 인스턴스를 저장하지 않습니다. 단일 `UGUI Screen Bootstrap` 오브젝트의 `WIUIScreenManager`가 `Assets/Prefabs/Administration`의 완성 프리팹 참조를 보관하고 플레이 시작 때 화면을 생성합니다. 별도의 `MainCanvas` 화면 관리자는 사용하지 않습니다. Scene에서 확인할 화면의 루트만 수동으로 활성화하면 다른 화면과 겹치지 않게 편집할 수 있습니다.
- 외교 UGUI는 하단 외교 버튼에서 열리며 존속 중인 다른 진영과 전쟁·중립·우호·불가침·동맹 상태를 표시합니다. 대상 진영을 선택하면 현재 관계에 맞춰 포로 처리, 관계 개선, 협정, 원조, 공동 공격 또는 선전포고 명령을 실행합니다.
- 첩보 UGUI는 하단 첩보 버튼에서 열리며 진행 중 임무와 조사·방첩·유언비어·인재 이간을 표시합니다. 첩보 종류, 대기 담당 인물, 대상 성을 차례로 선택하며 인재 이간은 조사로 정보가 공개된 성의 주둔 인물까지 선택합니다.
- 연구 UGUI는 하단 연구 버튼에서 열리며 완료 연구, 진행 중 연구와 미완료 연구를 함께 표시합니다. 마나·최고 기술·선행 연구·진행 중 연구 조건을 충족한 연구를 선택한 뒤 플레이어 진영의 대기 인물을 지력 순으로 비교해 담당자로 지정합니다.
- 진영 정세 UGUI는 하단 진영 버튼에서 열리며 대륙 5대 진영의 영토 수, AI 운영 성향, 존속·멸망 상태, 플레이어와의 외교 상태와 주요 인물 관계를 표시합니다. 금화·마나·영향력과 현재 방침은 플레이어 진영 카드에만 공개됩니다.
- 의회 UGUI는 하단 의회 버튼에서 열리며 부국·개발·안정·수비·원정·인재 방침과 각 강화 효과를 6개 고정 카드로 표시합니다. 현재 방침은 선택 표시와 함께 비활성화되며 다른 방침을 고르면 플레이어 진영의 `FactionPolicy`가 즉시 바뀌어 다음 월말 사업 성과 계산에 반영됩니다.
- 월간 보고의 선택 사건은 공통 사건 UGUI에서 처리합니다. 사업·인물 관계·지역·점령 통치·영입 사건은 데이터에 등록된 설명, 조건과 예상 결과를 고정 카드로 표시하며 조건 미달 선택지는 비활성화됩니다. 영웅의 흔적은 빈 슬롯에 기록하거나 기존 두 흔적 중 교체할 항목을 같은 화면에서 선택합니다.
- 다음 턴 버튼은 UGUI 연산 안내를 한 프레임 표시한 뒤 기존 월간 턴 시스템을 실행하고 월간 보고 UGUI를 엽니다. 보고를 닫으면 미확인 캠페인 결과를 우선 표시하고, 결과가 없으면 현재 월의 첫해 튜토리얼을 표시합니다. 캠페인 결과에서는 지도 계속 보기 또는 UGUI 시작 화면 복귀를, 튜토리얼에서는 현재 안내 확인 또는 전체 건너뛰기를 선택합니다.
- 새 캠페인을 시작하면 현재 캠페인 목표 UGUI가 먼저 열리고 목표 확인 또는 닫기 후 첫해 튜토리얼이 이어집니다. 자동 저장을 불러오면 미확인 캠페인 결과를 우선 표시하고, 결과가 없으면 저장된 월의 튜토리얼을 표시합니다. 목표 누락과 불러오기 실패·경고는 턴 후속 공통 프리팹의 확인 안내로 표시합니다.
- 인물 이동은 영지의 인재 활동 UGUI에서 대기 인물을 선택한 뒤 `이동 · 같은 진영의 인접 성`을 선택합니다. 목적지 카드에는 현재 주둔 인원과 이미 예약된 이동 인원을 합친 슬롯 수가 표시되며 가득 찬 성은 비활성화됩니다. 영지관 또는 다른 임무 중인 인물은 이동할 수 없고, 이동을 시작하면 출발 성에서 빠져 다음 월말에 목적지에 도착합니다.
- 공통 오류·안내 문장은 UGUI 공통 메시지 화면에 표시됩니다. 문자열 UID가 전달되면 데이터베이스의 현재 언어 문장으로 변환하고, 직접 작성한 결과 문장은 그대로 표시합니다. `확인` 카드 또는 우측 상단 닫기 버튼으로 안내를 닫을 수 있습니다.
- 시스템 UGUI는 월드 하단 `설정` 버튼에서 엽니다. 첫 페이지의 설정 카드는 누를 때 언어·화면 모드를 전환하고 음량은 25% 단위, 목표 프레임은 30·60·120 FPS 순서로 순환합니다. `설정 적용`을 눌러야 실행 환경과 PlayerPrefs에 저장되며 `기본값 복원`은 `WI_SystemSettingsConfig` 값을 즉시 적용합니다. 두 번째 페이지는 수동 슬롯 1~3 저장/불러오기와 자동 저장 불러오기를 제공하며 비어 있는 슬롯은 비활성화됩니다.
- 전역 UGUI 단축키는 M 군사, H 영웅, D 외교, S 첩보, R 연구, G 통치, C 의회, L 월간 보고, T 다음 턴입니다. UGUI 모달이 열려 있으면 전역 단축키는 동작하지 않으며 Escape는 Canvas 정렬 순서가 가장 높은 모달을 닫습니다. 입력은 Input System의 현재 키보드를 사용하므로 UI Toolkit 포커스가 필요하지 않습니다.
- 캠페인 저장 상태에 미결 플레이어 전투가 있으면 MainScene 초기화 직후 월간 보고 UGUI가 자동으로 열립니다. 화면별 `OnEnable` 구독보다 먼저 요청이 소실되지 않도록 한 프레임 뒤에 표시합니다.
- 캠페인 난이도와 시작 조건 카드는 `WICampaignTitleUGUI.prefab`에 고정 배치되어 있으며 `WICampaignTitleUGUIController`만 선택 상태와 버튼 입력을 관리합니다. 행정 UI Toolkit UXML에는 더 이상 런타임 카드가 생성되지 않습니다.
- 실제 플레이 입력은 UGUI 프리팹의 버튼만 사용합니다. 행정 초기화는 UI Toolkit 전역 버튼을 바인딩하거나 UXML 성 노드를 다시 구성하지 않으며, UGUI 지도는 `WIAdministrationWorldUGUI.prefab`에 저장된 60개 고정 버튼을 사용합니다.
- MainScene에는 화면 관리자 `UGUI Screen Bootstrap` 하나만 존재합니다. 화면 자식 없이 `WIUIScreenManager.screenPrefabs`에 23개 완성 프리팹 에셋만 연결되며 플레이 시 해당 프리팹을 자식으로 생성합니다. 빌더는 새 프리팹을 자동 등록하고, 수동 복구 메뉴는 이전 이름이나 중복 관리자의 참조를 병합한 뒤 중복 루트를 삭제합니다.
- `WIAdministrationUI.prefab`은 이름을 유지하지만 더 이상 UI 화면이나 `UIDocument`를 포함하지 않습니다. 캠페인 상태와 기능별 UGUI 스냅샷·명령을 연결하는 런타임 브리지 프리팹입니다.
- 군사 목록·전투단 편성·상세·이동/원정은 `WIAdministrationMilitaryUGUIController`와 `WIAdministrationUIController.UGUIMilitaryBridge`만 사용합니다. 이전 UI Toolkit 군사 모달 진입 메서드는 삭제됐습니다.
- `WIAdministrationUIController.Characters.cs`에는 UGUI 카드 표시용 `GetGradeDisplayName`, `GetTraitDisplayText`만 남습니다. 인물 선택과 시설 명령은 각 UGUI 컨트롤러가 고정 프리팹 카드를 갱신해 처리합니다.
- `WIAdministrationUIController.RealmGovernance.cs`에는 중점 사업의 실제 배정·확장 조건 검사와 사업/투자 표시 헬퍼만 남습니다. 중점 사업·연구·의회·시스템 화면은 각각의 완성 UGUI 프리팹과 전용 브리지에서 표시하고 입력을 처리합니다.
- `WIAdministrationUIController.RealmRelations.cs`에는 충성 상태·외교 상태·관계 설명·AI 성향의 UGUI 표시 헬퍼만 남습니다. 외교 명령과 첩보 예약은 각각 `UGUIDiplomacyBridge`, `UGUISchemeBridge`가 기존 게임 시스템에 직접 전달하며 진영 정세는 `UGUIFactionBridge`가 읽기 전용 스냅샷으로 구성합니다.
- `WIAdministrationUIController.RealmReports.cs`에는 영입 선택 조건과 진영·영지관 방침의 UGUI 표시 헬퍼만 남습니다. 월간 보고는 `UGUIBridge`, 선택 사건은 `UGUIEventChoiceBridge`, 위임 설정은 고정 `WIAdministrationDelegationUGUI.prefab` 흐름에서 처리합니다.
- 성 상세 기록과 특화 시설 선택은 각각 `WIAdministrationCastleRecordUGUI.prefab`, `WIAdministrationSpecialFacilityUGUI.prefab`만 사용합니다. 이전 `OpenCastleRecordModal`, `OpenSpecialFacilityModal` UI Toolkit 진입점은 삭제됐습니다.
- 턴 진행은 `WIAdministrationUIController.Turn.cs`의 `BeginTurn`에서 미결 전투를 검사한 뒤 `ExecuteUGUITurnRoutine`을 실행합니다. 처리 안내·월간 보고·캠페인 결과·첫해 튜토리얼은 `WIAdministrationMonthlyReportUGUI.prefab`과 `WIAdministrationTurnFollowupUGUI.prefab`만 사용합니다.
- 행정 런타임 컨트롤러는 더 이상 UI Toolkit 네임스페이스나 `UIDocument`, `VisualElement`, 동적 `CreateModal`을 사용하지 않습니다. 월드·영지 전환은 완성 UGUI 프리팹의 활성 상태를 바꾸고 모든 표시 데이터는 기능별 UGUI 스냅샷을 통해 갱신합니다.
- `Assets/UI/Administration`의 UXML/USS는 과거 시안 비교 및 문서용 레거시 에셋으로만 남아 있으며 MainScene과 `WIAdministrationUI.prefab` 런타임에는 연결되지 않습니다. 과거 지도 연결선을 그리던 `WIMapConnectionLayer` 런타임 스크립트는 제거했고 레거시 UXML의 같은 위치는 일반 `VisualElement`로 치환했습니다.
- 아레스는 `Ares_Portrait_Face_V1.png` 얼굴 초상화와 `Ares_Battle_1WU_A_OutlineBake_V1.png` 전투용 투명 전신을 사용합니다. `Ares_Battle_FullBody_V1.png`는 파생 제작용 고해상도 원본으로만 보존하며 애니메이션은 아직 적용하지 않습니다.
- 전투 카메라는 참가자 수와 초기 진형 범위를 계산해 이를 포함하는 A(6)·B(8)·C(10) 중 한 단계로 시작합니다. 마우스 휠 한 번마다 인접 단계로만 이동하며 `Home` 키는 자동 계산된 시작 단계로 복원합니다.
- Unity Editor 상단 `ProjectWI`와 `WI` 메뉴의 전체 기능, 실행 조건과 데이터·Prefab 변경 주의사항은 `GameDocuments/UnityToolMenuManual.md`를 기준으로 확인합니다.
- 전투 전장은 `WI_BattleConfig.arenaPrefab`에 연결된 `WIBattleCharacterScaleArenaV6.prefab`을 사용합니다. 프리팹이 없을 때만 `arenaBackground`의 `Battle_FortressField_V5_4K`를 폴백으로 표시합니다.
- 전투 HUD 프레임은 `Assets/Art/UI/Battle`에 상단 상태바, 캐릭터 정보, 명령바, 스킬바의 투명 단일 Sprite로 분리되어 있습니다. 이미지에는 문구·수치·초상화·스킬 아이콘이 포함되지 않으므로 UGUI의 TMP, Image와 Fill 컴포넌트를 위에 배치해 사용합니다.
- 전투 HUD는 `Assets/UI/Battle/WIBattleHUD.uxml`의 네 고정 컨테이너가 `Assets/Art/UI/Battle`의 상단 상태·캐릭터 정보·명령바·스킬바 이미지를 직접 참조합니다. 프레임 내부의 문구와 버튼은 기존 `WIBattleHUDController`가 갱신하며, 배경 이미지 자체에는 게임 데이터가 포함되지 않습니다.
- 전투 캐릭터 Transform은 `WI_BattleConfig`의 `Battle Sprite Scale = 1`을 사용합니다. 화면상 전신 크기는 알파 실루엣 높이 256px와 `256 PPU` 조합으로 통일하며 현재 아레스는 정확히 1유닛 높이입니다. `Arena Background Size`는 이동·충돌 판정 영역을 바꾸지 않고 배경 프리팹의 카메라 이동 범위를 정하며 현재 축척 검증 전장 기준은 `36×20`입니다.
- 현재 축척 검증 전장은 `Assets/Prefabs/Battle/WIBattleCharacterScaleArenaV6.prefab`입니다. 아레스 약 1유닛 높이와 36×20.25 배경을 사용하며 카메라 C 단계는 10입니다. 정적 성벽·야영지·망루·방책과 접지 그림자를 하나의 완성 그림에 포함하고, 중앙은 큰 양각형 무늬 없이 넓은 저대비 흙 색면으로 유지합니다. 기존 V1~V5는 비교용으로 보존합니다.
- V4의 좌우 천막은 `Tent_1_NeutralDay_V2`, `Tent_3_NeutralDay_V2`를 사용합니다. 원본 파일은 비교·복구용으로 유지하며 밝기 통일 시 프리팹 SpriteRenderer의 Color를 중복 적용하지 않습니다.
- 천막 접지 그림자는 각 천막의 `GroundShadow` 자식에서 조정합니다. 기본 Sorting Order는 -105이고 지면 -110보다 앞, 천막 -99보다 뒤에 표시됩니다. 그림자 강도는 SpriteRenderer 알파, 크기와 폭은 자식 Transform Scale로 조정하며 실시간 ShadowCaster2D는 사용하지 않습니다.
- 현재 배경 제작 원본은 `Assets/Art/Battle/GroundLayers/CharacterScaleArenaV6/Battle_CompleteArena_CharacterScale_V6_4K.png`입니다. 1유닛 캐릭터보다 훨씬 작은 표면 디테일과 넓은 회갈색 흙 색면을 사용하며 1920×1080 네 장으로 픽셀 무손실 분할했습니다. 106.6667 PPU, Mipmap 비활성, Bilinear, Clamp, 무압축을 사용합니다.
- 캐릭터 깊이는 월드 Y가 작을수록 큰 Sorting Order를 받도록 매 프레임 갱신됩니다. 따라서 같은 X 부근에서 겹칠 때 화면 아래쪽 캐릭터가 앞에 표시됩니다.
- 현재 A/B/C 모두 `WI_BattleCharacter_Unlit.mat`을 사용하고 외곽선은 전투 Sprite에 직접 베이크합니다. 셰이더 외곽선 머티리얼 두 종류는 비교·복구용으로만 보존합니다.
- 플레이 모드에서 `ProjectWI/Verification/Battle Zoom/A Near`, `B Middle`, `C Far` 메뉴로 각 단계를 즉시 비교할 수 있습니다.
- 현재 지면은 `WI_BattleGround_Unlit.mat`을 사용하고 BattleScene은 백색 Global Light 2D 강도 1만 활성화합니다. `Arena Key Light 2D`와 `Arena Fill Light 2D`는 비활성화되어 노란 조명과 방향성 명암이 없습니다. 노멀맵 단계에서는 지면을 Sprite Lit로 전환한 뒤 중립 광원에서 별도 검증합니다.
- 맵 그래픽 검증 중에는 `WI_BattleConfig`의 `showCharacterLabels`와 `showCharacterHealthBars`를 비활성화합니다. 다시 표시할 때는 해당 ScriptableObject 체크 항목만 켜며 전투 로직 코드를 변경하지 않습니다.
- 전투 카메라는 직교 크기 `6~14` 범위에서 마우스 휠로 조절합니다. 최대 14는 16:9 화면에서도 현재 배경 경계 안에 머무는 값이며, `Home`은 참가 인원에 맞춰 계산된 시작 시점으로 돌아갑니다.
- 현재 전투 배경은 `WIBattleCharacterScaleArenaV6.prefab`의 V6 청크 네 장입니다. V6 4K 마스터와 ImageGen 원본은 향후 재가공을 위해 현 위치에 유지합니다.
- `Battle_FortressField_V5_4K`는 `arenaPrefab`이 없을 때 사용하는 폴백 Sprite이므로 유지합니다.
- 전투 카메라의 이동 경계는 `arenaBackgroundSize`를 사용합니다. 기본 줌에서는 큰 맵의 일부를 보며 WASD·방향키로 이동할 수 있고, 최대 줌아웃에서는 더 넓은 범위를 확인합니다. 실제 전투 판정 영역 `arenaSize`는 이번 단계에서 변경하지 않았습니다.
- 과거 대형 청크 실험 원본과 조립 미리보기는 `Assets/TrashAsset/Art/Battle/Backgrounds` 아래에 보관합니다.
- 아레스의 `Ares_Battle_FullBody_V1.png`는 고해상도 원본으로만 보존합니다. 실제 전투는 `Ares_Battle_1WU_A_OutlineBake_V1.png`를 Transform Scale 1, 256 PPU로 사용해 외곽선 포함 가시 높이 1유닛으로 표시합니다. Mipmap 활성, Bilinear, Alpha Is Transparency, 무압축으로 임포트하며 `WI/Battle/Configure Ares Battle Sprite Defaults` 메뉴로 동일 설정을 다시 적용할 수 있습니다. 다른 캐릭터의 변환 절차는 `BattleSceneAssetSettingsGuide.md` 3.2를 따릅니다.
- 전장 전용 검토 후보 `Ares_Battle_Unit_V1.png`는 기존과 비슷한 약 7등신 비율을 유지한 256×384 투명 Sprite입니다. 실제 인물 높이 328px와 328 PPU를 사용하며, 검토 확정 전까지 `WIHeroDefinition.battleSprite`에는 연결하지 않습니다.
- 현재 검증 데이터의 전체 캐릭터 500명은 `Ares_Battle_1WU_A_OutlineBake_V1.png`를 공용 `battleSprite`로 사용합니다. 과거 일괄 배정·해제 테스트 메뉴는 데이터 훼손 위험을 줄이기 위해 제거했습니다.
- 30대30 밀도 검증은 Unity 메뉴 `ProjectWI/Verification/Start 30v30 Battle Density Test`로 실행합니다. 캠페인 저장과 분리된 테스트 세션에 양측 영웅 1명·커먼급 29명을 자동 편성하고 MainScene에서 BattleScene으로 진입합니다.
- 확대 검증용 실험 지면은 `Assets/Art/Battle/Backgrounds/GroundExperiment_V2/Battle_Ground_NeutralDay_V2_4K.png`입니다. 4096×4096 단일 대형 지면 후보이며 무압축·Mipmap 비활성 설정을 사용합니다. 반복 배치 시 이음선이 있으므로 Tile/Repeat 용도로 사용하지 않으며 현재 전투 설정에는 연결하지 않습니다.
- 공통 UGUI 버튼 배경 `button_normal`은 좌우·상하 28px, `button_primary`는 좌우 28px·상하 24px Border를 사용합니다. 크기가 변하는 버튼의 `Image Type`은 반드시 `Sliced`로 유지하며 현재 캠페인 선택 프리팹을 포함한 모든 연결 사용처가 이 규칙을 따릅니다.
- 전략 월드 UGUI는 1920×1080 기준으로 상단 HUD 92px, 하단 명령부 112px를 사용합니다. 상단은 진영 문장·진영명, 연월, 금화·마나·영향력과 월 수입, 월간 보고·의회·연구·설정 버튼 순서입니다. 본문은 좌측 선택 성 22%, 중앙 지도 59%, 우측 목표·알림 19%로 나누며 지도 Sprite와 60개 고정 성 노드는 기존 `WIAdministrationWorldSnapshot` 데이터를 사용합니다.
- `strategy_top_settings_v1`은 전략 상단 설정 버튼 전용 투명 톱니 Sprite입니다. 나머지 세 상단 기능은 `icon_flat_report`, `icon_flat_faction`, `icon_flat_research`를 재사용하며 각 아이콘 위의 투명 Button이 기존 기능을 호출합니다.
- 전략 하단 명령 바는 좌측 1.8%부터 군사·인사·외교·계략·연구·평정·월보 7개 버튼을 배치합니다. 버튼은 화면 폭 9.8%, 간격 10.5%이며 아이콘과 명조 계열 문구를 분리해 표시합니다. 다음 턴은 화면 78~98.2%에 독립 배치하고 하단 설정 버튼은 사용하지 않습니다.
- 전략 좌측 선택 성 패널은 상단 문장·성명·소속, 가로형 성 이미지, 영지관·번영·기술·질서·방어·주둔 전투단 6행, 영웅 카드 4개, 성 관리 버튼 순서로 구성합니다. 현재 데이터 모델에 없는 인구·식량·행복도는 표시를 위해 임의 계산하지 않습니다.
- 하단 일반 명령은 `strategy_command_button_v2`를 좌우 34px·상하 24px Border의 Sliced Image로 사용합니다. 다음 턴은 비대칭 `strategy_next_turn_button_v3`를 좌 88px·우 42px·상하 24px Border로 사용하며 아발론 문장, 문구, 우측 화살표는 별도 자식 요소입니다.
- `strategy_avalon_crest_v1`은 전략 화면의 상단 진영 표식과 좌측 선택 성 표식에 공용으로 사용하는 투명 Sprite입니다. 이미지에 문구나 수치는 포함하지 않습니다.
- 전략 화면 전용 `strategy_side_panel_v1`, `strategy_command_button_v1`, `strategy_next_turn_button_v1`, `strategy_hero_card_v1`은 문구가 없는 투명 PNG입니다. TMP 텍스트와 실제 영웅 초상은 프리팹 자식 요소로 별도 배치합니다.
- 선택 성 영웅 카드는 `WICastleRuntimeState.HeroIds`의 앞쪽 최대 4명을 표시합니다. 이름과 초상은 `WIHeroDefinition`, 표시 레벨은 `WICharacterRuntimeState.Experience`를 100 경험치당 1단계로 환산해 사용합니다.
- `bg_type_c`는 전략 화면 상단 HUD의 공통 프레임 Sprite입니다. 하단 명령부는 새 시안에 따라 단색 흑청색 배경과 얇은 상하 구분선을 사용합니다.
- 전략 지도 연결선은 `WIAdministrationMapConnectionGraphic` 하나가 `WIAdministrationWorldSnapshot.MapConnections`를 메시로 그립니다. 평상시에는 저명도 진영색, 적대 경계는 적색과 `×`, 선택 성의 인접 경로는 청백색 이중 광택과 마름모 표식으로 표시하며 포인터 입력은 받지 않습니다.

# 일반 인물 사망·재야·자동 영입

- 태생 영웅 등급 인물은 사망 또는 처형되면 영구 퇴장합니다.
- 일반 출신 인물은 영웅으로 승격했더라도 사망 6개월 후 일반 재야 인재로 복귀합니다. 이때 승격 상태·승격 성취·작위를 잃으며, 복귀 성은 살아남은 세력의 성 중 재현 가능한 방식으로 선택됩니다.
- 복귀 전에 성·전투단·포로·전향·영입 상태를 검사하므로 같은 인물이 고용된 상태로 중복 등장하지 않습니다.
- `ProjectWI/Tools/Campaign Auto Test Lab`은 실제 탐색·영입 활동을 수행하며 CSV와 Markdown에 `일반 재야 복귀`, `인재 발견`, `신규 영입` 지표를 출력합니다.
- 도구 결과 행의 `인물(영/일)`은 플레이어 세력의 최종 전체 고용 인원과 그중 영웅/일반 수이며, `영입(발/영/복)`은 실행 중 발견/신규 영입/재야 복귀 수입니다.

# 시나리오 성 배치 데이터

- `WICampaignVariantDefinition.castlePlacements`: 시나리오 전용 성 소유권, 정규화 지도 좌표, 인접 성 목록을 편집합니다.
- `playerStartingCastleId`: 기존 플레이어 시작 영웅을 모을 시나리오 시작 성 ID입니다.
- `valdorAttackIntervalMonths`: 해당 시나리오에서 발도르가 공격 출정을 검토하는 월 간격입니다.
- 아레스 메인은 프로스트혼 단독 시작, 프로스트혼-카르디아-룬포지/브론즈게이트 진출 연결, 발도르 24개월 공격 주기를 사용하며 프리 시나리오는 기본 마스터 배치를 사용합니다.
- `overrideInitialStats`: 시나리오 전용 첫 관문처럼 시작 성 수치를 별도로 지정할 때 사용합니다. 현재 카르디아만 방어 10·질서 20을 사용합니다.
- `recruitableHeroIds`: 성에 귀속된 인재 탐색 풀입니다. 해당 성에 있는 인물이 탐색할 때 귀속 인재를 먼저 발견합니다.
- 월드 UI는 `WIAdministrationWorldUGUI.prefab` 하나만 사용합니다. 성 소유 세력·좌표·연결은 프리팹이 아니라 선택한 시나리오의 `castlePlacements` 데이터와 성 마스터 데이터에서 결정합니다.
- Unity Inspector에서 `WI_AdministrationDatabase`의 `Campaign Variants`를 펼쳐 대상 시나리오의 `Castle Placements` 항목을 편집합니다. `Castle Id`는 성 식별자, `Faction Id`는 초기 소유 세력, `Override Map Position`과 `Normalized Map Position`은 위치, `Override Connections`와 `Adjacent Castle Ids`는 연결을 뜻합니다.
- 편집 경로는 Project 창의 `Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset` → Inspector의 `Campaign Variants` → `Ares Main` → `Castle Placements`입니다.
- 성 위치는 `Override Map Position`을 체크하고 `Normalized Map Position`을 수정합니다. X는 0이 왼쪽·1이 오른쪽이며, Y는 0이 아래·1이 위입니다.
- 성 연결은 두 성 모두 `Override Connections`를 체크하고 서로의 `Castle Id`를 `Adjacent Castle Ids`에 추가합니다. 한쪽만 입력하면 표시나 이동 판정이 비대칭이 될 수 있습니다.
- 초기 소유권은 해당 배치 행의 `Faction Id`를 `avalon`, `valdor`, `ironheart`, `sylvanroad`, `necropolis` 중 하나로 입력합니다.
- 프리 시나리오까지 공통으로 바꾸려면 위 시나리오 덮어쓰기가 아니라 같은 에셋의 기본 `Castles` 목록에서 성을 찾아 위치·연결·세력을 수정합니다.
