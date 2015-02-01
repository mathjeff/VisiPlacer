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
    class TextBlock_Configurer : TextItem_Configurer
    {
        /*public TextBlock_Configurer(TextBlock TextBlock)
        {
            this.TextBlock = TextBlock;
            TextBlock.TextWrapping = TextWrapping.Wrap;
        }
        */
        public TextBlock_Configurer(TextBlock TextBlock)
        {
            this.TextBlock = TextBlock;
            TextBlock.TextWrapping = TextWrapping.Wrap;
            TextBlock.FontFamily = new FontFamily("Segoe WP");
        }

        public double Width
        {
            get { return this.TextBlock.Width; }
            set { this.TextBlock.Width = value; }
        }
        public double Height
        {
            get { return this.TextBlock.Height; }
            set { this.TextBlock.Height = value; }
        }
        public double FontSize
        {
            get { return this.TextBlock.FontSize; }
            set { this.TextBlock.FontSize = value; }
        }
        public FontFamily FontFamily
        {
            get { return this.TextBlock.FontFamily; }
            set { this.TextBlock.FontFamily = value; }
        }

        public FontStyle FontStyle
        {
            get { return this.TextBlock.FontStyle; }
            set { this.TextBlock.FontStyle = value; }
        }
        public FontWeight FontWeight
        {
            get { return this.TextBlock.FontWeight; }
            set { this.TextBlock.FontWeight = value; }
        }
        public FontStretch FontStretch
        {
            get { return this.TextBlock.FontStretch; }
            set { this.TextBlock.FontStretch = value; }
        }
        public String Text
        {
            get { return this.TextBlock.Text; }
            set { this.TextBlock.Text = value; }
        }
        public FrameworkElement View
        {
            get
            {
                return this.TextBlock;
            }
        }
        public void Add_TextChanged_Handler(PropertyChangedCallback handler)
        {
            // It seems silly that we have to do this to get notification of when the text changes
            this.Setup_PropertyChange_Listener("Text", this.TextBlock, handler);
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

        private TextBlock TextBlock;
    }
}
