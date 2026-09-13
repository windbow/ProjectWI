# ProjectWI 씬 및 에셋 구조

## 업무 특성 및 유지 지시 데이터

- `WITraitType.Administration`, `TalentRecruitment`, `Scholar`, `Espionage`는 각각 성 사업·영지관, 탐색·영입, 연구, 첩보 자격입니다. `WIHeroDefinition.traits`가 마스터 원본이며 `WICharacterRuntimeState.Traits`는 저장 호환을 위한 런타임 사본입니다. 캠페인 생성과 불러오기에서 마스터 특성과 동기화합니다.
- 현재 `WI_AdministrationDatabase.asset`의 1,200명은 영웅 200명·일반 1,000명으로 구성됩니다. 일반 내정 특성은 정확히 300명이며, 시작 배치 성마다 담당자를 우선 확보한 뒤 종족 비율과 정치·지력 순으로 결정론적으로 배분합니다.
- 영웅은 시작 영지관, 핵심 인물 8명, 정치 65 이상을 기준으로 163명이 내정 특성을 가집니다. 승격은 내정 특성을 자동 부여하지 않습니다.
- 전문 업무 특성은 능력치와 시작 성의 최소 담당자를 기준으로 결정론적으로 배분합니다. `[인재영입]`은 일반 180명·영웅 71명, `[학자]`는 일반 160명·영웅 75명, `[첩보]`는 일반 140명·영웅 61명입니다.
- `automation.talentOfficeCapacity`는 성별 선술집 인재실의 탐색·영입 담당자 상한이며 현재 값은 2입니다. 현재 활동과 유지 지시가 탐색 또는 영입인 인물을 같은 슬롯으로 계산합니다.
- `WICastleRuntimeState.StandingProject`, `RepeatProject`는 다음 달 사업 지시를 저장합니다. `WICharacterRuntimeState.StandingActivity`, `StandingActivityTargetHeroId`, `RepeatActivity`, `AutomaticRecovery`는 반복 활동과 자동 휴식을 저장합니다.
- `WIAdministrationAutomationDefinition`은 반복 활동의 피로 시작·종료선과 훈련 경험 종료선을 ScriptableObject에서 조정합니다.
- `PendingGovernorAppointment`는 새 점령지의 자동 영지관 임명을 보류하며, 실제 후보가 생긴 달에만 일반 내정 인물을 우선 임명합니다.
- 관련 고정 문자열은 `UI_ADMIN_TRAIT_REQUIRED`, `UI_CHARACTER_TRAITS`, `UI_ADMIN_MAINTENANCE`, `UI_ADMIN_APPOINTED`, `UI_ORDER_REPEAT_ON/OFF`, `UI_ORDER_HINT`, `UI_ACTIVITY_REPEAT_HINT`, `UI_ADMIN_COMBAT_*` UID를 사용합니다.

- `WICampaignAutoPlayer.RecordArmyStateDurations`는 매월 플레이어 전투단의 상태를 전투 대기→이동→재편 우선순위로 하나만 분류해 중복 없이 누적합니다. `MovingArmyMonths`, `AwaitingBattleArmyMonths`, `ReorganizingArmyMonths`는 총 부대·월이며 각 `Longest...ArmyMonths`는 단일 전투단의 최장 연속 체류입니다.
- 전투단 체류 지표는 `WICampaignAutoTestLabWindow` 상세와 CSV·Markdown 내보내기에 포함됩니다. 종료 시점 상태 필드와 달리 캠페인 전 기간의 병목을 찾기 위한 진단 데이터입니다.
- `WICampaignResultSystem.Evaluate`는 아레스 메인에서 `valdor` 보유 성이 없으면 승리로 판정하고, 프리 시나리오는 기존 전체 성 점령 판정을 사용합니다.
- `WICampaignAutoPlayer.EnsurePlayerArmies`는 아레스 메인의 보유 성 구간에 따라 전투단 상한을 2/3/4/5개로 확장합니다. `IsFinalValdorCampaign`은 24성 이상에서 발도르 접경 목표·전쟁 재개·영향력 비축·장기 교착 결전을 활성화합니다.
- `IsStrategicTargetReachable`은 목표에 인접한 아군 집결지까지 작전 가능한 전투단이 아군 영토 경로로 도달 가능한지 검사합니다. `CanAutoAttack`과 `GetRequiredAttackPower`의 선택적 비율 덮어쓰기는 최종 공세의 100% 전력선에만 사용합니다.
- `HasAlternativeStrategicTarget`과 `TryOpenAlternativeFront`도 `IsStrategicTargetReachable`을 공유합니다. `TryOpenAlternativeFront`의 동시 전쟁 수는 실제 도달 가능한 접경을 가진 상대만 포함합니다.
- `WIAutoCampaignMetrics`의 `FinalPlayerArmyCount`부터 `FinalAtWarWithValdor`까지의 종료 진단 필드는 전투단 구성, 피로, 유휴 보충 인원, 발도르 접경, 무원정, 목표 집결 전력과 외교 자원을 Lab 상세에 표시합니다.
- 종료 전투단 상태는 `FinalOperationalArmyCount`, `FinalMovingArmyCount`, `FinalAwaitingBattleArmyCount`, `FinalReorganizingArmyCount`로 분리해 장기 표본 종료 시점이 실제 교착인지 단순 이동·전투 처리 중인지 구분합니다.
- `WIAutoCampaignMetrics.FinalValdorCastleCount`와 `WICampaignAutoBatchStatistics.ValdorCastles`, `CompletionMonths`는 장기 캠페인의 발도르 잔여 영토와 실제 종료 시점을 Lab 상세 및 테스트에서 집계합니다.

- `WICampaignAutoTestLab.uxml`은 `seed-start-field`, `seed-count-field`를 고정 배치합니다. `WICampaignAutoTestResult.Samples`가 시드별 원시 지표를 보존하고 `WICampaignAutoBatchStatistics`가 핵심 지표의 평균·중앙값·최솟값·최댓값을 계산합니다.
- `WIAutoStrategicPlan.ConsecutiveNoAttackChecks`는 공격 검토일의 연속 교착 횟수를 저장합니다. `HasAlternativeStrategicTarget`은 기존 전쟁의 다른 접경을, `TryOpenAlternativeFront`는 중립·우호 접경의 두 번째 전쟁 후보를 찾습니다. `WIAutoCampaignMetrics.WarsDeclared`는 이 교착 해소 선전포고 횟수입니다.

- `WIAdministrationState.SimulationSeed`: 자동 시뮬레이션 표본을 구분하는 저장 가능 정수입니다. 전략 전투의 공격·수비 변동, 근접전 패착, 패전 인물 운명 해시에 포함되며 기본값 0은 기존 저장과 호환됩니다.
- `WIAdministrationTurnSystem.ResolveDefeatedGarrisonCharacterFate`: 수비 전투단이 없는 점령전에서 성 주둔 인물을 임시 수비 명단으로 구성해 기존 사망·포로·전향·후퇴 규칙에 연결합니다.
- `ResolveDefeatedCharacterFate`는 혼합 편성의 운명 대상 등급을 안정 해시로 선택합니다. 일반 후보가 있어도 20%는 영웅 후보군을 선택하며, 같은 시드·턴·전투단에서는 같은 결과를 재현합니다.

## 시설 기능 진입 구조

- `WIAdministrationBasicFacilityUGUIController`는 성관·시장·훈련소·선술집의 네 고정 버튼을 기존 영지관·성 기록·인재 활동·선술집 의뢰 UGUI에 연결합니다.
- `WIAdministrationTerritoryUGUIController.basicFacilityButtons`는 중앙 성 전경 위 네 시설 핫스팟을 소유합니다.
- `WIAdministrationTerritoryUGUIController.facilitySlotButtons`는 하단 특화 시설 슬롯 두 개를 선택 입력으로 사용합니다.
- `WIAdministrationSlotSnapshot.ContentId`는 특화 시설 슬롯에 표시된 시설 ID를 전달합니다.
- `WIAdministrationUIController.ExecuteUGUIFacilitySlot`은 보유 특화 시설을 연구·군사·의뢰·회복·계략·외교·성 기록 화면에 연결하고 빈 슬롯은 시설 선택 화면에 연결합니다.

- `Assets/Editor/WICastleMapPlacementWindow.cs`: `ProjectWI > Tools > Castle Map Placement` 메뉴에서 여는 성 좌표 전용 EditorWindow입니다. 월드맵 Sprite, `castles[].normalizedMapPosition`, 인접 성 ID를 읽어 편집용 미리보기를 만들며 저장 전에는 ScriptableObject나 프리팹을 변경하지 않습니다.
- 저장 시 마스터 성 좌표를 갱신하고 선택 옵션에 따라 기존 `campaignVariants[].castlePlacements[]` 중 `overrideMapPosition=true`인 항목만 동기화합니다. Undo 기록과 `AssetDatabase.SaveAssets`는 사용자가 `좌표 저장`을 누른 경우에만 수행합니다.

- 60개 성 좌표는 `WICastleDefinition.normalizedMapPosition`에 저장되며, 아레스 메인의 명시적 재배치 성은 `campaignVariants[0].castlePlacements`의 위치 덮어쓰기를 사용합니다. 자동 V2 재배치 좌표는 철회하고 Git 기준 기존 좌표로 복원한 상태입니다.
- `CampaignVariants_UseScenarioCastlePlacementData`는 아레스 메인의 위치 덮어쓰기 값을 ScriptableObject와 런타임 상태 사이에서 비교합니다. `CastleMapPositions_AreNormalizedAndDistinct`는 수동 마스터 배치의 정규화 범위와 완전 중복만 검사합니다.

- `Assets/Art/Maps/ProjectWI_GlobalMap_Concept_V2.png`: 월드 UI 컨셉 기반의 16:9 글로벌맵이며 `WI_AdministrationDatabase.globalMapImage`에 연결됩니다.

## 월드맵 성 이미지

- `WICastleDefinition.mapMarkerImage`: 월드맵 노드 전용 Sprite입니다.
- `WICastleDefinition.castleImage`: 기존 성 상세·영지 화면 전경 Sprite입니다.
- 기본 데이터는 산악형, 해안형, 설원형 성 마커를 60개 성에 각각 20개씩 분산 배정합니다.

- `WIAdministrationTurnSystem.PlanAIActions`는 위협 성을 우선하고, 없으면 전쟁 중인 적과 인접한 접경 성을 집결지로 사용합니다. `CanAIFactionAttack`은 해당 집결지의 작전 가능 동일 진영 전투단 전력을 합산해 데이터베이스의 공격 허용 비율과 비교합니다.
- `WICampaignAutoPlayer.Military`의 `RefreshStrategicAssemblyCastle`은 유지 중인 목표의 유효 집결지를 갱신하고, `FindNearestReinforcementCastle`은 손실 전투단이 일반 등급 대기 인물을 편입할 수 있는 가장 가까운 아군 성을 찾습니다. `FillArmy`는 지휘관 외 보충 인원을 일반 등급으로 제한하며 성 배치 인원 수를 전투단 최대 인원으로 오인하지 않습니다.
- `WICampaignAutoPlayer.Prisoners.cs`의 `ResolvePrisonerPolicy`는 플레이어 출신 포로 중 유효 영웅·잔여 억류 기간 순으로 한 명을 골라 `ExchangePrisoners` 또는 `RansomPrisoner`를 호출합니다. 일반 포로 몸값 비축선은 내정/균형/공세 순으로 금화 1000/500/250, 마나 300/150/75입니다.
- `WIAutoCampaignMetrics`의 `PrisonerExchanges`, `PrisonerRansoms`, `PrisonersRecovered`, `PrisonerRansomGoldSpent`, `PrisonerRansomManaSpent`는 포로 대응의 횟수·귀환·자원 지출을 분리해 저장합니다.
- 전투 물리 회귀 테스트는 `CreateBattleConfigWithHiddenGrid(false)`로 원본 `WI_BattleConfig.asset`의 복제본을 만들고 연속 좌표 모드만 격리해 검증합니다. 실제 게임의 기본 숨은 격자 설정은 유지됩니다.
- `WIAdministrationAITurnTests`의 전투 결과 픽스처는 `PrepareValdorAttackOnAvalon`에서 발도르 전투단과 2개월 원정을 명시적으로 구성합니다. AI 공격 대상 선택 검증과 전투 세션·결과 검증은 서로 독립된 테스트 계약입니다.

## 전투 캐릭터 표시 프리팹

- `WIBattleCharacter.prefab`은 접지 그림자, 집중·선택 마커, 체력 배경·전경, 인물 라벨을 고정 자식으로 보유합니다.
- `WIBattleCharacterView`는 해당 자식을 찾아 Sprite, 색상, 체력 비율과 표시 여부만 갱신하며 런타임에 UI 오브젝트나 컴포넌트를 생성하지 않습니다.
- 표시 요소 이름은 `GroundShadow`, `FocusTargetMarker`, `SelectionMarker`, `HealthBackground/HealthFill`, `CharacterLabel`로 유지합니다.

## UGUI 브리지 코드 구조

- `WIUILayoutFormattingTests`는 현재 UGUI 모달·턴 후속 에셋, 전투 씬의 실제 전역광 강도와 전투 전용 버튼 클래스, 현행 전략 용어를 검증합니다. 삭제된 `popup_header.png` 경로는 USS와 테스트 계약에서 제거했습니다.
- 월드 성 상세 패널은 제목 `CastleDetailRow1~6`과 값 `CastleDetailValue1~6`을 분리해 프리팹에 고정 보유하며, `WIAdministrationWorldUGUIController`의 두 TMP 배열이 같은 인덱스로 갱신합니다.
- `turn_followup_info_panel_v1.png`은 1955×509, 사방 48px Border의 Sliced Sprite이며 턴 후속 설명 패널·체크리스트 패널·두 체크 행이 공용으로 사용합니다.
- `WIAdministrationTopHUDUGUI.prefab`: 월드 기준의 92px 상단 HUD를 저장한 공용 중첩 프리팹이며 월드·영지 화면이 동일 자산을 사용합니다.
- `WIAdministrationTopHUDUGUIController.cs`: 공용 HUD의 진영·날짜·자원 TMP 갱신과 월보·의회·연구·설정 버튼 연결을 담당합니다.
- 기능 화면 컨트롤러의 제어문은 모두 명시적 중괄호 블록을 사용합니다. 이벤트 연결, 조기 반환과 카드 반복 처리도 같은 형식을 유지합니다.
- `WIAdministrationUGUIPanelController.cs`: 24개 기능 화면 컨트롤러의 공통 기반 클래스입니다. 프리팹의 기존 `administrationController` 직렬화 이름을 유지하고 참조 누락 시 씬 탐색을 한 곳에서 처리합니다.
- `WIAdministrationUIController.UGUIBridge.cs`: 월드·영지 스냅샷, 화면 표시 상태와 화면 전환 이벤트를 담당합니다.
- `WIAdministrationUIController.UGUICastleCommandBridge.cs`: 중점 사업, 영웅 배치와 인물 활동을 담당합니다.
- `WIAdministrationUIController.UGUIFacilityBridge.cs`: 특화 시설, 선술집과 영지관 위임을 담당합니다.
- `WIAdministrationUIController.UGUIMarchBridge.cs`: 전투단 선택·편성·이동·원정을 담당합니다.
- `WIAdministrationUIController.UGUIReportBridge.cs`: 성 기록, 목표, 월간 보고, 단축 명령과 QA 진입을 담당합니다.
- `WIAdministrationUIController.UGUIMilitaryTransferBridge.cs`: 군사 현황과 인물 이동을 담당합니다.
- 모든 파일은 같은 partial 컨트롤러의 기존 공개 API와 이벤트를 유지하며 프리팹은 이 코드 분리로 변경되지 않습니다.

## 일반 인물 사망과 재야 복귀

- 태생 영웅 등급 인물은 사망·처형 시 `IsDead` 상태로 영구 퇴장합니다.
- 일반 출신 인물은 영웅 승격 여부와 무관하게 사망 후 `CommonReturnMonthsRemaining` 6개월을 거쳐 살아남은 세력의 성 하나에 일반 재야 인재로 다시 배치됩니다.
- 승격 일반 인물은 재야 복귀 대기 진입 시 `PromotedToHero`, 승격 성취와 작위를 초기화합니다.
- 복귀할 때 `Recruited`, `Discovered`, 영입 진척을 초기화하고 `RecruitmentCastleId`로 새 출현 지역을 기록합니다.
- 성·전투단·포로·전향·영입 상태 중 하나라도 남아 있으면 중복 출현하지 않습니다.
- `CommonReturnCount`는 시뮬레이션에서 일반 인물 재순환 횟수를 집계합니다.

## 월드 지도 프리팹과 시나리오 배치

- 월드 지도 UI 자산은 `WIAdministrationWorldUGUI.prefab` 하나입니다.
- `WICampaignVariantDefinition.castlePlacements`가 선택 시나리오의 성 소유 세력·정규화 좌표·인접 성을 덮어씁니다.
- 런타임의 `WICastleRuntimeState`에 확정된 배치를 저장하고, `WIAdministrationWorldSnapshot.MapNodes`와 `MapConnections`를 통해 공용 프리팹에 표시합니다.
- 프리 시나리오처럼 덮어쓰기가 없는 항목은 `WICastleDefinition`의 기본 소유 세력·좌표·연결을 유지합니다.
- 아레스 메인의 `aiPreservationFactionId=valdor`, `valdorAIPreservationCastleCount=36`은 제3세력 AI의 발도르 침식만 제한하는 시나리오 데이터입니다. 아발론의 정복 진행과 발도르 자체 경제·방어 수치에는 보너스를 주지 않습니다.
- `WIAdministrationTurnSystem.Pipeline.cs`는 월간 처리를 준비, 성별 처리, 후속 시스템, 턴 완료의 네 단계로 조율합니다. 개별 경제·인물·군사·AI 계산은 `WIAdministrationTurnSystem`의 도메인 함수가 담당합니다.
- `WICampaignAutoPlayer.cs`는 자동 캠페인 월간 실행, 내정 방침·영입 준비와 결과 지표 집계를 담당합니다. `WICampaignAutoPlayer.Decisions.cs`는 사업·관계·지역·점령·영입·유산 선택 해결, 정책별 선택 점수와 미결 선택 집계를 담당합니다.
- `WICampaignAutoPlayer.Military.cs`는 자동 플레이어의 전투단 생성·훈련·집결, 아군 영토 경로 탐색, 공격 가능 판정과 플레이어 참가 대기 전투 자동 해결을 담당합니다. 실제 군사 상태 변경은 `WIAdministrationTurnSystem` 공개 API에 위임합니다.
- 균형·공세 자동 정책은 최대 2개 전투단을 운용하며 같은 출발 성의 전력을 합산합니다. 첫 확장에는 목표 방어력 대비 80% 기준을 사용하고, 영토 확장 뒤에는 균형 125%·공세 115% 기준을 사용합니다. 정예 또는 평균 피로 50 초과 전투단에는 합동훈련을 예약하지 않습니다.
- `WIAdministrationTurnSystem.Diplomacy.cs`는 플레이어 외교 명령 API인 관계 개선, 협정, 전쟁, 원조, 포로 협상과 공동 공격 판정 및 외교 대기시간·월간 AI 관계 개선을 담당합니다. 기존 정적 공개 API를 유지하는 partial이며 상태 데이터는 복제하지 않습니다.
- `WIAdministrationTurnSystem.Military.cs`는 전략 계층의 전투단 생성·편성·해산, 이동·훈련, 보급과 행군 피로를 담당합니다.
- `WIAdministrationTurnSystem.Military.Sessions.cs`는 전략 전투 판정, 단일·공동 공격 전투 세션 생성, 실시간 전투 전환·결과 제출과 전투단 전투력·성 방어력 계산을 담당합니다. 아레스 메인의 동일 집결지·동일 전략 목표 공격군은 `AttackerArmyIds`로 합산하고 기존 `AttackerArmyId`는 주 공격군 및 구버전 저장 호환 필드로 유지합니다. `WIBattleSimulation`의 프레임 단위 실시간 전투 계산과는 분리됩니다.
- `WIAdministrationTurnSystem.Military.Results.cs`는 전략 전투 결과에 따른 패배 후퇴·재편성, 전투 관계 변화와 성 점령·점령지 안정을 담당합니다.
- `WIAdministrationTurnSystem.Military.Characters.cs`는 패배 인물의 사망·포로·전향·후퇴 판정, 등급별 사망 처리와 커먼 인물 재야 귀환, 포로 기간 만료와 원소속 귀환을 담당합니다.
- `WIAdministrationTurnSystem.Characters.Tavern.cs`는 선술집 의뢰 완료·보충, 의뢰 표시명·적성 능력치 계산과 의뢰별 성 상태·인재 발견·방첩 효과를 담당합니다.
- `WIAdministrationTurnSystem.Characters.Recruitment.cs`는 미발견 인물 탐색, 인물 교류 관계 개선, 영입 설득 진척·요구 사건 생성, 선택 조건·결과 적용과 영입 성 배치를 담당합니다.
- `WIAdministrationTurnSystem.Characters.cs`는 월간 인물 활동 조율, 커먼 인물 승격, 성 간 이동·도착, 영지관 배치·내정 가능 자격과 영입·연구·작위에서 공유하는 진영 소속 판정을 담당합니다. 인물 정의와 런타임 상태는 기존 데이터베이스 및 캠페인 상태를 그대로 사용합니다.
- `WIAdministrationTurnSystem.Events.cs`는 관계 사건 후보 생성·선택 결과·중복 방지와 사업 선택 사건의 성 수치·인물 평판·위험 결과 적용을 담당합니다. 사건 정의와 대기·완료 목록을 별도로 복제하지 않습니다.
- `WIAdministrationTurnSystem.Research.cs`는 AI 연구 후보 선택, 플레이어와 AI가 공유하는 연구 시작 조건, 월간 진행·완료와 완료 연구의 진영 수입 효과를 담당합니다. 연구 데이터와 완료 상태를 별도로 복제하지 않습니다.
- `WIAdministrationTurnSystem.AI.Projects.cs`는 AI 성향 기반 사업 배치, 사업 담당자 선택, 전략별 사업 종류 선택을 담당합니다.
- `WIAdministrationTurnSystem.AI.Military.cs`는 장기 교착 해소용 전쟁 압박, 월간 전투단 생성·편성·집결·공격, 공동 공격 수행, 성 위협도와 공격 목표 및 시나리오 보존선·예상 전력 기반 원정 가능 여부 평가를 담당합니다.
- `WIAdministrationTurnSystem.AI.Navigation.cs`는 아군 영토 최단 경로 탐색, 플레이어 성 침공 경고 갱신, 플레이어 인접 성 판정을 담당합니다.
- `WIAdministrationTurnSystem.AI.Recruitment.cs`는 프리 시나리오의 비플레이어 진영 방랑 인물 탐색, 영입 비용 지불, 영입 성 배치와 소식 생성을 담당합니다.
- `WIAdministrationTurnSystem.AI.cs`는 공통 판단 보고·난이도 후보 선택과 사업·군사에서 공유하는 AI 성향 설명을 담당합니다. 전투·외교 결과 데이터는 해당 도메인 partial에 위임합니다.
- `WIAdministrationTurnSystem.Projects.cs`는 성 사업의 월간 해결, 영지관 위임 계획, 진영 정책·성 특산 보너스와 투자 단계별 비용·기간·예상 성과 계산을 담당합니다.
- `WIAdministrationTurnSystem.Projects.Rewards.cs`는 담당 인물 특기 고유 효과, 영웅 유산 보너스·후보·설치, 사업 선택 사건 제목과 작위 수여·사업 보너스를 담당합니다. 성에서 발생하는 월간 자원 수입은 별도 경제 책임으로 분리합니다.
- `WIAdministrationTurnSystem.Economy.cs`는 진영 소유 성의 월간 수입 합계와 성 규모·질서·자원 특산·특수 시설에 따른 금화·마나·영향력 계산을 담당합니다. 완료 연구 효과는 `Research` partial에서 계산해 성별 수입에 적용합니다.
- `WIAdministrationTurnSystem.Factions.cs`는 영토가 사라진 진영을 한 번만 멸망 처리하고 진행 연구, 소속 전투단과 전투 세션, 첩보 임무, 포로·인물 상태, 공동 공격·원조 대기 상태를 정리하며 진영별 멸망 서사를 월보에 기록합니다.
- `WIAdministrationTurnSystem`은 본체를 포함한 기능별 소스 파일 21개로 구성되며, 모든 조건·반복문의 실행문은 중괄호 블록으로 유지합니다.
- `WIAdministrationDatabaseSO`의 `AggressiveAIAttackPowerPercent=90`, `StandardAIAttackPowerPercent=105`는 AI 성향별 원정 최소 전력 비율입니다.
- 아레스 메인의 현재 북부 진출로는 `castle_28 ↔ castle_04`, `castle_04 ↔ castle_33`, `castle_04 ↔ castle_34`입니다. 연결은 양쪽 성의 `AdjacentCastleIds`에 서로를 기록합니다.

## 성 내정 공용 모달 셸

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

- `Assets/Art/Characters/Ares/Ares_Battle_1WU_A_OutlineBake_V1.png`: 현재 A 근거리 줌 기준 전투용 아레스 Sprite입니다. 외곽 실루엣에 `#10141A` 3단계 그라데이션 선을 직접 베이크했으며 최종 190×256px, 256 PPU와 Scale 1에서 외곽선 포함 높이가 정확히 1월드 유닛입니다. Bilinear, Mipmap 활성, 무압축으로 임포트하며 현재 1200명 밀도 테스트의 공용 Sprite입니다.

## 1유닛 캐릭터 축척 기준 완성형 전장 V6

- `Assets/Art/Battle/Effects/WI_CharacterShadow_Oval_V1.png`: 모든 전투 캐릭터에 재사용하는 256×128 RGBA 접지 그림자 Sprite입니다. 중심 최대 알파는 약 51%이고 RGB는 중립 흑회색이며 256 PPU 기준 기본 월드 폭은 1유닛입니다. `WIBattleCharacter.prefab/GroundShadow`에 연결되어 본체보다 Sorting Order 1 낮게 표시됩니다.
- `WI_CharacterShadow_Oval_V1.png`은 전용 캐릭터 Sprite가 없을 때의 대체 표시와 캐릭터 프리팹의 선택·집중 마커, 체력 바에도 실제 에셋 참조로 연결됩니다. `WIBattleCharacterView`는 런타임에 Texture나 Sprite를 만들지 않고 색상과 표시 상태만 변경합니다.
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
- `WIAdministrationUIController.UGUIBridge`는 캠페인 타이틀 표시 상태를 월드·영지 스냅샷의 공통 가시성 조건으로 사용합니다. 최초 값은 표시 상태이며 새 캠페인 또는 이어하기 성공 시 해제되고, `ShowCampaignStart()`로 타이틀에 복귀하면 다시 활성화됩니다.
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
- 월드 프리팹의 `commandButtons`와 `commandActions`는 하단 전역 명령 7개를 일대일로 보관합니다. 상단 월보·의회·연구·설정 입력은 공용 `WIAdministrationTopHUDUGUIController`가 소유하며 월드 명령 배열에 포함하지 않습니다.
- `WIAdministrationEndTurnUGUIController`는 공용 다음 턴 프리팹의 클릭 이벤트, 활성 상태와 표시 문구를 담당합니다. 월드·영지 컨트롤러는 각각의 스냅샷에서 `CanEndTurn`과 `EndTurnText`를 전달하며 미결 플레이어 전투가 있으면 두 화면에서 동일하게 버튼을 비활성화합니다.
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
- 성 내정 모달의 `bg_type_d` 본문 패널, `button_flat_normal` 헤더·일반 버튼, `button_flat_primary` 주요 버튼, `turn_followup_close_button_v1` 닫기 버튼과 상태 색은 각 완성 프리팹에 직접 저장합니다.
- 공통 모달 스타일은 각 완성 프리팹에 직접 저장합니다. 고정 헤더·버튼·프레임 디자인은 런타임 데이터 갱신 대상이 아닙니다.

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
- `AdministrationBalancePlan.md`: 영웅 개인 전투력과 아이템을 제외한 내정·전쟁 밸런스 목표, 측정 지표와 단계별 조정 계획
- `WICampaignAutoTestLabWindow.cs`: 단일 조합 또는 정책 3종 × 난이도 3종 캠페인 완주와 결과 내보내기 에디터 도구
- `WICampaignAutoTestLab.uxml`, `WICampaignAutoTestLab.uss`: 자동 테스트 랩의 고정 입력·결과 9행 레이아웃과 스타일
- `WIFunValidationAutomationTests.cs`: 세 정책 24·60·120개월, AI 전선, 전투 목표와 첫 12개월 사건 밀도 회귀 검증
- `WICampaignAutoTestLabTests.cs`: 캠페인 종료 정지, UXML 고정 결과 행과 보고서 변환 검증
- 일반 인물 데이터는 현재 `WIMassCharacterSeeder`의 1200명 결정론적 재구성과 Character Data Viewer의 직접 편집 경로로 관리합니다. 과거 소규모 `WICommonRosterSeeder`는 제거했습니다.
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
- `Assets/Resources/UI/Generated/bg_type_g.png`: 567×292 가로형 공통 정보 패널 배경. 중립적인 냉색 흑청 내부 면과 얇은 저채도 회갈색 금속 프레임으로 구성하며 문구·아이콘·구분선은 포함하지 않음. Sprite/Single과 상하좌우 10px 9-Slice를 사용함
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
  - Main Camera의 `WIBattleCameraController`를 `BattleRuntime`에 직접 연결
  - `BattleRuntime`에 전투 설정, 캐릭터·효과·투사체 프리팹과 용도별 고정 루트 연결
  - `CharacterRoot` 아래에 세션 참가 캐릭터 프리팹을 런타임 생성
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
- 인재 활동 화면은 기존 `WICharacterRuntimeState.Activity`, `ActivityTargetHeroId`, 피로·부상·명성·발견 상태를 `WIAdministrationCharacterActivitySnapshot`으로 변환합니다. 후보 스냅샷에는 직업 표시명과 영웅·일반 등급이 포함되며, 컨트롤러가 이름 검색·등급 필터·추천순/이름순 정렬 후 4열×2행 고정 카드에 페이지 단위로 표시합니다. `CharacterActivity` 전용 버튼·검색·필터는 소형 9-Slice Sprite를 사용하고, 카드 배경은 내부 구획선 없이 초상과 TMP 정보를 함께 받는 360×270 비변형 단일 Sprite를 사용합니다. 탐색·교류·영입·훈련·휴식 결과 계산은 기존 `WIAdministrationTurnSystem`의 월말 처리를 그대로 사용합니다.
- `WICharacterSelectionCardPrefabUtility`는 캐릭터 버튼 배열을 가진 행정 UGUI 프리팹 7종에 공용 카드 배경, 우측 정보 구분선과 이전 버튼 배경을 에디터에서 저장합니다. 카드 배경과 이전 버튼은 각각 12px·20px 9-Slice이며, `CharacterInfoDivider`는 각 버튼 아래에 고정된 `raycastTarget=false` Image입니다. 런타임 컨트롤러는 기존 초상·TMP 정보와 클릭 동작만 갱신합니다.
- 특화 시설 선택 화면은 `WIAdministrationDatabaseSO.SpecialFacilities`, 성의 `SpecialFacilityIds`, `PendingSpecialFacilityChoice`를 `WIAdministrationSpecialFacilitySnapshot`으로 변환하며 UI 전용 시설 데이터는 추가하지 않습니다.
- 특화 시설의 실제 효과 계산은 `WIAdministrationTurnSystem.Facilities.cs`의 시설 보유·사업·월간 패시브 공통 함수와 경제·연구·영입·외교·첩보 시스템 연결부에서 처리합니다. 저장 데이터는 기존 `WICastleRuntimeState.SpecialFacilityIds`만 사용하므로 추가 마이그레이션 필드가 없습니다.
- 시설별 수치 효과는 대공방 기술·방어 사업 +3, 마법 탑 규모당 마나 +10·연구 기간 -1개월, 대시장 규모당 금화 +15, 기사단 훈련·방어 사업 +3·훈련 경험치 +5, 모험가 길드 모집 사업 +3·탐색 명성 +2·영입 진척 +10, 성소 회복·질서 사업 +3·피로 -15·부상 -1개월·질서 +1, 첩보원 거점 계략 성공 +10%·발각 -10%·방어 발각 +10%, 사절관 외교 영향력 비용 -5입니다.
- 기본 시설 화면의 선술집 의뢰는 성의 `TavernQuests`와 `WITavernQuestDefinition`을 `WIAdministrationBasicFacilitySnapshot`으로 변환합니다. 담당자 적성은 기존 `WIAdministrationTurnSystem.GetQuestAptitude`로 계산합니다.
- 영지관 위임 화면은 성의 `GovernorHeroId`, `GovernorPolicy`, `GovernorMonthlyBudget`, `DelegatedToGovernor`를 `WIAdministrationDelegationSnapshot`으로 변환합니다. 예상 사업은 기존 `WIAdministrationTurnSystem.GetDelegationPreview` 결과를 표시합니다.
- 원정 화면은 기존 `WIArmyState`, 성 인접 경로와 진영 관계를 `WIAdministrationMarchSnapshot`으로 변환합니다. 새 전투단은 `CreateArmy`, 이동·원정은 `BeginArmyMarch`를 호출하며 별도 전투단 데이터를 만들지 않습니다.
- 성 상세 기록 화면은 선택된 성 정의와 런타임 상태를 `WIAdministrationCastleRecordSnapshot`으로 변환합니다. 상세 공개 여부는 기존 `WIInformationVisibility` 판정을 그대로 사용합니다.
- 캠페인 목표 상세 화면은 `WICampaignObjectiveSystem.GetCurrent`와 `GetProgress` 결과를 `WIAdministrationObjectiveSnapshot`으로 변환하며 목표 마스터 데이터를 복제하지 않습니다.
- 월간 보고 화면은 `LastMonthlyReport`, 미결 선택 사건 컬렉션과 `BattleSessions`를 구조화된 `WIAdministrationMonthlyReportSnapshot`으로 변환합니다. 스냅샷은 연월 제목, 영지·인물·전투단·질서·연구 요약, 자원 증감, 운영 결과, 주요 소식과 분류·설명·긴급도를 가진 중요 결정 목록을 제공합니다. `WIAdministrationMonthlyReportUGUIController`는 고정 프리팹 카드 3개를 페이지와 전체·영지·군사·인물 필터로 갱신하며, 사건 선택은 기존 모달을 유지하고 전투는 `WICampaignRuntimeService.StartBattle`에 연결합니다.
- 군사 화면은 `BattleSessions`, 플레이어 소유 `WIArmyState`, 성 인접 경로와 대기 영웅을 단계별 `WIAdministrationMilitarySnapshot`으로 변환합니다. 편성·단원 관리·훈련·해산·원정은 기존 `WIAdministrationTurnSystem`을 호출하며 UI 전용 군사 데이터는 복제하지 않습니다.
- 영웅 전역 화면은 `WICharacterRuntimeState`와 `WIHeroDefinition`, `WITitleDefinition`을 `WIAdministrationHeroesSnapshot` 카드로 변환합니다. 승격과 작위 수여는 기존 `PromoteCommonCharacter`, `AwardTitle`을 호출하며 영웅·작위 데이터를 중복 생성하지 않습니다.
- MainScene의 `UGUI Screen Bootstrap`에 있는 유일한 `WIUIScreenManager` 컴포넌트는 UGUI 프리팹 에셋 참조만 보관합니다. 각 화면은 `Assets/Prefabs/Administration`의 독립 프리팹이며, 같은 컴포넌트를 가진 이전 이름 또는 중복 루트가 발견되면 `WIAdministrationUGUISceneUtility`가 참조를 정식 관리자에 병합하고 중복 루트를 제거합니다.
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
- 인물 이동 대상 화면은 `WIAdministrationTurnSystem.GetCharacterTransferPath`로 도달 가능한 모든 같은 진영 성을 찾고, 목적지 `HeroIds`, `CharacterTransfers` 예약 수와 경로 개월을 `WIAdministrationCharacterActivitySnapshot` 카드로 변환합니다. 확정 시 `StartCharacterTransfer`를 호출하며 UI 전용 이동 상태를 만들지 않습니다.
- `WICharacterTransferState`는 `OriginCastleId`, 최종 `TargetCastleId`, `CurrentCastleId`, 출발 성을 제외한 `RouteCastleIds`, 다음 경유지를 가리키는 `RouteIndex`, `RemainingMonths`를 저장합니다. 이전 저장에서 신규 경로 필드가 비어 있으면 현재/출발 성에서 목적지까지 경로를 다시 계산합니다.
- 공통 `ShowMessage`는 `WIAdministrationDatabaseSO.GetText`로 UID를 번역한 뒤 `WIAdministrationTurnFollowupMode.Message`와 제목·본문 문자열로 변환합니다. 기존 호출부의 오류·성공 결과 데이터는 변경하지 않고 UI Toolkit 모달 생성만 제거했습니다.
- 전투 HUD 이미지 연결은 `WIBattleHUD.uss`의 `battle-top-status-frame`, `battle-character-info-frame`, `battle-command-frame`, `battle-skill-frame` 클래스에 고정되어 있습니다. `battle-status`, `selection-info`, `command-feedback`, `skill-buttons` 및 네 명령 버튼의 이름은 런타임 컨트롤러 계약이므로 유지합니다.
- `WIBattleConfigSO.battleSpriteScale`: 전투용 캐릭터 Sprite의 공통 Transform 배율입니다. 현재 `1`이며 전투 이미지가 없는 플레이스홀더에는 적용되지 않습니다. 실제 캐릭터 크기는 전투 Sprite의 PPU 규격으로 관리합니다.
- `WIBattleSimulation.cs`: 프레임 전투 진행, 승패 목표와 일반 공격을 담당합니다. `WIBattleSimulation.Movement.cs`는 근접 밀치기, 연속 좌표 충돌, 숨은 격자 이동·점유 셀·근접 접근 위치와 전장 경계를 담당합니다. `WIBattleSimulation.Projectiles.cs`는 원거리 역할 판정, 발사체 생성·추적·수명·선분 충돌과 선택적 아군 오발을 담당합니다. `WIBattleSimulation.Skills.cs`는 영웅 스킬 조건 검사, 마나·재사용 대기시간, 범위 피해·회복·지휘 효과와 스킬 시각 효과 상태 생성을 담당합니다.
- `WIBattleConfigSO.arenaBackgroundSize`: 배경 Sprite 전용 월드 표시 크기입니다. 현재 `57.024×32.076`이며 `arenaSize` 18×10의 이동·진형·충돌 판정에는 영향을 주지 않습니다.
- `WIBattleConfigSO.cameraMinimumZoom`, `cameraMiddleZoom`, `cameraMaximumZoom`: A/B/C 고정 줌의 직교 크기이며 현재 각각 `6`, `8`, `10`입니다.
- `WI_BattleConfig.arenaBackground`: 현재 `Battle_FortressField_V4_4K` Sprite를 참조합니다. V1~V3는 비교와 복구용으로 유지합니다.
- `WI_BattleConfig.arenaBackground`: 현재 `Battle_FortressField_V5_4K` Sprite를 폴백 배경으로 참조합니다. V1~V4는 `Assets/TrashAsset/Art/Battle`로 이동했습니다.
- `WI_BattleConfig.arenaPrefab`: 현재 완성형 청크 전장 `WIBattleCharacterScaleArenaV6.prefab`을 참조합니다. 런타임은 이 프리팹만 사용하며 참조가 없으면 임시 전장이나 Sprite 폴백을 생성하지 않고 설정 오류를 기록합니다.
- `WIBattleVisualEffect.prefab`, `WIBattleProjectile.prefab`: 시뮬레이션 효과와 실제 충돌 투사체가 공용으로 복제하는 SpriteRenderer 프리팹입니다. 런타임에서 새 GameObject나 SpriteRenderer를 조립하지 않습니다.
- `WIBattleSpriteRendererPool`: 효과·투사체 프리팹 인스턴스를 용도별 루트 아래에 보관합니다. 비활성 인스턴스를 우선 재사용하고 동시 표시량이 기존 최대치를 넘을 때만 프리팹을 추가 생성합니다.
- `BattleScene/BattleRuntime.visualEffectPrefab`, `projectilePrefab`: 각각 `WIBattleVisualEffect.prefab`, `WIBattleProjectile.prefab`의 루트 `GameObject`를 참조합니다. 풀이 소실된 Unity 오브젝트 참조를 받으면 `IsValid`는 예외 대신 `false`를 반환합니다.
- `WIBattleCameraController.GetClampedPosition`: 카메라 이동 가능 범위 계산에 `ArenaBackgroundSize`를 사용합니다. 전투 판정 경계인 `ArenaSize`와 시각적 대형 맵 탐색 범위를 분리합니다.
- `Ares_Battle_FullBody_V1`: 보존하는 고해상도 원본입니다. 전투에서는 이 파일을 직접 사용하지 않고 여기서 파생한 `Ares_Battle_1WU_A_OutlineBake_V1`을 사용합니다.
- `Ares_Battle_1WU_A_OutlineBake_V1`: 바깥 실루엣 외곽선을 포함해 190×256px로 정규화한 현재 전투용 Sprite입니다. Transform Scale 1, 256 PPU에서 약 0.742×1월드 단위로 표시하며 Mipmap 활성, Bilinear·무압축·Alpha Is Transparency를 사용합니다.
- `WIHeroDefinition.battleSprite`: 현재 30명 이상 전투 배치 R&D를 위해 전체 1200명이 `Ares_Battle_1WU_A_OutlineBake_V1`을 임시 공유합니다. 이는 개별 캐릭터 이미지가 준비되기 전의 테스트 데이터입니다.
- `WIBattleTestLabWindow.StartThirtyVsThirtyBattleDensityTest`: 아레스와 커먼급 29명을 아군에, 별도 영웅과 중복 없는 커먼급 29명을 적군에 배정해 기존 테스트 세션 경로를 실행합니다. 검증 캡처는 `Assets/Screenshots/Battle_30v30_Density_FirstAttempt.png`와 `Battle_30v30_Density_ZoomOut_NoHUD.png`입니다.
- `Battle_Ground_NeutralDay_V2_4K`: `GroundExperiment_V2`에 보존된 4096×4096 실험용 중립 지면 Sprite입니다. 확대 상태의 지면 세부 검증을 위한 단일 화면 후보이며 현재 `WI_BattleConfig`와 전장 Prefab에서는 참조하지 않습니다. 반복 경계가 검증되지 않았으므로 타일 데이터로 취급하지 않습니다.
- `button_normal`: 공통 일반 UGUI 버튼 Sprite입니다. Single Sprite Border는 `{left: 28, bottom: 28, right: 28, top: 28}`이며 연결 Image는 Sliced를 사용합니다.
- `button_primary`: 공통 주요 UGUI 버튼 Sprite입니다. Border는 `{left: 28, bottom: 24, right: 28, top: 24}`이며 연결 Image는 Sliced를 사용합니다.
- `WIAdministrationWorldUGUI.prefab`: 전략 화면의 시안 기반 5영역 레이아웃과 60개 성 버튼을 저장한 고정 UGUI 프리팹입니다. 디자인은 Prefab Mode에서 직접 편집하며 런타임에는 새 UI를 동적으로 만들지 않습니다.
- `strategy_avalon_crest_v1`: `Assets/Resources/UI/Generated`에 저장된 아발론 문장 Sprite입니다. 상단 진영 및 선택 성 패널에서 재사용하며 투명 배경, 중립 청회색 금속 색조를 사용합니다.
- `WIAdministrationWorldSnapshot.CastleHeroCards`: 선택 성의 주둔 영웅 중 최대 네 명의 표시 이름, 경험 기반 레벨 문자열, 초상 Sprite를 전달하는 읽기 전용 목록입니다.
- `WIAdministrationUIController.UGUIWorldSnapshot.cs`: 월드 HUD·선택 성 상세·영웅 카드·지도 노드·연결선 데이터를 `WIAdministrationWorldSnapshot`으로 조립합니다. 화면 표시 상태와 전환 이벤트는 `WIAdministrationUIController.UGUIBridge.cs`가 담당합니다.
- `WIAdministrationWorldSnapshot.MapConnections`: 인접 성 두 곳의 정규화 좌표, 표시색, 적대 전선 여부와 선택 경로 여부를 전달합니다. `WIAdministrationMapConnectionGraphic`이 이 목록을 하나의 UGUI 메시로 렌더링합니다.
- `WIAdministrationWorldSnapshot.CastleDetailTitles`, `CastleDetailValues`: 좌측 선택 성 패널의 영지관, 번영, 기술, 질서, 방어, 주둔 전투단 제목과 값을 같은 인덱스의 두 읽기 전용 목록으로 전달합니다. `CastleDetailRow1~6`은 제목을 왼쪽 정렬하고 `CastleDetailValue1~6`은 값을 오른쪽 정렬하며, 원본 값은 `WICastleRuntimeState`와 현재 성에 위치한 `WIArmyState`에서 가져옵니다.
- `strategy_side_panel_v1`: 좌우 전략 정보 패널 공용 프레임입니다. 하단 일반 명령은 `strategy_command_button_v2`, 턴 진행은 `strategy_next_turn_button_v3`, 좌측 영웅 초상 슬롯은 `strategy_hero_card_v1`을 사용합니다.
- `strategy_top_settings_v1`: 전략 상단 우측 설정 메뉴의 512px 투명 톱니 Sprite입니다. `WIAdministrationWorldUGUI.prefab/WorldContent/TopHUD/TopIcon4`에서 표시하고 같은 영역의 `TopSystemButton`이 `OpenUGUISystem`을 호출합니다.
- `WIAdministrationWorldUGUI.prefab/WorldContent/CommandBar`: 112px 높이의 전략 하단 명령 영역입니다. 군사·인사·외교·계략·연구·평정·월보 7개 버튼과 `strategy_next_turn_button_v3` 기반 다음 턴 버튼으로 구성되며 설정은 상단 톱니 버튼에서 엽니다.
- `strategy_command_button_v2`: 시안 기반의 무문자 흑청색 일반 명령 프레임입니다. 512px 후처리 원본과 `{left:34,bottom:24,right:34,top:24}` Border를 사용합니다.
- `strategy_next_turn_button_v2`: 시안 기반의 무문자 남청색 다음 턴 프레임입니다. 양끝 금속 창날 장식을 보존하도록 `{left:92,bottom:24,right:92,top:24}` Border를 사용합니다.
- `strategy_next_turn_button_v3`: 새 하단 기준 시안의 비대칭 다음 턴 프레임입니다. 좌측 이중 화살촉과 우측 절삭 모서리를 가지며 `{left:88,bottom:24,right:42,top:24}` Border를 사용합니다.
# 전투 인물 운명 데이터

- `WICampaignDifficultyDefinition`: 난이도별 사망·포로·적 합류 확률을 보유하며 나머지는 후퇴 확률로 사용합니다.
- `WIHeroDefinition.BattleTraits`: `Survivor`, `Elusive`, `Unyielding`으로 전투 운명 가중치를 보정합니다. 내정 사업 특기 목록과 분리되어 기존 성과 보너스에 영향을 주지 않습니다.
- `WICharacterRuntimeState`: 포로 교환 요청 자원·수량과 적 합류 진영을 저장하여 세이브 데이터에서 유지합니다.

# 시나리오 런타임 지도

- `WICastleRuntimeState.NormalizedMapPosition`, `AdjacentCastleIds`: 선택한 시나리오가 적용된 실제 지도 좌표와 연결 정보입니다.
- 이동, 출정, AI 목표 선택, 퇴각, 지도 노드와 연결선은 마스터 성 정의가 아니라 런타임 지도 정보를 사용합니다.
- 구버전 저장 파일에 런타임 지도 필드가 없으면 불러올 때 마스터 배치와 선택 시나리오의 좌표·연결 재정의를 복구합니다.
- `WICharacterRuntimeState.RecruitmentCastleId`: 아직 영입되지 않은 인재가 어느 성의 탐색 풀에 속하는지 저장합니다.
- 영입 성공 시 후보 인물은 영입 담당자가 머무는 성의 `HeroIds`에 추가되어 실제 배치 가능한 인력이 됩니다.
# 시나리오 인물 데이터

- `WICampaignVariantDefinition.nonPlayerRecruitmentEnabled`: 해당 시나리오에서 비플레이어 세력의 재야 인재 고용 허용 여부입니다.
- `WICampaignVariantDefinition.characterPlacements`: 런타임 생성이 아닌 ScriptableObject 저장형 시작 인물 배치 목록입니다.
- 현재 캐릭터 마스터는 태생 영웅 200명과 일반 1000명, 총 1200명이며 시나리오별 시작 배치는 영웅 100명과 일반 150명, 총 250명입니다. 시작 시 재야 풀은 영웅 100명과 일반 850명, 총 950명입니다.
- 아레스 메인 `castle_28`에는 태생 영웅 4명과 일반 병사 10명, 총 14명이 배치됩니다.
- 자동 플레이어의 목표 고용 인원은 `14 + (보유 성 수 - 1) × 8`이며 6성 기준 54명입니다. 목표를 채운 뒤에는 불필요한 무한 영입을 중단합니다.
- 내정 가능 판정은 태생 영웅 또는 승격 영웅입니다. 일반 등급은 모든 개인 활동과 내정 담당에서 제외되며 전투단 편성·군사 행동·전투단 합동훈련과 대기 회복만 적용됩니다.
# Campaign Auto Test Lab 상세 데이터

- 결과 행의 `상세` 버튼과 `detail-panel`은 `WICampaignAutoTestLab.uxml`에 9개 행분이 고정 배치되어 있습니다.
- `BuildDetailText`는 최종 영토, 전투, 인물 등급 구성, 실제 신규 영입 성공, 재야 복귀, 사망과 미해결 상태를 설명하고 장기 정체 조건을 진단합니다.
# 인재 영입 진척 데이터

- `WIAdministrationDatabaseSO.RecruitmentBaseProgress`: 월간 영입 설득 기본 진척입니다. ScriptableObject 저장값은 35입니다.
- 후보별 월간 설득 진척 공식은 `RecruitmentBaseProgress + recruiter.Charisma / 4`이며 최대 100으로 제한됩니다.

# 인물 휴식 데이터

- `WIAdministrationDatabaseSO.CharacterRestFatigueRecovery`: 영웅 개인 휴식 1개월의 피로 회복량입니다. ScriptableObject 저장값은 50입니다.
- 개인 휴식은 부상 기간을 1개월 줄이며, 일반 등급의 개인 활동 제한과 전투단 대기 회복 15에는 영향을 주지 않습니다.

# 자동 캠페인 전략 데이터

# 내정 기반 군사 성장 데이터

- `WICharacterRuntimeState.Experience`: 전투단 전투력 계산에서 `Experience / 25`, 최대 +20의 개인 성장 보너스로 사용합니다.
- `WIArmyState.CohesionExperience`: 합동훈련뿐 아니라 주둔 성의 훈련 사업 성과로도 증가합니다. 훈련 사업은 성과의 2배를 합동 경험으로, 성과만큼을 각 구성원 경험으로 지급합니다.
- 진영 군사 기반 보너스는 현재 보유 성의 `Prosperity + Technology` 평균을 20으로 나누며 0~10 범위로 제한합니다. 별도 저장 필드 없이 성 런타임 데이터에서 계산합니다.

- `WIAutoStrategicPlan`: 한 자동 실행 동안 목표 성, 집결 성, 전략 단계, 회복 종료 턴, 최근 패배 턴과 연속 패배를 보존합니다. 캠페인 저장 데이터에는 포함하지 않습니다.
- `WIAutoDecisionTrace`: 월, 전략 단계, 목표 성, 이유 코드와 설명을 기록합니다.
- `WIAutoCampaignMetrics`: 개인 휴식, 회복 개월, 목표 변경, 군사 무행동 개월, 최장 무원정, 최고 영웅 피로와 판단 이유 분포를 수집합니다.
- `WIAutoCampaignMetrics.CharacterCaptures`, `CharacterDefections`: 자동 실행 중 전투 결과로 새로 발생한 포로와 적 합류 횟수를 누적합니다. 현재 상태 인원수가 아니라 발생 사건 수입니다.
- `PositionArmiesForJointAttack`: 목표 성의 현재 수비 전력과 정책 안전선을 계산해 가장 약한 전투단을 수비대로 남겨도 되는지 판정합니다. 원정 전력이 부족하면 고정 수비대를 해제하여 모든 가용 전투단을 집결시킵니다.
- `GetRequiredAttackPower`: 목표 수비 전력에 정책별 안전 비율을 적용한 필요 공격력을 반환합니다. 균형형·공세형은 180개월 이후 110%로 전환됩니다.
- `ResolveStrategicBattleOutcome`: 전략 자동 전투의 난이도별 전술 변동과 근접전 패착을 턴·세션 ID 기반으로 결정합니다. 실시간 전투 결과 제출에는 적용되지 않습니다.
- 자동 플레이는 손실 전투단의 현재 성에 대기 인물이 없을 때 후방의 유휴 일반 인물을 `StartCharacterTransfer`로 한 경유 구간 이동시키거나 전투단 자체를 가장 가까운 보충 성으로 이동시킵니다. 플레이어의 수동 인물 이동은 같은 API로 최종 목적지 전체 경로를 한 번에 예약할 수 있습니다.
- 자동 영입 권장 인원 공식은 `12 + 플레이어 보유 성 수 × 4`입니다.
- `WIAutoStrategicPlan.LastAttackTargetCastleId`, `SameTargetDefeats`: 같은 목표의 연속 공격 패배를 추적합니다.
- `AvoidedTargetCastleId`, `AvoidedTargetUntilTurn`: 2회 연속 패배한 목표를 18개월 동안 후보에서 제외합니다.
- 전략 목표 점수는 목표 성의 현재 수비 전력과 같은 세력의 인접 성·전투단 반격 위험을 더하고, 번영·기술·성 규모 가치를 뺍니다.
- `WIAutoCampaignMetrics.ArmyReinforcements`, `GoalsAbandoned`, `JointAttackBattles`: 회복 중 전투단 보충 인원, 반복 패배로 포기한 목표 수, 플레이어 공동 공격으로 해결한 전투 수입니다.
- `WIAutoCampaignMetrics.ThreatResponseMonths`, `DefensiveReinforcementMarches`: 적의 아군 성 접근을 감지해 원정을 보류한 개월 수와 실제 아군 성 경로로 시작한 방어 증원 이동 수입니다.


2026-09-12 UI 점검 기록: 데이터 구조 변경 없음. 프리팹 Image의 누락 GUID 2종/20개 위치를 UIAudit/2026-09-12/missing-sprites.json에 보존했습니다. 전체 시각 점검 결과와 검증 범위는 [UIAuditReport-2026-09-12.md](UIAuditReport-2026-09-12.md) 참고.

## UI 점검 후 수정 (2026-09-13)

- 내정 프리팹 20개의 공통 배경·제목·안내문·초상 영역을 보정했다. 월간 보고 피벗/경계/버튼 이미지, 개인 활동 버튼과 반복 지시 겹침, 첫해 안내 장식 간섭을 수정했다.
- 전투 HUD는 미리 작성한 스킬 슬롯 4개와 페이지 버튼을 사용한다. 지도는 기존 성 이름의 배치를 화면 크기와 주변 라벨에 맞춰 보정한다. 지원 목록에서 단일 페이지의 이동 버튼을 숨긴다.
- WIUIAuditRepairUtility는 지정한 기존 프리팹 하나를 보정하며 최초 백업은 Temp/UIRepair/Backups에 둔다. 편집 모드에서 사용한다. 월간 보고 재구성에도 공통 보정이 적용된다. 미준비 이미지는 빈 슬롯으로 유지한다.
- 누락 이미지 GUID 2종의 기존 20개 참조를 정리했다. 최종 재검색 0건. UI 회귀 및 UGUI 이행 EditMode 검사 54/54 통과(job 78684effea934dfab4f018a3963a2cec), 컴파일 후 오류/경고 조회 0건.
- 수정 후 화면 증거: GameDocuments/UIAudit/2026-09-12/After. 월간 보고 1366×768·1920×1080·2560×1440, 연구, 위임, 개인 활동 목록/행동, 지도, 첫해 안내, 전투 스킬 페이지 이동을 확인했다.
- 남은 시각 검증: 최신 성 기록/중점 사업 간격 보정 재촬영, 실제 결정 카드가 채워진 월간 보고의 모든 상태, 긴 영지 이름과 검색/필터 장식의 세부 가독성. 모든 데이터 조합과 해상도에 대한 검증 완료를 뜻하지 않는다.
- 최초 점검 보고서의 '수정 없음'과 누락 참조 목록은 수정 전 기록이다. 기존 다른 작업의 변경은 유지했으며 커밋·푸시는 하지 않았다. 밸런스/마스터 데이터 구조 변경은 없다.

- 2026-09-13 기획 점검: 현재 데이터로 자동 재미 검사 24개 실행, 프리 균형형 60개월 원정 없음 1건 실패 확인. 내정/균형 120개월 원정 0회·성 1개, 공세 17원정·성 2개. 상세 수치·시나리오 구분·분석 한계는 GameDesignAudit-2026-09-13.md 참조. 게임 구현 및 데이터 구조는 변경하지 않음.

- 2026-09-13 기획 2차 진단: 자동 영입 담당자 자격과 실제 탐색 자격 불일치 확인. 프리 60개월에서 기존 외교/원정 명령으로 대체 전선 진입 가능, 메인 균형형은 60~120개월 6성·8원정 정체. 진단용 WIGameDesignDiagnosticTests 및 DesignAuditEvidence 추가, 4개 실행 완료. 생산 규칙·마스터 데이터 변경 없음. 상세는 GameDesignAudit-2026-09-13.md 참조.

- 2026-09-13 메인 한정 진단: 60/120개월 castle_29 목표, 집결 976/자동 기준 1047로 동일. 3개 진단 실행 완료. 안전 기준·목표 유지·일반 한정 충원 등 자동 정책 제한을 게임 기획과 분리 기록함. 프리 진단 실행 제외, 생산 데이터 변경 없음. GameDesignAudit-2026-09-13.md 참조.

- 2026-09-13 메인 명령 비교: 기존 전력으로 원정 가능, 도착 공격925/수비872의 대기 전투 생성. 영웅 증원은 도착 성 인원13/한도4로 거절됨. 부대 집결과 개별 인물 수용 규칙 차이를 기획 검토 항목으로 기록. 진단2개 완료, 실제 전투 승패 미검증. GameDesignAudit-2026-09-13.md 참조.

## 2026-09-13 거주 인원과 전투단 인원 구분 적용
- GetCastleResidentHeroIds는 성 HeroIds 중 전투단 편성 인원을 제외한다. 저장 구조/마스터 데이터는 유지한다.
- 개별 이동 시작·도착·경로 중단 복귀와 기존 성 배치/이동 선택/인원 표시가 거주 인원 기준을 사용한다. 이동 예약은 거주 슬롯을 사용하며 이동 시간과 대장 편성 한도는 유지한다.
- 영입·부대 귀환·해산은 기존 초과 수용 동작을 유지한다. 초과 거주 상태에서 새 개별 이동은 차단되며 기존 인물을 삭제하지 않는다. 전투단으로 편성된 인물은 거주 카드에서 제외된다.
- 메인 60개월 비교: 증원 없이 즉시 원정하면 출발976/도착925 대 수비872. 후방 영웅4명을 4개월 이동 후 편입하면 출발1217/도착1147 대 수비872. 기존에는 4명 모두 이동 거절됐다. 이동 중 내정 사용 차단·도착 후 예약 해소·거주 한도 검사를 추가했다.
- UGUI 회귀47개와 메인 진단2개 합계49개 통과. 추가 검사는 진단용 파일에서 수행한다. 실제 실시간 전투의 승패/손실과 내정 손실량 비교는 남아 있다. 자동 플레이는 아직 이 보충 경로를 사용하도록 변경하지 않았다.
## 2026-09-13 메인 자동 보충 및 전투 비교 후속
- 메인 자동 보충/편입은 일반 한정을 해제해 영웅도 사용하며, 후방 인물은 경유 성이 아닌 최종 보충 성까지 기존 이동 명령으로 보낸다. 이동 예약 인원을 고려한다. 영지관/마지막 내정 인물 보존에 더해 마지막 대기 인재영입 담당자를 보존한다.
- 메인 자동 영입 담당자 선정은 실제 CanRecruitTalent 자격을 사용한다. 프리 시나리오는 검사하지 않았으며 해당 정책 조건은 기존대로 유지했다.
- 무조작 실시간 전투 로직(30Hz, 최대600초, 스킬/명령 입력 없음) 비교: 즉시 공격 승리63.03초·공격 전투불능7/13, 4개월 증원 후 승리49.03초·전투불능8/17. 전투불능은 영구 사망이 아니다. 결과를 캠페인에 제출한 뒤 부상·사망·포로까지 추적한 검사는 아니다. 증원이 절대 손실을 줄인다고 주장할 수 없다.
- 자동 보충만 늘린 최초 실험은 영입 담당자를 소진해 신규영입0으로 정체했다. 담당자 보존과 자격 보정 후 메인 균형형 24개월2성/3원정/신규영입2, 60개월7성/11원정/신규영입11, 120개월15성/29원정/신규영입24, 승패15/0. 기존120개월6성/8원정/영입5에서 개선됨. 여러 변경의 결합 효과이며 개별 인과 효과는 분리하지 않았다.
- 최종 진단3개 실행 완료(job656a22c1d29549678b864d3213b97bbb). 기본 시작 조건 한 표본이며 재미/승률 보증이 아니다. 이전 고정6성 배치 전용 비교 메서드는 Explicit로 표시해 현재 자동 정책의 일반 회귀에서 제외했다. 재실행하려면 당시 배치를 별도 고정 상태로 보존해야 한다.
- 내정 손실량의 공정한 비교는 미완료다. 같은 기간/같은 대체 내정 명령을 둔 대조군이 없어 단순 금화 차이를 증원 비용으로 해석하지 않는다. 기존 비교 인물은 대기 상태였으며 작업을 취소한 사례가 아니다. 앞으로 실제 담당자의 내정 공백을 포함한 대조와 전투 결과의 캠페인 반영을 확인해야 한다.

- 2026-09-13: 중점 사업 지정 시 확장을 제외한 사업은 지시 유지가 자동 활성화된다. 같은 담당자·사업·투자 방식으로 다음 달 실행하며 새 사업 지정 시 유지 지시도 교체한다. 기존 지시 유지 버튼으로 해제 가능하다. 자금 부족/담당자 사용 중에는 대기, 담당자 이탈/목표 완료 시 중단한다. 기존 저장의 이미 완료된 비반복 사업은 소급 복원하지 않으며 다시 한 번 지정하면 적용된다. 데이터 구조 변경 없음.


- 2026-09-13: 영웅 배치 모달을 '미배치 인물 배치'로 명확히 하고 빈 후보/배치 불가 화면에 역할과 다른 성 인물 이동 방법 안내를 추가했다. 인재 활동은 선택한 담당자 이름을 활동/교류 상대/영입 대상 제목에 유지하고 상대 선택 안내를 UID로 제공한다. 활동 선택 중 검색 콜백이 후보 카드 화면을 다시 표시하지 않도록 방어했다. 훈련/휴식은 직접 배정, 교류/영입은 상대 선택이라는 기존 규칙을 유지했다. 신규 고정 UI 생성 없음. WIActivityCopyUtility가 기존 DB uiStrings의 관련6개 UID만 저장한다. 컴파일 오류 없음, UGUI 회귀47개 실행. 실제 클릭 재현 검증은 별도 필요하다.
