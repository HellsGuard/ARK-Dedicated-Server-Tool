using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class ComboBoxItem : DependencyObject
    {
        public static readonly DependencyProperty ValueMemberProperty = DependencyProperty.Register(nameof(ValueMember), typeof(string), typeof(ComboBoxItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty DisplayMemberProperty = DependencyProperty.Register(nameof(DisplayMember), typeof(string), typeof(ComboBoxItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty GroupMemberProperty = DependencyProperty.Register(nameof(GroupMember), typeof(string), typeof(ComboBoxItem), new PropertyMetadata(string.Empty));

        public ComboBoxItem()
        {
        }

        public ComboBoxItem(string valueMember, string displayMember)
        {
            ValueMember = valueMember;
            DisplayMember = displayMember;
        }

        public ComboBoxItem(string valueMember, string displayMember, string groupMember)
        {
            ValueMember = valueMember;
            DisplayMember = displayMember;
            GroupMember = groupMember;
        }

        public string ValueMember
        {
            get { return (string)GetValue(ValueMemberProperty); }
            set { SetValue(ValueMemberProperty, value); }
        }

        public string DisplayMember
        {
            get { return (string)GetValue(DisplayMemberProperty); }
            set { SetValue(DisplayMemberProperty, value); }
        }

        public string GroupMember
        {
            get { return (string)GetValue(GroupMemberProperty); }
            set { SetValue(GroupMemberProperty, value); }
        }

        public string DisplayName => GameData.FriendlyNameForClass(ValueMember, true) ?? (DisplayMember ?? ValueMember);

        public ComboBoxItem Duplicate()
        {
            return new ComboBoxItem
            {
                DisplayMember = this.DisplayMember,
                ValueMember = this.ValueMember,
                GroupMember = this.GroupMember,
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
