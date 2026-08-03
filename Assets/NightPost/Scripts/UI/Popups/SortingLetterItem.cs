using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 분류 UI의 편지 목록 한 줄. New 상태 편지만 여기 표시된다.
    /// 클릭하면 자신의 letterID를 콜백으로 알린다. 미열람이면 새 표시(_newBadge)를 켠다.
    /// </summary>
    public class SortingLetterItem : MonoBehaviour
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

        private void Raise() => _onClick?.Invoke(_letterId);
    }
}
