using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib.Serialization
{
#if false
    public class ValueArray<T> : List<T>
    {
        private string keyNameRoot;
        private Func<string, T> stringToValueConverter;


        public IEnumerable<string> ToIniValues()
        {
            int index = 0;
            foreach( var item in this)
            {
                yield return $"{keyNameRoot}[{index}={item}";
                index++;
            }
        }

        public void FromIniValues(IEnumerable<string> iniValues)
        {
            //this.Clear();
            //foreach(var entry in iniValues)
            //{
            //    var match = Regex.Match(entry, @"(?<root>[^[]*)\[(?<id>[0-9]*)]\w*=\w*(?<value>.*)");
            //    if(match.Success)
            //    {
            //        var root = match.Groups["root"].Value;
            //        var index = Int32.Parse(match.Groups["id"].Value);
            //        var value = stringToValueConverter(match.Groups["value"].Value);

            //        if (root.Equals(this.keyNameRoot, StringComparison.OrdinalIgnoreCase))
            //        {
            //            result[index] = value;
            //        }
            //    }
            //}
        }
    }
#endif
}
