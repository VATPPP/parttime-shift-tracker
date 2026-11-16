using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShiftTracker.App.Models;

public partial class ThemePresetItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;

    public string HexCode { get; init; } = "#800020";

    public Color PreviewColor => Color.FromArgb(HexCode);

    [ObservableProperty]
    private bool _isSelected;

    public IRelayCommand<ThemePresetItem>? SelectCommand { get; set; }
}

public partial class WallpaperAspectItem : ObservableObject
{
    public Aspect AspectValue { get; init; }

    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    public IRelayCommand<WallpaperAspectItem>? SelectCommand { get; set; }
}
