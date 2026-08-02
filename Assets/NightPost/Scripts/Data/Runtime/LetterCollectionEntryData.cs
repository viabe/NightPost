public class LetterCollectionEntryData
{
    // 도감 항목에 해당하는 편지 ID임
    public int LetterID { get; private set; }

    // 플레이어가 해당 편지를 수신했는지 나타냄
    public bool IsReceived { get; private set; }

    // 플레이어가 해당 편지를 읽었는지 나타냄
    public bool IsRead { get; private set; }

    // 해당 편지의 배달을 완료했는지 나타냄
    public bool IsCompleted { get; private set; }

    // 도감에 표시할 편지 정적 데이터임
    // 아직 수신하지 않은 편지라면 null임
    public LetterStaticData LetterData { get; private set; }

    /// <summary>
    /// 편지의 도감 표시 상태와 정적 데이터를 생성함
    /// </summary>
    public LetterCollectionEntryData(int letterID, bool isReceived, bool isRead, bool isCompleted, LetterStaticData letterData)
    {
        // 전달받은 편지 ID를 저장함
        LetterID = letterID;

        // 편지 수신 여부를 저장함
        IsReceived = isReceived;

        // 편지 읽음 여부를 저장함
        IsRead = isRead;

        // 편지 배달 완료 여부를 저장함
        IsCompleted = isCompleted;

        // 도감에 표시할 편지 정적 데이터를 저장함
        LetterData = letterData;
    }
}
