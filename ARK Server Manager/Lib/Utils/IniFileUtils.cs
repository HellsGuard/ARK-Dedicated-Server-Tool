using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib.Utils
{
    public static class IniFileUtils
    {
        #region Windows Methods

        private const char TERMINATOR = '\0';

        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileSection(string section, char[] retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32")]
        private static extern int WritePrivateProfileSection(string section, string data, string filePath);

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        public static string[] IniReadSection(string file, string sectionName)
        {
            const int MAXSECTIONSIZE = 262144;

            var buffer = new char[MAXSECTIONSIZE];

            if (File.Exists(file))
            {
                int i = GetPrivateProfileSection(sectionName, buffer, buffer.Length, file);
            }

            return MultiStringToStringArray(buffer);
        }

        public static string IniReadValue(string file, string sectionName, string keyName, string defaultValue)
        {
            const int MAXVALUESIZE = 16384;

            StringBuilder buffer = new StringBuilder(MAXVALUESIZE);

            if (File.Exists(file))
            {
                int i = GetPrivateProfileString(sectionName, keyName, defaultValue, buffer, buffer.Capacity, file);
            }

            return buffer.ToString();
        }

        public static void IniWriteSection(string file, string sectionName, string[] keysValuePairs)
        {
            EnsureUTF16(file);
            WritePrivateProfileSection(sectionName, StringArrayToMultiString(keysValuePairs), file);
        }

        public static void IniWriteValue(string file, string sectionName, string keyName, string keyValue)
        {
            EnsureUTF16(file);
            WritePrivateProfileString(sectionName, keyName, keyValue, file);
        }

        private static void EnsureUTF16(string filePath)
        {
            var bytes = new byte[2];
            int bytesRead = 0;
            using (var file = File.Open(filePath, FileMode.OpenOrCreate))
            {
                bytesRead = file.Read(bytes, 0, bytes.Length);
            }

            if (bytesRead < 2 || bytes[0] != 0xFF || bytes[1] != 0xFE)
            {
                var tempFilePath = Path.GetTempFileName();
                using (var newFile = File.Create(tempFilePath))
                {
                    newFile.Write(new byte[] { 0xFF, 0xFE }, 0, 2);
                    var sourceText = File.ReadAllLines(filePath);
                    foreach (var line in sourceText)
                    {
                        var utf16le = UnicodeEncoding.Unicode.GetBytes(line);
                        newFile.Write(utf16le, 0, utf16le.Length);
                        var newLine = UnicodeEncoding.Unicode.GetBytes(new[] { '\n' });
                        newFile.Write(newLine, 0, newLine.Length);
                    }
                }

                File.Delete(filePath);
                File.Move(tempFilePath, filePath);
            }
        }

        private static string[] MultiStringToStringArray(char[] multiString)
        {
            if (multiString == null)
                throw new ArgumentNullException("multiString");

            return new string(multiString).Trim(TERMINATOR).Split(TERMINATOR);
        }

        private static string StringArrayToMultiString(string[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            return string.Join(TERMINATOR.ToString(), values);
        }

        #endregion

        #region Handwritten Methods

        /// <summary>
        /// Retrieves all the keys and values for the specified section of an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section in the initialization file.</param>
        /// <returns>A string array containing the key name and value pairs associated with the named section.</returns>
        public static string[] ReadSection(string file, string sectionName)
        {
            if (sectionName == null)
                return new string[0];

            var iniFile = ReadFromFile(file);
            return iniFile?.GetSection(sectionName)?.KeysToStringArray() ?? new string[0];
        }

        /// <summary>
        /// Retrieves a string from the specified section in an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section containing the key name.</param>
        /// <param name="keyName">The name of the key whose associated string is to be retrieved.</param>
        /// <param name="defaultValue">A default string. If the keyName key cannot be found in the initialization file, the default string is returned. If this parameter is NULL, the default is an empty string, "".</param>
        /// <returns></returns>
        public static string ReadValue(string file, string sectionName, string keyName, string defaultValue)
        {
            if (sectionName == null || keyName == null)
                return defaultValue ?? string.Empty;

            var iniFile = ReadFromFile(file);
            return iniFile?.GetSection(sectionName)?.GetKey(keyName)?.KeyValue ?? defaultValue ?? string.Empty;
        }

        /// <summary>
        /// Replaces the keys and values for the specified section in an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section in which data is written.</param>
        /// <param name="keysValuePairs">An array of key names and associated values that are to be written to the named section.</param>
        /// <returns>True if the function succeeds; otherwise False.</returns>
        public static bool WriteSection(string file, string sectionName, string[] keysValuePairs)
        {
            if (sectionName == null)
                return false;

            var iniFile = ReadFromFile(file) ?? new IniFile();

            var result = iniFile.WriteSection(sectionName, keysValuePairs);
            if (!result)
                return false;

            return result = SaveToFile(file, iniFile);
        }

        /// <summary>
        /// Copies a string into the specified section of an initialization file.
        /// </summary>
        /// <param name="file">The name of the initialization file.</param>
        /// <param name="sectionName">The name of the section to which the string will be copied. If the section does not exist, it is created. The name of the section is case-independent; the string can be any combination of uppercase and lowercase letters.</param>
        /// <param name="keyName">The name of the key to be associated with a string. If the key does not exist in the specified section, it is created. If this parameter is NULL, the entire section, including all entries within the section, is deleted.</param>
        /// <param name="keyValue">A null-terminated string to be written to the file. If this parameter is NULL, the key pointed to by the keyName parameter is deleted.</param>
        /// <returns>True if the function succeeds; otherwise False.</returns>
        public static bool WriteValue(string file, string sectionName, string keyName, string keyValue)
        {
            if (sectionName == null)
                return false;

            var iniFile = ReadFromFile(file) ?? new IniFile();

            var result = iniFile.WriteKey(sectionName, keyName, keyValue);
            if (!result)
                return false;

            return result = SaveToFile(file, iniFile);
        }

        public static IniFile ReadFromFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return null;

            if (!File.Exists(file))
                return new IniFile();

            var iniFile = new IniFile();

            using (StreamReader reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    var sectionName = Regex.Match(line, @"(?<=^\[).*(?=\]$)").Value.Trim();

                    var section = iniFile.AddSection(sectionName);
                    if (section != null)
                        continue;

                    iniFile.AddKey(line);
                }

                reader.Close();
            }

            return iniFile;
        }

        public static IniFile ReadString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new IniFile();

            var iniFile = new IniFile();

            var lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                var sectionName = Regex.Match(line, @"(?<=^\[).*(?=\]$)").Value.Trim();

                var section = iniFile.AddSection(sectionName);
                if (section != null)
                    continue;

                iniFile.AddKey(line);
            }

            return iniFile;
        }

        public static bool SaveToFile(string file, IniFile iniFile)
        {
            if (string.IsNullOrWhiteSpace(file) || iniFile == null)
                return false;

            using (StreamWriter writer = new StreamWriter(file, false))
            {
                foreach (var section in iniFile.Sections)
                {
                    writer.WriteLine($"[{section.SectionName}]");

                    foreach (var keyString in section.KeysToStringArray())
                    {
                        writer.WriteLine(keyString);
                    }

                    writer.WriteLine();
                }

                writer.Close();
            }

            return true;
        }

        #endregion
    }

    public class IniFile
    {
        public IniFile()
        {
            Sections = new List<IniSection>();
        }

        public List<IniSection> Sections;

        public IniSection AddSection(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                return null;

            var section = Sections.FirstOrDefault(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
            if (section == null)
            {
                section = new IniSection() { SectionName = sectionName };
                Sections.Add(section);
            }
            return section;
        }

        public IniKey AddKey(string keyName, string keyValue)
        {
            var section = Sections.LastOrDefault();
            if (section == null)
                section = AddSection(string.Empty);

            return section.AddKey(keyName, keyValue);
        }

        public IniKey AddKey(string keyValuePair)
        {
            var section = Sections.LastOrDefault();
            if (section == null)
                return null;

            return section.AddKey(keyValuePair);
        }

        public IniSection GetSection(string sectionName)
        {
            return Sections?.FirstOrDefault(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));
        }

        public IniKey GetKey(string sectionName, string keyName)
        {
            return GetSection(sectionName)?.GetKey(keyName);
        }

        public void RemoveSection(string sectionName)
        {
            var section = GetSection(sectionName);
            RemoveSection(section);
        }

        public void RemoveSection(IniSection section)
        {
            if (Sections.Contains(section))
                Sections.Remove(section);
        }

        public bool WriteSection(string sectionName, string[] keysValuePairs)
        {
            if (sectionName == null)
                return false;

            var result = true;

            // get the section.
            var section = GetSection(sectionName);

            // check if the section exists.
            if (section != null)
            {
                // delete the section.
                RemoveSection(section);
            }

            // create the section.
            section = AddSection(sectionName);

            foreach (var key in keysValuePairs)
            {
                section.AddKey(key);
            }

            return result;
        }

        public bool WriteKey(string sectionName, string keyName, string keyValue)
        {
            if (sectionName == null)
                return false;

            var result = true;

            // get the section.
            var section = GetSection(sectionName);

            // check if the section exists.
            if (section == null)
            {
                // section does not exist, check if keyname is NULL.
                if (keyName == null)
                {
                    // do nothing, the section does not exist and does not need to be removed.
                }
                else
                {
                    // create the section.
                    section = AddSection(sectionName);
                }
            }
            else
            {
                // section does exists, check if the keyname is NULL.
                if (keyName == null)
                {
                    // keyname is NULL, we need to delete the section.
                    RemoveSection(section);

                    // reset the section variable.
                    section = null;
                }
            }

            // check if the section exists.
            if (section != null)
            {
                // get the key.
                var key = section.GetKey(keyName);

                // check if the key exists.
                if (key == null)
                {
                    // key does not exist, check if keyvalue is NULL.
                    if (keyValue == null)
                    {
                        // do nothing, the key does not exist and does not need to be removed.
                    }
                    else
                    {
                        // create the key.
                        key = section.AddKey(keyName, keyValue);
                    }
                }
                else
                {
                    // key does exists, check if the keyvalue is NULL.
                    if (keyValue == null)
                    {
                        // keyvalue is NULL, we need to delete the key and exit.
                        section.RemoveKey(key);

                        // reset the key variable.
                        key = null;
                    }
                    else
                    {
                        // update the keyvalue.
                        key.KeyValue = keyValue;
                    }
                }
            }

            return result;
        }
    }

    public class IniSection
    {
        public IniSection()
        {
            SectionName = string.Empty;
            Keys = new List<IniKey>();
        }

        public string SectionName;
        public List<IniKey> Keys;

        public IniKey AddKey(string keyName, string keyValue)
        {
            var key = new IniKey() { KeyName = keyName, KeyValue = keyValue };
            Keys.Add(key);
            return key;
        }

        //public IniKey AddKey(string keyValuePair)
        //{
        //    var keyName = Regex.Match(keyValuePair, (@"(?<=^\p{Zs}*|])[^]=:]*(?==|:)")).Value.Trim();

        //    if (string.IsNullOrWhiteSpace(keyName))
        //        return null;

        //    var keyValue = Regex.Match(keyValuePair, "(?<==|:)[^;#]*").Value.Trim();

        //    var key = new IniKey() { KeyName = keyName, KeyValue = keyValue };
        //    Keys.Add(key);
        //    return key;
        //}

        public IniKey AddKey(string keyValuePair)
        {
            var parts = keyValuePair?.Split(new[] { '=' }, 2) ?? new string[1];

            if (string.IsNullOrWhiteSpace(parts[0]))
                return null;

            var key = new IniKey() { KeyName = parts[0] };
            if (parts.Length > 1)
                key.KeyValue = parts[1];
            Keys.Add(key);
            return key;
        }

        public IniKey GetKey(string keyName)
        {
            return Keys?.FirstOrDefault(s => s.KeyName.Equals(keyName, StringComparison.OrdinalIgnoreCase));
        }

        public string[] KeysToStringArray()
        {
            return Keys.Select(k => k.ToString()).ToArray();
        }

        public void RemoveKey(string keyName)
        {
            var key = GetKey(keyName);
            RemoveKey(key);
        }

        public void RemoveKey(IniKey key)
        {
            if (Keys.Contains(key))
                Keys.Remove(key);
        }

        public override string ToString()
        {
            return $"[{SectionName}]";
        }
    }

    public class IniKey
    {
        public IniKey()
        {
            KeyName = string.Empty;
            KeyValue = string.Empty;
        }

        public string KeyName;
        public string KeyValue;

        public override string ToString()
        {
            return $"{KeyName}={KeyValue}";
        }
    }
}
