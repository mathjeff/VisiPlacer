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
            this.TextBox.Margin = new Thickness();

            this.textBox_configurer = new TextBox_Configurer(textBox);
            double minFontSize = -1;
            foreach (double fontSize in fontSizes)
            {
                if (minFontSize < 0 || fontSize < minFontSize)
                    minFontSize = fontSize;
                this.layouts.Add(new TextLayout(textBox_configurer, fontSize));
            }
            if (minFontSize > 0)
                this.layouts.Add(new TextLayout(textBox_configurer, minFontSize, true));

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

        public void SetBackgroundColor(Color backgroundColor)
        {
            this.textBox_configurer.SetBackgroundColor(backgroundColor);
        }

        private void Setup_PropertyChange_Listener(string propertyName, View element, PropertyChangedEventHandler callback)
        {
            this.TextBox.PropertyChanged += callback;
        }

        private List<LayoutChoice_Set> layouts = new List<LayoutChoice_Set>();
        private Editor TextBox;
        private TextBox_Configurer textBox_configurer;
    }

    // The TextBox_Configurer is an implementation detail that facilitates sharing code between TextblockLayout and TextboxLayout
    // The TextBox_Configurer probably isn't interesting to external callers
    public class TextBox_Configurer : TextItem_Configurer
    {
        public TextBox_Configurer(Editor TextBox)
        {
            this.TextBox = TextBox;
            this.configuredBackgroundColor = this.TextBox.BackgroundColor;
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
        // TextboxLayout doesn't support having different ModelledText from DisplayText
        public string ModelledText
        {
            get
            {
                string text = this.TextBox.Text;
                if (text == null || text == "")
                {
                    return this.TextBox.Placeholder;
                }
                return text;
            }
        }
        // TextboxLayout doesn't support having different ModelledText from DisplayText
        public string DisplayText
        {
            get;
            set;
        }
        public string FontName
        {
            get { return this.TextBox.FontFamily; }
            set { this.TextBox.FontFamily = value; }
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

        public void ApplyDefaults(ViewDefaults layoutDefaults)
        {
            this.TextBox.TextColor = layoutDefaults.TextBox_Defaults.TextColor;
            this.defaultBackground = layoutDefaults.TextBox_Defaults.BackgroundColor;
            this.updateBackgroundWithDefaults();
        }

        public void SetBackgroundColor(Color backgroundColor)
        {
            this.configuredBackgroundColor = backgroundColor;
            this.updateBackgroundWithDefaults();
        }

        private void updateBackgroundWithDefaults()
        {
            if (this.configuredBackgroundColor.A > 0)
                this.TextBox.BackgroundColor = this.configuredBackgroundColor;
            else
                this.TextBox.BackgroundColor = this.defaultBackground;
        }

        private Editor TextBox;
        private Color configuredBackgroundColor;
        private Color defaultBackground;
    }
}
