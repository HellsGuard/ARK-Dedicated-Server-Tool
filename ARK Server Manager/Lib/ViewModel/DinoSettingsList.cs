using System;
using System.Linq;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class DinoSettingsList : SortableObservableCollection<DinoSettings>
    {
        public AggregateIniValueList<DinoSpawn> DinoSpawnWeightMultipliers { get; }
        public StringIniValueList PreventDinoTameClassNames { get; }
        public AggregateIniValueList<NPCReplacement> NpcReplacements { get; }
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassDamageMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassResistanceMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> DinoClassDamageMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> DinoClassResistanceMultipliers { get; }

        public DinoSettingsList()
        {
            Reset();
        }

        public DinoSettingsList(AggregateIniValueList<DinoSpawn> dinoSpawnWeightMultipliers, StringIniValueList preventDinoTameClassNames, AggregateIniValueList<NPCReplacement> npcReplacements,
                                AggregateIniValueList<ClassMultiplier> tamedDinoClassDamageMultipliers, AggregateIniValueList<ClassMultiplier> tamedDinoClassResistanceMultipliers,
                                AggregateIniValueList<ClassMultiplier> dinoClassDamageMultipliers, AggregateIniValueList<ClassMultiplier> dinoClassResistanceMultipliers)
        {
            this.DinoSpawnWeightMultipliers = dinoSpawnWeightMultipliers;
            this.PreventDinoTameClassNames = preventDinoTameClassNames;
            this.NpcReplacements = npcReplacements;
            this.TamedDinoClassDamageMultipliers = tamedDinoClassDamageMultipliers;
            this.TamedDinoClassResistanceMultipliers = tamedDinoClassResistanceMultipliers;
            this.DinoClassDamageMultipliers = dinoClassDamageMultipliers;
            this.DinoClassResistanceMultipliers = dinoClassResistanceMultipliers;
            Reset();
        }

        private DinoSettings CreateDinoSetting(string className, bool knownDino, bool hasNameTag, bool hasClassName, ArkApplication arkApplication)
        {
            var nameTag = GameData.NameTagForClass(className);
            var isSpawnable = GameData.IsSpawnableForClass(className);
            var isTameable = GameData.IsTameableForClass(className);

            return new DinoSettings()
            {
                ArkApplication = arkApplication,
                ClassName = className,
                NameTag = nameTag,

                CanSpawn = true,
                CanTame = isTameable == DinoTamable.True,
                ReplacementClass = className,

                SpawnWeightMultiplier = DinoSpawn.DEFAULT_SPAWN_WEIGHT_MULTIPLIER,
                OverrideSpawnLimitPercentage = DinoSpawn.DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE,
                SpawnLimitPercentage = DinoSpawn.DEFAULT_SPAWN_LIMIT_PERCENTAGE,

                TamedDamageMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,
                TamedResistanceMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,
                WildDamageMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,
                WildResistanceMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,

                KnownDino = knownDino,
                HasClassName = hasClassName,
                HasNameTag = hasNameTag,
                IsSpawnable = isSpawnable,
                IsTameable = isTameable,
            };
        }

        public DinoSettingsList Clone()
        {
            DinoSettingsList clone = new DinoSettingsList();
            clone.Clear();

            foreach (var dinoSetting in this)
            {
                clone.Add(dinoSetting.Clone());
            }

            return clone;
        }

        public void Reset()
        {
            this.Clear();

            var dinoSpawns = GameData.GetDinoSpawns();
            foreach (var entry in dinoSpawns)
            {
                this.Add(CreateDinoSetting(entry.ClassName, true, entry.DinoNameTag != null, true, entry.ArkApplication));
            }
        }

        public void RenderToView()
        {
            Reset();

            foreach(var entry in this.DinoSpawnWeightMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.NameTag == entry.DinoNameTag);
                if (dinoSettings == null)
                {
                    this.Add(CreateDinoSetting(entry.DinoNameTag, false, true, false, ArkApplication.Unknown));
                }

                dinoSettings = this.FirstOrDefault(vi => vi.NameTag == entry.DinoNameTag);
                if (dinoSettings != null)
                {
                    dinoSettings.SpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                    dinoSettings.OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                    dinoSettings.SpawnLimitPercentage = entry.SpawnLimitPercentage;
                }
            }

            foreach(var entry in this.PreventDinoTameClassNames)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry);
                if (dinoSettings == null)
                {
                    this.Add(CreateDinoSetting(entry, false, false, true, ArkApplication.Unknown));
                }

                dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry);
                if (dinoSettings != null)
                {
                    dinoSettings.CanTame = false;
                }
            }

            foreach(var entry in this.NpcReplacements)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.FromClassName);
                if (dinoSettings == null)
                {
                    this.Add(CreateDinoSetting(entry.FromClassName, false, false, true, ArkApplication.Unknown));
                }

                dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.FromClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.CanSpawn = !string.IsNullOrWhiteSpace(entry.ToClassName);
                    dinoSettings.ReplacementClass = dinoSettings.CanSpawn ? entry.ToClassName : dinoSettings.ClassName;
                }
            }

            foreach (var entry in this.TamedDinoClassDamageMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings == null)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, false, false, true, ArkApplication.Unknown));
                }

                dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.TamedDamageMultiplier = entry.Multiplier;
                }
            }

            foreach(var entry in this.TamedDinoClassResistanceMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings == null)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, false, false, true, ArkApplication.Unknown));
                }

                dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.TamedResistanceMultiplier = entry.Multiplier;
                }
            }

            foreach (var entry in this.DinoClassDamageMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings == null)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, false, false, true, ArkApplication.Unknown));
                }

                dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.WildDamageMultiplier = entry.Multiplier;
                }
            }

            foreach (var entry in this.DinoClassResistanceMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings == null)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, false, false, true, ArkApplication.Unknown));
                }

                dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.WildResistanceMultiplier = entry.Multiplier;
                }
            }
        }

        public void RenderToModel()
        {
            this.DinoSpawnWeightMultipliers.Clear();
            this.PreventDinoTameClassNames.Clear();
            this.PreventDinoTameClassNames.IsEnabled = true;
            this.NpcReplacements.Clear();
            this.NpcReplacements.IsEnabled = true;
            this.TamedDinoClassDamageMultipliers.Clear();
            this.TamedDinoClassResistanceMultipliers.Clear();
            this.DinoClassDamageMultipliers.Clear();
            this.DinoClassResistanceMultipliers.Clear();
                       
            foreach(var entry in this)
            {
                if (entry.HasNameTag && !string.IsNullOrWhiteSpace(entry.NameTag))
                {
                    if (!entry.OverrideSpawnLimitPercentage.Equals(DinoSpawn.DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE) ||
                        !entry.SpawnLimitPercentage.Equals(DinoSpawn.DEFAULT_SPAWN_LIMIT_PERCENTAGE) ||
                        !entry.SpawnWeightMultiplier.Equals(DinoSpawn.DEFAULT_SPAWN_WEIGHT_MULTIPLIER))
                    {
                        this.DinoSpawnWeightMultipliers.Add(new DinoSpawn()
                        {
                            ClassName = entry.ClassName,
                            DinoNameTag = entry.NameTag,
                            OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage,
                            SpawnLimitPercentage = entry.SpawnLimitPercentage,
                            SpawnWeightMultiplier = entry.SpawnWeightMultiplier
                        });
                    }
                }

                if (entry.HasClassName && !string.IsNullOrWhiteSpace(entry.ClassName))
                {
                    if (entry.IsTameable == DinoTamable.True && !entry.CanTame)
                    {
                        this.PreventDinoTameClassNames.Add(entry.ClassName);
                    }

                    this.NpcReplacements.Add(new NPCReplacement() { FromClassName = entry.ClassName, ToClassName = entry.CanSpawn ? entry.ReplacementClass : string.Empty });

                    if (entry.IsTameable == DinoTamable.True || entry.IsTameable == DinoTamable.ByBreeding)
                    {
                        // check if the value has changed.
                        if (!entry.TamedDamageMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                            this.TamedDinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedDamageMultiplier });

                        // check if the value has changed.
                        if (!entry.TamedResistanceMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                            this.TamedDinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedResistanceMultiplier });
                    }

                    // check if the value has changed.
                    if (!entry.WildDamageMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                        this.DinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildDamageMultiplier });

                    // check if the value has changed.
                    if (!entry.WildResistanceMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                        this.DinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildResistanceMultiplier });
                }
            }
        }

        public void UpdateForLocalization()
        {
            //foreach (var dinoSetting in this)
            //{
            //    dinoSetting.FriendlyName = GameData.FriendlyNameForClass(dinoSetting.ClassName);
            //}
        }
    }
}
