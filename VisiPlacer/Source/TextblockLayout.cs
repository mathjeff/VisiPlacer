using System;
using System.Collections.Generic;
using Xamarin.Forms;

// A TextblockLayout is what callers should make if they want to display uneditable text
namespace VisiPlacement
{
    public class TextblockLayout : ContainerLayout
    {
        public TextblockLayout(string text = "")
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, -1, false, false);
        }
        public TextblockLayout(string text, Color textColor)
        {
            Label textBlock = this.makeTextBlock(text);
            textBlock.TextColor = textColor;
            this.Initialize(textBlock, -1, false, false);
        }
        public TextblockLayout(string text, bool allowCropping, bool allowSplittingWords)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, -1, allowCropping, allowSplittingWords);
        }
        public TextblockLayout(string text, bool allowCropping, double fontSize)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, fontSize, allowCropping, false);
        }
        /*public TextblockLayout(Label textBlock, bool allowCropping, double fontSize = -1)
        {
            this.Initialize(textBlock, fontSize, allowCropping, false);
        }*/
        /*public TextblockLayout(Label textBlock, double fontSize = -1)
        {
            this.Initialize(textBlock, fontSize, false, false);
        }*/
        public TextblockLayout(String text, double fontSize = -1)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, fontSize, false, false);
        }
        public TextblockLayout(String text, double fontSize, bool allowCropping, bool allowSplittingWords)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, fontSize, allowCropping, allowSplittingWords);
        }
        public TextblockLayout(Label textBlock, double fontSize, bool allowCropping, bool allowSplittingWords)
        {
            this.Initialize(textBlock, fontSize, allowCropping, allowSplittingWords);
        }
        public TextblockLayout(string text, TextAlignment horizontalTextAlignment)
        {
            Label textBlock = this.makeTextBlock(text);
            this.Initialize(textBlock, -1, false, false);
            this.AlignHorizontally(horizontalTextAlignment);
        }
        public TextblockLayout AlignHorizontally(TextAlignment horizontalTextAlignment)
        {
            this.textBlock.HorizontalTextAlignment = horizontalTextAlignment;
            return this;
        }
        public TextblockLayout AlignVertically(TextAlignment verticalTextAlignment)
        {
            this.textBlock.VerticalTextAlignment = verticalTextAlignment;
            return this;
        }
        public void setText(string text)
        {
            this.ModelledText = text;
            foreach (TextLayout layout in this.layouts)
            {
                layout.On_TextChanged();
            }
        }

        public void setTextColor(Color color)
        {
            this.configurer.TextColor = color;
        }
        public void setBackgroundColor(Color color)
        {
            this.textBlock.BackgroundColor = color;
        }

        public string getText()
        {
            return this.ModelledText;
        }
        private Label makeTextBlock(string text)
        {
            this.ModelledText = text;
            return new Label();
        }
        private void Initialize(Label textBlock, double fontsize, bool allowCropping, bool allowSplittingWords)
        {
            this.configurer = new TextBlock_Configurer(textBlock, this);
            Effect effect = Effect.Resolve("VisiPlacement.TextItemEffect");
            textBlock.Effects.Add(effect);
            textBlock.Margin = new Thickness(0);
            this.textBlock = textBlock;

            this.layouts = new List<LayoutChoice_Set>();
            if (fontsize > 0)
            {
                if (allowCropping || allowSplittingWords)
                    layouts.Add(this.makeLayout(fontsize, allowCropping, allowSplittingWords));
                layouts.Add(this.makeLayout(fontsize, false, false));
            }
            else
            {
                layouts.Add(this.makeLayout(30, false, false));
                layouts.Add(this.makeLayout(16, false, false));
                layouts.Add(this.makeLayout(10, false, false));
                if (allowCropping || allowSplittingWords)
                    layouts.Add(this.makeLayout(10, allowCropping, allowSplittingWords));
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


        private LayoutChoice_Set makeLayout(double fontSize, bool allowCropping, bool allowSplittingWords)
        {
            LayoutChoice_Set layout;
            if (allowCropping)
                layout = TextLayout.New_Croppable(configurer, fontSize);
            else
                layout = new TextLayout(configurer, fontSize, allowSplittingWords);
            return layout;
        }

        public string ModelledText;
        private TextBlock_Configurer configurer;
        private Label textBlock;
        private List<LayoutChoice_Set> layouts;
    }

    // The TextBlock_Configurer is an implementation detail that facilitates sharing code between TextblockLayout and TextboxLayout
    // The TextBlock_Configurer probably isn't interesting to external callers
    class TextBlock_Configurer : TextItem_Configurer
    {
        public TextBlock_Configurer(Label label, TextblockLayout layout)
        {
            this.Label = label;
            this.Layout = layout;
            this.assignedTextColor = label.TextColor;
            this.assignedBackgroundColor = label.BackgroundColor;
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
        public string ModelledText
        {
            get
            {
                return this.Layout.ModelledText;
            }
            set
            {
                this.Layout.ModelledText = value;
            }
        }
        public string DisplayText
        {
            get { return this.Label.Text; }
            set { this.Label.Text = value; }
        }
        public string FontName
        {
            get { return this.Label.FontFamily; }
            set { this.Label.FontFamily = value; }
        }
        public View View
        {
            get
            {
                return this.Label;
            }
        }

        public Color TextColor
        {
            set
            {
                this.Label.TextColor = value;
                this.assignedTextColor = value;
            }
        }

        public void ApplyDefaults(ViewDefaults layoutDefaults)
        {
            // apply defaults if colors weren't already set
            if (this.assignedTextColor.A <= 0)
                this.Label.TextColor = layoutDefaults.TextBlock_Defaults.TextColor;
            if (this.assignedBackgroundColor.A <= 0)
                this.Label.BackgroundColor = layoutDefaults.TextBlock_Defaults.BackgroundColor;
        }

        public void Add_TextChanged_Handler(System.ComponentModel.PropertyChangedEventHandler handler)
        {
            this.Label.PropertyChanged += handler;
        }

        public Label Label;
        public TextblockLayout Layout;
        public Color assignedTextColor;
        public Color assignedBackgroundColor;
    }

}
