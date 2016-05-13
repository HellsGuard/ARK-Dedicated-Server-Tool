using System;
using System.Collections.Generic;
using System.IO;

namespace ARK_Server_Manager.Lib.Serialization
{
    public class IniFileStream
    {
        public enum LineType
        {
            SectionHeader,
            Value,
            Blank,
            Comment,
        };

        public struct Line
        {
            public LineType Type
            {
                get;
                set;
            }

            public string Value
            {
                get;
                set;
            }
        }

        private StreamReader stream;

        public IEnumerable<Line> GetLines()
        {
            using (stream)
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine().Trim();
                    if (String.IsNullOrWhiteSpace(line))
                    {
                        yield return new Line { Type = LineType.Blank };
                    }
                    else if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        yield return new Line { Type = LineType.SectionHeader, Value = line };
                    }
                    else if (line.StartsWith("#") || line.StartsWith(";"))
                    {
                        yield return new Line { Type = LineType.Comment, Value = line };
                    }
                    else
                    {
                        yield return new Line { Type = LineType.Value, Value = line };
                    }
                }
            }
        }

        public static IniFileStream FromFile(string file)
        {
            var stream = File.OpenText(file);
            return new IniFileStream { stream = stream };
        }
    }
}
