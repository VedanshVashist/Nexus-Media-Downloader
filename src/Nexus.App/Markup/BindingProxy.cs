using System.Windows;

namespace Nexus.App.Markup;

/// <summary>
/// A freezable that carries a <see cref="Data"/> value across the visual tree so
/// bindings inside DataTemplates, ContextMenus, and other disconnected islands can
/// reach an ancestor DataContext (typically the page view-model). Declared as a
/// resource, then referenced via <c>{Binding Data.SomeCommand, Source={StaticResource proxy}}</c>.
/// </summary>
public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
