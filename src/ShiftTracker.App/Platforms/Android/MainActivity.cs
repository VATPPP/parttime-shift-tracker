using Android.App;
using Android.Content.PM;
using Android.OS;
using ShiftTracker.App.Services;

namespace ShiftTracker.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnPostCreate(Bundle? savedInstanceState)
    {
        base.OnPostCreate(savedInstanceState);
        SyncBars();
    }

    protected override void OnResume()
    {
        base.OnResume();
        SyncBars();
    }

    private static void SyncBars()
    {
        var themeService = IPlatformApplication.Current?.Services.GetService<ThemeService>();
        themeService?.RefreshPlatformBars();
    }
}
