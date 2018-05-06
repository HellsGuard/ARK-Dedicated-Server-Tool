using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ARK_Server_Manager.Lib.Utils;
using ARK_Server_Manager.Lib.ViewModel;
using WPFSharp.Globalizer;

namespace ARK_Server_Manager.Lib
{
    [DefaultValue(SurvivalEvolved)]
    public enum ArkApplication
    {
        /// <summary>
        /// All has been added only for filter selection.
        /// </summary>
        All = 0,
        SurvivalEvolved,
        PrimitivePlus,
        ScorchedEarth,
        Ragnarok,
        Aberration,
        Unknown,
    }

    [DefaultValue(False)]
    public enum DinoTamable
    {
        False,
        True,
        ByBreeding,
    }

    public static class GameData
    {
        public static int DefaultMaxExperiencePointsDino = 10;
        public static int DefaultMaxExperiencePointsPlayer = 5;

        private static BaseGameData gameData = null;

        public static void Initialize()
        {
            // read static game data
            GameDataUtils.ReadAllData(out gameData);

            // read user game data
            var dataFolder = System.IO.Path.Combine(Config.Default.DataDir, Config.Default.GameDataDir);
            GameDataUtils.ReadAllData(out BaseGameData userGameData, dataFolder, true);

            // creatures
            gameData.Creatures.AddRange(userGameData.Creatures);

            dinoSpawns = gameData.Creatures.ConvertAll(item => new DinoSpawn { ClassName = item.ClassName, Mod = item.Mod, KnownDino = true, DinoNameTag = item.NameTag, ArkApplication = item.ArkApplication }).ToArray();
            dinoMultipliers = gameData.Creatures.ConvertAll(item => new ClassMultiplier { ClassName = item.ClassName }).ToArray();

            // engrams
            gameData.Engrams.AddRange(userGameData.Engrams);

            engrams = gameData.Engrams.ConvertAll(item => new EngramEntry { EngramClassName = item.ClassName, Mod = item.Mod, KnownEngram = true, EngramLevelRequirement = item.Level, EngramPointsCost = item.Points, IsTekgram = item.IsTekGram, ArkApplication = item.ArkApplication }).ToArray();

            // items
            gameData.Items.AddRange(userGameData.Items);

            items = gameData.Items.ConvertAll(item => new PrimalItem { ClassName = item.ClassName, Mod = item.Mod, KnownItem = true, Category = item.Category, ArkApplication = item.ArkApplication }).ToArray();

            // resources
            resourceMultipliers = gameData.Items.Where(item => item.IsHarvestable).ToList().ConvertAll(item => new ResourceClassMultiplier { ClassName = item.ClassName, Mod = item.Mod, KnownResource = true, ArkApplication = item.ArkApplication }).ToArray();

            // map spawners
            gameData.MapSpawners.AddRange(userGameData.MapSpawners);

            mapSpawners = gameData.MapSpawners.ConvertAll(item => new MapSpawner { ClassName = item.ClassName, Mod = item.Mod, KnownSpawner = true }).ToArray();

            // supply crates
            gameData.SupplyCrates.AddRange(userGameData.SupplyCrates);

            var crates = gameData.SupplyCrates.ConvertAll(item => new SupplyCrate { ClassName = item.ClassName, Mod = item.Mod, KnownSupplyCrate = true });

            // inventories
            gameData.Inventories.AddRange(userGameData.Inventories);

            crates.AddRange(gameData.Inventories.ConvertAll(item => new SupplyCrate { ClassName = item.ClassName, Mod = item.Mod, KnownSupplyCrate = true }));

            supplyCrates = crates.ToArray();

            // game maps
            gameData.GameMaps.AddRange(userGameData.GameMaps);

            if (gameData.GameMaps.Count > 0)
            {
                var maps1 = gameMaps.ToList();
                maps1.AddRange(gameData.GameMaps.Where(item => !item.IsSotF).ToList().ConvertAll(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));
                var maps2 = gameMapsSotF.ToList();
                maps2.AddRange(gameData.GameMaps.Where(item => item.IsSotF).ToList().ConvertAll(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));

                gameMaps = maps1.ToArray();
                gameMapsSotF = maps2.ToArray();
            }

            // total conversion mods
            gameData.Mods.AddRange(userGameData.Mods);

            if (gameData.Mods.Count > 0)
            {
                var mods1 = totalConversions.ToList();
                mods1.AddRange(gameData.Mods.Where(item => !item.IsSotF).ToList().ConvertAll(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));
                var mods2 = totalConversionsSotF.ToList();
                mods2.AddRange(gameData.Mods.Where(item => item.IsSotF).ToList().ConvertAll(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));

                totalConversions = mods1.ToArray();
                totalConversionsSotF = mods2.ToArray();
            }

            // creature levels
            if (userGameData.CreatureLevels.Count > 0)
                gameData.CreatureLevels = userGameData.CreatureLevels;

            if (gameData.CreatureLevels.Count > 0)
            {
                levelsDino = gameData.CreatureLevels.ConvertAll(item => new Level { XPRequired = item.XPRequired }).ToArray();
                DefaultMaxExperiencePointsDino = levelsDino.Max(l => l.XPRequired) + 1;
            }

            // player levels
            if (userGameData.PlayerLevels.Count > 0)
                gameData.PlayerLevels = userGameData.PlayerLevels;

            if (gameData.PlayerLevels.Count > 0)
            {
                levelsPlayer = gameData.PlayerLevels.ConvertAll(item => new Level { EngramPoints = item.EngramPoints, XPRequired = item.XPRequired }).ToArray();
                DefaultMaxExperiencePointsPlayer = levelsPlayer.Max(l => l.XPRequired) + 1;
            }

            // branches
            gameData.Branches.AddRange(userGameData.Branches);

            if (gameData.Branches.Count > 0)
            {
                var branches1 = branches.ToList();
                branches1.AddRange(gameData.Branches.Where(item => !item.IsSotF).ToList().ConvertAll(item => new ComboBoxItem { ValueMember = item.BranchName, DisplayMember = item.Description }));
                var branches2 = branchesSotF.ToList();
                branches2.AddRange(gameData.Branches.Where(item => item.IsSotF).ToList().ConvertAll(item => new ComboBoxItem { ValueMember = item.BranchName, DisplayMember = item.Description }));

                branches = branches1.ToArray();
                branchesSotF = branches2.ToArray();
            }
        }

        public static string FriendlyNameForClass(string className, bool returnNullIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? (returnNullIfNotFound ? null : string.Empty) : GlobalizedApplication.Instance.GetResourceString(className) ?? (returnNullIfNotFound ? null : className);

        #region Creatures
        private static DinoSpawn[] dinoSpawns = new DinoSpawn[0];

        public static IEnumerable<DinoSpawn> GetDinoSpawns() => dinoSpawns.Select(d => d.Duplicate<DinoSpawn>());

        public static IEnumerable<NPCReplacement> GetNPCReplacements() => dinoSpawns.Select(d => new NPCReplacement() { FromClassName = d.ClassName, ToClassName = d.ClassName });

        public static bool IsSpawnableForClass(string className) => gameData?.Creatures?.FirstOrDefault(c => c.ClassName.Equals(className))?.IsSpawnable ?? true;

        public static DinoTamable IsTameableForClass(string className) => gameData?.Creatures?.FirstOrDefault(c => c.ClassName.Equals(className))?.IsTameable ?? DinoTamable.True;

        public static string NameTagForClass(string className) => gameData?.Creatures?.FirstOrDefault(c => c.ClassName.Equals(className))?.NameTag ?? null;

        public static string FriendlyCreatureNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Creatures?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        private static ClassMultiplier[] dinoMultipliers = new ClassMultiplier[0];

        public static IEnumerable<ClassMultiplier> GetDinoMultipliers() => dinoMultipliers.Select(d => d.Duplicate<ClassMultiplier>());
        #endregion

        #region Engrams
        private static EngramEntry[] engrams = new EngramEntry[0];

        public static IEnumerable<EngramEntry> GetEngrams() => engrams.Select(d => d.Duplicate<EngramEntry>());

        public static EngramEntry GetEngramForClass(string className) => engrams.FirstOrDefault(e => e.EngramClassName.Equals(className));

        public static bool HasEngramForClass(string className) => engrams.Any(e => e.EngramClassName.Equals(className));

        public static bool IsTekgram(string className) => engrams.Any(e => e.EngramClassName.Equals(className) && e.IsTekgram);

        public static string FriendlyEngramNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Engrams?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Items
        private static PrimalItem[] items = new PrimalItem[0];

        public static IEnumerable<PrimalItem> GetItems() => items.Select(d => d.Duplicate());

        public static PrimalItem GetItemForClass(string className) => items.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasItemForClass(string className) => items.Any(e => e.ClassName.Equals(className));

        public static string FriendlyItemNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Items?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Resources
        private static ResourceClassMultiplier[] resourceMultipliers = new ResourceClassMultiplier[0];

        public static IEnumerable<ResourceClassMultiplier> GetResourceMultipliers() => resourceMultipliers.Select(d => d.Duplicate<ResourceClassMultiplier>());

        public static ResourceClassMultiplier GetResourceMultiplierForClass(string className) => resourceMultipliers.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasResourceMultiplierForClass(string className) => resourceMultipliers.Any(e => e.ClassName.Equals(className));

        public static string FriendlyResourceNameForClass(string className) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Items?.FirstOrDefault(i => i.ClassName.Equals(className) && i.IsHarvestable)?.Description ?? className;
        #endregion

        #region Map Spawners
        private static MapSpawner[] mapSpawners = new MapSpawner[0];

        public static IEnumerable<MapSpawner> GetMapSpawners() => mapSpawners.Select(d => d.Duplicate());

        public static MapSpawner GetMapSpawnerForClass(string className) => mapSpawners.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasMapSpawnerForClass(string className) => mapSpawners.Any(e => e.ClassName.Equals(className));

        public static string FriendlyMapSpawnerNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.MapSpawners?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Supply Crates
        private static SupplyCrate[] supplyCrates = new SupplyCrate[0];

        public static IEnumerable<SupplyCrate> GetSupplyCrates() => supplyCrates.Select(d => d.Duplicate());

        public static SupplyCrate GetSupplyCrateForClass(string className) => supplyCrates.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasSupplyCrateForClass(string className) => supplyCrates.Any(e => e.ClassName.Equals(className));

        public static string FriendlySupplyCrateNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.SupplyCrates?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Game Maps
        private static ComboBoxItem[] gameMaps = new[]
        {
            new ComboBoxItem { ValueMember=Config.Default.DefaultServerMap, DisplayMember=FriendlyNameForClass(Config.Default.DefaultServerMap) },
        };

        public static IEnumerable<ComboBoxItem> GetGameMaps() => gameMaps.Select(d => d.Duplicate());

        public static string FriendlyMapNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.GameMaps?.FirstOrDefault(i => i.ClassName.Equals(className) && !i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        private static ComboBoxItem[] gameMapsSotF = new[]
        {
            new ComboBoxItem { ValueMember=Config.Default.DefaultServerMap, DisplayMember=FriendlyNameForClass(Config.Default.DefaultServerMap) },
        };

        public static IEnumerable<ComboBoxItem> GetGameMapsSotF() => gameMapsSotF.Select(d => d.Duplicate());

        public static string FriendlyMapSotFNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.GameMaps?.FirstOrDefault(i => i.ClassName.Equals(className) && i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Total Conversions
        private static ComboBoxItem[] totalConversions = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetTotalConversions() => totalConversions.Select(d => d.Duplicate());

        public static string FriendlyTotalConversionNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Mods?.FirstOrDefault(i => i.ClassName.Equals(className) && !i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        private static ComboBoxItem[] totalConversionsSotF = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetTotalConversionsSotF() => totalConversionsSotF.Select(d => d.Duplicate());

        public static string FriendlyTotalConversionSotFNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Mods?.FirstOrDefault(i => i.ClassName.Equals(className) && i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Stats Multipliers
        public enum StatsMultiplier
        {
            Health = 0,
            Stamina = 1,
            Torpidity = 2,
            Oxygen = 3,
            Food = 4,
            Water = 5,
            Temperature = 6,
            Weight = 7,
            Melee = 8,
            Speed = 9,
            Fortitude = 10,
            Crafting = 11
        };

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoWild()
        {
            return new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoTamed()
        {
            return new float[] { 0.2f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.17f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoTamedAdd()
        {
            return new float[] { 0.14f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.14f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoTamedAffinity()
        {
            return new float[] { 0.44f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.44f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetBaseStatMultipliers_Player()
        {
            return new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_Player()
        {
            return new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoWildPerLevel()
        {
            return new bool[] { true, true, false, true, true, false, true, true, true, true, false, false };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoTamedPerLevel()
        {
            return new bool[] { true, true, false, true, true, false, true, true, true, true, false, false };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoTamedAdd()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, false };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoTamedAffinity()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, false };
        }

        internal static bool[] GetStatMultiplierInclusions_PlayerBase()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, true };
        }

        internal static bool[] GetStatMultiplierInclusions_PlayerPerLevel()
        {
            return new bool[] { true, true, false, true, true, true, true, true, true, true, true, true };
        }
        #endregion

        #region Levels
        private static Level[] levelsDino = new[]
        {
            new Level { XPRequired=10 },
        };

        private static Level[] levelsPlayer = new[]
        {
            new Level { XPRequired=5, EngramPoints=8 },
        };

        public static IEnumerable<Level> LevelsDino => levelsDino.Select(l => l.Duplicate());

        public static IEnumerable<Level> LevelsPlayer => levelsPlayer.Select(l => l.Duplicate());
        #endregion

        #region Branches
        private static ComboBoxItem[] branches = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetBranches() => branches.Select(d => d.Duplicate());

        public static string FriendlyBranchName(string branchName, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(branchName) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(branchName) ?? gameData?.Branches?.FirstOrDefault(i => i.BranchName.Equals(branchName) && !i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : branchName);

        private static ComboBoxItem[] branchesSotF = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetBranchesSotF() => branchesSotF.Select(d => d.Duplicate());

        public static string FriendlyBranchSotFName(string branchName, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(branchName) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(branchName) ?? gameData?.Branches?.FirstOrDefault(i => i.BranchName.Equals(branchName) && i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : branchName);
        #endregion
    }
}
