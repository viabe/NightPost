using System.Collections.Generic;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 아침 보고 Presenter. DeliveryCompleted를 구독해 완료 결과가 생기면 요약 팝업을 연다.
    /// "확인" 시 미확인 결과를 모두 확정(보상 지급 + 답장 등록)한다.
    ///
    /// 이벤트 구독형 Presenter의 예시:
    ///   - OnEnable에서 구독, OnDisable에서 "같은 이름 메서드"로 해제(익명 람다 금지).
    ///   - 확인 처리는 GameFlowController에 위임하고, 그 결과로 시스템이 발생시키는
    ///     CurrencyChanged / ReplyReceived / UnreadReplyCountChanged로 HUD가 자동 갱신된다.
    /// </summary>
    public class MorningReportPresenter : MonoBehaviour
    {
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private PlayerDataManager _playerData;
        [SerializeField] private StaticDataCatalog _catalog;

        private void OnEnable()
        {
            GameEvents.DeliveryCompleted += OnDeliveryCompleted;
        }

        private void OnDisable()
        {
            GameEvents.DeliveryCompleted -= OnDeliveryCompleted;
        }

        // 여러 편지가 연달아 완료돼도 팝업은 한 번만 연다(이미 열려 있으면 무시).
        // 확인 시 미확인 결과를 통째로 처리하므로 개별 이벤트마다 열 필요가 없다.
        private void OnDeliveryCompleted(int letterID)
        {
            MorningReportController popup = GetPopup();
            if (popup == null || popup.IsOpen) return;
            ShowReport();
        }

        /// <summary>미확인 결과를 요약 팝업으로 연다. 버튼으로도 호출 가능(수동 확인).</summary>
        public void ShowReport()
        {
            if (_playerData == null || _catalog == null) return;

            List<ReportRow> rows = new();
            foreach (DeliveryResultData res in _playerData.GetUncheckedDeliveryResults())
            {
                if (res == null) continue;
                LetterStaticData letter = _catalog.GetLetter(res.LetterID);
                rows.Add(new ReportRow
                {
                    Title = letter != null ? letter.LetterTitle : $"편지 {res.LetterID}",
                    Reward = res.RewardAmount,
                    HasReply = _catalog.GetReplyByLetterID(res.LetterID) != null,
                });
            }

            MorningReportController popup = GetPopup();
            if (popup == null) { Debug.LogWarning("[MorningReport] 팝업 미등록 — Id가 MorningReport인지 확인"); return; }
            popup.Open(new MorningReportModel { Rows = rows, OnConfirm = ConfirmAll });
        }

        // 미확인 결과를 모두 확인 처리한다.
        private void ConfirmAll()
        {
            if (_flow == null || _playerData == null) return;

            // 확인하면 목록에서 빠지므로 letterID 스냅샷을 먼저 뜬다.
            List<int> letterIds = new();
            foreach (DeliveryResultData res in _playerData.GetUncheckedDeliveryResults())
                if (res != null) letterIds.Add(res.LetterID);

            foreach (int letterId in letterIds)
            {
                if (!_flow.SelectDeliveryResult(letterId)) continue;
                if (!_flow.CheckSelectedDeliveryResult())
                    Debug.LogWarning($"[MorningReport] 결과 확인 실패 letterID={letterId} (답장 없는 편지일 수 있음)");
            }
        }

        private MorningReportController GetPopup()
            => PopupManager.Instance != null
                ? PopupManager.Instance.Get<MorningReportController>(UIScreenId.MorningReport) : null;
    }
}
