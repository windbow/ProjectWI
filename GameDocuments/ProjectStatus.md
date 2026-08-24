# ProjectWI 프로젝트 상태

- 2026-08-24: 아레스 메인 표준 난이도를 정책 3종 × 24·60·120개월 × 5개 시드로 재검증함. 카르디아는 제4개월 `148 대 35`로 점령되어 도입부는 통과했으나 균형형·공세형 모두 120개월 동안 후속 원정이 0회였고 모든 시드 결과가 동일했음. 120개월 발도르 26성·네크로폴리스 24성, 아발론 2성, 최종 고용 8명(영웅 2/일반 6), 신규 영입 2회로 나타나 장기 확장 재미와 최종 대국 구도는 불합격으로 판정함. 테스트 보고 로그에 최종 인물 구성과 발견·영입·재야 복귀 지표를 추가함.

- 2026-08-24: Campaign Auto Test Lab 결과 행에 `인물(영/일)`과 `영입(발/영/복)` 열을 추가함. 플레이어 세력의 최종 고용 인원·영웅·일반 수와 실행 중 발견·신규 영입·재야 복귀 수를 화면에서 확인할 수 있으며 CSV·Markdown에는 현재 재야 후보 수도 포함함.

- 2026-08-24: 인물 사망 규칙을 보정하여 태생 영웅만 영구 사망하게 변경함. 일반 출신 인물은 영웅으로 승격한 상태에서 사망·처형되어도 승격 상태·승격 성취·작위를 잃고 6개월 뒤 일반 재야 인재로 복귀함. 기존 재야 중복 고용 검사는 그대로 적용함.

- 2026-08-24: 기존 `ProjectWI/Tools/Campaign Auto Test Lab`을 인물 순환까지 검증하는 캠페인 시뮬레이션 도구로 확장함. 태생 영웅은 사망 시 영구 퇴장하고, 일반 출신 인물은 승격 여부와 무관하게 6개월 후 고용·성·전투단·포로·전향 중복을 검사한 뒤 살아남은 성의 일반 재야 인재로 복귀하도록 구현함. 자동 플레이어가 실제 탐색·영입 활동을 지정하며 재야 복귀·발견·신규 영입 수치를 CSV와 Markdown에 기록함.

- 2026-08-23: 자동 플레이를 수동 플레이에 가까운 가상 플레이어로 만들고 일반 세력 AI와 공통 전략 평가기를 공유하기 위한 `AIAutoPlayerAdvancementPlan.md`를 작성함. 현재 자동 플레이어가 인접·전쟁·피로·전력 우세 조건만 확인하고 실패 후 회복 계획이 없으며, 일반 AI는 위협받는 성이 없으면 공격 판단을 종료하는 구조임을 확인함. 결정 이유 추적 → 자동 플레이 복구 행동 → 공통 목표 평가기 → 일반 AI 전환 → 외교·난이도 고도화 순으로 계획함.

- 2026-08-23: 아레스 메인의 기본 진행을 내러티브 잠금과 분리함. 프로스트혼을 기존 카르디아 좌표 `(0.54, 0.18)`로 옮기고 카르디아를 아래 `(0.54, 0.11)`로 조정했으며, 카르디아에서 룬포지와 브론즈게이트로 이어지는 양방향 연결을 시나리오 `castlePlacements` 데이터에 추가함. 성 위치·소유권·연결을 Unity Inspector에서 편집하는 방법을 `ScenarioLayout.md`와 `DataManual.md`에 기록함.

- 2026-08-23: 단일 월드 프리팹 전환 후 아레스 메인 표준 난이도를 내정형·균형형·공세형 × 24·60·120개월 × 5회 재검증함. 균형형·공세형은 모든 실행에서 제4개월 카르디아를 160 대 35로 점령하고 아발론 2·발도르 46성을 유지했으며 발도르의 조기 탈환은 없었음. 반면 내러티브 잠금으로 120개월까지 추가 전투가 없고 금화가 약 6,984~7,213까지 누적되어, 첫해 도입부는 합격이나 후속 장기 밸런스는 이야기 개방 시점 확정 뒤 재조정이 필요함.

- 2026-08-23: March·CastleRecord 프리팹을 열어 공용 셸·제목 배치·닫기/일반/주요 버튼 Sprite와 상태 색을 변경한 뒤 같은 경로에 저장하던 `WIAdministrationModalVisualUtility`와 메타 파일을 제거함. 프리팹을 변경·저장하는 Editor 코드는 더 이상 남아 있지 않으며, MainScene UI 관리자만 정리하는 `WIAdministrationUGUISceneUtility`와 런타임 데이터 표시 코드는 유지함.

- 2026-08-23: 시나리오마다 월드 UI 프리팹을 복제하던 구성을 폐기함. 미참조 중복 자산 `WIAdministrationWorldUGUI_Free.prefab`을 제거하고 `WIAdministrationWorldUGUI.prefab` 하나만 유지함. 60개 성 노드는 공용 UI 구조로 사용하며 성 소유 세력·좌표·연결은 선택한 `WICampaignVariantDefinition.castlePlacements` 데이터 테이블에서 런타임에 적용함. 프리 시나리오는 성 마스터 데이터의 기본 배치를 사용함.

- 2026-08-23: UI Toolkit에서 UGUI로 최초 이행할 때 사용한 일회성 `WIAdministration*UGUIBuilder` 22개와 메타 파일을 전부 제거함. 완성된 UGUI 프리팹과 런타임 컨트롤러는 유지하며, 고정 디자인의 위치·앵커·Sprite를 코드 기본값으로 다시 덮어쓰는 `WI/UI/Build ... UGUI` 메뉴도 함께 제거됨. 이후 UGUI 디자인은 Prefab Mode에서 직접 편집하고 데이터 기반 텍스트·초상·성 이미지·진영 마커만 런타임 컨트롤러가 갱신함.

- 2026-08-23: 아레스 메인 영토를 아발론 1·발도르 47·나머지 세 세력 각 4성으로 재편하고 발도르 별도 국력 배율 없이 영토 우위만 적용함. 카르디아를 방어 10·질서 20의 도입 관문으로 낮추고 프로스트혼과 독립 연결하여 공세형 자동 진행 제4개월 점령·첫해 유지 검사를 통과함. 두 성에 일반 인재 각 4명을 귀속하고 성 기반 탐색 및 영입 후 실제 성 배치를 구현함.

- 2026-08-23: 아레스 메인 표준 난이도를 내정형·균형형·공세형 × 24·60·120개월 × 5회 검증함. 모든 정책이 120개월에도 프로스트혼 1성에 고립되었고, 발도르는 7~10성까지 줄어드는 동안 실반로드가 28~31성으로 성장해 메인 시나리오 목표와 반대되는 구조를 확인함. 원시 지표를 재실행하는 `WIAresMainBalanceSimulationTests`와 보고서 진단을 추가함.

- 2026-08-23: 시나리오 성 좌표를 별도 프리팹으로 미리보기하던 방식을 시험했으나, 후속 구조 정리에서 단일 월드 프리팹과 시나리오 데이터 테이블 방식으로 대체함.

- 2026-08-23: 아레스 메인과 프리 시나리오의 성 소유권·좌표·연결을 분리함. 아레스는 최북단 프로스트혼 한 성에서 시작해 카르디아와만 연결되고, 아이언하트 성은 모두 그 아래에 배치됨. 최초 발도르 공격 검토 주기는 12개월로 구성했으며 후속 도입부 검증에서 24개월로 조정함.

- 2026-08-23: 내정·전쟁 밸런스 작업 계획을 `AdministrationBalancePlan.md`로 정리하고 표준 난이도 자동 플레이 9회(내정형·균형형·공세형 × 24·60·120개월)의 기준값을 기록함. 24개월 전투가 각각 14·12·11회로 초반 밀도가 높음을 확인함. AI 공격을 일괄 6개월·4개월 금지하는 안은 균형형·공세형 조기 멸망을 유발해 철회했으며, 신규 선전포고를 만드는 전선 압력 주기만 12개월에서 18개월로 조정함. 초기 아발론–발도르 전쟁은 시나리오 결정 대상으로 남김.

- 2026-08-23: 전투단의 큰 패배 시 인물 한 명이 사망·포로·후퇴·적 합류 중 하나의 운명을 맞는 시스템을 추가함. 여유 난이도는 항상 후퇴하며, 표준/도전 난이도별 확률을 ScriptableObject에서 편집할 수 있음. 생존가·탈출가·불굴 특성이 각각 사망 감소·포로 감소·전향 금지를 제공하고, 포로 발생 즉시 금화 또는 마나 몸값 요청이 외교 포로 교환 카드에 표시됨. 저장 필드와 결정적 판정, 회귀 테스트를 함께 추가함.

- 2026-08-23: 가로형 정보 패널 초안을 게임용 무문자 배경 `bg_type_g.png`로 재제작함. 원본과 동일한 567×292 규격을 유지하면서 좌측의 끊긴 테두리와 좌상단 얼룩을 제거하고, 중립적인 냉색 흑청 내부 면과 얇은 저채도 회갈색 금속 프레임으로 정리함. Unity Sprite/Single, Mipmap 비활성, Bilinear, Clamp, 기본 플랫폼 무압축 및 사방 10px 9-Slice Border로 임포트했으며 프리팹과 UI는 변경하지 않음.

- 2026-08-23: `WIAdministrationWorldUGUI` 선택 성 상세 6개 행의 제목과 값을 공백으로 밀어낸 단일 TMP 문자열에서 각각 독립 TMP로 분리함. `CastleDetailRow1~6`은 영지관·번영·기술·질서·방어·주둔 전투단 제목을 왼쪽 정렬하고, 새 `CastleDetailValue1~6`은 실제 값을 오른쪽 정렬하며 `WIAdministrationWorldSnapshot`도 `CastleDetailTitles`와 `CastleDetailValues`로 분리함. 기존 프리팹 전체 재생성 없이 해당 행과 컨트롤러 배열만 UnityMCP로 수정했고 빌더·회귀 검사도 동기화함. 관련 EditMode 3/3, 플레이 모드 실제 값·정렬 확인 및 Console Error 0건 통과.

- 2026-08-23: 성 내정 명령 패널 시안의 외곽 박스를 위한 무문자 정사각형 배경 `bg_type_f.png`를 제작함. 512×512 규격에 중립적인 흑청색 내부 질감과 시안의 절제된 회갈색 금속 외곽선·작은 모서리 장식만 유지하고 문구·아이콘·버튼·구분선은 제외했으며, Unity Sprite/Single 및 사방 10px 9-Slice Border로 임포트함. 프리팹과 UI는 변경하지 않았고 Unity 재임포트 및 Console Error 0건 확인.

- 2026-08-23: `WIAdministrationWorldUGUI`의 175×68 명령 버튼에 사용하는 `bg_type_e.png`를 원본 시안의 무문자 냉색 흑청 버튼 배경으로 재제작함. 기존 1718×636 대형 에셋 때문에 100px 9-Slice 테두리가 과도하게 표시되던 문제를 해결하기 위해 최종 이미지를 175×68로 맞추고 Sprite Rect 175×68, Border 사방 10px로 조정했으며 기존 GUID와 Sprite 내부 ID를 유지해 프리팹 참조는 변경하지 않음. Unity 재임포트 및 Console Error 0건 확인.

- 2026-08-22: 성 내정의 `진격/출정`과 `성 상세` 화면도 공용 모달 UI 대상에 포함함. 기존 프리팹의 전투단 선택·성 이미지·상세 기록 본문 배치는 재생성하지 않고 유지하면서 `administration_modal_shell_v1` 외곽, 공용 제목 헤더, X 닫기 버튼과 평면 버튼만 적용함. 두 빌더에도 공용 처리 호출을 추가해 향후 재생성 시 구형 기본 UI로 돌아가지 않도록 했으며 공용 UI 회귀 검사 범위를 8개 화면으로 확장함.

- 2026-08-22: 목표 상세 시안의 외곽을 바탕으로 무문자 냉색 금속 `administration_modal_shell_v1.png`을 제작해 중점 사업·인사 배치·인재 활동·특화 시설·기본 시설·태수 위임 6개 프리팹의 공용 셸로 적용함. 사용자가 직접 조정한 `WIAdministrationObjectiveUGUI`의 이미지·텍스트 배치는 공용화 대상에서 제외하고 기존 `objective_modal_frame_v1.png` 기반 구성으로 복원함. UGUI 이행 EditMode 42/42 및 Console Error 0건 통과.

- 2026-08-22: 성 내정의 중점 사업·인사 배치·인재 활동·특화 시설·기본 시설·태수 위임 화면이 구형 `popup_header` 종이 헤더와 흰 `button_normal`을 재사용하던 문제를 공통 수정함. `WIAdministrationModalVisualUtility`를 추가해 6개 빌더가 흑청색 `bg_type_d` 본문, 얇은 냉색 금속 헤더, 평면 일반·주요 버튼, 정사각형 전용 X 닫기 버튼과 일관된 호버·비활성 색을 적용하도록 변경함. 6개 프리팹을 재생성하고 중점 사업 화면을 실제 Game View에서 확인했으며 UGUI 이행 EditMode 42/42 및 Console Error 0건 통과.

- 2026-08-22: 턴 후속 공용 정보 패널 `turn_followup_info_panel_v1.png`의 외곽 잡티 4픽셀을 제거한 뒤, 위 약 84px·아래 약 126px의 과도한 투명 여백 때문에 Slice 기준선이 실제 프레임과 어긋나던 문제를 추가 수정함. 장식 바깥에 4px 투명 여백만 남겨 1955×509로 정리하고 Sprite Border를 사방 48px로 지정했으며, 설명·체크 패널 4개를 모두 `Image.Type.Sliced`로 유지함. 잡티 원본과 정리 전 1955×711 이미지는 `Assets/TrashAsset/UI/Generated`에 각각 보관함. UGUI 이행 EditMode 41/41 및 Console Error 0건 통과.

- 2026-08-21: MainScene에 `WIUIScreenManager`가 `MainCanvas`와 `UGUI Screen Bootstrap` 두 개로 중복 배치되어 같은 UGUI 프리팹을 이중 생성하던 구성을 정리함. `MainCanvas`의 6개 참조를 정식 `UGUI Screen Bootstrap`의 전체 23개 참조에 병합한 뒤 중복 오브젝트를 삭제함. `WIAdministrationUGUISceneUtility`는 현재 씬의 모든 화면 관리자와 이전 루트 이름을 수집해 참조를 보존한 뒤 관리자 하나만 남기도록 보강했으며, 정리 메뉴를 두 번 실행해도 관리자 1개·프리팹 23개가 유지됨을 확인함. UGUI 이행 EditMode 40/40 및 Console Error 0건 통과.

- 2026-08-21: 성 내정 화면의 상단 HUD를 월드 화면 기준으로 통일함. 높이 92px, 배경색, 하단 금속선, 진영 문장과 명칭, 날짜·금화·마나·영향력 텍스트의 앵커·폰트 크기·정렬, 자원 아이콘, 세로 구분선, 우측 월보·의회·연구·설정 아이콘과 클릭 영역을 월드 프리팹과 동일하게 구성함. 성 내정 컨트롤러에도 월보·의회·연구·설정 기능을 연결했으며 두 프리팹의 TopHUD 직계 구조·RectTransform·Image·TMP 설정을 비교하는 회귀 검사를 추가함. UGUI 이행 EditMode 40/40 및 Console Error 0건 통과.

- 2026-08-21: 세로형 공통 패널 `bg_type_d.png`의 프레임 바깥 투명 여백에 남아 있던 1~4px 크기의 생성 노이즈 69픽셀을 제거함. 정상 패널 본체 307,605픽셀과 512×640 규격, 냉색 흑청 질감, 은회색 프레임은 그대로 유지했으며 기존 16px 9-Slice 설정도 보존함. 정리 전 원본은 `Assets/TrashAsset/UI/Generated/bg_type_d_noisy_original.png`에 보관함.

- 2026-08-20: 성 내정 화면의 `대륙 지도` 버튼이 공용 명령 버튼의 굵은 장식 외곽선을 크게 늘려 사용해 촌스럽게 보이던 부분을 정리함. 무문자 냉색 흑청 바탕과 얇은 은회색 이중선·작은 모서리 및 중앙 장식만 사용한 전용 512×142 `territory_back_button_v1.png`를 제작하고, 좌우 12px·상하 10px 9-Slice로 적용함. 버튼 표시 높이와 글자 크기도 시안 비율에 맞게 축소했으며 공용 명령·다음 턴 버튼에는 영향을 주지 않음. UGUI 이행 EditMode 39/39 및 Console Error 0건 통과.

- 2026-08-20: 성 내정 화면을 기준 시안의 성 전경 중심 구조로 전면 재배치함. 전략 화면과 같은 상단 진영·연월·자원 HUD, 좌측 성명·영지관·4종 능력치·월 수입·진행 사업, 중앙 대형 성 이미지, 우측 8개 성 내정 명령, 하단 주둔 영웅 4칸·특화 시설 2칸·이번 달 중점, 대륙 지도 및 다음 턴 버튼으로 구성함. 무문자 냉색 금속 하단 트레이 `territory_bottom_panel_v1.png`를 제작해 상하좌우 18px 9-Slice로 적용했으며 기존 성·영웅·시설 ScriptableObject와 명령 이벤트 연결을 유지함. 실제 Game View 1920×1080 시각 확인, UGUI 이행 EditMode 39/39 및 Console Error 0건 통과.

- 2026-08-17: 전략 화면 우측 알림 패널 시안을 기준으로 재사용 가능한 512×640 `bg_type_d.png`를 제작함. 문구·아이콘·알림 행을 제외하고 얇은 은회색 금속 프레임과 중립적인 흑청색 질감만 유지했으며, Sprite/Single·Clamp·Bilinear·Mipmap 비활성·무압축 및 상하좌우 16px 9-Slice를 적용함. `.bg-type-d` USS 클래스를 추가하고 UGUI 전략 화면의 `CampaignSidePanel` 배경을 D Type으로 교체함. 실제 Game View에서 확대 시 모서리 보존을 확인했고 UGUI 이행 EditMode 39/39 및 Console Error 0건을 통과함.

- 2026-08-17: 기존 `Builds/Windows` 테스트 빌드를 삭제하고 Unity 상단 `ProjectWI/Build/Package Windows Test Build` 원클릭 패키징 메뉴를 추가함. 활성 Build Settings 씬을 검증해 Windows x86-64 Development/LZ4 빌드를 `Builds/Windows`에 클린 생성하고, 완료된 폴더 전체를 버전·시각이 포함된 `Builds/Packages/ProjectWI-Windows-*.zip`으로 압축함. 경로가 프로젝트 밖으로 벗어나지 않도록 삭제·출력 안전 검사를 포함하며 컴파일 오류 0건과 메뉴 등록을 확인함.

- 2026-08-17: 전략 화면 좌측 선택 성 패널을 기준 시안 구조로 재작성함. 작은 문장·성명·소속·등급 아이콘 헤더, 가로형 성 이미지, 실제 영지관·번영·기술·질서·방어·주둔 전투단 6행, 영웅 카드 4개, 성 관리와 보조 성 아이콘 버튼을 배치함. 존재하지 않는 인구·식량·행복도 수치는 임의 생성하지 않고 현재 ScriptableObject/런타임 성 데이터만 표시하며 보조 버튼도 성 관리 기능에 연결함. Game View 확인, UGUI EditMode 39/39, Console Error 0건 통과.

- 2026-08-17: 새 하단 기준 시안에 따라 전략 명령부를 다시 구성함. 전체 청동 프레임과 하단 설정 버튼을 제거하고 군사·인사·외교·계략·연구·평정·월보 7개 평면 명령 버튼을 배치함. 좌측 이중 화살촉, 문장 슬롯, 남청색 본체를 가진 `strategy_next_turn_button_v3`를 제작해 아발론 문장·`다음 턴`·우측 화살표를 독립 요소로 배치했으며 기능은 기존 Military/Heroes/Diplomacy/Scheme/Research/Faction/MonthlyReport 및 EndTurn에 연결함. UGUI EditMode 39/39, Console Error 0건 확인.

- 2026-08-17: 전략 화면 하단을 단순 비율 보정에서 시안 전용 아트 적용으로 갱신함. 참고 이미지의 얇은 은색 이중선·작은 상단 장식·절제된 모서리를 재현한 `strategy_command_button_v2`와 양끝 창날 장식·남청색 본체의 `strategy_next_turn_button_v2`를 새로 제작하고 체크무늬 생성 배경을 투명 알파로 정리함. 9-Slice Border와 표시 높이를 고정해 8개 명령·설정·다음 턴의 기존 기능은 유지했으며 Game View 비교, UGUI EditMode 39/39, Console Error 0건을 확인함.

- 2026-08-17: 전략 화면 하단 명령 바를 기준 시안 비율에 맞춰 보정함. 8개 명령 버튼의 시작점·폭·간격을 조절하고 아이콘을 확대했으며 문구를 아이콘 오른쪽에 좌측 정렬함. 설정 영역을 넓히고 다음 턴 버튼과의 간격을 정리했으며 기존 전용 프레임과 실제 단축키·버튼 기능은 유지함. Game View 육안 확인, UGUI EditMode 39/39 및 컴파일 오류 0건을 확인함.

- 2026-08-17: UI Toolkit 전역 지도에 존재했던 성 연결 경로를 UGUI로 복원함. `AdjacentCastleIds`를 중복 제거한 스냅샷 경로로 변환하고 단일 `MaskableGraphic` 메시에서 그림자·진영색 중심선·적대 전선 `×`·선택 경로 청백색 광택과 마름모를 렌더링함. 연결선은 성 마커 앞에서 끝나고 Raycast를 받지 않아 기존 60개 성 버튼 입력을 방해하지 않음.

- 2026-08-17: 전략 화면 상·하단 공통 프레임 `bg_type_c`가 Sliced Image로 사용되지만 Sprite Border가 0이어서 모서리가 늘어나던 문제를 수정함. 1587×508 원본의 장식 범위를 기준으로 좌우 80px·상하 64px Border를 지정함.

- 2026-08-17: 턴 후속 시안의 선택 설명 위 중앙 다이아 구분선이 누락되고 닫기 버튼이 공용 배경+TMP 문자로 표시되던 차이를 수정함. 얇은 금속선과 중앙 다이아를 한 Sprite로 만든 `turn_followup_choice_divider_v1.png`을 좌우 선택 영역에 각각 배치하고, 금속 사각 프레임과 X가 통째로 포함된 `turn_followup_close_button_v1.png`을 제작해 닫기 Button Image에 적용함. 기존 닫기 TMP 문자는 비활성화해 에셋 자체의 X만 표시함.

- 2026-08-17: 턴 후속 전용 버튼이 작은 표시 영역에서 96/64px 9-Slice 때문에 모서리와 중앙 다이아가 안쪽으로 밀리고, 두 줄 선택 문구가 버튼 내부에 겹치던 문제를 수정함. 버튼 Sprite는 시안과 같은 약 4:1 고정 비율이므로 Sliced 대신 Simple로 표시하고 Border를 0으로 변경함. Snapshot 선택 문자열은 첫 줄 제목과 둘째 줄 설명으로 분리해 제목만 23pt로 버튼 중앙에, 설명은 16pt로 버튼 위에 배치함. Prefab Stage 육안 검사에서 양쪽 버튼 프레임과 중앙 장식이 원형대로 표시됨을 확인함.

- 2026-08-17: 턴 후속 시안과 달라진 단일 단순화 나침반 V2를 철회하고 장식 역할을 다시 분리함. 상단 설명 패널에는 시안과 같은 방패형 소형 나침반 `turn_followup_header_emblem_v1.png`(210×256), 하단 체크 패널에는 저채도 대륙 지도·원형 좌표·가느다란 8방향 나침반을 결합한 `turn_followup_map_compass_v1.png`(768×680)을 제작해 각각 적용함. 두 Sprite 모두 표시 크기에 맞춰 사전 축소하고 무압축·Mipmap 비활성·Bilinear로 임포트했으며, 단순화 V2는 `Assets/TrashAsset/UI/Generated`로 이동함.

- 2026-08-17: 턴 후속 화면의 공용 버튼을 시안 전용 무문자 버튼 `turn_followup_button_normal_v1.png`, `turn_followup_button_primary_v1.png`으로 교체하고 좌우·상하 96/64px 9-Slice를 적용함. 나침반 흐림은 기존 1,194×1,220 원본이 100~250px UI 영역으로 축소되면서 미세 지도선과 섬 디테일이 뭉개지는 문제로 확인함. 임포터는 이미 Max 2048·무압축·Mipmap 비활성·Bilinear로 원본을 보존하고 있었으므로, 가는 지도선을 제거하고 큰 8방향 금속 면과 단순 대륙 실루엣으로 정리한 512×486 `turn_followup_compass_v2.png`을 제작해 교체함. 기존 V1은 `Assets/TrashAsset/UI/Generated`로 이동했으며 MCP 프리팹 재생성, UGUI 이행 EditMode 39/39, Console Error 0건을 확인함.

- 2026-08-17: `WIAdministrationTurnFollowupUGUI`를 의회 후보 카드 8개 재사용 화면에서 턴 후속 전용 UI로 개편함. ImageGen으로 무문자 냉색 금속 프레임, 정보 패널, 지도·나침반 장식, 완료 체크 배지 4종을 제작하고 과도한 투명 여백과 밝은 배경을 정리해 적용함. 튜토리얼 모드에서만 `다음 턴 준비` 체크 영역을 표시하며 처리 중·일반 안내·캠페인 결과는 같은 프레임의 설명 패널과 선택 버튼만 재사용함. 주요 선택은 우측 `button_primary`, 보조 선택은 좌측 `button_normal`에 연결하고 고정 버튼 배열을 실제 기능 수인 2개로 축소함. MCP로 프리팹과 MainScene 배치를 재생성했고 UGUI 이행 EditMode 39/39 통과 및 컴파일 오류 0건을 확인함.

- 2026-08-16: 목표 상세 화면의 `목표 확인` 버튼, 진행도 트랙·Fill, G/M/I 보상 스트립을 참고 이미지 기반 전용 Sprite 4종으로 제작해 `WIAdministrationObjectiveUGUI`에 적용함. 생성 이미지의 투명 여백을 정리하고 Sprite/Single·무압축·Mipmap 비활성으로 임포트했으며, 진행률은 Fill Amount로 표시하고 세 보상 값은 아이콘 프레임 위 독립 TMP 라벨로 바인딩함. MCP로 프리팹과 MainScene 배치를 재생성했고 UGUI 이행 EditMode 39/39 및 Console Error 0건을 확인함.

- 2026-08-16: `WIAdministrationObjectiveUGUI`를 캠페인 타이틀 양피지 재사용 화면에서 목표 상세 전용 UI로 개편함. ImageGen으로 무문자 청회색 금속 프레임 `objective_modal_frame_v1.png`을 제작해 적용하고, 현재 상황·달성 조건·진행 상황·보상 구획과 진행도 Fill, 주요 확인 버튼을 고정 UGUI로 배치함. 기존 영웅 배치 복제 프리팹에 남아 있던 후보 카드·페이지 이동 오브젝트를 제거해 19개 오브젝트로 정리했으며 Snapshot을 진행 문자열·보상 문자열·정규화 진행도로 분리함. MCP로 빌더를 실행해 프리팹과 MainScene 참조를 갱신했고 UGUI 이행 EditMode 39/39, Console Error 0건을 확인함.

- 2026-08-16: 캠페인 선택 UGUI의 `VariantCard_0~2`와 `ContinueCampaignButton`이 Single Sprite 전환 후에도 구형 `button_normal_0` 서브 에셋 ID를 참조하던 문제를 수정함. 모든 일반 버튼 참조를 `button_normal` 메인 Sprite로 통일하고 난이도·시작 조건 카드는 선택 시 `button_primary`, 해제 시 `button_normal`로 실제 Sprite를 교체하도록 변경함. 이미지 Tint는 흰색으로 유지해 원본 색을 보존하며 플레이 캡처에서 선택 카드·새 캠페인 버튼은 청색 주요 이미지, 미선택 카드·이어하기 버튼은 흑청색 일반 이미지로 확인함. UGUI 이행 EditMode 39/39 및 Console Error 0건 통과.

- 2026-08-16: 플레이마다 새로 생성되어 수동 Scene Picking 설정이 유지되지 않는 `~~~UGUI(Clone)` 문제를 자동화함. `WIUIScreenManager`가 각 화면을 인스턴스화한 직후 Editor 전용 `SceneVisibilityManager.DisablePicking(screen, false)`를 호출해 전체 화면 루트만 선택되지 않게 하고 자식 패널·버튼의 피킹은 유지함. Player 빌드에는 포함되지 않으며 회귀 검사에서 Clone 루트 피킹 차단과 자식 피킹 허용을 함께 확인함.

- 2026-08-16: `MainCanvas` 아래 23개 UGUI 프리팹의 컨트롤러 루트는 이벤트 구독을 위해 활성 상태로 유지하면서, 숨겨진 화면의 전체 화면 Canvas가 Scene 선택과 입력을 가로막지 않도록 표시 상태를 분리함. 공통 모달 `Show/Hide`와 월드·영지 `Refresh`가 루트 `Canvas` 및 `GraphicRaycaster`를 내부 표시 상태와 함께 전환함. 캠페인 시작 전 플레이 검증에서 캠페인 선택 Canvas 1개만 활성, 나머지 22개 Canvas 비활성, 전용 EditMode 검사 2/2 및 Console Error 0건을 확인함.

- 2026-08-16: 공통 주요 버튼 `Assets/Resources/UI/Generated/button_primary.png`의 Unity Sprite 9-Slice를 버튼 타입 가이드 기준인 좌우 28px·상하 24px로 적용함. 메인 TextureImporter border와 `button_primary_0` Sprite 메타데이터 border가 모두 동일하게 저장됐음을 확인함.

- 2026-08-15: 플레이 시 `MainCanvas` 아래 캠페인 선택 UI와 각 UGUI 화면이 나타나지 않던 원인을 수정함. `WIUIScreenManager`가 등록된 23개 완성 프리팹을 생성한 직후 루트 전체를 비활성화해 `OnEnable` 이벤트 구독까지 차단하고 있었음. 프리팹 루트는 활성 상태로 유지하고 각 화면 내부의 `contentRoot`·`modalRoot`가 캠페인 및 모달 상태에 따라 표시를 관리하도록 초기화 방식을 바로잡았으며 회귀 테스트 기대값도 활성 상태 기준으로 변경함. 전용 EditMode 검사 1/1 통과, 플레이 시 `WICampaignTitleUGUI(Clone)`과 `Backdrop`·`CampaignPanel`의 활성 계층 및 Console Error 0건을 확인함.

- 2026-08-15: R&D 잔여물 정리 후 Unity 게임 검증을 수행함. 전체 EditMode 281개 중 272개 통과·9개 실패로, 아레스 현재 Sprite 경로 및 정리 대상 관련 검사는 통과함. 실패는 숨은 사각 그리드 전환 후 과거 충돌·공격·카메라 기대값이 남은 전투 검사 7개와 현재 조명 강도·전투 HUD 클래스에 맞지 않는 UI 검사 2개임. PlayMode 진입 시 `MainScene`의 `WIAdministrationUIController`와 Canvas 23개가 활성 생성됐고 게임 코드 오류는 0건이었음. 등록된 실질 PlayMode 테스트 케이스는 0개라 자동 플레이 동선 검증 범위는 제한됨.

- 2026-08-15: R&D 잔여물 정리를 추가 진행함. 미참조 복구 씬과 비어 있는 전장 실험 폴더, CharacterScaleArena V2~V5의 생성 스크립트·리포트를 삭제함. QA 스크린샷, 아레스·중세 검사 미사용 후보, 생성 파이프라인 원본, 미사용 UI 이미지, 구형 외곽선 셰이더·재질은 아트와 작업 기록을 보존하도록 `Assets/TrashAsset`으로 이동함. 현재 전투 Sprite 경로에 맞게 UGUI 마이그레이션 테스트 기대값과 전투 데이터 문서를 갱신함. 프로젝트 내부 `.tools/sprite-gen-venv`는 사용자 확인 대상으로 이번 정리에서 제외함.

- 2026-08-15: Unity 상단 `ProjectWI`·`WI` 메뉴를 실제 `MenuItem` 선언과 호출 관계 기준으로 전수 점검함. 현재 ScriptableObject에 결과가 확정됐거나 최신 값을 덮어쓸 위험이 있는 데이터·이미지 일회성 시더 10개 파일과 중복 Verification 메뉴를 제거함. Character Data Viewer가 직접 호출하는 500명 결정론적 로스터 시더, UGUI Prefab Build, 화면 QA, 전투·캠페인 테스트 랩과 성능 벤치마크는 유지함. `UnityToolMenuManual.md`를 남은 각 메뉴의 실행 모드, 처리 단계, 변경 대상, 출력, 위험과 권장 용도가 드러나도록 전면 개정함.

- 2026-08-15: 전투 아트 반복 작업에서 남은 미사용 코드를 정리함. 과거 V1~V5 및 요새 청크 실험 전장만 생성하던 `WIBattleGroundTextureBaker`, 구형 단일 배경을 다시 연결하던 `WIBattleVisualAssetSeeder`, 전체 인물에게 아레스 Sprite를 일괄 배정·해제하던 테스트 메뉴를 제거함. 현재 설정에서 참조되지 않는 전장 프리팹 10개를 삭제하고, 전용 구버전·실험 이미지 61개는 GUID와 상대 폴더 구조를 유지해 `Assets/TrashAsset/Art/Battle`로 이동함. 현재 V6 프리팹·청크·마스터·ImageGen 원본과 공용 환경물은 유지함.

- 2026-08-15: 납작 육각 좌표망에서 화면 정면 상·하 이동이 나오지 않는 문제를 반영해 숨은 사각 셀의 8방향 이동으로 교체함. `WIHiddenBattleGrid`는 폭 0.6·높이 0.3의 셀 좌표↔월드 좌표 변환, 상하좌우와 네 대각선 이웃, 최근접 빈 셀 탐색을 제공함. 초기 진형·점유·목적지 예약·연속 보간은 유지하고 기존 육각 구현과 테스트는 제거함. 근접 캐릭터는 목표 주변 여덟 셀 중 비어 있고 자신에게 가까운 공격 위치로 접근한 뒤 기존 월드 거리로 타격하며, 원거리 투사체와 광역 스킬도 월드 판정을 유지함. 좌표 왕복·8방향·60개 중복 없는 배정 EditMode 테스트 3/3 통과 및 Unity Console Error 0건을 확인함.

- 2026-08-15: 실시간 집단 전투의 공간 판정을 배경 위에 보이지 않는 납작 육각 좌표망으로 전환하는 1차 구현을 적용함. `WIFlatHexGrid`에 축 좌표↔압축 월드 좌표 변환, 6방향 이웃, 전장 내부의 최근접 빈 셀 탐색을 추가하고 `WI_BattleConfig`에 `Use Hidden Hex Grid=true`, Cell Width 0.68, Vertical Scale 0.52, Arrival Distance 0.015를 저장함. 30대30 초기 진형은 약 0.306유닛 행 간격의 셀에 중복 없이 배치하며 캐릭터 몸체는 겹치고 발 위치만 분리됨. 실시간 이동은 인접 셀 목적지를 예약한 뒤 셀 중심 사이를 연속 보간하며, 육각 사용 중 기존 원형 충돌 밀어내기는 비활성화함. 좌표 왕복·압축 투영·60개 중복 없는 배정 EditMode 테스트 3/3 통과 및 Unity Console Error 0건을 확인함.

- 2026-08-15: 런타임 외곽선 셰이더의 알파 노이즈와 확대 시 울퉁불퉁한 경계를 피하기 위해 `Ares_Battle_1WU_A_OutlineBake_V1.png`을 제작함. 캐릭터 내부 구멍은 제외하고 바깥 배경과 연결된 실루엣에만 중립 청흑색 `#10141A`을 84%→50%→20% 3단계로 확장했으며, 외곽선까지 포함한 최종 높이를 256px로 재정규화해 256 PPU·Scale 1의 정확한 1유닛 기준을 유지함. 전체 500명 테스트 데이터에 새 Sprite를 연결하고 A/B/C 모두 일반 Unlit 머티리얼을 사용하도록 셰이더 외곽선을 비활성화함. Unity 컴파일 및 Console Error 0건을 확인함.

- 2026-08-15: 다른 캐릭터도 동일하게 제작할 수 있도록 `BattleSceneAssetSettingsGuide.md`에 고해상도 원화를 1유닛 전투 Sprite로 변환하는 1차 표준을 기록함. 알파 경계 계산, 투명 여백 제거, 종횡비 유지, Premultiplied Alpha Lanczos 축소, 256px 가시 높이, 256 PPU·Scale 1 계산식, 파일명, Unity 임포트 설정, Pivot·A/B/C 검증 절차를 명시함. 과거 1178 PPU 원화 축소 기준과 Strong 외곽선 임시 상태도 현재 구현에 맞게 정정함.

- 2026-08-15: A 근거리 줌 기준 아레스의 큰 원화를 PPU로만 축소하던 방식을 중단하고 `Ares_Battle_1WU_A_V1.png` 전투용 Sprite를 추가함. 원본 1121×1403에서 실제 알파 경계 865×1176을 추출한 뒤 premultiplied-alpha Lanczos로 188×256에 축소해 가시 높이를 정확히 256px로 맞춤. Unity는 256 PPU, Transform Scale 1이므로 가시 캐릭터 높이는 정확히 1월드 유닛이며 Bilinear, Mipmap 활성, 무압축을 적용함. 전체 500명 테스트 데이터의 전투 Sprite를 새 파일로 교체하고, A·B 기본 Unlit/C 기존 외곽선 조합으로 강한 외곽선 실험을 해제함. 컴파일 및 Console Error 0건을 확인함.

- 2026-08-15: 기존 아레스 전투 Sprite를 교체하지 않고 시안의 진한 실루엣을 비교하기 위한 `WI_BattleCharacter_UnlitOutlineStrong.mat`을 추가함. 기존 외곽선 머티리얼은 보존하고 Strong 후보는 짙은 중립 청흑색 `(0.02, 0.024, 0.03, 0.98)`, 20 source texel, Softness 0.3으로 설정해 A/B/C 전 단계에 임시 적용함. 또한 파일만 존재하던 `WI_CharacterShadow_Oval_V1.png`을 `WIBattleCharacter.prefab/GroundShadow` SpriteRenderer에 실제 연결하고, 캐릭터 Y 깊이 정렬 바로 뒤(`character sorting order - 1`)를 따라가도록 구성함. Unity 컴파일 및 Console Error 0건을 확인함.

- 2026-08-15: V4 전장의 천막 두 종류에 URP 실시간 그림자 대신 독립 접지 그림자 Sprite를 적용함. `Tent_1_Shadow_V1.png`, `Tent_3_Shadow_V1.png`을 384×192 RGBA로 제작하고 중립 흑회색, 최대 알파 약 20%, 짧은 방향성의 부드러운 천막 바닥 실루엣으로 정규화함. 각 천막의 `GroundShadow` 자식 SpriteRenderer에 연결해 지면 청크(-110)와 천막(-99) 사이 Sorting Order -105를 사용하며, 천막 이동·배율을 따라가면서 자식 Scale과 알파는 독립 조정할 수 있게 구성함. V4 지면 합성 미리보기와 Prefab 계층 15개를 확인했으며 Unity Console Error 0건을 확인함.

- 2026-08-15: V4 지면과 환경물 대비 통일의 첫 단계로 `Tent_1_NeutralDay_V2.png`, `Tent_3_NeutralDay_V2.png`을 제작해 현재 `WIBattleCharacterScaleArenaV4.prefab`의 좌우 천막에 연결함. ImageGen 편집 후보에서 중립 회갈색 캔버스 기준을 잡은 뒤, 체크무늬가 굽혀진 생성 배경은 사용하지 않고 기존 원본의 RGB·알파·실루엣을 보존하는 색보정으로 최종 에셋을 제작함. 흰 캔버스 최고 밝기와 채도를 낮추고 넓은 반투명 스튜디오 후광을 제거했으며 위치·Scale 0.48은 유지함. Prefab Stage 전체 미리보기와 Unity Console Error 0건을 확인함.

- 2026-08-15: 모든 전투 캐릭터가 공용으로 사용할 수 있는 256×128 투명 타원형 접지 그림자 `WI_CharacterShadow_Oval_V1.png`을 제작함. 중립 흑회색 RGB와 중심 최대 알파 약 51%를 사용하고 방향성 꼬리 없이 대칭으로 부드럽게 사라지도록 구성함. Unity Sprite/Single, 256 PPU, Bilinear, Mipmap 비활성, Clamp, 무압축, 물리 Shape 비활성으로 설정했으며 이번 단계에서는 캐릭터 프리팹에 자동 부착하지 않음.

- 2026-08-15: 전투 배경 후속 작업으로 `Battle_GroundLayered_CharacterScale_V4_4K`을 제작함. V3의 상단 진입로와 좌우 하단 분기 구도를 유지하면서 중앙 집결지는 저밀도 다져진 흙으로 정돈하고, 길 가장자리부터 외곽으로 갈수록 풀·자갈·석재 밀도가 높아지는 명암·디테일 계층을 적용함. 노란 조명과 방향성 그림자가 없는 중립 대낮 Unlit 알베도로 생성했으며 3840×2160 마스터를 1920×1080 네 청크로 분할해 재조립 픽셀 완전 일치를 확인함. `WIBattleCharacterScaleArenaV4.prefab`은 V3 환경물 축척과 배치를 유지하고 지면만 V4로 교체했으며 현재 `WI_BattleConfig.arenaPrefab`에 연결함. Prefab Stage 전체 미리보기와 Unity Console Error 0건을 확인함.

- 2026-08-15: 전투 카메라의 연속 휠 줌을 A(근거리 6)·B(중거리 8)·C(원거리 10) 세 단계로 변경함. 마우스 휠 한 번마다 인접 단계만 이동하고 Home은 참가자 진형을 포함하는 단계로 복원함. 캐릭터 프리팹 기본 머티리얼은 일반 Unlit로 복구하고 C에서만 공용 외곽선 머티리얼로 전환함. 외곽선 셰이더는 안쪽·바깥쪽 알파 샘플과 `fwidth` 기반 `smoothstep`으로 경계 그라데이션과 화면 공간 안티앨리어싱을 보강함. A/B에서는 외곽선 샘플 비용이 발생하지 않으며 컴파일과 Unity Console Error 0건을 확인함. 플레이 모드 수동 검증용 A/B/C 메뉴도 추가함.

- 2026-08-15: 시안처럼 지면 위 캐릭터 실루엣을 분리하기 위해 URP 2D 단일 패스 `ProjectWI/Battle/Sprite Unlit Outline` 셰이더와 `WI_BattleCharacter_UnlitOutline.mat`을 제작해 `WIBattleCharacter.prefab`에 연결함. 원본 알파 주변 8방향을 두 거리에서 샘플링하며 추가 Sprite 복제 없이 Unlit 본체와 짙은 청회색 외곽선을 함께 출력함. 아레스 600 PPU와 줌 6~10을 기준으로 12 source texel과 20 source texel을 30대30 캡처로 비교하고 기본값을 16으로 확정함. Y 기반 깊이 정렬과 텍스트·HP 비활성 설정은 유지했으며 최종 Unity Console Error 0건을 확인함.

- 2026-08-15: V2 지면의 길 구도를 유지하면서 중립 회갈색 중간톤과 작은 자갈·잔디, 길 가장자리 대비를 강화한 `Battle_GroundLayered_CharacterScale_V3_4K`을 ImageGen 편집으로 제작. 3840×2160 마스터를 1920×1080 네 청크로 기계 분할해 재조립 픽셀 완전 일치를 확인하고 `WIBattleCharacterScaleArenaV3.prefab` 및 현재 `WI_BattleConfig`에 연결함. 캐릭터 SpriteRenderer는 `1000 - RoundToInt(worldY × 100)`으로 갱신해 화면 아래쪽 캐릭터가 앞에 표시되도록 변경했으며 런타임에서 Y -6 캐릭터 1600, Y -1.2 캐릭터 1120을 확인함. 30대30 캡처 `Battle_30v30_V3_YDepth.png`를 저장했고 HUD 전환 시 null 방어를 보완한 뒤 Unity Console Error 0건을 확인함.

- 2026-08-15: 맵 그래픽과 60명 캐릭터 실루엣을 방해하지 않도록 `WI_BattleConfig`에 `showCharacterLabels`, `showCharacterHealthBars` 표시 설정을 추가하고 모두 비활성화. `WIBattleCharacterView`는 설정이 켜졌을 때만 이름·등급·역할 TextMesh와 HP 바를 생성하도록 변경함. 30대30 재실행 캡처 `Battle_30v30_NoLabelsNoHealth_V2.png`에서 전장 표기가 제거되고 캐릭터와 V2 지면만 표시되는 것을 확인했으며 Unity Console Error 0건을 확인함. 청크 경계와 확대 선명도는 양호하고, 다음 맵 작업은 밝은 지면·석재·천막 사이의 중간 명암층과 길 가장자리 대비 조정으로 확정함.

- 2026-08-15: 캐릭터 약 2유닛 축척용 지면을 3840×2160 `Battle_GroundLayered_CharacterScale_V2_4K` 마스터로 고해상도화. V1의 길·집결지 구도와 중립 색을 유지하면서 기존 4K 중립 지면의 고주파 자갈·흙·잔디 질감을 30% 합성하고, 1920×1080 네 청크로 재샘플링 없이 분할해 재조립 픽셀 완전 일치를 확인함. 네 청크를 106.6667 PPU·Bilinear·Mipmap 비활성·무압축·Clamp 및 `WI_BattleGround_Unlit`으로 설정한 `WIBattleCharacterScaleArenaV2.prefab`을 제작해 현재 `WI_BattleConfig`에 연결. 30대30 실행에서 전체 36×20.25 지면의 청크 경계와 확대 선명도를 확인했으며 최종 Unity Console Error 0건을 확인함.

**마지막 갱신:** 2026-08-14

전체 개발 순서와 현재 체크 상태는 프로젝트 루트의 `PlanChecklist.md`를 기준으로 관리합니다.

## 현재 프로젝트 기준

- 2D 판타지 영토 확장 전략 게임
- 전체 대륙 60개 성
- 5대 진영
- 성 중심 영지 관리 및 영웅 배치
- 월 단위 턴 진행
- 병사 없이 고유 영웅과 일반 인물이 직접 전투단을 구성
- 일반 영웅 클래스 타입 12종과 고유 등급 100명·일반 등급 400명의 현재 검증용 로스터 구축
- **종합 UI 분석 및 설계 문서 구획 완료**: `GameDocuments/ProjectWI_UI_Structure_Document.md`

## 구현 완료

- 2026-08-15: 전투씬 에셋 작업의 단일 기준 문서 `GameDocuments/BattleSceneAssetSettingsGuide.md`를 작성. 현재 캐릭터 약 2유닛 축척, 아레스 600 PPU, 지면 36×20.25·46.6667 PPU, 카메라 6~10, Unlit 지면과 중립 백색 전역광 설정을 기록하고 캐릭터·지면 레이어·환경물·고해상도 청크·노멀맵·URP 2D 라이팅의 제작 및 검증 절차, 노란 조명 금지와 파일 이름 규칙을 정리함. 이후 전투 에셋 변경 시 이 문서를 함께 갱신하는 기준으로 지정함.

- 2026-08-15: 반복 타일만으로 표현하기 어려운 길·잔디 경계·마모 지형을 하나의 2D 레이어 합성 결과로 제작한 `Battle_GroundLayered_CharacterScale_V1.png`를 추가하고 현재 `WIBattleCharacterScaleArenaV1.prefab`의 지면으로 연결. 36×20.25 월드, 캐릭터 약 2유닛 축척을 기준으로 성문에서 중앙 집결지로 이어진 넓은 길과 양측 분기, 다져진 흙·자갈·희박한 풀·석재 파편의 비반복 전이를 구성함. 노란·주황·노을 색감과 방향성 명암·그림자를 배제한 중립 평광 알베도로 제작하고 지면에는 `Sprite-Unlit-Default` 전용 머티리얼을 적용. BattleScene의 방향성 키·필 Light 2D를 비활성화하고 백색 Global Light 2D만 강도 1로 유지했으며 30대30 실행 캡처와 Unity 콘솔 오류 0건을 확인함.

- 2026-08-15: 캐릭터 높이 약 2유닛을 전장 축척 기준으로 삼는 `WIBattleCharacterScaleArenaV1.prefab`을 제작하고 현재 `WI_BattleConfig.arenaPrefab`에 연결. 보이지 않던 14% 알파 `CentralGroundDetail` 중첩 대신 `Battle_GroundTile_Courtyard_V2`를 36×20 크기의 불투명 반복 지면으로 직접 사용하고, 성문·텐트·망루·방책을 캐릭터와 비교 가능한 실제 월드 크기로 재배치함. 전장 크기에 맞춰 최대 줌아웃을 14에서 10으로 복구했으며 30대30 실행 캡처에서 배경 외곽 노출 없이 시설물과 캐릭터 축척이 읽히고 Unity 콘솔 오류 0건을 확인함.

- 2026-08-15: 전장 확대·축소에 맞춘 과도한 캐릭터 보정을 제거하고 아레스 전투 Sprite를 기본 2D 캐릭터 표시 기준으로 복구. 원본 종횡비와 Transform Scale 1, 600 PPU(약 1.45×1.96 월드 단위), 무압축은 유지하고 Mipmap·Preserve Coverage를 끈 Bilinear 샘플링으로 변경했으며 공용 비주얼 재배정 도구도 같은 설정을 사용하도록 동기화함.

- 2026-08-14: 실제 전투 밀도 검증을 반복 실행할 수 있도록 `ProjectWI/Verification/Start 30v30 Battle Density Test` 메뉴를 추가. 아레스·상대 영웅 각 1명과 중복 없는 커먼급 각 29명을 클래스 권장 역할로 편성해 기존 Battle Test Lab과 BattleScene 경로로 실행하며, 실제 런타임 캐릭터 뷰 60개와 참가 상태 60명을 확인하고 최대 줌아웃 HUD 포함·제외 캡처를 저장함. 지면 위 캐릭터 실루엣은 구분되지만 60명의 이름·등급·역할 상시 표기 중첩과 세로로 긴 진형을 다음 가독성 개선 과제로 확인함.

- 2026-08-14: 실제 전투 이미지가 아레스만 준비된 상태에서 30명 이상 전투 밀도를 검증할 수 있도록 `WI_AdministrationDatabase`의 영웅급·일반급 전체 500명 `battleSprite`에 `Ares_Battle_FullBody_V1`을 테스트용으로 임시 배정. `ProjectWI/Data/Assign Ares Battle Sprite To All Characters (Test)`와 원복용 `Clear Shared Ares Battle Sprites (Test)` 메뉴를 추가했으며 데이터 참조 500/500, 빈 참조 0건과 전용 EditMode 2/2 통과, Unity 콘솔 오류·경고 0건을 확인함.

- 2026-08-14: 줌인 품질 검증용 하이브리드 청크 전장 `WIBattleFortressHybridChunkArenaV2.prefab`을 추가하고 현재 `WI_BattleConfig.arenaPrefab`에 연결. 넓은 성채 구도 원본을 재샘플링 없이 2×2 매크로 청크로 분리하고, 중앙 43×21 구역에는 중립 대낮 색감의 1254×1254 반복 지면 디테일을 14% 농도로 겹쳤으며 성문·텐트·망루는 독립 환경 모듈로 유지. 첫 타일의 십자 경계를 확인해 균일한 두 번째 버전으로 교체했고, 기존 기계 분할·개별 재묘사·시안 하이브리드 프리팹은 비교용으로 보존. 캐릭터 Scale 1과 카메라 6~14는 변경하지 않음

- 2026-08-14: 개별 AI 재묘사 청크의 지면 축척·경계 불일치 실험을 대체하는 기계 분할 전장 `WIBattleFortressMechanicalChunkArenaV1.prefab`을 추가하고 현재 설정에 연결. 하나의 중립 대낮 3840×2160 완성본 `Battle_FortressField_V3_4K`을 재샘플링 없이 1920×1080 네 청크로 분할했으며 재결합 결과 원본과 픽셀 단위 완전 일치 확인. 전체 월드 57.024×32.076에서 약 67.34 px/unit을 유지하고, 카메라 이동 제한도 18×10 전투 판정 영역이 아니라 전체 배경 크기를 사용하도록 변경. 캐릭터 Scale 1, 아레스 600 PPU, 줌 6~14는 유지

- 2026-08-14: 전투맵 URP 2D 라이팅 1차 패스를 적용. `BattleScene`의 백색 Global Light 2D를 강도 0.92로 조정하고 노멀맵 반응을 활성화했으며, 전장 중앙에 약한 청백색 키 라이트와 중립색 필 라이트를 추가해 노란·주황 색조 없이 교전 영역을 강조. 현재 배경과 아레스는 Sprite-Lit 머티리얼로 기본 조명을 받으며, 전용 노멀맵과 ShadowCaster2D는 후속 품질 단계로 유지

- 2026-08-14: 시안 화풍의 대형 청크 전장 실험 `WIBattleFortressChunkArenaV1.prefab`을 추가. 단순 업스케일 대신 실제로 더 넓은 성채 안뜰 구도를 새로 생성하고, 4개 구역을 각각 1672×941로 별도 고해상도 재묘사해 총 3344×1882 상당의 2×2 독립 Sprite 청크로 구성. 전체 월드 57.024×32.076에서 청크별 2048 제한 내 원본 해상도를 유지하며, 기존 모듈형·시안 하이브리드 프리팹은 비교용으로 보존. AI 구역 재묘사 특성상 중앙 경계에 약한 명암·구조 변화가 남는 점을 실험 한계로 기록

- 2026-08-14: 최대 줌아웃에서 아레스 전신 Sprite의 세부 픽셀이 뭉치는 현상을 1차 개선. 캐릭터 Scale 1, 600 PPU와 카메라는 유지하고 `Ares_Battle_FullBody_V1`에 Mipmap, Trilinear, Mip Maps Preserve Coverage, Alpha Is Transparency, 무압축 임포트를 적용. 공용 비주얼 배정 도구를 다시 실행해도 동일 설정이 유지되도록 전투 Sprite 분기와 재적용 메뉴를 추가

- 2026-08-14: 여러 전장 표현 방식을 보존하며 비교하기 위해 기존 모듈형 `WIBattleFortressArena.prefab`은 유지하고 `WIBattleFortressConceptHybridArena.prefab`을 추가. 실제 시안에서 UI·캐릭터만 제거한 `Battle_FortressField_V1`을 기본 시야 24×13.5에 완성형 중앙 맵으로 배치하고, 최대 줌아웃 57.024×32.076의 외곽만 고밀도 지면 타일과 독립 환경물로 확장. `WI_BattleConfig.arenaPrefab`은 새 하이브리드 버전을 참조하며 캐릭터 Scale 1과 기존 카메라 6~14 설정은 유지

- 2026-08-14: 모듈형 전장의 1차 중립 회색 지면이 전투 시안보다 비어 있고 저렴하게 보이던 문제를 개선. 시안의 흙·마모 석재·잔디·잔해 밀도를 참고한 중립 대낮의 `Battle_GroundTile_Courtyard_V2`를 제작하고 Repeat/Trilinear/Mipmap/Full Rect 타일로 적용. 성문·텐트·망루·방책은 중앙 교전 공간을 감싸는 화면 가장자리 세트로 크기와 위치를 재조정

- 2026-08-14: 타원형 알파 마스크 전경이 거대한 흐린 패치와 외곽 지면 고리를 만들던 1차 레이어 프리팹을 폐기. 투명 PNG 모듈로 성벽·성문 1종, 텐트 3종, 망루 2종, 방책 3종을 제작하고 `WIBattleFortressArena.prefab`에 지면 타일과 8개 독립 환경 GameObject를 고정 배치. 전경 합성 이미지나 중앙 마스크 없이 전 화면에 동일 지면 타일이 표시됨


- 2026-08-14: V4 지면에도 생성형 이미지 특유의 구불구불한 획이 남아 확대 시 차이가 작았던 문제를 재수정. ImageGen 지면을 사용하지 않고 방향성 없는 Perlin 저주파 색 변화·미세 입자·드문 자갈만 결정론적으로 굽는 `WIBattleGroundTextureBaker`를 추가하고, 중앙 교전 구역을 원본과 섞지 않고 완전히 교체한 `Battle_FortressField_V5_4K`를 적용. 캐릭터·카메라 설정은 유지

- 2026-08-14: 기본 카메라에서 지면의 늘어진 AI 붓 자국이 드러나던 문제를 해결하기 위해 노란·주황·노을 조명을 배제한 중립 대낮의 흙·잔디 지면 에셋 `Battle_Ground_NeutralDay_V1`을 제작. 기존 성벽·텐트 구도를 유지하면서 중앙 교전 구역을 잔잔한 저주파 지면으로 자연스럽게 재합성한 3840×2160 `Battle_FortressField_V4_4K`를 전투 설정에 연결

- 2026-08-14: 확대된 57.024×32.076 전투 배경 범위에 맞춰 카메라 최대 줌아웃을 10에서 14로 확장. 최소 줌 6, 캐릭터 Scale 1, 아레스 600 PPU는 유지하며 마우스 휠과 60인 자동 시점에서 더 넓은 전장을 표시

- 2026-08-14: 전투 배경을 직전 47.52×26.73에서 57.024×32.076으로 다시 20% 확대. 캐릭터 Scale 1과 아레스 600 PPU는 유지

- 2026-08-14: 전투 배경을 직전 39.6×22.275에서 47.52×26.73으로 추가 20% 확대. 캐릭터 Scale 1과 아레스 600 PPU는 그대로 유지

- 2026-08-14: 전투 시안 비율 피드백에 따라 4K 전투 배경의 월드 표시 크기만 36×20.25에서 39.6×22.275로 10% 확대. 캐릭터 Scale 1, 아레스 600 PPU, 카메라와 실제 전투 판정 영역 18×10은 유지

- 2026-08-14: 전투 캐릭터 Transform 배율을 공통 1로 고정하고 이미지 자체의 PPU 규격으로 화면 크기를 관리하도록 정리. 아레스 전투 Sprite는 600 PPU를 사용해 1178px 전신이 약 1.96 월드 단위로 표시되며, 4K V3 배경과 카메라 설정은 유지

- 2026-08-13: 전투 캐릭터가 4K 전장 시안보다 작게 보인다는 피드백을 반영해 공통 `battleSpriteScale`을 0.65에서 0.75로 확대. 배경 36×20.25와 카메라 6~10 범위는 유지

- 2026-08-13: 줌인 시 전투 배경의 낮은 픽셀 밀도가 드러나는 문제를 보완하기 위해 중립 대낮 색감과 세부 지형 묘사를 강화한 `Battle_FortressField_V3_4K.png` 3840×2160 에셋을 제작. 배경 표시 영역을 36×20.25, 최대 줌아웃을 10으로 맞춰 화면과 배경 종횡비를 통일하고, 캐릭터 배율은 시안 비율에 가까운 0.65로 복구

- 2026-08-13: 황갈색·노란 조명이 강한 `Battle_FortressField_V1`은 보존하고, 동일 전장 구도를 맑은 대낮의 중립 회색 석재·밝은 흙·자연 녹색으로 다시 조명한 `Battle_FortressField_V2`를 제작. 전투 판정 영역 18×10은 유지하면서 배경 표시만 42×24로 확장하고, 전투 Sprite 배율을 50%, 최소 카메라 줌을 6으로 조정해 소규모 전투에서도 시안에 가까운 캐릭터 비율을 사용

- 2026-08-13: 전투 일러스트가 전장에 비해 크게 표시되던 문제를 수정. 원본 PNG와 18×10 전장은 유지하고 `WI_BattleConfig.battleSpriteScale` 공통 설정을 추가해 전투 Sprite를 기본 65% 배율로 표시하도록 변경. 향후 모든 캐릭터 전투 이미지에 동일하게 적용되며 플레이스홀더 크기는 기존 값을 유지

- 2026-08-13: 아레스 전투 시안의 흑철·금장 HUD를 재사용 가능한 UI 에셋으로 재제작. 글자·수치·초상·아이콘을 제거한 상단 전투 상태바, 캐릭터 정보 패널, 4칸 명령바, 4칸 스킬바를 각각 투명 PNG로 분리하고 Unity 단일 Sprite·알파 투명·밉맵 비활성 설정 적용. 투명 통합 시트와 재처리용 크로마 원본도 함께 보존

- 2026-08-13: 아레스 30 대 30 전투 시안에서 UI·문자·캐릭터·체력바·선택 표시를 모두 제거하고 가려진 지형을 복원한 `Battle_FortressField_V1.png` 제작. 전투 설정에 `arenaBackground`를 추가해 실제 전투 배경으로 연결하고 Sprite 크기는 런타임 Transform으로 18×10 전장에 맞추며, 배경 미지정 시 기존 절차형 격자로 대체

- 2026-08-13: Unity 상단의 프로젝트 전용 `ProjectWI` 19개와 `WI` 50개 메뉴를 코드 기준으로 전수 조사해 `GameDocuments/UnityToolMenuManual.md` 작성. 데이터 Seed/Assign, UGUI 프리팹 Build, QA Preview, 테스트 랩과 성능 검증의 기능·실행 조건·변경 대상·주의도를 구분해 기록

- 2026-08-12: 최대 30 대 30 전투 가독성을 위해 직교 카메라 최대 줌아웃을 6.2에서 12로 확대하고, 참가자 수와 초기 진형 범위에 따른 자동 시작 배율을 추가. 60명 전투는 최대 줌아웃, 소규모 전투는 인원에 비례한 가까운 시점을 사용하며 마우스 휠 3.5~12 조절과 Home 자동 시점 복원을 유지

- 2026-08-12: 제공된 아레스 원본 PNG가 알파 없는 RGB 크로마키 이미지임을 확인하고 녹색 배경을 제거한 `Ares_Battle_FullBody_V1.png`와 얼굴 중심 `Ares_Portrait_Face_V1.png`를 제작. `WIHeroDefinition.battleSprite`를 추가해 전투에서 아레스만 전신 일러스트를 표시하고 다른 인물은 기존 플레이스홀더를 유지하도록 연결했으며, 아레스 기존 UI 초상화도 새 얼굴 Sprite로 임시 교체. 애니메이션은 미적용

- 2026-08-12: 행정 UGUI 프리팹 로더의 클래스명을 `WIAdministrationUGUIScreenBootstrap`에서 `WIUIScreenManager`로 변경. 기존 스크립트 GUID와 MainScene의 23개 프리팹 참조는 유지

- 2026-08-12: `WIUIScreenManager`가 등록된 UGUI 프리팹을 생성한 직후 루트 오브젝트를 비활성화하도록 변경. 플레이 모드 Hierarchy에서 모든 화면이 동시에 활성화되어 Scene 선택을 방해하지 않으며, 검토할 화면만 수동으로 활성화해 편집 가능

- 2026-08-12: 비전투 런타임의 마지막 UI Toolkit 형식인 `WIMapConnectionLayer`를 제거하고 레거시 월드 UXML의 사용자 정의 태그를 일반 `VisualElement`로 치환. `Assets/Scripts`와 `Assets/Editor`의 비전투 C# 기준 `UnityEngine.UIElements`, `UIDocument`, `VisualTreeAsset`, `PanelSettings` 참조 0건을 확인했으며 UGUI 이행+레이아웃 EditMode 검사 65/65 및 Unity 콘솔 오류 0건 통과. 전투 테스트용 UI Toolkit은 사용자 요청 범위에 따라 유지

- 2026-08-12: 행정 런타임 컨트롤러의 공통 UI Toolkit 표시 기반을 완전히 제거. `WIAdministrationUIController`와 World/Territory/Turn partial에서 `UIDocument`, `VisualElement`, `CreateModal`, 레거시 HUD·지도 갱신 및 UI Toolkit 전용 QA 화면 코드를 삭제하고 캠페인 상태·선택 성·UGUI 이벤트/스냅샷 브리지와 공용 표시 헬퍼만 유지. 기존 레이아웃 검사를 UGUI 프리팹 기준으로 전환했으며 UGUI 이행+레이아웃 EditMode 검사 65/65 통과

- 2026-08-12: 턴 처리 계열의 레거시 UI Toolkit 구현을 `WIAdministrationUIController.Turn.cs`에서 제거. 이전 턴 연산/요약, 전투 진입, 캠페인 결과와 튜토리얼 모달 코드를 삭제하고 `BeginTurn`과 UGUI 전용 코루틴만 유지했으며, 월간 보고 레이아웃 회귀 검사도 UGUI 프리팹의 스크롤 본문과 6개 고정 행동 슬롯을 검사하도록 전환. UGUI 이행 및 관련 검사 37/37 통과

- 2026-08-12: 성 상세 기록과 특화 시설 선택의 레거시 UI Toolkit 모달을 `WIAdministrationUIController.Territory.cs`에서 제거하고 숨은 영지 버튼을 `UGUICastleRecordRequested`, `UGUISpecialFacilityRequested` 이벤트로 직접 연결. UGUI 성 기록 스냅샷이 공유하는 전문 분야 설명과 차기 공통 레거시 정리 전까지 필요한 기존 슬롯 표시 헬퍼만 유지했으며 UGUI 이행 EditMode 검사 35/35 통과

- 2026-08-12: 월간 보고·영지관 위임·선택 사건 계열의 레거시 UI Toolkit 구현을 `WIAdministrationUIController.RealmReports.cs`에서 제거. 월간 보고, 위임 설정, 사업·관계·지역·점령·영입 사건과 영웅의 흔적 모달 및 중복 실행 코드를 삭제하고 숨은 버튼·전투 알림·L 단축키를 UGUI 이벤트로 직접 연결했으며, 해당 partial에는 UGUI 카드 표시용 조건·방침 문자열 헬퍼만 유지. UGUI 이행 검사 34개와 미결 전투 차단 검사까지 총 35/35 통과

- 2026-08-12: 외교·첩보·진영 정세 계열의 레거시 UI Toolkit 구현을 `WIAdministrationUIController.RealmRelations.cs`에서 제거. 외교 대상/명령, 첩보 종류/담당자/대상 성/대상 인물과 진영 정세 모달 생성 코드를 삭제하고 숨은 버튼·단축키를 UGUI 이벤트로 직접 연결했으며, 해당 partial에는 UGUI 카드 표시용 충성·외교 상태·관계 설명·AI 성향 헬퍼만 유지. UGUI 이행 EditMode 검사 33/33 통과

- 2026-08-12: 통치 계열의 레거시 UI Toolkit 구현을 `WIAdministrationUIController.RealmGovernance.cs`에서 제거. 중점 사업·연구·의회·시스템 모달과 저장/불러오기 UI 생성 코드를 삭제하고 UGUI 버튼·단축키 이벤트로 직접 연결했으며, 해당 partial에는 UGUI 브리지가 사용하는 사업 배정 검증과 표시 헬퍼만 유지. UGUI 이행 EditMode 검사 32/32 통과

- 2026-08-11: 인물·시설 계열의 레거시 UI Toolkit 모달 구현을 `WIAdministrationUIController.Characters.cs`에서 제거. 해당 partial에는 UGUI 표시용 등급·특기 문자열 헬퍼만 유지하고, 영웅 배치·인재 활동·기본 시설·영웅 목록 버튼 및 H 단축키는 UGUI 이벤트로 직접 연결함. 레거시 진입점 부재 회귀 검사를 추가해 UGUI 이행 EditMode 검사 31/31 통과

- 2026-08-10: 캠페인 타이틀 화면을 검증용 UGUI 프리팹 `WICampaignTitleUGUI.prefab`으로 병렬 이식. 1920×1080 기준 Canvas Scaler, 고정 RectTransform 난이도·시작 조건 카드, 기존 PNG 버튼과 패널, 정적 Noto CJK KR 원본 기반 Dynamic TMP 폰트, 새 캠페인·자동 저장 기존 로직 연결을 적용했으며 MainScene의 기존 EventSystem을 공유하도록 구성
- 2026-08-10: UI Toolkit 실제 게임 연결 코드를 화면별 UGUI 컨트롤러로 이전하고 기존 거대 컨트롤러를 단계적으로 해체하는 `GameDocuments/UGUIMigrationPlan.md` 작성. 타이틀 화면부터 프리팹 구조 검증 테스트를 추가
- 2026-08-10: 월드 전략 화면 1차 UGUI 이식. `WIAdministrationWorldUGUI.prefab`에 상단 자원 HUD, 좌측 대표 영지, 중앙 대륙 지도, 우측 목표·전투 알림, 하단 명령 바를 고정 배치하고 `WIAdministrationWorldUGUIController`와 읽기 전용 스냅샷 브리지로 실제 캠페인 상태 및 기존 게임 명령을 연결
- 2026-08-11: 월드 지도 UGUI 이식. ScriptableObject의 정규화 좌표를 사용해 60개 성 노드를 `WIAdministrationWorldUGUI.prefab`에 고정 배치하고, `WIAdministrationMapUGUIController`에서 성 이름·소유 진영 마커·선택 상태를 갱신하며 기존 영지 상세 흐름으로 연결
- 2026-08-11: 영지 상세 화면 1차 UGUI 이식. `WIAdministrationTerritoryUGUI.prefab`에 성 정보·영지관·4종 능력치·이번 달 중점·영웅 8칸·특화 시설 2칸·영지 명령 8개를 고정 배치하고, 전용 스냅샷과 `WIAdministrationTerritoryUGUIController`로 표시와 입력을 분리
- 2026-08-11: 아직 이식되지 않은 기존 UI Toolkit 모달을 호출할 때 UGUI 화면을 일시 중단하고 모달 종료 시 복원하는 이행 처리를 추가
- 2026-08-11: 재사용 가능한 `WIAdministrationModalUGUIController` 기반 공통 모달 프레임과 중점 사업 UGUI 이식. 8종 사업의 기본/집중 비용, 대기 담당 영웅, 예상 성과와 특기 보너스를 기존 계산 시스템에서 읽고 실제 사업 배정 로직에 연결
- 2026-08-11: 영웅 배치 UGUI 이식. 공통 모달 프레임과 8개 고정 후보 카드·페이지 전환을 적용하고, 영입·미배치·대기 상태를 만족하는 인물을 기존 캠페인 상태에서 조회해 실제 선택 성 배치 기능에 연결
- 2026-08-11: 인재 활동 UGUI 이식. 활동 인물·활동 종류·교류/영입 대상의 3단계를 공통 모달과 고정 카드로 분리하고, 기존 바쁨·부상·명성·발견 상태 검증 및 월말 개인 활동 처리 데이터에 직접 연결
- 2026-08-11: 특화 시설 선택 UGUI 이식. 확장 완료 선택 권한과 시설 슬롯을 검증하고, 기존 8종 시설 중 미보유 시설을 고정 카드로 표시해 성의 `SpecialFacilityIds`에 연결
- 2026-08-11: 기본 시설·선술집 의뢰 UGUI 이식. 성관·시장·훈련소·선술집 안내와 월간 의뢰·담당 인물 선택 단계를 분리하고, 기존 의뢰 적성 계산·상태·기간 데이터에 연결
- 2026-08-11: 영지관 위임 UGUI 이식. 주둔 인물 임명·해임, 운영 방침 5종, 기본·집중 예산, 위임·직접 관리 전환과 기존 자동 사업 예상 결과를 단일 설정 화면에 연결
- 2026-08-11: 영지 원정 UGUI 이식. 주둔 전투단·새 전투단 대장·인접 이동/원정 목표 선택을 고정 카드 단계로 분리하고, 기존 편성·교전 관계·영향력 20·이동 상태 검증에 연결
- 2026-08-11: 성 상세 기록 UGUI 이식. 고정 성 이미지와 스크롤 본문에 규모·지형·특산·수치·주둔·시설·영웅 흔적·전투단 기록을 표시하고 기존 첩보 정보 공개 제한을 유지
- 2026-08-11: 전역 우측 캠페인 목표 상세 팝업 UGUI 이식. 시작 정세·목표 조건·진행도·금화/마나/영향력 보상을 고정 섹션으로 분리하고 기존 목표 시스템 스냅샷에 연결
- 2026-08-11: 월간 보고 UGUI 이식. 자원 수입·지출, 위임 결과, AI 판단 근거와 뉴스를 스크롤 본문에 표시하고 선택 사건·미결 전투를 페이지 가능한 6개 고정 행동 슬롯에 연결
- 2026-08-11: 전역 군사 메뉴 UGUI 이식. 진행 중인 전투·전투단 목록, 전투 상세·시작, 편성 성·대장 선택, 역할별 단원 추가·제외, 합동 훈련, 해산, 인접 성 이동·원정을 하나의 8개 고정 카드 단계 화면으로 연결. 군사 흐름에서 기존 UI Toolkit 모달 호출을 제거했으며 UGUI 이행 EditMode 검사 16/16 통과
- 2026-08-11: 전역 영웅 메뉴 UGUI 이식. 영입 영웅과 발견 인재를 8개 고정 카드·페이지로 표시하고 영웅별 공훈·명성·작위·활동 상태, 일반 인물 영웅 승격, 작위 수여·변경을 같은 프리팹의 단계 화면으로 연결. 영웅 전역 흐름의 기존 UI Toolkit 모달 호출을 제거했으며 UGUI 이행 EditMode 검사 17/17 통과
- 2026-08-11: `MainScene` 루트에 나열되던 15개 UGUI 프리팹 인스턴스를 `UGUI Screens (Prefab Instances)` 정리 루트 아래로 이동. 공통 `WIAdministrationUGUISceneUtility`를 추가해 이후 각 UGUI 빌더를 다시 실행해도 생성 인스턴스가 같은 루트 아래에 자동 배치되도록 통일
- 2026-08-11: 전역 외교 메뉴 UGUI 이식. 존속 진영과 관계 상태, 포로 몸값·맞교환, 휴전·친선 사절, 불가침·동맹, 금화 원조·공동 공격, 선전포고를 페이지 가능한 8개 고정 카드 단계로 연결. 기존 외교 UI Toolkit 모달 호출을 제거하고 정리 루트에 전용 프리팹을 배치했으며 UGUI 이행 EditMode 검사 18/18 통과
- 2026-08-11: 전역 첩보 메뉴 UGUI 이식. 진행 중 임무, 첩보 종류·담당 인물·대상 성·이간 대상 인물의 4단계 선택을 페이지 가능한 8개 고정 카드로 연결. 영향력·바쁨·정보 공개·질서·방첩·지력 기반 성공률 규칙과 기존 `WISchemeSystem` 예약 로직을 유지하고 UI Toolkit 첩보 모달 호출을 제거했으며 UGUI 이행 EditMode 검사 19/19 통과
- 2026-08-11: 전역 연구 메뉴 UGUI 이식. 완료·진행 중·미완료 연구, 선행 연구·최고 기술·마나 조건, 지력 순 대기 담당자 선택과 연구 시작을 페이지 가능한 8개 고정 카드 단계로 연결. 기존 `BeginResearch`와 연구 튜토리얼 완료 처리를 유지하고 UI Toolkit 연구 모달 호출을 제거했으며 UGUI 이행 EditMode 검사 20/20 통과
- 2026-08-11: 전역 진영 정세 메뉴 UGUI 이식. 대륙 5대 진영의 영토·AI 성향·존속/멸망·플레이어와의 외교 상태·주요 인물 관계를 읽기 전용 고정 카드로 표시하고 플레이어 진영에만 자원과 방침을 공개. 기존 UI Toolkit 진영 정세 모달 호출을 제거했으며 UGUI 이행 EditMode 검사 21/21 통과
- 2026-08-11: 전역 의회 메뉴 UGUI 이식. 부국·개발·안정·수비·원정·인재 6개 월간 진영 방침과 효과를 고정 카드로 표시하고 선택 결과를 플레이어 진영의 기존 `FactionPolicy` 및 월말 사업 성과 계산에 연결. 기존 UI Toolkit 진영 방침 모달 호출을 제거했으며 전역 하단 메뉴의 주요 화면 이식을 완료하고 UGUI 이행 EditMode 검사 22/22 통과
- 2026-08-11: 월간 선택 사건 공통 UGUI 이식. 사업·인물 관계·지역·점령 통치·영입 사건과 영웅의 흔적 기록/교체를 하나의 8개 고정 카드 프리팹으로 구성하고 ScriptableObject 선택 조건 및 기존 사건 판정 API에 연결. 월간 보고 사건 버튼의 UI Toolkit 모달 호출과 UGUI 일시 중단 처리를 제거했으며 UGUI 이행 EditMode 검사 23/23 통과
- 2026-08-11: 다음 턴 후속 흐름 UGUI 이식. 연산 안내 후 기존 턴 계산을 실행하고 월간 보고 UGUI를 표시하며, 보고를 정상 종료하면 캠페인 승패 결과 또는 현재 월의 첫해 튜토리얼을 공통 고정 카드 프리팹으로 이어서 표시. 결과 확인·타이틀 복귀·튜토리얼 확인/전체 건너뛰기를 기존 저장 상태에 연결했으며 UGUI 이행 EditMode 검사 24/24 통과
- 2026-08-11: 새 캠페인·자동 저장 진입 후속 흐름 UGUI 이식. 새 캠페인은 기존 목표 UGUI를 연 뒤 정상 종료 시 첫해 튜토리얼 UGUI로 이어지고, 자동 저장은 미확인 캠페인 결과를 우선한 뒤 튜토리얼을 표시하도록 연결. 목표 없음·불러오기 실패/경고도 공통 UGUI 안내로 교체해 캠페인 시작 경로의 UI Toolkit 모달 호출을 제거했으며 UGUI 이행 EditMode 검사 24/24 통과
- 2026-08-11: 인물 이동 UGUI 이식. 인재 활동 프리팹에 여섯 번째 `인접 성 이동` 고정 버튼과 목적지 카드 단계를 추가하고 같은 진영 인접 경로, 실제 주둔 인원, 예약 이동 슬롯, 영지관·점유 상태를 기존 `StartCharacterTransfer` 규칙으로 검증. 성공 안내를 공통 UGUI 메시지로 표시하고 기존 UI Toolkit 인물 이동 모달 구현을 제거했으며 UGUI 이행 EditMode 검사 24/24 통과
- 2026-08-11: 공통 오류·안내 메시지 UGUI 통일. 거대 컨트롤러의 `ShowMessage`가 UI Toolkit `VisualElement` 모달을 생성하던 구현을 제거하고 UID 번역 또는 직접 작성 문장을 기존 턴 후속 공통 UGUI 프리팹의 Message 모드로 전달하도록 변경. 기존 27개 안내 호출을 일괄 전환했으며 UGUI 이행 EditMode 검사 24/24 및 플레이 모드 표시 확인 완료
- 2026-08-11: 시스템 설정·저장/불러오기 UGUI 이식. 언어·전체 화면·전체/음악/효과음 음량·30/60/120 FPS 설정과 적용·초기화를 첫 페이지에, 수동 슬롯 1~3 저장/불러오기와 자동 저장 불러오기를 두 번째 페이지에 고정 8카드로 구성. 기존 설정 서비스와 캠페인 저장 API를 유지하고 월드 하단 고정 설정 버튼 및 기존 UI Toolkit 시스템 버튼을 새 화면에 연결했으며 UGUI 이행 EditMode 검사 25/25와 플레이 모드 오류 0건 확인 완료
- 2026-08-11: 전역 키보드 단축키 UGUI 이식. M/H/D/S/R/G/C/L/T 명령을 숨은 UI Toolkit `rootVisualElement` 포커스에서 `WIAdministrationWorldUGUIController`의 Input System 입력으로 이전하고, UGUI 모달 표시 중 전역 명령 차단과 Escape 최상위 모달 닫기를 공통 모달 컨트롤러에 추가. 기존 UI Toolkit 키 이벤트 등록을 해제했으며 UGUI 이행 EditMode 검사 25/25와 컴파일 오류 0건 확인 완료
- 2026-08-11: 미결 플레이어 전투가 있는 저장 상태의 MainScene 초기 진입을 UGUI로 이식. 행정 컨트롤러 `Awake()`에서 UI Toolkit 월간 보고 모달을 직접 만들던 호출을 제거하고, 모든 프리팹 구독이 완료된 다음 프레임에 월간 보고 UGUI를 요청하도록 변경
- 2026-08-11: 캠페인 시작 화면의 숨은 UI Toolkit 동적 생성을 제거. 거대 행정 컨트롤러가 UXML 난이도·시작 조건 버튼을 런타임에 생성하고 선택 딕셔너리를 관리하던 코드를 삭제하고, 고정 `WICampaignTitleUGUI.prefab`과 `WICampaignTitleUGUIController`만 입력·표시를 담당하도록 단일화. 행정 컨트롤러에는 기존 캠페인 상태 생성·불러오기 API만 유지
- 2026-08-11: 실제 UGUI 실행 경로에서 숨은 UI Toolkit 전역 입력과 지도 바인딩을 중단. 행정 초기화 시 UXML 전역·영지 버튼 이벤트 연결과 60개 성 노드/연결선 갱신을 실행하지 않으며, 새 캠페인·설정 적용·기본값 복원·저장 불러오기에서도 `BuildMap()`을 호출하지 않도록 정리. UGUI 지도는 프리팹에 고정된 노드와 `WIAdministrationMapUGUIController` 스냅샷만 사용
- 2026-08-11: MainScene에 나열되던 23개 UGUI 화면 인스턴스를 제거하고 화면 관리자 하나로 정리. 현재 `WIUIScreenManager`가 직렬화된 완성 프리팹 에셋 23개를 플레이 시작 때 생성하며, 각 빌더는 씬 인스턴스 대신 프리팹 참조만 자동 등록. 행정 전역/영지 갱신은 UI Toolkit 표시 요소를 건드리지 않고 UGUI 상태 알림만 사용하도록 전환했으며 UGUI 이행 EditMode 검사 28/28, 전체 프리팹 Missing Script 0건, 플레이 월드 진입 오류 0건 확인
- 2026-08-11: 행정 런타임 프리팹의 UI Toolkit 필수 의존성 제거. `WIAdministrationUIController`의 `RequireComponent(UIDocument)`와 UGUI 모드 초기화의 `rootVisualElement` 조회를 제거하고, `WIAdministrationUI.prefab` 및 MainScene 인스턴스에서 `UIDocument` 컴포넌트를 UnityMCP로 삭제. 행정 컨트롤러는 캠페인 상태와 UGUI 브리지 역할만 유지
- 2026-08-11: 호출이 끊긴 UI Toolkit 군사 모달 구현 제거. 전투 세션·전투단 목록/상세·편성 성/대장/역할/인물·이동/원정 9개 `CreateModal` 흐름과 미사용 UGUI 레거시 우회 API를 삭제하고, 원정 실패 사유와 역할 표시 헬퍼만 UGUI 군사 브리지용으로 유지. 잔여 레거시 입력도 군사·원정 UGUI 이벤트로 전달
- 팝업 헤더 PNG는 변경하지 않고 Unity Sprite 표시 영역을 실제 크기인 1024×297로 맞춰 상단 프레임 잘림을 수정

### 데이터

- `WIAdministrationDatabaseSO` 기반 마스터 데이터 구조
- 진영 5개, 성 60개, 인접 경로
- 60개 성의 정식 한글·영문 고유 명칭, 지형 특성, 랜드마크와 전문 분야
- 성 전문 분야 5종 효과와 사업·월간 수입·전략 방어 적용
- 임시 영웅 8명과 기획 기준 특화 시설 8개
- 시작 자원 및 아레스 초기 배치
- 한/영 UI 문자열 데이터
- 성 규모, 번영/기술/안정/방어 초기값과 영웅 통솔 데이터
- 숫자 충성도를 제거하고 안정/동요/위험 상태로 단순화
- `[영웅]`/`[일반]` 등급과 8종 특기 데이터

### 영지 관리 로직

- 런타임 캠페인 상태 생성
- 성별 소유 진영, 영지관, 영웅과 특화 시설 상태
- 번영, 기술, 질서와 성 규모 기반 월간 자원 수입
- 연도, 월, 턴 증가
- 성별 번영, 기술, 안정, 방어 수치의 런타임 상태
- 8종 월간 중점 사업, 기본/집중 투자, 담당 영웅 지정
- 담당 영웅 능력치와 투자 등급에 따른 사업 성과 계산
- 확장 사업 3개월 완료 시 성 규모 상승
- 확장 완료 후 특화 시설 하나를 선택하는 흐름
- 다른 성을 포함한 중점 사업 담당 인물 중복 배치 방지
- 담당 인물별 예상 사업 성과 표시
- 부국, 개발, 안정, 수비, 원정, 인재의 월간 진영 방침
- 영지관 임명, 5종 운영 방침과 월 100G/200G 위임 예산
- 영지관 방침과 성 상태에 따른 월간 사업 자동 선택
- 영지관의 선택 사업과 이유를 기록하는 월간 보고
- 인물별 공훈, 명성, 경험, 피로, 부상과 영입 진척 상태
- 갈등, 보통, 친애의 3단계 인물 관계
- 탐색, 영입, 교류, 훈련, 휴식 월간 인재 활동
- 탐색을 통한 인재 발견과 매력 기반 누적 설득 영입
- 특기와 중점 사업이 일치할 때 성과 보너스 적용
- 중점 사업 성과에서만 생성되는 안전/과감/영웅 선택 사건
- 대성공 시 생성되고 성당 최대 2개를 유지하는 영웅의 흔적
- 영웅의 흔적과 같은 사업을 진행할 때 성과 보너스
- 성별 최대 3개의 선술집 월간 의뢰와 담당 인물 배정
- 의뢰 완료 시 금화, 공훈, 명성, 피로 결과 반영
- 인재별 필요 명성과 서사적 영입 요구 조건
- 인물만으로 구성되는 다인원 전투단과 대장 통솔 기반 권장 인원
- 대장, 전위, 근접, 원거리, 마법, 지원의 6개 전투단 역할
- 미숙, 숙련, 정예 전투단 숙련과 합동 경험
- 충분, 부족, 고갈의 3단계 보급 상태
- 인접 성 월간 이동과 보급 부족 시 이동 지연
- 아군 성 도착 시 재주둔 및 적 성 도착 시 전투 대기
- 전투단 배치 인물의 영지 관리·활동·의뢰 중복 배치 방지
- 합동 훈련 예약과 구성원 경험·피로·전투단 숙련 증가
- 전투단원 제외 시 합동 경험 감소 및 현재 성 복귀
- 이동·전투 중 해산 방지와 주둔 전투단 해산
- 전투 승리 결과를 받는 점령 처리 API
- 점령 성 질서 20 이하, 3개월 불안과 영지관·주둔 전투단 안정 조건

### UI

- UI Toolkit 기반 전체 대륙 지도
- 진영별 성채와 깃발 이미지로 구분되는 60개 성 노드
- 좌측 선택 성·주둔 영웅 요약, 중앙 대륙 지도, 우측 목표·전투 알림의 3열 전략 화면
- 60개 성 노드와 4개 전역 영웅 카드가 UXML에 고정 배치되어 UI Builder에서 직접 편집 가능
- `Noto Serif KR` 제목·본문 서체와 `Noto Sans KR` 숫자·소형 정보·버튼 서체 적용
- 시안형 단색 명령 아이콘 9종, HUD 아이콘 4종과 일반·선택·위험 9-Slice 버튼 3종 적용
- 우측 목표·알림 및 영지 관리 명령에 흑청색·은색 9-Slice 패널 에셋 6종 적용
- 실제 버튼 에셋 7종의 A~G Type 공식 별칭과 시각 카탈로그 문서화
- 동양 고전 전략 게임풍의 먹색, 남색, 황동, 양피지 UI 테마
- 상단 통치자 초상화 슬롯, 진영, 날짜, 3대 자원, 턴 안내, 진행 버튼
- 군사, 영웅, 외교, 영지 관리, 첩보, 연구, 통치, 정보, 설정 메뉴 바
- 대륙 지도 중심의 전역 전략 화면
- 군사, 영웅, 외교, 첩보, 연구, 통치 전역 명령 바
- 성 노드 선택 시 별도 영지 관리 화면으로 전환되는 2단계 UX
- 성 전경 및 영지관 전신 이미지 교체 슬롯
- 우측 영지 관리 명령과 대륙 지도 복귀 동선
- 성 현황·영지관 초상·4대 수치의 좌측 패널과 우측 영지 관리 명령을 분리한 성 화면
- 주둔 영웅 8칸과 특화 시설 2칸을 UXML에 고정 배치하고 독립 스크롤 영역으로 분리
- 영지 관리 UI를 조립 루트와 HUD·전략 지도·영지 관리·오버레이 UXML 템플릿으로 분리
- 영지 관리 컨트롤러를 캠페인·지도·영지·인물·군사·진영 관계·보고·턴 기능별 partial 파일로 분리
- 대륙 화면 의회 및 월간 보고 메뉴
- 성 화면 영지관 위임 설정 메뉴
- 성 화면 인재 활동 배정과 전역 인물 현황
- 전역 군사 메뉴의 전투단 생성, 역할 편성, 이동 및 원정
- 지도 성 노드의 주둔 전투단 수와 전투 대기 표시
- 진영별 독립 금화·마나·영향력 경제와 AI 성향
- AI의 실제 자원 소비 기반 중점 사업·전투단 생성·인접 원정
- 플레이어 인접 AI 사업 동향과 침공 경고
- 성별 전선 위협도 평가와 AI 방어·증원·공격 임무
- 영토 규모 및 가용 지휘관 기반 AI 복수 전투단 운용
- 아군 영토 최단 경로를 이용한 전선 증원
- 전쟁·중립·우호·불가침·동맹의 5단계 진영 외교 관계
- 친선 사절, 휴전, 불가침, 동맹, 선전포고와 동맹 금화 원조
- 외교 상태에 따른 적 성 원정 제한과 AI 월간 관계 개선
- 외교 관계 및 원조 대기 시간의 캠페인 저장·불러오기
- 결정론적 전략 전투력 판정과 성 방어 전력 계산
- 패배 전투단의 아군 성 후퇴, 부상 및 한 달 재편성
- 대패 포로, 3개월 임무 제한·원진영 귀환, 질서 있는 후퇴의 부상·포로 방지
- 정식 로스터 확장 전 영구 사망 비활성화
- ScriptableObject 전투 보상 수치, 인물별 월간 보고와 공동 승리 관계 발전
- 승리 전투단의 공훈·경험 보상과 기존 점령 안정 시스템 연동
- 전장·참가 전투단·양측 전력을 고정하는 전투 세션 데이터
- 전략 자동 판정과 실시간 전투가 공유하는 단일 결과 반환 API
- 중복 결과 제출 방지와 전투 세션 상태 관리
- 역할별 전열·중열·후열 배치와 캐릭터 전투 런타임 생성
- 기본 표적 탐색, 접근, 근접·원거리 공격과 전멸 승패 판정
- 인물 최소 간격과 겹침 해소, 근접 밀치기, 사수 저항과 진형 행 복귀
- 전투 설정 ScriptableObject, 전투 씬과 빈 이미지 캐릭터 프리팹
- 전진·위치 사수·집중 공격·후퇴의 네 가지 전투단 명령
- 현재 명령 강조, 집중 표적 표시, 후퇴 재확인과 1·2·3·R PC 단축키
- WASD·방향키 이동, 휠 확대, Home 복귀와 전장 클릭 인물 정보
- 20·40·60명 600스텝 벤치마크와 전투 핫 루프 할당 최적화
- 마나와 재사용 대기시간을 사용하는 영웅 액티브 스킬
- 고유 영웅 8명의 피해·회복·지휘 스킬, 범위 판정과 임시 범위 시각 효과
- 생존 인원·전투 시간·명령·스킬을 표시하는 모바일 대응 전투 HUD
- 영웅·일반 인물과 역할을 양측에 편성해 BattleScene을 실행하는 에디터 전투 테스트 랩
- 외부 아트 없이 동작하는 격자 전장, 영웅 원형·일반 사각형, 체력 바와 공격 도형
- 속도·수명·충돌 반경과 선택적 아군 오발 규칙을 사용하는 실제 원거리 발사체
- 씬 전환 중 캠페인 상태를 보존하는 영속 런타임 서비스
- 플레이어 참가 전투 자동 판정 보류와 전투 시작 동선
- BattleScene 초기화, 결과 제출과 MainScene 복귀 흐름
- 버전이 포함된 캠페인 JSON 저장·불러오기
- 3개 수동 슬롯과 턴 종료·전투 복귀 자동 저장
- 임시 파일 우선 기록, 손상 데이터와 미래 버전 거부
- 한국어·영어, 전체 화면, 30·60·120 FPS와 오디오 설정
- PC 캠페인 시작 화면, 아발론 고정 시작과 여유·표준·도전 난이도
- 숨은 자원 보너스 없이 AI 담당 후보 선택 정밀도만 변경하는 난이도 규칙
- 여유·표준·도전 12개월 장기 시뮬레이션과 경제·성·전투단·인물 무결성 검사
- 세 난이도 36개월 AI 사업·연구·전투단·이동·전투 운영 검사와 AI 수도 보조 인물 배치
- ScriptableObject 기반 첫해 1·2·3·6·12월 핵심 안내, 확인·건너뛰기와 저장 진행 상태
- 대륙 전체 성 점유 승리, 플레이어 영토 소멸 패배와 결과 확인·저장 상태
- 데이터 기반 중점 사업 비용·성과·확장 기간과 12개월 적극 투자 밸런스 검사
- 특기 8종 데이터·적용 사업·담당자 예상 보너스 UI·고유 사업 결과와 인물·전투 콘텐츠 수량 감사
- ScriptableObject 기본 설정과 PlayerPrefs 사용자 설정 분리
- 선행 연구·기술·마나·담당 인물을 사용하는 월간 연구
- 완료 연구의 자원·사업·전투단 전투력 진영 보너스
- AI 성향과 실제 마나를 사용하는 동일 연구 판단
- 공훈과 영향력을 사용하는 작위 수여 및 충성 안정
- 고유 영웅과 별도로 탐색·영입되는 `[일반]` 인물 6명
- 전투 승리 성취, 공훈 80, 명성 30과 영향력 30을 사용하는 영웅 승격
- 같은 진영 인접 성으로 한 달이 걸리는 개별 인물 이동
- 주둔·유휴 조건을 검증하는 영지관 임명·해임과 원정 시 직책 해제
- 중점 사업, 개인 활동, 의뢰, 전투단, 연구와 이동을 통합한 활동 중복 제한
- ScriptableObject 인접 관계를 사용하는 전략 지도 성 연결선
- 진영색 이동 경로, 적색 접경 전선과 선택 성 금색 경로 강조
- ImageGen으로 제작한 16:9 판타지 대륙 지도 배경과 ScriptableObject Sprite 연결
- 60개 성을 북부 산악·서부 숲·중앙 제국·남부 화산 지형에 맞춘 비정형 좌표로 재배치
- 진영 내부 근거리 도로망과 7개 국경 관문으로 전략 지도 인접 관계 재구성
- ScriptableObject 기반 조사·방첩·유언비어·인재 이간 첩보 MVP
- 미조사 적 성 수치·주둔·시설·전투단 정보 마스킹과 조사·동맹·전투 접촉 예외
- 지력·질서·방첩 기반 성공 판정, 조사 정보와 방첩 기간 저장 및 모략 AI 처리
- 첩보 출발 시 영향력 소비, 1개월 담당 인물 점유, 다음 턴 판정·복귀와 진행 상태 저장
- 첩보별 발각 데이터, 질서·방첩·지력 기반 발각 판정과 외교 관계 단계 악화
- 포로 몸값·맞교환 귀환과 동맹 공동 공격 목표·기간·AI 집결 및 원정
- 진영·분야별 AI 선택 이유와 점수를 표시하는 턴당 제한형 월간 보고
- 여유 3·표준 2·도전 1의 공통 AI 후보 선택 폭과 재현 가능한 목표 선택
- 영토 0개 진영의 멸망 턴 저장, 잔존 행동·전투단·포로·외교 약속 정리와 AI 중단
- 세 난이도 60·120개월 총 540개월 장기 시뮬레이션 및 전투단장 포로 승계 보완
- 성장형·균형형·공세형 자동 플레이 정책과 24·60·120개월 전투·원정·영토 변화 지표
- 실제 전쟁 접경만 사용하는 위협도 계산과 장기 평화 시 AI 제2전선 자동 활성화
- 영웅 100명·일반 인물 400명 회귀 기준과 핵심 인물 19명 시작 배치 동기화
- 핵심 인물 능력치·특기·초상화·관계를 보존하는 500명 결정론적 로스터 시더
- 성 방어·질서·주둔 인물의 전략 전력 가중치를 ScriptableObject로 분리
- 지휘관 단독 원정을 막고 성별 예비 인물을 남기는 AI 다인원 전투단 편성
- 자동 플레이의 피로·예상 전력비 기반 원정 제한과 첫 12개월 사건 밀도 지표
- 정책·난이도별 캠페인 완주, 고정 결과 9행과 CSV·Markdown 내보내기를 제공하는 Campaign Auto Test Lab
- 일반 성 55곳의 진영·지형별 공용 전경 5종 적용
- 기존 영웅·일반 인물 14명 초상화, 신규 일반 인물 10명 빈 이미지 슬롯과 특화 시설 8종 아이콘 적용
- PC 16:9 우선 영지 관리 레이아웃과 이미지·텍스트 분리형 인물/시설 카드
- 진영 문장, 성 전경, 영지관·인물 초상화와 특화 시설 아이콘의 자동 UI 적용 구조
- 아발론·발도르·아이언하트·실반로드·네크로폴리스 진영 문장 5종
- 아발론·발도르·아이언하트·실반로드·네크로폴리스 5대 진영 수도 전경과 ScriptableObject 연결
- 영웅 및 특화 시설 슬롯
- 기본 시설 안내와 확장 후 특화 시설 선택 모달
- 턴 처리 및 결과 요약
- 성 상태 수치와 진행 중인 중점 사업 표시 및 지정 모달
- 교체 가능한 지도, 문장, 초상화 이미지 슬롯

### 씬과 프리팹

- `MainScene/ManagerObjects/AdministrationUI`
- `WIAdministrationUI.prefab`
- Main Camera, Global Light 2D, EventSystem 유지
- 구형 uGUI MainCanvas 및 방치형 매니저 제거

## 제거 완료

- 던전 탐험, 몬스터, 파티 편성, 방치형 전투 관련 코드
- 구형 던전/몬스터/직업/스킬 ScriptableObject
- 구형 데이터 임포터, 데이터 관리자, 저장 창의 빈 코드
- 구형 UIManager 및 uGUI MainCanvas
- 방치형 프로젝트 복구 씬과 데이터 CSV
- 고정 성/시설 자원 생산 데이터와 자유 건설 비용
- 확률형 선술집 방문 및 금화 즉시 영입
- 인접 적 성만 선택하는 임시 원정 처리
- 선술집, 시장, 훈련소, 성관의 건설 항목화

## 다음 작업

- `crimson-knight` 실험 결과는 실제 게임 애니메이션 후보에서 제외하고 캐릭터 표현 방식 재논의
- 사용자가 전투 검증을 재개할 때 전략 전투 관련 잔여 회귀 3건을 별도 교정
- 첫 12개월 선택 사건이 현재 1·5개월에 집중된 상태의 체감 밀도 수동 검토
- 비전투 기계 검증을 기준으로 30~60분 수동 통합 플레이와 감정·체감 기록

## 최근 변경

- 2026-08-10: H Type의 비대칭 화살촉 장식을 원형 그대로 표시하기 위해 `#turn-button`의 9-Slice를 비활성화하고 `scale-to-fit`으로 변경. 240×72 표시 크기와 이미지 원본은 유지.

- 2026-08-10: H Type 원본이 작은 `다음 턴` 버튼에서 압축되던 문제를 확인하고, 이미지 파일은 유지한 채 버튼을 240×72로 확대하고 전용 9-Slice를 좌 56·우 16·상하 12로 축소. 하단 명령 바의 세로 패딩을 3px로 맞춰 버튼을 수용함.

- 2026-08-09: `다음 턴` 버튼이 공통 버튼 스타일에 덮이지 않도록 `#turn-button` 전용 최종 USS 규칙에서 `button_type_h.png`와 좌 112·우 32·상하 32 슬라이스를 직접 고정.

- 2026-08-09: `bg_type_c.png`에서 두꺼운 회색 석재 외곽 프레임을 제거하고 청동 테두리·대각 모서리·어두운 중앙 면만 유지. 모서리 보존용 9-Slice를 좌우 72·상하 40으로 조정.

- 2026-08-09: 제공된 회흑색 석재·철제 프레임 시안을 바탕으로 `bg_type_c.png` 9-Slice 배경을 신규 제작. 1587×508 투명 외곽 PNG와 `.bg-type-c` 재사용 클래스, 좌우 96·상하 48 슬라이스를 추가.

- 2026-08-09: 전역 지도 하단 `군사`~`월간 보고` 명령 버튼의 배경만 `button_flat_normal.png`로 교체하고 3px 9-Slice를 적용. 아이콘·문자·단축키·크기와 `다음 턴` H Type은 유지.

- 2026-08-09: 전역 지도 `다음 턴` 버튼을 Primary 타입에서 장식형 `button_type_h.png`로 교체하고, 다른 명령 버튼의 고딕과 구분되는 `NotoSerifKR` 굵은 명조체를 적용.

- 2026-08-09: 초기 검은 금속 패널을 기준으로 원본 대비 약 1/2 두께의 단일 테두리를 가진 `bg_type_b.png`를 신규 제작. A Type과 동일한 1587×508 규격·투명 모서리를 사용하며 재사용 USS 클래스 `.bg-type-b`와 16px 9-Slice 규칙을 추가.

- 2026-08-09: 넓고 좁은 모달 헤더에서 공통 사용되는 `popup_header.png`의 금속 테두리를 기존 약 1/4 두께의 단일 프레임으로 재작성. 1024×297 규격과 양피지·나침반·볼트 구성을 유지하고 9-Slice를 좌우 20·상하 10으로 축소.

- 2026-08-09: `ProjectWI_Strategy_UI_Concept_V1` 시안에 맞춰 전역 지도 오른쪽을 상단 목표 패널과 하단 알림 패널로 분리하고, 두 패널 모두 `bg_type_a.png` 3px 9-Slice 테두리로 통일.

- 2026-08-09: 전역 지도 좌측 성 요약 패널(`castle-summary-panel`)의 단색 배경을 공통 `bg_type_a.png` 3px 9-Slice 배경으로 교체.

- 2026-08-09: 공통 모달 헤더 PNG 교체 후 Sprite 영역 높이가 이전 160px로 남아 상단 프레임이 잘리던 문제를 수정. 이미지 자체는 변경하지 않고 Unity 표시 영역을 실제 크기 1024×297 전체로 맞춤.

- 2026-08-09: 사용자가 다시 지정한 은색 금속·양피지 헤더 이미지를 최종 공통 모달 헤더로 교체. 검은 바깥 여백만 제거한 1774×515 원본을 가공 소스로 보존하고 비율 유지 1024×297 에셋을 생성했으며, 모달 헤더 최소 높이를 80px, 9-Slice를 좌우 48·상하 28로 조정함.

- 2026-08-09: 공통 모달 헤더에 9-Slice를 적용해도 좌우가 비어 보이던 원인이 원본의 15px 남색 세로 띠임을 확인. `popup_header.png`에서 양피지 질감을 좌우 4px 외곽선까지 확장해 빈 띠를 제거하고 기존 좌우 24·상하 10 슬라이스는 유지함.

- 2026-08-09: 캠페인 시작 패널을 940px 폭·최소 650px 높이로 확대하고, 소개 문구를 제목 장식 아래이자 난이도 선택 바로 위로 내림. 난이도 안내는 하단 프레임 안으로 올렸으며 난이도·시작 조건 카드의 여러 문장을 마침표 뒤에서 줄바꿈하도록 표시 규칙과 회귀 검사를 추가함.

- 2026-08-09: `popup_header.png`가 1024×160 원본 미리보기와 약 520×64 실제 모달에서 다르게 보이던 9-Slice 비율 문제를 수정. 고정 테두리 영역을 좌우 54→24px, 상하 20→10px로 줄여 실제 화면에서도 양피지 면과 금속 프레임 비율이 원본에 가깝게 표시되도록 조정함.

- 2026-08-09: 공통 모달 헤더 `popup_header.png`의 좌우 남색 홈과 깊게 들어간 장식을 제거하고, 중앙 양피지 질감이 양쪽 금속 프레임 안까지 이어지도록 1024×160 에셋을 교체함. 기존 외곽 금속 레일·모서리 볼트·9-Slice 규격은 유지함.

- 2026-08-09: A/B Type 버튼을 2px만 파인 투명 모서리와 단일 2px 금속 테두리의 동일 형상으로 재작성. B Type은 A Type과 형상·알파를 일치시키고 기존 푸른 색조만 유지했으며, 두 버튼의 행정 UI 9-Slice를 상하좌우 3px로 조정함.

- 2026-08-09: Background A Type 사용자 피드백을 반영해 `bg_type_a.png`의 깊게 파인 모서리를 2px 사선으로 축소하고 이중 테두리를 제거해 단일 2px 금속 외곽선만 남김. 거의 직사각형인 형태에 맞춰 Unity Sprite border와 행정·전투 USS 9-Slice를 3px로 동기화함.

- 2026-08-09: 사용자 피드백에 따라 캠페인 첫 화면을 기존 `popup_panel.png` 배경으로 복구. `bg_type_a.png`는 모서리의 합성된 흰 체크무늬를 실제 투명 알파로 제거했으며 이후 단일 테두리 버전으로 추가 정리함.

- 2026-08-09: UI Toolkit의 단색 배경만 사용하던 주요 행정 패널(상단 HUD, 지도 제목·범례·도구, 하단 명령 바, 안내, 성 정보, 공통 모달)과 전투 HUD 정보 패널을 `bg_type_a.png` 3px 9-Slice 배경으로 교체. 캠페인 첫 화면과 버튼·진행바·성 노드·이미지 슬롯·상태 선택 카드는 의미별 기존 스타일을 유지하고 전용 회귀 검사를 추가함.

- 2026-08-09: `Panel_BackGround.png`의 가운데 질감을 바탕으로 1587×508 RGBA `bg_type_a.png`를 Background A Type 게임 에셋으로 추가. 현재 Unity Sprite/Single, 좌우상하 3px 9-Slice, 2px 사선 투명 모서리, 단일 2px 외곽선, 밉맵 비활성, Clamp, 무압축 설정을 적용함.

- 2026-08-09: `WIAdministration.uxml`을 조립용 루트와 HUD·전략 지도·영지 관리·오버레이의 네 Template UXML로 분리하고, 3,000줄 이상이던 `WIAdministrationUIController`를 기능별 partial 파일로 분리. 새 구조를 결합해 검사하도록 UI 회귀 테스트를 보강하고 실제 `VisualTreeAsset` 조립과 60개 성 노드를 포함한 EditMode 26/26 통과. 이후 UI·코드는 과도한 추상화 없이 책임별로 적당히 분리하도록 프로젝트 규칙에 명시.
- 2026-08-09: 플레이어 표시 용어를 중세 판타지 기준으로 통일. 세력→진영, 내정→영지 관리, 태수→영지관, 평정→의회, 월보→월간 보고, 계략→첩보, 출정→원정, 부대→전투단, 등용→영입, 재야→방랑, 치안→질서, 공적→공훈으로 교체하고 영웅 메뉴·영웅 배치 문맥을 분리. 영문 저장 식별자는 유지하고 `FantasyTerminologyGuide.md`와 폐기 용어 회귀 검사를 추가했으며 UI EditMode 25/25 통과.
- 2026-08-09: 빈 프레임 가이드만 사용한 기존 공격 시트의 동작 불일치를 보완하기 위해 `Assets/generated/sprites/medieval-swordsman-attack-v2`를 제작. 높은 당김·앞발 내딛기·대각선 타격·낮은 후속 자세를 관절 포즈 가이드로 먼저 고정해 하나의 연속 베기로 생성했으며, 4프레임 투명 아틀라스·manifest·GIF와 자동 100점 및 육안 QA를 완료함. 기존 시트는 비교용으로 보존하고 두 시트 모두 아직 전투 데이터에는 연결하지 않음.
- 2026-08-09: `Assets/generated/sprites/medieval-swordsman-idle`에 중세 검사 4프레임 비반복 공격 스프라이트 시트를 제작. 준비·전진 베기·후속 뻗기·복귀 동작, 투명 프레임, 2048×512 아틀라스, 512×512 프레임 레이아웃 manifest와 GIF QA를 생성했으며 추출·합성 자동 검사와 육안 동작 검증을 통과함. 아직 전투 데이터에는 연결하지 않음.
- 2026-08-09: 일러스트가 아닌 실제 전투 배치용 중세 검사 단일 프레임 스프라이트를 재제작. 약 4.5~5등신, 양손 검 전투 준비 자세, 작은 화면용 단순 색 덩어리와 투명 배경을 적용해 `Assets/Art/Characters/Battle_MedievalSwordsman_Test_V2.png`에 추가했으며 아직 전투 데이터에는 연결하지 않음.
- 2026-08-09: 캐릭터 화풍 검증용 중세 검사 전신 일러스트를 참고 이미지의 애니풍 선화·셀 셰이딩·약 5.5~6등신 비율에 맞춰 ImageGen으로 제작하고 `Assets/Art/Characters/Character_MedievalSwordsman_Test_V1.png`에 추가. 아직 캐릭터 데이터에는 연결하지 않은 테스트 에셋으로 보존.
- 2026-08-09: 기존 D Type 이미지를 제거하고 참고 월간 보고 버튼의 외형을 기반으로 한 빈 흑청색 금속 버튼으로 교체. 사용자 피드백에 따라 깊게 파인 모서리 장식을 폐기하고 외곽의 98%가 직선인 직사각형, 미세 사선 모서리와 작은 내부 리벳만 유지했으며 기존 `right_action_button.png` 파일명과 Unity 참조는 보존.
- 2026-08-07: 참고 이미지의 남청색 본체·왼쪽 화살촉 장식·은황동 이중 테두리를 바탕으로 텍스트와 아이콘이 없는 H Type 대표 진행 버튼을 ImageGen으로 제작. `button_type_h.png`, `generated-ornate-action` USS 클래스와 좌 112·우 32·상하 32의 9-Slice 규칙을 추가하고 버튼 타입 문서를 A~H로 확장.
- 2026-08-07: 하단 전역 명령 8개(군사~월간 보고)의 배경을 D Type(`right_action_button.png`)으로 변경하고 좌우 28·상하 20의 9-Slice를 적용. `다음 턴` 버튼은 기존 B Type을 유지하며 전용 EditMode 회귀 검사를 추가.
- 2026-08-07: 현재 게임에서 사용하는 버튼 배경 7종을 A~G Type으로 분류. A~C는 전략·영지 관리 기본/주요/위험, D는 우측 패널 하단 동작, E~G는 전투 HUD 기본/주요/위험 버튼으로 지정하고 실제 에셋을 모은 PNG 카탈로그와 `ButtonTypeGuide.md` 업무 지시 문서를 추가.
- 2026-08-07: `ProjectWI > Tools > Campaign Auto Test Lab` 에디터 도구 추가. UXML/USS에 입력과 결과 9행을 고정 배치하고 단일 조합 또는 정책 3종 × 난이도 3종을 결말/최대 개월까지 자동 실행. 진행 개월·결과·성·전투·승패·원정·소유권·선택 지표 표시와 UTF-8 CSV·Markdown 내보내기를 지원하며 전용 EditMode 3/3 및 실제 메뉴 창 열기 확인.
- 2026-08-07: 전략 자동 판정의 공격자 동률 승리 불일치를 수비 성공으로 통일하고 주둔 인물 방어 가중치를 60%로 조정. 자동 공세는 상호 침공 전투단을 예상 전력에 포함하고 첫 두 달 준비 후 115%, 균형형은 125% 전력부터 원정하도록 변경. 정책 균형창 3/3과 24·60·120개월 자동 플레이 9/9 통과했으며 성장형 120개월 방어전은 206전에서 22전, 공세형은 120개월 수도 1성 생존으로 개선.
- 2026-08-07: 500명 로스터를 영웅 100·일반 400 기준으로 회귀 동기화하고 핵심 인물 데이터와 19명 시작 배치를 복원. AI가 지휘관 한 명만 원정하거나 공격형 정책이 수도를 비우던 문제를 수정하고 전략 방어 가중치를 ScriptableObject로 분리. 초반 12개월 사건 밀도 자동 검사와 전투 목표 3종 기반을 추가했으며 전체 EditMode 227개 중 비전투 224개 통과, 사용자 요청에 따라 전투 관련 3건은 보류.
- 2026-08-06: UI 작업을 다른 세션에서 재개할 수 있도록 `GameDocuments/UIHandoffReport.md` 작성. 전략 지도와 영지 관리 목표 시안 두 장을 고정 기준으로 지정하고 현재 UXML/USS 구조, 재사용 에셋, 단계별 작업 순서, 해상도·스크롤·9-Slice·상태 검증 완료 기준을 정리. 최신 두 미리보기 캡처가 캠페인 시작 레이어만 보여 주므로 실제 전략·성 화면 재캡처를 첫 단계로 등록.
- 2026-08-06: UI 작업을 보류하고 재미 검증 자동화 진행. 성장형·균형형·공세형 24·60·120개월 자동 실행과 대기 선택·전투 해소를 추가하고 신규 검사 12/12 및 기존 세 난이도 36개월 AI 이동 검사 3/3 통과. 중립 접경을 적으로 보던 위협도 오류와 AI 전선 정체를 수정했으며, 성장형 방어 전승·공세형 붕괴를 다음 밸런스 과제로 확인.
- 2026-08-06: 작은 버튼의 명조 획과 외곽선이 뭉개지던 문제를 수정해 행정·팝업·전투 버튼을 Noto Sans KR 및 무외곽선으로 통일. ImageGen으로 우측 패널 제목 장식, 외곽 프레임, 목표 카드, 일반·위험 알림 행과 소형 동작 버튼 6종을 제작해 전략 지도 목표/알림 및 영지 관리 명령 UXML/USS에 직접 연결. UI 레이아웃 EditMode 23/23 통과.
- 2026-08-06: `ProjectWI > Viewer > Character Data Viewer` 유니티 에디터 데이터 뷰어/에디터 툴 추가. `WI_AdministrationDatabase.asset`에 수록된 14명 영웅 및 일반 인물 데이터를 엑셀 그리드 테이블 형태로 조회·검색·필터링·컬럼 정렬·인라인 셀 수정·추가·삭제 및 유니티 SerializedObject 기반 Undo/Redo/디스크 저장 기능 지원.
- 2026-08-05: 이미지 UI 연결을 코드 기반 런타임 부착에서 UXML/USS 직접 에셋 참조로 변경. UI Builder에서 버튼·아이콘·팝업 배경을 그대로 확인할 수 있으며, 전역 영지 관리 화면은 시안의 좌측 영지 카드·중앙 지도·우측 목표와 전투 알림·하단 명령 구조로 개편. EditMode 206/206 및 Windows 빌드·스모크 통과.
- 2026-08-05: ImageGen 기반 실제 UI 이미지 에셋 12종을 추가. 메뉴 아이콘 8종, 상태별 버튼 3종, 팝업 프레임을 Unity Resources에서 불러와 전역 명령·성 명령·캠페인·동적 팝업·전투 HUD에 적용. 생성 원본을 재처리할 수 있는 한국어 주석 파이프라인도 보존했으며 EditMode 206/206, Windows 빌드·스모크 통과.
- 2026-08-05: 전략 UI 콘셉트 V1의 청회색 지휘실 디자인을 실제 행정·캠페인·팝업·전투 HUD에 적용. 흰색 헤더와 짙은 본문, 청색 선택·다음 턴, 적색 위험 행동, 얇은 회색 프레임으로 정보 계층을 재정리하고 1366×768 축소 규칙을 유지. EditMode 205/205, Windows 빌드 오류·경고 0건 및 통합 스모크 통과.
- 2026-08-05: 전투 조명이 반경이 좁은 포인트 타입으로 잘못 저장돼 전장이 어둡던 문제를 수정. 전장 전체 백색 글로벌 2D 광원(밝기 1.1)으로 변경하고 그림자를 꺼 현재 임시 아트의 판독성을 확보. EditMode 204/204 및 Windows 빌드·스모크 통과.
- 2026-08-05: 월간 보고 내용과 전투 발생 영역을 각각 독립 스크롤 영역으로 분리하고 전투 버튼을 하단에 고정. 플레이어 전투를 모두 해결하기 전에는 다음 턴 진행을 차단하며, 전투 후 전략 화면 복귀 시 남은 전투를 자동으로 다시 표시. 상호 교차 침공은 하나의 회전 전투로 병합. EditMode 203/203 및 Windows 빌드·스모크 통과.
- 2026-08-05: 전체 영웅 목록을 포함한 공통 팝업의 세로 넘침을 수정. 제목과 닫기 버튼은 상단에 고정하고 긴 본문만 팝업 내부에서 마우스 휠·스크롤바로 이동하도록 공통 모달 구조를 변경. EditMode 200/200, Windows 빌드 및 통합 스모크 통과.

- 2026-08-03: 참고 이미지 방향에 맞춰 행정·캠페인 시작·전투 HUD를 흑백 중심 테마로 변경. 백색은 선택과 핵심 정보, 검정은 패널과 명령, 회색은 일반·비활성 상태에 사용하고 진영색과 위험 적색만 의미색으로 유지. 1920×1080 및 1366×768 전역·성 화면을 UnityMCP로 재검증.
- 2026-08-03: 긴 한글·영문 진영·성·인물 이름과 7자리 자원을 저장과 분리된 QA 미리보기에 주입. 이름은 생략 기호와 원문 툴팁, 큰 수는 한국어 만·억 및 영문 K·M 축약과 정확한 값 툴팁으로 처리하고 1366×768 전역·성 화면에서 잘림 없이 검증.
- 2026-08-03: 행정·모달·캠페인 시작·전투 버튼의 상태를 흑백 테마 기준으로 통일. 호버와 선택은 백색 반전, 비활성은 48% 저명도, 위험은 적갈색으로 제한하고 1366×768 상태 견본 화면을 UnityMCP로 검증.
- 2026-08-03: 전략 전역 메뉴 M·H·D·S·R·G·C·L과 다음 턴 T 단축키를 추가. ESC는 모달 닫기를 우선하고 성 화면에서는 전역으로 복귀하며, 캠페인 시작·모달·성·텍스트 입력 중 전역 명령을 차단. Unity 런타임 키 이벤트로 M 모달 열기, ESC 닫기와 성→전역 복귀를 검증.
- 2026-08-03: HUD 자원·성 수치·중점 사업·연구·외교·첩보에 비용·조건·예상 결과의 계산 근거 툴팁을 추가. 미조사 적 성은 기본 확률만 표시하고 조사 후에만 질서·방첩을 포함한 최종 첩보 확률을 공개. 1366×768 계산 근거 QA 화면과 EditMode 188/188을 검증.
- 2026-08-03: 지도 노드·범례·성 상세·툴팁에 진영 코드 AV·VD·IH·SY·NC를 추가하고, 적대 전선은 ×, 선택 연결은 ◎ 형태로 표시. 모든 노드와 경로를 회색으로 낮춘 저채도 QA에서도 판독 가능함을 확인하고 EditMode 189/189 통과.
- 2026-08-03: 저장 전 자체 역직렬화 검증과 임시 파일 원자 교체, 직전 정상본 `.bak` 보존을 추가. 주 저장 손상 시 백업 복구 안내, 빈·잘린 JSON과 실행 불가능한 빈 캠페인 거부, 구버전 선택 컬렉션·월·턴 복구를 실제 파일 테스트로 검증하고 EditMode 194/194 통과.
- 2026-08-03: 표준 캠페인을 120·240·480개월 실행해 각각 359.38ms·650.64ms·1,359.34ms를 기록. 저장 크기는 약 58KB, 대기·전투·이동·첩보 컬렉션은 일정하게 유지되어 무제한 누적이 없음을 확인하고 EditMode 195/195 통과.
- 2026-08-03: Windows x86-64 클린 개발 빌드를 생성. 빌드 오류·경고 0건, 일반 실행 로그 예외 0건을 확인하고 Player 자동 스모크 테스트에서 제2턴·60성·저장/불러오기와 MainScene·BattleScene 포함을 검증. 종료 코드 0으로 계획 마일스톤 A~G 완료.
- 2026-08-04: Windows Player 자동 통합 동선을 실제 전투 씬까지 확장. 표준 새 캠페인→제2턴→저장 왕복→아레스·리리아 전투 초기화→결과 처리→전략 UI 복귀가 종료 코드 0, 실패·예외 0건으로 완료됨. 다음 단계는 자동화로 판단할 수 없는 가독성·클릭 피로·밸런스 체감 수동 플레이.
- 2026-08-04: 시작 인물은 있으나 플레이어 전투단이 0개인 상태에서 성 원정 창이 편성 경로를 제공하지 않던 P1 UX 결함 수정. 원정 창에서 새 전투단 대장 선택으로 직접 이동하고 적 성 영향력 비용과 실패 이유를 표시. EditMode 196/196 및 새 Windows 빌드 통합 스모크 통과.
- 2026-08-04: `별빛 수도의 재건` 목표 팝업의 긴 상황·설명 문장이 오른쪽으로 넘치던 문제를 전용 자동 줄바꿈 라벨로 수정. 진행·보상도 작은 폭에서 줄바꿈되며 EditMode 197/197, Windows 빌드 오류·경고 0건과 통합 스모크 통과.
- 2026-08-04: 원정 전투단 도착 시 전투 세션은 생성되지만 실제 진입이 숨겨져 있던 문제를 수정. 턴 결과에 `전투 발생`과 전력·전투 시작 버튼을 표시하고 모든 공통 모달의 긴 Label·Button 문구를 기본 줄바꿈 처리. EditMode 199/199, Windows 빌드 오류·경고 0건과 통합 스모크 통과.
- 2026-08-13: 전투 시안에서 분리한 투명 HUD 에셋 4종을 실제 `WIBattleHUD.uxml`에 배치. 기존 컨트롤러 조회 이름과 전투 명령 연결은 유지하면서 상단 상태, 선택 인물, 명령, 스킬 영역을 각각 전용 배경 컨테이너로 분리하고 이전 `bg_type_a` 및 개별 버튼 텍스처 중첩을 제거함. Unity 에셋 재임포트 후 콘솔 오류 0건 확인.
- 2026-08-14: `Battle_Ground_NeutralDay_V1`을 기준으로 중립적인 대낮 색감과 확대용 미세 자갈·흙·희박한 풀 디테일을 강화한 실험 지면 `GroundExperiment_V2/Battle_Ground_NeutralDay_V2_4K.png`를 제작. 4096×4096, 무압축, Mipmap 비활성으로 임포트했으며 Unity 콘솔 오류·경고 0건을 확인함. 2×2 반복 QA에서는 이음선이 보여 현재는 단일 대형 지면 후보로만 보존하고 실제 전장에는 연결하지 않음.
- 2026-08-15: 최초 전투 시안의 회화적 밀도와 캐릭터 축척을 함께 검증하기 위한 완성형 청크 전장 V5를 적용함. 성벽·성문·야영지·망루·방책과 접지 그림자를 하나의 3840×2160 중립 대낮 배경에 함께 제작하고 1920×1080 네 청크로 기계 분할해 재조립 픽셀 완전 일치를 확인함. 다크 판타지의 검은 비네팅·압축된 암부와 GPT 이미지 특유의 노란/금색 조명을 배제하고 회갈색 흙, 절제된 올리브 잔디, 중립 회색 석재를 사용함. `WIBattleCharacterScaleArenaV5.prefab`의 기존 독립 환경물은 중복을 막기 위해 비활성화하고 현재 `WI_BattleConfig.arenaPrefab`에 연결함.
- 2026-08-15: 60명 전투의 캐릭터 밀도 재검증을 위해 아레스 전투 Sprite의 Transform Scale 1은 유지하고 PPU를 600에서 1178로 변경하여 원본 1178px 높이가 약 1월드 유닛으로 표시되도록 조정함. 공용 비주얼 재설정 메뉴도 1178 PPU 기준으로 동기화함. 동시에 V5의 캐릭터보다 큰 양각형 지면 무늬를 제거하고 넓은 회갈색 흙 색면·희미한 올리브 변화·드문 소형 자갈 중심으로 다시 그린 `Battle_CompleteArena_CharacterScale_V6_4K`을 제작함. 1920×1080 네 청크의 재조립 픽셀 완전 일치를 확인하고 `WIBattleCharacterScaleArenaV6.prefab` 및 현재 `WI_BattleConfig.arenaPrefab`에 연결함.
- 2026-08-15: 기존 아레스 원화와 비슷한 약 7등신 비율을 유지한 단일 전장용 후보 `Ares_Battle_Unit_V1.png`을 제작함. 흰 장발·검은 판금 갑옷·절제된 금장·짙은 남보라 망토·장검의 정체성을 유지하고 작은 화면에서 읽히도록 세부 장식과 명암 덩어리를 정리함. 녹색 크로마 원본을 투명화한 뒤 256×384 셀에 비율 유지 배치했으며 실제 캐릭터 알파 높이는 328px, 투명 모서리 검사를 통과함. Unity Sprite/Single, 328 PPU, Bilinear, Mipmap 비활성, 무압축, Max 512로 설정했으며 현재 데이터에는 연결하지 않은 검토 후보임. 정식 component-row 추출은 Windows에서 `fcntl` 잠금을 지원하지 않아 중단하고 단일 이미지 cutout 경로만 사용함.
- 2026-08-16: 캠페인 선택 UGUI 버튼의 9-Slice 설정을 정리함. `button_normal`의 실제 Single Sprite Border를 좌우·상하 28px로 복구하고, `WICampaignTitleUGUI.prefab`에서 `button_normal`·`button_primary`를 사용하는 Image 8개를 모두 Sliced로 변경함. 전체 행정 프리팹에서 두 Sprite를 Simple로 사용하는 잔여 항목은 0개이며 Unity 재임포트와 콘솔 컴파일 오류 없음 확인. 관련 EditMode 67개 중 65개 통과, 기존 전투 HUD·조명 기대값 불일치 2개는 본 변경과 무관하게 실패함.
- 2026-08-17: 전략 화면을 시안의 상단 자원 HUD·좌측 선택 성·중앙 지도·우측 목표/알림·하단 명령부 구조에 맞춰 재배치함. 중앙 지도 영역과 성 노드를 확대하고 좌우 패널 폭, 다음 턴 강조, 아발론 문장 장식을 조정했으며 기존 60성 선택 및 행정 명령 데이터 연결은 유지함.
- 2026-08-17: 전략 화면과 시안의 아트 차이를 줄이기 위해 전용 측면 패널·명령 버튼·다음 턴 버튼·영웅 초상 카드 에셋을 추가 적용함. 선택 성의 실제 주둔 영웅 최대 4명에 대해 초상·이름·경험 기반 표시 레벨을 연결하고 선택 지도 마커를 확대함. 런타임 UI 생성 없이 월드 UGUI 프리팹에 고정 카드 슬롯을 구성함.
- 2026-08-17: 전략 화면 상단 HUD를 기준 시안에 맞춰 재구성함. 중앙 진영 방침 문구를 제거하고 아발론 문장·진영명, 연월, 금화·마나·영향력과 월 수입, 우측 월간 보고·의회·연구·설정 아이콘을 한 줄로 재배치했으며 얇은 세로 구분선과 하단 금속선을 적용함. 설정 톱니 전용 투명 에셋을 제작하고 네 아이콘의 실제 버튼 기능을 연결했으며 UGUI EditMode 39/39 및 Unity 콘솔 오류 0건을 확인함.
