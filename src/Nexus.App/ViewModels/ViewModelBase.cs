using CommunityToolkit.Mvvm.ComponentModel;

namespace Nexus.App.ViewModels;

/// <summary>
/// Common base for all view-models. Adds a shared <see cref="IsBusy"/> flag used to
/// gate long-running work and drive progress affordances in the UI.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>True while the view-model is performing background work.</summary>
    [ObservableProperty]
    private bool _isBusy;
}
