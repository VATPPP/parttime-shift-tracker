using System.Globalization;

namespace ShiftTracker.App.Services;

public class HolidayService
{
    private static readonly ChineseLunisolarCalendar LunarCalendar = new();

    private static readonly IReadOnlyDictionary<(int Month, int Day), string> FixedSolarHolidays =
        new Dictionary<(int Month, int Day), string>
        {
            [(1, 1)] = "Tết Dương lịch (01/01)",
            [(4, 30)] = "Ngày Giải phóng miền Nam (30/04)",
            [(5, 1)] = "Ngày Quốc tế Lao động (01/05)",
            [(9, 2)] = "Ngày Quốc khánh (02/09)"
        };

    public IReadOnlyDictionary<(int Month, int Day), string> GetFixedHolidays() =>
        FixedSolarHolidays;

    public bool IsHoliday(DateTime date) =>
        GetHolidayName(date) is not null;

    public string? GetHolidayName(DateTime date)
    {
        var targetDate = date.Date;

        if (FixedSolarHolidays.TryGetValue((targetDate.Month, targetDate.Day), out var solarHolidayName))
        {
            return solarHolidayName;
        }

        if (targetDate >= LunarCalendar.MinSupportedDateTime &&
            targetDate <= LunarCalendar.MaxSupportedDateTime)
        {
            var lunarYear = LunarCalendar.GetYear(targetDate);
            var lunarMonth = LunarCalendar.GetMonth(targetDate);
            var lunarDay = LunarCalendar.GetDayOfMonth(targetDate);
            var leapMonth = LunarCalendar.GetLeapMonth(lunarYear);

            var isLeapMonth = leapMonth > 0 && lunarMonth == leapMonth;
            var actualLunarMonth = leapMonth > 0 && lunarMonth >= leapMonth
                ? lunarMonth - 1
                : lunarMonth;

            if (!isLeapMonth)
            {
                if (actualLunarMonth == 1 && lunarDay is >= 1 and <= 5)
                {
                    return $"Tết Nguyên Đán (Mùng {lunarDay} Tết)";
                }

                if (actualLunarMonth == 3 && lunarDay == 10)
                {
                    return "Giỗ Tổ Hùng Vương (10/03 Âm lịch)";
                }

                var nextDay = targetDate.AddDays(1);
                if (nextDay <= LunarCalendar.MaxSupportedDateTime)
                {
                    var nextLunarYear = LunarCalendar.GetYear(nextDay);
                    var nextLunarMonth = LunarCalendar.GetMonth(nextDay);
                    var nextLunarDay = LunarCalendar.GetDayOfMonth(nextDay);
                    var nextLeapMonth = LunarCalendar.GetLeapMonth(nextLunarYear);
                    var nextActualMonth = nextLeapMonth > 0 && nextLunarMonth >= nextLeapMonth
                        ? nextLunarMonth - 1
                        : nextLunarMonth;

                    if (nextActualMonth == 1 && nextLunarDay == 1)
                    {
                        return "Giao thừa Tết Nguyên Đán";
                    }
                }
            }
        }

        return null;
    }

    public List<(DateTime Date, string Name)> GetHolidaysInMonth(int year, int month)
    {
        var result = new List<(DateTime Date, string Name)>();
        var daysInMonth = DateTime.DaysInMonth(year, month);

        for (var day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day);
            var holidayName = GetHolidayName(currentDate);
            if (holidayName is not null)
            {
                result.Add((currentDate, holidayName));
            }
        }

        return result;
    }
}

