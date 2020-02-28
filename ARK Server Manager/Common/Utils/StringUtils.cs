﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace ARK_Server_Manager.Lib
{
    public static class StringUtils
    {
        public const string DEFAULT_CULTURE_CODE = "en-US";

        public static string GetArkColoredMessage(string message, Color color)
        {
            var r = Math.Round(color.R / 255.0, 2).ToString(CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            var g = Math.Round(color.G / 255.0, 2).ToString(CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            var b = Math.Round(color.B / 255.0, 2).ToString(CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            var a = Math.Round(color.A / 255.0, 2).ToString(CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));

            return $"<RichColor Color=\"{r},{g},{b},{a}\">{message}</>";
        }

        public static string GetPropertyValue(object value, PropertyInfo property)
        {
            string convertedVal;

            if (property.PropertyType == typeof(float))
                convertedVal = ((float)value).ToString("0.0#########", CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            else if (property.PropertyType == typeof(string))
                convertedVal = $"\"{value}\"";
            else
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));

            return convertedVal;
        }

        public static string GetPropertyValue(object value, PropertyInfo property, IniFileEntryAttribute attribute)
        {
            string convertedVal;

            if (property.PropertyType == typeof(float))
                convertedVal = ((float)value).ToString("0.0#########", CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            else if (property.PropertyType == typeof(bool) && attribute.InvertBoolean)
                convertedVal = (!(bool)(value)).ToString(CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            else
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));

            return convertedVal;
        }

        public static void SetPropertyValue(string value, object obj, PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool))
            {
                bool boolValue;
                bool.TryParse(value, out boolValue);
                property.SetValue(obj, boolValue);
            }
            else if (property.PropertyType == typeof(int))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                property.SetValue(obj, intValue);
            }
            else if (property.PropertyType == typeof(float))
            {
                var tempValue = value.Replace("f", "");

                float floatValue;
                float.TryParse(tempValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                property.SetValue(obj, floatValue);
            }
            else if (property.PropertyType.IsSubclassOf(typeof(AggregateIniValue)))
            {
                var field = property.GetValue(obj) as AggregateIniValue;
                field?.InitializeFromINIValue(value);
            }
            else
            {
                var convertedValue = Convert.ChangeType(value, property.PropertyType, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
                if (convertedValue is string)
                    convertedValue = (convertedValue as string).Trim('"');
                property.SetValue(obj, convertedValue);
            }
        }

        public static bool SetPropertyValue(string value, object obj, PropertyInfo property, IniFileEntryAttribute attribute)
        {
            if (property.PropertyType == typeof(bool))
            {
                bool boolValue;
                bool.TryParse(value, out boolValue);
                if (attribute.InvertBoolean)
                    boolValue = !boolValue;
                property.SetValue(obj, boolValue);
                return true;
            }
            if (property.PropertyType == typeof(int))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                property.SetValue(obj, intValue);
                return true;
            }
            if (property.PropertyType == typeof(float))
            {
                var tempValue = value.Replace("f", "");

                float floatValue;
                float.TryParse(tempValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                property.SetValue(obj, floatValue);
                return true;
            }
            if (property.PropertyType.IsSubclassOf(typeof(AggregateIniValue)))
            {
                var field = property.GetValue(obj) as AggregateIniValue;
                field?.InitializeFromINIValue(value);
                return true;
            }

            return false;
        }
    }
}
