using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ARK_Server_Manager.Lib
{
    public static class StringUtils
    {
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
                convertedVal = ((float)value).ToString("0.0#########", CultureInfo.GetCultureInfo("en-US"));
            else
                convertedVal = Convert.ToString(value, CultureInfo.GetCultureInfo("en-US"));

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
                int.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US"), out intValue);
                property.SetValue(obj, intValue);
            }
            else if (property.PropertyType == typeof(float))
            {
                float floatValue;
                float.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US"), out floatValue);
                property.SetValue(obj, floatValue);
            }
            else
            {
                object convertedValue = Convert.ChangeType(value, property.PropertyType, CultureInfo.GetCultureInfo("en-US"));
                if (convertedValue.GetType() == typeof(String))
                    convertedValue = (convertedValue as string).Trim('"');
                property.SetValue(obj, convertedValue);
            }
        }

        public static bool SetPropertyValueIniFile(string value, object obj, PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool))
            {
                var boolValue = false;
                bool.TryParse(value, out boolValue);
                property.SetValue(obj, boolValue);
                return true;
            }
            else if (property.PropertyType == typeof(int))
            {
                int intValue;
                int.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US"), out intValue);
                property.SetValue(obj, intValue);
                return true;
            }
            else if (property.PropertyType == typeof(float))
            {
                float floatValue;
                float.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US"), out floatValue);
                property.SetValue(obj, floatValue);
                return true;
            }

            return false;
        }
    }
}
