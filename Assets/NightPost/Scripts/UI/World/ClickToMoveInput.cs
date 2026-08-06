using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NightPost.UI
{
    /// <summary>
    /// 빈 바닥을 클릭·탭하면 그 지점으로 플레이어가 걸어가게 한다(기획서 §3-1).
    ///
    /// 시설(InteractableStation)은 스스로 IPointerClickHandler로 클릭을 받아
    /// "앞까지 이동 후 UI 열기"를 처리하므로 여기서 건드리지 않는다.
    /// EventSystem이 잡아낸 대상(UI 또는 시설) 위를 눌렀다면 이 스크립트는 물러난다.
    /// 즉 이 스크립트가 반응하는 건 "아무것도 없는 바닥"뿐이다.
    ///
    /// 좌우 버튼(RequestManualMove)과 공존한다. 마지막 입력이 이긴다
    /// (MoveTo가 수동 입력을 지우고, 수동 입력이 들어오면 자동 이동이 취소된다).
    ///
    /// 이동 범위 제한은 PlayerMovement.MoveTo가 Clamp로 처리하므로
    /// 화면 밖을 눌러도 갈 수 있는 가장 가까운 지점까지만 이동한다.
    /// </summary>
    public class ClickToMoveInput : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        [Tooltip("비우면 Camera.main을 쓴다.")]
        [SerializeField] private Camera _camera;

        [Tooltip("끄면 클릭 이동이 동작하지 않는다(연출 컷 등에서 사용).")]
        [SerializeField] private bool _enabled = true;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_playerController == null) Debug.LogError("[ClickToMove] PlayerController 미연결", this);
            if (_camera == null) Debug.LogError("[ClickToMove] 카메라를 찾지 못했다", this);
        }

        private void Update()
        {
            if (!_enabled || _playerController == null || _camera == null) return;
            if (!TryGetClickPosition(out Vector2 screenPos)) return;

            // UI 위나 시설 위를 눌렀다면 그쪽이 처리한다.
            // (Physics2DRaycaster가 있으면 시설도 EventSystem 대상으로 잡힌다)
            if (IsPointerOverEventSystemObject()) return;

            // 카메라에서 월드 평면(z=0)까지의 거리로 변환한다.
            Vector3 world = _camera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -_camera.transform.position.z));

            _playerController.RequestAutoMove(world.x);
        }

        /// <summary>이번 프레임에 눌림이 있었으면 화면 좌표를 돌려준다.</summary>
        private static bool TryGetClickPosition(out Vector2 position)
        {
            position = default;

#if ENABLE_INPUT_SYSTEM
            // 터치 우선(모바일). 손가락이 화면에 처음 닿은 순간만 받는다.
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    position = touch.position.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began) return false;
                position = touch.position;
                return true;
            }

            if (!Input.GetMouseButtonDown(0)) return false;
            position = Input.mousePosition;
            return true;
#else
            return false;
#endif
        }

        /// <summary>포인터가 UI나 시설 위에 있는지. 그렇다면 바닥 클릭이 아니다.</summary>
        private static bool IsPointerOverEventSystemObject()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;

#if ENABLE_INPUT_SYSTEM
            // 터치는 손가락 ID로 물어봐야 정확하다.
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return es.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
                return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
            return es.IsPointerOverGameObject();
        }

        /// <summary>클릭 이동을 켜고 끈다.</summary>
        public void SetEnabled(bool value) => _enabled = value;
    }
}
