using System.Diagnostics;
using System.Text.RegularExpressions;
#if ANDROID
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
#endif

namespace ShiftTracker.App.Services;

public class ThemeService
{
    public const string PreferenceKey = "app_theme_primary_hex";
    public const string DarkModePreferenceKey = "app_theme_is_dark_mode";
    public const string WallpaperPathPreferenceKey = "app_theme_wallpaper_path";
    public const string WallpaperOpacityPreferenceKey = "app_theme_wallpaper_opacity";
    public const string WallpaperAspectPreferenceKey = "app_theme_wallpaper_aspect";

    public const string DefaultPrimaryHex = "#800020";
    public const double DefaultWallpaperOpacity = 0.2;
    public const double MinWallpaperOpacity = 0.05;
    public const double MaxWallpaperOpacity = 0.9;

    private static readonly Regex HexRegex = new(
        @"^#?(?:[0-9a-fA-F]{6})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public event Action? ThemeChanged;

    public string CurrentHex { get; private set; } = DefaultPrimaryHex;

    public bool IsDarkMode { get; private set; }

    public string? WallpaperPath { get; private set; }

    public bool HasWallpaper => !string.IsNullOrWhiteSpace(WallpaperPath) && File.Exists(WallpaperPath);

    public double WallpaperOpacity { get; private set; } = DefaultWallpaperOpacity;

    public Aspect WallpaperAspect { get; private set; } = Aspect.AspectFill;

    public static IReadOnlyList<(string Name, string Hex)> LuxuryPresets { get; } =
    [
        ("Đỏ rượu vang", "#800020"),
        ("Vàng hồng Rose Gold", "#C87D65"),
        ("Xanh Navy", "#1A365D"),
        ("Xanh ngọc lục bảo", "#059669"),
        ("Đen khói", "#2D3142")
    ];

    public static IReadOnlyList<(string Key, string Label, Aspect Value)> WallpaperAspectPresets { get; } =
    [
        ("AspectFill", "Lấp đầy", Aspect.AspectFill),
        ("AspectFit", "Vừa khung", Aspect.AspectFit),
        ("Fill", "Kéo giãn", Aspect.Fill)
    ];

    public void InitializeTheme()
    {
        var savedHex = Preferences.Default.Get(PreferenceKey, DefaultPrimaryHex);
        if (!TryNormalizeHex(savedHex, out var validHex))
        {
            validHex = DefaultPrimaryHex;
        }

        IsDarkMode = Preferences.Default.Get(DarkModePreferenceKey, false);

        var savedWallpaperPath = Preferences.Default.Get(WallpaperPathPreferenceKey, string.Empty);
        WallpaperPath = !string.IsNullOrWhiteSpace(savedWallpaperPath) && File.Exists(savedWallpaperPath)
            ? savedWallpaperPath
            : null;

        var savedOpacity = Preferences.Default.Get(WallpaperOpacityPreferenceKey, DefaultWallpaperOpacity);
        WallpaperOpacity = Math.Clamp(savedOpacity, MinWallpaperOpacity, MaxWallpaperOpacity);

        var savedAspect = Preferences.Default.Get(WallpaperAspectPreferenceKey, nameof(Aspect.AspectFill));
        WallpaperAspect = Enum.TryParse<Aspect>(savedAspect, ignoreCase: true, out var parsedAspect)
            ? parsedAspect
            : Aspect.AspectFill;

        ApplyTheme(validHex, IsDarkMode, persist: false);
    }

    public void ToggleDarkMode()
    {
        SetDarkMode(!IsDarkMode);
    }

    public void SetDarkMode(bool isDarkMode)
    {
        ApplyTheme(CurrentHex, isDarkMode, persist: true);
    }

    public bool TryApplyCustomHex(string? rawHex, out string normalizedHex)
    {
        if (!TryNormalizeHex(rawHex, out normalizedHex))
        {
            return false;
        }

        ApplyTheme(normalizedHex, IsDarkMode, persist: true);
        return true;
    }

    public void SetWallpaperOpacity(double opacity, bool persist = true)
    {
        var clamped = Math.Round(Math.Clamp(opacity, MinWallpaperOpacity, MaxWallpaperOpacity), 2);
        WallpaperOpacity = clamped;
        if (persist)
        {
            Preferences.Default.Set(WallpaperOpacityPreferenceKey, clamped);
        }

        SyncWallpaperResources();
        ThemeChanged?.Invoke();
    }

    public void SetWallpaperAspect(Aspect aspect, bool persist = true)
    {
        WallpaperAspect = aspect;
        if (persist)
        {
            Preferences.Default.Set(WallpaperAspectPreferenceKey, aspect.ToString());
        }

        SyncWallpaperResources();
        ThemeChanged?.Invoke();
    }

    public async Task<bool> PickAndSaveWallpaperAsync()
    {
        try
        {
            FileResult? pickedFile = null;
            var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Chọn hình nền ứng dụng",
                SelectionLimit = 1
            });
            pickedFile = photos?.FirstOrDefault();

            pickedFile ??= await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Chọn hình nền ứng dụng",
                FileTypes = FilePickerFileType.Images
            });

            if (pickedFile is null)
            {
                return false;
            }

            var extension = Path.GetExtension(pickedFile.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            var newFileName = $"app_wallpaper_{DateTime.UtcNow.Ticks}{extension}";
            var destinationPath = Path.Combine(FileSystem.AppDataDirectory, newFileName);

            await using (var sourceStream = await pickedFile.OpenReadAsync())
            await using (var destinationStream = File.Create(destinationPath))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }

            CleanupOldWallpaperFile(WallpaperPath, destinationPath);

            WallpaperPath = destinationPath;
            Preferences.Default.Set(WallpaperPathPreferenceKey, destinationPath);

            ApplyTheme(CurrentHex, IsDarkMode, persist: false);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking wallpaper: {ex.Message}");
            return false;
        }
    }

    public void ClearWallpaper()
    {
        CleanupOldWallpaperFile(WallpaperPath, keepPath: null);
        WallpaperPath = null;
        Preferences.Default.Remove(WallpaperPathPreferenceKey);
        ApplyTheme(CurrentHex, IsDarkMode, persist: false);
    }

    public void ApplyTheme(string hexCode, bool persist = true)
    {
        ApplyTheme(hexCode, IsDarkMode, persist);
    }

    public void ApplyTheme(string hexCode, bool isDarkMode, bool persist = true)
    {
        if (!TryNormalizeHex(hexCode, out var normalized))
        {
            normalized = DefaultPrimaryHex;
        }

        CurrentHex = normalized;
        IsDarkMode = isDarkMode;

        if (persist)
        {
            Preferences.Default.Set(PreferenceKey, normalized);
            Preferences.Default.Set(DarkModePreferenceKey, isDarkMode);
        }

        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
        }

        var basePrimaryColor = Color.FromArgb(normalized);
        var effectivePrimaryColor = isDarkMode
            ? BlendWithWhite(basePrimaryColor, 0.25f)
            : basePrimaryColor;

        var accentColor = isDarkMode
            ? BlendWithWhite(basePrimaryColor, 0.42f)
            : BlendWithWhite(basePrimaryColor, 0.22f);

        var pageBgColor = isDarkMode ? Color.FromArgb("#121212") : Color.FromArgb("#FFFFFF");
        var cardBgColor = isDarkMode ? Color.FromArgb("#E6242426") : Color.FromArgb("#E6FFFFFF");
        var calendarBoardBgColor = isDarkMode ? Color.FromArgb("#E61C1C1F") : Color.FromArgb("#E6FDFBF7");
        var calendarCellBgColor = isDarkMode ? Color.FromArgb("#EC27272A") : Color.FromArgb("#ECFFFFFF");
        var calendarBoardBorderColor = isDarkMode ? Color.FromArgb("#3F3F46") : Color.FromArgb("#E5E7EB");
        var navBarBgColor = isDarkMode ? Color.FromArgb("#F01E1E22") : Color.FromArgb("#F0FFFFFF");
        var navButtonBgColor = isDarkMode ? Color.FromArgb("#2C2C30") : Color.FromArgb("#F5EFEF");
        var primaryTextColor = isDarkMode ? Color.FromArgb("#F4F4F5") : Color.FromArgb("#1F1F1F");
        var secondaryTextColor = isDarkMode ? Color.FromArgb("#A1A1AA") : Color.FromArgb("#757575");
        var subtleBorderColor = isDarkMode ? Color.FromArgb("#3F3F46") : Color.FromArgb("#EAEAEA");

        var softSurfaceOpaque = isDarkMode
            ? BlendColors(Color.FromArgb("#27272A"), effectivePrimaryColor, 0.24f)
            : BlendWithWhite(basePrimaryColor, 0.90f);
        var softSurfaceColor = WithAlpha(softSurfaceOpaque, 0.92f);

        var cardTintOpaque = isDarkMode
            ? BlendColors(Color.FromArgb("#1C1C1F"), effectivePrimaryColor, 0.14f)
            : BlendWithWhite(basePrimaryColor, 0.94f);
        var cardTintColor = WithAlpha(cardTintOpaque, 0.90f);

        var borderTintColor = isDarkMode
            ? BlendColors(Color.FromArgb("#3F3F46"), effectivePrimaryColor, 0.35f)
            : BlendWithWhite(basePrimaryColor, 0.78f);

        var holidayBadgeBgColor = isDarkMode ? Color.FromArgb("#E63A2E14") : Color.FromArgb("#E6FFF6DC");

        var resources = Application.Current?.Resources;
        if (resources is not null)
        {
            resources["Primary"] = effectivePrimaryColor;
            resources["PrimaryColor"] = effectivePrimaryColor;
            resources["PrimaryBrush"] = new SolidColorBrush(effectivePrimaryColor);

            resources["AccentColor"] = accentColor;
            resources["AccentBrush"] = new SolidColorBrush(accentColor);

            resources["PrimarySoftColor"] = softSurfaceColor;
            resources["PrimarySoftBrush"] = new SolidColorBrush(softSurfaceColor);

            resources["PrimaryCardTintColor"] = cardTintColor;
            resources["PrimaryBorderColor"] = borderTintColor;

            resources["AppBackgroundColor"] = pageBgColor;
            resources["CardBackgroundColor"] = cardBgColor;
            resources["CalendarBoardBgColor"] = calendarBoardBgColor;
            resources["CalendarBoardBorderColor"] = calendarBoardBorderColor;
            resources["CalendarCellBgColor"] = calendarCellBgColor;
            resources["NavBarBackgroundColor"] = navBarBgColor;
            resources["NavButtonBgColor"] = navButtonBgColor;
            resources["PrimaryTextColor"] = primaryTextColor;
            resources["SecondaryTextColor"] = secondaryTextColor;
            resources["SubtleBorderColor"] = subtleBorderColor;
            resources["HolidayBadgeBgColor"] = holidayBadgeBgColor;
        }

        SyncWallpaperResources();
        SyncPlatformSystemBars(basePrimaryColor, pageBgColor, isDarkMode);
        ThemeChanged?.Invoke();
    }

    public void RefreshPlatformBars()
    {
        var basePrimaryColor = Color.FromArgb(CurrentHex);
        var pageBgColor = IsDarkMode ? Color.FromArgb("#121212") : Color.FromArgb("#FFFFFF");
        SyncPlatformSystemBars(basePrimaryColor, pageBgColor, IsDarkMode);
    }

    private void SyncWallpaperResources()
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var hasWallpaper = HasWallpaper;
        resources["HasWallpaper"] = hasWallpaper;
        resources["WallpaperOpacity"] = WallpaperOpacity;
        resources["WallpaperAspect"] = WallpaperAspect;
        resources["WallpaperSource"] = hasWallpaper && WallpaperPath is not null
            ? ImageSource.FromFile(WallpaperPath)
            : null;
    }

    private static void CleanupOldWallpaperFile(string? oldPath, string? keepPath)
    {
        if (string.IsNullOrWhiteSpace(oldPath))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(keepPath) &&
            string.Equals(oldPath, keepPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            if (oldPath.StartsWith(FileSystem.AppDataDirectory, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting old wallpaper: {ex.Message}");
        }
    }

    private static void SyncPlatformSystemBars(Color statusBarColor, Color navigationBarColor, bool isDarkMode)
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var activity = Platform.CurrentActivity;
            var window = activity?.Window;
            if (window is null)
            {
                return;
            }

            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            window.ClearFlags(WindowManagerFlags.TranslucentNavigation);

            var topColor = isDarkMode
                ? BlendColors(Color.FromArgb("#121212"), statusBarColor, 0.35f)
                : statusBarColor;

            var androidStatusColor = ToAndroidColor(topColor);
            var androidNavColor = ToAndroidColor(navigationBarColor);

#pragma warning disable CA1422
            window.SetStatusBarColor(androidStatusColor);
            window.SetNavigationBarColor(androidNavColor);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                window.NavigationBarDividerColor = androidNavColor;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                window.NavigationBarContrastEnforced = false;
                window.StatusBarContrastEnforced = false;
            }
#pragma warning restore CA1422

            var decorView = window.DecorView;
            if (decorView is not null)
            {
                var insetsController = WindowCompat.GetInsetsController(window, decorView);
                if (insetsController is not null)
                {
                    var isStatusLight = CalculateLuminance(topColor) > 0.6f;
                    insetsController.AppearanceLightStatusBars = isStatusLight;
                    insetsController.AppearanceLightNavigationBars = !isDarkMode;
                }
            }
        });
#endif
    }

#if ANDROID
    private static Android.Graphics.Color ToAndroidColor(Color mauiColor) =>
        Android.Graphics.Color.Argb(
            (int)Math.Round(mauiColor.Alpha * 255),
            (int)Math.Round(mauiColor.Red * 255),
            (int)Math.Round(mauiColor.Green * 255),
            (int)Math.Round(mauiColor.Blue * 255));
#endif

    private static float CalculateLuminance(Color color) =>
        (0.299f * color.Red) + (0.587f * color.Green) + (0.114f * color.Blue);

    public static bool TryNormalizeHex(string? input, out string normalizedHex)
    {
        normalizedHex = DefaultPrimaryHex;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (!HexRegex.IsMatch(trimmed))
        {
            return false;
        }

        normalizedHex = trimmed.StartsWith('#')
            ? trimmed.ToUpperInvariant()
            : $"#{trimmed.ToUpperInvariant()}";
        return true;
    }

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.Red, color.Green, color.Blue, Math.Clamp(alpha, 0f, 1f));

    private static Color BlendWithWhite(Color baseColor, float whiteRatio) =>
        BlendColors(baseColor, Colors.White, whiteRatio);

    private static Color BlendColors(Color from, Color to, float ratio)
    {
        var r = from.Red + ((to.Red - from.Red) * ratio);
        var g = from.Green + ((to.Green - from.Green) * ratio);
        var b = from.Blue + ((to.Blue - from.Blue) * ratio);
        return new Color(
            Math.Clamp(r, 0f, 1f),
            Math.Clamp(g, 0f, 1f),
            Math.Clamp(b, 0f, 1f),
            1f);
    }
}
