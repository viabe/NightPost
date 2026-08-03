using SQLite;

// 플레이어가 보유한 배달부 ID를 SQLite에 보관하는 테이블 데이터임
[Table("OwnedCouriers")]
public class OwnedCourierRecord
{
    // 보유한 배달부를 구분하는 기본 키임
    [PrimaryKey]
    public int CourierID { get; set; }
}
