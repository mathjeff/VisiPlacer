using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace VisiPlacement
{
    public class TextBox_Configurer : TextItem_Configurer
    {
        public TextBox_Configurer(TextBox TextBox)
        {
            this.TextBox = TextBox;
            TextBox.TextWrapping = TextWrapping.Wrap;
            TextBox.FontFamily = new FontFamily("Segoe WP");
            TextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 153, 217, 234));
            //TextBox.BorderThickness = new Thickness(1);
        }
        public double Width
        {
            get { return this.TextBox.Width; }
            set { 
                this.TextBox.Width = value;
                if (this.TextBox.Style != null)
                    this.TextBox.Style = null;
            }
        }
        public double Height
        {
            get { return this.TextBox.Height; }
            set { this.TextBox.Height = value; }
        }
        public double FontSize
        {
            get { return this.TextBox.FontSize; }
            set { this.TextBox.FontSize = value; }
        }
        public FontFamily FontFamily
        {
            get { return this.TextBox.FontFamily; }
            set { this.TextBox.FontFamily = value; }
        }

        public FontStyle FontStyle
        {
            get { return this.TextBox.FontStyle; }
            set { this.TextBox.FontStyle = value; }
        }
        public FontWeight FontWeight
        {
            get { return this.TextBox.FontWeight; }
            set { this.TextBox.FontWeight = value; }
        }
        public FontStretch FontStretch
        {
            get { return this.TextBox.FontStretch; }
            set { this.TextBox.FontStretch = value; }
        }
        public String Text 
        {
            get { return this.TextBox.Text; }
            set { this.TextBox.Text = value; }
        }
        /*public TextItem_Configurer Clone()
        {
            TextBox_Configurer clone = new TextBox_Configurer(this.TextBox);
            return clone;
        }*/
        public FrameworkElement View
        {
            get
            {
                return this.TextBox;
            }
        }
        public void Add_TextChanged_Handler(PropertyChangedCallback handler)
        {
            // It seems silly that we have to do this to get notification of when the text changes
            this.Setup_PropertyChange_Listener("Text", this.TextBox, handler);
        }
        



        private void Setup_PropertyChange_Listener(string propertyName, FrameworkElement element, PropertyChangedCallback callback)
        {
            Binding b = new Binding(propertyName) { Source = element };
            var prop = System.Windows.DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                typeof(TextBlock),
                new System.Windows.PropertyMetadata(callback));

            element.SetBinding(prop, b);
        }

        private TextBox TextBox;
    }
}
