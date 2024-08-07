using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfVncClient;

public class BooleanVisibilityConverterInvertable : IValueConverter
{
    public bool IsInverted { get; set; }

    /// <summary>
    ///     Convert bool or Nullable&lt;bool&gt; to Visibility
    /// </summary>
    /// <param name="value">bool or Nullable&lt;bool&gt;</param>
    /// <param name="targetType">Visibility</param>
    /// <param name="parameter">null</param>
    /// <param name="culture">null</param>
    /// <returns>Visible or Collapsed</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool bValue = value switch {
            bool b => b,
            null   => false,
            var _  => true,
        };

        return bValue ^ IsInverted ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    ///     Convert Visibility to boolean
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return (visibility == Visibility.Visible) ^ IsInverted;
        }

        return IsInverted;
    }
}
