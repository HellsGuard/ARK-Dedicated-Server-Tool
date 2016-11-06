using System;
using System.Collections.Generic;
using System.Globalization;

namespace ARK_Server_Manager.Lib
{
    public class FloatIniValueArray : IniValueList<float>
    {
        public FloatIniValueArray(string iniKeyName, Func<IEnumerable<float>> resetFunc) : base(iniKeyName, resetFunc, (a, b) => a == b, m => m, ToIniValueInternal, FromIniValueInternal)
        {
            this.Reset();
            this.IsEnabled = false;
        }

        public override bool IsArray => true;

        private static string ToIniValueInternal(float val)
        {
            return val.ToString("0.0#########", CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }

        private static float FromIniValueInternal(string iniVal)
        {
            var tempValue = iniVal.Replace("f", "");
            return float.Parse(tempValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }
    }
}
