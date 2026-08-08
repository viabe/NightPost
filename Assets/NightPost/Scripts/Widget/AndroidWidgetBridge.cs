using UnityEngine;

public static class AndroidWidgetBridge
{
    /// <summary>
    /// 현재 게임의 위젯 표시 데이터를 Android 위젯에 전달함
    /// </summary>
    public static void SyncWidgetData(int waitingLetterCount,int arrivedLetterCount,string completionTimesCsv)
    {
        // 전달값이 음수가 되지 않도록 처리함
        waitingLetterCount = Mathf.Max(0, waitingLetterCount);
        arrivedLetterCount = Mathf.Max(0, arrivedLetterCount);
        // 완료 예정 시각 목록이 없다면 빈 문자열로 처리함
        if (completionTimesCsv == null)
        {
            completionTimesCsv = string.Empty;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject currentActivity = UnityEngine.Android.AndroidApplication.currentActivity;
        if (currentActivity == null)
        {
            Debug.LogWarning("[AndroidWidgetBridge] Android Activity를 찾을 수 없어 위젯을 갱신하지 못함");

            return;
        }

        try
        {
            using (AndroidJavaClass widgetProvider = new AndroidJavaClass( "com.nightpost.widget.NightPostWidgetProvider"))
            {
                widgetProvider.CallStatic(
                    "syncWidgetData",
                    currentActivity,
                    waitingLetterCount,
                    arrivedLetterCount,
                    completionTimesCsv
                );
            }
        }
        catch (AndroidJavaException exception)
        {
            Debug.LogError("[AndroidWidgetBridge] Android 위젯 데이터 전달 중 오류 발생\n" +exception);
        }
#else
        Debug.Log( "[AndroidWidgetBridge] 위젯 데이터 갱신\n" + $"Waiting: {waitingLetterCount}\n" +$"Arrived: {arrivedLetterCount}\n" +$"CompletionTimes: {completionTimesCsv}");
#endif
    }

}
