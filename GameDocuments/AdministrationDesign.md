# ProjectWI 내정 디자인

## 1. 문서 목적

이 문서는 `ProjectWI`의 성 이름, 성별 개성, 내정 화면, 메뉴 및 명령 명칭을 정의한다. 게임 규칙은 항상 `GameDesign.md`를 우선하며, 이 문서의 지역 분류는 데이터 수치가 아닌 작명과 배경 설정용 태그다.

`내정`의 영문 표기는 **Administration**을 사용한다. 게임 안에서 플레이어에게 보여 주는 영문 메뉴는 딱딱한 행정 업무보다 영지를 돌본다는 느낌을 주기 위해 **Realm Management**를 사용한다.

- 문서 파일명: `AdministrationDesign.md`
- 시스템 및 코드 명칭: `Administration`
- 한글 UI 명칭: `영지 관리`
- 영문 UI 명칭: `Realm Management`

## 2. 작명 원칙

### 2.1 성 이름

- 이름만 보고 어느 세력의 성인지 어느 정도 추측할 수 있어야 한다.
- 한글 이름은 2~6음절을 중심으로 짧게 작성한다.
- 영문 이름은 한국어 발음을 그대로 옮기기보다 문화권의 분위기를 살린다.
- 수도와 주요 거점은 고유명사를 사용하고, 일부 지방 성은 지형이나 랜드마크에서 이름을 얻는다.
- 성 이름과 지역 인상은 분리한다. UI에서는 공식 지형 특성과 특산을 표시한다.

### 2.2 세력별 언어 분위기

- 아발론: 잃어버린 이상향, 새벽, 호수와 별
- 발도르: 라틴·게르만풍 제국, 황금, 태양, 사자와 군단
- 아이언하트: 드워프·수인 연맹, 바위, 철, 서리와 야수
- 실반로드: 엘프·드루이드 의회, 숲, 달, 바람과 노래
- 네크로폴리스: 언데드·다크엘프 세력, 밤, 재, 뼈와 심연

## 3. 지역 인상 태그

아래 다섯 명칭은 작명과 배경 설명을 위한 태그다. 런타임 데이터에 별도 성 종류나 보너스로 저장하지 않는다. 실제 게임 효과는 `GameDesign.md`의 지형 특성, 고유 특산, 성 규모와 네 가지 성 수치로 결정한다.

| 배경 태그 | 한글명 | 영문명 | 서사적 인상 |
|---|---|---|---|
| `Capital` | 수도 | Capital | 세력의 중심. 평정과 고급 인사 기능 제공 |
| `City` | 도시 | City | 번영과 금화 수입에 유리 |
| `Fortress` | 요새 | Fortress | 방어와 전선 유지에 유리 |
| `Arcane` | 마도성 | Arcane Hold | 기술과 마나 크리스탈에 유리 |
| `Sanctuary` | 성역 | Sanctuary | 치안, 회복과 인재 교류에 유리 |

## 4. 60개 성 목록

성 ID는 현재 데이터의 `castle_00`~`castle_59`를 유지한다. 세력별 시작 성 개수도 기존 기획과 동일하게 아발론 1, 발도르 25, 아이언하트 12, 실반로드 12, 네크로폴리스 10으로 구성한다.

### 4.1 아발론 백작국 — 1개

| ID | 한글명 | 영문명 | 지역 인상 | 특산·랜드마크 | 내정 개성 |
|---|---|---|---|---|---|
| `castle_00` | 아발론 | Avalon | 수도 | 별빛 호수 | 모든 사업의 최소 성공률이 보장되며 영웅 방문 사건이 자주 발생 |

아발론은 시작 시 폐허 상태다. 성의 규모가 커질 때마다 `마지막 성채 → 호수의 왕성 → 재건된 아발론`로 외형과 지도 명칭이 변화한다.

### 4.2 발도르 군사 제국 — 25개

| ID | 한글명 | 영문명 | 지역 인상 | 특산·랜드마크 | 내정 개성 |
|---|---|---|---|---|---|
| `castle_01` | 발도르 | Valdor | 수도 | 황금 옥좌 | 영향력 수입과 기사 계열 인재의 공적 획득 증가 |
| `castle_02` | 솔가드 | Solgard | 요새 | 태양 성벽 | 주간 전투와 성채 정비에 유리 |
| `castle_03` | 레오니스 | Leonis | 도시 | 사자 대광장 | 선술집 의뢰와 인간 영웅 방문 증가 |
| `castle_04` | 카르디아 | Cardia | 도시 | 황금 곡창 | 영지 진흥 시 보급 상태도 함께 개선 |
| `castle_05` | 브란트 | Brandt | 요새 | 검은 철문 | 근접 장비 제작 비용 감소 |
| `castle_06` | 아르켄 | Arken | 마도성 | 제국 마도원 | 인간 마법사 훈련과 마나 정제에 유리 |
| `castle_07` | 로젠하임 | Rosenheim | 도시 | 장미 시장 | 선물과 교역 물품의 가격 감소 |
| `castle_08` | 벨그라드 | Belgrad | 요새 | 백사자 성채 | 성 방어전 시작 시 전위에게 방어 버프 부여 |
| `castle_09` | 루미나 | Lumina | 성역 | 광명의 대성당 | 부상 회복과 주민 안정에 유리 |
| `castle_10` | 에버란 | Everan | 도시 | 왕도 대로 | 인접 성 이동과 지원이 빠름 |
| `castle_11` | 그란델 | Grandel | 도시 | 대제국 은행 | 월간 금화 수입이 높지만 적 계략의 표적이 되기 쉬움 |
| `castle_12` | 세르반 | Servan | 요새 | 군단 훈련장 | `[일반]`의 합동 훈련 효과 증가 |
| `castle_13` | 알테아 | Althea | 성역 | 순례자의 길 | 매력 계열 임무와 외교에 유리 |
| `castle_14` | 베르크 | Berg | 요새 | 북부 관문 | 험지 이동 페널티 감소 |
| `castle_15` | 오르텔 | Ortel | 도시 | 푸른 포도원 | 연회와 인물 교류 효과 증가 |
| `castle_16` | 라디온 | Radion | 마도성 | 태양석 첨탑 | 빛 속성 장비와 결계 연구에 유리 |
| `castle_17` | 칼도르 | Kaldor | 요새 | 붉은 병기창 | 성채 정비 대성공 시 방어 장치 획득 |
| `castle_18` | 메르시아 | Mercia | 도시 | 남부 교역소 | 타 종족 인재 등용 불이익 감소 |
| `castle_19` | 하일렌 | Hylen | 도시 | 흰밀 평야 | 점령 피해에서 번영 회복이 빠름 |
| `castle_20` | 트리온 | Trion | 요새 | 삼중 성문 | 방어 사업의 최대 효과 증가 |
| `castle_21` | 아우렐 | Aurel | 도시 | 황제의 주조소 | 금화 수입과 장비 거래량 증가 |
| `castle_22` | 펠리온 | Pellion | 성역 | 승리의 기념비 | 승전 후 치안과 영향력 상승 |
| `castle_23` | 델카스 | Delcas | 요새 | 남부 감시탑 | 인접 적 성의 부대 정보를 한 단계 더 공개 |
| `castle_24` | 세라딘 | Seradin | 마도성 | 유리 천문대 | 적 마법과 날씨 관련 정보를 미리 확인 |
| `castle_25` | 벨로아 | Velloa | 도시 | 은빛 수로 | 영지 진흥의 치안 감소를 일부 상쇄 |

### 4.3 아이언하트 연맹 — 12개

| ID | 한글명 | 영문명 | 지역 인상 | 특산·랜드마크 | 내정 개성 |
|---|---|---|---|---|---|
| `castle_26` | 아이언홀드 | Ironhold | 수도 | 강철 의석 | 드워프와 수인 인재를 함께 배치할 때 협력 보너스 |
| `castle_27` | 카락둠 | Karak-Dum | 요새 | 지하 대성문 | 성 방어전에서 원거리 피해 감소 |
| `castle_28` | 프로스트혼 | Frosthorn | 요새 | 서리뿔 봉우리 | 추위와 설원 이동 페널티 무시 |
| `castle_29` | 앰버딥 | Amberdeep | 마도성 | 호박 광맥 | 마나 크리스탈과 제작 재료 동시 획득 가능 |
| `castle_30` | 스톤벨로 | Stonebellow | 도시 | 울림 대장간 | 중갑과 둔기 제작에 유리 |
| `castle_31` | 레드클로 | Redclaw | 요새 | 붉은 발톱 협곡 | 수인 근접 인재 모집과 훈련에 유리 |
| `castle_32` | 그레이메인 | Greymane | 성역 | 선조의 돌무덤 | 전투 불능 인물의 사망 확률 감소 |
| `castle_33` | 룬포지 | Runeforge | 마도성 | 고대 룬 화로 | 장비에 룬 효과를 부여할 수 있음 |
| `castle_34` | 브론즈게이트 | Bronzegate | 요새 | 청동 대문 | 성채 정비 비용 감소 |
| `castle_35` | 윈터팽 | Winterfang | 도시 | 설원 사냥터 | 보급이 부족할 때 받는 불이익 감소 |
| `castle_36` | 썬더크랙 | Thundercrag | 마도성 | 천둥 균열 | 번개 속성 영웅 스킬 연구에 유리 |
| `castle_37` | 울프덴 | Wolfden | 도시 | 대족장 시장 | `[일반]` 후보가 한 명 더 제시됨 |

### 4.4 실반로드 의회 — 12개

| ID | 한글명 | 영문명 | 지역 인상 | 특산·랜드마크 | 내정 개성 |
|---|---|---|---|---|---|
| `castle_38` | 실바리온 | Sylvarion | 수도 | 의회의 세계수 | 엘프와 드루이드 영웅의 충성이 흔들리기 어려움 |
| `castle_39` | 에일로렌 | Aeloren | 성역 | 은잎 성소 | 회복 사업과 자연 마법에 유리 |
| `castle_40` | 문셰이드 | Moonshade | 마도성 | 달그늘 연못 | 야간 전투와 환영 마법 연구에 유리 |
| `castle_41` | 윈드송 | Windsong | 도시 | 노래하는 수관 | 인물 교류와 명성 획득 증가 |
| `castle_42` | 그린베일 | Greenvale | 도시 | 영원의 과수원 | 번영과 보급 상태 회복에 유리 |
| `castle_43` | 스타브랜치 | Starbranch | 마도성 | 별가지 관측대 | 기술 사업에서 희귀 연구 단서 발견 가능 |
| `castle_44` | 리버윈 | Riverwyn | 도시 | 수정 강나루 | 아군 성 사이의 이동 지원 증가 |
| `castle_45` | 쏜워치 | Thornwatch | 요새 | 가시 장벽 | 성을 공격하는 적 근접 인물에게 지속 피해 |
| `castle_46` | 미스트우드 | Mistwood | 요새 | 안개 숲길 | 적의 조사와 원거리 공격 효율 감소 |
| `castle_47` | 엘더루트 | Elderroot | 성역 | 태고의 뿌리 | 영웅의 흔적 교체 시 기존 효과 일부 보존 |
| `castle_48` | 던글레이드 | Dawnglade | 도시 | 새벽 초원 | 치안이 높을 때 방문 인재 증가 |
| `castle_49` | 루나레스 | Lunareth | 마도성 | 월광 도서관 | 마나 소비 없이 낮은 등급 연구 하나 진행 가능 |

### 4.5 네크로폴리스 — 10개

| ID | 한글명 | 영문명 | 지역 인상 | 특산·랜드마크 | 내정 개성 |
|---|---|---|---|---|---|
| `castle_50` | 네크로폴리스 | Necropolis | 수도 | 영원의 묘궁 | 언데드 인재가 피로로 받는 불이익 무시 |
| `castle_51` | 모르가르 | Morgar | 요새 | 검은 뼈 성벽 | 성 방어전에서 전투 불능자가 망령으로 일시 귀환 |
| `castle_52` | 나이트베일 | Nightveil | 도시 | 영원한 야시장 | 희귀 물품 거래와 비밀 의뢰에 유리 |
| `castle_53` | 애쉬크라운 | Ashcrown | 요새 | 재의 왕관 | 점령과 파괴 후 방어 회복이 빠름 |
| `castle_54` | 벨모라 | Velmora | 마도성 | 피의 수정탑 | 체력을 대가로 마나 크리스탈 추가 생산 가능 |
| `castle_55` | 그림할로우 | Grimhollow | 도시 | 망자의 지하도시 | 인구가 낮아도 금화 수입 감소가 적음 |
| `castle_56` | 오브시디아 | Obsidia | 요새 | 흑요석 절벽 | 화염 및 마법 피해에 강한 성벽 |
| `castle_57` | 녹스미르 | Noxmere | 성역 | 검은 거울 호수 | 적 계략을 반사하는 특별 사건 발생 가능 |
| `castle_58` | 드레드스파이어 | Dreadspire | 마도성 | 공포의 첨탑 | 디버프와 소환 스킬 연구에 유리 |
| `castle_59` | 라스트리움 | Lastrium | 도시 | 황혼의 항구 | 중립·추방 인재와 포로 교환에 유리 |

## 5. 내정 화면 구조

내정 화면의 정식 한글 이름은 **영지 관리**다.

### 5.1 진입 화면

| UI ID | 한글명 | 영문명 | 기능 |
|---|---|---|---|
| `realm_overview` | 영지 현황 | Realm Overview | 모든 성의 상태, 위험과 월간 사업을 요약 |
| `castle_management` | 성 관리 | Castle Management | 선택한 성의 수치, 사업과 담당자 관리 |
| `personnel` | 인재 관리 | Personnel | 소속 인물의 배치, 상태, 공적과 충성 확인 |
| `facilities` | 시설 | Facilities | 성의 기본 및 특화 시설 확인과 선택 |
| `tavern` | 선술집 | Tavern | 의뢰, 소문과 방문 인재 확인 |
| `delegation` | 태수 위임 | Delegation | 태수, 운영 방침과 월간 예산 설정 |
| `monthly_report` | 월보 | Monthly Report | 지난달의 내정, 인물과 사건 결과 확인 |

### 5.2 성 관리 탭

| UI ID | 한글명 | 영문명 | 표시 내용 |
|---|---|---|---|
| `castle_summary` | 성 개요 | Castle Summary | 소유 세력, 규모, 태수, 특성, 수입과 위험 |
| `castle_focus` | 중점 사업 | Monthly Project | 이번 달 사업과 담당 인물 지정 |
| `castle_roster` | 주둔 인재 | Stationed Characters | 성에 소속되거나 체류 중인 인물 |
| `castle_party` | 주둔 부대 | Garrison | 방어 부대와 출정 가능한 부대 |
| `castle_legacy` | 영웅의 흔적 | Heroic Legacies | 성에 남은 영웅의 영구 효과 |
| `castle_history` | 성의 기록 | Castle Chronicle | 점령, 확장, 사업과 사건 이력 |

`중점 사업`의 영문명은 직역인 `Focus`보다 실제 업무 단위라는 의미를 전달하기 위해 `Monthly Project`를 사용한다.

## 6. 내정 수치 명칭

| 데이터 ID | 한글명 | 영문명 | 상태 단계 |
|---|---|---|---|
| `prosperity` | 번영 | Prosperity | 황폐 / 정체 / 성장 / 번성 |
| `development` | 기술 | Development | 낙후 / 기초 / 발달 / 첨단 |
| `security` | 치안 | Security | 혼란 / 불안 / 안정 / 평온 |
| `defense` | 방어 | Defense | 붕괴 / 취약 / 견고 / 철벽 |

기술의 영문명은 마법 연구만을 의미하지 않도록 `Technology` 대신 `Development`를 사용한다. 한글 UI는 이해하기 쉬운 `기술`을 유지한다.

## 7. 중점 사업 명칭

### 7.1 기본 사업

| 데이터 ID | 한글명 | 영문명 | 한 줄 설명 | 주요 능력 |
|---|---|---|---|---|
| `project_prosperity` | 영지 진흥 | Promote Prosperity | 농업, 상업과 주민 생활을 함께 발전시킨다 | 정치 |
| `project_development` | 기술 개발 | Advance Development | 제작, 마법과 도시 기반 기술을 발전시킨다 | 지력·정치 |
| `project_security` | 민생 안정 | Secure the Realm | 범죄, 갈등과 첩자를 해결해 치안을 높인다 | 무력·매력 |
| `project_defense` | 성채 정비 | Fortify Castle | 성문, 장벽과 방어 장치를 보수한다 | 정치·무력 |
| `project_recruitment` | 인재 발굴 | Seek Talent | `[일반]`, 재야 영웅과 등용 단서를 찾는다 | 매력 |
| `project_training` | 합동 훈련 | Joint Training | 주둔 인물과 부대의 숙련을 높인다 | 통솔 |
| `project_recovery` | 영지 회복 | Restore the Realm | 부상, 피로와 점령 피해를 회복한다 | 매력·지력 |
| `project_expansion` | 성 확장 | Expand Castle | 성의 규모와 시설 슬롯을 확장한다 | 정치 |

### 7.2 특화 시설 사업

| 데이터 ID | 한글명 | 영문명 | 필요 시설 |
|---|---|---|---|
| `project_crafting` | 장비 제작 | Craft Equipment | 대공방 |
| `project_arcane` | 마도 연구 | Arcane Research | 마법 탑 |
| `project_trade` | 대규모 교역 | Grand Trade | 대시장 |
| `project_order` | 기사단 훈련 | Order Training | 기사단 |
| `project_expedition` | 모험대 파견 | Adventurer Expedition | 모험가 길드 |
| `project_harmony` | 종족 화합 | Foster Harmony | 성소 |
| `project_intelligence` | 첩보 작전 | Intelligence Operation | 첩보원 거점 |
| `project_diplomacy` | 사절 파견 | Dispatch Envoy | 사절관 |

## 8. 세력 방침 명칭

| 데이터 ID | 한글명 | 영문명 | 효과 방향 |
|---|---|---|---|
| `policy_wealth` | 부국 | Prosperity | 번영 사업과 금화 수입 강화 |
| `policy_growth` | 개발 | Development | 기술 사업과 성 확장 강화 |
| `policy_order` | 안정 | Stability | 치안과 점령지 회복 강화 |
| `policy_defense` | 수비 | Defense | 성 방어와 부상 회복 강화 |
| `policy_campaign` | 원정 | Campaign | 부대 훈련, 이동과 공격 준비 강화 |
| `policy_talent` | 인재 | Talent | 탐색, 등용과 관계 활동 강화 |

## 9. 태수 위임 방침 명칭

| 데이터 ID | 한글명 | 영문명 | 판단 기준 |
|---|---|---|---|
| `delegate_balanced` | 균형 운영 | Balanced | 가장 낮은 성 수치를 우선 |
| `delegate_prosperity` | 부유한 도시 | Prosperity | 번영과 금화 수입을 우선 |
| `delegate_research` | 마도 연구 | Research | 기술, 마나와 제작을 우선 |
| `delegate_frontier` | 전선 요새 | Frontier | 치안, 방어와 훈련을 우선 |
| `delegate_talent` | 인재 집결 | Talent | 인재 발굴, 등용과 회복을 우선 |

## 10. 인재 활동 명칭

| 데이터 ID | 한글명 | 영문명 | 기능 |
|---|---|---|---|
| `activity_search` | 탐색 | Search | 인재, 물품, 소문과 사건 발견 |
| `activity_interact` | 교류 | Interact | 두 인물의 관계 개선 |
| `activity_recruit` | 등용 | Recruit | 발견한 인재를 세력에 영입 |
| `activity_train` | 개인 훈련 | Personal Training | 능력 경험과 클래스 숙련 획득 |
| `activity_rest` | 휴식 | Rest | 피로와 부상 회복 |
| `activity_equip` | 장비 정비 | Manage Equipment | 제작, 강화, 수리와 장비 교체 |
| `activity_reward` | 포상 | Reward | 공적에 맞춰 금화 또는 장비 지급 |
| `activity_title` | 작위 수여 | Grant Title | 영향력을 사용해 작위와 지휘 권한 부여 |

## 11. 선술집 메뉴 명칭

| UI ID | 한글명 | 영문명 | 기능 |
|---|---|---|---|
| `tavern_requests` | 의뢰 게시판 | Request Board | 이번 달의 최대 3개 의뢰 확인 |
| `tavern_rumors` | 소문 듣기 | Hear Rumors | 인재, 사건과 적 성에 대한 단서 획득 |
| `tavern_visitors` | 방문객 | Visitors | 현재 선술집에서 만날 수 있는 인물 확인 |
| `tavern_gathering` | 연회 열기 | Hold Gathering | 여러 인물의 관계와 충성 개선 |
| `tavern_broker` | 중개상 | Broker | 희귀 물품과 비공식 정보 거래 |

## 12. 월보 분류 명칭

| UI ID | 한글명 | 영문명 | 표시 우선순위 |
|---|---|---|---|
| `report_urgent` | 긴급 보고 | Urgent Reports | 침공, 반란, 충성 위험과 보급 고갈 |
| `report_projects` | 사업 결과 | Project Results | 중점 사업의 성공, 실패와 변화량 |
| `report_characters` | 인재 소식 | Character News | 공적, 관계, 충성, 부상과 등용 |
| `report_diplomacy` | 대륙 정세 | World Affairs | 세력 변화, 외교와 전쟁 |
| `report_resources` | 영지 결산 | Realm Summary | 자원과 성 수치 증감 |
| `report_rumors` | 새로운 소문 | New Rumors | 의뢰, 방문 인재와 발견 가능 사건 |

## 13. 버튼 및 상태 문구

### 13.1 공통 버튼

| 한글명 | 영문명 | 용도 |
|---|---|---|
| 담당자 선택 | Assign Leader | 중점 사업 담당 인물 지정 |
| 협력자 선택 | Assign Partner | 협력 인물 지정 |
| 기본 투자 | Standard Funding | 표준 비용과 성과 적용 |
| 집중 투자 | Intensive Funding | 비용과 사건 확률을 높여 큰 성과 시도 |
| 사업 시작 | Begin Project | 이번 달 중점 사업 확정 |
| 이전 명령 반복 | Repeat Last Order | 지난달 사업과 담당자 재사용 |
| 태수에게 위임 | Delegate | 태수 방침으로 자동 처리 |
| 추천 배정 | Auto Assign | 적합한 인물 후보를 자동 선택 |
| 턴 진행 | End Month | 이번 달 결정을 확정하고 결과 처리 |

### 13.2 인물 상태

| 한글명 | 영문명 | 의미 |
|---|---|---|
| 대기 | Available | 새로운 임무에 배치 가능 |
| 내정 중 | On Project | 성의 중점 사업 수행 중 |
| 활동 중 | On Activity | 탐색, 교류, 훈련 등을 수행 중 |
| 이동 중 | Traveling | 성 사이를 이동 중 |
| 출정 중 | Deployed | 부대에 편성되어 원정 중 |
| 휴식 중 | Resting | 피로 또는 부상을 회복 중 |
| 포로 | Captured | 적 세력에 억류된 상태 |

### 13.3 결과 등급

| 한글명 | 영문명 | 의미 |
|---|---|---|
| 미흡 | Poor | 비용에 비해 낮은 성과 |
| 완료 | Complete | 예상 범위의 정상 성과 |
| 성공 | Success | 예상보다 높은 성과와 추가 보상 |
| 대성공 | Great Success | 영웅 선택지, 희귀 보상 또는 영웅의 흔적 가능 |

실패라는 표현은 투자 전체가 사라졌다는 인상을 주므로 일반적인 최저 결과에는 `미흡`을 사용한다. 사건에서 명확히 잘못된 선택을 했을 때만 `실패`를 표시한다.

## 14. 데이터 작성 규칙

- 성 ID는 저장 데이터와 연결되므로 `castle_00`~`castle_59`를 변경하지 않는다.
- 한글명과 영문명은 모두 필수로 입력한다.
- 한 세력 안에서 첫 음절과 어미가 지나치게 반복되지 않게 한다.
- 성마다 반드시 하나의 특산·랜드마크와 하나의 내정 개성을 가진다.
- 내정 개성은 단순 수치 보너스보다 규칙 변화 또는 새로운 선택지를 우선한다.
- 수도는 다른 성보다 강하지만 모든 분야에서 최고가 되지 않게 한다.
- UI 명칭은 한글 8자 이내를 권장하며 버튼은 동작을 나타내는 말로 작성한다.
- 코드 ID는 소문자 스네이크 표기법을 사용하고 저장 후 의미를 바꾸지 않는다.

## 15. 데이터 적용 우선순위

1. 60개 성의 한글·영문 이름을 데이터베이스에 반영한다.
2. 성별 지형 특성, 고유 특산과 세력별 개성을 데이터에 추가한다.
3. 번영, 기술, 치안, 방어와 성 규모를 추가한다.
4. 기본 중점 사업 8종과 UI 문자열을 추가한다.
5. 태수 위임 방침과 인재 활동을 추가한다.
6. 성별 특산·랜드마크와 내정 개성을 구현한다.
7. 특화 시설 사업과 영웅의 흔적을 구현한다.
