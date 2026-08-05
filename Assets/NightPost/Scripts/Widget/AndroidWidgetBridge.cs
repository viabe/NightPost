using UnityEngine;

public enum EWidgetDeliveryState
{
    Waiting = 0,
    Delivering = 1,
    Arrived = 2
}

public static class AndroidWidgetBridge
{
    /// <summary>
    /// Android 홈 화면 위젯에 현재 배달 상태와 편지 수를 전달함
    /// </summary>
    public static void UpdateWidget(
        EWidgetDeliveryState state,
        int letterCount)
    {
        // 편지 수가 음수가 되지 않도록 처리함
        letterCount = Mathf.Max(0, letterCount);

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject currentActivity = UnityEngine.Android.AndroidApplication.currentActivity;

        if (currentActivity == null)
        {
            Debug.LogWarning("[AndroidWidgetBridge] Android Activity를 찾을 수 없어 위젯을 갱신하지 못함");

            return;
        }

        try
        {
            using (AndroidJavaClass widgetProvider = new AndroidJavaClass("com.nightpost.widget.NightPostWidgetProvider"))
            {
                widgetProvider.CallStatic("updateWidgetData",currentActivity,(int)state,letterCount);
            }
        }
        catch (AndroidJavaException exception)
        {
            Debug.LogError("[AndroidWidgetBridge] Android 위젯 갱신 중 오류 발생\n" +exception);
        }
#else
        Debug.Log(
            $"[AndroidWidgetBridge] 위젯 갱신 - " +
            $"상태: {state}, 편지 수: {letterCount}");
#endif
    }
}
