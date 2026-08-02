public class ReplyCollectionEntryData
{
    // 도감 항목에 해당하는 답장 ID임
    public int ReplyID { get; private set; }

    // 플레이어가 해당 답장을 수신했는지 나타냄
    public bool IsReceived { get; private set; }

    // 플레이어가 해당 답장을 읽었는지 나타냄
    public bool IsRead { get; private set; }

    // 도감에 표시할 답장 정적 데이터임
    // 아직 수신하지 않은 답장이라면 null임
    public ReplyStaticData ReplyData { get; private set; }

    /// <summary>
    /// 답장의 도감 표시 상태와 정적 데이터를 생성함
    /// </summary>
    public ReplyCollectionEntryData( int replyID, bool isReceived, bool isRead, ReplyStaticData replyData)
    {
        // 전달받은 답장 ID를 저장함
        ReplyID = replyID;

        // 답장 수신 여부를 저장함
        IsReceived = isReceived;

        // 답장 읽음 여부를 저장함
        IsRead = isRead;

        // 도감에 표시할 답장 정적 데이터를 저장함
        ReplyData = replyData;
    }
}
