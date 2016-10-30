using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using ARK_Server_Manager.Lib.ViewModel;

namespace ARK_Server_Manager.Lib
{
    public class Crafting : AggregateIniValue
    {
        private const char DELIMITER = ',';

        public Crafting()
        {
            BaseCraftingResourceRequirements = new AggregateIniValueList<CraftingResourceRequirement>(nameof(BaseCraftingResourceRequirements), null);
        }

        public static readonly DependencyProperty ItemClassStringProperty = DependencyProperty.Register(nameof(ItemClassString), typeof(string), typeof(Crafting), new PropertyMetadata(string.Empty));
        [AggregateIniValueEntry]
        public string ItemClassString
        {
            get { return (string)GetValue(ItemClassStringProperty); }
            set
            {
                SetValue(ItemClassStringProperty, value);
                DisplayName = PrimalItemClassNameToDisplayNameConverter.Convert(value).ToString();
            }
        }

        public static readonly DependencyProperty BaseCraftingResourceRequirementsProperty = DependencyProperty.Register(nameof(BaseCraftingResourceRequirements), typeof(AggregateIniValueList<CraftingResourceRequirement>), typeof(Crafting), new PropertyMetadata(null));
        [AggregateIniValueEntry]
        public AggregateIniValueList<CraftingResourceRequirement> BaseCraftingResourceRequirements
        {
            get { return (AggregateIniValueList<CraftingResourceRequirement>)GetValue(BaseCraftingResourceRequirementsProperty); }
            set { SetValue(BaseCraftingResourceRequirementsProperty, value); }
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

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return string.Equals(this.ItemClassString, ((Crafting)other).ItemClassString, StringComparison.OrdinalIgnoreCase);
        }

        public override void InitializeFromINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

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
                    if (property.Name == nameof(BaseCraftingResourceRequirements))
                    {
                        kvPropertyValue = kvPropertyPair[1].Trim();
                        if (kvPropertyValue.StartsWith("(("))
                            kvPropertyValue = kvPropertyValue.Substring(1);
                        if (kvPropertyValue.EndsWith("))"))
                            kvPropertyValue = kvPropertyValue.Substring(0, kvPropertyValue.Length - 1);
                        kvPropertyValue = kvPropertyPair[0].Trim() + "=" + kvPropertyValue;
                        kvPropertyValue = kvPropertyValue.Replace("),(", ")," + kvPropertyPair[0].Trim() + "=(");

                        string[] delimiters = { "," + kvPropertyPair[0].Trim() + "=" };
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

        public override string ToINIValue()
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            StringBuilder result = new StringBuilder();
            result.Append("(");

            var delimiter = "";
            foreach (var property in this.Properties)
            {
                result.Append(delimiter);

                var collection = property.GetValue(this) as IIniValuesCollection;
                if (collection != null)
                {
                    result.Append($"{property.Name}=(");

                    var vals = collection.ToIniValues();

                    var delimiter2 = DELIMITER.ToString();
                    foreach (var val in vals)
                    {
                        result.Append(delimiter2);

                        if (property.Name == nameof(BaseCraftingResourceRequirements))
                        {
                            if (val.StartsWith(property.Name))
                                result.Append(val.Substring(property.Name.Length + 1));
                            else
                                result.Append(val);
                        }
                        else
                            result.Append(val);

                        delimiter2 = "";
                    }
                    result.Append(")");
                }
                else
                {
                    var val = property.GetValue(this);
                    var propertyValue = StringUtils.GetPropertyValue(val, property);

                    result.Append($"{property.Name}={propertyValue}");
                }

                delimiter = DELIMITER.ToString();
            }

            result.Append(")");
            return result.ToString();
        }
    }

    public class CraftingResourceRequirement : AggregateIniValue
    {
        public static readonly DependencyProperty ResourceItemTypeStringProperty = DependencyProperty.Register(nameof(ResourceItemTypeString), typeof(string), typeof(CraftingResourceRequirement), new PropertyMetadata(string.Empty));
        [AggregateIniValueEntry]
        public string ResourceItemTypeString
        {
            get { return (string)GetValue(ResourceItemTypeStringProperty); }
            set
            {
                SetValue(ResourceItemTypeStringProperty, value);
                DisplayName = PrimalItemClassNameToDisplayNameConverter.Convert(value).ToString();
            }
        }

        public static readonly DependencyProperty BaseResourceRequirementProperty = DependencyProperty.Register(nameof(BaseResourceRequirement), typeof(float), typeof(CraftingResourceRequirement), new PropertyMetadata(1.0f));
        [AggregateIniValueEntry]
        public float BaseResourceRequirement
        {
            get { return (float)GetValue(BaseResourceRequirementProperty); }
            set { SetValue(BaseResourceRequirementProperty, value); }
        }

        public static readonly DependencyProperty bCraftingRequireExactResourceTypeProperty = DependencyProperty.Register(nameof(bCraftingRequireExactResourceType), typeof(bool), typeof(CraftingResourceRequirement), new PropertyMetadata(false));
        [AggregateIniValueEntry]
        public bool bCraftingRequireExactResourceType
        {
            get { return (bool)GetValue(bCraftingRequireExactResourceTypeProperty); }
            set { SetValue(bCraftingRequireExactResourceTypeProperty, value); }
        }

        public string DisplayName
        {
            get;
            protected set;
        }

        public bool KnownItem
        {
            get
            {
                return GameData.HasEngramForClass(ResourceItemTypeString);
            }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return string.Equals(this.ResourceItemTypeString, ((CraftingResourceRequirement)other).ResourceItemTypeString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
