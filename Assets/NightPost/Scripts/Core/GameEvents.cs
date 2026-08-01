using System;

// 게임 전반의 데이터 변경을 다른 시스템에 전달하는 정적 이벤트 모음임
public static class GameEvents
{
    // 현재 보유 재화가 변경되었을 때 발생함
    public static event Action<int> CurrencyChanged;

    // 새로운 편지를 수신했을 때 발생함
    public static event Action<int> LetterReceived;
    // 편지를 처음 읽었을 때 발생함
    public static event Action<int> LetterRead;
    // 편지의 진행 상태가 변경되었을 때 발생함
    public static event Action<int, ELetterProgressState> LetterStateChanged;

    // 편지 배달이 시작되었을 때 발생함
    public static event Action<int, int, int> DeliveryStarted;
    // 편지 배달이 완료되었을 때 발생함
    public static event Action<int> DeliveryCompleted;
    // 배달 결과를 확인했을 때 발생함
    public static event Action<int> DeliveryResultChecked;

    // 새로운 답장을 수신했을 때 발생함
    public static event Action<int> ReplyReceived;
    // 답장을 처음 읽었을 때 발생함
    public static event Action<int> ReplyRead;
    // 읽지 않은 답장 개수가 변경되었을 때 발생함
    public static event Action<int> UnreadReplyCountChanged;

    // 새로운 배달부가 해금되었을 때 발생함
    public static event Action<int> CourierUnlocked;
    // 새로운 노선이 해금되었을 때 발생함
    public static event Action<int> RouteUnlocked;

    public static event Action<int, int> FacilityUpgraded;


    /// <summary>
    /// 현재 보유 재화 변경 이벤트를 발생시킴
    /// </summary>
    public static void RaiseCurrencyChanged(int currentCurrency)
    {
        // 현재 재화량이 음수라면 이벤트를 발생시키지 않음
        if (currentCurrency < 0) return;
        // 변경된 현재 재화량을 구독 중인 시스템에 전달함
        CurrencyChanged?.Invoke(currentCurrency);
    }

    /// <summary>
    /// 지정한 편지의 수신 이벤트를 발생시킴
    /// </summary>
    public static void RaiseLetterReceived(int letterID)
    {
        // 유효하지 않은 편지 ID라면 이벤트를 발생시키지 않음
        if (letterID <= 0) return;
        // 수신한 편지 ID를 구독 중인 시스템에 전달함
        LetterReceived?.Invoke(letterID);
    }

    /// <summary>
    /// 지정한 편지의 읽음 이벤트를 발생시킴
    /// </summary>
    public static void RaiseLetterRead(int letterID)
    {
        // 유효하지 않은 편지 ID라면 이벤트를 발생시키지 않음
        if (letterID <= 0) return;
        // 읽은 편지 ID를 구독 중인 시스템에 전달함
        LetterRead?.Invoke(letterID);
    }

    /// <summary>
    /// 지정한 편지의 진행 상태 변경 이벤트를 발생시킴
    /// </summary>
    public static void RaiseLetterStateChanged(int letterID, ELetterProgressState state)
    {
        // 유효하지 않은 편지 ID라면 이벤트를 발생시키지 않음
        if (letterID <= 0) return;

        // 편지 ID와 변경된 진행 상태를 구독 중인 시스템에 전달함
        LetterStateChanged?.Invoke(letterID, state);
    }

    /// <summary>
    /// 지정한 편지의 배달 시작 이벤트를 발생시킴
    /// </summary>
    public static void RaiseDeliveryStarted(int letterID, int courierID, int routeID)
    {
        // 유효하지 않은 편지 ID라면 이벤트를 발생시키지 않음
        if (letterID <= 0) return;
        // 유효하지 않은 배달부 ID라면 이벤트를 발생시키지 않음
        if (courierID <= 0) return;
        // 유효하지 않은 노선 ID라면 이벤트를 발생시키지 않음
        if (routeID <= 0) return;

        // 편지, 배달부, 노선 ID를 구독 중인 시스템에 전달함
        DeliveryStarted?.Invoke(letterID, courierID, routeID);
    }

    /// <summary>
    /// 지정한 편지의 배달 완료 이벤트를 발생시킴
    /// </summary>
    public static void RaiseDeliveryCompleted(int letterID)
    {
        // 유효하지 않은 편지 ID라면 이벤트를 발생시키지 않음
        if (letterID <= 0) return;

        // 배달이 완료된 편지 ID를 구독 중인 시스템에 전달함
        DeliveryCompleted?.Invoke(letterID);
    }

    /// <summary>
    /// 지정한 편지의 배달 결과 확인 이벤트를 발생시킴
    /// </summary>
    public static void RaiseDeliveryResultChecked(int letterID)
    {
        // 유효하지 않은 편지 ID라면 이벤트를 발생시키지 않음
        if (letterID <= 0) return;

        // 결과 확인을 완료한 편지 ID를 구독 중인 시스템에 전달함
        DeliveryResultChecked?.Invoke(letterID);
    }

    /// <summary>
    /// 지정한 답장의 수신 이벤트를 발생시킴
    /// </summary>
    public static void RaiseReplyReceived(int replyID)
    {
        // 유효하지 않은 답장 ID라면 이벤트를 발생시키지 않음
        if (replyID <= 0) return;

        // 수신한 답장 ID를 구독 중인 시스템에 전달함
        ReplyReceived?.Invoke(replyID);
    }

    /// <summary>
    /// 지정한 답장의 읽음 이벤트를 발생시킴
    /// </summary>
    public static void RaiseReplyRead(int replyID)
    {
        // 유효하지 않은 답장 ID라면 이벤트를 발생시키지 않음
        if (replyID <= 0) return;

        // 읽은 답장 ID를 구독 중인 시스템에 전달함
        ReplyRead?.Invoke(replyID);
    }

    /// <summary>
    /// 읽지 않은 답장 개수 변경 이벤트를 발생시킴
    /// </summary>
    public static void RaiseUnreadReplyCountChanged(int unreadCount)
    {
        // 읽지 않은 답장 개수가 음수라면 이벤트를 발생시키지 않음
        if (unreadCount < 0) return;

        // 변경된 읽지 않은 답장 개수를 구독 중인 시스템에 전달함
        UnreadReplyCountChanged?.Invoke(unreadCount);
    }

    /// <summary>
    /// 지정한 배달부의 해금 이벤트를 발생시킴
    /// </summary>
    public static void RaiseCourierUnlocked(int courierID)
    {
        // 유효하지 않은 배달부 ID라면 이벤트를 발생시키지 않음
        if (courierID <= 0) return;
        // 해금된 배달부 ID를 구독 중인 시스템에 전달함
        CourierUnlocked?.Invoke(courierID);
    }

    /// <summary>
    /// 지정한 노선의 해금 이벤트를 발생시킴
    /// </summary>
    public static void RaiseRouteUnlocked(int routeID)
    {
        // 유효하지 않은 노선 ID라면 이벤트를 발생시키지 않음
        if (routeID <= 0) return;
        // 해금된 노선 ID를 구독 중인 시스템에 전달함
        RouteUnlocked?.Invoke(routeID);
    }

    /// <summary>
    /// 시설 업그레이드 완료 사실과 변경된 현재 레벨을 알림
    /// </summary>
    public static void RaiseFacilityUpgraded(int facilityID, int currentLevel)
    {
        // facilityID가 유효하지 않으면 이벤트 발생 중단
        // 변경된 시설 레벨이 1 미만이면 이벤트 발생 중단
        if(facilityID <= 0 || currentLevel <= 0) return;

        // 구독자에게 시설 ID와 변경된 현재 레벨 전달
        FacilityUpgraded?.Invoke(facilityID, currentLevel);
    }
}
