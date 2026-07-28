using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 아침 보고(결과 팝업). 재접속 후 오프라인 정산으로 완료된 배달 결과를 요약해 보여준다.
    /// "확인"을 누르면 보상 지급·답장 등록이 처리된다(실제 처리는 Presenter가 GameFlowController로).
    /// 도메인 비의존: 완료 결과는 표시용 ReportRow 목록으로만 받는다.
    /// </summary>
    public class MorningReportController : BaseView<MorningReportModel>
    {
        [Header("헤더")]
        [SerializeField] private TMP_Text _headerText;

        [Header("결과 목록")]
        [SerializeField] private Transform _rowRoot;          // Layout Group 권장
        [SerializeField] private MorningReportRowItem _rowPrefab;
        [SerializeField] private GameObject _emptyLabel;      // 완료 건 없을 때 표시

        [SerializeField] private Button _confirmButton;

        private readonly List<MorningReportRowItem> _rows = new();

        protected override void Bind(MorningReportModel model)
        {
            ClearRows();

            int count = model.Rows != null ? model.Rows.Count : 0;
            if (_headerText != null) _headerText.text = $"밤사이 편지 {count}건이 도착했습니다.";
            if (_emptyLabel != null) _emptyLabel.SetActive(count == 0);

            if (model.Rows != null && _rowPrefab != null && _rowRoot != null)
            {
                foreach (ReportRow r in model.Rows)
                {
                    MorningReportRowItem item = Instantiate(_rowPrefab, _rowRoot);
                    item.gameObject.SetActive(true); // 템플릿이 비활성이어도 복제본은 켠다
                    item.Setup(r.Title, r.Reward, r.HasReply);
                    _rows.Add(item);
                }
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        private void OnConfirmClicked()
        {
            Action cb = Model.OnConfirm;
            CloseSelf();       // 먼저 닫고
            cb?.Invoke();      // 확인 처리는 콜백에 위임
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
            if (_confirmButton != null) _confirmButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>아침 보고 모델. 완료 결과 요약 행 + 확인 콜백.</summary>
    public struct MorningReportModel
    {
        public List<ReportRow> Rows;
        public Action OnConfirm;   // "확인" → 미확인 결과 전체 확정(보상·답장) 후 닫기
    }

    /// <summary>완료된 배달 한 건의 표시 값.</summary>
    public struct ReportRow
    {
        public string Title;   // 편지 제목
        public int Reward;     // 지급 보상
        public bool HasReply;  // 답장 존재 여부
    }
}
