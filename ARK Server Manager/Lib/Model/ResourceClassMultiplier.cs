using ARK_Server_Manager.Lib.ViewModel;

namespace ARK_Server_Manager.Lib
{
    public class ResourceClassMultiplier : ClassMultiplier
    {
        public override string GetSortKey()
        {
            return ResourceNameValueConverter.Convert(this.ClassName).ToString();
        }

        public new static ResourceClassMultiplier FromINIValue(string iniValue)
        {
            var newSpawn = new ResourceClassMultiplier();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }
    }
}
