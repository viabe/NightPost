using SQLite;

// 플레이어가 읽은 답장 ID를 SQLite에 보관하는 테이블 데이터임
[Table("ReadReplies")]
public class ReadReplyRecord
{
    // 읽은 답장을 구분하는 기본 키임
    [PrimaryKey]
    public int ReplyID { get; set; }
}
