using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftTracker.App.Models;
using ShiftTracker.App.Services;
using ShiftTracker.App.Views;

namespace ShiftTracker.App.ViewModels;

public partial class MainShiftViewModel : PageChromeViewModelBase
{
    private readonly DatabaseService _databaseService;
    private List<WorkShift> _allWeekShifts = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentMonthYearTitle))]
    [NotifyPropertyChangedFor(nameof(CurrentWeekRangeText))]
    [NotifyPropertyChangedFor(nameof(CanGoPreviousWeek))]
    [NotifyPropertyChangedFor(nameof(CanGoNextWeek))]
    private DateTime _weekStartDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentMonthYearTitle))]
    [NotifyPropertyChangedFor(nameof(CurrentWeekRangeText))]
    [NotifyPropertyChangedFor(nameof(CanGoPreviousWeek))]
    [NotifyPropertyChangedFor(nameof(CanGoNextWeek))]
    private DateTime _weekEndDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterStatusText))]
    [NotifyPropertyChangedFor(nameof(IsDayFilterActive))]
    private DateTime? _selectedDate;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeeklySummarySubtitle))]
    [NotifyPropertyChangedFor(nameof(WeeklyTotalNetSalaryFormatted))]
    private decimal _weeklyTotalNetSalary;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeeklySummarySubtitle))]
    private double _weeklyTotalHours;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeeklySummarySubtitle))]
    private int _weeklyShiftCount;

    public ObservableCollection<WorkShift> Shifts { get; } = [];

    public ObservableCollection<WeekDayItem> WeekDays { get; } = [];

    public bool IsAnyDayExpanded => WeekDays.Any(d => d.ShowExpandedDetails);

    public ScrollOrientation BodyScrollOrientation =>
        IsAnyDayExpanded
            ? ScrollOrientation.Vertical
            : ScrollOrientation.Neither;

    public bool CanGoPreviousWeek =>
        WeekStartDate.AddDays(-7).Year >= MinAllowedYear;

    public bool CanGoNextWeek =>
        WeekStartDate.AddDays(7).Year <= MaxAllowedYear;

    public string CurrentMonthYearTitle =>
        WeekStartDate.Month == WeekEndDate.Month
            ? $"Tháng {WeekStartDate:MM/yyyy}"
            : $"Tháng {WeekStartDate:MM} - {WeekEndDate:MM/yyyy}";

    public string CurrentWeekRangeText =>
        $"Tuần {WeekStartDate:dd/MM} - {WeekEndDate:dd/MM/yyyy}";

    public string WeeklySummarySubtitle =>
        $"Tổng tuần: {WeeklyShiftCount} ca • {WeeklyTotalHours:0.#}h";

    public string WeeklyTotalNetSalaryFormatted =>
        WorkShift.FormatCurrencyK(WeeklyTotalNetSalary);

    public bool IsDayFilterActive => SelectedDate.HasValue;

    public string FilterStatusText =>
        SelectedDate.HasValue
            ? $"Đang chọn: {WorkShift.GetVietnameseDayOfWeek(SelectedDate.Value.DayOfWeek)}, {SelectedDate.Value:dd/MM/yyyy}"
            : "Tổng quan 7 ngày trong tuần";

    public MainShiftViewModel(DatabaseService databaseService, ThemeService themeService)
        : base(themeService)
    {
        _databaseService = databaseService;

        var (weekStart, weekEnd) = AnalyticsService.GetWeekRange(DateTime.Today);
        WeekStartDate = weekStart;
        WeekEndDate = weekEnd;
        SelectedDate = DateTime.Today;

        BuildWeekDays();
    }

    [RelayCommand]
    private void OpenQuickJumpPopup()
    {
        var refDate = SelectedDate ?? WeekStartDate;
        OpenQuickJumpForDate(refDate.Month, refDate.Year);
    }

    [RelayCommand]
    private async Task JumpToTodayAsync()
    {
        var today = DateTime.Today;
        var (weekStart, weekEnd) = AnalyticsService.GetWeekRange(today);
        WeekStartDate = weekStart;
        WeekEndDate = weekEnd;
        SelectedDate = today;
        IsQuickJumpPopupOpen = false;
        await LoadShiftsAsync();
    }

    [RelayCommand]
    private async Task ConfirmQuickJumpAsync()
    {
        var clampedYear = ClampAllowedYear(PickerYear);
        var clampedMonth = Math.Clamp(PickerMonth, 1, 12);
        var targetDate = new DateTime(clampedYear, clampedMonth, 1);
        if (clampedYear == DateTime.Today.Year && clampedMonth == DateTime.Today.Month)
        {
            targetDate = DateTime.Today;
        }

        var (weekStart, weekEnd) = AnalyticsService.GetWeekRange(targetDate);
        WeekStartDate = weekStart;
        WeekEndDate = weekEnd;
        SelectedDate = targetDate;
        IsQuickJumpPopupOpen = false;
        await LoadShiftsAsync();
    }

    [RelayCommand]
    private async Task LoadShiftsAsync()
    {
        if (IsBusy)
        {
            IsRefreshing = false;
            return;
        }

        try
        {
            IsBusy = true;
            _allWeekShifts = await _databaseService.GetShiftsByDateRangeAsync(WeekStartDate, WeekEndDate);

            foreach (var shift in _allWeekShifts)
            {
                shift.EditCommand = EditShiftCommand;
            }

            UpdateWeekSummary();
            BuildWeekDays();
            ApplyShiftFilter();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in LoadShiftsAsync: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task NextWeekAsync()
    {
        var nextStart = WeekStartDate.AddDays(7);
        if (nextStart.Year > MaxAllowedYear)
        {
            return;
        }

        WeekStartDate = nextStart;
        WeekEndDate = WeekEndDate.AddDays(7);
        SelectedDate = WeekStartDate;
        await LoadShiftsAsync();
    }

    [RelayCommand]
    private async Task PreviousWeekAsync()
    {
        var prevStart = WeekStartDate.AddDays(-7);
        if (prevStart.Year < MinAllowedYear)
        {
            return;
        }

        WeekStartDate = prevStart;
        WeekEndDate = WeekEndDate.AddDays(-7);
        SelectedDate = WeekStartDate;
        await LoadShiftsAsync();
    }

    [RelayCommand]
    private void SelectDay(WeekDayItem? selectedDay)
    {
        if (selectedDay is null)
        {
            return;
        }

        SelectedDate = selectedDay.Date.Date;

        foreach (var day in WeekDays)
        {
            day.IsSelected = false;
        }

        selectedDay.IsSelected = true;

        ApplyShiftFilter();
    }

    [RelayCommand]
    private void ToggleDayExpand(WeekDayItem? dayItem)
    {
        if (dayItem is null)
        {
            return;
        }

        SelectDay(dayItem);

        if (!dayItem.HasExpandableDetails)
        {
            dayItem.IsExpanded = false;
            OnPropertyChanged(nameof(IsAnyDayExpanded));
            OnPropertyChanged(nameof(BodyScrollOrientation));
            return;
        }

        dayItem.IsExpanded = !dayItem.IsExpanded;
        OnPropertyChanged(nameof(IsAnyDayExpanded));
        OnPropertyChanged(nameof(BodyScrollOrientation));
    }

    [RelayCommand]
    private async Task OpenDayDetailOrEditAsync(WeekDayItem? dayItem)
    {
        if (dayItem is null)
        {
            return;
        }

        SelectDay(dayItem);

        if (dayItem.HasShifts && dayItem.DayShifts.Count > 0)
        {
            var targetShift = dayItem.DayShifts[0];
            await Shell.Current.GoToAsync($"{nameof(AddShiftPage)}?shiftId={targetShift.Id}");
            return;
        }

        var dateParam = dayItem.Date.ToString("yyyy-MM-dd");
        await Shell.Current.GoToAsync($"{nameof(AddShiftPage)}?initialDate={dateParam}");
    }

    [RelayCommand]
    private void ClearDayFilter()
    {
        SelectedDate = null;
        foreach (var day in WeekDays)
        {
            day.IsSelected = false;
        }

        ApplyShiftFilter();
    }

    [RelayCommand]
    private async Task AddShiftAsync()
    {
        if (SelectedDate.HasValue)
        {
            var dateParam = SelectedDate.Value.ToString("yyyy-MM-dd");
            await Shell.Current.GoToAsync($"{nameof(AddShiftPage)}?initialDate={dateParam}");
            return;
        }

        await Shell.Current.GoToAsync(nameof(AddShiftPage));
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

    private void UpdateWeekSummary()
    {
        WeeklyShiftCount = _allWeekShifts.Count;
        WeeklyTotalHours = _allWeekShifts.Sum(s => s.TotalHours);
        WeeklyTotalNetSalary = _allWeekShifts.Sum(s => s.NetShiftSalary);
    }

    private void ApplyShiftFilter()
    {
        var filtered = SelectedDate.HasValue
            ? _allWeekShifts.Where(s => s.ShiftDate.Date == SelectedDate.Value.Date)
            : _allWeekShifts;

        Shifts.Clear();
        foreach (var shift in filtered)
        {
            Shifts.Add(shift);
        }
    }

    private void BuildWeekDays()
    {
        var expandedDates = WeekDays
            .Where(d => d.IsExpanded)
            .Select(d => d.Date.Date)
            .ToHashSet();

        var tempItems = new List<WeekDayItem>(7);
        for (var i = 0; i < 7; i++)
        {
            var currentDate = WeekStartDate.AddDays(i).Date;
            var dayShifts = _allWeekShifts
                .Where(s => s.ShiftDate.Date == currentDate)
                .OrderBy(s => s.StartTime)
                .ToList();

            var hasShiftsForDay = dayShifts.Count > 0;
            var timeSummary = hasShiftsForDay
                ? string.Join(", ", dayShifts.Select(s => s.TimeRangeFormatted))
                : string.Empty;

            var notes = dayShifts
                .Where(s => s.HasDisplayNote)
                .Select(s => s.DisplayNote)
                .ToList();

            var dayLateMins = hasShiftsForDay ? dayShifts.Sum(s => s.LateMinutes) : 0;
            var latePenaltyTotal = hasShiftsForDay ? dayShifts.Sum(s => s.LatePenaltyAmount) : 0m;
            var lateDetail = dayLateMins <= 0
                ? string.Empty
                : latePenaltyTotal > 0m
                    ? $"⏱️ Đi trễ: {dayLateMins} phút (-{WorkShift.FormatCurrencyK(latePenaltyTotal)})"
                    : $"⏱️ Đi trễ: {dayLateMins} phút (Miễn phạt ≤ 4p)";

            var hasCover = hasShiftsForDay && dayShifts.Any(s => s.IsCoverShift || s.CoverHours > 0);
            var coverNotes = dayShifts
                .Where(s => (s.IsCoverShift || s.CoverHours > 0) && !string.IsNullOrWhiteSpace(s.ShiftNote))
                .Select(s => s.ShiftNote.Trim())
                .ToList();
            var coverDetail = !hasCover
                ? string.Empty
                : coverNotes.Count > 0
                    ? $"🤝 Làm dùm: {string.Join(" • ", coverNotes)}"
                    : "🤝 Làm ca thay người khác (Làm dùm)";

            var penaltyShifts = dayShifts
                .Where(s => s.HasOtherPenalty && (s.EffectiveOtherPenaltyAmount > 0m || !string.IsNullOrWhiteSpace(s.OtherPenaltyNote)))
                .ToList();
            var hasPenalty = hasShiftsForDay && penaltyShifts.Count > 0;
            var penaltyDetails = penaltyShifts
                .Select(s =>
                {
                    var reason = string.IsNullOrWhiteSpace(s.OtherPenaltyNote) ? "Phạt phát sinh" : s.OtherPenaltyNote.Trim();
                    return s.EffectiveOtherPenaltyAmount > 0m
                        ? $"⚠️ Phạt: -{WorkShift.FormatCurrencyK(s.EffectiveOtherPenaltyAmount)} ({reason})"
                        : $"⚠️ Phạt: {reason}";
                })
                .ToList();

            var isDaySelected = SelectedDate.HasValue && SelectedDate.Value.Date == currentDate;

            var item = new WeekDayItem
            {
                ColumnIndex = i,
                Date = currentDate,
                DayOfWeekShort = GetShortDayOfWeek(currentDate.DayOfWeek),
                DayNameFull = WorkShift.GetVietnameseDayOfWeek(currentDate.DayOfWeek),
                IsSelected = isDaySelected,
                HasShifts = hasShiftsForDay,
                ShiftCount = dayShifts.Count,
                DayTotalHours = hasShiftsForDay ? dayShifts.Sum(s => s.TotalHours) : 0.0,
                HasCoverShift = hasCover,
                DayNetSalary = hasShiftsForDay ? dayShifts.Sum(s => s.NetShiftSalary) : 0m,
                DaySummaryNote = string.Join(" | ", notes),
                DayShiftsTimeSummary = timeSummary,
                HasHolidayShift = hasShiftsForDay && dayShifts.Any(s => s.IsHoliday),
                DayOtMinutes = hasShiftsForDay ? (int)Math.Round(dayShifts.Sum(s => s.OtMinutes)) : 0,
                DayLateMinutes = dayLateMins,
                HasOtherPenalty = hasPenalty,
                LateDetailText = lateDetail,
                CoverDetailText = coverDetail,
                OtherPenaltyDetailText = string.Join(" • ", penaltyDetails),
                SelectCommand = SelectDayCommand,
                ToggleExpandCommand = ToggleDayExpandCommand,
                EditCommand = OpenDayDetailOrEditCommand
            };

            foreach (var shift in dayShifts)
            {
                item.DayShifts.Add(shift);
            }

            var coworkers = dayShifts
                .SelectMany(s => s.CoworkerList)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var coworker in coworkers)
            {
                item.CoworkerTags.Add(coworker);
            }

            item.IsExpanded = item.HasExpandableDetails && expandedDates.Contains(currentDate);

            tempItems.Add(item);
        }

        WeekDays.Clear();
        foreach (var item in tempItems)
        {
            WeekDays.Add(item);
        }

        OnPropertyChanged(nameof(IsAnyDayExpanded));
        OnPropertyChanged(nameof(BodyScrollOrientation));
    }

    private static string GetShortDayOfWeek(DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Monday => "T2",
            DayOfWeek.Tuesday => "T3",
            DayOfWeek.Wednesday => "T4",
            DayOfWeek.Thursday => "T5",
            DayOfWeek.Friday => "T6",
            DayOfWeek.Saturday => "T7",
            DayOfWeek.Sunday => "CN",
            _ => string.Empty
        };
}
