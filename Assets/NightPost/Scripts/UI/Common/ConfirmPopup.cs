using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 공통 확인/알림 다이얼로그. BaseView&lt;TModel&gt; 사용 패턴의 참고 구현이기도 하다.
    /// - 확인만: OnConfirm 콜백 하나
    /// - 확인/취소: OnCancel 지정 시 취소 버튼 노출
    /// 도메인에 의존하지 않으므로 어느 화면에서든 재사용 가능.
    /// 사용 예:
    ///   PopupManager.Instance.Get&lt;ConfirmPopup&gt;(UIScreenId.Confirm)
    ///       .Open(ConfirmModel.Info("도보 배달부를 고용할까요?", Hire));
    /// </summary>
    public class ConfirmPopup : BaseView<ConfirmModel>
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TMP_Text _cancelLabel;

        protected override void Bind(ConfirmModel model)
        {
            if (_title != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(model.Title);
                _title.gameObject.SetActive(hasTitle);
                _title.text = model.Title;
            }
            if (_message != null) _message.text = model.Message;

            if (_confirmLabel != null)
                _confirmLabel.text = string.IsNullOrEmpty(model.ConfirmText) ? "확인" : model.ConfirmText;

            bool hasCancel = model.OnCancel != null;
            if (_cancelButton != null) _cancelButton.gameObject.SetActive(hasCancel);
            if (hasCancel && _cancelLabel != null)
                _cancelLabel.text = string.IsNullOrEmpty(model.CancelText) ? "취소" : model.CancelText;

            // 중복 구독 방지 후 재바인딩
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnConfirmClicked()
        {
            var cb = Model.OnConfirm;
            CloseSelf();
            cb?.Invoke();
        }

        private void OnCancelClicked()
        {
            var cb = Model.OnCancel;
            CloseSelf();
            cb?.Invoke();
        }

        private void CloseSelf()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.Close(this);
            else Close();
        }

        protected override void OnClose()
        {
            if (_confirmButton != null) _confirmButton.onClick.RemoveAllListeners();
            if (_cancelButton != null) _cancelButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>확인/알림 다이얼로그 모델.</summary>
    public struct ConfirmModel
    {
        public string Title;
        public string Message;
        public string ConfirmText;   // 비우면 "확인"
        public string CancelText;    // 비우면 "취소"
        public Action OnConfirm;
        public Action OnCancel;      // null이면 취소 버튼 숨김

        /// <summary>확인 버튼만 있는 단순 알림.</summary>
        public static ConfirmModel Info(string message, Action onConfirm = null)
            => new ConfirmModel { Message = message, OnConfirm = onConfirm };
    }
}
