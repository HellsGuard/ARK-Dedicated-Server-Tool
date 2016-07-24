using System;
using System.Windows;
using System.Xml.Serialization;
using ARK_Server_Manager.Lib.ViewModel;

namespace ARK_Server_Manager.Lib
{   
    public class DinoSpawn : AggregateIniValue
    {
        public const string AggregateValueName = "DinoSpawnWeightMultipliers";

        public const bool DefaultOverrideSpawnLimitPercentage = true;
        public const float DefaultSpawnLimitPercentage = ClassMultiplier.DefaultMultiplier;
        public const float DefaultSpawnWeightMultiplier = ClassMultiplier.DefaultMultiplier;

        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty DinoNameTagProperty = DependencyProperty.Register(nameof(DinoNameTag), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register(nameof(OverrideSpawnLimitPercentage), typeof(bool), typeof(DinoSpawn), new PropertyMetadata(DefaultOverrideSpawnLimitPercentage));
        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register(nameof(SpawnLimitPercentage), typeof(float), typeof(DinoSpawn), new PropertyMetadata(DefaultSpawnLimitPercentage));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register(nameof(SpawnWeightMultiplier), typeof(float), typeof(DinoSpawn), new PropertyMetadata(DefaultSpawnWeightMultiplier));

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
