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
    public static void UpdateWidget(EWidgetDeliveryState state, int letterCount)
    {
        letterCount = Mathf.Max(0, letterCount);

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject currentActivity =
            UnityEngine.Android.AndroidApplication.currentActivity;

        if (currentActivity == null)
        {
            Debug.LogWarning("Android Activity를 찾을 수 없어 위젯을 갱신하지 못함");
            return;
        }

        using (AndroidJavaClass widgetProvider = new AndroidJavaClass("com.nightpost.widget.NightPostWidgetProvider"))
        {
            widgetProvider.CallStatic("updateWidgetData",currentActivity,(int)state,letterCount);
        }
#else
        Debug.Log($"위젯 갱신 테스트 - 상태: {state}, 편지 수: {letterCount}");
#endif
    }
}
