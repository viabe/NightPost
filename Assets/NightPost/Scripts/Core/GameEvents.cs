using System;

public static class GameEvents
{
    // 재화
    public static event Action<int> CurrencyChanged;

    // 편지
    public static event Action<int> LetterReceived;
    public static event Action<int> LetterRead;
    public static event Action<int, ELetterProgressState> LetterStateChanged;

    // 배달
    public static event Action<int, int, int> DeliveryStarted;
    public static event Action<int> DeliveryCompleted;
    public static event Action<int> DeliveryResultChecked;

    // 답장
    public static event Action<int> ReplyReceived;
    public static event Action<int> ReplyRead;
    public static event Action<int> UnreadReplyCountChanged;

    public static void RaiseCurrencyChanged(int currentCurrency)
    {
        if(currentCurrency < 0) return;
        CurrencyChanged?.Invoke(currentCurrency);
    }
    public static void RaiseLetterReceived(int letterID)
    {
        if(letterID <= 0) return;
        LetterReceived?.Invoke(letterID);
    }
    public static void RaiseLetterRead(int letterID)
    {
        if (letterID <= 0) return;
        LetterRead?.Invoke(letterID);
    }
    public static void RaiseLetterStateChanged(int letterID, ELetterProgressState state)
    {
        if (letterID <= 0) return;

        LetterStateChanged?.Invoke(letterID, state);
    }
    public static void RaiseDeliveryStarted(int letterID, int courierID, int routeID)
    {
        if (letterID <= 0) return;
        if (courierID <= 0) return;
        if (routeID <= 0) return;

        DeliveryStarted?.Invoke(letterID, courierID, routeID);
    }

    public static void RaiseDeliveryCompleted(int letterID)
    {
        if (letterID <= 0) return;

        DeliveryCompleted?.Invoke(letterID);
    }

    public static void RaiseDeliveryResultChecked(int letterID)
    {
        if (letterID <= 0) return;

        DeliveryResultChecked?.Invoke(letterID);
    }

    public static void RaiseReplyReceived(int replyID)
    {
        if (replyID <= 0) return;

        ReplyReceived?.Invoke(replyID);
    }

    public static void RaiseReplyRead(int replyID)
    {
        if (replyID <= 0) return;

        ReplyRead?.Invoke(replyID);
    }

    public static void RaiseUnreadReplyCountChanged(int unreadCount)
    {
        if (unreadCount < 0) return;

        UnreadReplyCountChanged?.Invoke(unreadCount);
    }
}
