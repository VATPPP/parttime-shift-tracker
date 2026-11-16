namespace ShiftTracker.App.Controls;

public partial class FloatingNavBar : ContentView
{
    private static readonly Color InactivePillBackground = Colors.Transparent;

    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(
            nameof(ActiveTab),
            typeof(string),
            typeof(FloatingNavBar),
            "Shift",
            propertyChanged: OnActiveTabChanged);

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public FloatingNavBar()
    {
        InitializeComponent();
        UpdateVisualStates();
    }

    private static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FloatingNavBar navBar)
        {
            navBar.UpdateVisualStates();
        }
    }

    private void UpdateVisualStates()
    {
        bool isCalendar = string.Equals(ActiveTab, "Calendar", StringComparison.OrdinalIgnoreCase);
        bool isShift = string.Equals(ActiveTab, "Shift", StringComparison.OrdinalIgnoreCase);
        bool isDashboard = string.Equals(ActiveTab, "Dashboard", StringComparison.OrdinalIgnoreCase);

        ApplyTabState(CalendarPill, CalendarLabel, isCalendar);
        ApplyTabState(ShiftPill, ShiftLabel, isShift);
        ApplyTabState(DashboardPill, DashboardLabel, isDashboard);
    }

    private static void ApplyTabState(Border pill, Label label, bool isActive)
    {
        if (isActive)
        {
            pill.SetDynamicResource(VisualElement.BackgroundColorProperty, "PrimarySoftColor");
            label.SetDynamicResource(Label.TextColorProperty, "PrimaryColor");
        }
        else
        {
            pill.BackgroundColor = InactivePillBackground;
            label.SetDynamicResource(Label.TextColorProperty, "SecondaryTextColor");
        }
    }

    private async void OnCalendarTapped(object? sender, TappedEventArgs e)
    {
        if (!string.Equals(ActiveTab, "Calendar", StringComparison.OrdinalIgnoreCase))
        {
            await Shell.Current.GoToAsync("///CalendarPage");
        }
    }

    private async void OnShiftTapped(object? sender, TappedEventArgs e)
    {
        if (!string.Equals(ActiveTab, "Shift", StringComparison.OrdinalIgnoreCase))
        {
            await Shell.Current.GoToAsync("///MainShiftPage");
        }
    }

    private async void OnDashboardTapped(object? sender, TappedEventArgs e)
    {
        if (!string.Equals(ActiveTab, "Dashboard", StringComparison.OrdinalIgnoreCase))
        {
            await Shell.Current.GoToAsync("///DashboardPage");
        }
    }
}
