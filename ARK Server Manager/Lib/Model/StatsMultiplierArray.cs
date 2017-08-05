using System;
using System.Collections.Generic;

namespace ARK_Server_Manager.Lib.Model
{
    public class StatsMultiplierArray : FloatIniValueArray
    {
        public StatsMultiplierArray(string iniKeyName, Func<IEnumerable<float>> resetFunc, List<int> exclusions)
            : base(iniKeyName, resetFunc)
        {
            Exclusions = exclusions ?? new List<int>();
        }

        private List<int> Exclusions
        {
            get;
            set;
        }

        public override void FromIniValues(IEnumerable<string> values)
        {
            this.Clear();

            var list = new List<float>();
            if (this.ResetFunc != null)
                list.AddRange(this.ResetFunc());

            foreach (var v in values)
            {
                var indexStart = v.IndexOf('[');
                var indexEnd = v.IndexOf(']');

                if (indexStart >= indexEnd)
                {
                    // Invalid format
                    continue;
                }

                if (!int.TryParse(v.Substring(indexStart + 1, indexEnd - indexStart - 1), out int index))
                {
                    // Invalid index
                    continue;
                }

                if (index >= list.Count)
                {
                    // Unexpected size
                    continue;
                }

                list[index] = this.FromIniValue(v.Substring(v.IndexOf('=') + 1).Trim());
                this.IsEnabled = true;
            }

            this.AddRange(list);
        }

        public override IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            for (var i = 0; i < this.Count; i++)
            {
                if (Exclusions?.Contains(i) ?? false)
                    continue;

                if (string.IsNullOrWhiteSpace(IniCollectionKey))
                    values.Add(this.ToIniValue(this[i]));
                else
                    values.Add($"{this.IniCollectionKey}[{i}]={this.ToIniValue(this[i])}");
            }
            return values;
        }
    }
}
