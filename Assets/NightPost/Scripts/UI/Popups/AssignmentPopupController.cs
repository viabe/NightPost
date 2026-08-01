using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 배정 팝업. 편지 겉면에서 "배정하기"로 진입한다.
    /// 배달부 1명 + 노선 1개를 골라 "배달 시작"을 누르면 배달이 시작된다.
    /// (SaveData 명세: Waiting → Delivering 직행, Assigned 중간 상태 없음)
    ///
    /// ConfirmPopup/EnvelopePopup과 같은 BaseView&lt;TModel&gt; 패턴이며 도메인 비의존.
    ///   - 배달부/노선은 plain 옵션 리스트로 받는다(CourierStaticData/RouteStaticData 아님).
    ///   - 예상 시간·보상 계산은 도메인이므로 Model.Estimate 콜백에 위임한다.
    ///   - "배달 시작"은 선택된 (courierId, routeId)를 OnStartDelivery로 넘길 뿐,
    ///     실제 시작·저장은 바깥(서비스)이 담당한다.
    /// </summary>
    public class AssignmentPopupController : BaseView<AssignmentModel>
    {
        [Header("헤더 (어떤 편지 배정 중)")]
        [SerializeField] private TMP_Text _letterTitle;
        [SerializeField] private TMP_Text _region;
        [SerializeField] private GameObject _urgentMark;

        [Header("배달부 목록")]
        [SerializeField] private Transform _courierListRoot;         // Layout Group 권장
        [SerializeField] private AssignmentOptionItem _courierItemPrefab;

        [Header("노선 목록")]
        [SerializeField] private Transform _routeListRoot;
        [SerializeField] private AssignmentOptionItem _routeItemPrefab;

        [Header("예상치 / 실행")]
        [SerializeField] private TMP_Text _estimateText;   // "예상 3분 · 보상 30"
        [SerializeField] private Button _startButton;      // 배달 시작 (둘 다 선택 시 활성)
        [SerializeField] private Button _closeButton;

        private readonly List<AssignmentOptionItem> _courierItems = new();
        private readonly List<AssignmentOptionItem> _routeItems = new();

        private int _selectedCourierId;
        private int _selectedRouteId;
        private bool _hasCourier;
        private bool _hasRoute;

        protected override void Bind(AssignmentModel model)
        {
            _hasCourier = false;
            _hasRoute = false;
            _selectedCourierId = 0;
            _selectedRouteId = 0;

            if (_letterTitle != null) _letterTitle.text = model.LetterTitle;
            if (_region != null) _region.text = model.RegionLabel;
            if (_urgentMark != null) _urgentMark.SetActive(model.IsUrgent);

            BuildCouriers(model.Couriers);
            BuildRoutes(model.Routes);
            RefreshEstimateAndStart();

            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(OnStartClicked);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        private void BuildCouriers(List<CourierOption> couriers)
        {
            ClearItems(_courierItems);
            if (couriers == null || _courierItemPrefab == null || _courierListRoot == null) return;

            foreach (var c in couriers)
            {
                var item = Instantiate(_courierItemPrefab, _courierListRoot);
                item.gameObject.SetActive(true); // 템플릿이 비활성이어도 복제본은 켠다
                item.Setup(c.CourierId, c.Name, c.VehicleLabel, c.IsAvailable, OnCourierSelected);
                _courierItems.Add(item);
            }
        }

        private void BuildRoutes(List<RouteOption> routes)
        {
            ClearItems(_routeItems);
            if (routes == null || _routeItemPrefab == null || _routeListRoot == null) return;

            foreach (var r in routes)
            {
                var item = Instantiate(_routeItemPrefab, _routeListRoot);
                item.gameObject.SetActive(true);
                item.Setup(r.RouteId, r.Name, r.DifficultyLabel, r.IsUnlocked, OnRouteSelected);
                _routeItems.Add(item);
            }
        }

        private void OnCourierSelected(int id)
        {
            _selectedCourierId = id;
            _hasCourier = true;
            foreach (var it in _courierItems) it.SetSelected(it.Id == id);
            RefreshEstimateAndStart();
        }

        private void OnRouteSelected(int id)
        {
            _selectedRouteId = id;
            _hasRoute = true;
            foreach (var it in _routeItems) it.SetSelected(it.Id == id);
            RefreshEstimateAndStart();
        }

        private void RefreshEstimateAndStart()
        {
            bool ready = _hasCourier && _hasRoute;
            if (_startButton != null) _startButton.interactable = ready;

            if (_estimateText == null) return;

            if (ready && Model.Estimate != null)
            {
                var est = Model.Estimate(_selectedCourierId, _selectedRouteId);
                _estimateText.text = $"예상 {FormatTime(est.Seconds)} · 보상 {est.Reward:N0}";
            }
            else
            {
                _estimateText.text = "배달부와 노선을 선택하세요";
            }
        }

        private void OnStartClicked()
        {
            if (!_hasCourier || !_hasRoute) return; // 방어(버튼이 비활성이라 보통 안 걸림)
            // 성공했을 때만 닫는다. 실패면 팝업을 유지해 다른 배달부/노선으로 재시도할 수 있게 한다.
            bool ok = Model.OnStartDelivery != null && Model.OnStartDelivery(_selectedCourierId, _selectedRouteId);
            if (ok) CloseSelf();
        }

        private void ClearItems(List<AssignmentOptionItem> items)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null) Destroy(items[i].gameObject);
            items.Clear();
        }

        private void CloseSelf()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.Close(this);
            else Close();
        }

        protected override void OnClose()
        {
            ClearItems(_courierItems);
            ClearItems(_routeItems);
            if (_startButton != null) _startButton.onClick.RemoveAllListeners();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        }

        private static string FormatTime(int seconds)
        {
            if (seconds < 60) return $"{seconds}초";
            int m = seconds / 60, s = seconds % 60;
            return s == 0 ? $"{m}분" : $"{m}분 {s}초";
        }
    }

    /// <summary>
    /// 배정 팝업 모델. 도메인 타입에 의존하지 않도록 표시용 값만 담는다.
    /// 실제 값은 OwnedCourierIDs+CourierStaticData / UnlockedRouteIDs+RouteStaticData에서
    /// 서비스가 채운다. Estimate는 배달부 속도·특성·노선·설비를 아는 도메인 계산이므로 콜백.
    /// </summary>
    public struct AssignmentModel
    {
        public int LetterId;         // 배정 대상 편지 ID
        public string LetterTitle;   // 헤더에 표시할 편지 제목
        public string RegionLabel;   // 편지 목적 지역 표시명
        public bool IsUrgent;

        public List<CourierOption> Couriers; // 보유 배달부(배달 중이면 IsAvailable=false)
        public List<RouteOption> Routes;     // 편지 지역에 맞는 노선(잠김이면 IsUnlocked=false)

        public Func<int, int, DeliveryEstimate> Estimate; // (courierId, routeId) → 예상 시간·보상
        public Func<int, int, bool> OnStartDelivery;      // (courierId, routeId) → 배달 시작. 성공 시 true면 팝업 닫힘
    }

    /// <summary>배달부 선택 항목(표시용).</summary>
    public struct CourierOption
    {
        public int CourierId;
        public string Name;
        public string VehicleLabel;  // "도보" / "자전거" 등
        public bool IsAvailable;     // 이미 배달 중이면 false
    }

    /// <summary>노선 선택 항목(표시용).</summary>
    public struct RouteOption
    {
        public int RouteId;
        public string Name;
        public string DifficultyLabel; // "쉬움" / "보통" / "어려움"
        public bool IsUnlocked;        // 잠긴 노선이면 false
    }

    /// <summary>선택 조합에 대한 예상 결과(도메인 계산 결과).</summary>
    public struct DeliveryEstimate
    {
        public int Seconds;
        public int Reward;
    }
}
