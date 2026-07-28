using UnityEngine;

public class ReplyService : MonoBehaviour
{
    private PlayerDataManager playerDataManager;
    private StaticDataCatalog staticDataCatalog;
    public bool Initialize(PlayerDataManager dataManager, StaticDataCatalog catalog)
    {
        if (dataManager == null) return false;
        if (catalog == null) return false;

        playerDataManager = dataManager;
        staticDataCatalog = catalog;
        return true;
    }
    public ReplyStaticData OpenReply(int replyID)
    {
        if (playerDataManager == null || staticDataCatalog == null) return null;
        if(!playerDataManager.IsReplyReceived(replyID)) return null;
        ReplyStaticData reply = staticDataCatalog.GetReply(replyID);
        if (reply == null) return null;
        if(!playerDataManager.IsReplyRead(replyID))
        {
            if(!playerDataManager.MarkReplyAsRead(replyID)) return null;
        }

        return reply;
    }
}
