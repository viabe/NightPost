using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 시설 강화 Presenter. 시설 상세 팝업을 실제 시설 시스템에 연결한다.
    ///   - OpenFacility(id): SelectFacility → 조회해서 상세 표시
    ///   - 업그레이드 버튼 → UpgradeSelectedFacility (실제 갱신은 FacilityUpgraded 이벤트에서)
    ///   - FacilityUpgraded / CurrencyChanged 구독 → 현재 표시 중인 시설이면 다시 조회·갱신
    ///
    /// 진입: 월드 시설(Station_Facility)의 UnityEvent 또는 테스트 버튼에서 OpenFacility(id) 호출.
    /// 참조: 시설 시스템 UI 연동 명세서 v1.2.
    /// </summary>
    public class FacilityPresenter : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private FacilityService _facilityService;
        [SerializeField] private StaticDataCatalog _catalog;
        [SerializeField] private PlayerDataManager _playerData;

        private int _displayedFacilityId;
        // 아이콘은 정적 데이터에 없고 목록 UI가 들고 있다. 열 때 받아두고 갱신 때도 계속 쓴다.
        private Sprite _displayedIcon;

        private void OnEnable()
        {
            GameEvents.FacilityUpgraded += OnFacilityUpgraded;
            GameEvents.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            GameEvents.FacilityUpgraded -= OnFacilityUpgraded;
            GameEvents.CurrencyChanged -= OnCurrencyChanged;
        }

        /// <summary>
        /// 시설 상세 열기. Station UnityEvent나 테스트 버튼에서 호출.
        /// 아이콘 없이 열리므로 팝업의 아이콘 자리는 숨겨진다.
        /// </summary>
        public void OpenFacility(int facilityId) => OpenFacility(facilityId, null);

        /// <summary>아이콘까지 지정해 시설 상세를 연다. 시설 목록에서 넘어올 때 쓴다.</summary>
        public void OpenFacility(int facilityId, Sprite icon)
        {
            if (_flow == null) return;
            if (!_flow.SelectFacility(facilityId))
            {
                Debug.LogWarning($"[Facility] 시설 선택 실패 id={facilityId}");
                return;
            }
            _displayedFacilityId = facilityId;
            _displayedIcon = icon;

            FacilityDetailController popup = GetPopup();
            if (popup == null) { Debug.LogWarning("[Facility] 팝업 미등록 — Id가 Facility인지 확인"); return; }
            popup.Open(BuildModel(facilityId));
        }

        private void OnFacilityUpgraded(int facilityId, int currentLevel) => RefreshIfDisplayed(facilityId);
        private void OnCurrencyChanged(int currentCurrency) => RefreshIfDisplayed(_displayedFacilityId);

        // 현재 표시 중인 시설이고 팝업이 열려 있으면 다시 조회해 갱신한다.
        private void RefreshIfDisplayed(int facilityId)
        {
            if (facilityId != _displayedFacilityId || _displayedFacilityId <= 0) return;
            FacilityDetailController popup = GetPopup();
            if (popup == null || !popup.IsOpen) return;
            popup.Open(BuildModel(_displayedFacilityId));
        }

        private FacilityDetailModel BuildModel(int facilityId)
        {
            FacilityStaticData facility = _catalog != null ? _catalog.GetFacility(facilityId) : null;
            FacilityProgressData progress = _playerData != null ? _playerData.GetFacilityProgress(facilityId) : null;
            FacilityLevelData current = _facilityService != null ? _facilityService.GetCurrentLevelData(facilityId) : null;
            FacilityLevelData next = _facilityService != null ? _facilityService.GetNextLevelData(facilityId) : null;

            int currentLevel = progress == null ? 0 : progress.CurrentLevel;
            bool isMax = next == null;

            return new FacilityDetailModel
            {
                FacilityId = facilityId,
                Name = facility != null ? facility.FacilityName : $"시설 {facilityId}",
                Description = facility != null ? facility.Description : string.Empty,
                Icon = _displayedIcon,
                CurrentLevel = currentLevel,
                MaxLevel = facility != null ? facility.MaxLevel : 0,
                CurrentEffectText = UILabels.FacilityEffect(current, isCurrent: true),
                NextEffectText = isMax ? string.Empty : UILabels.FacilityEffect(next, isCurrent: false),
                UpgradeCost = isMax ? 0 : next.UpgradeCost,
                IsMax = isMax,
                CanUpgrade = _facilityService != null && _facilityService.CanUpgradeFacility(facilityId),
                OnUpgrade = OnUpgrade,
            };
        }

        // 실제 갱신은 FacilityUpgraded 이벤트가 담당하므로 여기서는 호출만 한다.
        private void OnUpgrade()
        {
            if (_flow == null) return;
            if (!_flow.UpgradeSelectedFacility())
                Debug.LogWarning("[Facility] 업그레이드 실패(재화 부족·최대 레벨 등)");
        }

        private FacilityDetailController GetPopup()
            => PopupManager.Instance != null
                ? PopupManager.Instance.Get<FacilityDetailController>(UIScreenId.Facility) : null;
    }
}
