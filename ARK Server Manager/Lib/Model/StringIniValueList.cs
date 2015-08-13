using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class StringIniValueList : IniValueList<string>
    {
        public StringIniValueList(string iniKeyNale, Func<IEnumerable<string>> resetFunc) : base(iniKeyNale, resetFunc, String.Equals, m => m, m => m, m => m)
        {
        }
    }
}
