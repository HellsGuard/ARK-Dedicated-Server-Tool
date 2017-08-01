using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        #region Overrides of Freezable
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
        #endregion
    }
}
