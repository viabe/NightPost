using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 월드 시설 위에 띄우는 알림 말풍선.
    /// 할 일이 생겼을 때만 나타나 플레이어를 그쪽으로 유도한다.
    ///
    ///   분류대 → 분류할 편지(New)가 있을 때
    ///   배달대 → 배달 대기 편지(Waiting) 또는 확인 안 한 배달 결과가 있을 때
    ///
    /// 시설 오브젝트의 자식으로 말풍선 스프라이트를 두고 이 컴포넌트를 붙인다.
    /// 표시 여부는 데이터로만 결정하며, 이 스크립트는 조회와 표시만 담당한다.
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
        [Tooltip("말풍선 오브젝트. 문구는 이미지에 포함돼 있으므로 켜고 끄기만 한다. 기본 비활성.")]
        [SerializeField] private GameObject _noticeRoot;

        [Header("연출")]
        [Tooltip("말풍선이 위아래로 살짝 떠다니는 폭. 0이면 고정.")]
        [SerializeField] private float _bobAmplitude = 0.08f;
        [SerializeField] private float _bobSpeed = 1.8f;

        private Vector3 _noticeBasePosition;
        private bool _isShowing;

        private void Awake()
        {
            if (_noticeRoot != null)
            {
                _noticeBasePosition = _noticeRoot.transform.localPosition;
                _noticeRoot.SetActive(false);
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
        private void Start() => Refresh();

        private void Update()
        {
            if (!_isShowing || _noticeRoot == null || _bobAmplitude <= 0f) return;

            // 살짝 떠다니게 해서 눈에 띄게 한다.
            float offsetY = Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude;
            _noticeRoot.transform.localPosition = _noticeBasePosition + new Vector3(0f, offsetY, 0f);
        }

        /// <summary>현재 데이터를 다시 조회해 말풍선을 켜고 끈다.</summary>
        public void Refresh()
        {
            bool show = GetCount() > 0;

            if (_noticeRoot != null && _isShowing != show)
            {
                _noticeRoot.SetActive(show);
                // 껐다 켜면 위치가 흔들린 채 남지 않도록 되돌린다.
                if (!show) _noticeRoot.transform.localPosition = _noticeBasePosition;
            }
            _isShowing = show;
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
