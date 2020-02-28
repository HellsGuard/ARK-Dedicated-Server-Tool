using System;
using System.Globalization;
using System.Windows.Data;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class InstalledVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = value as string;
            if (string.IsNullOrWhiteSpace(version))
                return "0.0 (start server to update)";

            if (version.Equals("0.0"))
                return "0.0 (start server to update)";

            return version;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
