﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ARK_Server_Manager.Lib
{
    public class AggregateIniValueList<T> : SortableObservableCollection<T>, IIniValuesCollection
         where T : AggregateIniValue, new()
    {
        protected readonly Func<IEnumerable<T>> _resetFunc;
        private bool _isEnabled;

        public AggregateIniValueList(string aggregateValueName, Func<IEnumerable<T>> resetFunc)
        {
            this.IniCollectionKey = aggregateValueName;
            this._resetFunc = resetFunc;
        }

        public string IniCollectionKey { get; }

        public bool IsEnabled
        {
            get { return this._isEnabled; }
            set
            {
                this._isEnabled = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }

        public bool IsArray => false;

        public virtual void AddRange(IEnumerable<T> values)
        {
            if (values == null)
                return;

            foreach (var value in values)
            {
                base.Add(value);
            }
        }

        public void Reset()
        {
            this.Clear();
            if (this._resetFunc != null)
                this.AddRange(this._resetFunc());

            this.Sort(AggregateIniValue.SortKeySelector);
        }

        public virtual void FromIniValues(IEnumerable<string> iniValues)
        {
            var items = iniValues?.Select(AggregateIniValue.FromINIValue<T>).ToArray();

            Clear();
            AddRange(items);
            IsEnabled = (Count != 0);

            // Add any default values which were missing
            if (_resetFunc != null)
            {
                var defaultItemsToAdd = _resetFunc().Where(r => !this.Any(v => v.IsEquivalent(r))).ToArray();
                AddRange(defaultItemsToAdd);
            }

            Sort(AggregateIniValue.SortKeySelector);
        }

        public virtual IEnumerable<string> ToIniValues()
        {
            if (string.IsNullOrWhiteSpace(IniCollectionKey))
                return this.Where(d => d.ShouldSave()).Select(d => d.ToINIValue());

            return this.Where(d => d.ShouldSave()).Select(d => $"{this.IniCollectionKey}={d.ToINIValue()}");
        }
    }
}
