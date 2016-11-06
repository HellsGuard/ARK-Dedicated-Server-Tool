using System;
using System.Collections.Generic;
using System.Globalization;

namespace ARK_Server_Manager.Lib
{
    public class IntegerIniValueList : IniValueList<int>
    {
        public IntegerIniValueList(string iniKeyName, Func<IEnumerable<int>> resetFunc) : base(iniKeyName, resetFunc, (a, b) => a == b, m => m, ToIniValueInternal, FromIniValueInternal)
        {
        }

        public override bool IsArray => false;

        private static string ToIniValueInternal(int val)
        {
            return val.ToString(CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }

        private static int FromIniValueInternal(string iniVal)
        {
            return int.Parse(iniVal, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo(StringUtils.DEFAULT_CULTURE_CODE));
        }
    }
}
