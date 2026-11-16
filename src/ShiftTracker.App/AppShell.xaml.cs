using ShiftTracker.App.Views;

namespace ShiftTracker.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(AddShiftPage), typeof(AddShiftPage));
    }
}
