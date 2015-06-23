using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public enum IniFileSections
    {
        ServerSettings,
        SessionSettings,
        GameSession,
        GameMode,
        MessageOfTheDay,
        MultiHome
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple=true)]
    public class IniFileEntryAttribute : Attribute
    {
        /// <summary>
        /// The section of the ini file.
        /// </summary>
        public IniFileSections Section;

        /// <summary>
        /// The key within the section.
        /// </summary>
        public string Key;

        /// <summary>
        /// If true, the value of booleans will be inverted when read or written.
        /// </summary>
        public bool InvertBoolean;

        /// <summary>
        /// If true, will also write a true boolean value when the underlying field is non-default (or empty for strings), otherwise a false value will be written.
        /// </summary>
        public bool WriteBoolValueIfNonEmpty;

        /// <summary>
        /// Attribute for the IniFile serializer
        /// </summary>
        /// <param name="Section">The section in the ini file.</param>
        /// <param name="Key">The key within the section.</param>
        public IniFileEntryAttribute(IniFileSections Section, string Key)
        {
            this.Section = Section;
            this.Key = Key;
        }
    }

    /// <summary>
    /// Class for reading/writing INI files
    /// </summary>
    /// <remarks>
    /// From http://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C
    /// </remarks>
    class IniFile
    {
        private readonly Dictionary<IniFileSections, string> SectionNames = new Dictionary<IniFileSections, string>()
        {
            { IniFileSections.GameMode, "/script/shootergame.shootergamemode" },
            { IniFileSections.GameSession, "/Script/Engine.GameSession"},
            { IniFileSections.MessageOfTheDay, "MessageOfTheDay" },
            { IniFileSections.MultiHome, "MultiHome" },
            { IniFileSections.ServerSettings, "ServerSettings" },
            { IniFileSections.SessionSettings, "SessionSettings" }
        };

        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public IniFile(string INIPath)
        {
            path = INIPath;
        }

        /// <summary>
        /// Writes the specified object's fields to the INI file, based on the field attributes.
        /// </summary>
        /// <param name="obj"></param>
        public void Serialize(object obj)
        {
            var fields = obj.GetType().GetFields().Where(f => f.IsDefined(typeof(IniFileEntryAttribute), false));
            foreach(var field in fields)
            {
                var attributes = field.GetCustomAttributes(typeof(IniFileEntryAttribute), false);   
                foreach(var attribute in attributes)
                {
                    var attr = attribute as IniFileEntryAttribute;
                    var value = field.GetValue(obj);

                    if(attr.WriteBoolValueIfNonEmpty)
                    {
                        if(value == null)
                        {
                            IniWriteValue(SectionNames[attr.Section], attr.Key, "False");
                        }
                        else
                        {                           
                            if(value is string)
                            {
                                var strValue = value as string;
                                IniWriteValue(SectionNames[attr.Section], attr.Key, String.IsNullOrEmpty(strValue) ? "False" : "True");
                            }
                            else
                            {
                                // Not supported
                                throw new NotSupportedException("Unexpected IniFileEntry value type.");
                            }                            
                        }
                    }
                    else
                    {
                        if (attr.InvertBoolean && value is Boolean)
                        {
                            IniWriteValue(SectionNames[attr.Section], attr.Key, Convert.ToString(!(bool)(value)));
                        }
                        else
                        {
                            IniWriteValue(SectionNames[attr.Section], attr.Key, Convert.ToString(value));
                        }
                    }
                }
            }
        }

        public void Deserialize(object obj)
        {
            var fields = obj.GetType().GetFields().Where(f => f.IsDefined(typeof(IniFileEntryAttribute), false));
            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes(typeof(IniFileEntryAttribute), false);
                foreach (var attribute in attributes)
                {
                    var attr = attribute as IniFileEntryAttribute;
                    var value = field.GetValue(obj);

                    bool hasExtraBoolValue = false;
                    if (attr.WriteBoolValueIfNonEmpty)
                    {
                        var flag = Convert.ToString(IniReadValue(SectionNames[attr.Section], attr.Key));
                        hasExtraBoolValue = true;
                        
                        
                        {
                            if (value is string)
                            {
                                var strValue = value as string;
                                IniWriteValue(SectionNames[attr.Section], attr.Key, String.IsNullOrEmpty(strValue) ? "False" : "True");
                            }
                            else
                            {
                                // Not supported
                                throw new NotSupportedException("Unexpected IniFileEntry value type.");
                            }
                        }
                    }
                    else
                    {
                        if (attr.InvertBoolean && value is Boolean)
                        {
                            IniWriteValue(SectionNames[attr.Section], attr.Key, Convert.ToString(!(bool)(value)));
                        }
                        else
                        {
                            IniWriteValue(SectionNames[attr.Section], attr.Key, Convert.ToString(value));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Section name
        /// <PARAM name="Key"></PARAM>
        /// Key Name
        /// <PARAM name="Value"></PARAM>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this.path);
            return temp.ToString();
        }
    }
}
