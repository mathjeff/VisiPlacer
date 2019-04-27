using System;
using System.Collections.Generic;
using Xamarin.Forms;

// A TextblockLayout is what callers should make if they want to display uneditable text
namespace VisiPlacement
{
    public class TextblockLayout : LayoutCache
    {
        public TextblockLayout(string text, bool allowCropping)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, -1, false);
        }
        public TextblockLayout(string text, bool allowCropping, double fontSize)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, fontSize, false);
        }
        public TextblockLayout(Label textBlock, bool allowCropping, double fontSize = -1)
        {
            this.Initialize(textBlock, fontSize, allowCropping);
        }
        public TextblockLayout(Label textBlock, double fontSize = -1)
        {
            this.Initialize(textBlock, fontSize, false);
        }
        public TextblockLayout(String text, double fontSize = -1)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, fontSize, false);
        }
        public TextblockLayout(string text, TextAlignment horizontalTextAlignment)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, -1, false);
            this.AlignHorizontally(horizontalTextAlignment);
        }
        public TextblockLayout AlignHorizontally(TextAlignment horizontalTextAlignment)
        {
            this.textBlock.HorizontalTextAlignment = horizontalTextAlignment;
            return this;
        }
        private Label makeTextBlock(string text)
        {
            Label textBlock = new Label();
            textBlock.Text = text;
            return textBlock;
        }
        private void Initialize(Label textBlock, double fontsize, bool allowCropping)
        {
            Effect effect = Effect.Resolve("VisiPlacement.TextItemEffect");
            textBlock.Effects.Add(effect);
            textBlock.Margin = new Thickness(0);
            textBlock.TextColor = Color.LightGray;
            this.textBlock = textBlock;

            this.layouts = new List<LayoutChoice_Set>();
            if (fontsize > 0)
            {
                layouts.Add(this.makeLayout(fontsize, allowCropping));
            }
            else
            {
                layouts.Add(this.makeLayout(30, allowCropping));
                layouts.Add(this.makeLayout(16, allowCropping));
                layouts.Add(this.makeLayout(10, allowCropping));
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

        public bool LoggingEnabled
        {
            set
            {
                foreach (TextLayout layout in this.layouts)
                {
                    layout.LoggingEnabled = value;
                }
            }
        }


        private LayoutChoice_Set makeLayout(double fontsize, bool allowCropping)
        {
            TextBlock_Configurer configurer = new TextBlock_Configurer(this.textBlock);
            LayoutChoice_Set layout;
            if (allowCropping)
                layout = TextLayout.New_Croppable(configurer, fontsize);
            else
                layout = new TextLayout(configurer, fontsize);
            return layout;
        }

        private Label textBlock;
        private List<LayoutChoice_Set> layouts;
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
