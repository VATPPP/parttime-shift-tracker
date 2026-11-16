using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShiftTracker.App.Models;

public partial class QuickMonthItem : ObservableObject
{
    public int Month { get; init; }

    public int RowIndex => (Month - 1) / 4;

    public int ColumnIndex => (Month - 1) % 4;

    public string Label => $"Th.{Month:00}";

    [ObservableProperty]
    private bool _isSelected;

    public IRelayCommand<QuickMonthItem>? SelectCommand { get; set; }
}

public partial class QuickYearItem : ObservableObject
{
    public int GridIndex { get; init; }

    public int RowIndex => GridIndex / 3;

    public int ColumnIndex => GridIndex % 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Label))]
    private int _year;

    public string Label => Year.ToString();

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isCurrentYear;

    [ObservableProperty]
    private bool _isEnabled = true;

    public IRelayCommand<QuickYearItem>? SelectCommand { get; set; }
}
