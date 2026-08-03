using SQLite;

// 시설의 현재 레벨을 SQLite에 보관하는 테이블 데이터임
[Table("FacilityProgresses")]
public class FacilityProgressRecord
{
    // 진행 상태를 저장할 시설을 구분하는 기본 키임
    [PrimaryKey]
    public int FacilityID { get; set; }

    // 시설의 현재 레벨임
    public int CurrentLevel { get; set; }
}
