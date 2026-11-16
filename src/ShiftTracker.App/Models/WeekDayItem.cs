using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShiftTracker.App.Models;

public partial class WeekDayItem : ObservableObject
{
    public int ColumnIndex { get; init; }

    public DateTime Date { get; init; }

    public string DayOfWeekShort { get; init; } = string.Empty;

    public string DayNameFull { get; init; } = string.Empty;

    public string DayNumberText => Date.ToString("dd");

    public string FullDateText => $"{DayNameFull}, {Date:dd/MM/yyyy}";

    public string DateMonthText => Date.ToString("dd/MM");

    public bool IsToday => Date.Date == DateTime.Today;

    public IRelayCommand<WeekDayItem>? SelectCommand { get; set; }

    public IRelayCommand<WeekDayItem>? ToggleExpandCommand { get; set; }

    public IAsyncRelayCommand<WeekDayItem>? EditCommand { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExpandedDetails))]
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRestDay))]
    [NotifyPropertyChangedFor(nameof(StatusIcon))]
    [NotifyPropertyChangedFor(nameof(DayNetSalaryFormatted))]
    [NotifyPropertyChangedFor(nameof(DayTotalHoursFormatted))]
    private bool _hasShifts;

    [ObservableProperty]
    private int _shiftCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayTotalHoursFormatted))]
    private double _dayTotalHours;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDayCover))]
    [NotifyPropertyChangedFor(nameof(DayCoverFormatted))]
    [NotifyPropertyChangedFor(nameof(HasExpandableDetails))]
    [NotifyPropertyChangedFor(nameof(ShowExpandedDetails))]
    private bool _hasCoverShift;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayNetSalaryFormatted))]
    private decimal _dayNetSalary;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDayNote))]
    private string _daySummaryNote = string.Empty;

    [ObservableProperty]
    private string _dayShiftsTimeSummary = string.Empty;

    [ObservableProperty]
    private bool _hasHolidayShift;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDayOt))]
    [NotifyPropertyChangedFor(nameof(DayOtFormatted))]
    private int _dayOtMinutes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDayLate))]
    [NotifyPropertyChangedFor(nameof(DayLateFormatted))]
    [NotifyPropertyChangedFor(nameof(HasExpandableDetails))]
    [NotifyPropertyChangedFor(nameof(ShowExpandedDetails))]
    private int _dayLateMinutes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExpandableDetails))]
    [NotifyPropertyChangedFor(nameof(ShowExpandedDetails))]
    private bool _hasOtherPenalty;

    [ObservableProperty]
    private string _lateDetailText = string.Empty;

    [ObservableProperty]
    private string _coverDetailText = string.Empty;

    [ObservableProperty]
    private string _otherPenaltyDetailText = string.Empty;

    public ObservableCollection<WorkShift> DayShifts { get; } = [];

    public ObservableCollection<string> CoworkerTags { get; } = [];

    public bool HasCoworkerTags => CoworkerTags.Count > 0;

    public bool IsRestDay => !HasShifts;

    public string StatusIcon => HasShifts ? "💎" : "☕";

    public bool HasDayNote => !string.IsNullOrWhiteSpace(DaySummaryNote);

    public bool HasDayCover => HasCoverShift;

    public bool HasDayOt => DayOtMinutes > 0;

    public bool HasDayLate => DayLateMinutes > 0;

    public bool HasExpandableDetails =>
        HasShifts && (DayLateMinutes > 0 || HasCoverShift || HasOtherPenalty);

    public bool ShowExpandedDetails =>
        HasExpandableDetails && IsExpanded;

    public string DayTotalHoursFormatted
    {
        get
        {
            if (!HasShifts)
            {
                return "0h";
            }

            var totalMins = (int)Math.Round(DayTotalHours * 60);
            var h = totalMins / 60;
            var m = totalMins % 60;
            return m > 0 ? $"{h}h {m}p" : $"{h}h";
        }
    }

    public string DayCoverFormatted =>
        HasCoverShift ? "Làm dùm" : string.Empty;

    public string DayOtFormatted =>
        DayOtMinutes > 0 ? $"⚡ OT {DayOtMinutes}p" : string.Empty;

    public string DayLateFormatted =>
        DayLateMinutes > 0 ? $"⏱️ Trễ {DayLateMinutes}p" : string.Empty;

    public string DayNetSalaryFormatted =>
        HasShifts ? WorkShift.FormatCurrencyK(DayNetSalary) : "Nghỉ";
}
