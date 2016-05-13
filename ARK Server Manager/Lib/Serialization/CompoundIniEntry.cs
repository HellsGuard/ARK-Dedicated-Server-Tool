using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib.Serialization
{
    class CompoundIniEntry : IniEntry
    {
        public Dictionary<string, string> SubValues
        {
            get;
            set;
        }

        internal static IniEntry FromIniEntry(IniEntry baseEntry)
        {
            var result = new CompoundIniEntry
            {
                SubValues = GetSubEntries(baseEntry),
                Key = baseEntry.Key,
                Value = baseEntry.Value
            };

            return result;
        }

        private static Dictionary<string, string> GetSubEntries(IniEntry baseEntry)
        {
            var result = new Dictionary<string, string>();
            var entries = baseEntry.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(IniEntry.FromLine);
            foreach(var entry in entries)
            {
                result[entry.Key] = entry.Value;
            }

            return result;
        }
    }
}
