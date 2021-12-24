using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfVncClient;

public class BooleanToScrollbarVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool b)
        {
            return value;
        }

        return b ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
