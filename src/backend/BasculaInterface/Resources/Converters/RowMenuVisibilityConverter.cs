using System.Globalization;

namespace BasculaInterface.Resources.Converters
{
    /// <summary>
    /// Combines <see cref="Models.WeightEntryDetailRow.CanChangeProductMenu"/> (value) with the
    /// per-platform baseline visibility (parameter, e.g. via `{OnPlatform Android=True, WinUI=False}`)
    /// so the "⋮" row menu never shows when the terminal/mode gate disallows it, regardless of platform.
    /// </summary>
    public class RowMenuVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool canChangeProduct = value is bool v && v;
            bool platformDefault = parameter is bool p && p;

            return canChangeProduct && platformDefault;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
