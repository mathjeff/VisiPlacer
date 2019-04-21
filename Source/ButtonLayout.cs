using System.Collections.Generic;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class ButtonLayout : ContainerLayout
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

        public ButtonLayout(Button button, string content, double fontSize, bool includeBevel = true, bool allowCropping = false)
        {
            button.Text = content;
            this.Initialize(button, fontSize, includeBevel, allowCropping);
        }


        public static ButtonLayout WithoutBevel(Button button)
        {
            return new ButtonLayout(button, button.Text, -1, false);
        }

        private void Initialize(Button button, double fontSize = -1, bool includeBevel = true, bool allowCropping = false)
        {
            button.Margin = new Thickness();
            button.BorderRadius = 0;
            button.TextColor = Color.LightGray;

            ButtonText_Configurer buttonConfigurer = new ButtonText_Configurer(button);
            LayoutChoice_Set sublayout;
            if (fontSize > 0)
            {
                if (allowCropping)
                    sublayout = TextLayout.New_Croppable(new ButtonText_Configurer(button), fontSize);
                else
                    sublayout = new TextLayout(new ButtonText_Configurer(button), fontSize);

                this.sublayoutOptions = new List<LayoutChoice_Set>() { sublayout };
            }
            else
            {
                this.sublayoutOptions = new List<LayoutChoice_Set>();
                if (allowCropping)
                {
                    this.sublayoutOptions.Add(TextLayout.New_Croppable(buttonConfigurer, 30));
                    this.sublayoutOptions.Add(TextLayout.New_Croppable(buttonConfigurer, 16));
                    this.sublayoutOptions.Add(TextLayout.New_Croppable(buttonConfigurer, 12));
                }
                else
                {
                    this.sublayoutOptions.Add(new TextLayout(buttonConfigurer, 30));
                    this.sublayoutOptions.Add(new TextLayout(buttonConfigurer, 16));
                    this.sublayoutOptions.Add(new TextLayout(buttonConfigurer, 12));
                }
                LayoutUnion layoutUnion = new LayoutUnion(this.sublayoutOptions);
                sublayout = layoutUnion;
            }

            // add a view behind the button to change its normal background color without changing its color when selected
            ContentView buttonBackground = new ContentView();
            buttonBackground.BackgroundColor = Color.Black;
            ContainerLayout backgroundLayout = new ContainerLayout(buttonBackground, sublayout, new Thickness(), LayoutScore.Zero, false);

            if (includeBevel)
            {
                // add a small border, so that it's easy to see where the buttons end
                Thickness innerBevelThickness = new Thickness(1);
                ContentView insideBevel = new ContentView();
                insideBevel.Padding = innerBevelThickness;
                insideBevel.BackgroundColor = Color.DarkGray;// Color.FromRgb(0.4, 0.4, 0.4);
                ContainerLayout middleLayout = new ContainerLayout(insideBevel, backgroundLayout, innerBevelThickness, LayoutScore.Zero, false);

                // add a bevel to the border
                Thickness outerBevelThickness = new Thickness(1);
                ContentView outsideBevel = new ContentView();
                outsideBevel.Padding = outerBevelThickness;
                outsideBevel.BackgroundColor = Color.LightGray;// Color.FromRgb(0.63, 0.63, 0.63);
                ContainerLayout outsideLayout = new ContainerLayout(outsideBevel, middleLayout, outerBevelThickness, LayoutScore.Zero, false);

                // add some extra space around it
                Thickness spacingThickness = new Thickness(1);
                ContentView spacing = new ContentView();
                spacing.Padding = spacingThickness;
                spacing.BackgroundColor = Color.FromRgba(0, 0, 0, 0);
                ContainerLayout spacingLayout = new ContainerLayout(spacing, outsideLayout, spacingThickness, LayoutScore.Zero, false);

                this.SubLayout = spacingLayout;
            }
            else
            {
                button.TextColor = Color.Black;
                button.BackgroundColor = Color.LightGray;
                this.SubLayout = backgroundLayout;
            }
        }

        private List<LayoutChoice_Set> sublayoutOptions;

    }

    public class ButtonText_Configurer : TextItem_Configurer
    {
        public ButtonText_Configurer(Button button)
        {
            this.button = button;
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
        public string Text
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
        public View View
        {
            get
            {
                return this.button;
            }
        }
        public void Add_TextChanged_Handler(System.ComponentModel.PropertyChangedEventHandler handler)
        {
            this.button.PropertyChanged += handler;
        }
        public Button button;

    }
}
