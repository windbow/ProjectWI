using System.Collections.Generic;
using UnityEngine;

namespace ProjectWI.Battle
{
    [RequireComponent(typeof(Camera))]
    public class WIBattleCameraController : MonoBehaviour
    {
        private Camera battleCamera;
        private WIBattleConfigSO config;
        private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();

        // 전투 설정을 연결하고 PC 전투용 직교 카메라 기본 시점을 구성합니다.
        public void Initialize(WIBattleConfigSO battleConfig)
        {
            config = battleConfig;
            battleCamera = GetComponent<Camera>();
            battleCamera.orthographic = true;
            ResetView();
        }

        // UI Toolkit 키 입력의 누름·해제 상태를 카메라 이동 상태로 기록합니다.
        public void SetMoveKey(KeyCode keyCode, bool pressed)
        {
            if (pressed) heldKeys.Add(keyCode);
            else heldKeys.Remove(keyCode);
        }

        // 마우스 휠 방향에 따라 직교 카메라 크기를 제한 범위 안에서 변경합니다.
        public void Zoom(float wheelDelta)
        {
            if (battleCamera == null || config == null) return;
            battleCamera.orthographicSize = Mathf.Clamp(
                battleCamera.orthographicSize + wheelDelta * config.CameraZoomSpeed,
                config.CameraMinimumZoom,
                config.CameraMaximumZoom);
            ClampToArena();
        }

        // 카메라 위치와 배율을 전장 중앙의 기본값으로 복원합니다.
        public void ResetView()
        {
            if (battleCamera == null || config == null) return;
            battleCamera.orthographicSize = config.CameraMaximumZoom;
            transform.position = new Vector3(0f, 0f, -10f);
            ClampToArena();
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
            float limitX = Mathf.Max(0f, config.ArenaSize.x * 0.5f - horizontalExtent);
            float limitY = Mathf.Max(0f, config.ArenaSize.y * 0.5f - verticalExtent);
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
