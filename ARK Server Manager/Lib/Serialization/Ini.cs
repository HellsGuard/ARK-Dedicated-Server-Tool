using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib.Serialization
{
    class Ini
    {
        class Entry
        {
            public string Key { get; set; }
            public List<string> Values { get; set; }
        }

        class Section
        {
            Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        }

        Dictionary<string, Section> sections = new Dictionary<string, Section>();
    }
}
