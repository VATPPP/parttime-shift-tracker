using ShiftTracker.App.ViewModels;

namespace ShiftTracker.App.Views;

public partial class AddShiftPage : ContentPage
{
    public AddShiftPage(AddShiftViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
