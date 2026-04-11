# Project Status
**Last Updated:** 2026-04-11

## 1. Implemented Features (구현 완료)
### Core System

### Graphics & UI
- MainCanvas 추가 및 모바일 세로 화면(1080x1920) 기준 UI 세팅 완료
- 화면 하단 중앙 정렬(BottomPanel, HorizontalLayoutGroup 사용)된 기본 메뉴 버튼 5개 추가 및 배경 이미지 적용
- 화면 상단 TopPanel, 중앙 영역을 덮는 MainPanel 추가 완성
- MainPanel 내부에 ScrollRect 및 Viewport, Content를 추가하여 스와이프 가능한 5개의 UI 페이지 준비 완료
- BottomPanel 버튼 5개 텍스트 및 오브젝트 이름 변경 (HQ, Adventurer, Shop, Adventure, Raid)
- WIUIManager 스크립트를 작성하고 MainCanvas에 부착하여 하단 버튼 클릭 시 부드럽게 스크롤하며 페이지가 전환되도록 연동 완료
- 모험(Adventure) 페이지 내부에 임시 던전 진입 버튼(던전 1) 추가
- 메인 화면을 덮는 독립적인 Combat Page(전투 화면 오버레이) 추가, 뒤로가기 버튼과 함께 스크립트에 연동하여 띄우도록 설정
- 전투 화면 내 레이아웃 구조 세분화 완료: 상단 필드 패널(캐릭터 및 배경용), 중단 게이지 패널(적/아군 턴 대기 프로그레스바), 하단 로그 패널(ScrollRect 형태의 텍스트 배틀 로그) 구성
- 방치형 헤드리스(Headless) 백그라운드 전투 구조 완성 (WIBattleSession에서 데이터 파이프로 독립 Tick 처리, WIBattleManager가 틱 관리, WIBattleSubSystem이 옵저버 형태로 UI 연동)
- 배틀 캐릭터 프리팹(BattleCharacter) 생성 및 Combat Page 내 모험가/몬스터 스팟 스폰 적용 완료
- `던전 1` 버튼 클릭 시 UI가 현재 백그라운드 전투 데이터를 관측하도록 WIUIManager 연동 완료

### Actor (Customer)
- WICharacterBase, WIAdventurer, WIMonster 클래스 설계 및 구조 작성 완료
- 직업 시스템 및 스킬 데이터 구조화를 위해 WIJob(3개 스킬 보유), WISkill(스킬 ID, 쿨다운 등) 클래스 추가 및 캐릭터에 연결 완료
- 메모리 변조를 막기 위한 간이 데이터 보안 구조(WIObscuredInt, WIObscuredFloat) 개발 밑 적용 완료
- 레벨, 공격력, 방어력, 속도 등 기획서 기반 필수 스테이터스 프로퍼티 추가

### Data

