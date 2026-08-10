using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 시설 강화 목록 UI. 하단 메뉴바의 "시설" 버튼에서 Open()으로 열린다.
    /// 보유한 시설 3종(분류대·배달차고·편지보관함)을 한 화면에 보여주고,
    /// 항목을 누르면 기존 상세 팝업(FacilityPresenter.OpenFacility)으로 넘긴다.
    ///
    /// 사용 API(FacilityService):
    ///   GetFacilities() / GetCurrentFacilityLevel(id)
    ///   GetCurrentLevelData(id) / GetNextLevelData(id) / CanUpgradeFacility(id)
    /// 강화 실행과 상세 표시는 FacilityPresenter가 담당한다(중복 구현하지 않는다).
    /// </summary>
    public class FacilityListController : MonoBehaviour, IUIScreen
    {
        /// <summary>
        /// 시설 ID ↔ 아이콘 매핑. 정적 데이터에는 아이콘이 없으므로
        /// 표현 영역인 UI에서 들고 있는다(인스펙터에서 지정).
        /// </summary>
        [System.Serializable]
        public class FacilityIcon
        {
            public int FacilityId;
            public Sprite Icon;
        }

        [Header("의존성")]
        [SerializeField] private FacilityService _facilityService;
        [SerializeField] private FacilityPresenter _facilityPresenter;
        [SerializeField] private PlayerController _playerController;

        [Header("패널")]
        [SerializeField] private GameObject _panel;

        [Header("목록")]
        [SerializeField] private Transform _listRoot;
        [SerializeField] private FacilityListItem _itemPrefab;
        [SerializeField] private GameObject _emptyState;

        [Header("아이콘 (시설 ID로 매칭)")]
        [SerializeField] private FacilityIcon[] _icons;

        [SerializeField] private Button _closeButton;

        private bool _isOpen;
        private bool _subscribed;

        private readonly List<FacilityListItem> _items = new();

        /// <summary>다른 화면이 열릴 때 닫아야 하는지 판단하는 데 쓰인다.</summary>
        public bool IsScreenOpen => _isOpen;

        private void OnDestroy() => UIScreenRouter.Unregister(this);

        private void Awake()
        {
            UIScreenRouter.Register(this);
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

            // 서로 대체 관계인 화면이므로 열려 있던 다른 화면을 먼저 닫는다.
            UIScreenRouter.NotifyOpened(this);

            if (_panel != null) _panel.SetActive(true);
            if (_playerController != null) _playerController.SetControlEnabled(false);

            if (_facilityService == null) Debug.LogError("[FacilityList] FacilityService 미연결", this);
            if (_facilityPresenter == null) Debug.LogError("[FacilityList] FacilityPresenter 미연결", this);

            Subscribe();
            RefreshList();
        }

        public void Close()
        {
            // _isOpen 여부와 무관하게 항상 닫는다(에디터에서 패널을 켜둔 채 실행한 경우 대비).
            _isOpen = false;

            Unsubscribe();

            if (_panel != null) _panel.SetActive(false);
            if (_playerController != null) _playerController.SetControlEnabled(true);
        }

        // ── 목록 ──
        private void RefreshList()
        {
            ClearList();
            if (_facilityService == null || _itemPrefab == null || _listRoot == null) return;

            IReadOnlyList<FacilityStaticData> facilities = _facilityService.GetFacilities();
            int count = 0;

            if (facilities != null)
            {
                foreach (FacilityStaticData facility in facilities)
                {
                    if (facility == null) continue;

                    int id = facility.FacilityID;
                    int level = _facilityService.GetCurrentFacilityLevel(id);
                    FacilityLevelData current = _facilityService.GetCurrentLevelData(id);
                    bool isMax = _facilityService.GetNextLevelData(id) == null;

                    FacilityListItem item = Instantiate(_itemPrefab, _listRoot);
                    item.gameObject.SetActive(true);
                    item.Setup(
                        id,
                        GetIcon(id),
                        facility.FacilityName,
                        level,
                        facility.MaxLevel,
                        UILabels.FacilityEffect(current, isCurrent: true),
                        _facilityService.CanUpgradeFacility(id),
                        isMax,
                        OnFacilitySelected);

                    _items.Add(item);
                    count++;
                }
            }

            if (_emptyState != null) _emptyState.SetActive(count == 0);
        }

        /// <summary>인스펙터에 등록된 아이콘 중 시설 ID가 맞는 것을 찾는다. 없으면 null.</summary>
        private Sprite GetIcon(int facilityId)
        {
            if (_icons == null) return null;
            foreach (FacilityIcon entry in _icons)
                if (entry != null && entry.FacilityId == facilityId) return entry.Icon;
            return null;
        }

        // 상세 표시와 강화는 기존 Presenter에 맡긴다.
        private void OnFacilitySelected(int facilityId)
        {
            if (_facilityPresenter == null)
            {
                Debug.LogError("[FacilityList] FacilityPresenter 미연결 — 상세 팝업이 열리지 않는다", this);
                return;
            }
            _facilityPresenter.OpenFacility(facilityId);
        }

        private void ClearList()
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] != null) Destroy(_items[i].gameObject);
            _items.Clear();
        }

        // ── 이벤트 ──
        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            // 강화하면 레벨·효과가, 재화가 바뀌면 강화 가능 여부가 달라진다.
            GameEvents.FacilityUpgraded += OnFacilityUpgraded;
            GameEvents.CurrencyChanged += OnCurrencyChanged;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GameEvents.FacilityUpgraded -= OnFacilityUpgraded;
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
        }

        private void OnFacilityUpgraded(int facilityID, int currentLevel) { if (_isOpen) RefreshList(); }
        private void OnCurrencyChanged(int currentCurrency) { if (_isOpen) RefreshList(); }
    }
}
