using System;
using System.Windows.Data;
using System.Globalization;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class AllTrueMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (object value in values)
            {
                if (!(value is bool) || !(bool)value)
                {
                    return false;
                }
            }
            return true;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("AllTrueConverter is a OneWay converter.");
        }
    }
}
