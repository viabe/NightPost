using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 배달 배정 UI. 배달대(Station_Delivery) 도착 시 Open()으로 열린다.
    /// Waiting 편지 → 배달부 → 노선을 고르면 예상 정보(시간·보상·시작가능)를 보여주고 배달을 시작한다.
    ///
    /// API 명세서 v1.1 기준. UI는 서비스 공개 API만 호출하고 PlayerDataManager/StaticDataCatalog를
    /// 직접 조회하지 않는다. 인자 순서는 (letterID, courierID, routeID)로 고정.
    ///   - LetterService.GetWaitingLetters()
    ///   - DeliveryService.GetAvailableCouriers() / GetCompatibleRoutes(letterID)
    ///   - DeliveryService.GetDeliveryPreview(letterID, courierID, routeID) → DeliveryPreviewData
    ///   - DeliveryService.StartDelivery(letterID, courierID, routeID)
    /// 편지 목록엔 SortingLetterItem, 배달부·노선엔 AssignmentOptionItem을 재사용한다.
    /// </summary>
    public class DeliveryUIController : MonoBehaviour
    {
        [Header("의존성")]
        [SerializeField] private DeliveryService _deliveryService;
        [SerializeField] private LetterService _letterService;
        [SerializeField] private PlayerController _playerController;

        [Header("패널")]
        [SerializeField] private GameObject _deliveryPanel;

        [Header("대기 편지")]
        [SerializeField] private Transform _letterListRoot;
        [SerializeField] private SortingLetterItem _letterItemPrefab;
        [SerializeField] private GameObject _letterEmpty;

        [Header("배달부")]
        [SerializeField] private Transform _courierListRoot;
        [SerializeField] private AssignmentOptionItem _courierItemPrefab;
        [SerializeField] private GameObject _courierEmpty;      // 배달부 0명 안내
        [SerializeField] private TMP_Text _courierEmptyText;

        [Header("노선")]
        [SerializeField] private Transform _routeListRoot;
        [SerializeField] private AssignmentOptionItem _routeItemPrefab;
        [SerializeField] private GameObject _routeEmpty;        // 노선 0개 안내
        [SerializeField] private TMP_Text _routeEmptyText;

        [Header("예상 정보")]
        [SerializeField] private TMP_Text _estimateText;      // "예상 3분"
        [SerializeField] private TMP_Text _rewardText;        // 예상 보상
        [SerializeField] private GameObject _regionMismatchWarn; // 지역 불일치 경고

        [Header("실행")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _closeButton;

        private int _selectedLetterID = -1;
        private int _selectedCourierID = -1;
        private int _selectedRouteID = -1;
        private bool _isOpen;
        private bool _subscribed;

        private readonly List<SortingLetterItem> _letterItems = new();
        private readonly List<AssignmentOptionItem> _courierItems = new();
        private readonly List<AssignmentOptionItem> _routeItems = new();

        private void Awake()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(OnStartClicked);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
        }

        // ── 열기 / 닫기 ──
        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            if (_deliveryPanel != null) _deliveryPanel.SetActive(true);
            if (_playerController != null) _playerController.SetControlEnabled(false);

            // 인스펙터 배선 누락은 조용히 실패해서 원인 찾기가 어렵다. 열 때 한 번 점검한다.
            if (_deliveryService == null) Debug.LogError("[Delivery] DeliveryService 미연결", this);
            if (_letterService == null) Debug.LogError("[Delivery] LetterService 미연결", this);
            if (_letterItemPrefab == null) Debug.LogError("[Delivery] 편지 아이템 프리팹 미연결", this);
            if (_letterListRoot == null) Debug.LogError("[Delivery] 편지 목록 루트 미연결", this);
            if (_courierItemPrefab == null) Debug.LogError("[Delivery] 배달부 아이템 프리팹 미연결", this);
            if (_courierListRoot == null) Debug.LogError("[Delivery] 배달부 목록 루트 미연결", this);
            if (_routeItemPrefab == null) Debug.LogError("[Delivery] 노선 아이템 프리팹 미연결", this);
            if (_routeListRoot == null) Debug.LogError("[Delivery] 노선 목록 루트 미연결", this);

            Subscribe();
            ResetSelection();
            RefreshWaitingLetters();
            ClearOptionsForNoLetter();
            ClearPreview();
            UpdateStartButton();
        }

        public void Close()
        {
            // _isOpen 여부와 무관하게 항상 닫는다.
            // (에디터에서 패널을 켜둔 채 실행하면 Open()을 거치지 않아 _isOpen이 false인데,
            //  여기서 early return 하면 닫기 버튼이 아무 반응도 없는 것처럼 보인다)
            _isOpen = false;

            ResetSelection();
            Unsubscribe();

            if (_deliveryPanel != null) _deliveryPanel.SetActive(false);
            if (_playerController != null) _playerController.SetControlEnabled(true);
        }

        // ── 편지 선택 ──
        private void OnLetterSelected(int letterID)
        {
            _selectedLetterID = letterID;
            _selectedCourierID = -1;
            _selectedRouteID = -1;

            HighlightLetters();
            RefreshCouriers();
            RefreshRoutes(letterID);
            ClearPreview();
            UpdateStartButton();
        }

        private void OnCourierSelected(int courierID)
        {
            _selectedCourierID = courierID;
            foreach (var it in _courierItems) it.SetSelected(it.Id == courierID);
            RefreshPreview();
        }

        private void OnRouteSelected(int routeID)
        {
            _selectedRouteID = routeID;
            foreach (var it in _routeItems) it.SetSelected(it.Id == routeID);
            RefreshPreview();
        }

        // ── 배달 시작 ──
        private void OnStartClicked()
        {
            if (_deliveryService == null) return;
            if (_selectedLetterID <= 0 || _selectedCourierID <= 0 || _selectedRouteID <= 0) return;

            // 인자 순서: letterID, courierID, routeID
            bool ok = _deliveryService.StartDelivery(_selectedLetterID, _selectedCourierID, _selectedRouteID);
            if (!ok)
            {
                Debug.LogWarning("[Delivery] 배달 시작 실패");
                ToastController.Instance?.Show("지금은 배달을 시작할 수 없어요.");
                return;
            }

            // 성공: 목록 갱신(편지는 Waiting에서 빠지고, 배달부는 사용 중)
            ResetSelection();
            RefreshWaitingLetters();
            ClearOptionsForNoLetter();
            ClearPreview();
            UpdateStartButton();
        }

        // ── 조회/갱신 ──
        private void RefreshWaitingLetters()
        {
            ClearLetterList();
            if (_letterService == null || _letterItemPrefab == null || _letterListRoot == null) return;

            int count = 0;
            bool selectedStill = false;
            foreach (LetterStaticData letter in _letterService.GetWaitingLetters())
            {
                if (letter == null) continue;
                SortingLetterItem item = Instantiate(_letterItemPrefab, _letterListRoot);
                item.gameObject.SetActive(true);
                // 분류 완료(Waiting)라 이미 읽은 편지 → 새 표시 없음(isRead=true)
                item.Setup(letter.LetterID, letter.LetterTitle, letter.SenderName, true, OnLetterSelected);
                _letterItems.Add(item);
                count++;
                if (letter.LetterID == _selectedLetterID) selectedStill = true;
            }
            if (_letterEmpty != null) _letterEmpty.SetActive(count == 0);

            if (_selectedLetterID > 0 && !selectedStill)
            {
                // 선택 편지가 사라짐 → 선택 초기화
                _selectedLetterID = -1;
                _selectedCourierID = -1;
                _selectedRouteID = -1;
                ClearOptionsForNoLetter();
                ClearPreview();
                UpdateStartButton();
            }
            else
            {
                HighlightLetters();
            }
        }

        private void RefreshCouriers()
        {
            ClearCourierList();
            if (_deliveryService == null || _courierItemPrefab == null || _courierListRoot == null) return;

            foreach (CourierStaticData courier in _deliveryService.GetAvailableCouriers())
            {
                if (courier == null) continue;
                AssignmentOptionItem item = Instantiate(_courierItemPrefab, _courierListRoot);
                item.gameObject.SetActive(true);
                item.Setup(courier.CourierID, courier.CourierName, VehicleLabel(courier.Transportation), true, OnCourierSelected);
                _courierItems.Add(item);
            }

            ShowCourierEmpty(_courierItems.Count > 0 ? null : "지금 나갈 수 있는 배달부가 없어요.");
        }

        private void RefreshRoutes(int letterID)
        {
            ClearRouteList();
            if (_deliveryService == null || _routeItemPrefab == null || _routeListRoot == null) return;

            foreach (RouteStaticData route in _deliveryService.GetCompatibleRoutes(letterID))
            {
                if (route == null) continue;
                AssignmentOptionItem item = Instantiate(_routeItemPrefab, _routeListRoot);
                item.gameObject.SetActive(true);
                item.Setup(route.RouteID, route.RouteName, DifficultyLabel(route.Difficulty), true, OnRouteSelected);
                _routeItems.Add(item);
            }

            ShowRouteEmpty(_routeItems.Count > 0 ? null : NoRouteMessage());
        }

        /// <summary>
        /// 노선이 0개인 이유에 맞는 안내 문구.
        /// 해금된 노선이 아예 없는 경우와, 있지만 이 편지의 목적지와 맞지 않는 경우를 구분한다.
        /// </summary>
        private string NoRouteMessage()
        {
            IReadOnlyList<RouteStaticData> unlocked = _deliveryService.GetUnlockedRoutes();
            if (unlocked == null || unlocked.Count == 0)
                return "아직 열린 노선이 없어요.";

            return "이 편지를 보낼 수 있는 노선이\n아직 열리지 않았어요.";
        }

        /// <summary>배달부 빈 상태 안내. message가 null이면 숨긴다.</summary>
        private void ShowCourierEmpty(string message)
        {
            if (_courierEmptyText != null && message != null) _courierEmptyText.text = message;
            if (_courierEmpty != null) _courierEmpty.SetActive(message != null);
        }

        /// <summary>노선 빈 상태 안내. message가 null이면 숨긴다.</summary>
        private void ShowRouteEmpty(string message)
        {
            if (_routeEmptyText != null && message != null) _routeEmptyText.text = message;
            if (_routeEmpty != null) _routeEmpty.SetActive(message != null);
        }

        private void RefreshPreview()
        {
            if (_deliveryService == null ||
                _selectedLetterID <= 0 || _selectedCourierID <= 0 || _selectedRouteID <= 0)
            {
                ClearPreview();
                UpdateStartButton();
                return;
            }

            DeliveryPreviewData preview =
                _deliveryService.GetDeliveryPreview(_selectedLetterID, _selectedCourierID, _selectedRouteID);

            if (preview == null)
            {
                ClearPreview();
                UpdateStartButton();
                return;
            }

            if (_estimateText != null) _estimateText.text = $"예상 {FormatTime(preview.EstimatedDurationSeconds)}";
            if (_rewardText != null) _rewardText.text = preview.ExpectedReward.ToString("N0");
            if (_regionMismatchWarn != null) _regionMismatchWarn.SetActive(!preview.IsRegionMatched);

            if (_startButton != null) _startButton.interactable = preview.CanStartDelivery;
        }

        private void ClearPreview()
        {
            if (_estimateText != null) _estimateText.text = "배달부와 노선을 선택하세요";
            if (_rewardText != null) _rewardText.text = "-";
            if (_regionMismatchWarn != null) _regionMismatchWarn.SetActive(false);
        }

        private void UpdateStartButton()
        {
            // 예상 정보가 없으면 비활성. 있으면 RefreshPreview가 CanStartDelivery로 제어.
            if (_startButton == null) return;
            bool ready = _selectedLetterID > 0 && _selectedCourierID > 0 && _selectedRouteID > 0;
            if (!ready) _startButton.interactable = false;
        }

        private void HighlightLetters()
        {
            foreach (var it in _letterItems)
                if (it != null) it.SetSelected(it.LetterId == _selectedLetterID);
        }

        private void ResetSelection()
        {
            _selectedLetterID = -1;
            _selectedCourierID = -1;
            _selectedRouteID = -1;
        }

        /// <summary>편지 미선택 상태로 되돌린다. 배달부·노선 목록을 비우고 안내를 띄운다.</summary>
        private void ClearOptionsForNoLetter()
        {
            ClearCourierList();
            ClearRouteList();
            ShowCourierEmpty("편지를 먼저 선택하세요.");
            ShowRouteEmpty("편지를 먼저 선택하세요.");
        }

        private void ClearLetterList() { for (int i = 0; i < _letterItems.Count; i++) if (_letterItems[i] != null) Destroy(_letterItems[i].gameObject); _letterItems.Clear(); }
        private void ClearCourierList() { for (int i = 0; i < _courierItems.Count; i++) if (_courierItems[i] != null) Destroy(_courierItems[i].gameObject); _courierItems.Clear(); }
        private void ClearRouteList() { for (int i = 0; i < _routeItems.Count; i++) if (_routeItems[i] != null) Destroy(_routeItems[i].gameObject); _routeItems.Clear(); }

        // ── 이벤트 ──
        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameEvents.DeliveryStarted += OnDeliveryChanged;
            GameEvents.DeliveryCompleted += OnDeliveryCompleted;
            GameEvents.LetterStateChanged += OnLetterStateChanged;
            GameEvents.FacilityUpgraded += OnFacilityUpgraded;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.DeliveryStarted -= OnDeliveryChanged;
            GameEvents.DeliveryCompleted -= OnDeliveryCompleted;
            GameEvents.LetterStateChanged -= OnLetterStateChanged;
            GameEvents.FacilityUpgraded -= OnFacilityUpgraded;
        }

        private void OnDeliveryChanged(int letterID, int courierID, int routeID)
        {
            if (!_isOpen) return;
            RefreshWaitingLetters();
            if (_selectedLetterID > 0) RefreshCouriers();
        }

        private void OnDeliveryCompleted(int letterID)
        {
            if (_isOpen) RefreshCouriers();
        }

        private void OnLetterStateChanged(int letterID, ELetterProgressState state)
        {
            if (_isOpen) RefreshWaitingLetters();
        }

        private void OnFacilityUpgraded(int facilityID, int currentLevel)
        {
            if (_isOpen) RefreshPreview(); // 시설 효과 바뀌면 예상 시간 재계산
        }

        // ── enum → 표시명 ──
        private static string VehicleLabel(EVehicleType v)
        {
            switch (v)
            {
                case EVehicleType.Walking: return "도보";
                case EVehicleType.Bicycle: return "자전거";
                case EVehicleType.Motorcycle: return "오토바이";
                case EVehicleType.Truck: return "트럭";
                default: return "-";
            }
        }

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

        private static string FormatTime(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            if (s < 60) return $"{s}초";
            int m = s / 60, r = s % 60;
            return r == 0 ? $"{m}분" : $"{m}분 {r}초";
        }
    }
}
