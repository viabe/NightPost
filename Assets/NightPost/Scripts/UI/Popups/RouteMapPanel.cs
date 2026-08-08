using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 배달대 팝업의 노선 탭. 지도 위 핀으로 노선을 고르고 우측 상세에서 해금한다.
    ///
    /// 해금 경로는 아침 보고와 완전히 같다.
    ///   - 판정: ProgressionService.CanUnlockRoute(routeID)
    ///   - 실행: GameFlowController.UnlockRoute(routeID)
    /// 성공하면 GameEvents.RouteUnlocked가 올라오고, 해금음과 저장은 시스템이 알아서 한다.
    ///
    /// 아침 보고는 접속 직후에만 열리므로, 플레이 중 조건을 채운 노선은 이 탭에서만 열 수 있다.
    /// 지금은 해금 비용이 없다. 재화 비용을 도입하면 StateOf에 부족 분기만 늘리면 된다.
    /// </summary>
    public class RouteMapPanel : MonoBehaviour
    {
        [Header("의존성")]
        [SerializeField] private ProgressionService _progression;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private StaticDataCatalog _catalog;
        [SerializeField] private PlayerDataManager _playerData;
        [Tooltip("선택. 연결하면 상세에 그 지역으로 갈 대기 편지 수를 보여준다.")]
        [SerializeField] private LetterService _letterService;

        [Header("지도")]
        [Tooltip("지도 위에 배치한 핀들. 노선 수만큼 넣는다.")]
        [SerializeField] private RouteMapPin[] _pins;

        [Header("상세")]
        [SerializeField] private GameObject _detailRoot;   // 노선을 골랐을 때 켜짐
        [SerializeField] private GameObject _detailEmpty;  // 미선택 안내
        [SerializeField] private TMP_Text _nameText;       // "바람 부는 외곽길"
        [SerializeField] private TMP_Text _metaText;       // "외곽 · 보통"
        [SerializeField] private TMP_Text _infoText;       // "기본 소요 8분 / 대기 편지 2통"
        [SerializeField] private Button _unlockButton;
        [SerializeField] private TMP_Text _unlockButtonLabel;

        private int _selectedRouteID = -1;
        private bool _subscribed;

        private void Awake()
        {
            if (_pins != null)
            {
                foreach (RouteMapPin pin in _pins)
                    if (pin != null) pin.Bind(OnPinClicked);
            }

            if (_unlockButton != null)
            {
                _unlockButton.onClick.RemoveAllListeners();
                _unlockButton.onClick.AddListener(OnUnlockClicked);
            }
        }

        // 탭 전환으로 켜지고 꺼지므로 구독과 갱신을 여기서 처리한다.
        private void OnEnable()
        {
            // 인스펙터 배선 누락은 조용히 실패해서 원인 찾기가 어렵다. 켜질 때 한 번 점검한다.
            if (_progression == null) Debug.LogError("[RouteMap] ProgressionService 미연결 — 모든 핀이 잠김으로 보인다", this);
            if (_flow == null) Debug.LogError("[RouteMap] GameFlowController 미연결 — 해금이 동작하지 않는다", this);
            if (_catalog == null) Debug.LogError("[RouteMap] StaticDataCatalog 미연결 — 이름과 지역이 비어 보인다", this);
            if (_playerData == null) Debug.LogError("[RouteMap] PlayerDataManager 미연결 — 이미 연 노선을 구분하지 못한다", this);
            if (_pins == null || _pins.Length == 0) Debug.LogError("[RouteMap] 지도 핀이 하나도 연결되지 않았다", this);

            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            _selectedRouteID = -1;
        }

        /// <summary>지도와 상세를 현재 진행 상태로 다시 그린다.</summary>
        public void Refresh()
        {
            RefreshPins();
            RefreshDetail();
        }

        /// <summary>지금 열 수 있는 노선이 하나라도 있는지. 탭 배지에 쓴다.</summary>
        public bool HasUnlockableRoute()
        {
            if (_pins == null) return false;

            foreach (RouteMapPin pin in _pins)
            {
                if (pin == null) continue;
                if (StateOf(pin.RouteId) == ERoutePinState.Unlockable) return true;
            }
            return false;
        }

        // ── 상태 판정 ──
        private ERoutePinState StateOf(int routeID)
        {
            // 진행도 서비스가 없으면 판정할 수 없으므로 잠김으로 본다.
            if (_progression == null) return ERoutePinState.Locked;

            // 이미 열린 노선인지 먼저 확인한다.
            if (_playerData != null && _playerData.IsRouteUnlocked(routeID)) return ERoutePinState.Unlocked;

            // 아침 보고와 같은 판정을 쓴다(진행도 조건 충족 + 미해금).
            return _progression.CanUnlockRoute(routeID) ? ERoutePinState.Unlockable : ERoutePinState.Locked;
        }

        // ── 그리기 ──
        private void RefreshPins()
        {
            if (_pins == null) return;

            foreach (RouteMapPin pin in _pins)
            {
                if (pin == null) continue;

                RouteStaticData route = _catalog != null ? _catalog.GetRoute(pin.RouteId) : null;
                ERoutePinState state = StateOf(pin.RouteId);

                string name = route != null ? UILabels.Region(route.RegionType) : "-";
                string sub;

                switch (state)
                {
                    // 이미 연 노선은 이름을 보여준다.
                    case ERoutePinState.Unlocked: sub = route != null ? route.RouteName : string.Empty; break;
                    // 열 수 있는 노선은 눌러보라고 알린다.
                    case ERoutePinState.Unlockable: sub = "열 수 있어요"; break;
                    // 아직 못 여는 노선은 남은 조건만 알린다.
                    default: sub = LockedHint(route); break;
                }

                pin.Apply(state, name, sub);
                pin.SetSelected(pin.RouteId == _selectedRouteID);
            }
        }

        /// <summary>조건 미달 핀에 띄울 문구. "배달 7회 필요" 형태.</summary>
        private string LockedHint(RouteStaticData route)
        {
            if (route == null || route.UnlockCondition == null) return "아직 잠겨 있어요";
            return $"배달 {route.UnlockCondition.RequiredCompletedDeliveryCount}회 필요";
        }

        private void RefreshDetail()
        {
            RouteStaticData route = _selectedRouteID > 0 && _catalog != null
                ? _catalog.GetRoute(_selectedRouteID) : null;

            if (_detailEmpty != null) _detailEmpty.SetActive(route == null);
            if (_detailRoot != null) _detailRoot.SetActive(route != null);
            if (route == null) return;

            ERoutePinState state = StateOf(route.RouteID);

            if (_nameText != null) _nameText.text = route.RouteName;
            if (_metaText != null)
                _metaText.text = $"{UILabels.Region(route.RegionType)} · {DifficultyLabel(route.Difficulty)}";

            if (_infoText != null)
            {
                string baseTime = $"기본 소요 {UILabels.Duration(Mathf.CeilToInt(route.BaseDeliveryTimeSeconds))}";
                int waiting = WaitingLetterCount(route.RegionType);
                _infoText.text = waiting >= 0 ? $"{baseTime}\n대기 편지 {waiting}통" : baseTime;
            }

            // 이미 연 노선에는 해금 버튼을 띄우지 않는다.
            if (_unlockButton != null)
            {
                _unlockButton.gameObject.SetActive(state != ERoutePinState.Unlocked);
                _unlockButton.interactable = state == ERoutePinState.Unlockable;
            }
            if (_unlockButtonLabel != null)
                _unlockButtonLabel.text = state == ERoutePinState.Unlockable ? "노선 열기" : LockedHint(route);
        }

        /// <summary>해당 지역으로 갈 대기 편지 수. 조회할 수 없으면 -1.</summary>
        private int WaitingLetterCount(ERegionType region)
        {
            if (_letterService == null) return -1;

            IReadOnlyList<LetterStaticData> waiting = _letterService.GetWaitingLetters();
            if (waiting == null) return 0;

            int count = 0;
            foreach (LetterStaticData letter in waiting)
                if (letter != null && letter.DestinationRegion == region) count++;
            return count;
        }

        // ── 동작 ──
        private void OnPinClicked(int routeID)
        {
            _selectedRouteID = routeID;
            UISoundPlayer.Play(ESFXType.RouteMapOpen);
            RefreshPins();
            RefreshDetail();
        }

        private void OnUnlockClicked()
        {
            if (_flow == null || _selectedRouteID <= 0) return;

            if (!_flow.UnlockRoute(_selectedRouteID))
            {
                // 실패에는 소리를 내지 않는다(사운드 명세 §5-1: 벌하는 느낌을 주지 않는다).
                Debug.LogWarning($"[RouteMap] 노선 해금 실패 routeID={_selectedRouteID}");
                ToastController.Instance?.Show("지금은 이 노선을 열 수 없어요.");
                return;
            }
            // 해금음과 화면 갱신은 RouteUnlocked 구독이 받는다.
        }

        // ── 이벤트 ──
        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameEvents.RouteUnlocked += OnRouteUnlocked;
            GameEvents.DeliveryCompleted += OnDeliveryCompleted;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.RouteUnlocked -= OnRouteUnlocked;
            GameEvents.DeliveryCompleted -= OnDeliveryCompleted;
        }

        private void OnRouteUnlocked(int routeID) => Refresh();
        // 배달이 끝나면 누적 완료 수가 늘어 해금 조건이 바뀔 수 있다.
        private void OnDeliveryCompleted(int letterID) => Refresh();

        private static string DifficultyLabel(ERouteDifficulty d)
        {
            switch (d)
            {
                case ERouteDifficulty.Easy: return "쉬움";
                case ERouteDifficulty.Normal: return "보통";
                case ERouteDifficulty.Hard: return "어려움";
                default: return "-";
            }
        }
    }
}
