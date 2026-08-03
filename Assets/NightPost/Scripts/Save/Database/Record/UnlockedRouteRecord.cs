using SQLite;

// 플레이어가 해금한 노선 ID를 SQLite에 보관하는 테이블 데이터임
[Table("UnlockedRoutes")]
public class UnlockedRouteRecord
{
    // 해금한 노선을 구분하는 기본 키임
    [PrimaryKey]
    public int RouteID { get; set; }
}
