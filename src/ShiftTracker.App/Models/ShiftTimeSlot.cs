using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShiftTracker.App.Models;

public partial class ShiftTimeSlot : ObservableObject
{
    private readonly Action? _onChanged;

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private bool _canRemove;

    [ObservableProperty]
    private TimeSpan _startTime = WorkShift.FirstPresetStart;

    [ObservableProperty]
    private TimeSpan _endTime = WorkShift.FirstPresetEnd;

    public IRelayCommand<ShiftTimeSlot>? RemoveCommand { get; set; }

    public ShiftTimeSlot(TimeSpan startTime, TimeSpan endTime, Action? onChanged = null)
    {
        _startTime = startTime;
        _endTime = endTime;
        _onChanged = onChanged;
    }

    partial void OnStartTimeChanged(TimeSpan value) => _onChanged?.Invoke();

    partial void OnEndTimeChanged(TimeSpan value) => _onChanged?.Invoke();

    public TimeSpan EffectiveEndTime =>
        EndTime < StartTime ? EndTime.Add(TimeSpan.FromDays(1)) : EndTime;

    public double TotalMinutes =>
        Math.Max(0.0, (EffectiveEndTime - StartTime).TotalMinutes);

    public double OtMinutes
    {
        get
        {
            var effectiveEnd = EffectiveEndTime;
            if (effectiveEnd <= WorkShift.OtThresholdTime)
            {
                return 0.0;
            }

            var otStart = StartTime > WorkShift.OtThresholdTime ? StartTime : WorkShift.OtThresholdTime;
            return Math.Max(0.0, (effectiveEnd - otStart).TotalMinutes);
        }
    }

    public double RegularMinutes =>
        Math.Max(0.0, TotalMinutes - OtMinutes);

    public string FormattedRange =>
        $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

    public static List<(TimeSpan Start, TimeSpan End)> ParseSlots(string? rawSlots, TimeSpan fallbackStart, TimeSpan fallbackEnd)
    {
        var result = new List<(TimeSpan Start, TimeSpan End)>();
        if (!string.IsNullOrWhiteSpace(rawSlots))
        {
            var segments = rawSlots.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments)
            {
                var parts = segment.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2 &&
                    TimeSpan.TryParseExact(parts[0], @"hh\:mm", CultureInfo.InvariantCulture, out var start) &&
                    TimeSpan.TryParseExact(parts[1], @"hh\:mm", CultureInfo.InvariantCulture, out var end))
                {
                    result.Add((start, end));
                }
            }
        }

        if (result.Count == 0)
        {
            result.Add((fallbackStart, fallbackEnd));
        }

        return result;
    }

    public static string SerializeSlots(IEnumerable<(TimeSpan Start, TimeSpan End)> slots) =>
        string.Join(", ", slots.Select(s => $"{s.Start:hh\\:mm} - {s.End:hh\\:mm}"));
}

