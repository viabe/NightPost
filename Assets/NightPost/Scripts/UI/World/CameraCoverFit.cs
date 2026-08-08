using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 배경 스프라이트가 화면을 항상 가득 채우도록 카메라 크기를 맞춘다.
    ///
    /// 배경이 UI Image가 아니라 월드 스프라이트라 Canvas Scaler로는 조절할 수 없다.
    /// 기기마다 화면 비율이 달라 그대로 두면 여백이 생기거나 엉뚱하게 확대되므로,
    /// 배경이 카메라 시야를 완전히 덮는 최대 크기를 계산해 적용한다.
    ///
    /// 화면이 배경보다 넓으면 위아래가, 좁으면 좌우가 잘린다.
    /// 잘려도 괜찮도록 배경 가장자리에는 중요한 그림을 두지 않는 것이 좋다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class CameraCoverFit : MonoBehaviour
    {
        [Tooltip("화면을 채울 배경 스프라이트.")]
        [SerializeField] private SpriteRenderer _background;

        [Tooltip("배경 안쪽으로 살짝 더 들어가 가장자리가 비치지 않게 한다.")]
        [SerializeField, Range(0f, 0.2f)] private float _inset = 0.02f;

        [Tooltip("카메라를 배경 중심으로 옮긴다. 카메라가 따로 움직이는 씬에서는 끈다.")]
        [SerializeField] private bool _centerOnBackground = true;

        private Camera _camera;
        private int _lastWidth;
        private int _lastHeight;

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            Fit();
        }

        // 해상도·회전이 바뀌면 다시 계산한다.
        private void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
            Fit();
        }

        /// <summary>배경이 화면을 덮는 가장 큰 카메라 크기를 계산해 적용한다.</summary>
        public void Fit()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null || _background == null) return;
            if (!_camera.orthographic) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            Bounds b = _background.bounds;
            float aspect = (float)Screen.width / Screen.height;

            // 세로로 꽉 채우는 크기와 가로로 꽉 채우는 크기 중
            // 더 작은 쪽을 쓰면 배경 밖이 보이지 않는다.
            float sizeByHeight = b.size.y * 0.5f;
            float sizeByWidth = b.size.x * 0.5f / aspect;
            float size = Mathf.Min(sizeByHeight, sizeByWidth);

            _camera.orthographicSize = Mathf.Max(0.01f, size * (1f - _inset));

            if (_centerOnBackground)
            {
                Vector3 p = transform.position;
                transform.position = new Vector3(b.center.x, b.center.y, p.z);
            }
        }
    }
}
