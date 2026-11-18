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

    public class StringToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasText = !string.IsNullOrWhiteSpace(value as string);
            if (Invert) hasText = !hasText;
            return hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringMatchToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strVal = value?.ToString() ?? string.Empty;
            var paramVal = parameter?.ToString() ?? string.Empty;
            bool matches = string.Equals(strVal, paramVal, StringComparison.OrdinalIgnoreCase);
            if (Invert) matches = !matches;
            return matches ? Visibility.Visible : Visibility.Collapsed;
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

    public class StringMatchToBrushConverter : IValueConverter
    {
        public Brush MatchBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0xF2, 0x5C, 0x05));
        public Brush DefaultBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return DefaultBrush;
            return value.ToString()!.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) 
                ? MatchBrush 
                : DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringMatchToBackgroundConverter : IValueConverter
    {
        public Brush MatchBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
        public Brush DefaultBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x10, 0x1A, 0x2E));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return DefaultBrush;
            return value.ToString()!.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) 
                ? MatchBrush 
                : DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
