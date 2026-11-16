using ShiftTracker.App.Models;
using SQLite;

namespace ShiftTracker.App.Services;

public class DatabaseService
{
    public const string DatabaseFileName = "shift_tracker.db3";

    public const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _database;

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    public async Task InitAsync()
    {
        if (_database is not null)
        {
            return;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_database is not null)
            {
                return;
            }

            _database = new SQLiteAsyncConnection(DatabasePath, Flags);
            await _database.CreateTableAsync<WorkShift>();
            await _database.CreateTableAsync<CoworkerTag>();
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<WorkShift>> GetAllShiftsAsync()
    {
        await InitAsync();
        var shifts = await _database!.Table<WorkShift>().ToListAsync();
        return shifts
            .OrderByDescending(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToList();
    }

    public async Task<WorkShift?> GetShiftByIdAsync(int id)
    {
        await InitAsync();
        return await _database!
            .Table<WorkShift>()
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<WorkShift>> GetShiftsByDateAsync(DateTime date)
    {
        var targetDate = date.Date;
        return await GetShiftsByDateRangeAsync(targetDate, targetDate);
    }

    public async Task<List<WorkShift>> GetShiftsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        await InitAsync();

        var fromDate = startDate.Date;
        var toDate = endDate.Date.AddDays(1).AddTicks(-1);

        var shifts = await _database!
            .Table<WorkShift>()
            .Where(s => s.ShiftDate >= fromDate && s.ShiftDate <= toDate)
            .ToListAsync();

        return shifts
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.StartTime)
            .ToList();
    }

    public async Task<List<WorkShift>> GetShiftsByMonthAsync(int year, int month)
    {
        var firstDayOfMonth = new DateTime(year, month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        return await GetShiftsByDateRangeAsync(firstDayOfMonth, lastDayOfMonth);
    }

    public async Task<int> AddShiftAsync(WorkShift shift)
    {
        ArgumentNullException.ThrowIfNull(shift);
        await InitAsync();

        NormalizeShiftBeforeSave(shift);
        shift.CreatedAt = DateTime.Now;
        shift.UpdatedAt = DateTime.Now;

        var result = await _database!.InsertAsync(shift);
        await EnsureCoworkerTagsExistAsync(shift.CoworkerList);
        NotifyWidget();
        return result;
    }

    public async Task<int> UpdateShiftAsync(WorkShift shift)
    {
        ArgumentNullException.ThrowIfNull(shift);
        await InitAsync();

        NormalizeShiftBeforeSave(shift);
        shift.UpdatedAt = DateTime.Now;

        var result = await _database!.UpdateAsync(shift);
        await EnsureCoworkerTagsExistAsync(shift.CoworkerList);
        NotifyWidget();
        return result;
    }

    public async Task<int> SaveShiftAsync(WorkShift shift)
    {
        ArgumentNullException.ThrowIfNull(shift);
        return shift.Id == 0
            ? await AddShiftAsync(shift)
            : await UpdateShiftAsync(shift);
    }

    public async Task<int> DeleteShiftAsync(WorkShift shift)
    {
        ArgumentNullException.ThrowIfNull(shift);
        await InitAsync();
        var deleted = await _database!.DeleteAsync(shift);
        NotifyWidget();
        return deleted;
    }

    public async Task<int> DeleteShiftByIdAsync(int id)
    {
        await InitAsync();
        var deleted = await _database!.DeleteAsync<WorkShift>(id);
        NotifyWidget();
        return deleted;
    }

    public async Task<List<CoworkerTag>> GetAllTagsAsync()
    {
        await InitAsync();
        var tags = await _database!.Table<CoworkerTag>().ToListAsync();
        return tags
            .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<CoworkerTag?> AddTagAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        await InitAsync();
        var trimmed = name.Trim();

        var existingTags = await _database!.Table<CoworkerTag>().ToListAsync();
        var existing = existingTags.FirstOrDefault(t =>
            string.Equals(t.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var newTag = new CoworkerTag
        {
            Name = trimmed,
            CreatedAt = DateTime.Now
        };

        await _database.InsertAsync(newTag);
        return newTag;
    }

    public async Task<int> DeleteTagAsync(int id)
    {
        await InitAsync();
        return await _database!.DeleteAsync<CoworkerTag>(id);
    }

    private async Task EnsureCoworkerTagsExistAsync(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await AddTagAsync(name);
            }
        }
    }

    private static void NotifyWidget()
    {
#if ANDROID
        ShiftTracker.App.Platforms.Android.WidgetUpdateHelper.UpdateWidget(global::Android.App.Application.Context);
#endif
    }

    private static void NormalizeShiftBeforeSave(WorkShift shift)
    {
        shift.ShiftDate = shift.ShiftDate.Date;

        var parsedSlots = ShiftTimeSlot.ParseSlots(shift.TimeSlotsText, shift.StartTime, shift.EndTime);
        shift.StartTime = parsedSlots[0].Start;
        shift.EndTime = parsedSlots[0].End;
        shift.TimeSlotsText = ShiftTimeSlot.SerializeSlots(parsedSlots);
        shift.ShiftType = shift.TimeSlotsText;

        shift.CoworkersText = string.Join(", ", shift.CoworkerList);

        if (!shift.HasLate)
        {
            shift.LateMinutes = 0;
        }
        else
        {
            shift.LateMinutes = Math.Max(0, shift.LateMinutes);
        }

        if (!shift.IsCoverShift)
        {
            shift.CoverHours = 0.0;
            shift.ShiftNote = string.Empty;
        }
        else
        {
            shift.CoverHours = shift.TotalHours;
            shift.ShiftNote = shift.ShiftNote?.Trim() ?? string.Empty;
        }

        if (shift.HourlyRate <= 0m)
        {
            shift.HourlyRate = WorkShift.DefaultHourlyRate;
        }

        if (!shift.HasOtherPenalty)
        {
            shift.OtherPenaltyAmount = 0m;
            shift.OtherPenaltyNote = string.Empty;
        }
        else
        {
            shift.OtherPenaltyAmount = Math.Max(0m, shift.OtherPenaltyAmount);
            shift.OtherPenaltyNote = shift.OtherPenaltyNote?.Trim() ?? string.Empty;
        }
    }
}
