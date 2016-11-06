using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class SupplyCrateItemEntrySettings : DependencyObject
    {
        public static readonly DependencyProperty ItemClassStringProperty = DependencyProperty.Register(nameof(ItemClassString), typeof(string), typeof(SupplyCrateItemEntrySettings), new PropertyMetadata(string.Empty));
        public string ItemClassString
        {
            get { return (string)GetValue(ItemClassStringProperty); }
            set { SetValue(ItemClassStringProperty, value); }
        }

        public static readonly DependencyProperty EntryWeightProperty = DependencyProperty.Register(nameof(EntryWeight), typeof(float), typeof(SupplyCrateItemEntrySettings), new PropertyMetadata(1.0f));
        public float EntryWeight
        {
            get { return (float)GetValue(EntryWeightProperty); }
            set { SetValue(EntryWeightProperty, value); }
        }

        public string DisplayName => GameData.FriendlyNameForClass(ItemClassString);

        public bool IsValid => !string.IsNullOrWhiteSpace(ItemClassString);
    }
}
