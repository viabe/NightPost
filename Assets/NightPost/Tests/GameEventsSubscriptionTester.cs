using UnityEngine;

public class GameEventsSubscriptionTester : MonoBehaviour
{
    private int currencyChangedCount;
    private int letterReceivedCount;
    private int letterReadCount;
    private int letterStateChangedCount;
    private int deliveryStartedCount;
    private int deliveryCompletedCount;
    private int deliveryResultCheckedCount;
    private int replyReceivedCount;
    private int replyReadCount;
    private int unreadReplyCountChangedCount;

    private void OnEnable()
    {
        GameEvents.CurrencyChanged += OnCurrencyChanged;

        GameEvents.LetterReceived += OnLetterReceived;
        GameEvents.LetterRead += OnLetterRead;
        GameEvents.LetterStateChanged += OnLetterStateChanged;

        GameEvents.DeliveryStarted += OnDeliveryStarted;
        GameEvents.DeliveryCompleted += OnDeliveryCompleted;
        GameEvents.DeliveryResultChecked += OnDeliveryResultChecked;

        GameEvents.ReplyReceived += OnReplyReceived;
        GameEvents.ReplyRead += OnReplyRead;
        GameEvents.UnreadReplyCountChanged += OnUnreadReplyCountChanged;

        Debug.Log("[GameEventsSubscriptionTester] 이벤트 구독 시작");
    }

    private void OnDisable()
    {
        GameEvents.CurrencyChanged -= OnCurrencyChanged;

        GameEvents.LetterReceived -= OnLetterReceived;
        GameEvents.LetterRead -= OnLetterRead;
        GameEvents.LetterStateChanged -= OnLetterStateChanged;

        GameEvents.DeliveryStarted -= OnDeliveryStarted;
        GameEvents.DeliveryCompleted -= OnDeliveryCompleted;
        GameEvents.DeliveryResultChecked -= OnDeliveryResultChecked;

        GameEvents.ReplyReceived -= OnReplyReceived;
        GameEvents.ReplyRead -= OnReplyRead;
        GameEvents.UnreadReplyCountChanged -= OnUnreadReplyCountChanged;
    }

    private void OnCurrencyChanged(int currentCurrency)
    {
        currencyChangedCount++;

        Debug.Log(
            $"[GameEvent] CurrencyChanged | 현재 재화: {currentCurrency} | 발생 횟수: {currencyChangedCount}");
    }

    private void OnLetterReceived(int letterID)
    {
        letterReceivedCount++;

        Debug.Log(
            $"[GameEvent] LetterReceived | LetterID: {letterID} | 발생 횟수: {letterReceivedCount}");
    }

    private void OnLetterRead(int letterID)
    {
        letterReadCount++;

        Debug.Log(
            $"[GameEvent] LetterRead | LetterID: {letterID} | 발생 횟수: {letterReadCount}");
    }

    private void OnLetterStateChanged(
        int letterID,
        ELetterProgressState state)
    {
        letterStateChangedCount++;

        Debug.Log(
            $"[GameEvent] LetterStateChanged | LetterID: {letterID} | 상태: {state} | 발생 횟수: {letterStateChangedCount}");
    }

    private void OnDeliveryStarted(
        int letterID,
        int courierID,
        int routeID)
    {
        deliveryStartedCount++;

        Debug.Log(
            $"[GameEvent] DeliveryStarted | LetterID: {letterID} | CourierID: {courierID} | RouteID: {routeID} | 발생 횟수: {deliveryStartedCount}");
    }

    private void OnDeliveryCompleted(int letterID)
    {
        deliveryCompletedCount++;

        Debug.Log(
            $"[GameEvent] DeliveryCompleted | LetterID: {letterID} | 발생 횟수: {deliveryCompletedCount}");
    }

    private void OnDeliveryResultChecked(int letterID)
    {
        deliveryResultCheckedCount++;

        Debug.Log(
            $"[GameEvent] DeliveryResultChecked | LetterID: {letterID} | 발생 횟수: {deliveryResultCheckedCount}");
    }

    private void OnReplyReceived(int replyID)
    {
        replyReceivedCount++;

        Debug.Log(
            $"[GameEvent] ReplyReceived | ReplyID: {replyID} | 발생 횟수: {replyReceivedCount}");
    }

    private void OnReplyRead(int replyID)
    {
        replyReadCount++;

        Debug.Log(
            $"[GameEvent] ReplyRead | ReplyID: {replyID} | 발생 횟수: {replyReadCount}");
    }

    private void OnUnreadReplyCountChanged(int unreadCount)
    {
        unreadReplyCountChangedCount++;

        Debug.Log(
            $"[GameEvent] UnreadReplyCountChanged | 미열람 답장 수: {unreadCount} | 발생 횟수: {unreadReplyCountChangedCount}");
    }

    [ContextMenu("이벤트 발생 횟수 출력")]
    private void PrintEventCounts()
    {
        Debug.Log(
            "[GameEventsSubscriptionTester] 이벤트 발생 횟수\n" +
            $"CurrencyChanged: {currencyChangedCount}\n" +
            $"LetterReceived: {letterReceivedCount}\n" +
            $"LetterRead: {letterReadCount}\n" +
            $"LetterStateChanged: {letterStateChangedCount}\n" +
            $"DeliveryStarted: {deliveryStartedCount}\n" +
            $"DeliveryCompleted: {deliveryCompletedCount}\n" +
            $"DeliveryResultChecked: {deliveryResultCheckedCount}\n" +
            $"ReplyReceived: {replyReceivedCount}\n" +
            $"ReplyRead: {replyReadCount}\n" +
            $"UnreadReplyCountChanged: {unreadReplyCountChangedCount}");
    }
}
