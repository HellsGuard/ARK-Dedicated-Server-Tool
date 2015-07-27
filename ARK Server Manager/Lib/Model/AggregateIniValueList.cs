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
        private string aggregateValueName;

        public AggregateIniValueList(string aggregateValueName)
        {
            this.aggregateValueName = aggregateValueName;
        }

        public void AddRange(IEnumerable<T> spawns)
        {
            foreach (var spawn in spawns)
            {
                base.Add(spawn);
            }
        }

        protected void InitializeFromINIValues(IEnumerable<string> iniValues)
        {
            this.AddRange(iniValues.Select(v => AggregateIniValue.FromINIValue<T>(v)));
        }

        public IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            values.AddRange(this.Select(d => String.Format("{0}={1}", this.aggregateValueName, d.ToINIValue())));
            return values;
        }

        public static AggregateIniValueList<T> FromINIValues(string aggregateValueName, IEnumerable<string> iniValues)
        {
            var spawns = new AggregateIniValueList<T>(aggregateValueName);
            spawns.InitializeFromINIValues(iniValues);
            return spawns;
        }
    }

}
