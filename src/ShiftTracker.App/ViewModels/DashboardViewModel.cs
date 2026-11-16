using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftTracker.App.Models;
using ShiftTracker.App.Services;

namespace ShiftTracker.App.ViewModels;

public partial class DashboardViewModel : PageChromeViewModelBase
{
    private const double MaxChartBarHeight = 112.0;
    private const double MinNonZeroBarHeight = 14.0;

    private readonly DatabaseService _databaseService;
    private readonly AnalyticsService _analyticsService;
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
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetSalaryFormatted))]
    [NotifyPropertyChangedFor(nameof(RegularWageFormatted))]
    [NotifyPropertyChangedFor(nameof(OtWageFormatted))]
    [NotifyPropertyChangedFor(nameof(TotalPenaltyFormatted))]
    [NotifyPropertyChangedFor(nameof(TotalHoursFormatted))]
    [NotifyPropertyChangedFor(nameof(TotalShiftCountText))]
    [NotifyPropertyChangedFor(nameof(HolidayShiftSummaryText))]
    [NotifyPropertyChangedFor(nameof(RegularHoursRatio))]
    [NotifyPropertyChangedFor(nameof(OtHoursRatio))]
    [NotifyPropertyChangedFor(nameof(RegularPercentText))]
    [NotifyPropertyChangedFor(nameof(OtPercentText))]
    [NotifyPropertyChangedFor(nameof(RegularDurationText))]
    [NotifyPropertyChangedFor(nameof(OtDurationText))]
    private DashboardSummary _summary = new();

    public ObservableCollection<WeeklyBarChartItem> WeeklyEarnings { get; } = [];

    public ObservableCollection<TopCoworkerStatItem> TopCoworkers { get; } = [];

    public bool HasTopCoworkers => TopCoworkers.Count > 0;

    public bool HasNoTopCoworkers => TopCoworkers.Count == 0;

    public bool CanGoPreviousMonth =>
        CurrentYear > MinAllowedYear || (CurrentYear == MinAllowedYear && CurrentMonth > 1);

    public bool CanGoNextMonth =>
        CurrentYear < MaxAllowedYear || (CurrentYear == MaxAllowedYear && CurrentMonth < 12);

    public string CurrentMonthYearTitle =>
        $"Tháng {CurrentMonth:00}/{CurrentYear}";

    public string NetSalaryFormatted =>
        FormatCurrencyCommaK(Summary.TotalNetSalary);

    public string RegularWageFormatted =>
        FormatCurrencyCommaK(Summary.TotalRegularWage);

    public string OtWageFormatted =>
        Summary.TotalOtWage > 0m
            ? $"+{FormatCurrencyCommaK(Summary.TotalOtWage)}"
            : "0k";

    public string TotalPenaltyFormatted =>
        Summary.TotalPenaltyAmount > 0m
            ? $"-{FormatCurrencyCommaK(Summary.TotalPenaltyAmount)}"
            : "0k";

    public string TotalHoursFormatted =>
        $"{Summary.TotalWorkHours:0.#} giờ";

    public string TotalShiftCountText =>
        $"{Summary.TotalShiftRecords} ca";

    public string HolidayShiftSummaryText =>
        Summary.HolidayShiftCount > 0
            ? $"{Summary.HolidayShiftCount} ca lễ ({FormatCurrencyCommaK(Summary.TotalHolidayNetSalary)})"
            : "0 ca lễ";

    public double RegularHoursRatio
    {
        get
        {
            if (Summary.TotalWorkMinutes <= 0)
            {
                return 1.0;
            }

            return Math.Clamp(Summary.TotalRegularMinutes / Summary.TotalWorkMinutes, 0.0, 1.0);
        }
    }

    public double OtHoursRatio
    {
        get
        {
            if (Summary.TotalWorkMinutes <= 0)
            {
                return 0.0;
            }

            return Math.Clamp(Summary.TotalOtMinutes / Summary.TotalWorkMinutes, 0.0, 1.0);
        }
    }

    public string RegularPercentText
    {
        get
        {
            if (Summary.TotalWorkMinutes <= 0)
            {
                return "0%";
            }

            var pct = Math.Round((Summary.TotalRegularMinutes / Summary.TotalWorkMinutes) * 100.0, 1);
            return $"{pct:0.#}%";
        }
    }

    public string OtPercentText
    {
        get
        {
            if (Summary.TotalWorkMinutes <= 0)
            {
                return "0%";
            }

            var pct = Math.Round((Summary.TotalOtMinutes / Summary.TotalWorkMinutes) * 100.0, 1);
            return $"{pct:0.#}%";
        }
    }

    public string RegularDurationText =>
        $"{Summary.TotalRegularHours:0.#}h ({RegularPercentText})";

    public string OtDurationText =>
        $"{Summary.TotalOtHours:0.#}h ({OtPercentText})";

    public DashboardViewModel(
        DatabaseService databaseService,
        AnalyticsService analyticsService,
        ThemeService themeService)
        : base(themeService)
    {
        _databaseService = databaseService;
        _analyticsService = analyticsService;

        BuildWeeklyChart();
    }

    [RelayCommand]
    private async Task LoadSummaryAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            _monthShifts = await _databaseService.GetShiftsByMonthAsync(CurrentYear, CurrentMonth);
            Summary = _analyticsService.CalculateSummary(_monthShifts);

            BuildWeeklyChart();
            BuildTopCoworkers();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in LoadSummaryAsync: {ex.Message}");
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
        await LoadSummaryAsync();
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
        await LoadSummaryAsync();
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
        IsQuickJumpPopupOpen = false;
        await LoadSummaryAsync();
    }

    [RelayCommand]
    private async Task ConfirmQuickJumpAsync()
    {
        CurrentYear = ClampAllowedYear(PickerYear);
        CurrentMonth = Math.Clamp(PickerMonth, 1, 12);
        IsQuickJumpPopupOpen = false;
        await LoadSummaryAsync();
    }

    private void BuildWeeklyChart()
    {
        var daysInMonth = DateTime.DaysInMonth(CurrentYear, CurrentMonth);
        var weekRanges = new List<(int WeekNum, int StartDay, int EndDay)>
        {
            (1, 1, Math.Min(7, daysInMonth)),
            (2, 8, Math.Min(14, daysInMonth)),
            (3, 15, Math.Min(21, daysInMonth)),
            (4, 22, Math.Min(28, daysInMonth))
        };

        if (daysInMonth > 28)
        {
            weekRanges.Add((5, 29, daysInMonth));
        }

        var rawBuckets = new List<(int WeekNum, string RangeLabel, decimal NetSalary, double Hours, int Count)>();
        foreach (var (weekNum, startDay, endDay) in weekRanges)
        {
            var weekShifts = _monthShifts
                .Where(s => s.ShiftDate.Day >= startDay && s.ShiftDate.Day <= endDay)
                .ToList();

            var net = Math.Max(0m, weekShifts.Sum(s => s.NetShiftSalary));
            var hours = weekShifts.Sum(s => s.TotalHours);
            var label = $"{startDay:00}-{endDay:00}/{CurrentMonth:00}";
            rawBuckets.Add((weekNum, label, net, hours, weekShifts.Count));
        }

        var maxSalary = rawBuckets.Count > 0 ? rawBuckets.Max(b => b.NetSalary) : 0m;

        WeeklyEarnings.Clear();
        foreach (var bucket in rawBuckets)
        {
            double barHeight;
            if (bucket.NetSalary <= 0m || maxSalary <= 0m)
            {
                barHeight = 6.0;
            }
            else
            {
                var ratio = (double)(bucket.NetSalary / maxSalary);
                barHeight = Math.Max(MinNonZeroBarHeight, Math.Round(ratio * MaxChartBarHeight, 1));
            }

            WeeklyEarnings.Add(new WeeklyBarChartItem
            {
                WeekNumber = bucket.WeekNum,
                DateRangeLabel = bucket.RangeLabel,
                NetSalary = bucket.NetSalary,
                TotalHours = bucket.Hours,
                ShiftCount = bucket.Count,
                BarHeight = barHeight
            });
        }
    }

    private void BuildTopCoworkers()
    {
        var coworkerCounts = _monthShifts
            .SelectMany(s => s.CoworkerList)
            .GroupBy(name => name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Name = g.First(),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(3)
            .ToList();

        TopCoworkers.Clear();
        for (var i = 0; i < coworkerCounts.Count; i++)
        {
            TopCoworkers.Add(new TopCoworkerStatItem
            {
                Rank = i + 1,
                Name = coworkerCounts[i].Name,
                ShiftCount = coworkerCounts[i].Count
            });
        }

        OnPropertyChanged(nameof(HasTopCoworkers));
        OnPropertyChanged(nameof(HasNoTopCoworkers));
    }

    private static string FormatCurrencyCommaK(decimal amount)
    {
        var kValue = Math.Round(amount / 1_000m, 1, MidpointRounding.AwayFromZero);
        return $"{kValue.ToString("#,0.#", CultureInfo.InvariantCulture)}k";
    }
}
