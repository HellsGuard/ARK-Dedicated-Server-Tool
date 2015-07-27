using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib.Model
{
    public class ClassMultiplier : AggregateIniValue
    {
        public static readonly DependencyProperty ClassNameProperty =
            DependencyProperty.Register("ClassName", typeof(string), typeof(ClassMultiplier), new PropertyMetadata(""));
        public static readonly DependencyProperty MultiplierProperty =
            DependencyProperty.Register("Multiplier", typeof(float), typeof(ClassMultiplier), new PropertyMetadata(0F));


        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public float Multiplier
        {
            get { return (float)GetValue(MultiplierProperty); }
            set { SetValue(MultiplierProperty, value); }
        }
        
        public static ClassMultiplier FromINIValue(string iniValue)
        {
            var newSpawn = new ClassMultiplier();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }
    }
}
