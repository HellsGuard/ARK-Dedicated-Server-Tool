namespace ARK_Server_Manager.Lib
{
    public interface IIniSectionCollection
    {
        IIniValuesCollection[] Sections { get; }

        void Add(string sectionName, string[] values);

        void Update();
    }
}
