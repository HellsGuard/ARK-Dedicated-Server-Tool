using System.Collections.Generic;

namespace ARK_Server_Manager.Lib
{
    public interface IIniValuesCollection
    {
        string IniCollectionKey { get; }
        bool IsArray { get; }
        bool IsEnabled { get; set; }

        void FromIniValues(IEnumerable<string> values);
        IEnumerable<string> ToIniValues();
    }
}
