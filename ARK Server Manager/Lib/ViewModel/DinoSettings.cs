using System;
using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    //
    // This class aggregates many settings related to dinos
    //
    public class DinoSettings : DependencyObject
    {
        public static readonly DependencyProperty ArkApplicationProperty = DependencyProperty.Register(nameof(ArkApplication), typeof(ArkApplication), typeof(DinoSettings), new PropertyMetadata(ArkApplication.SurvivalEvolved));
        public static readonly DependencyProperty FriendlyNameProperty = DependencyProperty.Register(nameof(FriendlyName), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty CanTameProperty = DependencyProperty.Register(nameof(CanTame), typeof(bool), typeof(DinoSettings), new PropertyMetadata(true));
        public static readonly DependencyProperty CanSpawnProperty = DependencyProperty.Register(nameof(CanSpawn), typeof(bool), typeof(DinoSettings), new PropertyMetadata(true));
        public static readonly DependencyProperty ReplacementClassProperty = DependencyProperty.Register(nameof(ReplacementClass), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register(nameof(SpawnWeightMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(DinoSpawn.DEFAULT_SPAWN_WEIGHT_MULTIPLIER));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register(nameof(OverrideSpawnLimitPercentage), typeof(bool), typeof(DinoSettings), new PropertyMetadata(DinoSpawn.DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register(nameof(SpawnLimitPercentage), typeof(float), typeof(DinoSettings), new PropertyMetadata(DinoSpawn.DEFAULT_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty TamedDamageMultiplierProperty = DependencyProperty.Register(nameof(TamedDamageMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));
        public static readonly DependencyProperty TamedResistanceMultiplierProperty = DependencyProperty.Register(nameof(TamedResistanceMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));
        public static readonly DependencyProperty WildDamageMultiplierProperty = DependencyProperty.Register(nameof(WildDamageMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));
        public static readonly DependencyProperty WildResistanceMultiplierProperty = DependencyProperty.Register(nameof(WildResistanceMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));

        public ArkApplication ArkApplication
        {
            get { return (ArkApplication)GetValue(ArkApplicationProperty); }
            set { SetValue(ArkApplicationProperty, value); }
        }

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
        public bool KnownDino { get; internal set; }
        public bool HasNameTag { get; internal set; }
        public bool HasClassName { get; internal set; }
        public DinoTamable IsTameable { get; internal set; }

        public DinoSettings Clone()
        {
            return new DinoSettings()
            {
                ArkApplication = ArkApplication,
                ClassName = ClassName,
                FriendlyName = FriendlyName,
                NameTag = NameTag,

                CanSpawn = CanSpawn,
                CanTame = CanTame,
                ReplacementClass = ReplacementClass,

                SpawnWeightMultiplier = SpawnWeightMultiplier,
                OverrideSpawnLimitPercentage = OverrideSpawnLimitPercentage,
                SpawnLimitPercentage = SpawnLimitPercentage,

                TamedDamageMultiplier = TamedDamageMultiplier,
                TamedResistanceMultiplier = TamedResistanceMultiplier,
                WildDamageMultiplier = WildDamageMultiplier,
                WildResistanceMultiplier = WildResistanceMultiplier,

                KnownDino = KnownDino,
                HasNameTag = HasNameTag,
                HasClassName = HasClassName,
                IsTameable = IsTameable,
            };
        }
    }
}
