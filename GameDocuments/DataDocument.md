# ProjectWI 씬 및 에셋 구조

## 1. MainScene 하이어라키

- `Main Camera`: 2D 전략 화면 렌더링 카메라입니다.
- `Global Light 2D`: 2D 렌더러의 전역 조명입니다.
- `EventSystem`: PC와 모바일 UI 입력을 처리합니다.
- `ManagerObjects`: 전략 게임 시스템 오브젝트의 루트입니다.
  - `AdministrationUI`: 내정 UI 프리팹 인스턴스입니다.

## 2. AdministrationUI

- `UIDocument`
  - `WIAdministration.uxml`
  - `WIAdministrationPanelSettings.asset`
- `WIAdministrationUIController`
  - 내정 데이터베이스 참조
  - 캠페인 런타임 상태 생성
  - UXML에 고정 배치된 60개 지도 노드와 HUD 데이터 갱신
  - 성 선택 및 Bottom Sheet
  - 영웅 배치, 기본 시설 안내, 특화 시설 선택
  - 성 상태 표시와 월간 중점 사업 지정
  - 세력 평정, 태수 위임과 월보 표시
  - 인물 현황과 월간 개인 활동 배정
  - 부대 생성, 역할 편성, 이동과 출정
  - 합동 훈련, 편성 변경, 해산과 점령 안정
  - 턴 실행과 결과 표시

## 3. 프리팹

- `Assets/Prefabs/Administration/WIAdministrationUI.prefab`
  - 내정 화면의 재사용 루트
  - UXML에 고정된 60개 성 노드에 ScriptableObject 좌표·이름·소유 세력을 바인딩
  - 전역 4개 영웅 카드, 성 화면 8개 영웅 슬롯과 2개 특화 시설 슬롯을 고정 보유
  - 지도, 문장, 태수 초상화는 빈 이미지 슬롯 제공
- `Assets/Prefabs/Battle/WIBattleCharacter.prefab`
  - SpriteRenderer와 `WIBattleCharacterView`로 구성된 교체 가능한 빈 전투 캐릭터 표시 프리팹

## 4. 데이터 에셋

- `Assets/Data/ScriptableObject/Administration/WI_AdministrationDatabase.asset`
  - 전역 지도, 세력 문장, 인물 초상화, 성 전경과 시설 아이콘 Sprite 참조
  - 세력 5개
  - 성 60개와 인접 경로
  - 고유 등급 100명과 일반 등급 400명(현재 자동 검증용 대규모 로스터)
  - 특화 시설 8개
  - 시작 자원과 시작 배치
  - 핵심 인물 19명의 시작 배치와 나머지 481명의 탐색·등용 대기 상태
  - 턴 규칙
  - UI 문자열
  - 성 규모와 번영/기술/안정/방어 초기값
  - 전략 방어 전력 가중치: 성 방어 300%, 치안 50%, 주둔 인물 무력 60%
  - 영웅 통솔 능력치
  - 인물 등급과 특기
  - 인재별 등용 요구와 필요 명성
  - 연구 5개와 작위 4개
  - 일반 인물 영웅 승격 기준: 공적 80, 명성 30, 영향력 30과 전투 승리 성취
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
- `WIAdministrationTurnSystem.cs`: 내정 턴, 인물 활동, 의뢰와 부대 이동 계산
  - 연구 시작·완료·세력 효과, 작위, 영웅 승격, 태수 임명과 개별 인물 이동 계산
  - 실제 전쟁 접경 위협도와 ScriptableObject 설정 기반 AI 전선 활성화
- `WICampaignAutoPlayer.cs`: 내정형·균형형·공세형 캠페인 자동 진행과 재미 검증 지표 수집
- `WICampaignAutoTestLabWindow.cs`: 단일 조합 또는 정책 3종 × 난이도 3종 캠페인 완주와 결과 내보내기 에디터 도구
- `WICampaignAutoTestLab.uxml`, `WICampaignAutoTestLab.uss`: 자동 테스트 랩의 고정 입력·결과 9행 레이아웃과 스타일
- `WIFunValidationAutomationTests.cs`: 세 정책 24·60·120개월, AI 전선, 전투 목표와 첫 12개월 사건 밀도 회귀 검증
- `WICampaignAutoTestLabTests.cs`: 캠페인 종료 정지, UXML 고정 결과 행과 보고서 변환 검증
- `WICommonRosterSeeder.cs`: 일반 인물 원본 데이터를 중복 없이 갱신하는 Unity 편집기 메뉴
- `WIAdministrationUIController.cs`: UI와 전략 로직 연결
- `WIMapConnectionLayer.cs`: 인접 성 경로, 접경 전선과 선택 경로의 UI Toolkit 벡터 렌더링
- `Assets/Resources/UI/Generated/map_castle_*.png`: 5대 세력의 성채·깃발 지도 마커
- `Assets/Resources/UI/Generated/icon_flat_*.png`: 군사·인사·외교·계략·연구·통치·평정·월보·다음 턴 명령 아이콘
- `Assets/Resources/UI/Generated/hud_flat_*.png`: 날짜·금화·마나·영향력 HUD 아이콘
- `Assets/Resources/UI/Generated/button_flat_*.png`: 일반·선택·위험 9-Slice 버튼 배경
- `GameDocuments/ButtonTypeGuide.md`: 현행 버튼 이미지의 A~H Type 공식 별칭, 사용처와 업무 지시 기준
- `GameDocuments/UIReferences/ProjectWI_ButtonTypeCatalog.png`: 기존 A~G 버튼 에셋 7종을 모은 시각 카탈로그
- `Assets/Resources/UI/Generated/button_type_h.png`: 왼쪽 화살촉 장식과 은·황동 이중 테두리를 갖춘 H Type 대표 진행 버튼 배경
- `Assets/Resources/UI/Generated/right_panel_*.png`, `right_objective_card.png`, `right_notice_row.png`, `right_danger_row.png`, `right_action_button.png`: 전략·성 화면 우측 정보 패널과 하단 전역 명령(군사~월보)에 사용하는 D Type 9-Slice 에셋
- `Assets/Fonts/NotoSerifKR-VariableFont_wght.ttf`: 제목과 본문용 한글 명조 서체
- `Assets/Fonts/NotoSansKR-VariableFont_wght.ttf`: 숫자·작은 명패·버튼용 한글 고딕 서체
- `WIBattleRuntimeBuilder.cs`: 전투 세션 참가자를 역할별 초기 진형으로 변환
- `WIBattleSimulation.cs`: 표적 탐색, 이동, 공격과 승패 계산
- `WIBattleRuntimeController.cs`: 전투 씬 오브젝트와 런타임 연결
- `WIBattleCharacterView.cs`: 빈 Sprite 기반 캐릭터 표시와 상태 동기화
- `WIBattleHUDController.cs`: 전투 상태, 부대 명령과 영웅 스킬 버튼 연결
- `WIBattleSceneBootstrap.cs`: 영속 캠페인에서 전투 세션을 받아 결과를 반환
- `WICampaignRuntimeService.cs`: 씬 전환 사이 캠페인 상태와 대기 전투 ID 보존
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

- `GameDocuments/UIHandoffReport.md`: 전략 지도·성 내정 목표 시안, 현재 UXML/USS 구조, 재사용 에셋, 후속 수정 순서와 완료 기준
- `GameDocuments/UIConcepts/ProjectWI_Strategy_UI_Concept_V1.png`: 전략 지도 목표 시안
- `GameDocuments/UIConcepts/ProjectWI_Castle_Administration_UI_Concept_V1.png`: 성 내정 목표 시안
