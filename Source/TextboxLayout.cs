using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

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
            layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 30));
            layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 16));

            this.LayoutToManage = new LayoutUnion(layouts);

        }



        private void Setup_PropertyChange_Listener(string propertyName, FrameworkElement element, PropertyChangedCallback callback)
        {
            Binding b = new Binding();
            b.Path = new PropertyPath(propertyName);
            b.Source = element;
            var prop = DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                typeof(TextBlock),
                new PropertyMetadata(callback));

            element.SetBinding(prop, b);
        }


        private TextBox TextBox;
    }
}
