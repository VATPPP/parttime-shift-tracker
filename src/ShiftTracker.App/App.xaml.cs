using ShiftTracker.App.Services;

namespace ShiftTracker.App;

public partial class App : Application
{
    public App(ThemeService themeService)
    {
        InitializeComponent();
        themeService.InitializeTheme();
    }

    protected override void OnSleep()
    {
        base.OnSleep();
#if ANDROID
        ShiftTracker.App.Platforms.Android.WidgetUpdateHelper.UpdateWidget(global::Android.App.Application.Context);
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell());
}
