using System;
using System.Globalization;
using System.Windows.Data;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class EngramClassNameToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var strVal = value as string;
                var firstIndex = strVal.IndexOf('_') + 1;
                var length = strVal.LastIndexOf('_') - firstIndex;
                return strVal.Substring(firstIndex, length);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
