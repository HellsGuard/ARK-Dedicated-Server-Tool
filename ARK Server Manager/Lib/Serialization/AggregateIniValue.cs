using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>
        /// Attribute for the IniFile value
        /// </summary>
        /// <param name="key">The key of the value.  Defaults to the same name as the attributed field.</param>
        public AggregateIniValueEntryAttribute(string key = "")
        {
            this.Key = key;
        }

        /// <summary>
        /// The key of the value.
        /// </summary>
        public string Key;

        /// <summary>
        /// If true, the value will always be surrounded with brackets
        /// </summary>
        public bool ValueWithinBrackets;

        /// <summary>
        /// If true, the every list value will always be surrounded with brackets
        /// </summary>
        public bool ListValueWithinBrackets;

        /// <summary>
        /// The delimiter used between each of the values.
        /// </summary>
        public string Delimiter;
    }

    /// <summary>
    /// An INI style value of the form AggregateName=(Key1=val1, Key2=val2...)
    /// </summary>
    public abstract class AggregateIniValue : DependencyObject
    {
        private const char DELIMITER = ',';

        protected readonly List<PropertyInfo> Properties = new List<PropertyInfo>();

        public T Duplicate<T>() where T : AggregateIniValue, new()
        {
            GetPropertyInfos(true);

            var result = new T();
            foreach (var prop in this.Properties.Where(prop => prop.CanWrite))
            {
                prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }

        public static T FromINIValue<T>(string value) where T : AggregateIniValue, new()
        {
            var result = new T();
            result.InitializeFromINIValue(value);
            return result;
        }

        protected void GetPropertyInfos(bool allProperties = false)
        {
            if (this.Properties.Count != 0)
                return;

            if (allProperties)
                this.Properties.AddRange(this.GetType().GetProperties());
            else
                this.Properties.AddRange(this.GetType().GetProperties().Where(p => p.GetCustomAttribute(typeof(AggregateIniValueEntryAttribute)) != null));
        }

        public abstract string GetSortKey();

        public virtual void InitializeFromINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return;

            var kvPair = value.Split(new[] { '=' }, 2);
            value = kvPair[1].Trim('(', ')', ' ');
            var pairs = value.Split(DELIMITER);

            foreach (var pair in pairs)
            {
                kvPair = pair.Split('=');
                if (kvPair.Length != 2)
                    continue;

                var key = kvPair[0].Trim();
                var val = kvPair[1].Trim();
                var propInfo = this.Properties.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
                if (propInfo != null)
                    StringUtils.SetPropertyValue(val, this, propInfo);
                else
                {
                    propInfo = this.Properties.FirstOrDefault(f => f.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().Any(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase)));
                    if (propInfo != null)
                        StringUtils.SetPropertyValue(val, this, propInfo);
                }
            }
        }

        public abstract bool IsEquivalent(AggregateIniValue other);

        public virtual bool ShouldSave() { return true; }

        internal static object SortKeySelector(AggregateIniValue arg)
        {
            return arg.GetSortKey();
        }

        public virtual string ToINIValue()
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();
            result.Append("(");

            var delimiter = "";
            foreach (var prop in this.Properties)
            {
                result.Append(delimiter);

                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

                var val = prop.GetValue(this);
                var propValue = StringUtils.GetPropertyValue(val, prop);

                result.Append($"{propName}={propValue}");

                delimiter = DELIMITER.ToString();
            }

            result.Append(")");
            return result.ToString();
        }

        public override string ToString()
        {
            return ToINIValue();
        }
    }
}
