# ProjectWU UI 구성 세팅 매뉴얼

## 1. 메뉴 레벨업 UI (좌측 패널) 세팅
메뉴의 레벨을 올리기 위한 전용 UI 창을 좌측 패널 형태로 구성하는 방법입니다.

### 1-1. UI 구조 잡기
1. Hierarchy 창에서 `MainCanvas` 아래에 만들어진 임시 오브젝트 **`MenuUpgradeUI`** 를 선택합니다.
2. 컴포넌트로 `Image`와 `Vertical Layout Group`, `Content Size Fitter` 등을 추가하여 좌측 패널 디자인을 구성합니다.
   - **Rect Transform**: 좌측에 고정되도록 Anchor를 설정하세요 (예: Min X: 0, Max X: 0.3, Min Y: 0, Max Y: 1).
3. `MenuUpgradeUI` 아래에 각 메뉴별 업그레이드 정보를 보여줄 **Slot (버튼, 텍스트, 아이콘 포함)** 템플릿(프리팹)을 만듭니다.

### 1-2. 스크립트 연결
1. **`MenuUpgradeUI`** 오브젝트에 제공된 `WUMenuUpgradeUI` 컴포넌트를 추가합니다.
2. 만들어둔 Slot 프리팹을 Project 창에 저장한 뒤, `WUMenuUpgradeUI`의 `Slot Prefab` 필드에 할당합니다.
3. `Menu Layout Group` 필드에는 Slot들이 생성될 부모 Transform(예: `Vertical Layout Group`이 있는 오브젝트)을 연결합니다.
4. Slot 프리팹 최상단에는 `WUMenuUpgradeSlotUI` 컴포넌트를 붙이고, 내부의 텍스트(이름, 레벨, 가격)와 버튼(업그레이드 버튼) UI 요소들을 각각 연결해줍니다.

### UI 작동 방식
- `WUMenuUpgradeUI`는 시스템에 해금되어 있는 메뉴 목록을 불러옵니다.
- 각 메뉴별로 Slot을 생성하고, 버튼 클릭 시 `WUDataManager.LevelUpMenu()`가 호출되면서 메뉴 레벨이 오릅니다.
