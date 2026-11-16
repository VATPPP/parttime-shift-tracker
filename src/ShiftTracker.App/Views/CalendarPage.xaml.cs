using ShiftTracker.App.ViewModels;

namespace ShiftTracker.App.Views;

public partial class CalendarPage : ContentPage
{
    public CalendarPage(CalendarViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CalendarViewModel vm)
        {
            await vm.LoadMonthCommand.ExecuteAsync(null);
        }
    }
}
