using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShiftTracker.App.Models;

public partial class CoworkerTagItem : ObservableObject
{
    public int Id { get; set; }

    public string Name { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    public IRelayCommand<CoworkerTagItem>? ToggleCommand { get; set; }

    public IAsyncRelayCommand<CoworkerTagItem>? DeleteCommand { get; set; }
}
