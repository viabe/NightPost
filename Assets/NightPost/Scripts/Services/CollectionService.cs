using System.Collections.Generic;

public class CollectionService
{
    // 편지·답장 정적 데이터를 조회하는 카탈로그임
    private StaticDataCatalog staticDataCatalog;

    // 플레이어의 편지·답장 수신 및 읽음 상태를 조회하는 데이터 관리자임
    private PlayerDataManager playerDataManager;

    /// <summary>
    /// 도감 서비스에 필요한 의존성을 연결함
    /// </summary>
    public bool Initialize(StaticDataCatalog catalog, PlayerDataManager dataManager)
    {
        // 정적 데이터 카탈로그가 없다면 초기화하지 않음
        if (catalog == null) return false;

        // 플레이어 데이터 관리자가 없다면 초기화하지 않음
        if(dataManager == null) return false;

        // 전달받은 정적 데이터 카탈로그를 저장함
        staticDataCatalog = catalog;

        // 전달받은 플레이어 데이터 관리자를 저장함
        playerDataManager = dataManager;

        // 모든 의존성 연결이 완료되면 초기화 성공을 반환함
        return true;
    }

    /// <summary>
    /// 전체 편지의 수신·읽음·배달 완료 상태를 도감 항목 목록으로 반환함
    /// </summary>
    public IReadOnlyList<LetterCollectionEntryData> GetLetterCollectionEntries()
    {
        // 생성한 편지 도감 항목을 저장할 목록을 생성함
        List<LetterCollectionEntryData> letterCollectionEntryDatas = new();
        // 정적 데이터 카탈로그 또는 플레이어 데이터 관리자가 없다면 빈 목록을 반환함
        if(staticDataCatalog == null || playerDataManager == null) return letterCollectionEntryDatas;

        // 정적 데이터 카탈로그에서 전체 편지 목록을 조회함
        IReadOnlyList<LetterStaticData> letterStaticDatas = staticDataCatalog.Letters();

        // 전체 편지 목록이 없거나 비어 있다면 빈 목록을 반환함
        if (letterStaticDatas == null || letterStaticDatas.Count == 0) return letterCollectionEntryDatas;

        // 전체 편지 정적 데이터를 순회함
        foreach (LetterStaticData letterStaticData in letterStaticDatas)
        {
            // 유효하지 않은 편지 정적 데이터는 건너뜀
            if (letterStaticData == null || letterStaticData.LetterID <= 0) continue;
            // 현재 편지의 플레이어 진행 데이터를 조회함
            LetterProgressData letterProgressData = playerDataManager.GetLetterProgress(letterStaticData.LetterID);

            // 진행 데이터 존재 여부로 편지 수신 여부를 판단함
            bool isReceived = letterProgressData != null;

            // 수신한 편지라면 진행 데이터의 읽음 여부를 저장함
            bool isRead = isReceived && letterProgressData.IsRead;

            // 수신한 편지의 상태가 Completed인지 확인함
            bool isCompleted = isReceived && letterProgressData.State == ELetterProgressState.Completed;

            // 수신하지 않은 편지는 내용을 숨기기 위해 정적 데이터를 null로 설정함
            LetterStaticData visibleLetterData = isReceived ? letterStaticData : null;

            // 계산한 상태로 편지 도감 항목을 생성함
            LetterCollectionEntryData letterCollectionEntryData = new LetterCollectionEntryData(letterStaticData.LetterID, isReceived, isRead, isCompleted, visibleLetterData);

            // 생성한 도감 항목을 반환 목록에 추가함
            letterCollectionEntryDatas.Add(letterCollectionEntryData);
        }

        // 생성한 전체 편지 도감 항목 목록을 반환함
        return letterCollectionEntryDatas;
    }
    /// <summary>
    /// 전체 답장의 수신·읽음 상태를 도감 항목 목록으로 반환함
    /// </summary>
    public IReadOnlyList<ReplyCollectionEntryData> GetReplyCollectionEntries()
    {
        // 생성한 답장 도감 항목을 저장할 목록을 생성함
        List<ReplyCollectionEntryData> replyCollectionEntryDatas = new();

        // 정적 데이터 카탈로그 또는 플레이어 데이터 관리자가 없다면 빈 목록을 반환함
        if (staticDataCatalog == null || playerDataManager == null) return replyCollectionEntryDatas;

        // 정적 데이터 카탈로그에서 전체 답장 목록을 조회함
        IReadOnlyList<ReplyStaticData> replyStaticDatas = staticDataCatalog.Replies();

        // 전체 답장 목록이 없거나 비어 있다면 빈 목록을 반환함
        if(replyStaticDatas == null || replyStaticDatas.Count == 0) return replyCollectionEntryDatas;

        // 전체 답장 정적 데이터를 순회함
        foreach(ReplyStaticData replyStaticData in replyStaticDatas)
        {
            // 유효하지 않은 답장 정적 데이터는 건너뜀
            if(replyStaticData == null || replyStaticData.ReplyID <= 0) continue;

            // 플레이어가 현재 답장을 수신했는지 확인함
            bool isReceived = playerDataManager.IsReplyReceived(replyStaticData.ReplyID);

            // 수신한 답장이라면 현재 읽음 여부를 확인함
            bool isRead = isReceived && playerDataManager.IsReplyRead(replyStaticData.ReplyID);

            // 수신하지 않은 답장은 내용을 숨기기 위해 정적 데이터를 null로 설정함
            ReplyStaticData visibleReplyData = isReceived ? replyStaticData : null;

            // 계산한 상태로 답장 도감 항목을 생성함
            ReplyCollectionEntryData replyCollectionEntryData = new ReplyCollectionEntryData(replyStaticData.ReplyID, isReceived, isRead, visibleReplyData);

            // 생성한 답장 도감 항목을 반환 목록에 추가함
            replyCollectionEntryDatas.Add(replyCollectionEntryData);
        }

        // 생성한 전체 답장 도감 항목 목록을 반환함
        return replyCollectionEntryDatas;
    }
}
