using System.Globalization;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using ShiftTracker.App.Models;
using ShiftTracker.App.Services;
using SQLite;

namespace ShiftTracker.App.Platforms.Android;

[BroadcastReceiver(
    Name = "com.companyname.shifttracker.app.ShiftWidgetProvider",
    Label = "Lịch ca làm tuần",
    Exported = true)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate, ShiftWidgetProvider.ActionMidnightSync])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/shift_widget_info")]
public class ShiftWidgetProvider : AppWidgetProvider
{
    public const string ActionMidnightSync = "com.companyname.shifttracker.app.ACTION_MIDNIGHT_SYNC";

    private const int MidnightRequestCode = 7001;

    private static readonly string[] DayShortNames = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

    private static readonly int[] HeadIds =
    [
        Resource.Id.day_head_0,
        Resource.Id.day_head_1,
        Resource.Id.day_head_2,
        Resource.Id.day_head_3,
        Resource.Id.day_head_4,
        Resource.Id.day_head_5,
        Resource.Id.day_head_6
    ];

    private static readonly int[] DowTextViewIds =
    [
        Resource.Id.day_dow_0,
        Resource.Id.day_dow_1,
        Resource.Id.day_dow_2,
        Resource.Id.day_dow_3,
        Resource.Id.day_dow_4,
        Resource.Id.day_dow_5,
        Resource.Id.day_dow_6
    ];

    private static readonly int[] DateTextViewIds =
    [
        Resource.Id.day_date_0,
        Resource.Id.day_date_1,
        Resource.Id.day_date_2,
        Resource.Id.day_date_3,
        Resource.Id.day_date_4,
        Resource.Id.day_date_5,
        Resource.Id.day_date_6
    ];

    private static readonly int[] ShiftTextViewIds =
    [
        Resource.Id.day_shift_0,
        Resource.Id.day_shift_1,
        Resource.Id.day_shift_2,
        Resource.Id.day_shift_3,
        Resource.Id.day_shift_4,
        Resource.Id.day_shift_5,
        Resource.Id.day_shift_6
    ];

    public override void OnEnabled(Context? context)
    {
        base.OnEnabled(context);
        ScheduleMidnightAlarm(context);
    }

    public override void OnDisabled(Context? context)
    {
        base.OnDisabled(context);
        CancelMidnightAlarm(context);
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        base.OnReceive(context, intent);

        if (context is null || intent?.Action != ActionMidnightSync)
        {
            return;
        }

        try
        {
            var manager = AppWidgetManager.GetInstance(context);
            var ids = manager?.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(ShiftWidgetProvider))));
            if (manager is not null && ids is { Length: > 0 })
            {
                OnUpdate(context, manager, ids);
            }
        }
        catch
        {
            // Widget refresh must never crash the host process.
        }
        finally
        {
            ScheduleMidnightAlarm(context);
        }
    }

    /// <summary>
    /// Schedules a one-shot alarm for 00:00:01 of the next day; re-armed after each trigger.
    /// </summary>
    public static void ScheduleMidnightAlarm(Context? context)
    {
        if (context is null)
        {
            return;
        }

        try
        {
            var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
            var pendingIntent = CreateMidnightPendingIntent(context, PendingIntentFlags.UpdateCurrent);
            if (alarmManager is null || pendingIntent is null)
            {
                return;
            }

            var nextMidnight = DateTime.Today.AddDays(1).AddSeconds(1);
            var delayMs = Math.Max(1000L, (long)(nextMidnight - DateTime.Now).TotalMilliseconds);
            var triggerAtMs = Java.Lang.JavaSystem.CurrentTimeMillis() + delayMs;

            if (!OperatingSystem.IsAndroidVersionAtLeast(31) || alarmManager.CanScheduleExactAlarms())
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.Rtc, triggerAtMs, pendingIntent);
            }
            else
            {
                alarmManager.SetAndAllowWhileIdle(AlarmType.Rtc, triggerAtMs, pendingIntent);
            }
        }
        catch
        {
            // Scheduling is best-effort (e.g. exact-alarm permission revoked).
        }
    }

    public static void CancelMidnightAlarm(Context? context)
    {
        if (context is null)
        {
            return;
        }

        try
        {
            var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
            var pendingIntent = CreateMidnightPendingIntent(context, PendingIntentFlags.NoCreate);
            if (alarmManager is not null && pendingIntent is not null)
            {
                alarmManager.Cancel(pendingIntent);
                pendingIntent.Cancel();
            }
        }
        catch
        {
            // Ignore.
        }
    }

    private static PendingIntent? CreateMidnightPendingIntent(Context context, PendingIntentFlags extraFlags)
    {
        var intent = new Intent(context, typeof(ShiftWidgetProvider));
        intent.SetAction(ActionMidnightSync);
        return PendingIntent.GetBroadcast(context, MidnightRequestCode, intent, extraFlags | PendingIntentFlags.Immutable);
    }

    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context is null || appWidgetManager is null || appWidgetIds is null || appWidgetIds.Length == 0)
        {
            return;
        }

        ScheduleMidnightAlarm(context);

        var today = DateTime.Today;
        var diffToMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = today.AddDays(-diffToMonday).Date;
        var sunday = monday.AddDays(6).Date;

        var shifts = LoadWeekShiftsFromDatabase(context, monday, sunday);

        foreach (var widgetId in appWidgetIds)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.shift_widget_layout);
            views.SetTextViewText(
                Resource.Id.widget_week_title,
                $"Lịch tuần ({monday:dd/MM} - {sunday:dd/MM})");

            for (var i = 0; i < 7; i++)
            {
                var date = monday.AddDays(i);
                var dayShifts = shifts
                    .Where(s => s.ShiftDate.Date == date)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                views.SetTextViewText(DowTextViewIds[i], DayShortNames[i]);
                views.SetTextViewText(DateTextViewIds[i], date.ToString("dd/MM", CultureInfo.InvariantCulture));

                if (dayShifts.Count > 0)
                {
                    var coworkers = dayShifts
                        .SelectMany(s => s.CoworkerList)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var text = coworkers.Count > 0 ? string.Join("\n", coworkers) : "Một mình";
                    views.SetTextViewText(ShiftTextViewIds[i], text);
                    views.SetTextColor(ShiftTextViewIds[i], global::Android.Graphics.Color.ParseColor("#C2185B"));
                }
                else
                {
                    views.SetTextViewText(ShiftTextViewIds[i], "Nghỉ");
                    views.SetTextColor(ShiftTextViewIds[i], global::Android.Graphics.Color.ParseColor("#B5A5A8"));
                }

                var isHighlighted = date == today || dayShifts.Count > 0;
                views.SetInt(
                    HeadIds[i],
                    "setBackgroundResource",
                    isHighlighted ? Resource.Drawable.widget_today_bg : 0);
            }

            var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty);
            if (launchIntent is not null)
            {
                var pendingIntent = PendingIntent.GetActivity(
                    context,
                    0,
                    launchIntent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.widget_root, pendingIntent);
            }

            appWidgetManager.UpdateAppWidget(widgetId, views);
        }
    }

    private static List<WorkShift> LoadWeekShiftsFromDatabase(Context context, DateTime startDate, DateTime endDate)
    {
        try
        {
            var filesDir = context.FilesDir?.AbsolutePath;
            var dbPath = !string.IsNullOrWhiteSpace(filesDir)
                ? Path.Combine(filesDir, DatabaseService.DatabaseFileName)
                : DatabaseService.DatabasePath;

            if (!File.Exists(dbPath))
            {
                return [];
            }

            using var conn = new SQLiteConnection(dbPath, DatabaseService.Flags);
            conn.CreateTable<WorkShift>();

            var from = startDate.Date;
            var to = endDate.Date.AddDays(1).AddTicks(-1);

            return conn.Table<WorkShift>()
                .Where(s => s.ShiftDate >= from && s.ShiftDate <= to)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}

