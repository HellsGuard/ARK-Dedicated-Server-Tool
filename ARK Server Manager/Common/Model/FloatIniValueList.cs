using System;
using System.Collections.Generic;
using System.Globalization;

namespace ARK_Server_Manager.Lib
{
    public class FloatIniValueList : IniValueList<float>
    {
        public FloatIniValueList(string iniKeyName, Func<IEnumerable<float>> resetFunc) : 
            base(iniKeyName, resetFunc, (a, b) => a == b, m => m, ToIniValueInternal, FromIniValueInternal)
        {
        }

        public override bool IsArray => false;

        private static string ToIniValueInternal(float val)
        {
            return val.ToString("0.0#########", CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }

        private static float FromIniValueInternal(string iniVal)
        {
            return float.Parse(iniVal, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }
    }
}
