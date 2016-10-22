using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using ARK_Server_Manager.Lib.ViewModel;

namespace ARK_Server_Manager.Lib
{
    public class SupplyCrate : AggregateIniValue
    {
        public SupplyCrate()
        {
            ItemSets = new AggregateIniValueList<SupplyCrateItemSet>(nameof(ItemSets), null);
        }

        public static readonly DependencyProperty SupplyCrateClassStringProperty = DependencyProperty.Register(nameof(SupplyCrateClassString), typeof(string), typeof(SupplyCrate), new PropertyMetadata(String.Empty));
        [AggregateIniValueEntry]
        public string SupplyCrateClassString
        {
            get { return (string)GetValue(SupplyCrateClassStringProperty); }
            set
            {
                SetValue(SupplyCrateClassStringProperty, value);
                DisplayName = SupplyCrateClassNameToDisplayNameConverter.Convert(value).ToString();
            }
        }

        public static readonly DependencyProperty MinItemSetsProperty = DependencyProperty.Register(nameof(MinItemSets), typeof(int), typeof(SupplyCrate), new PropertyMetadata(1));
        [AggregateIniValueEntry]
        public int MinItemSets
        {
            get { return (int)GetValue(MinItemSetsProperty); }
            set { SetValue(MinItemSetsProperty, value); }
        }

        public static readonly DependencyProperty MaxItemSetsProperty = DependencyProperty.Register(nameof(MaxItemSets), typeof(int), typeof(SupplyCrate), new PropertyMetadata(1));
        [AggregateIniValueEntry]
        public int MaxItemSets
        {
            get { return (int)GetValue(MaxItemSetsProperty); }
            set { SetValue(MaxItemSetsProperty, value); }
        }

        public static readonly DependencyProperty NumItemSetsPowerProperty = DependencyProperty.Register(nameof(NumItemSetsPower), typeof(float), typeof(SupplyCrate), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float NumItemSetsPower
        {
            get { return (float)GetValue(NumItemSetsPowerProperty); }
            set { SetValue(NumItemSetsPowerProperty, value); }
        }

        public static readonly DependencyProperty bSetsRandomWithoutReplacementProperty = DependencyProperty.Register(nameof(bSetsRandomWithoutReplacement), typeof(bool), typeof(SupplyCrate), new PropertyMetadata(true));
        [AggregateIniValueEntry]
        public bool bSetsRandomWithoutReplacement
        {
            get { return (bool)GetValue(bSetsRandomWithoutReplacementProperty); }
            set { SetValue(bSetsRandomWithoutReplacementProperty, value); }
        }

        public static readonly DependencyProperty ItemSetsProperty = DependencyProperty.Register(nameof(ItemSets), typeof(AggregateIniValueList<SupplyCrateItemSet>), typeof(SupplyCrate), new PropertyMetadata(null));
        [AggregateIniValueEntry]
        public AggregateIniValueList<SupplyCrateItemSet> ItemSets
        {
            get { return (AggregateIniValueList<SupplyCrateItemSet>)GetValue(ItemSetsProperty); }
            set { SetValue(ItemSetsProperty, value); }
        }

        public string DisplayName
        {
            get;
            protected set;
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override void InitializeFromINIValue(string value)
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            var propertyNames = this.Properties.Select(p => p.Name).ToArray();

            var kvPair = value.Split(new[] { '=' }, 2);
            var kvValue = kvPair[1].Trim(' ');
            if (kvValue.StartsWith("("))
                kvValue = kvValue.Substring(1);
            if (kvValue.EndsWith(")))))"))
                kvValue = kvValue.Substring(0, kvValue.Length - 1);

            var propertyValues = StringUtils.SplitIncludingDelimiters(kvValue, propertyNames);

            foreach (var property in this.Properties)
            {
                var propertyValue = propertyValues.FirstOrDefault(p => p.StartsWith(property.Name));
                if (propertyValue == null)
                    continue;

                var kvPropertyPair = propertyValue.Split(new[] { '=' }, 2);
                var kvPropertyValue = kvPropertyPair[1].Trim(',', ' ');

                var collection = property.GetValue(this) as IIniValuesCollection;
                if (collection != null)
                {
                    if (property.Name == nameof(ItemSets))
                    {
                        kvPropertyValue = kvPropertyPair[1].Trim();
                        if (kvPropertyValue.StartsWith("(("))
                            kvPropertyValue = kvPropertyValue.Substring(1);
                        if (kvPropertyValue.EndsWith("))))"))
                            kvPropertyValue = kvPropertyValue.Substring(0, kvPropertyValue.Length - 1);
                        kvPropertyValue = kvPropertyPair[0].Trim() + "=" + kvPropertyValue;
                        kvPropertyValue = kvPropertyValue.Replace(")),(", "))," + kvPropertyPair[0].Trim() + "=(");

                        string[] delimiters = new[] { "," + kvPropertyPair[0].Trim() + "=" };
                        var items = StringUtils.SplitIncludingDelimiters("," + kvPropertyValue, delimiters);
                        collection.FromIniValues(items.Select(i => i.Trim(',', ' ')));
                    }
                    else
                    {
                        collection.FromIniValues(new[] { kvPropertyValue });
                    }
                }
                else
                {
                    StringUtils.SetPropertyValue(kvPropertyValue, this, property);
                }
            }
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.SupplyCrateClassString, ((SupplyCrate)other).SupplyCrateClassString, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToINIValue()
        {
            return base.ToINIValue();
        }
    }

    public class SupplyCrateItemSet : AggregateIniValue
    {
        public SupplyCrateItemSet()
        {
            ItemEntries = new AggregateIniValueList<SupplyCrateItemSetEntry>(nameof(ItemEntries), null);
        }

        public static readonly DependencyProperty MinNumItemsProperty = DependencyProperty.Register(nameof(MinNumItems), typeof(int), typeof(SupplyCrateItemSet), new PropertyMetadata(1));
        [AggregateIniValueEntry]
        public int MinNumItems
        {
            get { return (int)GetValue(MinNumItemsProperty); }
            set { SetValue(MinNumItemsProperty, value); }
        }

        public static readonly DependencyProperty MaxNumItemsProperty = DependencyProperty.Register(nameof(MaxNumItems), typeof(int), typeof(SupplyCrateItemSet), new PropertyMetadata(1));
        [AggregateIniValueEntry]
        public int MaxNumItems
        {
            get { return (int)GetValue(MaxNumItemsProperty); }
            set { SetValue(MaxNumItemsProperty, value); }
        }

        public static readonly DependencyProperty NumItemsPowerProperty = DependencyProperty.Register(nameof(NumItemsPower), typeof(float), typeof(SupplyCrateItemSet), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float NumItemsPower
        {
            get { return (float)GetValue(NumItemsPowerProperty); }
            set { SetValue(NumItemsPowerProperty, value); }
        }

        public static readonly DependencyProperty SetWeightProperty = DependencyProperty.Register(nameof(SetWeight), typeof(float), typeof(SupplyCrateItemSet), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float SetWeight
        {
            get { return (float)GetValue(SetWeightProperty); }
            set { SetValue(SetWeightProperty, value); }
        }

        public static readonly DependencyProperty bItemsRandomWithoutReplacementProperty = DependencyProperty.Register(nameof(bItemsRandomWithoutReplacement), typeof(bool), typeof(SupplyCrateItemSet), new PropertyMetadata(true));
        [AggregateIniValueEntry]
        public bool bItemsRandomWithoutReplacement
        {
            get { return (bool)GetValue(bItemsRandomWithoutReplacementProperty); }
            set { SetValue(bItemsRandomWithoutReplacementProperty, value); }
        }

        public static readonly DependencyProperty ItemEntriesProperty = DependencyProperty.Register(nameof(ItemEntries), typeof(AggregateIniValueList<SupplyCrateItemSetEntry>), typeof(SupplyCrateItemSet), new PropertyMetadata(null));
        [AggregateIniValueEntry]
        public AggregateIniValueList<SupplyCrateItemSetEntry> ItemEntries
        {
            get { return (AggregateIniValueList<SupplyCrateItemSetEntry>)GetValue(ItemEntriesProperty); }
            set { SetValue(ItemEntriesProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override void InitializeFromINIValue(string value)
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            var propertyNames = this.Properties.Select(p => p.Name).ToArray();

            var kvPair = value.Split(new[] { '=' }, 2);
            var kvValue = kvPair[1].Trim(' ');
            if (kvValue.StartsWith("("))
                kvValue = kvValue.Substring(1);
            if (kvValue.EndsWith(")))"))
                kvValue = kvValue.Substring(0, kvValue.Length - 1);

            var propertyValues = StringUtils.SplitIncludingDelimiters(kvValue, propertyNames);

            foreach (var property in this.Properties)
            {
                var propertyValue = propertyValues.FirstOrDefault(p => p.StartsWith(property.Name));
                if (propertyValue == null)
                    continue;

                var kvPropertyPair = propertyValue.Split(new[] { '=' }, 2);
                var kvPropertyValue = kvPropertyPair[1].Trim(',', ' ');

                var collection = property.GetValue(this) as IIniValuesCollection;
                if (collection != null)
                {
                    if (property.Name == nameof(ItemEntries))
                    {
                        kvPropertyValue = kvPropertyPair[1].Trim();
                        if (kvPropertyValue.StartsWith("(("))
                            kvPropertyValue = kvPropertyValue.Substring(1);
                        if (kvPropertyValue.EndsWith("))"))
                            kvPropertyValue = kvPropertyValue.Substring(0, kvPropertyValue.Length - 1);
                        kvPropertyValue = kvPropertyPair[0].Trim() + "=" + kvPropertyValue;
                        kvPropertyValue = kvPropertyValue.Replace("),(", ")," + kvPropertyPair[0].Trim() + "=(");

                        string[] delimiters = new[] { "," + kvPropertyPair[0].Trim() + "=" };
                        var items = StringUtils.SplitIncludingDelimiters("," + kvPropertyValue, delimiters);
                        collection.FromIniValues(items.Select(i => i.Trim(',', ' ')));
                    }
                    else
                    {
                        collection.FromIniValues(new[] { kvPropertyValue });
                    }
                }
                else
                {
                    StringUtils.SetPropertyValue(kvPropertyValue, this, property);
                }
            }
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override string ToINIValue()
        {
            return base.ToINIValue();
        }
    }

    public class SupplyCrateItemSetEntry : AggregateIniValue
    {
        public SupplyCrateItemSetEntry()
        {
            //ItemClassStrings = new StringIniValueList(nameof(ItemClassStrings), null);
            //ItemsWeights = new SingleIniValueList(nameof(ItemsWeights), null);
        }

        public static readonly DependencyProperty EntryWeightProperty = DependencyProperty.Register(nameof(EntryWeight), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float EntryWeight
        {
            get { return (float)GetValue(EntryWeightProperty); }
            set { SetValue(EntryWeightProperty, value); }
        }

        //public static readonly DependencyProperty ItemClassStringsProperty = DependencyProperty.Register(nameof(ItemClassStrings), typeof(StringIniValueList), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(null));
        //[AggregateIniValueEntry]
        //public StringIniValueList ItemClassStrings
        //{
        //    get { return (StringIniValueList)GetValue(ItemClassStringsProperty); }
        //    set { SetValue(ItemClassStringsProperty, value); }
        //}

        //public static readonly DependencyProperty ItemsWeightsProperty = DependencyProperty.Register(nameof(ItemsWeights), typeof(SingleIniValueList), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(null));
        //[AggregateIniValueEntry]
        //public SingleIniValueList ItemsWeights
        //{
        //    get { return (SingleIniValueList)GetValue(ItemsWeightsProperty); }
        //    set { SetValue(ItemsWeightsProperty, value); }
        //}

        public static readonly DependencyProperty MinQuantityProperty = DependencyProperty.Register(nameof(MinQuantity), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float MinQuantity
        {
            get { return (float)GetValue(MinQuantityProperty); }
            set { SetValue(MinQuantityProperty, value); }
        }

        public static readonly DependencyProperty MaxQuantityProperty = DependencyProperty.Register(nameof(MaxQuantity), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float MaxQuantity
        {
            get { return (float)GetValue(MaxQuantityProperty); }
            set { SetValue(MaxQuantityProperty, value); }
        }

        public static readonly DependencyProperty MinQualityProperty = DependencyProperty.Register(nameof(MinQuality), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float MinQuality
        {
            get { return (float)GetValue(MinQualityProperty); }
            set { SetValue(MinQualityProperty, value); }
        }

        public static readonly DependencyProperty MaxQualityProperty = DependencyProperty.Register(nameof(MaxQuality), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float MaxQuality
        {
            get { return (float)GetValue(MaxQualityProperty); }
            set { SetValue(MaxQualityProperty, value); }
        }

        public static readonly DependencyProperty bForceBlueprintProperty = DependencyProperty.Register(nameof(bForceBlueprint), typeof(bool), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(false));
        [AggregateIniValueEntry]
        public bool bForceBlueprint
        {
            get { return (bool)GetValue(bForceBlueprintProperty); }
            set { SetValue(bForceBlueprintProperty, value); }
        }

        public static readonly DependencyProperty ChanceToBeBlueprintOverrideProperty = DependencyProperty.Register(nameof(ChanceToBeBlueprintOverride), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(0.0f));
        [AggregateIniValueEntry]
        public float ChanceToBeBlueprintOverride
        {
            get { return (float)GetValue(ChanceToBeBlueprintOverrideProperty); }
            set { SetValue(ChanceToBeBlueprintOverrideProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override void InitializeFromINIValue(string value)
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            var propertyNames = this.Properties.Select(p => p.Name).ToArray();

            var kvPair = value.Split(new[] { '=' }, 2);
            var kvValue = kvPair[1].Trim(' ');
            if (kvValue.StartsWith("("))
                kvValue = kvValue.Substring(1);
            if (kvValue.EndsWith(")"))
                kvValue = kvValue.Substring(0, kvValue.Length - 1);

            var propertyValues = StringUtils.SplitIncludingDelimiters(kvValue, propertyNames);

            foreach (var property in this.Properties)
            {
                var propertyValue = propertyValues.FirstOrDefault(p => p.StartsWith(property.Name));
                if (propertyValue == null)
                    continue;

                var kvPropertyPair = propertyValue.Split(new[] { '=' }, 2);
                var kvPropertyValue = kvPropertyPair[1].Trim(',', ' ');

                var collection = property.GetValue(this) as IIniValuesCollection;
                if (collection != null)
                {
                    //if (property.Name == nameof(ItemClassStrings))
                    //{
                    //    kvPropertyValue = kvPropertyPair[1].Trim(',', ' ');
                    //    if (kvPropertyValue.StartsWith("("))
                    //        kvPropertyValue = kvPropertyValue.Substring(1);
                    //    if (kvPropertyValue.EndsWith(")"))
                    //        kvPropertyValue = kvPropertyValue.Substring(0, kvPropertyValue.Length - 1);
                    //    kvPropertyValue = kvPropertyPair[0].Trim() + "=" + kvPropertyValue;
                    //    kvPropertyValue = kvPropertyValue.Replace("),(", ")," + kvPropertyPair[0].Trim() + "=(");

                    //    string[] delimiters = new[] { "," + kvPropertyPair[0].Trim() + "=" };
                    //    var items = StringUtils.SplitIncludingDelimiters("," + kvPropertyValue, delimiters);
                    //    collection.FromIniValues(items.Select(i => i.Trim(',', ' ')));
                    //}
                    //else if (property.Name == nameof(ItemsWeights))
                    //{

                    //}
                    //else
                    //{
                    //    collection.FromIniValues(new[] { kvPropertyValue });
                    //}
                }
                else
                {
                    StringUtils.SetPropertyValue(kvPropertyValue, this, property);
                }
            }
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }
    }
}
