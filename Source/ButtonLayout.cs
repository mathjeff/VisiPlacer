using Xamarin.Forms;

namespace VisiPlacement
{
    public class ButtonLayout : SingleItem_Layout
    {
        public ButtonLayout(Button button)
        // : base(new ButtonText_Configurer(button), 12)
        {
            this.Initialize(button);
        }

        public ButtonLayout(Button button, string content)
        // : base(new ButtonText_Configurer(button), 12)
        {
            button.Text = content;
            this.Initialize(button);
        }

        private void Initialize(Button button)
        {
            button.Margin = new Thickness();
            button.BorderRadius = 0;

            LayoutChoice_Set buttonLayout = new TextLayout(new ButtonText_Configurer(button), 12);

            // add a small border, so that it's easy to see where the buttons end
            Thickness innerBevelThickness = new Thickness(2);
            ContentView insideBevel = new ContentView();
            insideBevel.Padding = innerBevelThickness;
            insideBevel.BackgroundColor = Color.LightGray;
            SingleItem_Layout middleLayout = new SingleItem_Layout(insideBevel, buttonLayout, innerBevelThickness, LayoutScore.Zero, false);


            // add a bevel to the border
            Thickness outerBevelThickness = new Thickness(2);
            ContentView outsideBevel = new ContentView();
            outsideBevel.Padding = outerBevelThickness;
            outsideBevel.BackgroundColor = Color.Gray;
            SingleItem_Layout outsideLayout = new SingleItem_Layout(outsideBevel, middleLayout, outerBevelThickness, LayoutScore.Zero, false);
            this.SubLayout = outsideLayout;

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
                return this.button.Text;
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
