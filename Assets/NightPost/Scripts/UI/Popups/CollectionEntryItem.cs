using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 도감 격자 한 칸. 편지 탭과 답장 탭이 공용으로 쓴다.
    /// 미수신 항목은 잠금 상태로 표시하며 제목·발신자를 노출하지 않는다(스포일러 방지).
    ///
    /// 썸네일 이미지는 아직 아트가 없어 자리만 잡아둔다.
    /// 스프라이트가 준비되면 Setup에 넘겨 _thumbnail에 지정하면 된다.
    /// </summary>
    public class CollectionEntryItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _thumbnail;        // [추후] 편지/답장 아트
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _subText;       // 발신자
        [SerializeField] private GameObject _lockOverlay; // 미수신 표시
        [SerializeField] private GameObject _unreadMark;  // 수신했지만 안 읽음
        [SerializeField] private GameObject _selectedMark;
        [SerializeField] private Button _button;

        private int _id;
        private bool _isLocked;
        private Action<int> _onClick;

        public int Id => _id;

        /// <summary>
        /// 격자 칸 초기화.
        /// isLocked면 제목을 물음표로 덮고 클릭을 막는다.
        /// </summary>
        public void Setup(int id, string title, string sub, bool isLocked, bool isUnread, Action<int> onClick)
        {
            _id = id;
            _isLocked = isLocked;
            _onClick = onClick;

            if (_titleText != null) _titleText.text = isLocked ? "???" : title;
            if (_subText != null) _subText.text = isLocked ? "아직 받지 못한 편지" : sub;
            if (_lockOverlay != null) _lockOverlay.SetActive(isLocked);
            if (_unreadMark != null) _unreadMark.SetActive(!isLocked && isUnread);
            if (_thumbnail != null) _thumbnail.enabled = !isLocked; // 아트 적용 전까지 자리만

            SetSelected(false);

            // 루트 Button만 자동 보정한다(자식 Button은 그 영역에서만 눌림).
            if (_button == null) _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = !isLocked;
                if (!isLocked) _button.onClick.AddListener(Raise);
            }
        }

        public void SetSelected(bool on)
        {
            if (_selectedMark != null) _selectedMark.SetActive(on);
        }

        /// <summary>칸 아무 데나 눌러도 선택되게 한다. 루트 Button이 있으면 중복 방지로 넘긴다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isLocked) return;
            if (_button != null && _button.gameObject == gameObject) return; // Button이 처리함
            Raise();
        }

        private void Raise() => _onClick?.Invoke(_id);
    }
}
