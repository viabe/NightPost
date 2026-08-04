package com.nightpost.widget;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;
import android.widget.RemoteViews;

public class NightPostWidgetProvider extends AppWidgetProvider
{
    private static final String PREFERENCES_NAME = "NightPostWidgetPreferences";
    private static final String KEY_WIDGET_STATE = "WidgetState";
    private static final String KEY_LETTER_COUNT = "LetterCount";

    public static final int STATE_WAITING = 0;
    public static final int STATE_DELIVERING = 1;
    public static final int STATE_ARRIVED = 2;

    @Override
    public void onUpdate(
            Context context,
            AppWidgetManager appWidgetManager,
            int[] appWidgetIds)
    {
        for (int appWidgetId : appWidgetIds)
        {
            updateAppWidget(context, appWidgetManager, appWidgetId);
        }
    }

    /**
     * Unity에서 전달받은 위젯 데이터를 저장하고 모든 위젯을 갱신함
     */
    public static void updateWidgetData(
            Context context,
            int widgetState,
            int letterCount)
    {
        if (context == null)
        {
            return;
        }

        if (widgetState < STATE_WAITING || widgetState > STATE_ARRIVED)
        {
            widgetState = STATE_WAITING;
        }

        letterCount = Math.max(0, letterCount);

        SharedPreferences preferences = context.getSharedPreferences(
                PREFERENCES_NAME,
                Context.MODE_PRIVATE
        );

        preferences.edit()
                .putInt(KEY_WIDGET_STATE, widgetState)
                .putInt(KEY_LETTER_COUNT, letterCount)
                .apply();

        refreshAllWidgets(context);
    }

    /**
     * 현재 설치된 모든 밤에 오는 편지 위젯을 갱신함
     */
    public static void refreshAllWidgets(Context context)
    {
        AppWidgetManager appWidgetManager =
                AppWidgetManager.getInstance(context);

        ComponentName widgetComponent = new ComponentName(
                context,
                NightPostWidgetProvider.class
        );

        int[] appWidgetIds =
                appWidgetManager.getAppWidgetIds(widgetComponent);

        for (int appWidgetId : appWidgetIds)
        {
            updateAppWidget(context, appWidgetManager, appWidgetId);
        }
    }

    /**
     * 저장된 데이터를 읽고 지정된 위젯 화면에 반영함
     */
    private static void updateAppWidget(
            Context context,
            AppWidgetManager appWidgetManager,
            int appWidgetId)
    {
        SharedPreferences preferences = context.getSharedPreferences(
                PREFERENCES_NAME,
                Context.MODE_PRIVATE
        );

        int widgetState = preferences.getInt(
                KEY_WIDGET_STATE,
                STATE_WAITING
        );

        int letterCount = Math.max(
                0,
                preferences.getInt(KEY_LETTER_COUNT, 0)
        );

        String statusText;
        String countText;

        switch (widgetState)
        {
            case STATE_DELIVERING:
                statusText = "배달 중";
                countText = "편지 " + letterCount + "통 이동 중";
                break;

            case STATE_ARRIVED:
                statusText = "편지가 도착했어요";
                countText = "도착한 편지 " + letterCount + "통";
                break;

            case STATE_WAITING:
            default:
                statusText = "배달 대기";
                countText = "준비된 편지 " + letterCount + "통";
                break;
        }

        RemoteViews remoteViews = new RemoteViews(
                context.getPackageName(),
                R.layout.night_post_widget
        );

        remoteViews.setTextViewText(
                R.id.widget_status_text,
                statusText
        );

        remoteViews.setTextViewText(
                R.id.widget_count_text,
                countText
        );

        Intent launchIntent = context
                .getPackageManager()
                .getLaunchIntentForPackage(context.getPackageName());

        if (launchIntent != null)
        {
            launchIntent.addFlags(
                    Intent.FLAG_ACTIVITY_NEW_TASK |
                    Intent.FLAG_ACTIVITY_CLEAR_TOP
            );

            int pendingIntentFlags = PendingIntent.FLAG_UPDATE_CURRENT;

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M)
            {
                pendingIntentFlags |= PendingIntent.FLAG_IMMUTABLE;
            }

            PendingIntent launchPendingIntent = PendingIntent.getActivity(
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

        appWidgetManager.updateAppWidget(
                appWidgetId,
                remoteViews
        );
    }
}
