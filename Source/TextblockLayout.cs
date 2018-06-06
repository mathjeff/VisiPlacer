using System;
using System.Collections.Generic;
using Xamarin.Forms;

// A TextblockLayout is what callers should make if they want to display uneditable text
namespace VisiPlacement
{
    public class TextblockLayout : LayoutCache
    {
        public TextblockLayout(Label textBlock, double fontSize = -1)
        {
            this.Initialize(textBlock, fontSize);
        }
        public TextblockLayout(String text, double fontsize = -1)
        {
            Label textBlock = new Label();
            textBlock.TextColor = Color.Black;
            textBlock.Text = text;
            this.Initialize(textBlock, fontsize);
        }
        public TextblockLayout(string text, TextAlignment horizontalTextAlignment)
        {
            Label textBlock = new Label();
            textBlock.HorizontalTextAlignment = horizontalTextAlignment;
            textBlock.Text = text;
            this.Initialize(textBlock, -1);
        }
        private void Initialize(Label textBlock, double fontsize)
        {
            Effect effect = Effect.Resolve("VisiPlacement.TextItemEffect");
            textBlock.Effects.Add(effect);
            textBlock.Margin = new Thickness(0);
            textBlock.TextColor = Color.LightGray;
            this.textBlock = textBlock;

            this.layouts = new List<TextLayout>();
            if (fontsize > 0)
            {
                layouts.Add(this.makeLayout(fontsize));
            }
            else
            {
                layouts.Add(this.makeLayout(30));
                layouts.Add(this.makeLayout(16));
                layouts.Add(this.makeLayout(10));
            }
                
            this.LayoutToManage = new LayoutUnion(layouts);
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

        private TextLayout makeLayout(double fontsize)
        {
            TextBlock_Configurer configurer = new TextBlock_Configurer(this.textBlock);
            TextLayout layout = new TextLayout(configurer, fontsize);
            layout.ScoreIfEmpty = false;
            return layout;
        }

        private Label textBlock;
        private List<TextLayout> layouts;
    }

    // The TextBlock_Configurer is an implementation detail that facilitates sharing code between TextblockLayout and TextboxLayout
    // The TextBlock_Configurer probably isn't interesting to external callers
    class TextBlock_Configurer : TextItem_Configurer
    {
        public TextBlock_Configurer(Label Label)
        {
            this.Label = Label;
        }

        public double Width
        {
            get { return this.Label.Width; }
            set { this.Label.WidthRequest = value; }
        }
        public double Height
        {
            get { return this.Label.Height; }
            set { this.Label.HeightRequest = value; }
        }
        public double FontSize
        {
            get { return this.Label.FontSize; }
            set { this.Label.FontSize = value; }
        }
        public String Text
        {
            get { return this.Label.Text; }
            set { this.Label.Text = value; }
        }
        public View View
        {
            get
            {
                return this.Label;
            }
        }
        public void Add_TextChanged_Handler(System.ComponentModel.PropertyChangedEventHandler handler)
        {
            this.Label.PropertyChanged += handler;
        }

        public Label Label;
    }

}
