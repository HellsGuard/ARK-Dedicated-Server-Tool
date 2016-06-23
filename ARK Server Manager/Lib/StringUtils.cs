using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ARK_Server_Manager.Lib
{
    public static class StringUtils
    {
        public const string DEFAULT_CULTURE_CODE = "en-US";

        public static List<string> SplitIncludingDelimiters(string input, string[] delimiters)
        {
            List<string> result = new List<string>();

            int[] nextPosition = delimiters.SelectMany(d => AllIndexesOf(input, d)).ToArray();
            Array.Sort(nextPosition);
            Array.Reverse(nextPosition);

            int lastPos = input.Length;
            foreach (var pos in nextPosition)
            {
                var value = input.Substring(pos, lastPos - pos);
                result.Add(value);

                lastPos = pos;
            }

            return result;
        }

        private static IEnumerable<int> AllIndexesOf(string input, string delimiter)
        {
            int minIndex = input.IndexOf(delimiter);
            while (minIndex != -1)
            {
                yield return minIndex;
                minIndex = input.IndexOf(delimiter, minIndex + delimiter.Length);
            }
        }

        public static string GetPropertyValue(object value, PropertyInfo property)
        {
            string convertedVal;

            if (property.PropertyType == typeof(float))
                convertedVal = ((float)value).ToString("0.0#########", CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
            else
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));

            return convertedVal;
        }

        public static string GetPropertyValue(object value, PropertyInfo property, IniFileEntryAttribute attribute)
        {
            string convertedVal;

            if (property.PropertyType == typeof(float))
                convertedVal = ((float)value).ToString("0.0#########", CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            if (property.PropertyType == typeof(bool) && attribute.InvertBoolean)
                convertedVal = (!(bool)(value)).ToString(CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
            else
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));

            return convertedVal;
        }

        public static void SetPropertyValue(string value, object obj, PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool))
            {
                var boolValue = false;
                bool.TryParse(value, out boolValue);
                property.SetValue(obj, boolValue);
            }
            else if (property.PropertyType == typeof(int))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                property.SetValue(obj, intValue);
            }
            else if (property.PropertyType == typeof(float))
            {
                float floatValue;
                float.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                property.SetValue(obj, floatValue);
            }
            else
            {
                object convertedValue = Convert.ChangeType(value, property.PropertyType, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE));
                if (convertedValue.GetType() == typeof(String))
                    convertedValue = (convertedValue as string).Trim('"');
                property.SetValue(obj, convertedValue);
            }
        }

        public static bool SetPropertyValue(string value, object obj, PropertyInfo property, IniFileEntryAttribute attribute)
        {
            if (property.PropertyType == typeof(bool))
            {
                var boolValue = false;
                bool.TryParse(value, out boolValue);
                if (attribute.InvertBoolean)
                    boolValue = !boolValue;
                property.SetValue(obj, boolValue);
                return true;
            }
            else if (property.PropertyType == typeof(int))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out intValue);
                property.SetValue(obj, intValue);
                return true;
            }
            else if (property.PropertyType == typeof(float))
            {
                float floatValue;
                float.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo(DEFAULT_CULTURE_CODE), out floatValue);
                property.SetValue(obj, floatValue);
                return true;
            }

            return false;
        }
    }
}
