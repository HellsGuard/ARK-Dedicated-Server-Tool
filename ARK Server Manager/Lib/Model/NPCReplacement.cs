using System;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class NPCReplacement : AggregateIniValue
    {
        public static readonly DependencyProperty FromClassNameProperty = DependencyProperty.Register(nameof(FromClassName), typeof(string), typeof(NPCReplacement), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ToClassNameProperty = DependencyProperty.Register(nameof(ToClassName), typeof(string), typeof(NPCReplacement), new PropertyMetadata(String.Empty));

        [AggregateIniValueEntry]
        public string FromClassName
        {
            get { return (string)GetValue(FromClassNameProperty); }
            set { SetValue(FromClassNameProperty, value); }
        }

        [AggregateIniValueEntry]
        public string ToClassName
        {
            get { return (string)GetValue(ToClassNameProperty); }
            set { SetValue(ToClassNameProperty, value); }
        }
      
        public static NPCReplacement FromINIValue(string iniValue)
        {
            var newSpawn = new NPCReplacement();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey()
        {
            return this.FromClassName;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.FromClassName, ((NPCReplacement)other).FromClassName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool ShouldSave()
        {
            return (!String.Equals(FromClassName, ToClassName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
