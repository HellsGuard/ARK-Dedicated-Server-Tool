using System.Windows.Controls;
using System.Windows.Data;

namespace WPFSharp.Globalizer.Controls
{
    public partial class StyleSelectionComboBox : ComboBox
    {
        public StyleSelectionComboBox()
        {
            var itemSourceBinding = new Binding
            {
                Source = AvailableStyles.Instance,
                BindsDirectlyToSource = true,
            };
            SetBinding(ItemsSourceProperty, itemSourceBinding);

            SelectionChanged += LanguageSelectionComboBox_SelectionChanged;

            var selectedItemBinding = new Binding("SelectedStyle")
            {
                Source = AvailableStyles.Instance,
                Mode = BindingMode.OneWay
            };
            SetBinding(SelectedItemProperty, selectedItemBinding);
        }

        void LanguageSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lang = e.AddedItems[0].ToString();
            if (!string.IsNullOrWhiteSpace(lang))
                GlobalizedApplication.Instance.StyleManager.SwitchStyle($"{lang}.xaml");
        }
    }
}
