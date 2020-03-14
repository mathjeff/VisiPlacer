using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

// A TextboxLayout is what callers should make if they want to display editable text
namespace VisiPlacement
{
    public class TextboxLayout : ContainerLayout
    {
        public TextboxLayout()
        {
            this.initialize(new Editor(), null);
        }
        public TextboxLayout(Editor textBox, double fontSize)
        {
            this.initialize(textBox, new List<double>() { fontSize });
        }
        public TextboxLayout(Editor textBox)
        {
            this.initialize(textBox, null);
        }

        private void initialize(Editor textBox, IEnumerable<double> fontSizes)
        {
            if (fontSizes == null)
                fontSizes = new List<double>() { 30, 16 };
            Effect effect = Effect.Resolve("VisiPlacement.TextItemEffect");
            textBox.Effects.Add(effect);

            textBox.Margin = new Thickness();

            this.TextBox = textBox;
            textBox.BackgroundColor = Color.LightGray;
            this.TextBox.Margin = new Thickness();

            TextBox_Configurer configurer = new TextBox_Configurer(textBox);
            foreach (double fontSize in fontSizes)
            {
                this.layouts.Add(new TextLayout(configurer, fontSize));
                this.layouts.Add(new ScoreShifted_Layout(new TextLayout(configurer, fontSize, true), LayoutScore.Get_UnCentered_LayoutScore(1)));
            }

            this.SubLayout = LayoutUnion.New(layouts);
       }

        public bool ScoreIfEmpty
        {
            set
            {
                foreach (TextLayout layout in this.layouts)
                {
                    layout.ScoreIfEmpty = value;
                }
            }
        }

        private void Setup_PropertyChange_Listener(string propertyName, View element, PropertyChangedEventHandler callback)
        {
            this.TextBox.PropertyChanged += callback;
        }

        private List<LayoutChoice_Set> layouts = new List<LayoutChoice_Set>();
        private Editor TextBox;
    }

    // The TextBox_Configurer is an implementation detail that facilitates sharing code between TextblockLayout and TextboxLayout
    // The TextBox_Configurer probably isn't interesting to external callers
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
            get
            {
                // add another character because the operating system leaves extra space for the user to type another character
                return this.TextBox.Text + "M";
            }
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
