using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 모바일 노치/펀치홀 대응. RectTransform 앵커를 Screen.safeArea에 맞춘다.
    /// 가로 고정 1920x1080 기준, 세이프 영역 안으로 주요 UI를 넣는다(아트 명세 1-3).
    /// 상단바/하단바 등 화면 가장자리에 붙는 컨테이너에 부착한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("가로(좌우) 세이프 영역 적용")]
        [SerializeField] private bool _applyX = true;
        [Tooltip("세로(상하) 세이프 영역 적용")]
        [SerializeField] private bool _applyY = true;

        private RectTransform _rt;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            // 회전/해상도 변경 감지 (에디터·기기 공통)
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreen.x != Screen.width ||
                _lastScreen.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rt == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _lastSafeArea = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            Rect safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            if (!_applyX) { min.x = 0f; max.x = 1f; }
            if (!_applyY) { min.y = 0f; max.y = 1f; }

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
