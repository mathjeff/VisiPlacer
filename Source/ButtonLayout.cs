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

        public ButtonLayout(Button button, string content, double fontSize, double bevelWidth = 2)
        {
            button.Text = content;
            this.Initialize(button, fontSize, false);
        }


        public static ButtonLayout WithoutBevel(Button button)
        {
            return new ButtonLayout(button, button.Text, 12, 0);
        }

        private void Initialize(Button button, double fontSize = 12, bool includeBevel = true)
        {
            button.Margin = new Thickness();
            button.BorderRadius = 0;
            button.TextColor = Color.LightGray;

            LayoutChoice_Set buttonLayout = new TextLayout(new ButtonText_Configurer(button), fontSize);

            // add a view behind the button to change its normal background color without changing its color when selected
            ContentView buttonBackground = new ContentView();
            buttonBackground.BackgroundColor = Color.Black;
            ContainerLayout backgroundLayout = new ContainerLayout(buttonBackground, buttonLayout, new Thickness(), LayoutScore.Zero, false);

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
            } else
            {
                this.SubLayout = backgroundLayout;
            }
        }

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
                return text.ToUpper();
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
