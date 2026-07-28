using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 편지 겉면 팝업. 도착 우편 더미를 클릭하면 열려, 이 편지를 어디로/누구에게
    /// 배정할지 판단할 정보를 보여준다. 본문(개봉)은 여기서 다루지 않는다.
    /// ConfirmPopup과 동일한 BaseView&lt;TModel&gt; 패턴이며 도메인에 의존하지 않는다.
    ///   - 표시 데이터는 EnvelopeModel(plain 값)로 받는다.
    ///   - "배정하기"는 OnAssign 콜백만 호출한다. 실제 배정 팝업 열기는 바깥(씬 컨트롤러)이 담당.
    ///
    /// 데이터 출처(메인 개발자 명세): LetterStaticData(Title/Sender/DestinationRegion/
    /// Urgency/Weight/Reward) + LetterProgressData(IsRead). 지역 enum→표시명 변환은
    /// UI 바깥에서 하고, 이 팝업에는 이미 만들어진 문자열(RegionLabel)만 넘긴다.
    /// </summary>
    public class EnvelopePopupController : BaseView<EnvelopeModel>
    {
        [Header("텍스트")]
        [SerializeField] private TMP_Text _title;      // 편지 제목
        [SerializeField] private TMP_Text _sender;     // 발신자
        [SerializeField] private TMP_Text _region;     // 목적 지역 표시명
        [SerializeField] private TMP_Text _reward;     // 예상 보상(코인)

        [Header("소인/뱃지 (선택)")]
        [SerializeField] private GameObject _urgentMark; // 급함 소인
        [SerializeField] private GameObject _heavyMark;  // 무거운 편지 표시
        [SerializeField] private GameObject _readMark;   // 이미 확인한 편지 표시

        [Header("버튼")]
        [SerializeField] private Button _assignButton;   // 배정하기
        [SerializeField] private Button _closeButton;    // 닫기

        protected override void Bind(EnvelopeModel model)
        {
            if (_title != null) _title.text = model.Title;
            if (_sender != null) _sender.text = model.SenderName;
            if (_region != null) _region.text = model.RegionLabel;
            if (_reward != null) _reward.text = model.Reward.ToString("N0");

            if (_urgentMark != null) _urgentMark.SetActive(model.IsUrgent);
            if (_heavyMark != null) _heavyMark.SetActive(model.IsHeavy);
            if (_readMark != null) _readMark.SetActive(model.IsRead);

            // 중복 구독 방지 후 재바인딩 (ConfirmPopup과 동일 패턴)
            if (_assignButton != null)
            {
                _assignButton.onClick.RemoveAllListeners();
                _assignButton.onClick.AddListener(OnAssignClicked);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(CloseSelf);
            }
        }

        private void OnAssignClicked()
        {
            var cb = Model.OnAssign;
            CloseSelf();      // 겉면을 닫고
            cb?.Invoke();     // 배정 팝업 열기는 콜백에 위임
        }

        private void CloseSelf()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.Close(this);
            else Close();
        }

        protected override void OnClose()
        {
            if (_assignButton != null) _assignButton.onClick.RemoveAllListeners();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 편지 겉면 팝업 모델. 도메인 타입(ERegionType 등)에 의존하지 않도록
    /// 이미 가공된 표시 값만 담는다. 실제 값은 LetterStaticData + LetterProgressData에서
    /// 서비스/프리젠터가 채운다.
    /// </summary>
    public struct EnvelopeModel
    {
        public int LetterId;       // 배정 시 시스템에 넘길 편지 ID (LetterStaticData.LetterID)
        public string Title;       // LetterTitle
        public string SenderName;  // SenderName
        public string RegionLabel; // DestinationRegion → 표시명("마을"/"산간"/"외곽")
        public int Reward;         // LetterReward (예상 보상)
        public bool IsUrgent;      // Urgency == Urgent
        public bool IsHeavy;       // Weight == Heavy
        public bool IsRead;        // LetterProgressData.IsRead
        public Action OnAssign;    // "배정하기" → 씬 컨트롤러가 Assignment 팝업을 연다
    }
}
