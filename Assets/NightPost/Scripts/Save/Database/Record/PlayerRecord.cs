using SQLite;

// 플레이어의 기본 저장 상태를 SQLite에 보관하는 테이블 데이터임
[Table("PlayerState")]
public class PlayerRecord
{
    // 단일 저장 데이터를 구분하는 기본 키임
    [PrimaryKey]
    public int SaveID { get; set; }

    // 플레이어가 현재 보유한 재화임
    public int Currency { get; set; }

    // 플레이어의 누적 배달 완료 횟수임
    public int CompletedDeliveryCount { get; set; }

    // 마지막으로 저장에 성공한 Unix 시각임
    public long LastSaveUnixTime { get; set; }
}
