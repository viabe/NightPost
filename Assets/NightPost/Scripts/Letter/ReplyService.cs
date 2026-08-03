using System.Collections.Generic;
using UnityEngine;

// 답장 정적 데이터 조회와 답장 읽음 상태 변경을 관리함
public class ReplyService : MonoBehaviour
{
    // 플레이어의 답장 수신 및 읽음 데이터를 관리하는 매니저임
    private PlayerDataManager playerDataManager;
    // 답장 정적 데이터를 조회하는 카탈로그임
    private StaticDataCatalog staticDataCatalog;
    /// <summary>
    /// ReplyService에서 사용할 데이터 매니저와 정적 데이터 카탈로그를 등록함
    /// </summary>
    public bool Initialize(PlayerDataManager dataManager, StaticDataCatalog catalog)
    {
        // 전달받은 데이터 매니저가 없다면 초기화하지 않음
        if (dataManager == null) return false;
        // 전달받은 정적 데이터 카탈로그가 없다면 초기화하지 않음
        if (catalog == null) return false;

        // 플레이어 데이터 매니저를 저장함
        playerDataManager = dataManager;
        // 정적 데이터 카탈로그를 저장함
        staticDataCatalog = catalog;
        // 필요한 참조 등록이 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 지정한 답장을 열고 읽음 상태로 변경한 뒤 정적 데이터를 반환함
    /// </summary>
    public ReplyStaticData OpenReply(int replyID)
    {
        // 서비스 초기화가 완료되지 않았다면 답장을 열지 않음
        if (replyID <= 0) return null;
        if (playerDataManager == null || staticDataCatalog == null) return null;
        // 플레이어가 수신하지 않은 답장이라면 열지 않음
        if (!playerDataManager.IsReplyReceived(replyID)) return null;
        // 지정한 ID에 해당하는 답장 정적 데이터를 조회함
        ReplyStaticData reply = staticDataCatalog.GetReply(replyID);
        // 답장 정적 데이터가 없다면 열지 않음
        if (reply == null) return null;
        // 아직 읽지 않은 답장인 경우에만 읽음 상태 변경을 처리함
        if (!playerDataManager.IsReplyRead(replyID))
        {
            // 답장 읽음 상태 변경에 실패했다면 답장 데이터를 반환하지 않음
            if (!playerDataManager.MarkReplyAsRead(replyID)) return null;
        }

        // 화면에 표시할 답장 정적 데이터를 반환함
        return reply;
    }

    /// <summary>
    /// 플레이어가 현재까지 수신한 전체 답장 정적 데이터 목록을 반환함
    /// </summary>
    public IReadOnlyList<ReplyStaticData> GetReceivedReplies()
    {
        // 화면에 제공할 수신 답장 목록을 생성함
        List<ReplyStaticData> replyStatics = new();

        // 플레이어 데이터 관리자 또는 정적 데이터 카탈로그가 없다면 빈 목록을 반환함
        if(playerDataManager == null || staticDataCatalog == null) return replyStatics;

        // 정적 데이터 카탈로그에서 전체 답장 목록을 조회함
        IReadOnlyList<ReplyStaticData> replies = staticDataCatalog.Replies();

        // 전체 답장 목록이 없다면 빈 목록을 반환함
        if(replies == null) return replyStatics;

        // 전체 답장 정적 데이터를 순회함
        foreach (ReplyStaticData reply in replies)
        {
            // 유효하지 않은 답장 데이터는 제외함
            if (reply == null || reply.ReplyID <= 0) continue;

            // 플레이어가 아직 수신하지 않은 답장은 제외함
            if (!playerDataManager.IsReplyReceived(reply.ReplyID)) continue;

            // 수신한 답장 목록에 추가함
            replyStatics.Add(reply);
        }

        // 수신한 답장 정적 데이터 목록을 반환함
        return replyStatics;
    }

    /// <summary>
    /// 플레이어가 수신했지만 아직 읽지 않은 답장 정적 데이터 목록을 반환함
    /// </summary>
    public IReadOnlyList<ReplyStaticData> GetUnreadReplies()
    {
        // 화면에 제공할 읽지 않은 답장 목록을 생성함
        List<ReplyStaticData> replyStatics = new();

        // 플레이어 데이터 관리자 또는 정적 데이터 카탈로그가 없다면 빈 목록을 반환함
        if (playerDataManager == null || staticDataCatalog == null) return replyStatics;

        // 플레이어가 수신한 전체 답장 목록을 조회함
        IReadOnlyList<ReplyStaticData> receivedReplies = GetReceivedReplies();

        // 수신한 답장 목록이 없다면 빈 목록을 반환함
        if(receivedReplies == null) return replyStatics;

        // 수신한 전체 답장을 순회함
        foreach (ReplyStaticData reply in receivedReplies)
        {
            // 유효하지 않은 답장 데이터는 제외함
            if(reply == null || reply.ReplyID <= 0) continue;

            // 이미 읽은 답장은 제외함
            if (playerDataManager.IsReplyRead(reply.ReplyID)) continue;

            // 읽지 않은 답장 목록에 추가함
            replyStatics.Add(reply);
        }

        // 읽지 않은 답장 정적 데이터 목록을 반환함
        return replyStatics;
    }
}
