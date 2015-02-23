using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using VisiPlacement;

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
            // record the 
            this.layoutNames.AddLast(name);
            ContentControl button = this.MakeButton(name, layout);
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
            //this.layoutStack.AddLayout(layout);
            return layout;
        }

        private Button MakeButton(string name, LayoutChoice_Set target)
        {
            Button button = new Button();
            button.Content = name;
            this.buttonsByName[name] = button;
            button.Click += this.button_Click;
            ButtonLayout buttonLayout = new ButtonLayout(button, new TextblockLayout(name));
            this.buttonLayouts_by_button[button] = buttonLayout;
            //SingleItem_Layout layout = new SingleItem_Layout(null, buttonLayout, new System.Windows.Thickness(), LayoutScore.Zero);
            this.buttonDestinations[buttonLayout] = target;
            return button;
        }

        void button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // find the sender's name
            ContentControl button = sender as ContentControl;
            // find where the sender wants to go
            LayoutChoice_Set sourceLayout = this.buttonLayouts_by_button[button];
            LayoutChoice_Set destination = this.buttonDestinations[sourceLayout];
            // update the view
            this.layoutStack.AddLayout(destination);
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
        Dictionary<string, ContentControl> buttonsByName = new Dictionary<string, ContentControl>();
        Dictionary<ContentControl, LayoutChoice_Set> buttonLayouts_by_button = new Dictionary<ContentControl, LayoutChoice_Set>();
        Dictionary<LayoutChoice_Set, LayoutChoice_Set> buttonDestinations = new Dictionary<LayoutChoice_Set, LayoutChoice_Set>();
        LayoutStack layoutStack;

    }
}
