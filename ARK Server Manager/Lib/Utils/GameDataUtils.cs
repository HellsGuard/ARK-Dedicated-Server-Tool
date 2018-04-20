using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace ARK_Server_Manager.Lib.Utils
{
    public static class GameDataUtils
    {
        public static string DataFolder = Path.Combine(Environment.CurrentDirectory, Config.Default.GameDataDir);

        public static void ReadAllData(out BaseGameData data, string dataFolder = null, bool isUserData = false)
        {
            data = new BaseGameData();

            if (!string.IsNullOrWhiteSpace(dataFolder))
                DataFolder = dataFolder;

            if (!Directory.Exists(DataFolder))
                return;

            foreach (var file in Directory.GetFiles(DataFolder, $"*{Config.Default.GameDataExtension}", SearchOption.TopDirectoryOnly))
            {
                var fileData = BaseGameData.Load(file, isUserData);
                data.Creatures.AddRange(fileData.Creatures);
                data.Engrams.AddRange(fileData.Engrams);
                data.Items.AddRange(fileData.Items);
                data.MapSpawners.AddRange(fileData.MapSpawners);
                data.SupplyCrates.AddRange(fileData.SupplyCrates);
                data.Inventories.AddRange(fileData.Inventories);
                data.GameMaps.AddRange(fileData.GameMaps);
                data.Mods.AddRange(fileData.Mods);
                data.PlayerLevels.AddRange(fileData.PlayerLevels);
                data.CreatureLevels.AddRange(fileData.CreatureLevels);
            }
        }

        public static void WriteAllData(string dataFolder = null)
        {
            if (!string.IsNullOrWhiteSpace(dataFolder))
                DataFolder = dataFolder;

            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            var data = new Dictionary<string, BaseGameData>();

            WriteCreatureData(data);
            WriteEngramData(data);
            WriteItemData(data);
            WriteMapSpawnerData(data);
            WriteSupplyCrateData(data);
            WriteInventoryData(data);
            WriteGameMapData(data);
            WriteTotalConversionData(data);
            WritePlayerLevelData(data);
            WriteCreatureLevelData(data);

            foreach (var kvp in data)
            {
                var dataFile = Path.Combine(DataFolder, $"{kvp.Key}{Config.Default.GameDataExtension}");
                kvp.Value.Save(dataFile);
            }
        }

        private static void WriteCreatureData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetDinoSpawns();
            foreach (var item in list)
            {
                if (!data.ContainsKey(item.ArkApplication.ToString()))
                {
                    data.Add(item.ArkApplication.ToString(), new BaseGameData());
                }

                var dataItem = new CreatureDataItem
                {
                    ClassName = item.ClassName,
                    Description = item.DisplayName,
                    Mod = item.ArkApplication.ToString(),
                    NameTag = item.DinoNameTag,
                    IsSpawnable = GameData.IsSpawnableForClass(item.ClassName),
                    IsTameable = GameData.IsTameableForClass(item.ClassName),
                    ArkApplication = item.ArkApplication,
                };

                data[item.ArkApplication.ToString()].Creatures.Add(dataItem);
            }
        }

        private static void WriteEngramData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetStandardEngramOverrides();
            foreach (var item in list)
            {
                if (!data.ContainsKey(item.ArkApplication.ToString()))
                {
                    data.Add(item.ArkApplication.ToString(), new BaseGameData());
                }

                var dataItem = new EngramDataItem
                {
                    ClassName = item.EngramClassName,
                    Description = item.DisplayName,
                    Mod = item.ArkApplication.ToString(),
                    Level = item.EngramLevelRequirement,
                    Points = item.EngramPointsCost,
                    IsTekGram = item.IsTekgram,
                    ArkApplication = item.ArkApplication,
                };

                data[item.ArkApplication.ToString()].Engrams.Add(dataItem);
            }
        }

        private static void WriteItemData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetStandardPrimalItems();
            foreach (var item in list)
            {
                if (!data.ContainsKey(item.ArkApplication.ToString()))
                {
                    data.Add(item.ArkApplication.ToString(), new BaseGameData());
                }

                var dataItem = new ItemDataItem
                {
                    ClassName = item.ClassName,
                    Description = item.DisplayName,
                    Mod = item.ArkApplication.ToString(),
                    Category = item.Category,
                    IsHarvestable = GameData.HasResourceForClass(item.ClassName),
                    ArkApplication = item.ArkApplication,
                };

                data[item.ArkApplication.ToString()].Items.Add(dataItem);
            }
        }

        private static void WriteMapSpawnerData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetStandardMapSpawners();
            foreach (var item in list)
            {
                var arkApplication = ArkApplication.SurvivalEvolved.ToString();
                if (item.ClassName.StartsWith("SE_"))
                    arkApplication = ArkApplication.ScorchedEarth.ToString();
                else if (item.ClassName.EndsWith("PGM_C"))
                    arkApplication = "PGM";
                else if (item.ClassName.Contains("_Ragnarok_"))
                    arkApplication = ArkApplication.Ragnarok.ToString();
                else if (item.ClassName.StartsWith("AB_"))
                    arkApplication = ArkApplication.Aberration.ToString();
                else if (!item.ClassName.StartsWith("DinoSpawnEntries"))
                    arkApplication = "TheCenter";

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new MapSpawnerDataItem
                {
                    ClassName = item.ClassName,
                    Description = item.DisplayName,
                    Mod = arkApplication,
                };

                data[arkApplication].MapSpawners.Add(dataItem);
            }
        }

        private static void WriteSupplyCrateData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetStandardSupplyCrates();
            foreach (var item in list)
            {
                if (item.ClassName.StartsWith("DinoDropInventoryComponent_"))
                    continue;

                var arkApplication = ArkApplication.SurvivalEvolved.ToString();
                if (item.ClassName.EndsWith("_ScorchedEarth_C"))
                    arkApplication = ArkApplication.ScorchedEarth.ToString();
                else if (item.ClassName.EndsWith("_SE_C"))
                    arkApplication = ArkApplication.ScorchedEarth.ToString();
                else if (item.ClassName.Contains("_Aberration_"))
                    arkApplication = ArkApplication.Aberration.ToString();
                else if (item.ClassName.Contains("_Aberrant_"))
                    arkApplication = ArkApplication.Aberration.ToString();
                else if (item.ClassName.EndsWith("_AB_C"))
                    arkApplication = ArkApplication.Aberration.ToString();

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new SupplyCrateDataItem
                {
                    ClassName = item.ClassName,
                    Description = item.DisplayName,
                    Mod = arkApplication,
                };

                data[arkApplication].SupplyCrates.Add(dataItem);
            }
        }

        private static void WriteInventoryData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetStandardSupplyCrates();
            foreach (var item in list)
            {
                if (!item.ClassName.StartsWith("DinoDropInventoryComponent_"))
                    continue;

                var arkApplication = ArkApplication.SurvivalEvolved.ToString();
                if (item.ClassName.Contains("_BossManticore_"))
                    arkApplication = ArkApplication.ScorchedEarth.ToString();
                else if (item.ClassName.EndsWith("_TheCenter_C"))
                    arkApplication = "TheCenter";

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new InventoryDataItem
                {
                    ClassName = item.ClassName,
                    Description = item.DisplayName,
                    Mod = arkApplication,
                };

                data[arkApplication].Inventories.Add(dataItem);
            }
        }

        private static void WriteGameMapData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetGameMaps();
            foreach (var item in list)
            {
                var arkApplication = ArkApplication.SurvivalEvolved.ToString();

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new GameMapDataItem
                {
                    ClassName = item.ValueMember,
                    Description = item.DisplayMember,
                    Mod = arkApplication,
                    IsSotF = false,
                };

                data[arkApplication].GameMaps.Add(dataItem);
            }

            list = GameData.GetGameMapsSotF();
            foreach (var item in list)
            {
                var arkApplication = ArkApplication.SurvivalEvolved.ToString();

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new GameMapDataItem
                {
                    ClassName = item.ValueMember,
                    Description = item.DisplayMember,
                    Mod = arkApplication,
                    IsSotF = true,
                };

                data[arkApplication].GameMaps.Add(dataItem);
            }
        }

        private static void WriteTotalConversionData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.GetTotalConversions();
            foreach (var item in list)
            {
                var arkApplication = ArkApplication.SurvivalEvolved.ToString();

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new TotalConversionDataItem
                {
                    ClassName = item.ValueMember,
                    Description = item.DisplayMember,
                    Mod = arkApplication,
                    IsSotF = false,
                };

                data[arkApplication].Mods.Add(dataItem);
            }

            list = GameData.GetTotalConversionsSotF();
            foreach (var item in list)
            {
                var arkApplication = ArkApplication.SurvivalEvolved.ToString();

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new TotalConversionDataItem
                {
                    ClassName = item.ValueMember,
                    Description = item.DisplayMember,
                    Mod = arkApplication,
                    IsSotF = true,
                };

                data[arkApplication].Mods.Add(dataItem);
            }
        }

        private static void WritePlayerLevelData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.LevelProgressionPlayerOfficial;
            foreach (var item in list)
            {
                var arkApplication = ArkApplication.SurvivalEvolved.ToString();

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new PlayerLevelDataItem
                {
                    XPRequired = item.XPRequired,
                    EngramPoints = item.EngramPoints,
                };

                data[arkApplication].PlayerLevels.Add(dataItem);
            }
        }

        private static void WriteCreatureLevelData(Dictionary<string, BaseGameData> data)
        {
            var list = GameData.LevelProgressionDinoOfficial;
            foreach (var item in list)
            {
                var arkApplication = ArkApplication.SurvivalEvolved.ToString();

                if (!data.ContainsKey(arkApplication))
                {
                    data.Add(arkApplication, new BaseGameData());
                }

                var dataItem = new CreatureLevelDataItem
                {
                    XPRequired = item.XPRequired,
                };

                data[arkApplication].CreatureLevels.Add(dataItem);
            }
        }
    }

    [DataContract]
    public class BaseGameData
    {
        [DataMember]
        public string Version = "1.0.0";
        [DataMember]
        public DateTime Created = DateTime.UtcNow;
        [DataMember]
        public string Color = "White";

        [DataMember(IsRequired = false)]
        public List<CreatureDataItem> Creatures = new List<CreatureDataItem>();

        [DataMember(IsRequired = false)]
        public List<EngramDataItem> Engrams = new List<EngramDataItem>();

        [DataMember(IsRequired = false)]
        public List<ItemDataItem> Items = new List<ItemDataItem>();

        [DataMember(IsRequired = false)]
        public List<MapSpawnerDataItem> MapSpawners = new List<MapSpawnerDataItem>();

        [DataMember(IsRequired = false)]
        public List<SupplyCrateDataItem> SupplyCrates = new List<SupplyCrateDataItem>();

        [DataMember(IsRequired = false)]
        public List<InventoryDataItem> Inventories = new List<InventoryDataItem>();

        [DataMember(IsRequired = false)]
        public List<GameMapDataItem> GameMaps = new List<GameMapDataItem>();

        [DataMember(IsRequired = false)]
        public List<TotalConversionDataItem> Mods = new List<TotalConversionDataItem>();

        [DataMember(IsRequired = false)]
        public List<PlayerLevelDataItem> PlayerLevels = new List<PlayerLevelDataItem>();

        [DataMember(IsRequired = false)]
        public List<CreatureLevelDataItem> CreatureLevels = new List<CreatureLevelDataItem>();

        public static BaseGameData Load(string file, bool isUserData)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return null;

            var data = JsonUtils.DeserializeFromFile<BaseGameData>(file);
            data.Creatures.ForEach(c => c.IsUserData = isUserData);
            data.Engrams.ForEach(c => c.IsUserData = isUserData);
            data.Items.ForEach(c => c.IsUserData = isUserData);
            data.MapSpawners.ForEach(c => c.IsUserData = isUserData);
            data.SupplyCrates.ForEach(c => c.IsUserData = isUserData);
            data.Inventories.ForEach(c => c.IsUserData = isUserData);
            data.GameMaps.ForEach(c => c.IsUserData = isUserData);
            data.Mods.ForEach(c => c.IsUserData = isUserData);
            return data;
        }

        public bool Save(string file)
        {
            var folder = Path.GetDirectoryName(file);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return JsonUtils.Serialize(this, file);
        }
    }

    [DataContract]
    public class BaseDataItem
    {
        [DataMember]
        public string ClassName = string.Empty;
        [DataMember]
        public string Description = string.Empty;
        [DataMember]
        public string Mod = string.Empty;

        public bool IsUserData = false;
    }

    [DataContract]
    public class CreatureDataItem : BaseDataItem
    {
        [DataMember]
        public string NameTag = string.Empty;
        [DataMember]
        public bool IsSpawnable = false;
        [DataMember(Name = "IsTameable")]
        public string IsTameableString
        {
            get
            {
                return ArkApplication.ToString();
            }
            set
            {
                if (!Enum.TryParse(value, true, out IsTameable))
                    IsTameable = DinoTamable.False;
            }
        }
        [DataMember(Name = "ArkApplication")]
        public string ArkApplicationString
        {
            get
            {
                return ArkApplication.ToString();
            }
            set
            {
                if (!Enum.TryParse(value, true, out ArkApplication))
                    ArkApplication = ArkApplication.Unknown;
            }
        }

        public DinoTamable IsTameable = DinoTamable.False;
        public ArkApplication ArkApplication = ArkApplication.Unknown;
    }

    [DataContract]
    public class EngramDataItem : BaseDataItem
    {
        [DataMember]
        public int Level = 0;
        [DataMember]
        public int Points = 0;
        [DataMember]
        public bool IsTekGram = false;
        [DataMember(Name = "ArkApplication")]
        public string ArkApplicationString
        {
            get
            {
                return ArkApplication.ToString();
            }
            set
            {
                if (!Enum.TryParse(value, true, out ArkApplication))
                    ArkApplication = ArkApplication.Unknown;
            }
        }

        public ArkApplication ArkApplication = ArkApplication.Unknown;
    }

    [DataContract]
    public class ItemDataItem : BaseDataItem
    {
        [DataMember]
        public string Category = string.Empty;
        [DataMember]
        public bool IsHarvestable = false;
        [DataMember(Name = "ArkApplication")]
        public string ArkApplicationString
        {
            get
            {
                return ArkApplication.ToString();
            }
            set
            {
                if (!Enum.TryParse(value, true, out ArkApplication))
                    ArkApplication = ArkApplication.Unknown;
            }
        }

        public ArkApplication ArkApplication = ArkApplication.Unknown;
    }

    [DataContract]
    public class MapSpawnerDataItem : BaseDataItem
    {
    }

    [DataContract]
    public class SupplyCrateDataItem : BaseDataItem
    {
    }

    [DataContract]
    public class InventoryDataItem : BaseDataItem
    {
    }

    [DataContract]
    public class GameMapDataItem : BaseDataItem
    {
        [DataMember]
        public bool IsSotF = false;
    }

    [DataContract]
    public class TotalConversionDataItem : BaseDataItem
    {
        [DataMember]
        public bool IsSotF = false;
    }

    [DataContract]
    public class PlayerLevelDataItem
    {
        [DataMember]
        public int XPRequired = 0;
        [DataMember]
        public int EngramPoints = 0;
    }

    [DataContract]
    public class CreatureLevelDataItem
    {
        [DataMember]
        public int XPRequired = 0;
    }
}
