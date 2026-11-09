using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NGEOS.Desktop.UI.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool flag && flag;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class CurrencyFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
                return $"Rs. {d:N0}";
            if (value is double db)
                return $"Rs. {db:N0}";
            return "Rs. 0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value?.ToString() ?? "";
            if (str.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("Success", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromRgb(22, 163, 74)); // Green
            }
            if (str.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromRgb(217, 119, 6)); // Amber / Warning
            }
            if (str.Equals("Refunded", StringComparison.OrdinalIgnoreCase) ||
                str.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Red / Danger
            }

            return new SolidColorBrush(Color.FromRgb(100, 116, 139)); // Slate
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToStringConverter : IValueConverter
    {
        public string TrueText { get; set; } = "Active";
        public string FalseText { get; set; } = "Inactive";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueText : FalseText;
            return FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
