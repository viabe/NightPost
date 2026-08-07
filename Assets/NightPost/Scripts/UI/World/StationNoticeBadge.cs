using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 월드 시설 위에 띄우는 알림 말풍선.
    ///
    /// 말풍선은 항상 떠 있고, 할 일이 생겼는지는 "움직임"으로 구분한다.
    ///   평상시   → 가만히 있는다(흐릿하게 둘 수도 있다)
    ///   할 일 있음 → 위아래로 떠다니며 눈에 띈다
    ///
    ///   분류대 → 분류할 편지(New)가 있을 때
    ///   배달대 → 배달 대기 편지(Waiting) 또는 확인 안 한 배달 결과가 있을 때
    ///
    /// 시설 오브젝트의 자식으로 말풍선 스프라이트를 두고 이 컴포넌트를 붙인다.
    /// 표시 여부는 데이터로만 결정하며, 이 스크립트는 조회와 표현만 담당한다.
    /// </summary>
    public class StationNoticeBadge : MonoBehaviour
    {
        /// <summary>무엇이 생겼을 때 알릴지.</summary>
        public enum ENoticeSource
        {
            UnsortedLetters,   // 분류 전 편지 (분류대)
            WaitingLetters,    // 배달 대기 편지 (배달대)
            DeliveryResults,   // 확인 안 한 배달 결과 (배달대)
            WaitingOrResults,  // 위 둘 중 하나라도 있으면 (배달대 권장)
        }

        [Header("알림 조건")]
        [SerializeField] private ENoticeSource _source = ENoticeSource.UnsortedLetters;

        [Header("의존성")]
        [SerializeField] private LetterService _letterService;
        [Tooltip("배달 결과를 조건으로 쓸 때만 필요하다.")]
        [SerializeField] private PlayerDataManager _playerData;

        [Header("표시")]
        [Tooltip("말풍선 오브젝트. 항상 켜둔 채 움직임으로만 구분한다.")]
        [SerializeField] private GameObject _noticeRoot;
        [Tooltip("끄면 할 일이 없을 때 말풍선을 아예 숨긴다.")]
        [SerializeField] private bool _keepVisibleWhenIdle = true;
        [Tooltip("흐리기 연출을 쓰려면 말풍선의 SpriteRenderer를 넣는다. 비워도 동작한다.")]
        [SerializeField] private SpriteRenderer _noticeSprite;

        [Header("할 일 있을 때")]
        [Tooltip("위아래로 떠다니는 폭. 0이면 움직이지 않는다.")]
        [SerializeField] private float _bobAmplitude = 0.12f;
        [SerializeField] private float _bobSpeed = 2.4f;
        [Tooltip("살짝 커지는 정도. 1이면 크기 변화 없음.")]
        [SerializeField, Min(1f)] private float _alertScale = 1.1f;
        [SerializeField, Range(0f, 1f)] private float _alertAlpha = 1f;

        [Header("평상시")]
        [Tooltip("할 일이 없을 때의 투명도. 낮출수록 조용해 보인다.")]
        [SerializeField, Range(0f, 1f)] private float _idleAlpha = 0.45f;

        [Tooltip("상태가 바뀔 때 부드럽게 넘어가는 시간(초).")]
        [SerializeField, Min(0f)] private float _transitionSeconds = 0.25f;

        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private bool _hasWork;
        private float _blend;   // 0 = 평상시, 1 = 알림

        private void Awake()
        {
            if (_noticeRoot != null)
            {
                _basePosition = _noticeRoot.transform.localPosition;
                _baseScale = _noticeRoot.transform.localScale;
                if (_noticeSprite == null) _noticeSprite = _noticeRoot.GetComponent<SpriteRenderer>();
            }
            else
            {
                Debug.LogError("[StationNotice] 말풍선 오브젝트 미연결", this);
            }

            if (_letterService == null) Debug.LogError("[StationNotice] LetterService 미연결", this);
            if (NeedsPlayerData() && _playerData == null)
                Debug.LogError("[StationNotice] 배달 결과 조건인데 PlayerDataManager 미연결", this);
        }

        private void OnEnable()
        {
            GameEvents.LetterReceived += OnLetterChanged;
            GameEvents.LetterStateChanged += OnLetterStateChanged;
            GameEvents.DeliveryCompleted += OnLetterChanged;
            GameEvents.DeliveryResultChecked += OnLetterChanged;
        }

        private void OnDisable()
        {
            GameEvents.LetterReceived -= OnLetterChanged;
            GameEvents.LetterStateChanged -= OnLetterStateChanged;
            GameEvents.DeliveryCompleted -= OnLetterChanged;
            GameEvents.DeliveryResultChecked -= OnLetterChanged;
        }

        // GameBootstrap이 Awake에서 데이터를 채우므로 Start에서 첫 조회를 한다.
        private void Start()
        {
            Refresh();
            _blend = _hasWork ? 1f : 0f;   // 첫 프레임부터 자연스러운 상태로 시작
            ApplyVisual();
        }

        private void Update()
        {
            if (_noticeRoot == null) return;

            // 상태가 바뀌면 뚝 끊기지 않게 서서히 넘어간다.
            float target = _hasWork ? 1f : 0f;
            _blend = _transitionSeconds > 0f
                ? Mathf.MoveTowards(_blend, target, Time.deltaTime / _transitionSeconds)
                : target;

            ApplyVisual();
        }

        /// <summary>현재 데이터를 다시 조회해 할 일 여부를 갱신한다.</summary>
        public void Refresh()
        {
            _hasWork = GetCount() > 0;

            // 평상시에 숨기는 설정이면 여기서 켜고 끈다.
            if (_noticeRoot != null && !_keepVisibleWhenIdle)
                _noticeRoot.SetActive(_hasWork);
            else if (_noticeRoot != null && !_noticeRoot.activeSelf)
                _noticeRoot.SetActive(true);
        }

        /// <summary>_blend(0~1)에 맞춰 위치·크기·투명도를 적용한다.</summary>
        private void ApplyVisual()
        {
            Transform t = _noticeRoot.transform;

            // 떠다니는 움직임은 알림 상태일 때만 폭이 생긴다.
            float bob = 0f;
            if (_bobAmplitude > 0f && _blend > 0.001f)
                bob = Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude * _blend;

            t.localPosition = _basePosition + new Vector3(0f, bob, 0f);
            t.localScale = _baseScale * Mathf.Lerp(1f, _alertScale, _blend);

            if (_noticeSprite != null)
            {
                Color c = _noticeSprite.color;
                c.a = Mathf.Lerp(_idleAlpha, _alertAlpha, _blend);
                _noticeSprite.color = c;
            }
        }

        private int GetCount()
        {
            switch (_source)
            {
                case ENoticeSource.UnsortedLetters:
                    return UnsortedCount();

                case ENoticeSource.WaitingLetters:
                    return WaitingCount();

                case ENoticeSource.DeliveryResults:
                    return ResultCount();

                case ENoticeSource.WaitingOrResults:
                    return WaitingCount() + ResultCount();

                default:
                    return 0;
            }
        }

        private int UnsortedCount()
        {
            if (_letterService == null) return 0;
            var list = _letterService.GetUnsortedLetters();
            return list != null ? list.Count : 0;
        }

        private int WaitingCount()
        {
            if (_letterService == null) return 0;
            var list = _letterService.GetWaitingLetters();
            return list != null ? list.Count : 0;
        }

        private int ResultCount()
        {
            if (_playerData == null) return 0;
            var list = _playerData.GetUncheckedDeliveryResults();
            return list != null ? list.Count : 0;
        }

        private bool NeedsPlayerData()
            => _source == ENoticeSource.DeliveryResults || _source == ENoticeSource.WaitingOrResults;

        private void OnLetterChanged(int id) => Refresh();
        private void OnLetterStateChanged(int id, ELetterProgressState state) => Refresh();
    }
}
