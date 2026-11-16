using Android.Appwidget;
using Android.Content;

namespace ShiftTracker.App.Platforms.Android;

public static class WidgetUpdateHelper
{
    /// <summary>
    /// Asks every placed ShiftWidgetProvider instance to redraw itself.
    /// Safe to call with a null context or when no widget is placed.
    /// </summary>
    public static void UpdateWidget(Context? context)
    {
        if (context is null)
        {
            return;
        }

        try
        {
            var manager = AppWidgetManager.GetInstance(context);
            var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(ShiftWidgetProvider)));
            var ids = manager?.GetAppWidgetIds(component);
            if (ids is null || ids.Length == 0)
            {
                return;
            }

            var intent = new Intent(context, typeof(ShiftWidgetProvider));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }
        catch
        {
            // Widget sync is best-effort and must never crash the app.
        }
    }
}

