using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftTracker.App.Models;
using ShiftTracker.App.Services;
using ShiftTracker.App.Views;

namespace ShiftTracker.App.ViewModels;

public partial class CalendarViewModel : PageChromeViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly HolidayService _holidayService;
    private List<WorkShift> _monthShifts = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentMonthYearTitle))]
    [NotifyPropertyChangedFor(nameof(CanGoPreviousMonth))]
    [NotifyPropertyChangedFor(nameof(CanGoNextMonth))]
    private int _currentYear = DateTime.Today.Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentMonthYearTitle))]
    [NotifyPropertyChangedFor(nameof(CanGoPreviousMonth))]
    [NotifyPropertyChangedFor(nameof(CanGoNextMonth))]
    private int _currentMonth = DateTime.Today.Month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDateTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedDayNetSalaryFormatted))]
    [NotifyPropertyChangedFor(nameof(SelectedDayHoursFormatted))]
    private DateTime? _selectedDate;

    [ObservableProperty]
    private bool _isDetailPanelVisible;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlySummaryText))]
    [NotifyPropertyChangedFor(nameof(MonthlyNetSalaryFormatted))]
    private decimal _monthlyNetSalary;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlySummaryText))]
    private double _monthlyTotalHours;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlySummaryText))]
    private int _monthlyShiftCount;

    public ObservableCollection<CalendarDayCell> CalendarCells { get; } = [];

    public ObservableCollection<WorkShift> SelectedDayShifts { get; } = [];

    public bool HasSelectedDayShifts => SelectedDayShifts.Count > 0;

    public bool CanGoPreviousMonth =>
        CurrentYear > MinAllowedYear || (CurrentYear == MinAllowedYear && CurrentMonth > 1);

    public bool CanGoNextMonth =>
        CurrentYear < MaxAllowedYear || (CurrentYear == MaxAllowedYear && CurrentMonth < 12);

    public string CurrentMonthYearTitle =>
        $"Tháng {CurrentMonth:00}/{CurrentYear}";

    public string MonthlySummaryText =>
        $"Tháng này: {MonthlyShiftCount} ca • {MonthlyTotalHours:0.#}h";

    public string MonthlyNetSalaryFormatted =>
        WorkShift.FormatCurrencyK(MonthlyNetSalary);

    public string SelectedDateTitle =>
        SelectedDate.HasValue
            ? $"{WorkShift.GetVietnameseDayOfWeek(SelectedDate.Value.DayOfWeek)}, {SelectedDate.Value:dd/MM/yyyy}"
            : string.Empty;

    public string SelectedDayNetSalaryFormatted =>
        HasSelectedDayShifts
            ? WorkShift.FormatCurrencyK(SelectedDayShifts.Sum(s => s.NetShiftSalary))
            : "0k";

    public string SelectedDayHoursFormatted
    {
        get
        {
            if (!HasSelectedDayShifts)
            {
                return "0h";
            }

            var totalMins = (int)Math.Round(SelectedDayShifts.Sum(s => s.TotalMinutes));
            var h = totalMins / 60;
            var m = totalMins % 60;
            return m > 0 ? $"{h}h {m}p" : $"{h}h";
        }
    }

    public CalendarViewModel(
        DatabaseService databaseService,
        HolidayService holidayService,
        ThemeService themeService)
        : base(themeService)
    {
        _databaseService = databaseService;
        _holidayService = holidayService;

        BuildCalendarGrid();
    }

    [RelayCommand]
    private void OpenQuickJumpPopup()
    {
        OpenQuickJumpForDate(CurrentMonth, CurrentYear);
    }

    [RelayCommand]
    private async Task JumpToTodayAsync()
    {
        CurrentYear = DateTime.Today.Year;
        CurrentMonth = DateTime.Today.Month;
        SelectedDate = null;
        IsDetailPanelVisible = false;
        IsQuickJumpPopupOpen = false;
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task ConfirmQuickJumpAsync()
    {
        CurrentYear = ClampAllowedYear(PickerYear);
        CurrentMonth = Math.Clamp(PickerMonth, 1, 12);
        SelectedDate = null;
        IsDetailPanelVisible = false;
        IsQuickJumpPopupOpen = false;
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task LoadMonthAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            _monthShifts = await _databaseService.GetShiftsByMonthAsync(CurrentYear, CurrentMonth);

            foreach (var shift in _monthShifts)
            {
                shift.EditCommand = EditShiftCommand;
            }

            MonthlyShiftCount = _monthShifts.Count;
            MonthlyTotalHours = _monthShifts.Sum(s => s.TotalHours);
            MonthlyNetSalary = _monthShifts.Sum(s => s.NetShiftSalary);

            BuildCalendarGrid();
            UpdateSelectedDayShifts();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in LoadMonthAsync: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        if (!CanGoPreviousMonth)
        {
            return;
        }

        var prev = new DateTime(CurrentYear, CurrentMonth, 1).AddMonths(-1);
        CurrentYear = ClampAllowedYear(prev.Year);
        CurrentMonth = prev.Month;
        SelectedDate = null;
        IsDetailPanelVisible = false;
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        if (!CanGoNextMonth)
        {
            return;
        }

        var next = new DateTime(CurrentYear, CurrentMonth, 1).AddMonths(1);
        CurrentYear = ClampAllowedYear(next.Year);
        CurrentMonth = next.Month;
        SelectedDate = null;
        IsDetailPanelVisible = false;
        await LoadMonthAsync();
    }

    [RelayCommand]
    private void SelectCalendarDay(CalendarDayCell? cell)
    {
        if (cell is null)
        {
            return;
        }

        if (!cell.HasShifts)
        {
            foreach (var item in CalendarCells)
            {
                item.IsSelected = false;
            }

            SelectedDate = null;
            SelectedDayShifts.Clear();
            IsDetailPanelVisible = false;
            return;
        }

        if (SelectedDate.HasValue && SelectedDate.Value.Date == cell.Date.Date && IsDetailPanelVisible)
        {
            foreach (var item in CalendarCells)
            {
                item.IsSelected = false;
            }

            SelectedDate = null;
            SelectedDayShifts.Clear();
            IsDetailPanelVisible = false;
            return;
        }

        SelectedDate = cell.Date.Date;
        foreach (var item in CalendarCells)
        {
            item.IsSelected = false;
        }

        cell.IsSelected = true;
        UpdateSelectedDayShifts();
    }

    [RelayCommand]
    private void CloseDetailPanel()
    {
        foreach (var item in CalendarCells)
        {
            item.IsSelected = false;
        }

        SelectedDate = null;
        SelectedDayShifts.Clear();
        IsDetailPanelVisible = false;
    }

    [RelayCommand]
    private async Task EditShiftAsync(WorkShift? shift)
    {
        if (shift is null || shift.Id <= 0)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(AddShiftPage)}?shiftId={shift.Id}");
    }

    private void BuildCalendarGrid()
    {
        var firstDayOfMonth = new DateTime(CurrentYear, CurrentMonth, 1);
        var leadingDays = (7 + (firstDayOfMonth.DayOfWeek - DayOfWeek.Monday)) % 7;
        var gridStartDate = firstDayOfMonth.AddDays(-leadingDays);

        var tempCells = new List<CalendarDayCell>(42);
        for (var i = 0; i < 42; i++)
        {
            var cellDate = gridStartDate.AddDays(i).Date;
            var isCurrentMonth = cellDate.Year == CurrentYear && cellDate.Month == CurrentMonth;

            var dayShifts = _monthShifts
                .Where(s => s.ShiftDate.Date == cellDate)
                .OrderBy(s => s.StartTime)
                .ToList();

            var hasShifts = dayShifts.Count > 0;
            var holidayName = _holidayService.GetHolidayName(cellDate);
            var isHoliday = holidayName is not null || (hasShifts && dayShifts.Any(s => s.IsHoliday));

            var isCellSelected = hasShifts &&
                SelectedDate.HasValue &&
                SelectedDate.Value.Date == cellDate &&
                IsDetailPanelVisible;

            var cell = new CalendarDayCell
            {
                RowIndex = i / 7,
                ColumnIndex = i % 7,
                Date = cellDate,
                IsCurrentMonth = isCurrentMonth,
                IsSelected = isCellSelected,
                HasShifts = hasShifts,
                IsHolidayDate = isHoliday,
                HolidayName = holidayName ?? string.Empty,
                DayNetSalary = hasShifts ? dayShifts.Sum(s => s.NetShiftSalary) : 0m,
                DayTotalHours = hasShifts ? dayShifts.Sum(s => s.TotalHours) : 0.0,
                SelectCommand = SelectCalendarDayCommand
            };

            foreach (var shift in dayShifts)
            {
                cell.DayShifts.Add(shift);
            }

            tempCells.Add(cell);
        }

        CalendarCells.Clear();
        foreach (var cell in tempCells)
        {
            CalendarCells.Add(cell);
        }
    }

    private void UpdateSelectedDayShifts()
    {
        if (!SelectedDate.HasValue)
        {
            SelectedDayShifts.Clear();
            IsDetailPanelVisible = false;
            OnPropertyChanged(nameof(HasSelectedDayShifts));
            OnPropertyChanged(nameof(SelectedDayNetSalaryFormatted));
            OnPropertyChanged(nameof(SelectedDayHoursFormatted));
            return;
        }

        var matching = _monthShifts
            .Where(s => s.ShiftDate.Date == SelectedDate.Value.Date)
            .OrderBy(s => s.StartTime)
            .ToList();

        SelectedDayShifts.Clear();
        foreach (var shift in matching)
        {
            SelectedDayShifts.Add(shift);
        }

        IsDetailPanelVisible = SelectedDayShifts.Count > 0;
        if (!IsDetailPanelVisible)
        {
            foreach (var item in CalendarCells)
            {
                item.IsSelected = false;
            }
        }

        OnPropertyChanged(nameof(HasSelectedDayShifts));
        OnPropertyChanged(nameof(SelectedDayNetSalaryFormatted));
        OnPropertyChanged(nameof(SelectedDayHoursFormatted));
    }
}
