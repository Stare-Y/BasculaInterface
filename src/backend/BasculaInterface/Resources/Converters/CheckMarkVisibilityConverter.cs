using System.Globalization;

namespace BasculaInterface.Resources.Converters
{
    public class CheckMarkVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return false;

            bool isLoaded = values[0] is bool b && b;
            bool hasSecondaryTare = values[1] is double;
            bool hasWeight = values[2] is double w && w != 0;

            return !isLoaded && hasSecondaryTare && hasWeight;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
