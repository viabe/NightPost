using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 상단 HUD. 코인/우표 카운터, 레벨 바, 오늘의 업무(미션 3종)를 표시한다.
    /// 원칙(역할분담표 3.1): HUD는 저장 데이터를 직접 읽지 않는다.
    /// 지금은 public Set* 메서드로 갱신하고,
    /// 메인 개발자의 GameEvents(CoinChanged/StampChanged 등)가 확정되면
    /// 그 이벤트 구독으로 연결한다. (TODO 참고)
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Serializable]
        public class MissionRow
        {
            public TMP_Text Label;      // 예: "편지 분류"
            public TMP_Text Progress;   // 예: "45/60"
            public GameObject Check;     // 완료 체크 아이콘(선택)
        }

        [Header("재화")]
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private TMP_Text _stampText;

        [Header("레벨/경험치")]
        [SerializeField] private Slider _levelBar;
        [SerializeField] private TMP_Text _levelText;

        [Header("오늘의 업무 (고정 3종)")]
        [SerializeField] private MissionRow[] _missions = new MissionRow[3];

        [Header("알림")]
        [SerializeField] private GameObject _inboxBadge; // 미열람 답장 배지

        public void SetCoin(int value)
        {
            if (_coinText != null) _coinText.text = value.ToString("N0");
        }

        public void SetStamp(int value)
        {
            if (_stampText != null) _stampText.text = value.ToString("N0");
        }

        public void SetLevel(int level, float progress01)
        {
            if (_levelText != null) _levelText.text = $"Lv.{level}";
            if (_levelBar != null) _levelBar.value = Mathf.Clamp01(progress01);
        }

        public void SetMission(int index, string label, int current, int goal)
        {
            if (_missions == null || index < 0 || index >= _missions.Length) return;
            var row = _missions[index];
            if (row == null) return;

            if (row.Label != null && label != null) row.Label.text = label;
            if (row.Progress != null) row.Progress.text = $"{current}/{goal}";
            if (row.Check != null) row.Check.SetActive(goal > 0 && current >= goal);
        }

        public void SetInboxBadge(bool hasUnread)
        {
            if (_inboxBadge != null) _inboxBadge.SetActive(hasUnread);
        }

        // TODO(연결): 메인 개발자와 이벤트 시그니처 합의 후 아래 형태로 구독.
        //   GameEvents.CoinChanged  += SetCoin;
        //   GameEvents.StampChanged += SetStamp;
        //   GameEvents.ReplyReceived += _ => SetInboxBadge(true);
        // OnEnable에서 += , OnDisable에서 -= 로 짝을 맞춘다.
    }
}
