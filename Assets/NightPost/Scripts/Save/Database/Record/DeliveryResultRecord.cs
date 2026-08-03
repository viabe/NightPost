using SQLite;

// 완료된 배달 결과 정보를 SQLite에 보관하는 테이블 데이터임
[Table("DeliveryResults")]
public class DeliveryResultRecord
{
    // 배달이 완료된 편지를 구분하는 기본 키임
    [PrimaryKey]
    public int LetterID { get; set; }

    // 플레이어에게 실제로 지급할 보상량임
    public int RewardAmount { get; set; }

    // 배달이 완료된 Unix 시각임
    public long CompletedAtUnixTime { get; set; }

    // 플레이어가 배달 결과를 확인했는지 저장함
    public bool IsChecked { get; set; }
}
