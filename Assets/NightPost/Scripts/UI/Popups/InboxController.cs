using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 수신함. 배달로 돌아온 답장 목록을 보여준다. 행을 누르면 답장 본문(LetterRead)이 열린다.
    /// 도메인 비의존: 표시용 InboxRow 목록 + 열기 콜백만 받는다.
    /// </summary>
    public class InboxController : BaseView<InboxModel>
    {
        [Header("헤더")]
        [SerializeField] private TMP_Text _headerText;

        [Header("답장 목록")]
        [SerializeField] private Transform _rowRoot;        // Layout Group 권장
        [SerializeField] private InboxRowItem _rowPrefab;
        [SerializeField] private GameObject _emptyLabel;    // 받은 답장 없을 때

        [SerializeField] private Button _closeButton;

        private readonly List<InboxRowItem> _rows = new();

        protected override void Bind(InboxModel model)
        {
            ClearRows();

            int count = model.Rows != null ? model.Rows.Count : 0;
            if (_headerText != null) _headerText.text = $"받은 답장 {count}통";
            if (_emptyLabel != null) _emptyLabel.SetActive(count == 0);

            if (model.Rows != null && _rowPrefab != null && _rowRoot != null)
            {
                foreach (InboxRow r in model.Rows)
                {
                    InboxRowItem item = Instantiate(_rowPrefab, _rowRoot);
                    item.gameObject.SetActive(true);
                    item.Setup(r.ReplyId, r.SenderName, r.Title, r.IsRead, model.OnOpenReply);
                    _rows.Add(item);
                }
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
                if (_rows[i] != null) Destroy(_rows[i].gameObject);
            _rows.Clear();
        }

        private void CloseSelf()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.Close(this);
            else Close();
        }

        protected override void OnClose()
        {
            ClearRows();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>수신함 모델. 답장 행 목록 + 행 클릭 시 열기 콜백.</summary>
    public struct InboxModel
    {
        public List<InboxRow> Rows;
        public Action<int> OnOpenReply;  // (replyId) → 답장 본문 열기
    }

    /// <summary>수신함 한 줄의 표시 값.</summary>
    public struct InboxRow
    {
        public int ReplyId;
        public string SenderName;
        public string Title;
        public bool IsRead;   // false면 미열람 점 표시
    }
}
