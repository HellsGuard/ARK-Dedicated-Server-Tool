using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public abstract class IniValueList<T> : SortableObservableCollection<T>, IIniValuesCollection
    {
        public Func<T, string> ToIniValue { get; }
        public Func<string, T> FromIniValue { get; }
        private Func<IEnumerable<T>> ResetFunc { get; }
        private Func<T, T, bool> EquivalencyFunc { get; }
        private Func<T, object> SortKeySelectorFunc { get; set; }

        public IniValueList(
            string aggregateValueName, 
            Func<IEnumerable<T>> resetFunc,
            Func<T, T, bool> equivalencyFunc,
            Func<T, object> sortKeySelectorFunc,
            Func<T, string> toIniValue, 
            Func<string, T> fromIniValue)
        {
            this.ToIniValue = toIniValue;
            this.FromIniValue = fromIniValue;
            this.ResetFunc = resetFunc;
            this.EquivalencyFunc = equivalencyFunc;
            this.SortKeySelectorFunc = sortKeySelectorFunc;
            this.IniCollectionKey = aggregateValueName;
        }

        private bool isEnabled;
        public bool IsEnabled
        {
            get { return this.isEnabled; }
            set
            {
                this.isEnabled = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }

        public abstract bool IsArray { get; }

        public string IniCollectionKey { get; }

        public void Reset()
        {
            this.Clear();
            this.AddRange(this.ResetFunc());
        }

        public void FromIniValues(IEnumerable<string> values)
        {
            this.Clear();
            if (this.IsArray)
            {
                var list = new List<T>();
                list.AddRange(this.ResetFunc());
                foreach(var v in values)
                {
                    int indexStart = v.IndexOf('[');
                    int indexEnd = v.IndexOf(']');
                    if(indexStart >= indexEnd)
                    {
                        // Invalid format
                        continue;
                    }

                    int index;
                    if(!Int32.TryParse(v.Substring(indexStart + 1, indexEnd - indexStart - 1), out index))
                    {
                        // Invalid index
                        continue;
                    }

                    if(index >= list.Count)
                    {
                        // Unexpected size
                        continue;
                    }

                    list[index] = this.FromIniValue(v.Substring(v.IndexOf('=') + 1).Trim());
                }
                this.AddRange(list);
            }
            else
            {
                
                this.AddRange(values.Select(v => v.Substring(v.IndexOf('=') + 1)).Select(this.FromIniValue));
                this.IsEnabled = (this.Count != 0);

                // Add any default values which were missing
                var defaultItemsToAdd = this.ResetFunc().Where(r => !this.Any(v => this.EquivalencyFunc(v, r))).ToArray();
                this.AddRange(defaultItemsToAdd);
                this.Sort(this.SortKeySelectorFunc);
            }            
        }

        public IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            if (this.IsArray)
            {
                for(int i = 0; i < this.Count; i++)
                {
                    values.Add($"{this.IniCollectionKey}[{i}]={this.ToIniValue(this[i])}");
                }
            }
            else
            {
                values.AddRange(this.Select(d => $"{this.IniCollectionKey}={this.ToIniValue(d)}"));
            }
            return values;
        }

        public void AddRange(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                base.Add(value);
            }
        }
    }
}
