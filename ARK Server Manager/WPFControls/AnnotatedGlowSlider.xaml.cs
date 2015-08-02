using System;
using System.Collections.Generic;
using System.Text;
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
	/// Interaction logic for AnnotatedGlowSlider.xaml
	/// </summary>
	public partial class AnnotatedGlowSlider : UserControl
	{
		// Register dependency properties

        // Front note
        public static readonly DependencyProperty FrontNoteProperty = DependencyProperty.Register("FrontNote", typeof(string), typeof(AnnotatedGlowSlider), new PropertyMetadata("Front Note"));

        // End note
        public static readonly DependencyProperty EndNoteProperty = DependencyProperty.Register("EndNote", typeof(string), typeof(AnnotatedGlowSlider), new PropertyMetadata("End Note"));

        // Actual slider value, shared between the slider itself and the slider text input
        public static readonly DependencyProperty SliderValueProperty = DependencyProperty.Register("SliderValue", typeof(float), typeof(AnnotatedGlowSlider), new FrameworkPropertyMetadata(default(float), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // Slider min and max values
        public static readonly DependencyProperty SliderMinProperty = DependencyProperty.Register("SliderMin", typeof(float), typeof(AnnotatedGlowSlider));
        public static readonly DependencyProperty SliderMaxProperty = DependencyProperty.Register("SliderMax", typeof(float), typeof(AnnotatedGlowSlider));
		
		// Properties		
        public string FrontNote
        {
            get { return (string)GetValue(FrontNoteProperty); }
            set { SetValue(FrontNoteProperty, value); }
        }

        public string EndNote
        {
            get { return (string)GetValue(EndNoteProperty); }
            set { SetValue(EndNoteProperty, value); }
        }

        public float SliderValue
        {
            get { return (float)GetValue(SliderValueProperty); }
            set { SetValue(SliderValueProperty, value); }
        }

        public float SliderMin
        {
            get { return (float)GetValue(SliderMinProperty); }
            set { SetValue(SliderMinProperty, value); }
        }

        public float SliderMax
        {
            get { return (float)GetValue(SliderMaxProperty); }
            set { SetValue(SliderMaxProperty, value); }
        }

		public AnnotatedGlowSlider()
		{
			this.InitializeComponent();
		}
		
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if(Slider.IsFocused)
            //{
                unchecked
                {
                    SliderValue = (float)e.NewValue;
                }
            //}
        }
	}
}