using System.Collections.Generic;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 수신함 Presenter. HUD 수신함 버튼과 답장 이벤트를 잇는다.
    ///   - HUD의 InboxClicked → 수신함 열기
    ///   - 행 클릭 → GameFlowController로 답장 열기(읽음 처리) → LetterRead 본문 표시
    ///   - ReplyReceived/ReplyRead 구독 → 수신함이 열려 있으면 목록 갱신(미열람 점 등)
    ///
    /// 받은 답장 목록은 ReplyService.GetReceivedReplies()로 조회한다.
    /// </summary>
    public class InboxPresenter : MonoBehaviour
    {
        [SerializeField] private HUDController _hud;         // 수신함 버튼 이벤트 소스
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayerDataManager _playerData; // 읽음 여부 조회
        [SerializeField] private ReplyService _replyService;    // 받은 답장 목록 조회

        private void OnEnable()
        {
            if (_hud != null) _hud.InboxClicked += OpenInbox;
            GameEvents.ReplyReceived += OnReplyChanged;
            GameEvents.ReplyRead += OnReplyChanged;
        }

        private void OnDisable()
        {
            if (_hud != null) _hud.InboxClicked -= OpenInbox;
            GameEvents.ReplyReceived -= OnReplyChanged;
            GameEvents.ReplyRead -= OnReplyChanged;
        }

        // 답장이 새로 오거나 읽음 처리되면, 수신함이 열려 있는 경우에만 목록을 다시 그린다.
        private void OnReplyChanged(int replyId)
        {
            InboxController inbox = GetInbox();
            if (inbox != null && inbox.IsOpen) inbox.Open(BuildModel());
        }

        /// <summary>수신함 열기. HUD 버튼 또는 테스트 버튼에서 호출.</summary>
        public void OpenInbox()
        {
            InboxController inbox = GetInbox();
            if (inbox == null) { Debug.LogWarning("[Inbox] 팝업 미등록 — Id가 Inbox인지 확인"); return; }
            inbox.Open(BuildModel());
        }

        private InboxModel BuildModel()
        {
            List<InboxRow> rows = new();
            if (_replyService != null)
            {
                foreach (ReplyStaticData reply in _replyService.GetReceivedReplies())
                {
                    if (reply == null) continue;
                    rows.Add(new InboxRow
                    {
                        ReplyId = reply.ReplyID,
                        SenderName = reply.SenderName,
                        Title = reply.ReplyTitle,
                        IsRead = _playerData != null && _playerData.IsReplyRead(reply.ReplyID),
                    });
                }
            }
            else
            {
                Debug.LogError("[Inbox] ReplyService 미연결", this);
            }
            return new InboxModel { Rows = rows, OnOpenReply = OpenReply };
        }

        // 답장 열기: 선택 → OpenSelectedReply(읽음 처리 + ReplyRead 발생) → 본문 표시
        private void OpenReply(int replyId)
        {
            if (_flow == null) return;
            if (!_flow.SelectReply(replyId))
            {
                Debug.LogWarning($"[Inbox] 답장 선택 실패 replyId={replyId}");
                ToastController.Instance?.Show("답장을 열 수 없어요.");
                return;
            }

            ReplyStaticData reply = _flow.OpenSelectedReply();
            if (reply == null)
            {
                Debug.LogWarning("[Inbox] 답장 열기 실패");
                ToastController.Instance?.Show("답장을 열 수 없어요.");
                return;
            }

            LetterReadController reader = GetReader();
            if (reader == null) { Debug.LogWarning("[Inbox] LetterRead 팝업 미등록 — Id가 LetterRead인지 확인"); return; }
            reader.Open(new LetterReadModel
            {
                Title = reply.ReplyTitle,
                Sender = reply.SenderName,
                Body = reply.ReplyBody,
            });
        }

        private InboxController GetInbox()
            => PopupManager.Instance != null
                ? PopupManager.Instance.Get<InboxController>(UIScreenId.Inbox) : null;

        private LetterReadController GetReader()
            => PopupManager.Instance != null
                ? PopupManager.Instance.Get<LetterReadController>(UIScreenId.LetterRead) : null;
    }
}
