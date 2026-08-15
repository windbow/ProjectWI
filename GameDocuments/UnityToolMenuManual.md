# ProjectWI Unity 도구 메뉴 설명서

**기준일:** 2026-08-15  
**조사 기준:** `Assets/Editor`의 실제 `MenuItem` 선언, 호출 코드, 생성 에셋과 현재 프로젝트 연결 상태

## 1. 메뉴 구성과 안전 등급

Unity 상단의 프로젝트 전용 메뉴는 두 루트로 나뉩니다.

- `ProjectWI`: 데이터 관리, 데이터 편집기, 전투·캠페인 테스트와 성능 검증
- `WI`: UGUI 프리팹 생성, 화면 관리자 구성, 해상도·화면 QA와 캡처

| 등급 | 의미 |
|---|---|
| 조회 | 에셋이나 게임 상태를 변경하지 않고 창 또는 정보를 표시합니다. |
| 임시 상태 | 플레이 모드의 메모리 상태만 변경하며 캠페인 저장에는 반영하지 않습니다. |
| 파일 생성 | CSV, Markdown 또는 Screenshot 파일을 생성·덮어씁니다. |
| 에셋 변경 | Prefab, Scene 또는 ScriptableObject를 저장합니다. 실행 전 Git Diff 확인이 필요합니다. |
| 대량 변경 | 배열 전체나 다수 프리팹을 재작성합니다. 실행 전 확인과 실행 후 회귀 테스트가 필요합니다. |

## 2. 이번 정리에서 제거한 메뉴

아래 메뉴는 현재 데이터에 이미 결과가 반영된 일회성 시더이거나 최신 구조를 과거 값으로 되돌릴 가능성이 있어 제거했습니다. 런타임은 이 코드에 의존하지 않습니다.

| 제거 메뉴 | 제거 이유 | 현재 관리 방법 |
|---|---|---|
| `ProjectWI/Data/Seed Hero Battle Skills` | 핵심 영웅 스킬이 `WI_BattleConfig.asset`에 저장되어 있고 재실행 시 수동 밸런스 값을 덮어쓸 수 있습니다. | 전투 설정 ScriptableObject에서 직접 편집합니다. |
| `ProjectWI/Data/Seed Campaign Difficulties` | 난이도 3종이 데이터베이스에 확정되어 있습니다. 함께 있던 표준 캠페인 시작 메뉴는 QA Onboarding 메뉴와 중복됩니다. | 난이도 정의를 데이터베이스에서 직접 편집하고 `New Campaign Onboarding UGUI`로 확인합니다. |
| `ProjectWI/Data/Seed Castle Names And Lore` | 60개 성의 이름·지형·설명이 확정되었습니다. | Character/Database 데이터 편집과 전용 검사를 사용합니다. |
| `ProjectWI/Data/Classify Castle Specialty Effects` | 전문 분야 분류가 완료됐으며 문구 추론을 다시 수행할 이유가 없습니다. | 성별 효과 필드를 직접 편집합니다. |
| `ProjectWI/Data/Seed Common Roster` | 소규모 일반 인물 시더가 현재 500명 로스터와 중복됩니다. | `Seed Mass Character Roster`와 Character Data Viewer를 사용합니다. |
| `ProjectWI/Data/Assign Faction Emblems` | 문장 Sprite 연결과 Import 설정이 완료됐습니다. | 진영 ScriptableObject와 Texture Importer에서 직접 관리합니다. |
| `ProjectWI/Data/Assign Geographic Castle Layout` | 60개 성 좌표와 도로망이 확정됐고 재실행 시 지도 수동 조정을 덮어쓸 수 있습니다. | 성 데이터의 좌표·인접 UID를 직접 편집합니다. |
| `ProjectWI/Data/Seed Scheme Definitions` | 첩보 4종의 정의가 데이터베이스에 저장되어 있습니다. | ScriptableObject에서 직접 편집합니다. |
| `ProjectWI/Data/Assign Ares Visual Assets` | 개별 연결 작업이 완료됐으며 현재 500명 검증 데이터와 충돌할 수 있습니다. | Character Data Viewer 또는 Inspector에서 Sprite를 지정합니다. |
| `ProjectWI/Data/Assign Shared Visual Assets` | 여러 이미지와 500명 데이터를 일괄 덮어쓰는 복구용 코드였습니다. | 이미지 Import 설정과 데이터 참조를 개별 관리합니다. |
| `ProjectWI/Data/Assign Visual Assets` | 5대 수도 전경 연결이 완료된 일회성 도구입니다. | 성 데이터의 이미지 필드를 직접 관리합니다. |
| `ProjectWI/Verification/Start Standard Campaign` | `WI/QA/Preview/New Campaign Onboarding UGUI`와 기능이 겹칩니다. | QA Onboarding 메뉴를 사용합니다. |
| `ProjectWI/Verification/Open Common Castle Preview` | `WI/QA/Preview/Avalon Castle` 및 다른 화면별 Preview와 중복됩니다. | WI/QA Preview 메뉴를 사용합니다. |
| `WI/Battle/Configure Ares Battle Sprite Defaults` | 현재 Ares Sprite의 Import 설정이 확정됐으며 재실행 메뉴가 필요하지 않습니다. | Texture Importer와 `BattleSceneAssetSettingsGuide.md`를 기준으로 관리합니다. |

## 3. ProjectWI 메뉴

### 3.1 ProjectWI/Data

#### `ProjectWI/Data/Seed Mass Character Roster (100 Heroes, 400 Commons)`

- 구현: `WIMassCharacterSeeder.SeedMassRoster`
- 목적: 현재 검증 기준인 영웅 100명과 일반 인물 400명을 결정론적 시드 `20260806`으로 재구성합니다.
- 처리: `heroes` 배열을 먼저 비운 뒤 500명을 다시 만들고, 핵심 영웅 ID, 시작 배치, 진영, 관계와 기본 데이터를 동기화합니다.
- 변경 대상: `Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset`
- 호출 경로: 상단 메뉴뿐 아니라 Character Data Viewer의 `500명 인물 생성` 버튼도 같은 함수를 호출합니다.
- 위험: 기존 500명에 적용한 이름, 능력치, Sprite, 관계 등의 수동 수정이 사라질 수 있습니다. 실행 전 데이터베이스 Diff 또는 백업이 필수입니다.
- 안전 등급: **대량 변경**

이 메뉴는 현재 데이터 뷰어의 명시적 재생성 기능이므로 유지합니다. 일반적인 데이터 편집에는 사용하지 않고 로스터를 처음부터 다시 만들 때만 사용합니다.

### 3.2 ProjectWI/Tools

#### `ProjectWI/Tools/Battle Test Lab`

- 구현: `WIBattleTestLabWindow`
- 목적: 데이터베이스의 인물을 아군·적군에 배치하고 각 참가자의 전투 역할을 지정해 독립 전투를 실행합니다.
- 실행 흐름: 편성 정보를 `EditorPrefs`에 JSON으로 임시 저장하고 `MainScene`을 연 뒤 플레이 모드로 진입합니다. 런타임 부트스트랩이 데이터를 읽어 `BattleScene`으로 전환합니다.
- 결과: 캠페인 저장과 분리된 임시 전투 세션이므로 전략 상태에 보상·부상·점령을 기록하지 않습니다.
- 사용 시점: 전투 AI, 진형, 역할 조합, 스킬, HUD와 카메라의 수동 검증
- 안전 등급: **임시 상태**

#### `ProjectWI/Tools/Campaign Auto Test Lab`

- 구현: `WICampaignAutoTestLabWindow`
- 목적: 난이도, 자동 플레이 정책과 최대 개월을 지정해 캠페인을 자동 실행합니다.
- 단일 실행: 선택한 난이도 1종 × 정책 1종을 실행합니다.
- 행렬 실행: 여유·표준·도전 × 내정형·균형형·공세형의 9개 조합을 실행합니다.
- 수집 결과: 진행 개월, 승패, 소유 성, 전투, 원정, 대기 선택과 정책별 지표를 표에 표시합니다.
- 파일 출력: 사용자가 내보내기를 누를 때만 UTF-8 CSV 또는 Markdown을 생성합니다.
- 주의: 월 수를 최대 1200까지 지정할 수 있어 큰 값은 에디터를 잠시 점유합니다. 실제 캠페인 저장은 변경하지 않습니다.
- 안전 등급: 실행은 **조회**, 내보내기는 **파일 생성**

### 3.3 ProjectWI/Viewer

#### `ProjectWI/Viewer/Character Data Viewer`

- 구현: `WICharacterDataViewerWindow`
- 목적: 500명 캐릭터를 등급·검색어·정렬 조건으로 조회하고 스프레드시트 형태로 편집합니다.
- 편집 기능: 셀 수정, 캐릭터 추가·삭제, 페이지 이동, Undo/Redo와 데이터베이스 저장을 지원합니다.
- 특수 기능: `500명 인물 생성` 버튼은 `WIMassCharacterSeeder`를 호출해 전체 배열을 재작성합니다.
- 변경 시점: 일반 조회는 저장하지 않지만 셀 수정 후 저장하거나 생성·삭제를 수행하면 데이터베이스가 변경됩니다.
- 안전 등급: 조회는 **조회**, 편집·저장은 **에셋 변경**, 500명 생성은 **대량 변경**

### 3.4 ProjectWI/Verification

#### `ProjectWI/Verification/Run Battle Performance Benchmark`

- 구현: `WIBattlePerformanceBenchmark.RunBenchmark`
- 처리: 20명, 40명, 60명 전투 상태를 각각 생성하고 1/60초 간격으로 600스텝을 계산합니다.
- 출력: 총 처리 시간, 종료 시 남은 발사체 수와 시각 효과 수를 Console에 기록합니다.
- 특징: GameObject와 화면 렌더링이 아닌 `WIBattleSimulation` 순수 계산 성능을 측정합니다.
- 변경 대상: 없음
- 안전 등급: **조회**

#### `ProjectWI/Verification/Start 30v30 Battle Density Test`

- 구현: `WIBattleTestLabWindow.StartThirtyVsThirtyBattleDensityTest`
- 참가자: 양측 각각 영웅 1명과 일반 인물 29명입니다. 아군 영웅은 Ares를 우선 사용합니다.
- 선행 조건: 영웅 2명 이상, 일반 인물 58명 이상과 `MainScene`이 필요합니다.
- 실행 흐름: 편성 데이터를 `EditorPrefs`에 기록하고 MainScene을 연 뒤 플레이 모드를 시작합니다.
- 목적: 60명 밀도, 겹침, 그리드 이동, Y 정렬, HUD, 프레임 성능과 전장 가독성 검증
- 안전 등급: **임시 상태**

#### `ProjectWI/Verification/Battle Zoom/A Near`

- 실행 조건: 플레이 모드이며 현재 전투 카메라가 존재해야 합니다.
- 동작: 카메라를 근거리 A 단계, Orthographic Size 6으로 전환합니다.
- 용도: 캐릭터 Sprite 경계, 애니메이션과 근접 전투 판독성 확인
- 안전 등급: **임시 상태**

#### `ProjectWI/Verification/Battle Zoom/B Middle`

- 실행 조건: 플레이 모드이며 현재 전투 카메라가 존재해야 합니다.
- 동작: 카메라를 중거리 B 단계, Orthographic Size 8로 전환합니다.
- 용도: 기본 집단 전투 밀도와 진형 가독성 확인
- 안전 등급: **임시 상태**

#### `ProjectWI/Verification/Battle Zoom/C Far`

- 실행 조건: 플레이 모드이며 현재 전투 카메라가 존재해야 합니다.
- 동작: 카메라를 원거리 C 단계, Orthographic Size 10으로 전환합니다.
- 용도: 30대30 전체 전선, 외곽선과 전장 배치 확인
- 안전 등급: **임시 상태**

#### `ProjectWI/Verification/Run Campaign Long Benchmark`

- 구현: `WICampaignPerformanceBenchmark`
- 처리: 각각 새 표준 캠페인을 만들어 120, 240, 480개월을 자동 계산합니다.
- 출력: 실행 시간, GC 전후 메모리 변화, 대기 사건 수, 전투단, 전투 세션, 이동, 첩보 임무, 월간 보고 항목과 직렬화 저장 크기를 Console에 기록합니다.
- 목적: 장기 실행 시 컬렉션 무제한 증가, 저장 비대화와 성능 회귀 확인
- 변경 대상: 실제 저장 슬롯과 ScriptableObject는 변경하지 않습니다.
- 안전 등급: **조회**

## 4. WI/UI 메뉴

모든 `Build ... UGUI` 메뉴는 런타임 UI를 동적으로 만드는 기능이 아닙니다. 에디터에서 실제 Prefab을 생성·갱신하고 `WIUIScreenManager`에 등록합니다. 기존 Prefab을 직접 수정한 내용은 재생성 과정에서 덮어쓸 수 있습니다.

| 메뉴 | 생성·갱신 대상 | 주요 구성 | 실행 영향 |
|---|---|---|---|
| `WI/UI/Build Administration World UGUI` | 전역 화면 Prefab | 자원 HUD, 성 요약, 60개 성 지도, 목표·알림, 하단 명령 | 대형 Prefab 재저장 및 화면 관리자 등록 |
| `WI/UI/Build Administration Territory UGUI` | 영지 상세 Prefab | 성 현황, 영지관, 4대 수치, 영웅·시설 슬롯, 영지 명령 | 대형 Prefab 재저장 및 화면 관리자 등록 |
| `WI/UI/Build Focus Project UGUI` | 중점 사업 Prefab | 사업 카드, 담당자 선택, 비용·예상 결과 | 해당 Prefab 재저장 |
| `WI/UI/Build Hero Assignment UGUI` | 영웅 배치 Prefab | 후보 카드, 페이지 이동, 배치·해제 | 해당 Prefab 재저장 |
| `WI/UI/Build Character Activity UGUI` | 인재 활동 Prefab | 인물, 활동, 대상의 단계별 선택 | 해당 Prefab 재저장 |
| `WI/UI/Build Special Facility UGUI` | 특화 시설 Prefab | 8개 시설 후보와 조건·효과 | 해당 Prefab 재저장 |
| `WI/UI/Build Basic Facility UGUI` | 기본 시설 Prefab | 기본 시설, 선술집 의뢰, 담당자 선택 | 해당 Prefab 재저장 |
| `WI/UI/Build Delegation UGUI` | 위임 설정 Prefab | 영지관, 운영 방침, 예산, 위임 토글 | 해당 Prefab 재저장 |
| `WI/UI/Build March UGUI` | 원정 Prefab | 전투단, 대장, 이동·공격 목표 | 해당 Prefab 재저장 |
| `WI/UI/Build Castle Record UGUI` | 성 기록 Prefab | 성 이미지와 읽기 전용 기록 ScrollView | 해당 Prefab 재저장 |
| `WI/UI/Build Objective UGUI` | 캠페인 목표 Prefab | 상황, 조건, 진행도와 보상 | 해당 Prefab 재저장 |
| `WI/UI/Build Monthly Report UGUI` | 월간 보고 Prefab | 보고 ScrollView, 사건·전투 행동 슬롯 | 해당 Prefab 재저장 |
| `WI/UI/Build Military UGUI` | 군사 Prefab | 미결 전투, 전투단 편성·상세·이동 | 해당 Prefab 재저장 |
| `WI/UI/Build Heroes UGUI` | 영웅 관리 Prefab | 목록, 상세, 승격과 작위 | 해당 Prefab 재저장 |
| `WI/UI/Build Diplomacy UGUI` | 외교 Prefab | 대상 진영과 외교 행동 카드 | 해당 Prefab 재저장 |
| `WI/UI/Build Scheme UGUI` | 첩보 Prefab | 첩보 종류, 담당자와 대상 선택 | 해당 Prefab 재저장 |
| `WI/UI/Build Research UGUI` | 연구 Prefab | 연구 목록, 조건과 담당자 | 해당 Prefab 재저장 |
| `WI/UI/Build Faction UGUI` | 진영 정세 Prefab | 진영별 영토·관계·상태 카드 | 해당 Prefab 재저장 |
| `WI/UI/Build Council UGUI` | 의회 Prefab | 6개 월간 진영 방침 | 해당 Prefab 재저장 |
| `WI/UI/Build Event Choice UGUI` | 선택 사건 Prefab | 일반 사건 선택과 영웅의 흔적 교체 | 해당 Prefab 재저장 |
| `WI/UI/Build Turn Followup UGUI` | 턴 후속 Prefab | 턴 처리, 튜토리얼, 결과와 공통 안내 | 해당 Prefab 재저장 |
| `WI/UI/Build System UGUI` | 시스템 Prefab | 설정과 저장·불러오기 2페이지 | 해당 Prefab 재저장 |

각 Build 메뉴는 생성한 Prefab을 `WIAdministrationUGUISceneUtility.InstantiateUnderSceneRoot`에 전달합니다. 이 함수는 Scene에 화면 인스턴스를 펼치지 않고 `UGUI Screen Bootstrap`의 `WIUIScreenManager.screenPrefabs` 목록에 Prefab 참조를 등록합니다.

### `WI/UI/Configure UGUI Scene Visibility`

- 실제 동작: 현재 Scene에서 `UGUI Screen Bootstrap`을 찾거나 만들고 `WIUIScreenManager`를 보장합니다.
- 수집 범위: 기존 `screenPrefabs`, Scene 자식의 원본 Prefab, `Assets/Prefabs/Administration` 아래 이름이 `UGUI.prefab`으로 끝나는 모든 Prefab입니다.
- 정리: 중복과 null을 제거해 `screenPrefabs`를 다시 기록한 후 Bootstrap 아래에 펼쳐진 모든 화면 자식을 삭제합니다.
- 저장: 현재 열린 Scene을 Dirty 처리하고 즉시 저장합니다.
- 실행 위치: `MainScene`에서만 사용하는 것이 안전합니다.
- 위험: 이름과 달리 단순 표시 전환이 아니라 Scene 계층을 변경하고 저장합니다.
- 안전 등급: **대량 변경**

## 5. WI/QA 메뉴

### 5.1 Game View 해상도

| 메뉴 | 세부 동작 | 변경 범위 |
|---|---|---|
| `WI/QA/Game View/1366x768` | Standalone Game View 목록에서 1366×768 고정 해상도를 찾아 선택하고, 없으면 `WI 1366x768` 항목을 추가합니다. | Unity Editor Game View 설정만 변경 |
| `WI/QA/Game View/1920x1080` | 1920×1080 고정 해상도를 찾아 선택하거나 추가합니다. 기준 PC UI 캡처에 사용합니다. | Unity Editor Game View 설정만 변경 |
| `WI/QA/Game View/2560x1440` | 2560×1440 고정 해상도를 찾아 선택하거나 추가합니다. 고해상도 여백과 스케일 검증에 사용합니다. | Unity Editor Game View 설정만 변경 |

세 메뉴 모두 Unity 내부 비공개 Game View API를 Reflection으로 호출합니다. Player 해상도, 품질 설정과 게임 저장값은 변경하지 않습니다.

### 5.2 화면 Preview

모든 Preview 메뉴는 플레이 모드에서 `WIAdministrationUIController`를 찾아 고정 QA 상태를 준비하고 해당 UGUI Prefab을 엽니다. 편집 모드에서는 상태를 만들지 않고 경고만 출력합니다.

| 메뉴 | 준비·표시하는 상태 | 상태 변경 주의 |
|---|---|---|
| `WI/QA/Preview/Global Map` | 기본 캠페인 상태와 전역 지도 | QA 런타임 상태만 변경 |
| `WI/QA/Preview/Avalon Castle` | Avalon 성 선택과 영지 상세 | 선택 성과 화면 상태 변경 |
| `WI/QA/Preview/Focus Project UGUI` | 중점 사업 및 담당자 후보 | QA용 성·후보 상태 준비 |
| `WI/QA/Preview/Hero Assignment UGUI` | 영웅 배치 후보와 페이지 | QA용 후보 상태 준비 |
| `WI/QA/Preview/Character Activity UGUI` | 인물 활동 및 대상 선택 | QA용 후보 상태 준비 |
| `WI/QA/Preview/Special Facility UGUI` | 특화 시설 후보 8종 | QA용 조건 상태 준비 |
| `WI/QA/Preview/Basic Facility UGUI` | 기본 시설과 선술집 의뢰 | QA용 시설·의뢰 상태 준비 |
| `WI/QA/Preview/Delegation UGUI` | 영지관, 방침과 예산 | QA용 영지관 상태 준비 |
| `WI/QA/Preview/March UGUI` | 전투단과 인접 원정 목표 | QA용 전투단·목표 상태 준비 |
| `WI/QA/Preview/Castle Record UGUI` | 성 이미지와 긴 기록 본문 | 표시 상태만 변경 |
| `WI/QA/Preview/Objective UGUI` | 현재 캠페인 목표, 진행도와 보상 | 표시 상태만 변경 |
| `WI/QA/Preview/Monthly Report UGUI` | 긴 월간 보고와 행동 슬롯 | QA용 보고 상태 준비 |
| `WI/QA/Preview/Military UGUI` | 미결 전투와 전투단 관리 | QA용 군사 상태 준비 |
| `WI/QA/Preview/Heroes UGUI` | 영웅 목록, 상세, 승격·작위 | QA용 영웅 상태 준비 |
| `WI/QA/Preview/Diplomacy UGUI` | 진영 관계와 외교 행동 | QA용 외교 상태 준비 |
| `WI/QA/Preview/Scheme UGUI` | 첩보 종류, 담당자와 대상 | QA용 첩보 상태 준비 |
| `WI/QA/Preview/Research UGUI` | 연구 목록, 조건과 담당자 | QA용 연구 상태 준비 |
| `WI/QA/Preview/Faction UGUI` | 전체 진영 정세 카드 | 표시 상태만 변경 |
| `WI/QA/Preview/Council UGUI` | 6개 월간 방침 | QA용 의회 상태 준비 |
| `WI/QA/Preview/System UGUI` | 전역 상태를 먼저 만든 뒤 설정·저장 화면 | QA 런타임 상태만 변경 |
| `WI/QA/Preview/Advance Turn UGUI` | 실제 다음 턴 계산과 후속 보고 흐름 | 런타임 월·자원·사건 상태 변경 |
| `WI/QA/Preview/New Campaign Onboarding UGUI` | 표준 난이도·정통 재건으로 새 캠페인을 시작하고 타이틀을 닫은 상태 | 기존 플레이 모드 상태를 새 캠페인으로 교체 |
| `WI/QA/Preview/Common Message UGUI` | 공통 오류·안내 메시지 | 표시 상태만 변경 |

Preview 상태는 플레이 모드를 종료하면 사라지는 것이 원칙입니다. 다만 실제 저장 기능을 별도로 누르면 저장 슬롯에 기록될 수 있으므로 `Advance Turn`과 `New Campaign Onboarding` 검증 중에는 저장 버튼을 사용하지 않습니다.

### 5.3 Capture

#### `WI/QA/Capture/Current Game View`

- 실행 조건: 플레이 모드
- 동작: `ScreenCapture.CaptureScreenshot`으로 현재 Game View를 1배 배율로 캡처합니다.
- 출력: `Assets/Screenshots/UI_Current_Preview.png`
- 주의: 같은 경로의 기존 파일을 덮어쓸 수 있으며 Unity가 새 에셋으로 임포트합니다.
- 안전 등급: **파일 생성**

## 6. 권장 작업 흐름

### UGUI 화면 수정

1. 현재 Prefab 변경분을 확인합니다.
2. 해당 `WI/UI/Build ... UGUI` 메뉴를 실행합니다.
3. MainScene에서 화면 관리자 구성이 의심될 때만 `Configure UGUI Scene Visibility`를 실행합니다.
4. 플레이 모드에 진입하고 기준 Game View 해상도를 선택합니다.
5. 대응하는 Preview 메뉴로 화면을 엽니다.
6. Console 오류와 Prefab Diff를 확인합니다.
7. 필요할 때만 Current Game View 캡처를 저장합니다.

### 전투 수정

1. 조합 검증은 `Battle Test Lab`을 사용합니다.
2. 최대 밀도 검증은 `Start 30v30 Battle Density Test`를 사용합니다.
3. A/B/C Zoom 메뉴로 같은 전투를 거리별로 확인합니다.
4. 순수 계산 회귀는 `Run Battle Performance Benchmark`로 확인합니다.

### 캠페인 수정

1. 정책별 결과는 `Campaign Auto Test Lab`에서 확인합니다.
2. 누적 성능·저장 크기는 `Run Campaign Long Benchmark`로 확인합니다.
3. 실제 시작 UX는 `New Campaign Onboarding UGUI`로 확인합니다.

## 7. 유지 원칙

- 일회성 데이터 변환 코드는 결과가 ScriptableObject에 확정되면 메뉴에서 제거합니다.
- 현재 데이터 전체를 덮어쓰는 메뉴는 명확한 재생성 요구가 있을 때만 유지합니다.
- Build 메뉴는 런타임 동적 UI 생성용이 아니라 에디터에서 Prefab을 만드는 제작 도구로만 사용합니다.
- 새 메뉴를 추가할 때는 이 문서에 실행 모드, 변경 대상, 출력, 실패 조건과 복구 방법을 함께 기록합니다.
- `ProjectWI`와 `WI` 루트 통합은 모든 코드·문서·사용자 작업 흐름을 함께 변경해야 하므로 이번 정리 범위에서는 수행하지 않았습니다.
