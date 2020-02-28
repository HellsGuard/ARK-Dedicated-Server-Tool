using System.Windows;

namespace ARK_Server_Manager.Lib.Model
{
    public class ArkApplicationComboBoxItem : DependencyObject
    {
        public static readonly DependencyProperty ValueMemberProperty = DependencyProperty.Register(nameof(ValueMember), typeof(ArkApplication), typeof(ArkApplicationComboBoxItem), new PropertyMetadata(ArkApplication.All));
        public static readonly DependencyProperty DisplayMemberProperty = DependencyProperty.Register(nameof(DisplayMember), typeof(string), typeof(ArkApplicationComboBoxItem), new PropertyMetadata(string.Empty));

        public ArkApplicationComboBoxItem()
        {
        }

        public ArkApplicationComboBoxItem(ArkApplication valueMember, string displayMember)
        {
            ValueMember = valueMember;
            DisplayMember = displayMember;
        }

        public ArkApplication ValueMember
        {
            get { return (ArkApplication)GetValue(ValueMemberProperty); }
            set { SetValue(ValueMemberProperty, value); }
        }

        public string DisplayMember
        {
            get { return (string)GetValue(DisplayMemberProperty); }
            set { SetValue(DisplayMemberProperty, value); }
        }

        public ArkApplicationComboBoxItem Duplicate()
        {
            return new ArkApplicationComboBoxItem
            {
                DisplayMember = this.DisplayMember,
                ValueMember = this.ValueMember,
            };
        }
    }

    public class ArkApplicationComboBoxItemList : SortableObservableCollection<ArkApplicationComboBoxItem>
    {
        public ArkApplicationComboBoxItemList()
        {
        }
    }
}
