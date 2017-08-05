using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace VisiPlacement
{
    class TextBlock_Configurer : TextItem_Configurer
    {
        public TextBlock_Configurer(TextBlock TextBlock)
        {
            this.TextBlock = TextBlock;
            TextBlock.TextWrapping = TextWrapping.Wrap;
            //TextBlock.FontFamily = new FontFamily("Segoe WP");
            TextBlock.FontFamily = FontFamily.XamlAutoFontFamily;
            this.listener = new PropertyListener(TextBlock);
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
            this.Setup_PropertyChange_Listener("Text", this.TextBlock, handler, true);
        }

        private void Setup_PropertyChange_Listener(string propertyName, FrameworkElement element, PropertyChangedCallback callback, bool triggerOnProgrammaticUpdates)
        {
            Binding b = new Binding();
            b.Path = new PropertyPath(propertyName);
            b.Source = element;
            PropertyMetadata metadata;
            if (triggerOnProgrammaticUpdates)
                metadata = new PropertyMetadata(null);
            else
                metadata = new PropertyMetadata(callback);
            var prop = DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                typeof(TextBlock),
                metadata);


            if (triggerOnProgrammaticUpdates)
            {
                b.Converter = this.listener;
                this.listener.AddHandler(callback);
            }

            element.SetBinding(prop, b);
        }

        private TextBlock TextBlock;
        private PropertyListener listener;
    }

    class PropertyListener : IValueConverter
    {
        public PropertyListener(DependencyObject source)
        {
            this.source = source;
        }
        public void AddHandler(PropertyChangedCallback handler)
        {
            this.handlers.AddLast(handler);
        }
        public object Convert(object item, Type targetType, object parameter, String language)
        {
            this.triggerAll();
            return item;
        }
        public object ConvertBack(object item, Type targetType, object parameter, String language)
        {
            return item;
        }
        private void triggerAll()
        {
            foreach (PropertyChangedCallback handler in this.handlers)
            {
                handler.Invoke(this.source, null); // new DependencyPropertyChangedEventArgs());
            }
        }
        LinkedList<PropertyChangedCallback> handlers = new LinkedList<PropertyChangedCallback>();
        DependencyObject source;
    }
}
