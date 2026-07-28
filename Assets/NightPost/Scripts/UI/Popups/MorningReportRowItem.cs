using TMPro;
using UnityEngine;

namespace NightPost.UI
{
    /// <summary>
    /// 아침 보고의 완료 편지 한 줄(표시 전용). 선택·버튼 없음.
    /// Presenter가 만든 표시 값(제목/보상/답장여부)만 그린다.
    /// </summary>
    public class MorningReportRowItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private GameObject _replyBadge; // 답장 있음 표시(선택)

        public void Setup(string title, int reward, bool hasReply)
        {
            if (_title != null) _title.text = title;
            if (_reward != null) _reward.text = $"+{reward:N0}";
            if (_replyBadge != null) _replyBadge.SetActive(hasReply);
        }
    }
}
