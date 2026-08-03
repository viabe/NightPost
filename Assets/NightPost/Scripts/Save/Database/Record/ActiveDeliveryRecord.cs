using SQLite;

// 현재 진행 중인 배달 정보를 SQLite에 보관하는 테이블 데이터임
[Table("ActiveDeliveries")]
public class ActiveDeliveryRecord
{
    // 배달 중인 편지를 구분하는 기본 키임
    [PrimaryKey]
    public int LetterID { get; set; }

    // 배달에 배정된 배달부 ID임
    public int CourierID { get; set; }

    // 배달에 선택된 노선 ID임
    public int RouteID { get; set; }

    // 배달을 시작한 Unix 시각임
    public long StartedAtUnixTime { get; set; }

    // 배달이 완료될 예정인 Unix 시각임
    public long CompleteAtUnixTime { get; set; }
}
