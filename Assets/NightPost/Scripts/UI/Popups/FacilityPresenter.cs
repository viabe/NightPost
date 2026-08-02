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

        /// <summary>시설 상세 열기. Station UnityEvent나 테스트 버튼에서 호출.</summary>
        public void OpenFacility(int facilityId)
        {
            if (_flow == null) return;
            if (!_flow.SelectFacility(facilityId))
            {
                Debug.LogWarning($"[Facility] 시설 선택 실패 id={facilityId}");
                return;
            }
            _displayedFacilityId = facilityId;

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
                CurrentLevel = currentLevel,
                MaxLevel = facility != null ? facility.MaxLevel : 0,
                CurrentEffectText = EffectText(current, isCurrent: true),
                NextEffectText = isMax ? string.Empty : EffectText(next, isCurrent: false),
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

        // EffectType별 문구 규칙 (명세서 v1.2 §9)
        private static string EffectText(FacilityLevelData data, bool isCurrent)
        {
            if (data == null) return "효과 없음";
            switch (data.EffectType)
            {
                case EFacilityEffectType.DeliveryTimeReduction:
                {
                    int pct = Mathf.RoundToInt(data.EffectValue * 100f);
                    return isCurrent ? $"배달 시간 {pct}% 감소" : $"업그레이드 후 {pct}% 감소";
                }
                case EFacilityEffectType.LetterCapacityIncrease:
                {
                    int n = Mathf.FloorToInt(data.EffectValue);
                    return isCurrent ? $"편지 보유 한도 +{n}통" : $"업그레이드 후 +{n}통";
                }
                default:
                    return "적용 효과 없음";
            }
        }

        private FacilityDetailController GetPopup()
            => PopupManager.Instance != null
                ? PopupManager.Instance.Get<FacilityDetailController>(UIScreenId.Facility) : null;
    }
}
