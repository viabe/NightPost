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
    /// 현재 연결 범위(명세서 §4): 재화(CurrencyChanged) + 미열람 답장 배지(UnreadReplyCountChanged).
    ///   Stamp/Level/Mission은 관련 데이터 시스템이 생긴 뒤 별도 이벤트로 연결한다.
    /// </summary>
    public class HUDPresenter : MonoBehaviour
    {
        [SerializeField] private HUDController _hud;          // 표시 대상 뷰
        [SerializeField] private PlayerDataManager _playerData; // 초기 조회용(같은 씬의 인스턴스)

        private void OnEnable()
        {
            GameEvents.CurrencyChanged += OnCurrencyChanged;
            GameEvents.UnreadReplyCountChanged += OnUnreadReplyCountChanged;
            RefreshInitialView();
        }

        private void OnDisable()
        {
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
            GameEvents.UnreadReplyCountChanged -= OnUnreadReplyCountChanged;
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
            if (_hud == null || _playerData == null) return;
            _hud.SetCoin(_playerData.GetCurrency());
            _hud.SetInboxBadge(_playerData.GetUnreadReplyCount() > 0);
        }

        private void OnCurrencyChanged(int currentCurrency)
        {
            if (_hud != null) _hud.SetCoin(currentCurrency);
        }

        private void OnUnreadReplyCountChanged(int unreadCount)
        {
            if (_hud != null) _hud.SetInboxBadge(unreadCount > 0);
        }
    }
}
