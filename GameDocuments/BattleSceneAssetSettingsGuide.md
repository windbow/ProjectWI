# ProjectWI 전투씬 에셋 설정 및 제작 가이드

**문서 역할:** 전투씬에서 사용하는 캐릭터, 지면, 배경 구조물, 머티리얼과 조명의 제작·임포트·배치 기준을 관리합니다.

**현재 기준일:** 2026-08-15

전투 에셋을 추가하거나 표시 규격을 변경할 때는 이 문서를 먼저 확인하고, 작업 완료 후 실제 구현값에 맞게 함께 갱신합니다. 과거 실험용 청크와 설정은 비교 자료일 뿐 현재 제작 기준으로 사용하지 않습니다.

## 1. 현재 전투씬 기준

- 아레스 기준 전투 Sprite는 `Assets/Art/Characters/Ares/Ares_Battle_1WU_A_OutlineBake_V1.png`이며 190×256, 256 PPU, Scale 1에서 외곽선 포함 가시 높이 정확히 1월드 유닛입니다.

| 항목 | 현재 기준 |
|---|---|
| 전투씬 | `Assets/Scenes/BattleScene.unity` |
| 전투 설정 | `Assets/Data/ScriptableObject/Battle/WI_BattleConfig.asset` |
| 현재 전장 프리팹 | `Assets/Prefabs/Battle/WIBattleCharacterScaleArenaV6.prefab` |
| 전투 판정 영역 | 18×10 월드 유닛 |
| 배경 표시·카메라 이동 범위 | 36×20.25 월드 유닛 |
| 카메라 줌 | A=6, B=8, C=10의 세 고정 단계 |
| 캐릭터 기준 키 | 약 1 월드 유닛 |
| 캐릭터 공통 Transform Scale | 1 |
| 현재 기준 캐릭터 | 아레스 |
| 현재 지면 | `Battle_CompleteArena_CharacterScale_V6_4K.png`에서 분할한 4청크 |
| 지면 표현 | 정적 환경물·접지 그림자를 포함한 4K 완성 그림 → 픽셀 무손실 2×2 청크 |
| 지면 조명 | Sprite Unlit |
| 전투씬 조명 | 백색 Global Light 2D 강도 1, 키·필 라이트 비활성 |
| 캐릭터 전장 표기 | 이름·등급·역할 및 HP 바 비활성 |
| 캐릭터 머티리얼 | A·B·C 모두 `WI_BattleCharacter_Unlit.mat` |
| 숨은 전투 좌표망 | 8방향 사각 셀, 폭 0.6·높이 0.3유닛 |

## 2. 공통 제작 원칙

### 2.1 캐릭터가 축척의 기준입니다

- 배경에 맞춰 캐릭터 크기를 임의로 변경하지 않습니다.
- 현재 아레스의 약 1유닛 높이를 전투 캐릭터 표시 기준으로 사용합니다.
- 성문, 텐트, 망루, 방책, 길 폭, 돌과 풀 크기는 캐릭터와 비교해 결정합니다.
- 새로운 캐릭터도 원본 이미지의 알파 여백을 정리한 뒤 가시 높이 1유닛에 맞는 PPU를 사용합니다.
- 영웅의 등급이나 중요도를 표현하기 위해 Transform 크기를 다르게 하지 않습니다. 등급 표현은 의상, 이펙트, UI와 실루엣으로 처리합니다.

### 2.2 노란 조명을 이미지에 굽지 않습니다

- GPT 이미지 생성에서 자주 나타나는 금색, 노란색, 주황색, 노을, 석양, 횃불 중심 색보정을 사용하지 않습니다.
- 기본 색감은 중립적인 대낮 또는 조명이 없는 알베도 이미지입니다.
- 정적 환경물에는 짧고 부드러운 접지 그림자와 제한된 AO만 함께 그립니다. 긴 방향성 그림자와 화면 전체를 누르는 AO는 사용하지 않습니다.
- 권장 색은 중립 회갈색 흙, 차가운 회색 석재, 절제된 자연 녹색입니다.
- 최종 입체감이 필요하면 별도 노멀맵과 URP 2D Light로 처리합니다.

### 2.3 반복 타일만으로 최종 지면을 만들지 않습니다

- 반복 타일은 프로토타입, 외곽 확장 또는 미세 디테일 보조 용도로만 사용합니다.
- 최종 전투 지면은 길, 마모, 잔디 경계와 공간 구도가 포함된 하나의 마스터 지면으로 제작합니다.
- 마스터 지면이 Unity 텍스처 제한보다 크면 완성 후 기계적으로 청크를 분할합니다.
- 청크별 AI 재생성은 구도, 축척, 색과 경계 불일치를 만들기 때문에 사용하지 않습니다.
- 성마다 60개 배경을 별도 제작하지 않고, 성벽·야영지·평원 등 소수의 완성형 전장 계열과 색·소품 변형으로 확장합니다.

## 3. 캐릭터 전투 Sprite 설정

### 3.0 숨은 8방향 사각 배치

- 배경 이미지는 타일로 자르지 않으며 독립된 완성 그림을 그대로 유지합니다.
- 숨은 사각 좌표는 캐릭터 발 위치, 셀 점유와 이동 목적지 예약만 담당하고 일반 화면에는 표시하지 않습니다.
- 현재 Cell Width는 0.6, Cell Height는 0.3유닛입니다.
- 상하좌우와 네 대각선의 8방향으로 이동해 화면 정면 상·하 이동과 자연스러운 우회가 모두 가능합니다.
- 1유닛 높이 캐릭터의 몸체가 인접 행과 겹치지만 발 위치는 서로 다른 셀 중심에 놓입니다.
- 이동은 인접 셀 중심 사이를 실시간 보간하고 도착 거리 0.015 안에서 좌표를 확정합니다.
- 근접 캐릭터는 대상 주변 여덟 셀 중 빈 공격 위치를 선택하고 최종 공격은 월드 거리로 판정합니다.
- 원거리 투사체와 광역 스킬은 격자 칸 수가 아니라 기존 월드 거리와 충돌을 사용합니다.
- 캐릭터 깊이는 기존과 동일하게 발 위치의 월드 Y로 정렬합니다.

### 3.1 현재 아레스 설정

| 항목 | 값 |
|---|---:|
| 원본 파일 | `Assets/Art/Characters/Ares/Ares_Battle_FullBody_V1.png` |
| 전투용 파일 | `Assets/Art/Characters/Ares/Ares_Battle_1WU_A_OutlineBake_V1.png` |
| Sprite Mode | Single |
| 원본 캔버스 | 1121×1403px |
| 원본 알파 경계 | 865×1176px |
| 전투용 Sprite | 190×256px |
| Pixels Per Unit | 256 |
| 가시 월드 표시 크기 | 약 0.734×1유닛 |
| Transform Scale | 1 |
| Filter Mode | Bilinear |
| Mipmap | 활성 |
| Alpha Is Transparency | 활성 |
| Texture Compression | None/Uncompressed |
| Max Texture Size | 2048 |
| Wrap Mode | Clamp |

### 3.2 큰 원화를 1유닛 전투용 Sprite로 변환하는 임시 표준

이 절차는 아레스 검증값을 기준으로 한 1차 표준입니다. 캐릭터 아트 방향이 확정되면 목표 픽셀 높이와 Mipmap 정책은 변경할 수 있지만, 모든 캐릭터에 동일한 계산을 적용해야 합니다.

1. 초상화와 전투용 파일을 분리하고 1121×1403 같은 고해상도 원본은 덮어쓰지 않습니다.
2. 원본을 RGBA로 열어 Alpha가 0보다 큰 픽셀의 최소 경계 상자를 계산합니다. 캔버스 높이가 아니라 이 실제 알파 경계 높이가 캐릭터 키입니다.
3. 경계 상자에는 머리카락, 망토, 무기를 포함하되 완전히 투명한 바깥 여백은 제거합니다. 아레스는 `(x 75~939, y 116~1291)`의 865×1176px 영역입니다.
4. 잘라낸 이미지는 종횡비를 유지하며 알파 실루엣 높이를 정확히 256px로 축소합니다. 아레스 결과는 `865×1176 → 188×256`입니다.
5. 반투명 가장자리의 검거나 흰 테두리를 막기 위해 RGB에 Alpha를 먼저 곱한 Premultiplied Alpha 상태에서 Lanczos로 축소한 뒤, Alpha가 0이 아닌 픽셀만 다시 Unpremultiply합니다.
6. 바깥 실루엣에만 중립 청흑색 `#10141A` 외곽선을 1px 84%, 다음 1px 50%, 마지막 1px 20% 알파로 베이크합니다. 캐릭터 내부의 갑옷 틈과 머리카락 사이 구멍에는 자동 외곽선을 만들지 않습니다.
7. 외곽선을 포함한 결과 전체를 다시 가시 높이 256px로 정규화합니다. 결과는 `<캐릭터명>_Battle_1WU_A_OutlineBake_V1.png` 형식으로 저장합니다.
8. Unity 임포트 설정은 Sprite/Single, 256 PPU, Bilinear, Mipmap 활성, Alpha Is Transparency 활성, Clamp, None/Uncompressed를 사용합니다.
9. 캐릭터 프리팹과 `WI_BattleConfig.battleSpriteScale`은 X/Y 모두 1을 유지합니다. 개별 캐릭터 크기를 Transform Scale로 보정하지 않습니다.
10. 발바닥 또는 대표 접지점이 프리팹 원점에 맞는지 확인합니다. 크롭 때문에 발 위치가 달라졌다면 Sprite Pivot을 Bottom Center 기준으로 통일하고 그림자 위치를 확인합니다.
11. A/B/C 모두 외곽선 셰이더 없이 Unlit 상태로 판독성, 가장자리 떨림과 Mipmap 축소 품질을 확인합니다.

계산 기준은 다음과 같습니다.

```text
가시 월드 높이 = 알파 실루엣 세로 픽셀 ÷ Pixels Per Unit × Transform Scale

현재 표준: 256px ÷ 256 PPU × Scale 1 = 1 World Unit
```

주의할 점:

- 256px은 파일 캔버스 높이가 아니라 실제 캐릭터 알파 실루엣의 목표 높이입니다. 투명 여백을 다시 추가하면 파일 높이와 월드 높이가 달라질 수 있습니다.
- 원본을 단순 축소할 때도 매우 가는 머리카락, 체인, 장식은 사라지거나 떨릴 수 있습니다. 이 경우 Transform을 키우지 말고 전투용 그림에서 해당 요소를 굵게 단순화합니다.
- 캐릭터마다 원본 크기가 달라도 최종 알파 실루엣 256px·PPU 256·Scale 1 조합은 동일하게 유지합니다.

### 3.3 축소 화면 품질 원칙

- 하나의 고해상도 이미지로 가까운 화면과 60인 전체 화면의 모든 세부를 동시에 보존할 수는 없습니다.
- 먼저 현재 Bilinear·Mipmap 활성 기준으로 실제 화면을 확인합니다.
- 지속적인 자글거림이 있으면 A/B/C 거리 단계와 원거리 전용 파생 Sprite를 검토합니다.
- 원거리 Sprite는 새로운 캐릭터 디자인이 아니라 원본 실루엣과 주요 색면을 단순화한 파생 에셋으로 제작합니다.
- 거리별 Sprite를 사용해도 월드 크기와 발 위치는 동일해야 합니다.

### 3.4 캐릭터 Y 깊이 정렬

- 화면 아래쪽, 즉 월드 Y가 더 작은 캐릭터가 앞에 표시됩니다.
- 현재 계산식은 `1000 - RoundToInt(worldY × 100)`입니다.
- 캐릭터가 이동할 때마다 `WIBattleCharacterView.Refresh()`에서 Sorting Order를 갱신합니다.
- 동일 Y에서는 같은 Sorting Order를 사용하며 전후 차이가 필요한 배치는 Y 위치로 구분합니다.
- 지면은 -110으로 유지해 모든 캐릭터보다 뒤에 표시합니다.

### 3.5 캐릭터 외곽선

- 현재 A·B·C 모두 일반 `WI_BattleCharacter_Unlit.mat`을 사용하며 외곽선은 Sprite에 직접 베이크합니다.
- 베이크 외곽선은 바깥 실루엣에만 `#10141A` 색으로 84%→50%→20% 알파를 사용합니다.
- 구형 `ProjectWI/Battle/Sprite Unlit Outline` 셰이더와 비교 재질 두 종은 현재 런타임에서 사용하지 않으며 `Assets/TrashAsset/Art/Battle/OutlineExperiment`에 복구용으로 보관합니다.
- 셰이더 방식은 머리카락·갑옷 틈의 알파 노이즈까지 경계로 검출해 확대 시 울퉁불퉁해졌으므로 현재 기준에서 제외합니다.

### 3.5.1 공용 접지 그림자

- 기본 에셋은 `Assets/Art/Battle/Effects/WI_CharacterShadow_Oval_V1.png`입니다.
- `WIBattleCharacter.prefab/GroundShadow`에 미리 배치하며 로컬 위치 `(0, -0.47, 0)`, Scale `(0.8, 0.5, 1)`을 기본값으로 사용합니다. 본체 SpriteRenderer의 Sorting Order보다 1 낮게 자동 동기화합니다.
- 256×128 RGBA, 256 PPU, Bilinear, Mipmap 비활성, Clamp, 무압축을 사용합니다.
- 방향성 그림자가 아니라 캐릭터 발밑 접촉감만 보조하는 대칭 타원형이며 중심 최대 알파는 약 51%입니다.
- 기본 월드 폭은 1유닛입니다. 캐릭터 체형 차이는 Sprite 교체보다 자식 Transform의 X Scale로 조절합니다.
- 캐릭터 Sprite보다 뒤, 지면보다 앞에 표시하고 캐릭터 기준 음수 오프셋 Sorting Order를 사용합니다.
- `WIBattleCharacter.prefab/GroundShadow`에 연결되어 있으며 캐릭터 본체보다 Sorting Order 1 낮게 표시됩니다.

### 3.5.2 환경물 접지 그림자

- 천막은 `Tent_1_Shadow_V1.png`, `Tent_3_Shadow_V1.png` 전용 Sprite를 사용합니다.
- 두 에셋은 384×192 RGBA, 100 PPU, Bilinear, Mipmap 비활성, Clamp, 무압축이며 최대 알파는 약 20%입니다.
- V4 프리팹에서 천막 자식 `GroundShadow`로 배치하고 Sorting Order -105를 사용합니다. 지면 청크는 -110, 천막 본체는 -99입니다.
- 천막 이동·배율을 자동으로 따라가되 위치·비율·알파는 자식에서 독립 조절합니다.
- 기본 전투 화면에서는 ShadowCaster2D나 URP 실시간 투영 그림자를 사용하지 않습니다. 향후 Normal Map은 표면 입체감에만 사용하고 접지 그림자와 병행합니다.

### 3.6 전투 카메라 A/B/C 단계

| 단계 | Orthographic Size | 외곽선 | 용도 |
|---|---:|---|---|
| A | 6 | 끔 | 근거리 원화 확인 |
| B | 8 | 끔 | 일반 전투 조작 |
| C | 10 | 켬 | 30대30 진형과 실루엣 확인 |

- 마우스 휠 한 번은 인접 단계 하나만 이동하며 연속 중간값은 사용하지 않습니다.
- `Home`은 참가자 초기 진형을 포함하는 자동 단계로 돌아갑니다.
- 플레이 모드에서는 `ProjectWI/Verification/Battle Zoom/A Near`, `B Middle`, `C Far` 메뉴로 비교합니다.
- 원본 텍스처 1 texel은 전체 전투 화면에서 약 0.06 화면 픽셀이므로 `_OutlinePixels = 1`을 화면상 1px로 해석하지 않습니다.
- 캐릭터 복제 SpriteRenderer를 추가하지 않으므로 Y 정렬값과 배치 구조는 기존과 동일합니다.

## 4. 지면 에셋 설정

### 4.1 현재 마스터 지면

| 항목 | 값 |
|---|---:|
| 제작 마스터 | `Assets/Art/Battle/GroundLayers/CharacterScaleArenaV3/Battle_GroundLayered_CharacterScale_V3_4K.png` |
| 마스터 해상도 | 3840×2160px |
| 런타임 청크 | `Battle_GroundLayered_CharacterScale_V3_TL/TR/BL/BR.png` |
| 청크 해상도 | 각각 1920×1080px |
| Pixels Per Unit | 106.6667 |
| 전체 월드 표시 크기 | 36×20.25유닛 |
| 청크 월드 표시 크기 | 각각 18×10.125유닛 |
| Sprite Mode | Single |
| Draw Mode | Simple |
| Filter Mode | Bilinear |
| Mipmap | 비활성 |
| Texture Compression | None/Uncompressed |
| Max Texture Size | 2048 |
| Wrap Mode | Clamp |
| Material | `WI_BattleGround_Unlit.mat` |

V3는 V2의 길과 집결지 배치를 참조해 중립 회갈색 중간톤, 작은 자갈·흙·잔디 질감과 길 가장자리 대비를 강화한 후보입니다. 방향성 조명과 그림자는 포함하지 않습니다. `CharacterScaleArenaV3/Source/bake_v3_ground_chunks.py`로 4K 마스터와 청크를 다시 만들 수 있고 생성 보고서의 `reconstructionPixelPerfect` 값으로 무손실 재조립 여부를 확인합니다.

### 4.2 2D 지형 레이어 구성

지면은 다음 레이어가 한 장의 그림처럼 연결되어야 합니다.

1. 기본 흙·자갈 알베도
2. 다져진 땅과 중앙 집결지
3. 성문에서 중앙으로 이어지는 주 경로
4. 좌우 또는 전투 목적에 맞는 보조 경로
5. 희박한 잔디와 흙 경계
6. 마모된 석재와 작은 파편
7. 바퀴 자국, 발자국과 이동 흔적
8. 작은 돌·풀·잔해 데칼

길은 직선 타일을 이어 붙이지 않습니다. 성문 앞에서 넓어지고 이동 방향에 따라 자연스럽게 갈라지는 경로 마스크로 제작합니다. 가장자리는 일정한 Feather가 아니라 잔디 침범, 자갈과 마모로 불규칙하게 끊어 줍니다.

### 4.3 지면 이미지 생성 프롬프트 필수 조건

이미지 생성 도구를 사용할 때 다음 조건을 프롬프트에 포함합니다.

```text
strict orthographic top-down terrain
neutral overcast midday or unlit albedo
flat even illumination
no directional light and no cast shadow
neutral gray-brown earth, cool gray stone, restrained natural green
no golden-hour yellow, orange, sepia, sunset or cinematic color grading
terrain only, no characters, buildings, UI or text
human character scale is about 2 units in a 36×20.25 unit battlefield
```

생성 결과에 큰 돌, 거대한 풀, 캔버스 전체를 가르는 균열, 반복되는 붓 자국이나 방사형 패턴이 있으면 사용하지 않습니다.

### 4.4 고해상도와 청크 분할

- 최종 목표는 일반 전투 줌에서 최소 1 화면 픽셀당 1 원본 픽셀 이상을 확보하는 것입니다.
- 36×20.25유닛 전장을 1920×1080 기준으로 표시할 경우 약 53.33px/unit이 필요합니다.
- 가까운 줌 6에서는 약 90px/unit이 필요하므로 최종 마스터는 약 3240×1823px 이상을 권장합니다.
- 제작 권장 규격은 3840×2160 또는 그 이상의 16:9 마스터입니다.
- 2048 제한을 넘으면 2×2 또는 필요한 수의 청크로 분할합니다.
- 모든 청크는 같은 PPU, Filter, Mipmap, Compression과 머티리얼을 사용합니다.
- 분할 전후 이미지를 재결합해 원본과 픽셀 단위로 비교합니다.

## 5. 환경물 에셋 설정

### 5.1 현재 축척 참고값

| 환경물 | 현재 월드 표시 크기 |
|---|---:|
| 성문 | 약 12.77×6.39유닛 |
| 텐트 | 약 3.27×3.69유닛 |
| 망루 | 약 3.38×4.51유닛 |
| 방책 | 약 3.04×3.04유닛 |

환경물 Sprite의 투명 캔버스 전체 크기와 실제 물체가 차지하는 크기는 다를 수 있습니다. 수치만 보지 말고 아레스 1유닛 실루엣을 바로 옆에 배치해 육안으로 검증합니다.

### 5.2 제작 및 배치 규칙

- 환경물은 투명 PNG와 교체 가능한 독립 GameObject로 제작합니다.
- 프리팹에서 직접 확인할 수 있도록 런타임 일회성 생성 코드를 사용하지 않습니다.
- 건물과 물체 자체에 노란 조명과 긴 그림자를 굽지 않습니다.
- 바닥 접촉부의 짧고 중립적인 접촉 음영은 알베도에 최소한으로 포함할 수 있습니다.
- 강한 방향성 그림자와 재질 입체감은 노멀맵과 URP 조명 단계에서 처리합니다.
- 중앙 18×10 전투 판정 영역을 가리는 대형 구조물은 피합니다.
- 주요 시설은 외곽을 구성하되 카메라 줌 10에서 화면 밖으로 잘리지 않는지 확인합니다.

## 6. 머티리얼과 URP 2D 라이팅

### 6.1 현재 무조명 기준

- 지면: `Assets/Art/Battle/Materials/WI_BattleGround_Unlit.mat`
- Shader: `Universal Render Pipeline/2D/Sprite-Unlit-Default`
- `Global Light 2D`: 백색, 강도 1
- `Arena Key Light 2D`: 비활성
- `Arena Fill Light 2D`: 비활성
- 그림자: 비활성

현재 단계에서는 지면의 원본 알베도와 캐릭터·환경물 축척을 판단하는 것이 목적입니다. 색이 잘못 보일 때 광원을 추가해 보정하지 말고 먼저 원본 이미지와 머티리얼을 확인합니다.

### 6.2 노멀맵 적용 단계

1. 지면과 환경물의 알베도 구도를 먼저 확정합니다.
2. 알베도와 동일한 UV·해상도의 노멀맵을 제작합니다.
3. Unity Sprite Editor의 Secondary Textures에 `_NormalMap`을 연결합니다.
4. 대상 SpriteRenderer를 Sprite Lit 머티리얼로 전환합니다.
5. 백색 Global Light를 유지한 채 약한 중립색 광원 하나로 노멀 반응을 확인합니다.
6. 노란색·주황색 광원은 사용하지 않습니다.
7. 노멀 강도가 바닥 무늬를 과장하거나 캐릭터보다 입체적으로 보이면 낮춥니다.
8. ShadowCaster2D는 대형 구조물에만 별도 검토하고 작은 지면 디테일에는 사용하지 않습니다.

## 7. 전장 프리팹 제작 절차

1. `WI_BattleConfig`에서 현재 전투 판정 영역과 배경 표시 범위를 확인합니다.
2. 아레스 또는 2유닛 높이의 기준 캐릭터를 배치해 축척 가이드를 만듭니다.
3. 성문·텐트·망루·방책을 단순 배치해 통로와 전투 공간을 확보합니다.
4. 배치 가이드를 기준으로 지면 마스터의 길과 마모 구도를 제작합니다.
5. 2048 제한 이하이면 지면 Sprite를 `GroundTile`에 연결하고, 초과하면 완성 마스터를 기계적으로 분할해 동일 PPU의 `GroundChunkTL/TR/BL/BR`에 연결합니다.
6. 환경물은 독립 SpriteRenderer 자식으로 배치합니다.
7. 지면, 환경물, 캐릭터의 Sorting Order를 확인합니다.
8. 프리팹을 `Assets/Prefabs/Battle` 아래 버전 이름으로 저장합니다.
9. `WI_BattleConfig.arenaPrefab`과 `arenaBackgroundSize`를 갱신합니다.
10. 기존 비교용 프리팹은 삭제하지 않습니다.

## 8. 검증 절차

### 8.1 화면 검증

- 1920×1080 또는 실제 목표 해상도를 사용합니다.
- 줌 6에서 지면과 캐릭터가 과도하게 확대되거나 흐려지지 않는지 확인합니다.
- 줌 10에서 배경 외곽이나 카메라 배경색이 노출되지 않는지 확인합니다.
- 캐릭터 높이가 시설물·길·돌·풀과 자연스럽게 맞는지 확인합니다.
- 길과 잔디 경계가 반복 타일처럼 보이지 않는지 확인합니다.
- 화면에 노란색·주황색 전체 색조가 생기지 않는지 확인합니다.
- 지면이 캐릭터보다 명암과 디테일이 강해 가독성을 해치지 않는지 확인합니다.

### 8.2 전투 밀도 검증

- 소규모 전투와 30대30 전투를 모두 실행합니다.
- 30대30 검증 메뉴는 `ProjectWI/Verification/Start 30v30 Battle Density Test`입니다.
- 캐릭터 실루엣과 체력바가 지면 위에서 구분되는지 확인합니다.
- 이름·등급·역할 텍스트 중첩은 지면 품질과 별개의 UI 가독성 문제로 기록합니다.
- 순수 맵·캐릭터 실루엣 검증 시 `WI_BattleConfig.showCharacterLabels`와 `showCharacterHealthBars`를 `false`로 사용합니다.

### 8.3 기술 검증

- Unity 컴파일이 완료됐는지 확인합니다.
- Console의 Error를 확인합니다.
- Sprite PPU, Mipmap, Filter, Compression과 Wrap Mode를 확인합니다.
- 프리팹 자식이 활성 상태인지 확인합니다.
- SpriteRenderer의 Sprite와 Material 참조가 비어 있지 않은지 확인합니다.
- 씬에 Main Camera와 Global Light 2D를 유지합니다.

## 9. 파일과 이름 규칙

권장 경로와 이름은 다음과 같습니다.

```text
Assets/Art/Characters/<CharacterId>/<CharacterId>_Battle_<Purpose>_V#.png
Assets/Art/Battle/GroundLayers/<ArenaId>/Battle_Ground_<ArenaId>_V#.png
Assets/Art/Battle/Environment/Modules/<ModuleName>_V#.png
Assets/Art/Battle/Materials/WI_Battle<Purpose>_<LitOrUnlit>.mat
Assets/Prefabs/Battle/WIBattle<ArenaName>ArenaV#.prefab
Assets/Screenshots/Battle_<ArenaName>_<Verification>.png
```

- 기존 파일을 덮어쓰기보다 V2, V3처럼 버전을 올립니다.
- 생성 원본, 최종 에셋과 검증 캡처의 역할을 이름으로 구분합니다.
- 사용하지 않는 RND 에셋을 현재 설정에 연결하지 않습니다.

## 10. 현재 후속 과제

- `Battle_GroundLayered_CharacterScale_V4_4K`의 실제 30대30 구도·밝기·색감 승인
- 현재 4K 마스터와 2×2 무손실 청크의 장기 사용 여부 확정
- 외곽선 16 texel 기본값의 실제 기기 성능과 모바일 해상도 검증
- V4 중앙 집결지에 진형 가이드가 필요할지 검토
- `Tent_1_NeutralDay_V2`, `Tent_3_NeutralDay_V2`의 실제 전투 화면 대비 승인
- 성문 석재와 망루 목재의 스튜디오 하이라이트·후광 제거
- 캐릭터·환경물용 노멀맵 필요성 검토
- A/B/C 카메라 단계와 원거리 캐릭터 Sprite 필요성 별도 검증

