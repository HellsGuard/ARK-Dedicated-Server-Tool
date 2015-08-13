using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public DinoSettingsList(AggregateIniValueList<DinoSpawn> dinoSpawnWeightMultipliers, 
                                StringIniValueList preventDinoTameClassNames,
                                AggregateIniValueList<NPCReplacement> npcReplacements,
                                AggregateIniValueList<ClassMultiplier> tamedDinoClassDamageMultipliers, 
                                AggregateIniValueList<ClassMultiplier> tamedDinoClassResistanceMultipliers,
                                AggregateIniValueList<ClassMultiplier> dinoClassDamageMultipliers,
                                AggregateIniValueList<ClassMultiplier> dinoClassResistanceMultipliers)
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

        public void Reset()
        {
            this.Clear();
            foreach(var entry in GameData.GetDinoClasses())
            {
                this.Add(new DinoSettings()
                {
                    ClassName = entry,
                    FriendlyName = GameData.FriendlyNameForClass(entry),
                    NameTag = GameData.NameTagForClass(entry),
                    CanSpawn = true,
                    ReplacementClass = entry,
                    CanTame = true,
                    SpawnWeightMultiplier = 1.0f,
                    OverrideSpawnLimitPercentage = false,
                    SpawnLimitPercentage = 1.0f,
                    TamedDamageMultiplier = 1.0f,
                    TamedResistanceMultiplier = 1.0f,
                    WildDamageMultiplier = 1.0f,
                    WildResistanceMultiplier = 1.0f,
                });
            }
        }

        public void RenderToView()
        {
            Reset();

            foreach(var entry in this.DinoSpawnWeightMultipliers)
            {
                var settings = this.FirstOrDefault(vi => vi.NameTag == entry.DinoNameTag);
                if (settings != null)
                {
                    settings.SpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                    settings.OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                    settings.SpawnLimitPercentage = entry.SpawnLimitPercentage;
                }
            }

            foreach(var entry in this.PreventDinoTameClassNames)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry);
                if (dinoSettings != null)
                {
                    dinoSettings.CanTame = false;
                }
            }

            foreach(var entry in this.NpcReplacements)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.FromClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.CanSpawn = !string.IsNullOrWhiteSpace(entry.ToClassName);
                    dinoSettings.ReplacementClass = dinoSettings.CanSpawn ? entry.ToClassName : dinoSettings.ClassName;
                }
            }

            foreach (var entry in this.TamedDinoClassDamageMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.TamedDamageMultiplier = entry.Multiplier;
                }
            }

            foreach(var entry in this.TamedDinoClassResistanceMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.TamedResistanceMultiplier = entry.Multiplier;
                }
            }

            foreach (var entry in this.DinoClassDamageMultipliers)
            {
                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
                if (dinoSettings != null)
                {
                    dinoSettings.WildDamageMultiplier = entry.Multiplier;
                }
            }

            foreach (var entry in this.DinoClassResistanceMultipliers)
            {

                var dinoSettings = this.FirstOrDefault(vi => vi.ClassName == entry.ClassName);
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
                this.DinoSpawnWeightMultipliers.Add(new DinoSpawn()
                {
                    ClassName = entry.ClassName,
                    DinoNameTag = entry.NameTag,
                    OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage,
                    SpawnLimitPercentage = entry.SpawnLimitPercentage,
                    SpawnWeightMultiplier = entry.SpawnWeightMultiplier
                });

                if(!entry.CanTame)
                {
                    this.PreventDinoTameClassNames.Add(entry.ClassName);
                }

                this.NpcReplacements.Add(new NPCReplacement() { FromClassName = entry.ClassName, ToClassName = entry.CanSpawn ? entry.ReplacementClass : String.Empty });

                this.TamedDinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedDamageMultiplier });
                this.TamedDinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedResistanceMultiplier });
                this.DinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildDamageMultiplier });
                this.DinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildResistanceMultiplier });
            }
        }
    }
}
