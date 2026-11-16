namespace ShiftTracker.App.Models;

public class DashboardSummary
{
    public int TotalShiftRecords { get; set; }

    public double TotalWorkMinutes { get; set; }

    public double TotalWorkHours => TotalWorkMinutes / 60.0;

    public double StandardShiftCount => TotalWorkHours / WorkShift.StandardShiftHours;

    public double TotalRegularMinutes { get; set; }

    public double TotalRegularHours => TotalRegularMinutes / 60.0;

    public decimal TotalRegularWage { get; set; }

    public double TotalOtMinutes { get; set; }

    public double TotalOtHours => TotalOtMinutes / 60.0;

    public int OtDisplayHours => (int)(Math.Round(TotalOtMinutes, MidpointRounding.AwayFromZero) / 60);

    public int OtDisplayRemainingMinutes => (int)(Math.Round(TotalOtMinutes, MidpointRounding.AwayFromZero) % 60);

    public string FormattedOtDuration => $"{OtDisplayHours} giờ {OtDisplayRemainingMinutes} phút";

    public decimal TotalOtWage { get; set; }

    public double TotalCoverHours { get; set; }

    public int PenalizedLateShiftCount { get; set; }

    public int PenalizedLateMinutes { get; set; }

    public decimal TotalLatePenaltyAmount { get; set; }

    public int UnpenalizedLateShiftCount { get; set; }

    public int UnpenalizedLateMinutes { get; set; }

    public decimal TotalOtherPenaltyAmount { get; set; }

    public List<string> OtherPenaltyNotes { get; set; } = [];

    public int HolidayShiftCount { get; set; }

    public double TotalHolidayMinutes { get; set; }

    public double TotalHolidayHours => TotalHolidayMinutes / 60.0;

    public decimal TotalHolidayWage { get; set; }

    public decimal TotalHolidayNetSalary { get; set; }

    public decimal TotalGrossSalary => TotalRegularWage + TotalOtWage;

    public decimal TotalPenaltyAmount => TotalLatePenaltyAmount + TotalOtherPenaltyAmount;

    public decimal TotalNetSalary { get; set; }
}

