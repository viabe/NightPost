using UnityEngine;
[CreateAssetMenu(fileName = "Reply_", menuName = "NightPost/Static Data/Reply")]
public class ReplyStaticData : ScriptableObject
{
    // 답장 ID
    [SerializeField, Min(1)] private int replyID = 1;
    // 연결 편지 ID
    [SerializeField, Min(1)] private int linkedLetterID = 1;
    // 발신자
    [SerializeField] private string senderName = string.Empty;
    // 내용
    [SerializeField] private string replyBody = string.Empty;
    // 이미지
    [SerializeField] private Sprite replyImage;

    public int ReplyID => replyID;
    public int LinkedLetterID => linkedLetterID;
    public string SenderName => senderName;
    public string ReplyBody => replyBody;
    public Sprite ReplyImage => replyImage;
}
