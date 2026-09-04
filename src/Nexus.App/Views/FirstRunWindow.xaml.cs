using System.Windows;

namespace Nexus.App.Views;

/// <summary>
/// First-run setup wizard window. Navigation between steps and persistence are handled
/// by <see cref="ViewModels.FirstRunViewModel"/>; this shell only draws the custom
/// title bar and routes the close button. Closing before finishing is treated by the
/// application as declining setup.
/// </summary>
public partial class FirstRunWindow : Window
{
    public FirstRunWindow() => InitializeComponent();

    private void OnCloseClick(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
}
