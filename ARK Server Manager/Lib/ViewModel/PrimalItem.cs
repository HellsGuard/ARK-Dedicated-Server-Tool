using System;
using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class PrimalItem : DependencyObject
    {
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(PrimalItem), new PropertyMetadata(String.Empty));

        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public string DisplayName
        {
            get
            {
                return PrimalItemClassNameToDisplayNameConverter.Convert(ClassName).ToString();
            }
        }

        public bool KnownItem
        {
            get
            {
                return GameData.HasPrimalItemForClass(ClassName);
            }
        }

        public PrimalItem Duplicate()
        {
            var properties = this.GetType().GetProperties();

            var result = new PrimalItem();
            foreach (var prop in properties)
            {
                prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }
    }
}
