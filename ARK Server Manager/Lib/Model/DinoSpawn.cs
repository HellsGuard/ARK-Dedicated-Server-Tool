using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace ARK_Server_Manager.Lib
{   
    public class DinoSpawn : AggregateIniValue
    {
        public const string AggregateValueName = "DinoSpawnWeightMultipliers";
        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register(nameof(SpawnLimitPercentage), typeof(float), typeof(DinoSpawn), new PropertyMetadata(10.0F));
        public static readonly DependencyProperty DinoNameTagProperty = DependencyProperty.Register(nameof(DinoNameTag), typeof(string), typeof(DinoSpawn), new PropertyMetadata("--SET ME--"));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register(nameof(SpawnWeightMultiplier), typeof(float), typeof(DinoSpawn), new PropertyMetadata(0.0F));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register(nameof(OverrideSpawnLimitPercentage), typeof(bool), typeof(DinoSpawn), new PropertyMetadata(false));

        [XmlElement(ElementName="Name")]
        [AggregateIniValueEntry]
        public string DinoNameTag
        {
            get { return (string)GetValue(DinoNameTagProperty); }
            set { SetValue(DinoNameTagProperty, value); }
        }

        [AggregateIniValueEntry]
        public float SpawnWeightMultiplier
        {
            get { return (float)GetValue(SpawnWeightMultiplierProperty); }
            set { SetValue(SpawnWeightMultiplierProperty, value); }
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



        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));



        public static DinoSpawn FromINIValue(string iniValue)
        {
            var newSpawn = new DinoSpawn();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.DinoNameTag, ((DinoSpawn)other).DinoNameTag, StringComparison.OrdinalIgnoreCase);
        }

        public override string GetSortKey()
        {
            return this.DinoNameTag;
        }
    }
}
