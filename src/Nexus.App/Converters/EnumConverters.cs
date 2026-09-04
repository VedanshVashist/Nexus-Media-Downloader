using System.Globalization;
using System.Windows.Data;

namespace Nexus.App.Converters;
/// <summary>
/// Two-way converter between an enum-valued binding and a bool, for wiring
/// RadioButtons/ToggleButtons to a single enum property. The target enum value is
/// supplied via ConverterParameter (as the enum member name or the value itself).
/// </summary>
public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        var target = ResolveParameter(value.GetType(), parameter);
        return target is not null && value.Equals(target);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Only the checked radio writes back; unchecked ones are ignored.
        if (value is true && parameter is not null)
        {
            var resolved = ResolveParameter(targetType, parameter);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return Binding.DoNothing;
    }

    private static object? ResolveParameter(Type enumType, object parameter)
    {
        var type = Nullable.GetUnderlyingType(enumType) ?? enumType;
        if (!type.IsEnum)
        {
            return null;
        }

        if (type.IsInstanceOfType(parameter))
        {
            return parameter;
        }

        var text = parameter.ToString();
        return string.IsNullOrEmpty(text) || !Enum.IsDefined(type, text)
            ? null
            : Enum.Parse(type, text);
    }
}

/// <summary>
/// Returns true when the bound value equals the ConverterParameter (compared by
/// string). Useful for highlighting the active nav item or status pill.
/// </summary>
public sealed class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Returns true when two bound values are equal. Used to light up the active sidebar
/// item by comparing each item's key with the shell's current-page key, avoiding the
/// selection-coercion pitfalls of sharing one <c>Selector.SelectedItem</c> across groups.
/// </summary>
public sealed class EqualityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values is { Length: 2 } && Equals(values[0], values[1]);

    public object[] ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
