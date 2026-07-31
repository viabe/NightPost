using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 편지 열람. 답장 본문(제목/발신자/본문)을 읽는 화면.
    /// 서사 편지·할아버지 편지 등 다른 본문에도 재사용 가능한 표시 전용 뷰.
    /// 본문은 긴 텍스트라 스크롤 뷰 안에 두는 것을 권장한다.
    /// </summary>
    public class LetterReadController : BaseView<LetterReadModel>
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _sender;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private Button _closeButton;

        protected override void Bind(LetterReadModel model)
        {
            if (_title != null) _title.text = model.Title;
            if (_sender != null) _sender.text = model.Sender;
            if (_body != null) _body.text = model.Body;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        private void CloseSelf()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.Close(this);
            else Close();
        }

        protected override void OnClose()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>편지/답장 본문 표시 모델.</summary>
    public struct LetterReadModel
    {
        public string Title;
        public string Sender;
        public string Body;
    }
}
