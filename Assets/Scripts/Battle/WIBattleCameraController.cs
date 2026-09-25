using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace ProjectWI.Battle
{
    [RequireComponent(typeof(Camera))]
    public class WIBattleCameraController : MonoBehaviour
    {
        private Camera battleCamera;
        // 범위 선택의 월드→패널 좌표 변환에 사용하는 전투 카메라입니다.
        public Camera BattleCamera => battleCamera;
        private WIBattleConfigSO config;
        private float framedOrthographicSize;
        private WIBattleZoomLevel framedZoomLevel = WIBattleZoomLevel.C;
        private WIBattleZoomLevel currentZoomLevel = WIBattleZoomLevel.C;
        private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();

        public WIBattleZoomLevel CurrentZoomLevel => currentZoomLevel;
        public event Action<WIBattleZoomLevel> ZoomLevelChanged;

        // 전투 설정을 연결하고 PC 전투용 직교 카메라 기본 시점을 구성합니다.
        public void Initialize(WIBattleConfigSO battleConfig)
        {
            config = battleConfig;
            battleCamera = GetComponent<Camera>();
            battleCamera.orthographic = true;
            currentZoomLevel = WIBattleZoomLevel.C;
            framedZoomLevel = currentZoomLevel;
            framedOrthographicSize = GetZoomSize(currentZoomLevel);
            ResetView();
        }

        // 참가 인원과 초기 진형 범위를 기준으로 전투 시작 시 필요한 줌아웃 배율을 계산합니다.
        public void FrameCombatants(IReadOnlyList<WIBattleCharacterState> characters)
        {
            if (battleCamera == null || config == null || characters == null || characters.Count == 0)
            {
                return;
            }

            float minimumX = characters.Min(character => character.Position.x);
            float maximumX = characters.Max(character => character.Position.x);
            float minimumY = characters.Min(character => character.Position.y);
            float maximumY = characters.Max(character => character.Position.y);
            float verticalSize = (maximumY - minimumY) * 0.5f + 2f;
            float horizontalSize = ((maximumX - minimumX) * 0.5f + 2f) / Mathf.Max(0.1f, battleCamera.aspect);
            float densityRatio = Mathf.InverseLerp(2f, 60f, characters.Count);
            float densitySize = Mathf.Lerp(config.CameraMinimumZoom, config.CameraMaximumZoom, densityRatio);
            float requiredSize = Mathf.Clamp(
                Mathf.Max(verticalSize, horizontalSize, densitySize),
                config.CameraMinimumZoom,
                config.CameraMaximumZoom);
            currentZoomLevel = GetContainingZoomLevel(requiredSize);
            framedZoomLevel = currentZoomLevel;
            framedOrthographicSize = GetZoomSize(currentZoomLevel);
            ResetView();
            ZoomLevelChanged?.Invoke(currentZoomLevel);
        }

        // UI Toolkit 키 입력의 누름·해제 상태를 카메라 이동 상태로 기록합니다.
        public void SetMoveKey(KeyCode keyCode, bool pressed)
        {
            if (pressed) heldKeys.Add(keyCode);
            else heldKeys.Remove(keyCode);
        }

        // 마우스 휠 방향에 따라 A/B/C 세 단계 중 인접한 줌으로 이동합니다.
        public void Zoom(float wheelDelta)
        {
            if (battleCamera == null || config == null) return;
            int direction = wheelDelta > 0f ? 1 : wheelDelta < 0f ? -1 : 0;
            if (direction == 0) return;
            int nextIndex = Mathf.Clamp((int)currentZoomLevel + direction, 0, 2);
            SetZoomLevel((WIBattleZoomLevel)nextIndex);
        }

        // 지정한 A/B/C 줌 단계로 즉시 이동하고 단계 변경을 알립니다.
        public void SetZoomLevel(WIBattleZoomLevel zoomLevel)
        {
            if (battleCamera == null || config == null || zoomLevel == currentZoomLevel) return;
            currentZoomLevel = zoomLevel;
            battleCamera.orthographicSize = GetZoomSize(currentZoomLevel);
            ClampToArena();
            ZoomLevelChanged?.Invoke(currentZoomLevel);
        }

        // 카메라 위치와 배율을 전장 중앙의 기본값으로 복원합니다.
        public void ResetView()
        {
            if (battleCamera == null || config == null) return;
            currentZoomLevel = framedZoomLevel;
            battleCamera.orthographicSize = framedOrthographicSize;
            transform.position = new Vector3(0f, 0f, -10f);
            ClampToArena();
            ZoomLevelChanged?.Invoke(currentZoomLevel);
        }

        // 설정된 A/B/C 단계의 직교 카메라 크기를 반환합니다.
        private float GetZoomSize(WIBattleZoomLevel zoomLevel)
        {
            if (zoomLevel == WIBattleZoomLevel.A) return config.CameraMinimumZoom;
            if (zoomLevel == WIBattleZoomLevel.B) return config.CameraMiddleZoom;
            return config.CameraMaximumZoom;
        }

        // 필요한 화면 범위를 모두 포함하는 가장 가까운 A/B/C 줌 단계를 반환합니다.
        private WIBattleZoomLevel GetContainingZoomLevel(float requiredSize)
        {
            if (requiredSize <= config.CameraMinimumZoom) return WIBattleZoomLevel.A;
            if (requiredSize <= config.CameraMiddleZoom) return WIBattleZoomLevel.B;
            return WIBattleZoomLevel.C;
        }

        // 화면 좌표를 캐릭터 선택에 사용할 전장 월드 좌표로 변환합니다.
        public Vector2 ScreenToBattlePosition(Vector2 screenPosition)
        {
            if (battleCamera == null) return Vector2.zero;
            Vector3 world = battleCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -battleCamera.transform.position.z));
            return new Vector2(world.x, world.y);
        }

        // 현재 확대율과 화면 비율을 고려해 카메라 위치를 전장 바깥으로 나가지 않게 제한합니다.
        public Vector2 GetClampedPosition(Vector2 requestedPosition)
        {
            if (battleCamera == null || config == null) return requestedPosition;
            float verticalExtent = battleCamera.orthographicSize;
            float horizontalExtent = verticalExtent * battleCamera.aspect;
            float limitX = Mathf.Max(0f, config.ArenaBackgroundSize.x * 0.5f - horizontalExtent);
            float limitY = Mathf.Max(0f, config.ArenaBackgroundSize.y * 0.5f - verticalExtent);
            return new Vector2(
                Mathf.Clamp(requestedPosition.x, -limitX, limitX),
                Mathf.Clamp(requestedPosition.y, -limitY, limitY));
        }

        // 누르고 있는 WASD·방향키를 조합해 매 프레임 카메라를 이동합니다.
        private void Update()
        {
            if (config == null) return;
            Vector2 direction = Vector2.zero;
            if (heldKeys.Contains(KeyCode.A) || heldKeys.Contains(KeyCode.LeftArrow)) direction.x -= 1f;
            if (heldKeys.Contains(KeyCode.D) || heldKeys.Contains(KeyCode.RightArrow)) direction.x += 1f;
            if (heldKeys.Contains(KeyCode.S) || heldKeys.Contains(KeyCode.DownArrow)) direction.y -= 1f;
            if (heldKeys.Contains(KeyCode.W) || heldKeys.Contains(KeyCode.UpArrow)) direction.y += 1f;
            if (direction.sqrMagnitude <= 0f) return;
            Vector2 next = (Vector2)transform.position + direction.normalized * config.CameraPanSpeed * Time.unscaledDeltaTime;
            Vector2 clamped = GetClampedPosition(next);
            transform.position = new Vector3(clamped.x, clamped.y, -10f);
        }

        // 현재 카메라 좌표를 전장 경계 안으로 즉시 보정합니다.
        private void ClampToArena()
        {
            Vector2 clamped = GetClampedPosition(transform.position);
            transform.position = new Vector3(clamped.x, clamped.y, -10f);
        }
    }
}
