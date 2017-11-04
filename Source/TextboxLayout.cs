using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class TextboxLayout : LayoutCache
    {
        public TextboxLayout(Editor textBox)
        {
            Effect effect = Effect.Resolve("VisiPlacement.TextItemEffect");
            textBox.Effects.Add(effect);

            textBox.Margin = new Thickness();

            this.TextBox = textBox;
            textBox.BackgroundColor = Color.White;
            this.TextBox.Margin = new Thickness();

            List<LayoutChoice_Set> layouts = new List<LayoutChoice_Set>();
            layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 30));
            layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 16));

            this.LayoutToManage = new LayoutUnion(layouts);

        }

        private void Setup_PropertyChange_Listener(string propertyName, View element, PropertyChangedEventHandler callback)
        {
            this.TextBox.PropertyChanged += callback;
        }


        private Editor TextBox;
    }

    public class TextBox_Configurer : TextItem_Configurer
    {
        public TextBox_Configurer(Editor TextBox)
        {
            this.TextBox = TextBox;
        }
        public double Width
        {
            get { return this.TextBox.WidthRequest; }
            set
            {
                this.TextBox.WidthRequest = value;
            }
        }
        public double Height
        {
            get { return this.TextBox.HeightRequest; }
            set
            {
                this.TextBox.HeightRequest = value;
            }
        }
        public double FontSize
        {
            get { return this.TextBox.FontSize; }
            set { this.TextBox.FontSize = value; }
        }
        public string Text
        {
            get { return this.TextBox.Text; }
            set { this.TextBox.Text = value; }
        }
        public View View
        {
            get
            {
                return this.TextBox;
            }
        }
        public void Add_TextChanged_Handler(PropertyChangedEventHandler handler)
        {
            this.TextBox.PropertyChanged += handler;
        }

        private Editor TextBox;
    }
}
