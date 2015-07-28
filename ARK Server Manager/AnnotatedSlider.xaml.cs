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
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(AnnotatedSlider));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(float), typeof(AnnotatedSlider), new FrameworkPropertyMetadata(default(float), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(AnnotatedSlider));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty LargeChangeProperty = DependencyProperty.Register(nameof(LargeChange), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(nameof(SmallChange), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register(nameof(TickFrequency), typeof(float), typeof(AnnotatedSlider));
        public static readonly DependencyProperty LabelRelativeWidthProperty = DependencyProperty.Register(nameof(LabelRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("4*"));
        public static readonly DependencyProperty SliderRelativeWidthProperty = DependencyProperty.Register(nameof(SliderRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("8*"));
        public static readonly DependencyProperty ValueRelativeWidthProperty = DependencyProperty.Register(nameof(ValueRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("3*"));
        public static readonly DependencyProperty SuffixRelativeWidthProperty = DependencyProperty.Register(nameof(SuffixRelativeWidth), typeof(string), typeof(AnnotatedSlider), new PropertyMetadata("1*"));

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

        public string LabelRelativeWidth
        {
            get { return (string)GetValue(LabelRelativeWidthProperty); }
            set { SetValue(LabelRelativeWidthProperty, value); }
        }

        public string SliderRelativeWidth
        {
            get { return (string)GetValue(SliderRelativeWidthProperty); }
            set { SetValue(SliderRelativeWidthProperty, value); }
        }

        public string ValueRelativeWidth
        {
            get { return (string)GetValue(ValueRelativeWidthProperty); }
            set { SetValue(ValueRelativeWidthProperty, value); }
        }

        public string SuffixRelativeWidth
        {
            get { return (string)GetValue(SuffixRelativeWidthProperty); }
            set { SetValue(SuffixRelativeWidthProperty, value); }
        }

        public AnnotatedSlider()
        {
            InitializeComponent();
            (this.Content as FrameworkElement).DataContext = this;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(Slider.IsFocused)
            {
                unchecked
                {
                    Value = (float)e.NewValue;
                }
            }
        }
    }
}
