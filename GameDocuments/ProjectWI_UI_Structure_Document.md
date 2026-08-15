# ProjectWI 종합 UI 시스템 분석 및 설계 문서

본 문서는 **ProjectWI** 프로젝트의 게임 런타임 UI(Unity UI Toolkit 기반) 및 에디터 전용 툴 UI(Unity EditorWindow IMGUI 기반)의 전체적인 구조, 레이아웃 계층, 디자인 원칙, 컨트롤러 및 데이터 바인딩 동작 방식을 상세히 분석하여 기록한 종합 UI 명세서입니다.

---

## 1. 개요 및 UI 기술 스택 (Overview & UI Tech Stack)

ProjectWI의 UI는 역할과 실행 환경에 따라 2가지 기술 스택으로 엄격히 분리되어 개발되어 있습니다.

| 구 분 | 사용 기술 스택 | 주요 목적 및 대상 화면 |
| :--- | :--- | :--- |
| **게임 런타임 UI** | **Unity UI Toolkit** (`UnityEngine.UIElements`) | 공통 HUD, 대륙 전략 지도, 영지 관리, 전투 HUD, 캠페인 시작 레이어, 모달 팝업 (15종+) |
| **에디터 툴 UI** | **Unity IMGUI** (`UnityEditor.EditorWindow`) | 캐릭터 데이터 뷰어(Excel 그리드 편집기), 대규모 캐릭터 및 성 배치 시더 툴 |

### 핵심 UI 원칙
1. **런타임 동적 생성 금지 (Zero Runtime Instantiation)**:
   - 게임 실행 중 C# 코드로 UI 컴포넌트를 동적으로 생성(`new VisualElement()`, `Instantiate()`)하는 대신, 사전 정의된 UXML 파일에서 구조를 로드하여 `style.display` (Flex vs None) 조절 및 프로퍼티 바인딩만 수행합니다.
2. **Master Data 에셋 연동**:
   - UI에 표시되는 모든 데이터(성 수치, 영웅 스탯, 진영 정보, 아이콘 등)는 100% ScriptableObject 에셋([`WI_AdministrationDatabase.asset`](file:///c:/Fork/ProjectWI/Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset))과 직접 연동됩니다.
3. **선언적 스타일링 및 9-Slice 테마**:
   - 중세 판타지 테마의 골드/은색 테테두리 패널, 흑청색 배경, Noto Serif KR / Noto Sans KR 폰트를 USS 스타일시트로 관리합니다.

---

## 2. 런타임 UI 시스템 분석 (Runtime UI Systems)

### 2.1 공통 상단 HUD (Compact HUD)
* **UXML 위치**: [`Assets/UI/Administration/WIAdministration.uxml`](file:///c:/Fork/ProjectWI/Assets/UI/Administration/WIAdministration.uxml) (`#compact-hud`)
* **구성 요소**:
  * **진영 휘장 (`#faction-emblem`) & 플레이어 세력 이름 (`#faction-label`)**: 현재 선택한 플레이어 세력(예: 아발론)의 아이콘과 명칭
  * **날짜/턴 표기 (`#date-label`)**: 연도, 월, 턴 표기 (예: "190년 10월")
  * **3대 핵심 자원 칩**:
    * 금화 (`#gold-label`): 성 수입 및 군사 보급, 사업에 필요한 기본 자금
    * 마나 크리스탈 (`#mana-label`): 연구, 첩보, 마법 구사에 필요한 마법 자원
    * 영향력 (`#influence-label`): 외교, 의회 발언, 인재 영입에 필요한 정치 영향력
  * **시스템 컨트롤**: 정보 버튼 (`#information-button`), 설정 버튼 (`#system-button`)

---

### 2.2 대륙 전략 화면 (Global Strategy Map View)
대륙 전체의 60개 성과 진영 구도를 한눈에 파악하고 전략 명령을 내리는 메인 화면입니다.

![전략 지도 목표 시안](file:///c:/Fork/ProjectWI/GameDocuments/UIConcepts/ProjectWI_Strategy_UI_Concept_V1.png)

* **UXML 위치**: `WIAdministration.uxml` (`#global-view`)
* **주요 구성 패널**:
  1. **천하 전략도 지도 패널 (`#map-frame`)**:
     * **60개 성 노드 (`#castle-castle_00` ~ `#castle-castle_59`)**: 60개 성이 지도 상의 상대 위치(`left: %`, `top: %`)에 사각 버튼으로 정적배치. 성 소유 진영 색상의 마커 및 성 이름 표시.
     * **지도 연결선 자리 (`#map-connection-layer`)**: 과거 UXML 시안에는 일반 `VisualElement`만 남아 있으며 실제 런타임 지도는 UGUI 프리팹의 고정 성 노드를 사용합니다.
  2. **좌측 선택 성 정보 요약 카드 (`.castle-summary-panel`)**:
     * 성 전경 이미지 (`#global-castle-image`), 성 명칭 (`#global-castle-name`), 소유 세력 (`#global-castle-owner`), 4대 성 수치 (`#global-castle-stats`), 주둔 영웅 4명 카드 (`#global-castle-hero-cards`).
  3. **하단 전역 명령 툴바 (Global Command Bar)**:
     * **군사**: 전투단 편성, 부대 보급, 원정 준비
     * **영웅**: 전체 인물 조회, 인재 관리, 피로/충성도 점검
     * **외교**: 진영 간 동맹, 정전, 포로 몸값 협상 및 교환
     * **첩보**: 적 성 조사, 방첩, 유언비어, 이간책 실행
     * **연구**: 기술 발전에 따른 진영 패시브 및 시설 해금
     * **통치 / 의회**: 이번 달 세력 정책 수립 및 성 위임 관리
     * **월간 보고**: 지난 턴 결산 보고 및 선택 사건(Event Choice) 확인
  4. **우측 서브 패널 & 컨트롤**:
     * **목표 진행도 (`#objective-progress-fill`)**: 승리 목표 달성율
     * **전투 위협 경보 버튼 (`#battle-alert-button`)**: 접촉 발생 시 적색 강조 표시
     * **다음 턴 진행 버튼 (`#next-turn-button`)**: 월간 턴 연산 실행

---

### 2.3 영지 관리 화면 (Castle Administration View)
특정 성 노드를 선택하여 해당 영지의 내정을 집중 관리하는 개별 성 화면입니다.

![영지 관리 목표 시안](file:///c:/Fork/ProjectWI/GameDocuments/UIConcepts/ProjectWI_Castle_Administration_UI_Concept_V1.png)

* **UXML 위치**: `WIAdministration.uxml` (`#castle-view`)
* **주요 구성 요소**:
  1. **좌측 영지관 및 4대 성 수치 패널**:
     * **태수/영지관 전신 초상화 (`#governor-portrait-placeholder`)**
     * **4대 내정 수치**: 번영(`Prosperity`), 기술(`Technology`), 치안(`Stability`), 방어(`Defense`)
     * **수도 지정 및 특화 전문 분야**: 성의 규모 및 내정 특화 효과 표기
  2. **중앙 성 전경 일러스트 슬롯 (`#castle-background-placeholder`)**:
     * 성의 개성을 나타내는 비주얼 슬롯
  3. **우측 영지 관리 행동 패널**:
     * **중점 사업**: 번영/기술/치안/방어 중 이번 달 집중 개발 항목 선택
     * **영웅 배치**: 주둔 영웅 추가/제외
     * **인재 활동**: 탐색, 영입, 교류, 훈련, 휴식 실행
     * **특화 시설 선택**: 상업지구, 마법탑, 철광산 등 특화 건물 건설
     * **영지관 위임**: 태수에게 내정 자동 위임 및 월간 예산 설정
  4. **하단 영웅 & 시설 슬롯 패널**:
     * **주둔 영웅 스크롤뷰 (`#hero-slots`)**: 최대 8명의 주둔 영웅 독립 카드 스크롤 표시
     * **특화 시설 스크롤뷰 (`#special-facility-slots`)**: 2개의 특화 시설 독립 카드 스크롤 표시

---

### 2.4 전투 HUD 화면 (Battle HUD View)
실시간 2D tactical 전투 씬에서 플레이어가 부대에 명령을 내리고 전황을 파악하는 인터페이스입니다.

* **UXML 위치**: [`Assets/UI/Battle/WIBattleHUD.uxml`](file:///c:/Fork/ProjectWI/Assets/UI/Battle/WIBattleHUD.uxml)
* **C# 컨트롤러**: [`WIBattleHUDController.cs`](file:///c:/Fork/ProjectWI/Assets/Scripts/Battle/WIBattleHUDController.cs)
* **주요 요소**:
  1. **상황판 (`#battle-status`)**: 전장 목표 이름, 경과 시간, 생존 아군/적군 부대 수
  2. **부대 전술 명령 버튼 그룹**:
     * **전진 (`#advance-command`)**: 아군 부대 전방 돌격
     * **대기 (`#hold-command`)**: 아군 부대 현재 위치 방어 진형
     * **집중공격 (`#focus-command`)**: 가장 가까운 적 부대 집중 화력 사격
     * **후퇴 (`#retreat-command`)**: 질서 있는 후퇴 (3초 내 재클릭 확정 안전장치 적용)
  3. **영웅 액티브 스킬 패널 (`#skill-buttons`)**: 출전 영웅의 액티브 스킬 아이콘, 쿨다운 쿨타임 오버레이, 클릭 및 단축키(1~4번) 발동

---

### 2.5 캠페인 시작 화면 & 모달 팝업 레이어 (Start Screen & Modals)
* **캠페인 시작 레이어 (`#campaign-start-layer`)**:
  * 난이도 선택: **여유** (초보자용), **표준** (기본), **도전** (고급 AI)
  * 시나리오 선택: **클래식 통일전**, **군웅 분할**
  * 자동 저장 이어하기 및 새로 시작 버튼
* **통합 모달 팝업 레이어 (`#modal-layer`)**:
  * 15종 이상의 모달(의회, 첩보, 외교, 포로 교환, 월간 보고, 연구 트리, 영웅 관리 등)이 공통 팝업 템플릿 프레임을 공유하며 켜지고 닫힘.

---

## 3. 에디터 UI 툴 및 개발자 도구 (Editor UI Tools)

개발 및 데이터 관리를 위해 Unity IMGUI(`UnityEditor.EditorWindow`) 기반으로 제작된 전용 에디터 도구입니다.

### 3.1 캐릭터 데이터 뷰어 (`WICharacterDataViewerWindow.cs`)
* **메뉴 경로**: `ProjectWI > Viewer > Character Data Viewer`
* **주요 기능**:
  * **Excel 그리드 방식 데이터 편집기**: `WI_AdministrationDatabase.asset`에 등록된 캐릭터 데이터를 테이블 형태로 한눈에 조회 및 즉시 편집.
  * **극강의 렌더링 성능 최적화 (Virtual Scroll Windowing & Pagination)**:
    * 500명의 캐릭터 데이터 처리 시 프레임 드랍을 막기 위해 **25/50/100/500개씩 보기 페이지네이션** 및 **현재 화면 뷰포트에 보이는 20개 행만 선택적 렌더링하는 가상 스크롤** 기법 적용.
    * **C# `CharacterCache` 메모리 캐싱**: 유니티 C++ Native `SerializedProperty` 72,000회 호출을 20회(99.97% 감소)로 줄여 **60+ FPS 초고속 반응속도** 보장.
  * **정밀 픽셀 정렬 그리드 Layout**: headers와 data rows 간 오차 없는 absolute `Rect` pixel-grid 렌더링.
  * **컬럼 정렬 & 검색**: ID, 한글 이름, 영문 이름, 등급, 종족, 직업, 5대 능력치(통솔, 무력, 지력, 정치, 매력), 충성도, 명성 헤더 클릭 시 오름차순/내림차순 정렬 및 검색어 필터링.
  * **초상화 Sprite 지정 및 삭제**: 초상화 에셋 Drag & Drop 지정 및 단일 캐릭터 삭제 지원.
  * **Undo/Redo 및 자동 저장**: 유니티 `SerializedObject` 연동으로 Ctrl+Z/Ctrl+Y 실행 취소 지원.

---

### 3.2 대규모 인물 시딩 툴 (`WIMassCharacterSeeder.cs`)
* **메뉴 경로**: `ProjectWI > Data > Seed Mass Character Roster (100 Heroes, 400 Commons)`
* **주요 기능**:
  * 총 500명(영웅 100명 + 일반 400명, Human 60%, 기타 6종족 각 6.67%)의 판타지 이름 및 직업 밸런스 능력치를 자동 생성하여 `WI_AdministrationDatabase.asset`에 구워 넣음.
  * 8대 세력 주요 영웅 식별자 (`ares`, `lyria`, `brom`, `morrigan`, `theron` 등) 보존.
  * 전체 500명 중 **1/3(167명)을 60개 성에 무작위 비율 배치**하며, 주인공 아발론 수도(`castle_00`)에는 영웅 1, 2호인 `ares`와 `lyria`가 반드시 배치되도록 자동 시딩 처리.

---

## 4. UI 에셋 및 코드 파일 구조 맵 (File & Asset Map)

| 구분 | 파일 경로 | 설명 |
| :--- | :--- | :--- |
| **UXML** | [`Assets/UI/Administration/WIAdministration.uxml`](file:///c:/Fork/ProjectWI/Assets/UI/Administration/WIAdministration.uxml) | 내정, 전략 지도, 팝업, 시작 레이어 구조 |
| **UXML** | [`Assets/UI/Battle/WIBattleHUD.uxml`](file:///c:/Fork/ProjectWI/Assets/UI/Battle/WIBattleHUD.uxml) | 전투 HUD 전술 버튼 및 상황판 구조 |
| **USS** | [`Assets/UI/Administration/WIAdministration.uss`](file:///c:/Fork/ProjectWI/Assets/UI/Administration/WIAdministration.uss) | 내정/지도/모달 전체 CSS 스타일시트 |
| **USS** | [`Assets/UI/Battle/WIBattleHUD.uss`](file:///c:/Fork/ProjectWI/Assets/UI/Battle/WIBattleHUD.uss) | 전투 HUD 스타일시트 |
| **C#** | [`Assets/Scripts/Administration/WIAdministrationUIController.cs`](file:///c:/Fork/ProjectWI/Assets/Scripts/Administration/WIAdministrationUIController.cs) | 내정 UI 컨트롤러 (데이터 바인딩, 뷰 전환) |
| **C#** | [`Assets/Scripts/Administration/WIAdministrationMapUGUIController.cs`](file:///c:/Fork/ProjectWI/Assets/Scripts/Administration/WIAdministrationMapUGUIController.cs) | UGUI 지도 성 노드의 소유·선택 상태와 입력 갱신 |
| **C#** | [`Assets/Scripts/Battle/WIBattleHUDController.cs`](file:///c:/Fork/ProjectWI/Assets/Scripts/Battle/WIBattleHUDController.cs) | 전투 HUD 컨트롤러 (명령, 스킬, 이벤트) |
| **C# (Editor)** | [`Assets/Editor/WICharacterDataViewerWindow.cs`](file:///c:/Fork/ProjectWI/Assets/Editor/WICharacterDataViewerWindow.cs) | 에디터 캐릭터 데이터 뷰어 (Virtual Scroll Windowing) |
| **C# (Editor)** | [`Assets/Editor/WIMassCharacterSeeder.cs`](file:///c:/Fork/ProjectWI/Assets/Editor/WIMassCharacterSeeder.cs) | 에디터 500명 인물 생성 및 1/3 성 배치 시더 |
| **에셋 문서** | [`GameDocuments/UIManual.md`](file:///c:/Fork/ProjectWI/GameDocuments/UIManual.md) | UI/UX 매뉴얼 |
| **인수인계** | [`GameDocuments/UIHandoffReport.md`](file:///c:/Fork/ProjectWI/GameDocuments/UIHandoffReport.md) | UI 디자인 인수인계 및 시안 보고서 |
