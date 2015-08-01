using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib.Serialization
{
    public class IniFieldDefinition
    {
        public string FieldName
        {
            get;
            set;
        }

        public bool IsMultiLine
        {
            get;
            set;
        }

        public bool IsCompound
        {
            get;
            set;
        }
    }

    public class IniSectionDefinition
    {
        public IniSectionDefinition(string name)
        {
            this.SectionName = name;
            this.Fields = new Dictionary<string, IniFieldDefinition>();
        }

        public string SectionName
        {
            get;
            set;
        }

        public Dictionary<string, IniFieldDefinition> Fields
        {
            get;
            set;
        }
    }

    public class IniDefinition
    {
        public IniDefinition()
        {
            this.Sections = new Dictionary<string, IniSectionDefinition>();
        }

        public Dictionary<string, IniSectionDefinition> Sections
        {
            get;
            set;
        }
    }
}
