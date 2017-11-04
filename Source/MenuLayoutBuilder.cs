using System.Collections.Generic;
using Xamarin.Forms;

// A MenuLayoutBuilder can build a menu that talks to a particular LayoutStack and pushes layouts onto it when the corresponding menu item is chosen
namespace VisiPlacement
{
    public class MenuLayoutBuilder
    {
        public MenuLayoutBuilder(LayoutStack layoutStack)
        {
            this.layoutStack = layoutStack;
        }
        public MenuLayoutBuilder AddLayout(string name, LayoutChoice_Set layout)
        {
            this.layoutNames.AddLast(name);
            Button button = this.MakeButton(name, layout);
            return this;
        }

        public LayoutChoice_Set Build()
        {
            GridLayout layout = GridLayout.New(BoundProperty_List.Uniform(this.layoutNames.Count), BoundProperty_List.Uniform(1), LayoutScore.Zero);
            foreach (string name in this.layoutNames)
            {
                LayoutChoice_Set subLayout = this.Get_ButtonLayout_By_Name(name);
                layout.AddLayout(subLayout);
            }
            return layout;
        }

        private Button MakeButton(string name, LayoutChoice_Set target)
        {
            Button button = new Button();
            this.buttonsByName[name] = button;
            button.Clicked += button_Click;
            ButtonLayout buttonLayout = new ButtonLayout(button, name);
            this.buttonLayouts_by_button[button] = buttonLayout;
            //ContainerLayout layout = new ContainerLayout(null, buttonLayout, new System.Windows.Thickness(), LayoutScore.Zero);
            this.buttonDestinations[buttonLayout] = target;
            return button;
        }

        private void button_Click(object sender, System.EventArgs e)
        {
            // find the sender's name
            Button button = sender as Button;
            
            if (button != null)
            {
                button = (Button)sender;
                // find where the sender wants to go
                LayoutChoice_Set sourceLayout = this.buttonLayouts_by_button[button];
                LayoutChoice_Set destination = this.buttonDestinations[sourceLayout];
                // update the view
                this.layoutStack.AddLayout(destination);
            }
        }

        private LayoutChoice_Set GetDestinationLayout(string name)
        {
            return this.buttonDestinations[this.Get_ButtonLayout_By_Name(name)];
        }

        private LayoutChoice_Set Get_ButtonLayout_By_Name(string name)
        {
            return this.buttonLayouts_by_button[this.buttonsByName[name]];
        }


        LinkedList<string> layoutNames = new LinkedList<string>();
        Dictionary<string, Button> buttonsByName = new Dictionary<string, Button>();
        Dictionary<Button, LayoutChoice_Set> buttonLayouts_by_button = new Dictionary<Button, LayoutChoice_Set>();
        Dictionary<LayoutChoice_Set, LayoutChoice_Set> buttonDestinations = new Dictionary<LayoutChoice_Set, LayoutChoice_Set>();
        LayoutStack layoutStack;

    }
}
