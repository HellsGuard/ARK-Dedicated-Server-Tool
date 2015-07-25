using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib.Serialization
{
    public class IniEntry
    {
        public bool IsBlank
        {
            get;
            set;
        }

        public bool IsComment
        {
            get;
            set;
        }

        public string Key
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }

        public static IniEntry FromBlank()
        {
            return new IniEntry { IsBlank = true };
        }
        public static IniEntry FromComment(string line)
        {
            return new IniEntry { IsComment = true, Value = line };
        }
        public static IniEntry FromLine(IniFileStream.Line line)        
        {
            switch (line.Type)
            {
                case IniFileStream.LineType.Blank:
                    return FromBlank();

                case IniFileStream.LineType.Comment:
                    return FromComment(line.Value);

                case IniFileStream.LineType.Value:
                    return FromLine(line.Value);
            }

            throw new Exception("Unexpected line type");
        }

        public static IniEntry FromLine(string line)
        {
            var pair = line.Trim().Split(new[] { '=' }, 2);
            if (pair.Length == 2)
            {
                return new IniEntry { Key = pair[0], Value = pair[1] };
            }
            else
            {
                return new IniEntry { Key = pair[0], Value = String.Empty };
            }
        }

        public virtual string ToLine()
        {
            if(this.IsBlank)
            {
                return String.Empty;
            }

            return String.Format("{0}={1}", this.Key, this.Value);
        }
    }
}
