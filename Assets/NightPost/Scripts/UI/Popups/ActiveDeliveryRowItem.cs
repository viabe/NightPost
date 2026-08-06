using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 진행 중인 배달 한 줄. 남은 시간이 매초 줄어드는 표시 전용 항목이다.
    ///
    /// 남은 시간은 저장되지 않고 시작·완료 시각으로 계산한다(세이브 명세 기준).
    /// 그래서 시각 두 개만 받아두고, 부모가 UpdateRemaining(now)를 주기적으로 호출한다.
    /// 완료 판정은 UI 책임이 아니다 — OfflineProgressService가 처리하고 결과는 이벤트로 온다.
    /// </summary>
    public class ActiveDeliveryRowItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;    // 편지 제목
        [SerializeField] private TMP_Text _subText;      // "민아 · 오래된 골목길"
        [SerializeField] private TMP_Text _remainText;   // "3분 20초"
        [SerializeField] private Slider _progressBar;    // 선택

        private int _letterId;
        private long _startUnix;
        private long _completeUnix;

        public int LetterId => _letterId;

        public void Setup(int letterId, string title, string sub, long startUnix, long completeUnix)
        {
            _letterId = letterId;
            _startUnix = startUnix;
            _completeUnix = completeUnix;

            if (_titleText != null) _titleText.text = title;
            if (_subText != null) _subText.text = sub;
        }

        /// <summary>남은 시간과 진행률을 현재 시각 기준으로 갱신한다.</summary>
        public void UpdateRemaining(long nowUnix)
        {
            long remain = _completeUnix - nowUnix;
            if (remain < 0) remain = 0;

            if (_remainText != null) _remainText.text = UILabels.Duration(remain);

            if (_progressBar != null)
            {
                long total = _completeUnix - _startUnix;
                _progressBar.value = total > 0
                    ? Mathf.Clamp01((float)(nowUnix - _startUnix) / total)
                    : 1f;
            }
        }
    }
}
