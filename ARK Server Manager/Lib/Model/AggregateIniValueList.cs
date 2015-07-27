using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class AggregateIniValueList<T> : SortableObservableCollection<T>, IIniValuesCollection
         where T : AggregateIniValue
    {
        private Func<string, T> valueFactory;
        private string aggregateValueName;

        protected AggregateIniValueList(Func<string, T> valueFactory, string aggregateValueName)
        {
            this.valueFactory = valueFactory;
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
            this.AddRange(iniValues.Select(v => valueFactory(v)));
        }

        public IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            values.AddRange(this.Select(d => String.Format("{0}={1}", this.aggregateValueName, d.ToINIValue())));
            return values;
        }
    }

}
