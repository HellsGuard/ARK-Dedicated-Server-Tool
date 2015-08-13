using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    //
    // This class aggregates many settings related to dinos
    //
    public class DinoSettings : DependencyObject
    {
        public static readonly DependencyProperty FriendlyNameProperty = DependencyProperty.Register(nameof(FriendlyName), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty CanTameProperty = DependencyProperty.Register(nameof(CanTame), typeof(bool), typeof(DinoSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty CanSpawnProperty = DependencyProperty.Register(nameof(CanSpawn), typeof(bool), typeof(DinoSettings), new PropertyMetadata(true));
        public static readonly DependencyProperty ReplacementClassProperty = DependencyProperty.Register(nameof(ReplacementClass), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register(nameof(SpawnWeightMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register(nameof(OverrideSpawnLimitPercentage), typeof(bool), typeof(DinoSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty WildDamageMultiplierProperty = DependencyProperty.Register(nameof(WildDamageMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty WildResistanceMultiplierProperty = DependencyProperty.Register(nameof(WildResistanceMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty TamedResistanceMultiplierProperty = DependencyProperty.Register(nameof(TamedResistanceMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty TamedDamageMultiplierProperty = DependencyProperty.Register(nameof(TamedDamageMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register(nameof(SpawnLimitPercentage), typeof(float), typeof(DinoSettings), new PropertyMetadata(1.0f));

        public string FriendlyName
        {
            get { return (string)GetValue(FriendlyNameProperty); }
            set { SetValue(FriendlyNameProperty, value); }
        }
      
        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public bool CanTame
        {
            get { return (bool)GetValue(CanTameProperty); }
            set { SetValue(CanTameProperty, value); }
        }

        public bool CanSpawn
        {
            get { return (bool)GetValue(CanSpawnProperty); }
            set { SetValue(CanSpawnProperty, value); }
        }

        public string ReplacementClass
        {
            get { return (string)GetValue(ReplacementClassProperty); }
            set { SetValue(ReplacementClassProperty, value); }
        }

        public float SpawnWeightMultiplier
        {
            get { return (float)GetValue(SpawnWeightMultiplierProperty); }
            set { SetValue(SpawnWeightMultiplierProperty, value); }
        }

        public bool OverrideSpawnLimitPercentage
        {
            get { return (bool)GetValue(OverrideSpawnLimitPercentageProperty); }
            set { SetValue(OverrideSpawnLimitPercentageProperty, value); }
        }

        public float SpawnLimitPercentage
        {
            get { return (float)GetValue(SpawnLimitPercentageProperty); }
            set { SetValue(SpawnLimitPercentageProperty, value); }
        }

        public float TamedDamageMultiplier
        {
            get { return (float)GetValue(TamedDamageMultiplierProperty); }
            set { SetValue(TamedDamageMultiplierProperty, value); }
        }

        public float TamedResistanceMultiplier
        {
            get { return (float)GetValue(TamedResistanceMultiplierProperty); }
            set { SetValue(TamedResistanceMultiplierProperty, value); }
        }

        public float WildDamageMultiplier
        {
            get { return (float)GetValue(WildDamageMultiplierProperty); }
            set { SetValue(WildDamageMultiplierProperty, value); }
        }
       
        public float WildResistanceMultiplier
        {
            get { return (float)GetValue(WildResistanceMultiplierProperty); }
            set { SetValue(WildResistanceMultiplierProperty, value); }
        }

        public string NameTag { get; internal set; }
    }
}
