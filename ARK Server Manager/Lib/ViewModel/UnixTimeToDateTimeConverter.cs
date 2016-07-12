using System;
using System.Globalization;
using System.Windows.Data;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class UnixTimeToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int unixTimestamp = (int)value;
            return ModUtils.UnixTimeStampToDateTime(unixTimestamp).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }
}
