namespace Nexus.App.ViewModels;

/// <summary>
/// Base class for the top-level pages hosted by the shell. Carries the navigation
/// metadata (key, title, sidebar glyph) and an <see cref="OnActivatedAsync"/> hook
/// so pages can lazily load their data the first time they're shown.
/// </summary>
public abstract class PageViewModel : ViewModelBase
{
    protected PageViewModel(string navigationKey, string title, string glyph)
    {
        NavigationKey = navigationKey;
        Title = title;
        Glyph = glyph;
    }

    /// <summary>Stable key used by the shell to identify/select this page.</summary>
    public string NavigationKey { get; }

    /// <summary>Human-readable page title shown in the header and sidebar.</summary>
    public string Title { get; }

    /// <summary>Segoe Fluent icon glyph (as a string) shown in the sidebar.</summary>
    public string Glyph { get; }

    /// <summary>
    /// Whether this page appears in the primary sidebar navigation. Pages such as
    /// Settings/About are reachable but rendered in a secondary group.
    /// </summary>
    public bool IsPrimaryNavigation { get; init; } = true;

    private bool _activatedOnce;

    /// <summary>
    /// Invoked by the shell each time the page becomes current. The first activation
    /// also triggers <see cref="OnFirstActivatedAsync"/> for one-time data loading.
    /// </summary>
    public async Task OnActivatedAsync()
    {
        if (!_activatedOnce)
        {
            _activatedOnce = true;
            await OnFirstActivatedAsync().ConfigureAwait(true);
        }

        await OnReactivatedAsync().ConfigureAwait(true);
    }

    /// <summary>Override to load data the first time the page is shown.</summary>
    protected virtual Task OnFirstActivatedAsync() => Task.CompletedTask;

    /// <summary>Override to refresh transient state every time the page is shown.</summary>
    protected virtual Task OnReactivatedAsync() => Task.CompletedTask;
}
