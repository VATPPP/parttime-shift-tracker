using ShiftTracker.App.ViewModels;

namespace ShiftTracker.App.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DashboardViewModel vm)
        {
            await vm.LoadSummaryCommand.ExecuteAsync(null);
        }
    }
}
