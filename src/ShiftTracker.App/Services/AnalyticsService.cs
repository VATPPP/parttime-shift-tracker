using ShiftTracker.App.Models;

namespace ShiftTracker.App.Services;

public class AnalyticsService
{
    private readonly DatabaseService _databaseService;

    public AnalyticsService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public DashboardSummary CalculateSummary(IEnumerable<WorkShift>? shifts)
    {
        if (shifts is null)
        {
            return new DashboardSummary();
        }

        var shiftList = shifts as IList<WorkShift> ?? shifts.ToList();
        if (shiftList.Count == 0)
        {
            return new DashboardSummary();
        }

        var penalizedLateShifts = shiftList
            .Where(s => s.LateMinutes > WorkShift.FreeLateMinutesThreshold)
            .ToList();

        var unpenalizedLateShifts = shiftList
            .Where(s => s.LateMinutes is >= 1 and <= WorkShift.FreeLateMinutesThreshold)
            .ToList();

        var holidayShifts = shiftList
            .Where(s => s.IsHoliday)
            .ToList();

        var otherPenaltyNotes = shiftList
            .Where(s => s.HasOtherPenalty && (s.EffectiveOtherPenaltyAmount > 0m || !string.IsNullOrWhiteSpace(s.OtherPenaltyNote)))
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .Select(s =>
            {
                var reason = string.IsNullOrWhiteSpace(s.OtherPenaltyNote)
                    ? "Phạt phát sinh"
                    : s.OtherPenaltyNote.Trim();
                return $"{s.ShiftDate:dd/MM/yyyy} ({s.TimeRangeFormatted}): {reason} (-{WorkShift.FormatCurrencyK(s.EffectiveOtherPenaltyAmount)})";
            })
            .ToList();

        return new DashboardSummary
        {
            TotalShiftRecords = shiftList.Count,
            TotalWorkMinutes = shiftList.Sum(s => s.TotalMinutes),
            TotalRegularMinutes = shiftList.Sum(s => s.RegularMinutes),
            TotalRegularWage = shiftList.Sum(s => s.RegularWage),
            TotalOtMinutes = shiftList.Sum(s => s.OtMinutes),
            TotalOtWage = shiftList.Sum(s => s.OtWage),
            TotalCoverHours = shiftList.Sum(s => s.CoverHours),
            PenalizedLateShiftCount = penalizedLateShifts.Count,
            PenalizedLateMinutes = penalizedLateShifts.Sum(s => s.LateMinutes),
            TotalLatePenaltyAmount = shiftList.Sum(s => s.LatePenaltyAmount),
            UnpenalizedLateShiftCount = unpenalizedLateShifts.Count,
            UnpenalizedLateMinutes = unpenalizedLateShifts.Sum(s => s.LateMinutes),
            TotalOtherPenaltyAmount = shiftList.Sum(s => s.EffectiveOtherPenaltyAmount),
            OtherPenaltyNotes = otherPenaltyNotes,
            HolidayShiftCount = holidayShifts.Count,
            TotalHolidayMinutes = holidayShifts.Sum(s => s.TotalMinutes),
            TotalHolidayWage = holidayShifts.Sum(s => s.GrossShiftSalary),
            TotalHolidayNetSalary = holidayShifts.Sum(s => s.NetShiftSalary),
            TotalNetSalary = shiftList.Sum(s => s.NetShiftSalary)
        };
    }

    public async Task<DashboardSummary> GetSummaryByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var shifts = await _databaseService.GetShiftsByDateRangeAsync(startDate, endDate);
        return CalculateSummary(shifts);
    }

    public async Task<DashboardSummary> GetWeeklySummaryAsync(DateTime dateInWeek)
    {
        var (weekStart, weekEnd) = GetWeekRange(dateInWeek);
        return await GetSummaryByDateRangeAsync(weekStart, weekEnd);
    }

    public async Task<DashboardSummary> GetMonthlySummaryAsync(int year, int month)
    {
        var shifts = await _databaseService.GetShiftsByMonthAsync(year, month);
        return CalculateSummary(shifts);
    }

    public static (DateTime WeekStart, DateTime WeekEnd) GetWeekRange(DateTime date)
    {
        var target = date.Date;
        var diff = (7 + (target.DayOfWeek - DayOfWeek.Monday)) % 7;
        var weekStart = target.AddDays(-diff);
        var weekEnd = weekStart.AddDays(6);
        return (weekStart, weekEnd);
    }
}
