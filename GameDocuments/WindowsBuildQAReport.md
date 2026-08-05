# ProjectWI Windows 빌드 검증 보고서

검증일: 2026-08-03  
Unity: 6000.3.10f1  
대상: Windows x86-64 개발 빌드

## 빌드 결과

- 실행 파일: `Builds/Windows/ProjectWI.exe`
- 포함 씬: `MainScene`, `BattleScene`
- 클린 빌드 시간: 78.23초
- 전체 빌드 크기: 208.06MB
- 빌드 오류: 0건
- 빌드 경고: 0건

## 실행 검증

- 1366×768 창 모드에서 일반 실행 후 프로세스 응답 정상
- 일반 실행 Player 로그의 예외·오류·크래시 표식 0건
- `-wi-smoke-test` 자동 검증 종료 코드 0
- 새 표준 캠페인 생성 정상
- 1개월 턴 연산 후 제2턴 진입 정상
- 60개 성 상태 유지
- 실제 Player 영속 경로의 파일 저장·불러오기 왕복 정상
- `BattleScene` 로드, 양측 임시 인물 초기화와 시뮬레이션 진행 확인
- 전투 결과 처리 후 `MainScene` 전략 UI 복귀 확인
- 검증용 임시 저장 파일 정리 완료
- 스모크 로그의 실패·예외 표식 0건

성공 표식:

```text
[WI_BUILD_SMOKE_PASS] 턴 2 · 성 60 · 저장/불러오기 · 전투 진입/복귀 정상
```

## 재검증 방법

```powershell
Builds/Windows/ProjectWI.exe -batchmode -nographics -wi-smoke-test -logFile Builds/Windows/SmokeTest.log
```

프로세스 종료 코드가 0이고 로그에 `[WI_BUILD_SMOKE_PASS]`가 한 번 기록되면 통과입니다.
