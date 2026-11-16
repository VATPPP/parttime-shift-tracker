using Microsoft.Extensions.Logging;
using ShiftTracker.App.Services;
using ShiftTracker.App.ViewModels;
using ShiftTracker.App.Views;

namespace ShiftTracker.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<HolidayService>();
        builder.Services.AddSingleton<AnalyticsService>();
        builder.Services.AddSingleton<ThemeService>();

        builder.Services.AddTransient<CalendarViewModel>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<MainShiftViewModel>();
        builder.Services.AddTransient<MainShiftPage>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<AddShiftViewModel>();
        builder.Services.AddTransient<AddShiftPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
