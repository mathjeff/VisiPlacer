using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Data;

namespace VisiPlacement
{
    public class TextboxLayout : LayoutCache
    {
        public TextboxLayout(TextBox textBox)
        {
            textBox.Margin = new Thickness();
            textBox.Padding = new Thickness();
            textBox.BorderThickness = new Thickness();
            //textBox.Background = new SolidColorBrush(Colors.Yellow);

            this.TextBox = textBox;

            List<LayoutChoice_Set> layouts = new List<LayoutChoice_Set>();
            layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 16));
            layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 30));

            this.LayoutToManage = new LayoutUnion(layouts);

            //this.TextBox.TextChanged += this.OnTextChange;
            //this.Setup_PropertyChange_Listener("Text", this.TextBox, this.OnTextChange);

            //DependencyPropertyDescriptor textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBox.TextProperty, typeof(TextBlock));
            //textDescriptor.AddValueChanged(textBox, new EventHandler(this.OnTextChange));

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
