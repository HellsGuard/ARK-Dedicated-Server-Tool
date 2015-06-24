using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ARK_Server_Manager
{
    /// <summary>
    /// Interaction logic for AnnotatedSlider.xaml
    /// </summary>
    public partial class AnnotatedSlider : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(AnnotatedSlider));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register("Suffix", typeof(string), typeof(AnnotatedSlider));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty LargeChangeProperty = DependencyProperty.Register("LargeChange", typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register("SmallChange", typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register("TickFrequency", typeof(float), typeof(AnnotatedSlider));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public float Value
        {
            get { return (float)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string Suffix
        {
            get { return (string)GetValue(SuffixProperty); }
            set { SetValue(SuffixProperty, value); }
        }

        public float Minimum
        {
            get { return (float)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public float Maximum
        {
            get { return (float)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public float LargeChange
        {
            get { return (float)GetValue(LargeChangeProperty); }
            set { SetValue(LargeChangeProperty, value); }
        }

        public float SmallChange
        {
            get { return (float)GetValue(SmallChangeProperty); }
            set { SetValue(SmallChangeProperty, value); }
        }

        public float TickFrequency
        {
            get { return (float)GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        public AnnotatedSlider()
        {
            InitializeComponent();
        }
    }
}
