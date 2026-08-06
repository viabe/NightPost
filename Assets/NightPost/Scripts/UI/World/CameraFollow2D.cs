using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 플레이어를 좌우로 따라가는 2D 카메라.
    ///
    /// 배경이 화면보다 넓어져 플레이어가 화면 밖으로 나가는 문제를 해결한다.
    /// 세로는 고정하고 가로만 따라가며, 배경 경계를 넘어가 빈 공간이 보이지 않도록
    /// 카메라 X를 배경 안쪽으로 제한한다.
    ///
    /// 플레이어는 FixedUpdate에서 Rigidbody2D로 움직이므로,
    /// 이동이 모두 끝난 LateUpdate에서 따라가야 화면이 떨리지 않는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("추적 대상")]
        [SerializeField] private Transform _target;

        [Tooltip("클수록 부드럽게, 작을수록 빠르게 따라간다. 0이면 즉시 따라간다.")]
        [SerializeField, Min(0f)] private float _smoothTime = 0.18f;

        [Tooltip("대상보다 화면을 조금 위/아래로 보정할 때 사용.")]
        [SerializeField] private float _offsetX = 0f;

        [Tooltip("세로도 따라갈지. 좌우로만 움직이는 우체국에서는 꺼 둔다.")]
        [SerializeField] private bool _followY = false;
        [SerializeField] private float _offsetY = 0f;

        [Header("이동 범위 제한")]
        [Tooltip("배경 스프라이트를 넣으면 그 폭을 경계로 자동 계산한다.")]
        [SerializeField] private SpriteRenderer _boundsSource;

        [Tooltip("배경 스프라이트를 쓰지 않고 직접 값을 넣을 때 켠다.")]
        [SerializeField] private bool _useManualBounds = false;
        [SerializeField] private float _manualMinX = -20f;
        [SerializeField] private float _manualMaxX = 20f;

        private Camera _camera;
        private float _baseY;
        private float _velocityX;
        private float _velocityY;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseY = transform.position.y;

            if (_target == null) Debug.LogError("[CameraFollow] 추적 대상 미연결", this);
            if (!_camera.orthographic) Debug.LogWarning("[CameraFollow] 직교 카메라 기준으로 계산한다", this);
        }

        // 플레이어 이동(FixedUpdate)이 끝난 뒤 따라가야 흔들리지 않는다.
        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 current = transform.position;

            float desiredX = ClampToBounds(_target.position.x + _offsetX);
            float desiredY = _followY ? _target.position.y + _offsetY : _baseY;

            float nextX = _smoothTime > 0f
                ? Mathf.SmoothDamp(current.x, desiredX, ref _velocityX, _smoothTime)
                : desiredX;

            float nextY = !_followY
                ? desiredY
                : (_smoothTime > 0f
                    ? Mathf.SmoothDamp(current.y, desiredY, ref _velocityY, _smoothTime)
                    : desiredY);

            transform.position = new Vector3(nextX, nextY, current.z);
        }

        /// <summary>카메라가 배경 밖을 비추지 않도록 목표 X를 배경 안쪽으로 제한한다.</summary>
        private float ClampToBounds(float desiredX)
        {
            if (!TryGetBounds(out float minX, out float maxX)) return desiredX;

            // 카메라가 실제로 비추는 가로 절반 폭
            float halfWidth = _camera.orthographicSize * _camera.aspect;

            float limitMin = minX + halfWidth;
            float limitMax = maxX - halfWidth;

            // 배경이 화면보다 좁으면 가운데 고정한다(양쪽 여백을 만들지 않는다).
            if (limitMin > limitMax) return (minX + maxX) * 0.5f;

            return Mathf.Clamp(desiredX, limitMin, limitMax);
        }

        private bool TryGetBounds(out float minX, out float maxX)
        {
            if (_useManualBounds)
            {
                minX = _manualMinX;
                maxX = _manualMaxX;
                return _manualMaxX > _manualMinX;
            }

            if (_boundsSource != null)
            {
                Bounds b = _boundsSource.bounds;
                minX = b.min.x;
                maxX = b.max.x;
                return true;
            }

            minX = 0f;
            maxX = 0f;
            return false; // 경계 정보가 없으면 제한하지 않는다
        }

        /// <summary>씬 뷰에서 카메라 이동 한계를 눈으로 확인한다.</summary>
        private void OnDrawGizmosSelected()
        {
            if (!TryGetBounds(out float minX, out float maxX)) return;

            float y = transform.position.y;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(minX, y - 5f, 0f), new Vector3(minX, y + 5f, 0f));
            Gizmos.DrawLine(new Vector3(maxX, y - 5f, 0f), new Vector3(maxX, y + 5f, 0f));
        }
    }
}
