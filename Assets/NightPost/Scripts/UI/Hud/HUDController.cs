using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightPost.UI
{
    /// <summary>
    /// 상단 HUD(상단바). 코인/우표, 레벨·경험치, 오늘의 업무(미션 3종),
    /// 설정·수신함 버튼을 담는다.
    /// 원칙(역할분담표 3.1): HUD는 저장 데이터를 직접 읽지 않는다.
    /// 표시는 public Set* 로 갱신하고, 버튼 입력은 이벤트로 밖에 알린다.
    /// 메인 개발자의 GameEvents가 확정되면 그 구독으로 값 갱신을 연결한다(하단 TODO).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Serializable]
        public class MissionRow
        {
            public TMP_Text Label;      // 예: "편지 분류"
            public TMP_Text Progress;   // 예: "45/60"
            public GameObject Check;    // 완료 체크 아이콘(선택)
        }

        [Header("재화")]
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private TMP_Text _stampText;

        [Header("레벨/경험치")]
        [SerializeField] private Slider _levelBar;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _expText;     // "2450/3600" (선택)

        [Header("편지 보관")]
        [SerializeField] private TMP_Text _letterCapacityText; // "4 / 10"
        [SerializeField] private Slider _letterCapacityBar;    // 선택
        [SerializeField] private GameObject _letterFullMark;   // 한도 도달 표시(선택)

        [Header("오늘의 업무 (고정 3종)")]
        [SerializeField] private MissionRow[] _missions = new MissionRow[3];

        [Header("상단 버튼")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _inboxButton;
        [SerializeField] private GameObject _inboxBadge;  // 미열람 답장 배지

        /// <summary>설정 버튼 클릭. 씬 컨트롤러가 구독해 설정 팝업을 연다.</summary>
        public event Action SettingsClicked;
        /// <summary>수신함 버튼 클릭. 씬 컨트롤러가 구독해 수신함을 연다.</summary>
        public event Action InboxClicked;

        private void Awake()
        {
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(RaiseSettings);
            if (_inboxButton != null)
                _inboxButton.onClick.AddListener(RaiseInbox);
        }

        private void OnDestroy()
        {
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(RaiseSettings);
            if (_inboxButton != null)
                _inboxButton.onClick.RemoveListener(RaiseInbox);
        }

        private void RaiseSettings() => SettingsClicked?.Invoke();
        private void RaiseInbox() => InboxClicked?.Invoke();

        // ── 표시 갱신 ───────────────────────────
        public void SetCoin(int value)
        {
            if (_coinText != null) _coinText.text = value.ToString("N0");
        }

        public void SetStamp(int value)
        {
            if (_stampText != null) _stampText.text = value.ToString("N0");
        }

        /// <summary>레벨 + 진행률(0~1)로 갱신.</summary>
        public void SetLevel(int level, float progress01)
        {
            if (_levelText != null) _levelText.text = $"Lv.{level}";
            if (_levelBar != null) _levelBar.value = Mathf.Clamp01(progress01);
        }

        /// <summary>레벨 + 경험치(현재/최대)로 갱신. 경험치 텍스트도 함께 표시.</summary>
        public void SetLevel(int level, int currentExp, int maxExp)
        {
            if (_expText != null) _expText.text = $"{currentExp}/{maxExp}";
            SetLevel(level, maxExp > 0 ? (float)currentExp / maxExp : 0f);
        }

        /// <summary>
        /// 편지 보관 현황 표시. current는 분류 전(New) + 배달 대기(Waiting) 편지 수다.
        /// 한도에 도달하면 새 편지가 들어오지 않으므로 눈에 띄게 알린다.
        /// </summary>
        public void SetLetterCapacity(int current, int max)
        {
            if (_letterCapacityText != null) _letterCapacityText.text = $"{current} / {max}";
            if (_letterCapacityBar != null) _letterCapacityBar.value = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (_letterFullMark != null) _letterFullMark.SetActive(max > 0 && current >= max);
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

        // TODO(연결): 서비스/이벤트 계약 확정 후 값 갱신을 이벤트 구독으로 연결.
        //   OnEnable:  GameEvents.CoinChanged += SetCoin;  GameEvents.StampChanged += SetStamp; ...
        //   OnDisable: GameEvents.CoinChanged -= SetCoin;  GameEvents.StampChanged -= SetStamp; ...
        //   (짝을 맞춰 구독/해제. HUD는 값만 표시하고 로직은 갖지 않는다.)
    }
}
