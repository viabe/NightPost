using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// HUD 프리젠터. 시스템과 HUDController(뷰)를 잇는 표현 계층.
    /// 아키텍처(UI 이벤트 명세서): System → GameEvents → [HUDPresenter] → HUDController.
    ///
    /// 규칙:
    ///   - OnEnable에서 구독 + 초기 1회 조회, OnDisable에서 "같은 이름 메서드"로 해제(익명 람다 금지)
    ///   - UI는 GameEvents.Raise* 를 호출하지 않는다. 저장 데이터도 직접 수정하지 않는다.
    ///   - 조회는 PlayerDataManager, 표시는 HUDController에만 위임한다.
    ///
    /// 현재 연결 범위: 재화(CurrencyChanged) + 미열람 답장 배지(UnreadReplyCountChanged)
    ///                 + 편지 보관 현황(LetterReceived / LetterStateChanged / FacilityUpgraded).
    ///   Stamp/Level/Mission은 관련 데이터 시스템이 생긴 뒤 별도 이벤트로 연결한다.
    /// </summary>
    public class HUDPresenter : MonoBehaviour
    {
        [SerializeField] private HUDController _hud;          // 표시 대상 뷰
        [SerializeField] private PlayerDataManager _playerData; // 재화·답장 조회
        [SerializeField] private LetterService _letterService;  // 편지 보관 현황 조회

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.UnreadReplyCountChanged += OnUnreadReplyCountChanged;

            // 편지 보관 수는 New+Waiting 합계라 수신·상태변경 양쪽에서 바뀐다.
            // 최대치는 시설 효과가 반영되므로 시설 강화 시에도 갱신한다.
            GameEvents.LetterReceived += OnLetterReceived;
            GameEvents.LetterStateChanged += OnLetterStateChanged;
            GameEvents.FacilityUpgraded += OnFacilityUpgraded;

            RefreshInitialView();
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.UnreadReplyCountChanged -= OnUnreadReplyCountChanged;
            GameEvents.LetterReceived -= OnLetterReceived;
            GameEvents.LetterStateChanged -= OnLetterStateChanged;
            GameEvents.FacilityUpgraded -= OnFacilityUpgraded;
        }

        // GameBootstrap이 Awake에서 PlayerDataManager를 초기화하므로, 최초 진입 시
        // 모든 Awake가 끝난 Start 시점에 한 번 더 조회해 초기값을 확실히 반영한다.
        // (OnEnable이 GameBootstrap.Awake보다 먼저 돌면 조회값이 비어 있을 수 있어 이중 안전장치)
        private void Start()
        {
            RefreshInitialView();
        }

        /// <summary>이벤트는 구독 이후 변경만 전달하므로, 진입 시 현재 데이터를 먼저 조회해 채운다.</summary>
        private void RefreshInitialView()
        {
            if (_hud == null) return;

            if (_playerData != null)
            {
                _hud.SetCoin(_playerData.GetCurrency());
                _hud.SetInboxBadge(_playerData.GetUnreadReplyCount() > 0);
            }

            RefreshLetterCapacity();
        }

        /// <summary>보관 중인 편지 수와 최대 보관 수를 다시 조회해 표시한다.</summary>
        private void RefreshLetterCapacity()
        {
            if (_hud == null || _letterService == null) return;
            _hud.SetLetterCapacity(_letterService.GetCurrentLetterCount(), _letterService.GetMaxLetterCapacity());
        }

        private void OnCurrencyChanged(int currentCurrency)
        {
            if (_hud != null) _hud.SetCoin(currentCurrency);
        }

        private void OnUnreadReplyCountChanged(int unreadCount)
        {
            if (_hud != null) _hud.SetInboxBadge(unreadCount > 0);
        }

        private void OnLetterReceived(int letterID) => RefreshLetterCapacity();

        private void OnLetterStateChanged(int letterID, ELetterProgressState state) => RefreshLetterCapacity();

        private void OnFacilityUpgraded(int facilityID, int currentLevel) => RefreshLetterCapacity();
    }
}
