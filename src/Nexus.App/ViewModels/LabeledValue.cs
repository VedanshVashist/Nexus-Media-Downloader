namespace Nexus.App.ViewModels;

/// <summary>
/// A value paired with a display label, for populating combo boxes from enums
/// without scattering enum-to-text converters through the XAML. Bind the combo's
/// <c>DisplayMemberPath</c> to <see cref="Label"/> and <c>SelectedValuePath</c> to
/// <see cref="Value"/>.
/// </summary>
public sealed record LabeledValue<T>(T Value, string Label)
{
    // A re-templated ComboBox renders its collapsed selection through
    // SelectionBoxItem/SelectionBoxItemTemplate. With DisplayMemberPath (and no
    // explicit ItemTemplate) WPF leaves SelectionBoxItemTemplate null, so the
    // selection box falls back to ToString(). Return the label so the collapsed
    // box shows "1080p" rather than the record's synthesized member dump.
    public override string ToString() => Label;
}
