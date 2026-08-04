using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 분류 UI의 편지 목록 한 줄. New 상태 편지만 여기 표시된다.
    /// 클릭하면 자신의 letterID를 콜백으로 알린다. 미열람이면 새 표시(_newBadge)를 켠다.
    ///
    /// 클릭 처리: 루트에 Button이 있으면 Button이, 없으면 IPointerClickHandler가 받는다.
    /// (Button이 자식에만 있으면 카드 여백을 눌렀을 때 이벤트가 루트까지 올라와도
    ///  처리할 컴포넌트가 없어 아무 반응이 없다. 그 경우를 이 핸들러가 덮는다)
    /// </summary>
    public class SortingLetterItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _sender;
        [SerializeField] private GameObject _newBadge;   // 아직 안 읽은 편지 표시
        [SerializeField] private GameObject _selectedMark; // 현재 선택된 편지 표시(선택)
        [SerializeField] private Button _button;

        private int _letterId;
        private Action<int> _onClick;

        public int LetterId => _letterId;

        public void Setup(int letterId, string title, string sender, bool isRead, Action<int> onClick)
        {
            _letterId = letterId;
            _onClick = onClick;
            if (_title != null) _title.text = title;
            if (_sender != null) _sender.text = sender;
            if (_newBadge != null) _newBadge.SetActive(!isRead);
            SetSelected(false);

            // 루트 Button만 자동 보정한다. 자식 Button은 그 영역에서만 눌리므로
            // 카드 전체 클릭은 아래 OnPointerClick이 담당한다.
            if (_button == null) _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(Raise);
            }
        }

        public void SetSelected(bool on)
        {
            if (_selectedMark != null) _selectedMark.SetActive(on);
        }

        /// <summary>카드 어디를 눌러도 선택되게 한다. 루트 Button이 있으면 중복 방지로 넘긴다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && _button.gameObject == gameObject) return; // Button이 처리함
            Raise();
        }

        private void Raise() => _onClick?.Invoke(_letterId);
    }
}
