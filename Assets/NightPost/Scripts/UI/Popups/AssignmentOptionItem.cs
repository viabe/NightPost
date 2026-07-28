using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 배정 팝업의 선택 항목. 배달부 리스트와 노선 리스트가 공용으로 쓴다.
    /// 리스트 루트 밑에 복제되며, 클릭하면 자신의 Id를 콜백으로 알린다.
    /// 선택 강조(_selectedFrame)와 선택 불가 표시(_lockedOverlay)를 토글한다.
    /// (배달 중인 배달부·잠긴 노선은 selectable=false로 넘겨 버튼을 막는다)
    /// </summary>
    public class AssignmentOptionItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;      // 배달부/노선 이름
        [SerializeField] private TMP_Text _subText;       // 이동수단 / 난이도 등 부가정보
        [SerializeField] private GameObject _selectedFrame; // 선택 강조 테두리
        [SerializeField] private GameObject _lockedOverlay; // 선택 불가(잠금/배달중) 오버레이
        [SerializeField] private Button _button;

        private int _id;
        private Action<int> _onClick;

        public int Id => _id;

        /// <summary>항목 초기화. selectable=false면 클릭 불가 + 잠금 표시.</summary>
        public void Setup(int id, string name, string sub, bool selectable, Action<int> onClick)
        {
            _id = id;
            _onClick = onClick;

            if (_nameText != null) _nameText.text = name;
            if (_subText != null) _subText.text = sub;
            if (_lockedOverlay != null) _lockedOverlay.SetActive(!selectable);
            SetSelected(false);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = selectable;
                if (selectable) _button.onClick.AddListener(Raise);
            }
        }

        public void SetSelected(bool on)
        {
            if (_selectedFrame != null) _selectedFrame.SetActive(on);
        }

        private void Raise() => _onClick?.Invoke(_id);
    }
}
