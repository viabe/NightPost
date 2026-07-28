using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    private LetterService letterService;
    private DeliveryService deliveryService;
    private ReplyService replyService;
    private PlayerDataManager playerDataManager;

    private int selectedLetterID;
    private int selectedResultLetterID;
    private int selectedReplyID;

    public bool Initialize(LetterService letter, DeliveryService delivery,ReplyService reply,PlayerDataManager dataManager)
    {
        if (letter == null || delivery == null || reply == null || dataManager == null) return false;

        letterService = letter;
        deliveryService = delivery;
        replyService = reply;
        playerDataManager = dataManager;

        return true;
    }

    public bool SelectLetter(int letterID)
    {
        if(letterService == null) return false;
        if(letterID <= 0) return false;

        LetterStaticData letterStaticData = letterService.OpenLetter(letterID);
        if(letterStaticData == null) return false;

        selectedLetterID = letterStaticData.LetterID;

        return true;
    }

    public bool CompleteSelectedLetterSorting()
    {
        if (letterService == null) return false;
        if(selectedLetterID <= 0) return false;
        return letterService.CompleteSorting(selectedLetterID);
    }

    public bool StartSelectedLetterDelivery(int courierID, int routeID)
    {
        if (deliveryService == null) return false;
        if (selectedLetterID <= 0) return false;
        if(courierID <= 0 || routeID <= 0) return false;
        bool startResult = deliveryService.StartDelivery(courierID, selectedLetterID, routeID);
        if (!startResult) return false;

        selectedLetterID = 0;

        return true;
    }
    public bool SelectDeliveryResult(int letterID)
    {
        if (playerDataManager == null) return false;
        if (letterID <= 0) return false;
        DeliveryResultData deliveryResultData = playerDataManager.GetDeliveryResult(letterID);
        if(deliveryResultData == null) return false;
        if (deliveryResultData.IsChecked) return false;

        selectedResultLetterID = deliveryResultData.LetterID;
        return true;
    }

    public bool CheckSelectedDeliveryResult()
    {
        if (deliveryService == null) return false;
        if (selectedResultLetterID <= 0) return false;
        bool checkResult = deliveryService.CheckDeliveryResult(selectedResultLetterID);
        if (!checkResult) return false;
        selectedResultLetterID = 0;
        return true;
    }
    public bool SelectReply(int replyID)
    {
        if(playerDataManager == null) return false;
        if(replyID <= 0) return false;

        bool isReceived = playerDataManager.IsReplyReceived(replyID);
        if (!isReceived) return false;
        selectedReplyID = replyID;
        return true;
    }
    public ReplyStaticData OpenSelectedReply()
    {
        if (replyService == null) return null;
        ReplyStaticData reply = replyService.OpenReply(selectedReplyID);
        if(reply == null) return null;
        selectedReplyID = 0;

        return reply;
    }
}
