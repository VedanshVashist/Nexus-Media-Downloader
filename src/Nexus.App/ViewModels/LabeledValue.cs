namespace Nexus.App.ViewModels;

/// <summary>
/// A value paired with a display label, for populating combo boxes from enums
/// without scattering enum-to-text converters through the XAML. Bind the combo's
/// <c>DisplayMemberPath</c> to <see cref="Label"/> and <c>SelectedValuePath</c> to
/// <see cref="Value"/>.
/// </summary>
public sealed record LabeledValue<T>(T Value, string Label);
