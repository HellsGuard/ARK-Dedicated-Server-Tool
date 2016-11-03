using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ARK_Server_Manager.Lib.ViewModel;

namespace ARK_Server_Manager.Lib
{
    public class EngramEntryList<T> : AggregateIniValueList<T>
         where T : EngramEntry, new()
    {
        private bool _onlyAllowSelectedEngrams;

        public bool OnlyAllowSelectedEngrams
        {
            get { return this._onlyAllowSelectedEngrams; }
            set
            {
                this._onlyAllowSelectedEngrams = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(OnlyAllowSelectedEngrams)));
            }
        }

        public EngramEntryList(string aggregateValueName, Func<IEnumerable<T>> resetFunc)
            : base(aggregateValueName, resetFunc)
        {
        }

        public override IEnumerable<string> ToIniValues()
        {
            if (!OnlyAllowSelectedEngrams)
                return base.ToIniValues();

            var values = new List<string>();
            values.AddRange(this.Where(d => d.SaveEngramOverride).Select(d => $"{this.IniCollectionKey}={d.ToINIValue()}"));
            return values;
        }
    }

    public class EngramEntry : AggregateIniValue
    {
        public static readonly DependencyProperty ArkApplicationProperty = DependencyProperty.Register(nameof(ArkApplication), typeof(ArkApplication), typeof(EngramEntry), new PropertyMetadata(ArkApplication.SurvivalEvolved));
        public static readonly DependencyProperty EngramClassNameProperty = DependencyProperty.Register(nameof(EngramClassName), typeof(string), typeof(EngramEntry), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty EngramLevelRequirementProperty = DependencyProperty.Register(nameof(EngramLevelRequirement), typeof(int), typeof(EngramEntry), new PropertyMetadata(1));
        public static readonly DependencyProperty EngramPointsCostProperty = DependencyProperty.Register(nameof(EngramPointsCost), typeof(int), typeof(EngramEntry), new PropertyMetadata(1));
        public static readonly DependencyProperty EngramHiddenProperty = DependencyProperty.Register(nameof(EngramHidden), typeof(bool), typeof(EngramEntry), new PropertyMetadata(false));
        public static readonly DependencyProperty RemoveEngramPreReqProperty = DependencyProperty.Register(nameof(RemoveEngramPreReq), typeof(bool), typeof(EngramEntry), new PropertyMetadata(false));
        public static readonly DependencyProperty SaveEngramOverrideProperty = DependencyProperty.Register(nameof(SaveEngramOverride), typeof(bool), typeof(EngramEntry), new PropertyMetadata(false));

        public ArkApplication ArkApplication
        {
            get { return (ArkApplication)GetValue(ArkApplicationProperty); }
            set { SetValue(ArkApplicationProperty, value); }
        }

        [AggregateIniValueEntry]
        public string EngramClassName
        {
            get { return (string)GetValue(EngramClassNameProperty); }
            set { SetValue(EngramClassNameProperty, value); }
        }

        [AggregateIniValueEntry]
        public int EngramLevelRequirement
        {
            get { return (int)GetValue(EngramLevelRequirementProperty); }
            set { SetValue(EngramLevelRequirementProperty, value); }
        }

        [AggregateIniValueEntry]
        public int EngramPointsCost
        {
            get { return (int)GetValue(EngramPointsCostProperty); }
            set { SetValue(EngramPointsCostProperty, value); }
        }

        [AggregateIniValueEntry]
        public bool EngramHidden
        {
            get { return (bool)GetValue(EngramHiddenProperty); }
            set { SetValue(EngramHiddenProperty, value); }
        }

        [AggregateIniValueEntry]
        public bool RemoveEngramPreReq
        {
            get { return (bool)GetValue(RemoveEngramPreReqProperty); }
            set { SetValue(RemoveEngramPreReqProperty, value); }
        }

        public string DisplayName => GameData.FriendlyNameForClass(EngramClassName);

        public bool KnownEngram
        {
            get
            {
                return GameData.HasEngramForClass(EngramClassName);
            }
        }

        public bool SaveEngramOverride
        {
            get { return (bool)GetValue(SaveEngramOverrideProperty); }
            set { SetValue(SaveEngramOverrideProperty, value); }
        }

        public static EngramEntry FromINIValue(string iniValue)
        {
            var newSpawn = new EngramEntry();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override void InitializeFromINIValue(string value)
        {
            base.InitializeFromINIValue(value);

            if (!KnownEngram)
                ArkApplication = ArkApplication.Unknown;
            SaveEngramOverride = true;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.EngramClassName, ((EngramEntry)other).EngramClassName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool ShouldSave()
        {
            if (!KnownEngram || SaveEngramOverride)
                return true;

            var engramEntry = GameData.GetEngramForClass(EngramClassName);
            if (engramEntry == null)
                return true;

            return (!engramEntry.EngramHidden.Equals(EngramHidden) ||
                !engramEntry.EngramPointsCost.Equals(EngramPointsCost) ||
                !engramEntry.EngramLevelRequirement.Equals(EngramLevelRequirement) ||
                !engramEntry.RemoveEngramPreReq.Equals(RemoveEngramPreReq));
        }
    }
}
