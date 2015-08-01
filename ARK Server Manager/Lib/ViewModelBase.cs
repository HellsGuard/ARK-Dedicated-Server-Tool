using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ARK_Server_Manager.Lib
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected T Get<T>(ISettingsBag settings, [CallerMemberName] string propertyName = "")
        {
            return (T)settings[propertyName];
        }

        protected T Get<T>(ref T field, [CallerMemberName] string propertyName = "")
        {
            return field;
        }                                                                 

        protected void Set<T>(ISettingsBag settings, T value, [CallerMemberName] string propertyName = "")
        {
            T existingValue = Get<T>(settings, propertyName);
            if(!Object.Equals(existingValue, value))
            {
                settings[propertyName] = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {            
            if (!Object.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }


        protected void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                Task.Factory.StartNew(() => PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
