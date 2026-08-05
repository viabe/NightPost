package com.nightpost.widget;

import android.app.AlarmManager;
import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;
import android.widget.RemoteViews;

import java.util.Calendar;

public class NightPostWidgetProvider extends AppWidgetProvider
{
    // 위젯 표시 데이터를 저장하는 SharedPreferences 이름임
    private static final String PREFERENCES_NAME =
            "NightPostWidgetPreferences";

    // 현재 배달 상태를 저장하는 키임
    private static final String KEY_WIDGET_STATE =
            "WidgetState";

    // 현재 편지 수를 저장하는 키임
    private static final String KEY_LETTER_COUNT =
            "LetterCount";

    // 자동 또는 테스트 시간대를 저장하는 키임
    private static final String KEY_TIME_STATE =
            "TimeState";

    // 시간대 배경 갱신에 사용하는 Broadcast Action임
    private static final String ACTION_REFRESH_TIME_BACKGROUND =
            "com.nightpost.widget.ACTION_REFRESH_TIME_BACKGROUND";

    // 시간대 갱신 PendingIntent 식별값임
    private static final int TIME_REFRESH_REQUEST_CODE = 1001;

    // 배경을 현재 시각에 따라 자동으로 선택함
    private static final int TIME_AUTO = -1;

    // 아침 배경 상태임
    public static final int TIME_MORNING = 0;

    // 낮 배경 상태임
    public static final int TIME_DAY = 1;

    // 밤 배경 상태임
    public static final int TIME_NIGHT = 2;

    // 배달 대기 상태임
    public static final int STATE_WAITING = 0;

    // 배달 진행 상태임
    public static final int STATE_DELIVERING = 1;

    // 편지 도착 상태임
    public static final int STATE_ARRIVED = 2;

    /**
     * 첫 번째 위젯이 홈 화면에 추가되면 화면을 갱신함
     */
    @Override
    public void onEnabled(Context context)
    {
        super.onEnabled(context);

        refreshAllWidgets(context);

        if (isAutomaticTimeMode(context))
        {
            scheduleNextTimeBackgroundRefresh(context);
        }
    }

    /**
     * Android가 위젯 갱신을 요청하면 설치된 위젯 화면을 갱신함
     */
    @Override
    public void onUpdate(
            Context context,
            AppWidgetManager appWidgetManager,
            int[] appWidgetIds)
    {
        for (int appWidgetId : appWidgetIds)
        {
            updateAppWidget(
                    context,
                    appWidgetManager,
                    appWidgetId
            );
        }

        if (isAutomaticTimeMode(context))
        {
            scheduleNextTimeBackgroundRefresh(context);
        }
    }

    /**
     * 시간대 변경 예약 Broadcast를 받으면 자동 배경을 갱신함
     */
    @Override
    public void onReceive(
            Context context,
            Intent intent)
    {
        super.onReceive(context, intent);

        if (intent == null)
        {
            return;
        }

        String action = intent.getAction();

        if (!ACTION_REFRESH_TIME_BACKGROUND.equals(action))
        {
            return;
        }

        // 수동 테스트 배경 중에는 자동 시간 갱신을 적용하지 않음
        if (!isAutomaticTimeMode(context))
        {
            return;
        }

        refreshAllWidgets(context);
        scheduleNextTimeBackgroundRefresh(context);
    }

    /**
     * 마지막 위젯이 홈 화면에서 제거되면 시간대 갱신 예약을 해제함
     */
    @Override
    public void onDisabled(Context context)
    {
        super.onDisabled(context);

        cancelTimeBackgroundRefresh(context);
    }

    /**
     * Unity에서 전달받은 상태와 편지 수를 저장하고 현재 시간 배경을 적용함
     */
    public static void updateWidgetData(
            Context context,
            int widgetState,
            int letterCount)
    {
        saveWidgetData(
                context,
                widgetState,
                letterCount,
                TIME_AUTO
        );
    }

    /**
     * Unity 테스트에서 전달한 아침·낮·밤 배경을 강제로 적용함
     */
    public static void updateWidgetTestData(
            Context context,
            int widgetState,
            int letterCount,
            int timeState)
    {
        if (timeState < TIME_MORNING ||
                timeState > TIME_NIGHT)
        {
            timeState = TIME_AUTO;
        }

        saveWidgetData(
                context,
                widgetState,
                letterCount,
                timeState
        );
    }

    /**
     * 위젯 표시 데이터를 저장하고 모든 위젯을 갱신함
     */
    private static void saveWidgetData(
            Context context,
            int widgetState,
            int letterCount,
            int timeState)
    {
        if (context == null)
        {
            return;
        }

        Context applicationContext =
                getApplicationContext(context);

        // 잘못된 상태값은 배달 대기로 변경함
        if (widgetState < STATE_WAITING ||
                widgetState > STATE_ARRIVED)
        {
            widgetState = STATE_WAITING;
        }

        // 편지 수가 음수가 되지 않도록 처리함
        letterCount = Math.max(0, letterCount);

        SharedPreferences preferences =
                applicationContext.getSharedPreferences(
                        PREFERENCES_NAME,
                        Context.MODE_PRIVATE
                );

        preferences.edit()
                .putInt(KEY_WIDGET_STATE, widgetState)
                .putInt(KEY_LETTER_COUNT, letterCount)
                .putInt(KEY_TIME_STATE, timeState)
                .apply();

        refreshAllWidgets(applicationContext);

        if (timeState == TIME_AUTO)
        {
            scheduleNextTimeBackgroundRefresh(
                    applicationContext
            );
        }
        else
        {
            cancelTimeBackgroundRefresh(
                    applicationContext
            );
        }
    }

    /**
     * 현재 홈 화면에 설치된 모든 밤에 오는 편지 위젯을 갱신함
     */
    public static void refreshAllWidgets(Context context)
    {
        if (context == null)
        {
            return;
        }

        Context applicationContext =
                getApplicationContext(context);

        AppWidgetManager appWidgetManager =
                AppWidgetManager.getInstance(
                        applicationContext
                );

        ComponentName widgetComponent =
                new ComponentName(
                        applicationContext,
                        NightPostWidgetProvider.class
                );

        int[] appWidgetIds =
                appWidgetManager.getAppWidgetIds(
                        widgetComponent
                );

        for (int appWidgetId : appWidgetIds)
        {
            updateAppWidget(
                    applicationContext,
                    appWidgetManager,
                    appWidgetId
            );
        }
    }

    /**
     * 저장된 상태와 시간대를 읽고 지정된 위젯 화면에 적용함
     */
    private static void updateAppWidget(
            Context context,
            AppWidgetManager appWidgetManager,
            int appWidgetId)
    {
        SharedPreferences preferences =
                context.getSharedPreferences(
                        PREFERENCES_NAME,
                        Context.MODE_PRIVATE
                );

        int widgetState =
                preferences.getInt(
                        KEY_WIDGET_STATE,
                        STATE_WAITING
                );

        int letterCount =
                Math.max(
                        0,
                        preferences.getInt(
                                KEY_LETTER_COUNT,
                                0
                        )
                );

        int timeState =
                preferences.getInt(
                        KEY_TIME_STATE,
                        TIME_AUTO
                );

        String statusText;
        String countText;
        int stateIconResource;

        switch (widgetState)
        {
            case STATE_DELIVERING:
                statusText = "배달 중";
                countText =
                        "편지 " + letterCount + "통 이동 중";

                stateIconResource =
                        R.drawable.widget_icon_delivering;
                break;

            case STATE_ARRIVED:
                statusText = "편지가 도착했어요";
                countText =
                        "도착한 편지 " + letterCount + "통";

                stateIconResource =
                        R.drawable.widget_icon_arrived;
                break;

            case STATE_WAITING:
            default:
                statusText = "배달 대기";
                countText =
                        "준비된 편지 " + letterCount + "통";

                stateIconResource =
                        R.drawable.widget_icon_waiting;
                break;
        }

        int backgroundResource =
                getBackgroundResource(timeState);

        RemoteViews remoteViews =
                new RemoteViews(
                        context.getPackageName(),
                        R.layout.night_post_widget
                );

        // 현재 시간대에 맞는 배경 이미지를 적용함
        remoteViews.setImageViewResource(
                R.id.widget_background_image,
                backgroundResource
        );

        // 현재 배달 상태에 맞는 아이콘을 적용함
        remoteViews.setImageViewResource(
                R.id.widget_state_icon,
                stateIconResource
        );

        // 현재 배달 상태 문구를 적용함
        remoteViews.setTextViewText(
                R.id.widget_status_text,
                statusText
        );

        // 현재 편지 수 문구를 적용함
        remoteViews.setTextViewText(
                R.id.widget_count_text,
                countText
        );

        connectGameLaunchIntent(
                context,
                remoteViews,
                appWidgetId
        );

        appWidgetManager.updateAppWidget(
                appWidgetId,
                remoteViews
        );
    }

    /**
     * 저장된 시간대 설정에 맞는 배경 이미지 리소스를 반환함
     */
    private static int getBackgroundResource(
            int timeState)
    {
        switch (timeState)
        {
            case TIME_MORNING:
                return R.drawable.widget_background_morning;

            case TIME_DAY:
                return R.drawable.widget_background_day;

            case TIME_NIGHT:
                return R.drawable.widget_background_night;

            case TIME_AUTO:
            default:
                return getCurrentTimeBackgroundResource();
        }
    }

    /**
     * 현재 휴대폰 시각에 맞는 배경 이미지 리소스를 반환함
     */
    private static int getCurrentTimeBackgroundResource()
    {
        int currentHour =
                Calendar.getInstance()
                        .get(Calendar.HOUR_OF_DAY);

        // 06:00부터 11:59까지 아침 배경을 사용함
        if (currentHour >= 6 && currentHour < 12)
        {
            return R.drawable.widget_background_morning;
        }

        // 12:00부터 17:59까지 낮 배경을 사용함
        if (currentHour >= 12 && currentHour < 18)
        {
            return R.drawable.widget_background_day;
        }

        // 18:00부터 05:59까지 밤 배경을 사용함
        return R.drawable.widget_background_night;
    }

    /**
     * 현재 배경이 자동 시간 모드인지 확인함
     */
    private static boolean isAutomaticTimeMode(
            Context context)
    {
        if (context == null)
        {
            return true;
        }

        Context applicationContext =
                getApplicationContext(context);

        SharedPreferences preferences =
                applicationContext.getSharedPreferences(
                        PREFERENCES_NAME,
                        Context.MODE_PRIVATE
                );

        int timeState =
                preferences.getInt(
                        KEY_TIME_STATE,
                        TIME_AUTO
                );

        return timeState == TIME_AUTO;
    }

    /**
     * 위젯 전체를 누르면 게임이 실행되도록 연결함
     */
    private static void connectGameLaunchIntent(
            Context context,
            RemoteViews remoteViews,
            int appWidgetId)
    {
        Intent launchIntent =
                context.getPackageManager()
                        .getLaunchIntentForPackage(
                                context.getPackageName()
                        );

        if (launchIntent == null)
        {
            return;
        }

        launchIntent.addFlags(
                Intent.FLAG_ACTIVITY_NEW_TASK |
                Intent.FLAG_ACTIVITY_CLEAR_TOP
        );

        int pendingIntentFlags =
                PendingIntent.FLAG_UPDATE_CURRENT;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
        {
            pendingIntentFlags |=
                    PendingIntent.FLAG_IMMUTABLE;
        }

        PendingIntent launchPendingIntent =
                PendingIntent.getActivity(
                        context,
                        appWidgetId,
                        launchIntent,
                        pendingIntentFlags
                );

        remoteViews.setOnClickPendingIntent(
                R.id.widget_root,
                launchPendingIntent
        );
    }

    /**
     * 다음 아침·낮·밤 전환 시각에 배경 갱신을 예약함
     */
    private static void scheduleNextTimeBackgroundRefresh(
            Context context)
    {
        if (context == null)
        {
            return;
        }

        AlarmManager alarmManager =
                (AlarmManager) context.getSystemService(
                        Context.ALARM_SERVICE
                );

        if (alarmManager == null)
        {
            return;
        }

        long nextRefreshTime =
                calculateNextTimeBoundaryMillis();

        PendingIntent refreshPendingIntent =
                createTimeRefreshPendingIntent(context);

        // 기존 예약이 있다면 중복되지 않도록 제거함
        alarmManager.cancel(refreshPendingIntent);

        // 정확한 알람 권한이 필요 없는 일반 알람을 사용함
        alarmManager.set(
                AlarmManager.RTC,
                nextRefreshTime,
                refreshPendingIntent
        );
    }

    /**
     * 다음 06시·12시·18시 중 가장 가까운 시각을 계산함
     */
    private static long calculateNextTimeBoundaryMillis()
    {
        Calendar nextRefresh =
                Calendar.getInstance();

        int currentHour =
                nextRefresh.get(Calendar.HOUR_OF_DAY);

        if (currentHour < 6)
        {
            nextRefresh.set(
                    Calendar.HOUR_OF_DAY,
                    6
            );
        }
        else if (currentHour < 12)
        {
            nextRefresh.set(
                    Calendar.HOUR_OF_DAY,
                    12
            );
        }
        else if (currentHour < 18)
        {
            nextRefresh.set(
                    Calendar.HOUR_OF_DAY,
                    18
            );
        }
        else
        {
            nextRefresh.add(
                    Calendar.DAY_OF_MONTH,
                    1
            );

            nextRefresh.set(
                    Calendar.HOUR_OF_DAY,
                    6
            );
        }

        nextRefresh.set(Calendar.MINUTE, 0);
        nextRefresh.set(Calendar.SECOND, 0);
        nextRefresh.set(Calendar.MILLISECOND, 0);

        return nextRefresh.getTimeInMillis();
    }

    /**
     * 시간대 배경 갱신 Broadcast용 PendingIntent를 생성함
     */
    private static PendingIntent createTimeRefreshPendingIntent(
            Context context)
    {
        Intent refreshIntent =
                new Intent(
                        context,
                        NightPostWidgetProvider.class
                );

        refreshIntent.setAction(
                ACTION_REFRESH_TIME_BACKGROUND
        );

        int pendingIntentFlags =
                PendingIntent.FLAG_UPDATE_CURRENT;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
        {
            pendingIntentFlags |=
                    PendingIntent.FLAG_IMMUTABLE;
        }

        return PendingIntent.getBroadcast(
                context,
                TIME_REFRESH_REQUEST_CODE,
                refreshIntent,
                pendingIntentFlags
        );
    }

    /**
     * 예약된 시간대 배경 갱신을 해제함
     */
    private static void cancelTimeBackgroundRefresh(
            Context context)
    {
        if (context == null)
        {
            return;
        }

        AlarmManager alarmManager =
                (AlarmManager) context.getSystemService(
                        Context.ALARM_SERVICE
                );

        if (alarmManager == null)
        {
            return;
        }

        PendingIntent refreshPendingIntent =
                createTimeRefreshPendingIntent(context);

        alarmManager.cancel(refreshPendingIntent);
        refreshPendingIntent.cancel();
    }

    /**
     * 안전하게 Application Context를 반환함
     */
    private static Context getApplicationContext(
            Context context)
    {
        Context applicationContext =
                context.getApplicationContext();

        if (applicationContext != null)
        {
            return applicationContext;
        }

        return context;
    }
}
