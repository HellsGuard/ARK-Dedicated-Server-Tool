using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class FloatIniValueArray : IniValueList<float>
    {
        public FloatIniValueArray(string iniKeyName, Func<IEnumerable<float>> resetFunc) : base(iniKeyName, resetFunc, (a, b) => a == b, m => m, ToIniValueInternal, FromIniValueInternal)
        {
            this.Reset();
            this.IsEnabled = true;
        }       

        private static string ToIniValueInternal(float val)
        {
            return val.ToString();
        }

        private static float FromIniValueInternal(string iniVal)
        {
            return float.Parse(iniVal);
        }

        public override bool IsArray => true;
    }
}
