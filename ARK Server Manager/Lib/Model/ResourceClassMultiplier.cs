using ARK_Server_Manager.Lib.ViewModel;

namespace ARK_Server_Manager.Lib
{
    public class ResourceClassMultiplier : ClassMultiplier
    {
        public bool KnownResource
        {
            get
            {
                return GameData.HasResourceForClass(ClassName);
            }
        }

        public new static ResourceClassMultiplier FromINIValue(string iniValue)
        {
            var newSpawn = new ResourceClassMultiplier();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey()
        {
            return ResourceNameValueConverter.Convert(this.ClassName).ToString();
        }

        public override bool ShouldSave()
        {
            if (!KnownResource)
                return true;

            var resource = GameData.GetResourceForClass(ClassName);
            if (resource == null)
                return true;

            return (!resource.Multiplier.Equals(Multiplier));
        }
    }
}
