# Project WI 메모

-통합 데이터 매니저 - 각 SO가 떠있는데 그 SO의 에셋 위치로 컨텐츠 브라우저에서 가도록
-통합 데이터 매니저 - 던전 데이터에 던전 이벤트가 단순히 몇개 인지만 나오는데 드롭 다운 형태로 SO를 등록 할 수 있게 하면 좋음
-WIGameSettingDataSO (Assets/Data/ScriptableObject/WI_GameSettingData.asset)에서 각종 수치를 받아 오도록 할 예정
-UI Manager에 각 멤버 변수와 함수들의 역할에 대해 주석 필요
-모험가 배정을 던전에 들어 갈때 하므로 WIBattleManager에서 더미 캐릭터를 세팅 하는게 아니라 WIDungeonSession에서 해야함.
 현재 WIBattleManager에서 그냥 던전 세션을 만들었는데 실제로는 던전 입장 버튼을 눌러야 하므로 테스트 코드도 정상적으로 수정 필요
-모험가 획득 시스템 필요.
-데이터 저장 필요, 일단 디바이스에 그냥 저장(차후 클라우드 추가), 모험가 정보, 골드, 다이아, 던전 클리어 상황 등

