using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace VisiPlacement
{
    public class ButtonLayout : LayoutUnion
    {
        public ButtonLayout(Button button)
        {
            this.Initialize(button);
        }

        public ButtonLayout(Button button, string content)
        {
            button.Text = content;
            this.Initialize(button);
        }

        public ButtonLayout(Button button, string content, double fontSize, bool includeBevel = true, bool allowCropping = false, bool scoreIfEmpty = false)
        {
            button.Text = content;
            this.Initialize(button, fontSize, includeBevel, allowCropping, scoreIfEmpty);
        }


        public static ButtonLayout WithoutBevel(Button button)
        {
            return new ButtonLayout(button, button.Text, -1, false, false, true);
        }

        public static LayoutChoice_Set HideIfEmpty(ButtonLayout buttonLayout)
        {
            return new LayoutUnion(buttonLayout, new ContainerLayout());
        }
        
        private void Initialize(Button button, double fontSize = -1, bool includeBevel = true, bool allowCropping = false, bool scoreIfEmpty = false)
        {
            bool isButtonColorSet = button.BackgroundColor.A > 0;
            LayoutChoice_Set sublayout;
            this.buttonBackground = new ContentView();
            ButtonConfigurer buttonConfigurer = new ButtonConfigurer(button, this.buttonBackground);
            if (fontSize > 0)
            {
                if (allowCropping)
                    sublayout = TextLayout.New_Croppable(buttonConfigurer, fontSize, scoreIfEmpty);
                else
                    sublayout = new TextLayout(buttonConfigurer, fontSize, scoreIfEmpty);
            }
            else
            {
                List<LayoutChoice_Set> sublayoutOptions = new List<LayoutChoice_Set>(3);
                if (allowCropping)
                {
                    sublayoutOptions.Add(TextLayout.New_Croppable(buttonConfigurer, 30, scoreIfEmpty));
                    sublayoutOptions.Add(TextLayout.New_Croppable(buttonConfigurer, 16, scoreIfEmpty));
                    sublayoutOptions.Add(TextLayout.New_Croppable(buttonConfigurer, 12, scoreIfEmpty));
                }
                else
                {
                    sublayoutOptions.Add(new TextLayout(buttonConfigurer, 30, false, scoreIfEmpty));
                    sublayoutOptions.Add(new TextLayout(buttonConfigurer, 16, false, scoreIfEmpty));
                    sublayoutOptions.Add(new TextLayout(buttonConfigurer, 12, false, scoreIfEmpty));
                }
                LayoutUnion layoutUnion = new LayoutUnion(sublayoutOptions);
                sublayout = layoutUnion;
            }

            // add a view behind the button to change its normal background color without changing its color when selected
            ContainerLayout backgroundLayout = new ContainerLayout(buttonBackground, sublayout, false);

            if (includeBevel)
            {
                buttonBackground.BackgroundColor = Color.Black;
                // add a small border, so that it's easy to see where the buttons end
                Thickness innerBevelThickness = new Thickness(1);
                ContentView insideBevel = new ContentView();
                insideBevel.Padding = innerBevelThickness;
                //insideBevel.BackgroundColor = Color.DarkGray;// Color.FromRgb(0.4, 0.4, 0.4);
                buttonConfigurer.InnerBevel = insideBevel;
                ContainerLayout middleLayout = new MustBorderLayout(insideBevel, backgroundLayout, innerBevelThickness, false);

                // add a bevel to the border
                Thickness outerBevelThickness = new Thickness(1);
                ContentView outsideBevel = new ContentView();
                outsideBevel.Padding = outerBevelThickness;
                //outsideBevel.BackgroundColor = Color.LightGray;// Color.FromRgb(0.63, 0.63, 0.63);
                buttonConfigurer.OuterBevel = outsideBevel;
                ContainerLayout outsideLayout = new MustBorderLayout(outsideBevel, middleLayout, outerBevelThickness, false);

                // add some extra space around it
                Thickness spacingThickness = new Thickness(1);
                ContentView spacing = new ContentView();
                spacing.Padding = spacingThickness;
                spacing.BackgroundColor = Color.FromRgba(0, 0, 0, 0);
                ContainerLayout spacingLayout = new MustBorderLayout(spacing, outsideLayout, spacingThickness, false);

                this.Set_LayoutChoices(new List<LayoutChoice_Set>() { spacingLayout, new ScoreShifted_Layout(null, LayoutScore.Get_CutOff_LayoutScore(1)) });

                button.TextColor = Color.LightGray;
            }
            else
            {
                if (!isButtonColorSet)
                {
                    buttonBackground.BackgroundColor = Color.White;
                    button.TextColor = Color.Black;
                }
                this.Set_LayoutChoices(new List<LayoutChoice_Set>() { backgroundLayout });
            }
            Effect effect = Effect.Resolve("VisiPlacement.ButtonEffect");
            button.Effects.Add(effect);

            this.buttonConfigurer = buttonConfigurer;
        }

        public void setText(string text)
        {
            this.buttonConfigurer.ModelledText = text;
        }
        public void setTextColor(Color color)
        {
            this.buttonConfigurer.TextColor = color;
        }
        public void resetTextColor()
        {
            this.buttonConfigurer.TextColor = null;
        }

        private ButtonConfigurer buttonConfigurer;
        private ContentView buttonBackground;
    }

    public class ButtonConfigurer : TextItem_Configurer
    {
        public ButtonConfigurer(Button button, ContentView buttonBackground)
        {
            this.button = button;
            this.buttonBackground = buttonBackground;
        }

        public double Width
        {
            get
            {
                return this.button.WidthRequest;
            }
            set
            {
                this.button.WidthRequest = value;
            }
        }
        public double Height
        {
            get
            {
                return this.button.HeightRequest;
            }
            set
            {
                this.button.HeightRequest = value;
            }
        }
        public double FontSize
        {
            get
            {
                return this.button.FontSize;
            }
            set
            {
                this.button.FontSize = value;
            }
        }
        // ButtonLayout doesn't support having a separate ModelledText from DisplayText
        // TODO: make ButtonLayout support this
        public string ModelledText
        {
            get
            {
                string text = this.button.Text;
                if (text == null)
                    return null;
                return text;
            }
            set
            {
                this.button.Text = value;
            }
        }

        public string DisplayText { get; set; }
        public string FontName
        {
            get { return this.button.FontFamily; }
            set { this.button.FontFamily = value; }
        }
        public View View
        {
            get
            {
                return this.button;
            }
        }
        public bool IncludeBevel
        {
            get
            {
                return this.InnerBevel != null || this.OuterBevel != null;
            }
        }
        public ContentView InnerBevel { get; set; }
        public ContentView OuterBevel { get; set; }

        public void ApplyDefaults(ViewDefaults layoutDefaults)
        {
            if (this.TextColor != null)
            {
                this.button.TextColor = this.TextColor.Value;
            }
            else
            {
                if (this.IncludeBevel)
                    this.button.TextColor = layoutDefaults.ButtonWithBevel_Defaults.TextColor;
                else
                    this.button.TextColor = layoutDefaults.ButtonWithoutBevel_Defaults.TextColor;
            }
            if (this.IncludeBevel)
                this.buttonBackground.BackgroundColor = layoutDefaults.ButtonWithBevel_Defaults.BackgroundColor;
            else
                this.buttonBackground.BackgroundColor = layoutDefaults.ButtonWithoutBevel_Defaults.BackgroundColor;
            if (this.InnerBevel != null)
                this.InnerBevel.BackgroundColor = layoutDefaults.ButtonWithBevel_Defaults.InnerBevelColor;
            if (this.OuterBevel != null)
                this.OuterBevel.BackgroundColor = layoutDefaults.ButtonWithBevel_Defaults.OuterBevelColor;
        }

        public void Add_TextChanged_Handler(System.ComponentModel.PropertyChangedEventHandler handler)
        {
            this.button.PropertyChanged += handler;
        }
        public Button button;
        public Color? TextColor;
        ContentView buttonBackground;

    }
}
