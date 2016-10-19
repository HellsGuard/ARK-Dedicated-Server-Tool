using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class AggregateIniValueList<T> : SortableObservableCollection<T>, IIniValuesCollection
         where T : AggregateIniValue, new()
    {
        private bool _isEnabled;
        private readonly Func<IEnumerable<T>> _resetFunc;

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

        public void AddRange(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                base.Add(value);
            }
        }

        public string IniCollectionKey { get; }

        public AggregateIniValueList(string aggregateValueName, Func<IEnumerable<T>> resetFunc)
        {
            this.IniCollectionKey = aggregateValueName;
            this._resetFunc = resetFunc;
        }


        public void Reset()
        {
            this.Clear();
            if (this._resetFunc != null)
                this.AddRange(this._resetFunc());

            this.Sort(AggregateIniValue.SortKeySelector);
        }

        public virtual IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            values.AddRange(this.Where(d => d.ShouldSave()).Select(d => $"{this.IniCollectionKey}={d.ToINIValue()}"));
            return values;
        }

        public virtual void FromIniValues(IEnumerable<string> iniValues)
        {
            this.Clear();
            this.AddRange(iniValues.Select(v => AggregateIniValue.FromINIValue<T>(v)));
            this.IsEnabled = (this.Count != 0);

            // Add any default values which were missing
            if (this._resetFunc != null)
            {
                var defaultItemsToAdd = this._resetFunc().Where(r => !this.Any(v => v.IsEquivalent(r))).ToArray();
                this.AddRange(defaultItemsToAdd);
            }

            this.Sort(AggregateIniValue.SortKeySelector);
        }
    }
}
