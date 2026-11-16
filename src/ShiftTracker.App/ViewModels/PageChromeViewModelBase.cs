using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftTracker.App.Models;
using ShiftTracker.App.Services;

namespace ShiftTracker.App.ViewModels;

public abstract partial class PageChromeViewModelBase : ObservableObject
{
    public const int YearRangeOffset = 10;
    public const int YearGridSize = 9;

    protected readonly ThemeService ThemeServiceInstance;
    private bool _isSyncingThemeState;

    public int MinAllowedYear => DateTime.Today.Year - YearRangeOffset;

    public int MaxAllowedYear => DateTime.Today.Year + YearRangeOffset;

    public string AllowedYearRangeLabel =>
        $"Chọn Năm ({MinAllowedYear} - {MaxAllowedYear}):";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DarkModeIcon))]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isThemePopupOpen;

    [ObservableProperty]
    private bool _isQuickJumpPopupOpen;

    [ObservableProperty]
    private int _pickerMonth = DateTime.Today.Month;

    [ObservableProperty]
    private int _pickerYear = DateTime.Today.Year;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YearPageRangeLabel))]
    [NotifyPropertyChangedFor(nameof(CanDecrementYearPage))]
    [NotifyPropertyChangedFor(nameof(CanIncrementYearPage))]
    [NotifyPropertyChangedFor(nameof(CanDecrementPickerYear))]
    [NotifyPropertyChangedFor(nameof(CanIncrementPickerYear))]
    private int _yearPageStart = DateTime.Today.Year - (YearGridSize / 2);

    [ObservableProperty]
    private string _customHexInput = ThemeService.DefaultPrimaryHex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasThemeError))]
    private string _themeErrorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WallpaperStatusText))]
    private bool _hasWallpaper;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WallpaperOpacityPercentText))]
    private double _wallpaperOpacity = ThemeService.DefaultWallpaperOpacity;

    public ObservableCollection<ThemePresetItem> ThemePresets { get; } = [];

    public ObservableCollection<WallpaperAspectItem> WallpaperAspects { get; } = [];

    public ObservableCollection<QuickMonthItem> QuickMonths { get; } = [];

    public ObservableCollection<QuickYearItem> QuickYears { get; } = [];

    public string DarkModeIcon => IsDarkMode ? "☀️" : "🌙";

    public bool HasThemeError => !string.IsNullOrWhiteSpace(ThemeErrorMessage);

    public string YearPageRangeLabel =>
        $"{YearPageStart} - {YearPageStart + YearGridSize - 1}";

    public bool CanDecrementYearPage =>
        (YearPageStart - 1) >= MinAllowedYear;

    public bool CanIncrementYearPage =>
        (YearPageStart + YearGridSize) <= MaxAllowedYear;

    public bool CanDecrementPickerYear => CanDecrementYearPage;

    public bool CanIncrementPickerYear => CanIncrementYearPage;

    public string WallpaperStatusText =>
        HasWallpaper
            ? "Đang sử dụng ảnh cá nhân"
            : "Đang sử dụng nền đơn sắc mặc định";

    public string WallpaperOpacityPercentText =>
        $"{(int)Math.Round(WallpaperOpacity * 100)}%";

    protected PageChromeViewModelBase(ThemeService themeService)
    {
        ThemeServiceInstance = themeService;
        _isSyncingThemeState = true;
        _isDarkMode = themeService.IsDarkMode;
        _hasWallpaper = themeService.HasWallpaper;
        _wallpaperOpacity = themeService.WallpaperOpacity;
        _customHexInput = themeService.CurrentHex;
        _yearPageStart = CalculatePageStart(DateTime.Today.Year);
        _isSyncingThemeState = false;

        ThemeServiceInstance.ThemeChanged += OnThemeServiceChanged;

        InitThemePresets();
        InitWallpaperAspects();
        InitQuickPickerItems();
    }

    public int ClampAllowedYear(int year) =>
        Math.Clamp(year, MinAllowedYear, MaxAllowedYear);

    partial void OnIsDarkModeChanged(bool value)
    {
        if (!_isSyncingThemeState && ThemeServiceInstance.IsDarkMode != value)
        {
            ThemeServiceInstance.SetDarkMode(value);
        }
    }

    partial void OnWallpaperOpacityChanged(double value)
    {
        if (!_isSyncingThemeState && Math.Abs(ThemeServiceInstance.WallpaperOpacity - value) > 0.005)
        {
            ThemeServiceInstance.SetWallpaperOpacity(value);
        }
    }

    private void OnThemeServiceChanged()
    {
        _isSyncingThemeState = true;
        try
        {
            IsDarkMode = ThemeServiceInstance.IsDarkMode;
            HasWallpaper = ThemeServiceInstance.HasWallpaper;
            WallpaperOpacity = ThemeServiceInstance.WallpaperOpacity;
            CustomHexInput = ThemeServiceInstance.CurrentHex;
            UpdateSelectedThemePreset(ThemeServiceInstance.CurrentHex);
            UpdateSelectedWallpaperAspect(ThemeServiceInstance.WallpaperAspect);
        }
        finally
        {
            _isSyncingThemeState = false;
        }
    }

    [RelayCommand]
    private void ToggleDarkModeQuick()
    {
        ThemeServiceInstance.ToggleDarkMode();
    }

    [RelayCommand]
    private void OpenThemePopup()
    {
        _isSyncingThemeState = true;
        CustomHexInput = ThemeServiceInstance.CurrentHex;
        IsDarkMode = ThemeServiceInstance.IsDarkMode;
        HasWallpaper = ThemeServiceInstance.HasWallpaper;
        WallpaperOpacity = ThemeServiceInstance.WallpaperOpacity;
        _isSyncingThemeState = false;

        ThemeErrorMessage = string.Empty;
        UpdateSelectedThemePreset(ThemeServiceInstance.CurrentHex);
        UpdateSelectedWallpaperAspect(ThemeServiceInstance.WallpaperAspect);
        IsThemePopupOpen = true;
    }

    [RelayCommand]
    private void CloseThemePopup()
    {
        ThemeErrorMessage = string.Empty;
        IsThemePopupOpen = false;
    }

    [RelayCommand]
    private void SelectThemePreset(ThemePresetItem? preset)
    {
        if (preset is null)
        {
            return;
        }

        ThemeServiceInstance.ApplyTheme(preset.HexCode, persist: true);
        CustomHexInput = ThemeServiceInstance.CurrentHex;
        ThemeErrorMessage = string.Empty;
        UpdateSelectedThemePreset(ThemeServiceInstance.CurrentHex);
    }

    [RelayCommand]
    private void ApplyCustomHexTheme()
    {
        if (!ThemeServiceInstance.TryApplyCustomHex(CustomHexInput, out var normalizedHex))
        {
            ThemeErrorMessage = "Mã màu Hex không hợp lệ (ví dụ hợp lệ: #800020 hoặc #1A365D).";
            return;
        }

        CustomHexInput = normalizedHex;
        ThemeErrorMessage = string.Empty;
        UpdateSelectedThemePreset(normalizedHex);
    }

    [RelayCommand]
    private async Task PickWallpaperAsync()
    {
        var picked = await ThemeServiceInstance.PickAndSaveWallpaperAsync();
        if (picked)
        {
            HasWallpaper = ThemeServiceInstance.HasWallpaper;
        }
    }

    [RelayCommand]
    private void ClearWallpaper()
    {
        ThemeServiceInstance.ClearWallpaper();
        HasWallpaper = ThemeServiceInstance.HasWallpaper;
    }

    [RelayCommand]
    private void SelectWallpaperAspect(WallpaperAspectItem? item)
    {
        if (item is null)
        {
            return;
        }

        ThemeServiceInstance.SetWallpaperAspect(item.AspectValue);
        UpdateSelectedWallpaperAspect(item.AspectValue);
    }

    protected void OpenQuickJumpForDate(int month, int year)
    {
        PickerMonth = Math.Clamp(month, 1, 12);
        PickerYear = ClampAllowedYear(year);
        YearPageStart = CalculatePageStart(PickerYear);
        SyncQuickPickerSelection();
        IsQuickJumpPopupOpen = true;
    }

    [RelayCommand]
    private void CloseQuickJumpPopup()
    {
        IsQuickJumpPopupOpen = false;
    }

    [RelayCommand]
    private void SelectQuickMonth(QuickMonthItem? item)
    {
        if (item is null)
        {
            return;
        }

        PickerMonth = Math.Clamp(item.Month, 1, 12);
        SyncQuickPickerSelection();
    }

    [RelayCommand]
    private void SelectQuickYear(QuickYearItem? item)
    {
        if (item is null || !item.IsEnabled)
        {
            return;
        }

        PickerYear = ClampAllowedYear(item.Year);
        SyncQuickPickerSelection();
    }

    [RelayCommand]
    private void DecrementYearPage()
    {
        if (!CanDecrementYearPage)
        {
            return;
        }

        YearPageStart -= YearGridSize;
        SyncQuickPickerSelection();
    }

    [RelayCommand]
    private void IncrementYearPage()
    {
        if (!CanIncrementYearPage)
        {
            return;
        }

        YearPageStart += YearGridSize;
        SyncQuickPickerSelection();
    }

    [RelayCommand]
    private void DecrementPickerYear() => DecrementYearPage();

    [RelayCommand]
    private void IncrementPickerYear() => IncrementYearPage();

    private static int CalculatePageStart(int targetYear)
    {
        var anchor = DateTime.Today.Year - (YearGridSize / 2);
        var pageOffset = (int)Math.Floor((double)(targetYear - anchor) / YearGridSize);
        return anchor + (pageOffset * YearGridSize);
    }

    private void InitThemePresets()
    {
        ThemePresets.Clear();
        foreach (var (name, hex) in ThemeService.LuxuryPresets)
        {
            ThemePresets.Add(new ThemePresetItem
            {
                Name = name,
                HexCode = hex,
                IsSelected = string.Equals(hex, ThemeServiceInstance.CurrentHex, StringComparison.OrdinalIgnoreCase),
                SelectCommand = SelectThemePresetCommand
            });
        }
    }

    private void InitWallpaperAspects()
    {
        WallpaperAspects.Clear();
        foreach (var (key, label, aspect) in ThemeService.WallpaperAspectPresets)
        {
            WallpaperAspects.Add(new WallpaperAspectItem
            {
                Key = key,
                Label = label,
                AspectValue = aspect,
                IsSelected = aspect == ThemeServiceInstance.WallpaperAspect,
                SelectCommand = SelectWallpaperAspectCommand
            });
        }
    }

    private void InitQuickPickerItems()
    {
        QuickMonths.Clear();
        for (var m = 1; m <= 12; m++)
        {
            QuickMonths.Add(new QuickMonthItem
            {
                Month = m,
                IsSelected = m == PickerMonth,
                SelectCommand = SelectQuickMonthCommand
            });
        }

        QuickYears.Clear();
        for (var i = 0; i < YearGridSize; i++)
        {
            QuickYears.Add(new QuickYearItem
            {
                GridIndex = i,
                Year = YearPageStart + i,
                SelectCommand = SelectQuickYearCommand
            });
        }

        SyncQuickPickerSelection();
    }

    protected void SyncQuickPickerSelection()
    {
        PickerYear = ClampAllowedYear(PickerYear);

        foreach (var m in QuickMonths)
        {
            m.IsSelected = m.Month == PickerMonth;
        }

        var currentCalendarYear = DateTime.Today.Year;
        for (var i = 0; i < QuickYears.Count; i++)
        {
            var yearVal = YearPageStart + i;
            var item = QuickYears[i];
            item.Year = yearVal;
            item.IsEnabled = yearVal >= MinAllowedYear && yearVal <= MaxAllowedYear;
            item.IsSelected = item.IsEnabled && yearVal == PickerYear;
            item.IsCurrentYear = yearVal == currentCalendarYear;
        }

        OnPropertyChanged(nameof(YearPageRangeLabel));
        OnPropertyChanged(nameof(CanDecrementYearPage));
        OnPropertyChanged(nameof(CanIncrementYearPage));
        OnPropertyChanged(nameof(CanDecrementPickerYear));
        OnPropertyChanged(nameof(CanIncrementPickerYear));
    }

    private void UpdateSelectedThemePreset(string currentHex)
    {
        foreach (var preset in ThemePresets)
        {
            preset.IsSelected = string.Equals(preset.HexCode, currentHex, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void UpdateSelectedWallpaperAspect(Aspect currentAspect)
    {
        foreach (var item in WallpaperAspects)
        {
            item.IsSelected = item.AspectValue == currentAspect;
        }
    }
}
