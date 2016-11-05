using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class SupplyCrate : DependencyObject
    {
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(SupplyCrate), new PropertyMetadata(string.Empty));

        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public string DisplayName => GameData.FriendlyNameForClass(ClassName);

        public bool KnownSpawner => GameData.HasSupplyCrateForClass(ClassName);

        public SupplyCrate Duplicate()
        {
            var properties = this.GetType().GetProperties();

            var result = new SupplyCrate();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                    prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }
    }
}
