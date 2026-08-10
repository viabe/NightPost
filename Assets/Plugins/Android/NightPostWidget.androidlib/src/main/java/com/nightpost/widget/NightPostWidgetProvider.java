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

import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;

public class NightPostWidgetProvider extends AppWidgetProvider
{
    // 위젯 표시 데이터를 저장하는 SharedPreferences 이름임
    private static final String PREFERENCES_NAME =
            "NightPostWidgetPreferences";

    // 배달 대기 편지 수를 저장하는 키임
    private static final String KEY_WAITING_COUNT =
            "WaitingCount";

    // 실제 게임에서 확인하지 않은 도착 편지 수를 저장하는 키임
    private static final String KEY_ARRIVED_COUNT =
            "ArrivedCount";

    // 진행 중 배달들의 완료 예정 Unix 시각을 저장하는 키임
    private static final String KEY_COMPLETION_TIMES =
            "CompletionTimes";

    // 시간대 배경 갱신 Broadcast Action임
    private static final String ACTION_REFRESH_TIME_BACKGROUND =
            "com.nightpost.widget.ACTION_REFRESH_TIME_BACKGROUND";

    // 배달 완료 상태 갱신 Broadcast Action임
    private static final String ACTION_REFRESH_DELIVERY =
            "com.nightpost.widget.ACTION_REFRESH_DELIVERY";

    // 시간대 갱신 PendingIntent 식별값임
    private static final int TIME_REFRESH_REQUEST_CODE =
            1001;

    // 배달 완료 갱신 PendingIntent 식별값임
    private static final int DELIVERY_REFRESH_REQUEST_CODE =
            1002;

    // 배달 대기 상태임
    private static final int STATE_WAITING = 0;

    // 배달 진행 상태임
    private static final int STATE_DELIVERING = 1;

    // 편지 도착 상태임
    private static final int STATE_ARRIVED = 2;

    /**
     * 첫 번째 위젯이 홈 화면에 추가되면 현재 화면을 갱신함
     */
    @Override
    public void onEnabled(Context context)
    {
        super.onEnabled(context);

        refreshAllWidgets(context);

        scheduleNextTimeBackgroundRefresh(context);
        scheduleNextDeliveryRefresh(context);
    }

    /**
     * Android가 위젯 갱신을 요청하면 현재 데이터를 다시 표시함
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

        scheduleNextTimeBackgroundRefresh(context);
        scheduleNextDeliveryRefresh(context);
    }

    /**
     * 시간대 또는 배달 완료 예약 Broadcast를 처리함
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

        String action =
                intent.getAction();

        // 아침·낮·밤 배경 변경 시점임
        if (ACTION_REFRESH_TIME_BACKGROUND.equals(action))
        {
            refreshAllWidgets(context);

            scheduleNextTimeBackgroundRefresh(context);

            return;
        }

        // 진행 중 배달의 완료 예정 시각에 도달함
        if (ACTION_REFRESH_DELIVERY.equals(action))
        {
            refreshAllWidgets(context);

            // 다음 진행 중 배달이 있다면 다시 예약함
            scheduleNextDeliveryRefresh(context);
        }
    }

    /**
     * 마지막 위젯이 제거되면 예약된 알람을 해제함
     */
    @Override
    public void onDisabled(Context context)
    {
        super.onDisabled(context);

        cancelTimeBackgroundRefresh(context);
        cancelDeliveryRefresh(context);
    }

    /**
     * Unity에서 현재 게임의 위젯 데이터를 전달받음
     */
    public static void syncWidgetData(
            Context context,
            int waitingCount,
            int arrivedCount,
            String completionTimesCsv)
    {
        if (context == null)
        {
            return;
        }

        Context applicationContext =
                getApplicationContext(context);

        // 전달된 편지 수가 음수가 되지 않도록 처리함
        waitingCount =
                Math.max(0, waitingCount);

        arrivedCount =
                Math.max(0, arrivedCount);

        // 완료 예정 시각 목록이 없다면 빈 문자열로 처리함
        if (completionTimesCsv == null)
        {
            completionTimesCsv = "";
        }

        SharedPreferences preferences =
                applicationContext.getSharedPreferences(
                        PREFERENCES_NAME,
                        Context.MODE_PRIVATE
                );

        // Unity에서 전달받은 최신 데이터를 저장함
        preferences.edit()
                .putInt(
                        KEY_WAITING_COUNT,
                        waitingCount
                )
                .putInt(
                        KEY_ARRIVED_COUNT,
                        arrivedCount
                )
                .putString(
                        KEY_COMPLETION_TIMES,
                        completionTimesCsv
                )
                .apply();

        // 현재 데이터를 위젯에 즉시 반영함
        refreshAllWidgets(
                applicationContext
        );

        // 진행 중 배달의 다음 완료 시각을 예약함
        scheduleNextDeliveryRefresh(
                applicationContext
        );

        // 다음 아침·낮·밤 전환도 예약함
        scheduleNextTimeBackgroundRefresh(
                applicationContext
        );
    }

    /**
     * 현재 홈 화면에 설치된 모든 위젯을 갱신함
     */
    public static void refreshAllWidgets(
            Context context)
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
     * 현재 시간과 저장 데이터를 기준으로 위젯 상태를 계산하여 표시함
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

        // Unity에서 전달받은 배달 대기 편지 수임
        int waitingCount =
                Math.max(
                        0,
                        preferences.getInt(
                                KEY_WAITING_COUNT,
                                0
                        )
                );

        // Unity가 실제 게임 데이터로 처리한 미확인 도착 편지 수임
        int savedArrivedCount =
                Math.max(
                        0,
                        preferences.getInt(
                                KEY_ARRIVED_COUNT,
                                0
                        )
                );

        // Unity에서 전달받은 진행 중 배달 완료 예정 시각 목록임
        String completionTimesCsv =
                preferences.getString(
                        KEY_COMPLETION_TIMES,
                        ""
                );

        List<Long> completionTimes =
                parseCompletionTimes(
                        completionTimesCsv
                );

        // 현재 Unix 시각을 초 단위로 계산함
        long currentUnixTime =
                System.currentTimeMillis() / 1000L;

        // 게임 종료 후 완료된 배달 수임
        int completedSinceLastSync = 0;

        // 아직 완료되지 않은 진행 중 배달 수임
        int deliveringCount = 0;

        for (long completionTime : completionTimes)
        {
            if (completionTime <= currentUnixTime)
            {
                completedSinceLastSync++;
            }
            else
            {
                deliveringCount++;
            }
        }

        // 실제 미확인 도착 편지와
        // 게임 종료 후 새로 완료된 편지를 합산함
        int arrivedCount =
                savedArrivedCount +
                completedSinceLastSync;

        int widgetState;
        int displayCount;

        // 도착한 편지를 가장 우선해서 표시함
        if (arrivedCount > 0)
        {
            widgetState =
                    STATE_ARRIVED;

            displayCount =
                    arrivedCount;
        }
        // 도착 편지가 없고 진행 중 배달이 있다면 배달 중으로 표시함
        else if (deliveringCount > 0)
        {
            widgetState =
                    STATE_DELIVERING;

            displayCount =
                    deliveringCount;
        }
        // 그 외에는 배달 대기로 표시함
        else
        {
            widgetState =
                    STATE_WAITING;

            displayCount =
                    waitingCount;
        }

        String statusText;
        String countText;
        int stateIconResource;

        switch (widgetState)
        {
            case STATE_DELIVERING:
                statusText =
                        "배달 중";

                countText =
                        "편지 " +
                        displayCount +
                        "통 이동 중";

                stateIconResource =
                        R.drawable.widget_icon_delivering;

                break;

            case STATE_ARRIVED:
                statusText =
                        "편지가 도착했어요";

                countText =
                        "도착한 편지 " +
                        displayCount +
                        "통";

                stateIconResource =
                        R.drawable.widget_icon_arrived;

                break;

            case STATE_WAITING:
            default:
                statusText =
                        "배달 대기";

                countText =
                        "준비된 편지 " +
                        displayCount +
                        "통";

                stateIconResource =
                        R.drawable.widget_icon_waiting;

                break;
        }

        // 현재 휴대폰 시간에 맞는 배경을 선택함
        int backgroundResource =
                getCurrentTimeBackgroundResource();

        RemoteViews remoteViews =
                new RemoteViews(
                        context.getPackageName(),
                        R.layout.night_post_widget
                );

        // 아침·낮·밤 배경을 적용함
        remoteViews.setImageViewResource(
                R.id.widget_background_image,
                backgroundResource
        );

        // 현재 배달 상태 아이콘을 적용함
        remoteViews.setImageViewResource(
                R.id.widget_state_icon,
                stateIconResource
        );

        // 현재 상태 문구를 적용함
        remoteViews.setTextViewText(
                R.id.widget_status_text,
                statusText
        );

        // 현재 편지 수를 적용함
        remoteViews.setTextViewText(
                R.id.widget_count_text,
                countText
        );

        // 위젯을 누르면 게임이 실행되도록 연결함
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
     * Unity에서 전달받은 완료 예정 시각 문자열을 목록으로 변환함
     */
    private static List<Long> parseCompletionTimes(
            String completionTimesCsv)
    {
        List<Long> completionTimes =
                new ArrayList<>();

        if (completionTimesCsv == null ||
                completionTimesCsv.trim().isEmpty())
        {
            return completionTimes;
        }

        String[] values =
                completionTimesCsv.split(",");

        for (String value : values)
        {
            if (value == null ||
                    value.trim().isEmpty())
            {
                continue;
            }

            try
            {
                long completionTime =
                        Long.parseLong(
                                value.trim()
                        );

                if (completionTime <= 0)
                {
                    continue;
                }

                completionTimes.add(
                        completionTime
                );
            }
            catch (NumberFormatException ignored)
            {
                // 잘못된 완료 시각은 무시함
            }
        }

        return completionTimes;
    }

    /**
     * 현재 휴대폰 시각에 맞는 배경 이미지 리소스를 반환함
     */
    private static int getCurrentTimeBackgroundResource()
    {
        int currentHour =
                Calendar.getInstance()
                        .get(Calendar.HOUR_OF_DAY);

        // 06:00 ~ 11:59 아침 배경임
        if (currentHour >= 6 &&
                currentHour < 12)
        {
            return R.drawable.widget_background_morning;
        }

        // 12:00 ~ 17:59 낮 배경임
        if (currentHour >= 12 &&
                currentHour < 18)
        {
            return R.drawable.widget_background_day;
        }

        // 18:00 ~ 05:59 밤 배경임
        return R.drawable.widget_background_night;
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

        if (Build.VERSION.SDK_INT >=
                Build.VERSION_CODES.M)
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
     * 가장 가까운 배달 완료 예정 시각에 위젯 갱신을 예약함
     */
    private static void scheduleNextDeliveryRefresh(
            Context context)
    {
        if (context == null)
        {
            return;
        }

        Context applicationContext =
                getApplicationContext(context);

        AlarmManager alarmManager =
                (AlarmManager)
                        applicationContext.getSystemService(
                                Context.ALARM_SERVICE
                        );

        if (alarmManager == null)
        {
            return;
        }

        SharedPreferences preferences =
                applicationContext.getSharedPreferences(
                        PREFERENCES_NAME,
                        Context.MODE_PRIVATE
                );

        String completionTimesCsv =
                preferences.getString(
                        KEY_COMPLETION_TIMES,
                        ""
                );

        List<Long> completionTimes =
                parseCompletionTimes(
                        completionTimesCsv
                );

        long currentUnixTime =
                System.currentTimeMillis() / 1000L;

        long nextCompletionTime =
                Long.MAX_VALUE;

        // 아직 완료되지 않은 배달 중 가장 빠른 완료시간을 찾음
        for (long completionTime : completionTimes)
        {
            if (completionTime <= currentUnixTime)
            {
                continue;
            }

            if (completionTime < nextCompletionTime)
            {
                nextCompletionTime =
                        completionTime;
            }
        }

        PendingIntent refreshPendingIntent =
                createDeliveryRefreshPendingIntent(
                        applicationContext
                );

        // 이전에 등록된 배달 완료 갱신을 제거함
        alarmManager.cancel(
                refreshPendingIntent
        );

        // 아직 진행 중인 배달이 없다면 새 알람을 등록하지 않음
        if (nextCompletionTime == Long.MAX_VALUE)
        {
            return;
        }

        long triggerTimeMillis =
                nextCompletionTime * 1000L;

        // 게임이 종료되거나 Doze 상태여도 갱신될 수 있도록 예약함
        if (Build.VERSION.SDK_INT >=
                Build.VERSION_CODES.M)
        {
            alarmManager.setAndAllowWhileIdle(
                    AlarmManager.RTC_WAKEUP,
                    triggerTimeMillis,
                    refreshPendingIntent
            );
        }
        else
        {
            alarmManager.set(
                    AlarmManager.RTC_WAKEUP,
                    triggerTimeMillis,
                    refreshPendingIntent
            );
        }
    }

    /**
     * 배달 완료 갱신 Broadcast용 PendingIntent를 생성함
     */
    private static PendingIntent createDeliveryRefreshPendingIntent(
            Context context)
    {
        Intent refreshIntent =
                new Intent(
                        context,
                        NightPostWidgetProvider.class
                );

        refreshIntent.setAction(
                ACTION_REFRESH_DELIVERY
        );

        int pendingIntentFlags =
                PendingIntent.FLAG_UPDATE_CURRENT;

        if (Build.VERSION.SDK_INT >=
                Build.VERSION_CODES.M)
        {
            pendingIntentFlags |=
                    PendingIntent.FLAG_IMMUTABLE;
        }

        return PendingIntent.getBroadcast(
                context,
                DELIVERY_REFRESH_REQUEST_CODE,
                refreshIntent,
                pendingIntentFlags
        );
    }

    /**
     * 예약된 배달 완료 갱신을 해제함
     */
    private static void cancelDeliveryRefresh(
            Context context)
    {
        if (context == null)
        {
            return;
        }

        AlarmManager alarmManager =
                (AlarmManager)
                        context.getSystemService(
                                Context.ALARM_SERVICE
                        );

        if (alarmManager == null)
        {
            return;
        }

        PendingIntent refreshPendingIntent =
                createDeliveryRefreshPendingIntent(
                        context
                );

        alarmManager.cancel(
                refreshPendingIntent
        );

        refreshPendingIntent.cancel();
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
                (AlarmManager)
                        context.getSystemService(
                                Context.ALARM_SERVICE
                        );

        if (alarmManager == null)
        {
            return;
        }

        long nextRefreshTime =
                calculateNextTimeBoundaryMillis();

        PendingIntent refreshPendingIntent =
                createTimeRefreshPendingIntent(
                        context
                );

        // 기존 시간대 갱신 예약을 제거함
        alarmManager.cancel(
                refreshPendingIntent
        );

        // 정확 알람 권한 없이 일반 시간대 갱신을 예약함
        if (Build.VERSION.SDK_INT >=
                Build.VERSION_CODES.M)
        {
            alarmManager.setAndAllowWhileIdle(
                    AlarmManager.RTC,
                    nextRefreshTime,
                    refreshPendingIntent
            );
        }
        else
        {
            alarmManager.set(
                    AlarmManager.RTC,
                    nextRefreshTime,
                    refreshPendingIntent
            );
        }
    }

    /**
     * 다음 06시·12시·18시 중 가장 가까운 시각을 계산함
     */
    private static long calculateNextTimeBoundaryMillis()
    {
        Calendar nextRefresh =
                Calendar.getInstance();

        int currentHour =
                nextRefresh.get(
                        Calendar.HOUR_OF_DAY
                );

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

        nextRefresh.set(
                Calendar.MINUTE,
                0
        );

        nextRefresh.set(
                Calendar.SECOND,
                0
        );

        nextRefresh.set(
                Calendar.MILLISECOND,
                0
        );

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

        if (Build.VERSION.SDK_INT >=
                Build.VERSION_CODES.M)
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
                (AlarmManager)
                        context.getSystemService(
                                Context.ALARM_SERVICE
                        );

        if (alarmManager == null)
        {
            return;
        }

        PendingIntent refreshPendingIntent =
                createTimeRefreshPendingIntent(
                        context
                );

        alarmManager.cancel(
                refreshPendingIntent
        );

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
