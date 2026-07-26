using UnityEngine;

public class LetterProgressData : ScriptableObject
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
}
