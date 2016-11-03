using System;
using System.Windows;
using System.Xml.Serialization;

namespace ARK_Server_Manager.Lib
{   
    public class DinoSpawn : AggregateIniValue
    {
        public const bool DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE = true;
        public const float DEFAULT_SPAWN_LIMIT_PERCENTAGE = ClassMultiplier.DEFAULT_MULTIPLIER;
        public const float DEFAULT_SPAWN_WEIGHT_MULTIPLIER = ClassMultiplier.DEFAULT_MULTIPLIER;

        public static readonly DependencyProperty ArkApplicationProperty = DependencyProperty.Register(nameof(ArkApplication), typeof(ArkApplication), typeof(DinoSpawn), new PropertyMetadata(ArkApplication.SurvivalEvolved));
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty DinoNameTagProperty = DependencyProperty.Register(nameof(DinoNameTag), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register(nameof(OverrideSpawnLimitPercentage), typeof(bool), typeof(DinoSpawn), new PropertyMetadata(DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register(nameof(SpawnLimitPercentage), typeof(float), typeof(DinoSpawn), new PropertyMetadata(DEFAULT_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register(nameof(SpawnWeightMultiplier), typeof(float), typeof(DinoSpawn), new PropertyMetadata(DEFAULT_SPAWN_WEIGHT_MULTIPLIER));

        public ArkApplication ArkApplication
        {
            get { return (ArkApplication)GetValue(ArkApplicationProperty); }
            set { SetValue(ArkApplicationProperty, value); }
        }

        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        [XmlElement(ElementName="Name")]
        [AggregateIniValueEntry]
        public string DinoNameTag
        {
            get { return (string)GetValue(DinoNameTagProperty); }
            set { SetValue(DinoNameTagProperty, value); }
        }

        [AggregateIniValueEntry]
        public bool OverrideSpawnLimitPercentage
        {
            get { return (bool)GetValue(OverrideSpawnLimitPercentageProperty); }
            set { SetValue(OverrideSpawnLimitPercentageProperty, value); }
        }

        [AggregateIniValueEntry]
        public float SpawnLimitPercentage
        {
            get { return (float)GetValue(SpawnLimitPercentageProperty); }
            set { SetValue(SpawnLimitPercentageProperty, value); }  
        }

        [AggregateIniValueEntry]
        public float SpawnWeightMultiplier
        {
            get { return (float)GetValue(SpawnWeightMultiplierProperty); }
            set { SetValue(SpawnWeightMultiplierProperty, value); }
        }

        public string DisplayName => GameData.FriendlyNameForClass(ClassName);


        public static DinoSpawn FromINIValue(string iniValue)
        {
            var newSpawn = new DinoSpawn();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey()
        {
            return this.DinoNameTag;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.DinoNameTag, ((DinoSpawn)other).DinoNameTag, StringComparison.OrdinalIgnoreCase);
        }
    }
}
