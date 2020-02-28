﻿using System;
using System.Collections.Generic;

namespace ARK_Server_Manager.Lib
{
    public class StringIniValueList : IniValueList<string>
    {
        public StringIniValueList(string iniKeyName, Func<IEnumerable<string>> resetFunc) : 
            base(iniKeyName, resetFunc, string.Equals, m => m, ToIniValueInternal, FromIniValueInternal)
        {
        }

        public override bool IsArray => false;

        private static string ToIniValueInternal(string val)
        {
            return "\"" + val + "\"";            
        }

        private static string FromIniValueInternal(string iniVal)
        {
            return iniVal.Trim('"');            
        }
    }
}
