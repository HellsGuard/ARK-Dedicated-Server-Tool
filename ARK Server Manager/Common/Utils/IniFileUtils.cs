using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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
}
