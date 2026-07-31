using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 수신함의 답장 한 줄. 클릭하면 자신의 replyId를 콜백으로 알린다.
    /// 미열람이면 점(_unreadDot)을 표시한다.
    /// </summary>
    public class InboxRowItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _sender;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private GameObject _unreadDot;
        [SerializeField] private Button _button;

        private int _replyId;
        private Action<int> _onClick;

        public void Setup(int replyId, string sender, string title, bool isRead, Action<int> onClick)
        {
            _replyId = replyId;
            _onClick = onClick;

            if (_sender != null) _sender.text = sender;
            if (_title != null) _title.text = title;
            if (_unreadDot != null) _unreadDot.SetActive(!isRead);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(Raise);
            }
        }

        private void Raise() => _onClick?.Invoke(_replyId);
    }
}
