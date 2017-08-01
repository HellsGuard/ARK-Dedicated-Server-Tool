using Newtonsoft.Json;

namespace ARK_Server_Manager.Lib
{
    public static class JsonUtils
    {
        public static string Serialize<T>(T value)
        {
            if (value == null)
                return string.Empty;

            try
            {
                return JsonConvert.SerializeObject(value, Formatting.Indented);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool Serialize<T>(T value, string filename)
        {
            if (value == null)
                return false;

            try
            {
                var jsonString = Serialize(value);
                System.IO.File.WriteAllText(filename, jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static T Deserialize<T>(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch
            {
                return default(T);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            if (string.IsNullOrEmpty(file) || !System.IO.File.Exists(file))
                return default(T);

            try
            {
                return Deserialize<T>(System.IO.File.ReadAllText(file));
            }
            catch
            {
                return default(T);
            }
        }
    }
}
