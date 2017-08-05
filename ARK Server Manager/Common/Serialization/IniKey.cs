namespace ARK_Server_Manager.Lib.Utils
{
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
