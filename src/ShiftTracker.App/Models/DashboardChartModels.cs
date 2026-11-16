namespace ShiftTracker.App.Models;

public class WeeklyBarChartItem
{
    public int WeekNumber { get; init; }

    public string WeekLabel => $"Tuần {WeekNumber}";

    public string DateRangeLabel { get; init; } = string.Empty;

    public decimal NetSalary { get; init; }

    public double TotalHours { get; init; }

    public int ShiftCount { get; init; }

    public double BarHeight { get; init; }

    public bool HasIncome => NetSalary > 0m;

    public string NetSalaryFormatted =>
        NetSalary > 0m ? WorkShift.FormatCurrencyK(NetSalary) : "0k";
}

public class TopCoworkerStatItem
{
    public int Rank { get; init; }

    public string Name { get; init; } = string.Empty;

    public int ShiftCount { get; init; }

    public string DisplayBadge => $"#{Rank} {Name} ({ShiftCount} ca)";
}

