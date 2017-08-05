using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class ComboBoxItem : DependencyObject
    {
        public ComboBoxItem()
        {
        }

        public ComboBoxItem(string valueMember, string displayMember)
        {
            ValueMember = valueMember;
            DisplayMember = displayMember;
        }

        public static readonly DependencyProperty DisplayMemberProperty = DependencyProperty.Register(nameof(DisplayMember), typeof(string), typeof(ComboBoxItem), new PropertyMetadata(string.Empty));
        public string DisplayMember
        {
            get { return (string)GetValue(DisplayMemberProperty); }
            set { SetValue(DisplayMemberProperty, value); }
        }

        public static readonly DependencyProperty ValueMemberProperty = DependencyProperty.Register(nameof(ValueMember), typeof(string), typeof(ComboBoxItem), new PropertyMetadata(string.Empty));
        public string ValueMember
        {
            get { return (string)GetValue(ValueMemberProperty); }
            set { SetValue(ValueMemberProperty, value); }
        }

        public ComboBoxItem Duplicate()
        {
            return new ComboBoxItem
            {
                DisplayMember = this.DisplayMember,
                ValueMember = this.ValueMember,
            };
        }
    }

    public class ComboBoxItemList : SortableObservableCollection<ComboBoxItem>
    {
        public ComboBoxItemList()
        {
        }
    }
}
