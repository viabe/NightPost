using UnityEngine;

[System.Serializable]
public class LetterProgressData
{
    // 연결된 편지 정적 데이터 ID
    [SerializeField] private int letterID = 0;
    // 현재 편지 상태
    [SerializeField] private ELetterProgressState state = ELetterProgressState.New;
    // 	플레이어가 편지 본문을 읽었는지
    [SerializeField] private bool isRead = false;

    public int LetterID => letterID;
    public ELetterProgressState State => state;
    public bool IsRead => isRead;

    public LetterProgressData(int letterID)
    {
        this.letterID = letterID;
        this.state = ELetterProgressState.New;
        this.isRead = false;
    }
    public LetterProgressData(int letterID, ELetterProgressState state, bool isRead)
    {
        this.letterID = letterID;
        this.state = state;
        this.isRead = isRead;
    }

    public bool MarkAsRead()
    {
        if (IsRead) return false;
        isRead = true;
        return true;
    }
    public bool CompleteSorting()
    {
        if (!isRead) return false;
        if (state != ELetterProgressState.New) return false;
        state = ELetterProgressState.Waiting;
        return true;
    }
}
