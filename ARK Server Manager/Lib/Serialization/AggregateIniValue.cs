using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AggregateIniValueEntryAttribute : Attribute
    {
    }

    /// <summary>
    /// An INI style value of the form AggrevateName=(Key1=val1, Key2=val2...)
    /// </summary>
    public abstract class AggregateIniValue : DependencyObject
    {
        private readonly List<PropertyInfo> properties = new List<PropertyInfo>();

        public static T FromINIValue<T>(string value) where T : AggregateIniValue, new()
        {
            var result = new T();
            result.InitializeFromINIValue(value);
            return result;
        }

        public T Duplicate<T>() where T : AggregateIniValue, new()
        {
            GetPropertyInfos();
            var result = new T();
            foreach (var prop in this.properties)
            {
                prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }

        public string ToINIValue()
        {
            GetPropertyInfos();
            StringBuilder result = new StringBuilder();
            result.Append("(");

            bool firstItem = true;
            foreach (var prop in this.properties)
            {
                if(!firstItem)
                {
                    result.Append(',');
                }

                var val = prop.GetValue(this);
                var convertedVal = Convert.ToString(val);
                result.Append(prop.Name).Append('=');
                if (prop.PropertyType == typeof(String))
                {
                    result.Append('"').Append(convertedVal).Append('"');
                }
                else
                {
                    result.Append(convertedVal);
                }

                firstItem = false;
            }

            result.Append(")");
            return result.ToString();
        }

        internal static object SortKeySelector(AggregateIniValue arg)
        {
            return arg.GetSortKey();
        }

        public abstract bool IsEquivalent(AggregateIniValue other);
        public abstract string GetSortKey();

        private void GetPropertyInfos()
        {
            if (this.properties.Count == 0)
            {
                this.properties.AddRange(this.GetType().GetProperties().Where(p => p.GetCustomAttribute(typeof(AggregateIniValueEntryAttribute)) != null));
            }
        }

        protected void InitializeFromINIValue(string value)
        {
            GetPropertyInfos();
            var kvPair = value.Split(new [] { '='} , 2);
            value = kvPair[1].Trim('(', ')', ' ');
            var pairs = value.Split(',');
            foreach(var pair in pairs)
            {
                kvPair = pair.Split('=');
                if(kvPair.Length == 2)
                {
                    var key = kvPair[0].Trim();
                    var val = kvPair[1].Trim();
                    var propInfo = this.properties.FirstOrDefault(p => String.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
                    if(propInfo != null)
                    {
                        object convertedValue = Convert.ChangeType(val, propInfo.PropertyType);
                        if(convertedValue.GetType() == typeof(String))
                        {
                            convertedValue = (convertedValue as string).Trim('"');
                        }
                        propInfo.SetValue(this, convertedValue);
                    }
                }
            }            
        }
    }
}
