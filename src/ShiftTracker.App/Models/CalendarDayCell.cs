using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShiftTracker.App.Models;

public partial class CalendarDayCell : ObservableObject
{
    public int RowIndex { get; init; }

    public int ColumnIndex { get; init; }

    public DateTime Date { get; init; }

    public bool IsCurrentMonth { get; init; }

    public bool IsToday => Date.Date == DateTime.Today;

    public string DayNumberText => Date.Day.ToString();

    public IRelayCommand<CalendarDayCell>? SelectCommand { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCellSummary))]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCellSummary))]
    [NotifyPropertyChangedFor(nameof(DayNetSalaryFormatted))]
    [NotifyPropertyChangedFor(nameof(DayTotalHoursFormatted))]
    private bool _hasShifts;

    [ObservableProperty]
    private bool _isHolidayDate;

    [ObservableProperty]
    private string _holidayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayNetSalaryFormatted))]
    private decimal _dayNetSalary;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayTotalHoursFormatted))]
    private double _dayTotalHours;

    public ObservableCollection<WorkShift> DayShifts { get; } = [];

    public bool ShowCellSummary => HasShifts && !IsSelected;

    public string DayNetSalaryFormatted =>
        HasShifts ? WorkShift.FormatCurrencyK(DayNetSalary) : string.Empty;

    public string DayTotalHoursFormatted
    {
        get
        {
            if (!HasShifts || DayTotalHours <= 0)
            {
                return string.Empty;
            }

            var rounded = Math.Round(DayTotalHours, 1, MidpointRounding.AwayFromZero);
            return $"{rounded.ToString("0.#", CultureInfo.InvariantCulture)}h";
        }
    }
}
