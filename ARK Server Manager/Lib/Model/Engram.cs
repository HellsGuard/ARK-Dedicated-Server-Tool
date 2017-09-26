using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ARK_Server_Manager.Lib.ViewModel;
using System.Runtime.Serialization;

namespace ARK_Server_Manager.Lib
{
    [DataContract]
    public class EngramEntryList<T> : AggregateIniValueList<T>
         where T : EngramEntry, new()
    {
        private bool _onlyAllowSelectedEngrams;

        [DataMember]
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

    [DataContract]
    public class EngramEntry : AggregateIniValue
    {
        public static readonly DependencyProperty ArkApplicationProperty = DependencyProperty.Register(nameof(ArkApplication), typeof(ArkApplication), typeof(EngramEntry), new PropertyMetadata(ArkApplication.SurvivalEvolved));
        public static readonly DependencyProperty EngramClassNameProperty = DependencyProperty.Register(nameof(EngramClassName), typeof(string), typeof(EngramEntry), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty EngramLevelRequirementProperty = DependencyProperty.Register(nameof(EngramLevelRequirement), typeof(int), typeof(EngramEntry), new PropertyMetadata(1));
        public static readonly DependencyProperty EngramPointsCostProperty = DependencyProperty.Register(nameof(EngramPointsCost), typeof(int), typeof(EngramEntry), new PropertyMetadata(1));
        public static readonly DependencyProperty EngramHiddenProperty = DependencyProperty.Register(nameof(EngramHidden), typeof(bool), typeof(EngramEntry), new PropertyMetadata(false));
        public static readonly DependencyProperty RemoveEngramPreReqProperty = DependencyProperty.Register(nameof(RemoveEngramPreReq), typeof(bool), typeof(EngramEntry), new PropertyMetadata(false));
        public static readonly DependencyProperty SaveEngramOverrideProperty = DependencyProperty.Register(nameof(SaveEngramOverride), typeof(bool), typeof(EngramEntry), new PropertyMetadata(false));

        [DataMember]
        public ArkApplication ArkApplication
        {
            get { return (ArkApplication)GetValue(ArkApplicationProperty); }
            set { SetValue(ArkApplicationProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public string EngramClassName
        {
            get { return (string)GetValue(EngramClassNameProperty); }
            set { SetValue(EngramClassNameProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public int EngramLevelRequirement
        {
            get { return (int)GetValue(EngramLevelRequirementProperty); }
            set { SetValue(EngramLevelRequirementProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public int EngramPointsCost
        {
            get { return (int)GetValue(EngramPointsCostProperty); }
            set { SetValue(EngramPointsCostProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public bool EngramHidden
        {
            get { return (bool)GetValue(EngramHiddenProperty); }
            set { SetValue(EngramHiddenProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public bool RemoveEngramPreReq
        {
            get { return (bool)GetValue(RemoveEngramPreReqProperty); }
            set { SetValue(RemoveEngramPreReqProperty, value); }
        }

        public string DisplayName => GameData.FriendlyNameForClass(EngramClassName);

        public bool IsTekgram
        {
            get;
            set;
        }

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
            IsTekgram = GameData.IsTekgram(EngramClassName);
            SaveEngramOverride = !IsTekgram;

            if (IsTekgram)
            {
                // always make sure that the tekgrams have default values.
                EngramLevelRequirement = 0;
                EngramPointsCost = 0;
                RemoveEngramPreReq = false;
            }
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

            var engramLevelRequirement = IsTekgram ? 0 : EngramLevelRequirement;
            var engramPointsCost = IsTekgram ? 0 : EngramPointsCost;
            var engramHidden = EngramHidden;
            var removeEngramPreReq = IsTekgram ? false : RemoveEngramPreReq;

            return (!engramEntry.EngramHidden.Equals(engramHidden) ||
                !engramEntry.EngramPointsCost.Equals(engramPointsCost) ||
                !engramEntry.EngramLevelRequirement.Equals(engramLevelRequirement) ||
                !engramEntry.RemoveEngramPreReq.Equals(removeEngramPreReq));
        }

        public EngramEntry Clone()
        {
            var engramEntry = new EngramEntry();
            engramEntry.ArkApplication = this.ArkApplication;
            engramEntry.EngramClassName = this.EngramClassName;
            engramEntry.EngramLevelRequirement = this.EngramLevelRequirement;
            engramEntry.EngramPointsCost = this.EngramPointsCost;
            engramEntry.EngramHidden = this.EngramHidden;
            engramEntry.RemoveEngramPreReq = this.RemoveEngramPreReq;
            engramEntry.SaveEngramOverride = this.SaveEngramOverride;
            engramEntry.IsTekgram = this.IsTekgram;
            return engramEntry;
        }

        public override string ToINIValue()
        {
            if (IsTekgram)
            {
                // always make sure that the tekgrams have default values.
                EngramLevelRequirement = 0;
                EngramPointsCost = 0;
                RemoveEngramPreReq = false;
            }

            return base.ToINIValue();
        }
    }
}
