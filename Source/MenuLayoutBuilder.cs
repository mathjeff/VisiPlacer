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
            this.AddLayout(name, new ConstantValueProvider<StackEntry>(new StackEntry(layout)));
            return this;
        }
        public MenuLayoutBuilder AddLayout(string name, ValueProvider<StackEntry> layoutProvider)
        {
            this.layoutNames.AddLast(name);
            Button button = this.MakeButton(name, layoutProvider);
            return this;
        }

        public LayoutChoice_Set Build()
        {
            Vertical_GridLayout_Builder gridBuilder = new Vertical_GridLayout_Builder().Uniform();
            foreach (string name in this.layoutNames)
            {
                LayoutChoice_Set subLayout = this.Get_ButtonLayout_By_Name(name);
                gridBuilder.AddLayout(subLayout);
            }
            return gridBuilder.BuildAnyLayout();
        }

        private Button MakeButton(string name, ValueProvider<StackEntry> target)
        {
            Button button = new Button();
            this.buttonsByName[name] = button;
            button.Clicked += button_Click;
            ButtonLayout buttonLayout = new ButtonLayout(button, name);
            this.buttonLayouts_by_button[button] = buttonLayout;
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
                StackEntry destination = this.buttonDestinations[sourceLayout].Get();
                // update the view
                this.layoutStack.AddEntry(destination);
            }
        }

        private LayoutChoice_Set Get_ButtonLayout_By_Name(string name)
        {
            return this.buttonLayouts_by_button[this.buttonsByName[name]];
        }


        LinkedList<string> layoutNames = new LinkedList<string>();
        Dictionary<string, Button> buttonsByName = new Dictionary<string, Button>();
        Dictionary<Button, LayoutChoice_Set> buttonLayouts_by_button = new Dictionary<Button, LayoutChoice_Set>();
        Dictionary<LayoutChoice_Set, ValueProvider<StackEntry>> buttonDestinations = new Dictionary<LayoutChoice_Set, ValueProvider<StackEntry>>();
        LayoutStack layoutStack;
    }

    public interface ValueProvider<T>
    {
        T Get();
    }

    public class ConstantValueProvider<T> : ValueProvider<T>
    {
        public ConstantValueProvider(T value)
        {
            this.value = value;
        }
        public T Get()
        {
            return this.value;
        }
        public T value;
    }

}
