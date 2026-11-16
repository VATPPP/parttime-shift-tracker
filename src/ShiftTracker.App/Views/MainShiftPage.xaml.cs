using ShiftTracker.App.ViewModels;

namespace ShiftTracker.App.Views;

public partial class MainShiftPage : ContentPage
{
    public MainShiftPage(MainShiftViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainShiftViewModel vm)
        {
            await vm.LoadShiftsCommand.ExecuteAsync(null);
        }
    }
}
