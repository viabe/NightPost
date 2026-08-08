using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class WidgetService : MonoBehaviour
{
    private PlayerDataManager playerDataManager;

    private bool isInitialized;
    private bool isEventSubscribed;

    /// <summary>
    /// 위젯 갱신에 필요한 플레이어 데이터 관리자를 연결함
    /// </summary>
    public bool Initialize(PlayerDataManager dataManager)
    {
        if (dataManager == null) return false;

        playerDataManager = dataManager;
        isInitialized = true;

        SubscribeEvents();
        return true;
    }

    /// <summary>
    /// 오브젝트가 다시 활성화되면 게임 상태 이벤트를 구독함
    /// </summary>
    private void OnEnable()
    {
        if (!isInitialized) return;

        SubscribeEvents();
    }

    /// <summary>
    /// 오브젝트가 비활성화되면 게임 상태 이벤트 구독을 해제함
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    /// <summary>
    /// 앱이 백그라운드로 이동할 때 최신 상태를 위젯에 반영함
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) return;

        RefreshWidget();
    }

    /// <summary>
    /// 앱이 다시 활성화되면 현재 게임 상태를 위젯에 반영함
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        RefreshWidget();
    }

    /// <summary>
    /// 현재 게임 데이터를 기준으로 위젯 상태와 편지 수를 갱신함
    /// </summary>
    public void RefreshWidget()
    {
        if (!isInitialized || playerDataManager == null) return;

        // 배달을 기다리는 편지 수를 계산함
        int waitingLetterCount = CountWaitingLetters();

        // 아직 확인하지 않은 도착 편지 수를 계산함
        int arrivedLetterCount = CountUncheckedDeliveryResults();

        // 진행 중 배달들의 완료 예정 시각을 생성함
        string completionTimesCsv = BuildCompletionTimesCsv();


        // Android가 현재 상태와 배달 완료 시점을 계산할 수 있도록 데이터를 전달함
        AndroidWidgetBridge.SyncWidgetData(waitingLetterCount, arrivedLetterCount,completionTimesCsv);
    }


    /// <summary>
    /// 확인하지 않은 배달 결과 수를 반환함
    /// </summary>
    private int CountUncheckedDeliveryResults()
    {
        IReadOnlyList<DeliveryResultData> deliveryResults = playerDataManager.GetUncheckedDeliveryResults();

        if (deliveryResults == null) return 0;

        int resultCount = 0;

        foreach (DeliveryResultData deliveryResult in deliveryResults)
        {
            if (deliveryResult == null) continue;

            resultCount++;
        }

        return resultCount;
    }

    /// <summary>
    /// 분류가 끝나 배달을 기다리는 편지 수를 반환함
    /// </summary>
    private int CountWaitingLetters()
    {
        IReadOnlyList<LetterProgressData> letterProgresses = playerDataManager.GetLetterProgresses();

        if (letterProgresses == null) return 0;

        int waitingLetterCount = 0;

        foreach (LetterProgressData letterProgress in letterProgresses)
        {
            if (letterProgress == null) continue;

            if (letterProgress.State != ELetterProgressState.Waiting)
            {
                continue;
            }

            waitingLetterCount++;
        }

        return waitingLetterCount;
    }
    /// <summary>
    /// 진행 중 배달의 완료 예정 Unix 시각을 문자열로 생성함
    /// </summary>
    private string BuildCompletionTimesCsv()
    {
        IReadOnlyList<ActiveDeliveryData> activeDeliveries = playerDataManager.GetActiveDeliveries();
        if (activeDeliveries == null || activeDeliveries.Count <= 0) return string.Empty;
        StringBuilder builder = new StringBuilder();

        foreach(ActiveDeliveryData activeDelivery in activeDeliveries)
        {
            if (activeDelivery == null) continue;
            if (activeDelivery.CompleteAtUnixTime <= 0) continue;
            if(builder.Length > 0) builder.Append(",");
            builder.Append(activeDelivery.CompleteAtUnixTime);
        }
        return builder.ToString();
    }
    /// <summary>
    /// 위젯 상태에 영향을 주는 게임 이벤트를 구독함
    /// </summary>
    private void SubscribeEvents()
    {
        if (isEventSubscribed) return;

        GameEvents.LetterStateChanged += OnLetterStateChanged;

        GameEvents.DeliveryResultChecked += OnDeliveryResultChecked;

        isEventSubscribed = true;
    }

    /// <summary>
    /// 구독 중인 게임 이벤트를 해제함
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (!isEventSubscribed) return;

        GameEvents.LetterStateChanged -= OnLetterStateChanged;

        GameEvents.DeliveryResultChecked -= OnDeliveryResultChecked;

        isEventSubscribed = false;
    }

    /// <summary>
    /// 편지 진행 상태가 변경되면 위젯을 갱신함
    /// </summary>
    private void OnLetterStateChanged(int letterID, ELetterProgressState state)
    {
        RefreshWidget();
    }

    /// <summary>
    /// 도착한 배달 결과를 확인하면 위젯을 갱신함
    /// </summary>
    private void OnDeliveryResultChecked(int letterID)
    {
        RefreshWidget();
    }
}
