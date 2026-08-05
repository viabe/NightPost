using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 시설 목록 한 줄. 이름·레벨·현재 효과를 보여주고,
    /// 강화 가능하면 강조 표시를 켠다. 누르면 상세 팝업으로 넘긴다.
    /// </summary>
    public class FacilityListItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _icon;                // 시설 아이콘
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _levelText;        // "Lv.1 / 3"
        [SerializeField] private TMP_Text _effectText;       // "배달 시간 10% 감소"
        [SerializeField] private GameObject _upgradableMark; // 강화 가능(재화 충분)
        [SerializeField] private GameObject _maxMark;        // 최대 레벨
        [SerializeField] private Button _button;

        private int _id;
        private Action<int> _onClick;

        public int Id => _id;

        public void Setup(int id, Sprite icon, string name, int level, int maxLevel, string effect,
                          bool canUpgrade, bool isMax, Action<int> onClick)
        {
            _id = id;
            _onClick = onClick;

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null; // 아이콘 미지정이면 빈 사각형이 보이지 않게 끈다
            }
            if (_nameText != null) _nameText.text = name;
            if (_levelText != null) _levelText.text = $"Lv.{level} / {maxLevel}";
            if (_effectText != null) _effectText.text = effect;
            if (_maxMark != null) _maxMark.SetActive(isMax);
            if (_upgradableMark != null) _upgradableMark.SetActive(!isMax && canUpgrade);

            // 루트 Button만 자동 보정한다(자식 Button은 그 영역에서만 눌림).
            if (_button == null) _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(Raise);
            }
        }

        /// <summary>행 아무 데나 눌러도 열리게 한다. 루트 Button이 있으면 중복 방지로 넘긴다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && _button.gameObject == gameObject) return; // Button이 처리함
            Raise();
        }

        private void Raise() => _onClick?.Invoke(_id);
    }
}
