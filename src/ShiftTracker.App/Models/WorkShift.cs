using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using SQLite;

namespace ShiftTracker.App.Models;

[Table("work_shifts")]
public class WorkShift
{
    public const decimal DefaultHourlyRate = 22_000m;
    public const double StandardShiftHours = 7.0;
    public const int FreeLateMinutesThreshold = 4;
    public const decimal LatePenaltyPerMinute = 1_000m;

    public static readonly TimeSpan FirstPresetStart = new(8, 0, 0);
    public static readonly TimeSpan FirstPresetEnd = new(15, 0, 0);
    public static readonly TimeSpan SecondPresetStart = new(15, 0, 0);
    public static readonly TimeSpan SecondPresetEnd = new(22, 0, 0);
    public static readonly TimeSpan OtThresholdTime = new(22, 0, 0);

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime ShiftDate { get; set; } = DateTime.Today;

    [Ignore]
    public DateTime WorkDate
    {
        get => ShiftDate;
        set => ShiftDate = value.Date;
    }

    [Ignore]
    public IAsyncRelayCommand<WorkShift>? EditCommand { get; set; }

    public string ShiftType { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; } = FirstPresetStart;

    public TimeSpan EndTime { get; set; } = FirstPresetEnd;

    public string TimeSlotsText { get; set; } = string.Empty;

    public string CoworkersText { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; } = DefaultHourlyRate;

    public bool IsHoliday { get; set; }

    public bool IsCoverShift { get; set; }

    public double CoverHours { get; set; }

    public string ShiftNote { get; set; } = string.Empty;

    public bool HasLate { get; set; }

    public int LateMinutes { get; set; }

    public bool HasOtherPenalty { get; set; }

    public decimal OtherPenaltyAmount { get; set; }

    public string OtherPenaltyNote { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    private List<(TimeSpan Start, TimeSpan End)> ParsedTimeSlots =>
        ShiftTimeSlot.ParseSlots(TimeSlotsText, StartTime, EndTime);

    [Ignore]
    public double TotalMinutes =>
        ParsedTimeSlots.Sum(slot =>
        {
            var effectiveEnd = slot.End < slot.Start
                ? slot.End.Add(TimeSpan.FromDays(1))
                : slot.End;
            return Math.Max(0.0, (effectiveEnd - slot.Start).TotalMinutes);
        });

    [Ignore]
    public double TotalHours => TotalMinutes / 60.0;

    [Ignore]
    public double OtMinutes =>
        ParsedTimeSlots.Sum(slot =>
        {
            var effectiveEnd = slot.End < slot.Start
                ? slot.End.Add(TimeSpan.FromDays(1))
                : slot.End;
            if (effectiveEnd <= OtThresholdTime)
            {
                return 0.0;
            }

            var otStart = slot.Start > OtThresholdTime ? slot.Start : OtThresholdTime;
            return Math.Max(0.0, (effectiveEnd - otStart).TotalMinutes);
        });

    [Ignore]
    public double OtHours => OtMinutes / 60.0;

    [Ignore]
    public double RegularMinutes => Math.Max(0.0, TotalMinutes - OtMinutes);

    [Ignore]
    public double RegularHours => RegularMinutes / 60.0;

    [Ignore]
    public decimal RegularMultiplier => IsHoliday ? 2m : 1m;

    [Ignore]
    public decimal OtMultiplier => IsHoliday ? 4m : 2m;

    [Ignore]
    public decimal RegularWage =>
        ((decimal)RegularMinutes * HourlyRate * RegularMultiplier) / 60m;

    [Ignore]
    public decimal OtWage =>
        ((decimal)OtMinutes * HourlyRate * OtMultiplier) / 60m;

    [Ignore]
    public decimal GrossShiftSalary => RegularWage + OtWage;

    [Ignore]
    public bool IsLatePenalized => LateMinutes > FreeLateMinutesThreshold;

    [Ignore]
    public decimal LatePenaltyAmount =>
        LateMinutes > FreeLateMinutesThreshold
            ? LateMinutes * LatePenaltyPerMinute
            : 0m;

    [Ignore]
    public decimal EffectiveOtherPenaltyAmount =>
        HasOtherPenalty && OtherPenaltyAmount > 0m
            ? OtherPenaltyAmount
            : 0m;

    [Ignore]
    public decimal NetShiftSalary =>
        RegularWage + OtWage - LatePenaltyAmount - EffectiveOtherPenaltyAmount;

    [Ignore]
    public string TimeRangeFormatted =>
        ShiftTimeSlot.SerializeSlots(ParsedTimeSlots);

    [Ignore]
    public List<string> CoworkerList =>
        string.IsNullOrWhiteSpace(CoworkersText)
            ? []
            : CoworkersText
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    [Ignore]
    public bool HasCoworkers => CoworkerList.Count > 0;

    [Ignore]
    public string ShiftDateFormatted =>
        $"{GetVietnameseDayOfWeek(ShiftDate.DayOfWeek)}, {ShiftDate:dd/MM/yyyy}";

    [Ignore]
    public string DurationFormatted
    {
        get
        {
            var totalMins = (int)Math.Round(TotalMinutes);
            var hours = totalMins / 60;
            var mins = totalMins % 60;
            return mins > 0 ? $"{hours}h {mins}p" : $"{hours}h";
        }
    }

    [Ignore]
    public string NetShiftSalaryFormatted =>
        FormatCurrencyK(NetShiftSalary);

    [Ignore]
    public bool HasLateMinutes => HasLate || LateMinutes > 0;

    [Ignore]
    public string LateStatusText =>
        LateMinutes <= 0
            ? string.Empty
            : IsLatePenalized
                ? $"⏱️ Đi trễ {LateMinutes} phút (-{FormatCurrencyK(LatePenaltyAmount)})"
                : $"⏱️ Đi trễ {LateMinutes} phút (Miễn phạt ≤ 4p)";

    [Ignore]
    public bool HasCoverHours => IsCoverShift || CoverHours > 0;

    [Ignore]
    public string CoverHoursFormatted =>
        HasCoverHours ? "Làm dùm" : string.Empty;

    [Ignore]
    public bool HasOt => OtMinutes > 0;

    [Ignore]
    public string OtFormatted =>
        OtMinutes > 0 ? $"⚡ OT: {(int)Math.Round(OtMinutes)}p (+{FormatCurrencyK(OtWage)})" : string.Empty;

    [Ignore]
    public bool HasDisplayNote =>
        !string.IsNullOrWhiteSpace(DisplayNote);

    [Ignore]
    public string DisplayNote
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(ShiftNote))
            {
                parts.Add(ShiftNote.Trim());
            }

            if (HasOtherPenalty && (EffectiveOtherPenaltyAmount > 0m || !string.IsNullOrWhiteSpace(OtherPenaltyNote)))
            {
                var penaltyReason = string.IsNullOrWhiteSpace(OtherPenaltyNote)
                    ? "Phạt phát sinh"
                    : OtherPenaltyNote.Trim();
                parts.Add($"Phạt: {penaltyReason} (-{FormatCurrencyK(EffectiveOtherPenaltyAmount)})");
            }

            return string.Join(" • ", parts);
        }
    }

    public static string FormatCurrencyK(decimal amount)
    {
        var kValue = Math.Round(amount / 1_000m, 1, MidpointRounding.AwayFromZero);
        return $"{kValue.ToString("0.#", CultureInfo.InvariantCulture)}k";
    }

    public static string GetVietnameseDayOfWeek(DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => string.Empty
        };
}
