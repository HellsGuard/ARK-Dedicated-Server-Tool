﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class ResourceNameValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static object Convert(object value)
        {
            try
            {
                var strVal = value as string;
                var name = GameData.FriendlyNameForClass(strVal);
                if (!string.IsNullOrWhiteSpace(name) && !name.Equals(strVal))
                    return name;

                var firstIndex = strVal.IndexOf('_');
                var lastIndex = strVal.LastIndexOf('_');
                return strVal.Substring(firstIndex + 1, lastIndex - firstIndex - 1).Replace('_', ' ');
            }
            catch
            {
                return value;
            }
        }
    }
}
