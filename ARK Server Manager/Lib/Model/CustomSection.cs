using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ARK_Server_Manager.Lib.Model
{
    public class CustomSectionList : SortableObservableCollection<CustomSection>, IIniSectionCollection
    {
        public IIniValuesCollection[] Sections
        {
            get
            {
                return this.ToArray();
            }
        }

        public void Add(string sectionName, string[] values)
        {
            Add(sectionName, values, true);
        }

        public void Add(string sectionName, string[] values, bool clearExisting)
        {
            var section = this.Items.FirstOrDefault(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase) && !s.IsDeleted);
            if (section == null)
            {
                section = new CustomSection();
                section.SectionName = sectionName;

                this.Add(section);
            }

            if (clearExisting)
                section.Clear();
            section.FromIniValues(values);
        }

        public new void Clear()
        {
            foreach (var section in this)
            {
                section.IsDeleted = true;
            }
            Update();
        }

        public new void Remove(CustomSection item)
        {
            if (item != null)
                item.IsDeleted = true;
            Update();
        }

        public override string ToString()
        {
            return $"Count={Count}";
        }

        public void Update()
        {
            foreach (var section in this)
            {
                section.Update();
            }

            this.Sort(s => s.SectionName);
        }
    }

    public class CustomSection : DependencyObject, IIniValuesCollection, IEnumerable<CustomItem>
    {
        public CustomSection()
        {
            SectionItems = new ObservableCollection<CustomItem>();
            Update();
        }

        public static readonly DependencyProperty IsDeletedProperty = DependencyProperty.Register(nameof(IsDeleted), typeof(bool), typeof(CustomSection), new PropertyMetadata(false));
        public bool IsDeleted
        {
            get { return (bool)GetValue(IsDeletedProperty); }
            set { SetValue(IsDeletedProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(CustomSection), new PropertyMetadata(false));
        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty SectionItemsProperty = DependencyProperty.Register(nameof(SectionItems), typeof(ObservableCollection<CustomItem>), typeof(CustomSection), new PropertyMetadata(null));
        public ObservableCollection<CustomItem> SectionItems
        {
            get { return (ObservableCollection<CustomItem>)GetValue(SectionItemsProperty); }
            set { SetValue(SectionItemsProperty, value); }
        }

        public static readonly DependencyProperty SectionNameProperty = DependencyProperty.Register(nameof(SectionName), typeof(string), typeof(CustomSection), new PropertyMetadata(string.Empty));
        public string SectionName
        {
            get { return (string)GetValue(SectionNameProperty); }
            set { SetValue(SectionNameProperty, value); }
        }

        public bool IsArray => false;

        public string IniCollectionKey => SectionName;

        public void Add(string itemKey, string itemValue)
        {
            var item = new CustomItem();
            item.ItemKey = itemKey;
            item.ItemValue = itemValue;
            SectionItems.Add(item);

            Update();
        }

        public void AddRange(IEnumerable<CustomItem> values)
        {
            foreach (var value in values)
            {
                SectionItems.Add(value);
            }

            Update();
        }

        public void Clear()
        {
            SectionItems.Clear();
        }

        public void FromIniValues(IEnumerable<string> values)
        {
            AddRange(values.Select(v => CustomItem.FromINIValue(v)));
        }

        public IEnumerator<CustomItem> GetEnumerator()
        {
            return SectionItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return SectionItems.GetEnumerator();
        }

        public bool Remove(CustomItem item)
        {
            return SectionItems.Remove(item);
        }

        public IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            values.AddRange(SectionItems.Select(i => i.ToINIValue()).Where(i => i != null));
            return values;
        }

        public override string ToString()
        {
            return $"{SectionName}; Count={SectionItems.Count}";
        }

        public void Update()
        {
            this.IsEnabled = (!IsDeleted && SectionItems.Count != 0);
        }
    }

    public class CustomItem : DependencyObject
    {
        public static readonly DependencyProperty ItemKeyProperty = DependencyProperty.Register(nameof(ItemKey), typeof(string), typeof(CustomItem), new PropertyMetadata(string.Empty));
        public string ItemKey
        {
            get { return (string)GetValue(ItemKeyProperty); }
            set { SetValue(ItemKeyProperty, value); }
        }

        public static readonly DependencyProperty ItemValueProperty = DependencyProperty.Register(nameof(ItemValue), typeof(string), typeof(CustomItem), new PropertyMetadata(string.Empty));
        public string ItemValue
        {
            get { return (string)GetValue(ItemValueProperty); }
            set { SetValue(ItemValueProperty, value); }
        }

        public static CustomItem FromINIValue(string value)
        {
            var result = new CustomItem();
            result.InitializeFromINIValue(value);
            return result;
        }

        protected virtual void InitializeFromINIValue(string value)
        {
            var kvPair = value.Split(new[] { '=' }, 2);
            if (kvPair.Length > 1)
            {
                ItemKey = kvPair[0];
                ItemValue = kvPair[1];
            }
            else if (kvPair.Length > 0)
            {
                ItemKey = kvPair[0];
                ItemValue = string.Empty;
            }
        }

        public virtual string ToINIValue()
        {
            return this.ToString();
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(ItemKey))
                return null;
            return $"{ItemKey}={ItemValue}";
        }
    }
}
