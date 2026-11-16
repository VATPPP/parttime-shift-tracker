using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftTracker.App.Models;
using ShiftTracker.App.Services;

namespace ShiftTracker.App.ViewModels;

[QueryProperty(nameof(ShiftId), "shiftId")]
[QueryProperty(nameof(InitialDateString), "initialDate")]
public partial class AddShiftViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly HolidayService _holidayService;
    private bool _isLoadingExisting;
    private readonly HashSet<string> _selectedCoworkerNames = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private WorkShift _currentShift = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private int _shiftId;

    [ObservableProperty]
    private string? _initialDateString;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderDateText))]
    private DateTime _shiftDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirstPreset))]
    [NotifyPropertyChangedFor(nameof(IsSecondPreset))]
    private TimeSpan _startTime = WorkShift.FirstPresetStart;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirstPreset))]
    [NotifyPropertyChangedFor(nameof(IsSecondPreset))]
    private TimeSpan _endTime = WorkShift.FirstPresetEnd;

    [ObservableProperty]
    private bool _isHoliday;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHolidayHint))]
    private string _holidayHintText = string.Empty;

    [ObservableProperty]
    private string _newCoworkerName = string.Empty;

    [ObservableProperty]
    private bool _isManageTagsPopupOpen;

    [ObservableProperty]
    private bool _hasLate;

    [ObservableProperty]
    private string _lateMinutesText = string.Empty;

    [ObservableProperty]
    private bool _isCoverShift;

    [ObservableProperty]
    private string _shiftNote = string.Empty;

    [ObservableProperty]
    private bool _hasOtherPenalty;

    [ObservableProperty]
    private string _otherPenaltyAmountText = string.Empty;

    [ObservableProperty]
    private string _otherPenaltyNote = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    public ObservableCollection<ShiftTimeSlot> ExtraTimeSlots { get; } = [];

    public ObservableCollection<CoworkerTagItem> AvailableCoworkerTags { get; } = [];

    public bool HasExtraTimeSlots => ExtraTimeSlots.Count > 0;

    public bool HasAvailableTags => AvailableCoworkerTags.Count > 0;

    public bool HasNoAvailableTags => AvailableCoworkerTags.Count == 0;

    public bool IsEditing => ShiftId > 0;

    public string PageTitle => IsEditing ? "Chỉnh sửa ca làm" : "Thêm ca làm mới";

    public string HeaderDateText =>
        $"{WorkShift.GetVietnameseDayOfWeek(ShiftDate.DayOfWeek)}, {ShiftDate:dd/MM/yyyy}";

    public bool IsFirstPreset =>
        ExtraTimeSlots.Count == 0 &&
        StartTime == WorkShift.FirstPresetStart &&
        EndTime == WorkShift.FirstPresetEnd;

    public bool IsSecondPreset =>
        ExtraTimeSlots.Count == 0 &&
        StartTime == WorkShift.SecondPresetStart &&
        EndTime == WorkShift.SecondPresetEnd;

    public bool HasHolidayHint => !string.IsNullOrWhiteSpace(HolidayHintText);

    public string PreviewDurationText => CurrentShift.DurationFormatted;

    public string PreviewRegularWageText => WorkShift.FormatCurrencyK(CurrentShift.RegularWage);

    public bool PreviewHasOt => CurrentShift.HasOt;

    public string PreviewOtText =>
        CurrentShift.HasOt
            ? $"+{WorkShift.FormatCurrencyK(CurrentShift.OtWage)} ({(int)Math.Round(CurrentShift.OtMinutes)} phút)"
            : "0k";

    public bool PreviewHasLatePenalty => CurrentShift.LateMinutes > 0;

    public string PreviewLatePenaltyText =>
        CurrentShift.LateMinutes <= 0
            ? "0k"
            : CurrentShift.IsLatePenalized
                ? $"-{WorkShift.FormatCurrencyK(CurrentShift.LatePenaltyAmount)} ({CurrentShift.LateMinutes} phút)"
                : $"0k (Trễ {CurrentShift.LateMinutes}p - Miễn phạt)";

    public bool PreviewHasOtherPenalty =>
        CurrentShift.HasOtherPenalty && CurrentShift.EffectiveOtherPenaltyAmount > 0m;

    public string PreviewOtherPenaltyText =>
        $"-{WorkShift.FormatCurrencyK(CurrentShift.EffectiveOtherPenaltyAmount)}";

    public string PreviewNetSalaryText => CurrentShift.NetShiftSalaryFormatted;

    public AddShiftViewModel(DatabaseService databaseService, HolidayService holidayService)
    {
        _databaseService = databaseService;
        _holidayService = holidayService;

        CheckHolidayForDate(ShiftDate, autoToggle: true);
        SyncToCurrentShift();
        _ = LoadSavedTagsAsync();
    }

    async partial void OnShiftIdChanged(int value)
    {
        if (value <= 0)
        {
            return;
        }

        await LoadExistingShiftAsync(value);
    }

    partial void OnInitialDateStringChanged(string? value)
    {
        if (IsEditing || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            ShiftDate = parsedDate.Date;
        }
    }

    partial void OnShiftDateChanged(DateTime value)
    {
        CheckHolidayForDate(value, autoToggle: !_isLoadingExisting);
        SyncToCurrentShift();
    }

    partial void OnStartTimeChanged(TimeSpan value) => SyncToCurrentShift();

    partial void OnEndTimeChanged(TimeSpan value) => SyncToCurrentShift();

    partial void OnIsHolidayChanged(bool value) => SyncToCurrentShift();

    partial void OnHasLateChanged(bool value)
    {
        if (!value)
        {
            LateMinutesText = string.Empty;
        }

        SyncToCurrentShift();
    }

    partial void OnLateMinutesTextChanged(string value) => SyncToCurrentShift();

    partial void OnIsCoverShiftChanged(bool value)
    {
        if (!value)
        {
            ShiftNote = string.Empty;
        }

        SyncToCurrentShift();
    }

    partial void OnShiftNoteChanged(string value) => SyncToCurrentShift();

    partial void OnHasOtherPenaltyChanged(bool value)
    {
        if (!value)
        {
            OtherPenaltyAmountText = string.Empty;
            OtherPenaltyNote = string.Empty;
        }

        SyncToCurrentShift();
    }

    partial void OnOtherPenaltyAmountTextChanged(string value) => SyncToCurrentShift();

    partial void OnOtherPenaltyNoteChanged(string value) => SyncToCurrentShift();

    [RelayCommand]
    private void SelectFirstPreset()
    {
        StartTime = WorkShift.FirstPresetStart;
        EndTime = WorkShift.FirstPresetEnd;
        OnPropertyChanged(nameof(IsFirstPreset));
        OnPropertyChanged(nameof(IsSecondPreset));
    }

    [RelayCommand]
    private void SelectSecondPreset()
    {
        StartTime = WorkShift.SecondPresetStart;
        EndTime = WorkShift.SecondPresetEnd;
        OnPropertyChanged(nameof(IsFirstPreset));
        OnPropertyChanged(nameof(IsSecondPreset));
    }

    [RelayCommand]
    private void AddTimeSlot()
    {
        var defaultStart = ExtraTimeSlots.Count == 0
            ? new TimeSpan(17, 0, 0)
            : ExtraTimeSlots[^1].EndTime;
        var defaultEnd = new TimeSpan(22, 0, 0);
        if (defaultEnd <= defaultStart)
        {
            defaultEnd = defaultStart.Add(TimeSpan.FromHours(4));
        }

        var slot = new ShiftTimeSlot(defaultStart, defaultEnd, SyncToCurrentShift)
        {
            SlotIndex = ExtraTimeSlots.Count + 2,
            CanRemove = true,
            RemoveCommand = RemoveTimeSlotCommand
        };

        ExtraTimeSlots.Add(slot);
        ReindexExtraSlots();
        OnPropertyChanged(nameof(HasExtraTimeSlots));
        OnPropertyChanged(nameof(IsFirstPreset));
        OnPropertyChanged(nameof(IsSecondPreset));
        SyncToCurrentShift();
    }

    [RelayCommand]
    private void RemoveTimeSlot(ShiftTimeSlot? slot)
    {
        if (slot is null)
        {
            return;
        }

        ExtraTimeSlots.Remove(slot);
        ReindexExtraSlots();
        OnPropertyChanged(nameof(HasExtraTimeSlots));
        OnPropertyChanged(nameof(IsFirstPreset));
        OnPropertyChanged(nameof(IsSecondPreset));
        SyncToCurrentShift();
    }

    [RelayCommand]
    private void OpenManageTagsPopup()
    {
        NewCoworkerName = string.Empty;
        IsManageTagsPopupOpen = true;
    }

    [RelayCommand]
    private void CloseManageTagsPopup()
    {
        NewCoworkerName = string.Empty;
        IsManageTagsPopupOpen = false;
    }

    [RelayCommand]
    private void ToggleCoworkerTag(CoworkerTagItem? tag)
    {
        if (tag is null)
        {
            return;
        }

        tag.IsSelected = !tag.IsSelected;
        if (tag.IsSelected)
        {
            _selectedCoworkerNames.Add(tag.Name);
        }
        else
        {
            _selectedCoworkerNames.Remove(tag.Name);
        }

        SyncToCurrentShift();
    }

    [RelayCommand]
    private async Task AddCoworkerTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCoworkerName))
        {
            return;
        }

        var names = NewCoworkerName
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var rawName in names)
        {
            var trimmed = rawName.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var savedTag = await _databaseService.AddTagAsync(trimmed);
            if (savedTag is not null)
            {
                _selectedCoworkerNames.Add(savedTag.Name);
            }
        }

        NewCoworkerName = string.Empty;
        await LoadSavedTagsAsync();
        SyncToCurrentShift();
    }

    [RelayCommand]
    private async Task DeleteCoworkerTagAsync(CoworkerTagItem? tag)
    {
        if (tag is null)
        {
            return;
        }

        if (tag.Id > 0)
        {
            await _databaseService.DeleteTagAsync(tag.Id);
        }

        _selectedCoworkerNames.Remove(tag.Name);
        AvailableCoworkerTags.Remove(tag);
        NotifyTagCollectionState();
        SyncToCurrentShift();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        SyncToCurrentShift();

        if (CurrentShift.TotalMinutes <= 0)
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await page.DisplayAlertAsync(
                    "Giờ làm chưa hợp lệ",
                    "Vui lòng kiểm tra lại Giờ vào và Giờ ra của ca làm.",
                    "Đồng ý");
            }

            return;
        }

        try
        {
            IsSaving = true;
            CurrentShift.WorkDate = ShiftDate.Date;
            CurrentShift.ShiftDate = ShiftDate.Date;
            await _databaseService.SaveShiftAsync(CurrentShift);
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsEditing || IsSaving)
        {
            return;
        }

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is not null)
        {
            var confirmed = await page.DisplayAlertAsync(
                "Xóa ca làm",
                "Bạn có chắc chắn muốn xóa ca làm này không?",
                "Xóa",
                "Hủy");

            if (!confirmed)
            {
                return;
            }
        }

        try
        {
            IsSaving = true;
            await _databaseService.DeleteShiftByIdAsync(ShiftId);
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public async Task LoadSavedTagsAsync()
    {
        var dbTags = await _databaseService.GetAllTagsAsync();

        foreach (var selectedName in _selectedCoworkerNames.ToList())
        {
            var existsInDb = dbTags.Any(t => string.Equals(t.Name, selectedName, StringComparison.OrdinalIgnoreCase));
            if (!existsInDb)
            {
                var created = await _databaseService.AddTagAsync(selectedName);
                if (created is not null)
                {
                    dbTags.Add(created);
                }
            }
        }

        AvailableCoworkerTags.Clear();
        foreach (var tag in dbTags.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            AvailableCoworkerTags.Add(new CoworkerTagItem
            {
                Id = tag.Id,
                Name = tag.Name,
                IsSelected = _selectedCoworkerNames.Contains(tag.Name),
                ToggleCommand = ToggleCoworkerTagCommand,
                DeleteCommand = DeleteCoworkerTagCommand
            });
        }

        NotifyTagCollectionState();
    }

    private void NotifyTagCollectionState()
    {
        OnPropertyChanged(nameof(HasAvailableTags));
        OnPropertyChanged(nameof(HasNoAvailableTags));
    }

    private async Task LoadExistingShiftAsync(int id)
    {
        var existing = await _databaseService.GetShiftByIdAsync(id);
        if (existing is null)
        {
            return;
        }

        try
        {
            _isLoadingExisting = true;
            CurrentShift = existing;
            ShiftDate = existing.ShiftDate.Date;

            var parsedSlots = ShiftTimeSlot.ParseSlots(existing.TimeSlotsText, existing.StartTime, existing.EndTime);
            StartTime = parsedSlots[0].Start;
            EndTime = parsedSlots[0].End;

            ExtraTimeSlots.Clear();
            for (var i = 1; i < parsedSlots.Count; i++)
            {
                ExtraTimeSlots.Add(new ShiftTimeSlot(parsedSlots[i].Start, parsedSlots[i].End, SyncToCurrentShift)
                {
                    SlotIndex = i + 1,
                    CanRemove = true,
                    RemoveCommand = RemoveTimeSlotCommand
                });
            }

            _selectedCoworkerNames.Clear();
            foreach (var name in existing.CoworkerList)
            {
                _selectedCoworkerNames.Add(name);
            }

            await LoadSavedTagsAsync();

            IsHoliday = existing.IsHoliday;

            HasLate = existing.HasLate || existing.LateMinutes > 0;
            LateMinutesText = existing.LateMinutes > 0
                ? existing.LateMinutes.ToString(CultureInfo.InvariantCulture)
                : string.Empty;

            IsCoverShift = existing.IsCoverShift || existing.CoverHours > 0 || !string.IsNullOrWhiteSpace(existing.ShiftNote);
            ShiftNote = existing.ShiftNote;

            HasOtherPenalty = existing.HasOtherPenalty;
            OtherPenaltyAmountText = existing.HasOtherPenalty && existing.OtherPenaltyAmount > 0m
                ? existing.OtherPenaltyAmount.ToString("0", CultureInfo.InvariantCulture)
                : string.Empty;
            OtherPenaltyNote = existing.OtherPenaltyNote;

            CheckHolidayForDate(ShiftDate, autoToggle: false);
            OnPropertyChanged(nameof(HasExtraTimeSlots));
        }
        finally
        {
            _isLoadingExisting = false;
            SyncToCurrentShift();
        }
    }

    private void ReindexExtraSlots()
    {
        for (var i = 0; i < ExtraTimeSlots.Count; i++)
        {
            ExtraTimeSlots[i].SlotIndex = i + 2;
        }
    }

    private void CheckHolidayForDate(DateTime date, bool autoToggle)
    {
        var holidayName = _holidayService.GetHolidayName(date);
        if (holidayName is not null)
        {
            HolidayHintText = $"🏮 Gợi ý ngày lễ: {holidayName}";
            if (autoToggle)
            {
                IsHoliday = true;
            }
        }
        else
        {
            HolidayHintText = string.Empty;
            if (autoToggle)
            {
                IsHoliday = false;
            }
        }
    }

    private void SyncToCurrentShift()
    {
        if (_isLoadingExisting)
        {
            return;
        }

        CurrentShift.Id = ShiftId;
        CurrentShift.WorkDate = ShiftDate.Date;
        CurrentShift.ShiftDate = ShiftDate.Date;
        CurrentShift.StartTime = StartTime;
        CurrentShift.EndTime = EndTime;

        var allSlots = new List<(TimeSpan Start, TimeSpan End)>
        {
            (StartTime, EndTime)
        };
        foreach (var extra in ExtraTimeSlots)
        {
            allSlots.Add((extra.StartTime, extra.EndTime));
        }

        CurrentShift.TimeSlotsText = ShiftTimeSlot.SerializeSlots(allSlots);
        CurrentShift.ShiftType = CurrentShift.TimeSlotsText;

        var selectedNames = AvailableCoworkerTags
            .Where(t => t.IsSelected)
            .Select(t => t.Name)
            .ToList();
        if (selectedNames.Count == 0 && _selectedCoworkerNames.Count > 0)
        {
            selectedNames = _selectedCoworkerNames.ToList();
        }

        CurrentShift.CoworkersText = string.Join(", ", selectedNames);
        CurrentShift.IsHoliday = IsHoliday;

        CurrentShift.HasLate = HasLate;
        CurrentShift.LateMinutes = HasLate
            ? ParseNonNegativeInt(LateMinutesText)
            : 0;

        CurrentShift.IsCoverShift = IsCoverShift;
        CurrentShift.CoverHours = IsCoverShift
            ? CurrentShift.TotalHours
            : 0.0;
        CurrentShift.ShiftNote = IsCoverShift
            ? (ShiftNote?.Trim() ?? string.Empty)
            : string.Empty;

        CurrentShift.HasOtherPenalty = HasOtherPenalty;
        CurrentShift.OtherPenaltyAmount = HasOtherPenalty
            ? ParseNonNegativeDecimal(OtherPenaltyAmountText)
            : 0m;
        CurrentShift.OtherPenaltyNote = HasOtherPenalty
            ? (OtherPenaltyNote?.Trim() ?? string.Empty)
            : string.Empty;

        OnPropertyChanged(nameof(CurrentShift));
        OnPropertyChanged(nameof(PreviewDurationText));
        OnPropertyChanged(nameof(PreviewRegularWageText));
        OnPropertyChanged(nameof(PreviewHasOt));
        OnPropertyChanged(nameof(PreviewOtText));
        OnPropertyChanged(nameof(PreviewHasLatePenalty));
        OnPropertyChanged(nameof(PreviewLatePenaltyText));
        OnPropertyChanged(nameof(PreviewHasOtherPenalty));
        OnPropertyChanged(nameof(PreviewOtherPenaltyText));
        OnPropertyChanged(nameof(PreviewNetSalaryText));
    }

    private static int ParseNonNegativeInt(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return 0;
        }

        var cleaned = new string(input.Where(char.IsDigit).ToArray());
        return int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? Math.Max(0, result)
            : 0;
    }

    private static decimal ParseNonNegativeDecimal(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return 0m;
        }

        var cleaned = new string(input.Where(char.IsDigit).ToArray());
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? Math.Max(0m, result)
            : 0m;
    }
}
