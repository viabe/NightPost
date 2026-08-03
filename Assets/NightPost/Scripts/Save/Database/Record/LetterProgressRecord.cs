using SQLite;

// 편지의 진행 상태와 읽음 여부를 SQLite에 보관하는 테이블 데이터임
[Table("LetterProgresses")]
public class LetterProgressRecord
{
    // 진행 상태를 저장할 편지를 구분하는 기본 키임
    [PrimaryKey]
    public int LetterID { get; set; }

    // ELetterProgressState 값을 정수로 변환하여 저장함
    public int State { get; set; }

    // 플레이어가 편지 본문을 읽었는지 저장함
    public bool IsRead { get; set; }
}
