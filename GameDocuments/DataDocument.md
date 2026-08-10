# ProjectWI 씬 및 에셋 구조

## 1. MainScene 하이어라키

- `Main Camera`: 2D 전략 화면 렌더링 카메라입니다.
- `Global Light 2D`: 2D 렌더러의 전역 조명입니다.
- `EventSystem`: PC와 모바일 UI 입력을 처리합니다.
- `ManagerObjects`: 전략 게임 시스템 오브젝트의 루트입니다.
  - `AdministrationUI`: 영지 관리 UI 프리팹 인스턴스입니다.

## 2. AdministrationUI

- `UIDocument`
  - `WIAdministration.uxml`: 화면 템플릿을 조립하는 루트
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
  - 성 상태 표시와 월간 중점 사업 지정
  - 진영 의회, 영지관 위임과 월간 보고 표시
  - 인물 현황과 월간 개인 활동 배정
  - 전투단 생성, 역할 편성, 이동과 원정
  - 합동 훈련, 편성 변경, 해산과 점령 안정
  - 턴 실행과 결과 표시

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
- `WICommonRosterSeeder.cs`: 일반 인물 원본 데이터를 중복 없이 갱신하는 Unity 편집기 메뉴
- `WIAdministrationUIController.cs`: UI 참조 캐시, 공통 이벤트와 표시 갱신
- `WIAdministrationUIController.*.cs`: 캠페인·지도·영지·인물·군사·진영 관계·보고·턴 기능별 partial 컨트롤러
- `WIMapConnectionLayer.cs`: 인접 성 경로, 접경 전선과 선택 경로의 UI Toolkit 벡터 렌더링
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
