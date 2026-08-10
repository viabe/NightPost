using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 시설 강화 상세. 시설 하나의 이름/설명/레벨/현재·다음 효과/비용을 보여주고 업그레이드한다.
    /// 시설 시스템 UI 연동 명세서 v1.2 기준. 도메인 비의존: 효과 문구·비용은 표시용 값으로 받는다.
    ///
    /// 업그레이드 버튼은 팝업을 닫지 않는다(연속 업그레이드). 실제 갱신은
    /// FacilityUpgraded 이벤트를 받은 Presenter가 이 팝업을 다시 Bind해서 처리한다.
    /// </summary>
    public class FacilityDetailController : BaseView<FacilityDetailModel>
    {
        [Header("정보")]
        [Tooltip("시설 아이콘. 모델에 그림이 없으면 빈 사각형이 보이지 않게 통째로 끈다.")]
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private TMP_Text _levelText;         // "1 / 3"
        [SerializeField] private TMP_Text _currentEffectText; // "배달 시간 5% 감소"
        [SerializeField] private TMP_Text _nextEffectText;    // "업그레이드 후 10% 감소"

        [Header("업그레이드")]
        [SerializeField] private TMP_Text _costText;          // 비용
        [SerializeField] private GameObject _maxLabel;        // MAX 표시(최대 레벨)
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _closeButton;

        protected override void Bind(FacilityDetailModel model)
        {
            if (_icon != null)
            {
                _icon.sprite = model.Icon;
                _icon.enabled = model.Icon != null; // 아이콘 미지정이면 빈 사각형이 보이지 않게 끈다
            }
            if (_name != null) _name.text = model.Name;
            if (_description != null) _description.text = model.Description;
            if (_levelText != null) _levelText.text = $"{model.CurrentLevel} / {model.MaxLevel}";
            if (_currentEffectText != null) _currentEffectText.text = model.CurrentEffectText;

            // 다음 효과: 최대 레벨이면 숨기고 MAX 표시
            if (_nextEffectText != null)
            {
                _nextEffectText.gameObject.SetActive(!model.IsMax);
                _nextEffectText.text = model.NextEffectText;
            }
            if (_maxLabel != null) _maxLabel.SetActive(model.IsMax);

            // 비용: 최대면 숨김, 0이면 무료
            if (_costText != null)
            {
                _costText.gameObject.SetActive(!model.IsMax);
                _costText.text = model.UpgradeCost <= 0 ? "무료" : model.UpgradeCost.ToString("N0");
            }

            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = model.CanUpgrade; // 재화 부족·최대 레벨이면 비활성
                _upgradeButton.onClick.RemoveAllListeners();
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        // 업그레이드는 팝업을 닫지 않는다. 성공 시 Presenter가 FacilityUpgraded로 다시 Bind한다.
        private void OnUpgradeClicked() => Model.OnUpgrade?.Invoke();

        private void CloseSelf()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.Close(this);
            else Close();
        }

        protected override void OnClose()
        {
            if (_upgradeButton != null) _upgradeButton.onClick.RemoveAllListeners();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>시설 강화 상세 표시 모델. 효과 문구·비용은 Presenter가 EffectType 규칙대로 만들어 넣는다.</summary>
    public struct FacilityDetailModel
    {
        public int FacilityId;
        public string Name;
        public string Description;
        public Sprite Icon;              // null이면 아이콘을 숨긴다
        public int CurrentLevel;
        public int MaxLevel;
        public string CurrentEffectText; // "효과 없음" / "배달 시간 10% 감소" / "편지 보유 한도 +4통"
        public string NextEffectText;    // "업그레이드 후 …" (최대면 무시)
        public int UpgradeCost;          // 0이면 무료
        public bool IsMax;               // 다음 레벨 데이터 없음
        public bool CanUpgrade;          // CanUpgradeFacility 결과
        public Action OnUpgrade;         // 업그레이드 버튼 → UpgradeSelectedFacility
    }
}
