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
            return this.AddLayout(new StackEntry(layout, name, null));
        }
        public MenuLayoutBuilder AddLayout(StackEntry entry)
        {
            return this.AddLayout(entry.Name, new ConstantValueProvider<StackEntry>(entry));
        }
        public MenuLayoutBuilder AddLayout(string name, StackEntry entry)
        {
            return this.AddLayout(name, new ConstantValueProvider<StackEntry>(entry));
        }
        public MenuLayoutBuilder AddLayout(string name, ValueProvider<StackEntry> layoutProvider)
        {
            return this.AddLayout(new ConstantValueProvider<string>(name), layoutProvider);
        }
        public MenuLayoutBuilder AddLayout(ValueProvider<string> nameProvider, StackEntry layout)
        {
            return this.AddLayout(nameProvider, new ConstantValueProvider<StackEntry>(layout));
        }
        public MenuLayoutBuilder AddLayout(ValueProvider<string> nameProvider, ValueProvider<StackEntry> layoutProvider)
        {
            this.layoutNameProviders.Add(nameProvider);
            this.destinationProviders.Add(layoutProvider);
            return this;
        }

        public LayoutChoice_Set Build()
        {
            return new MenuLayout(this.layoutNameProviders, this.destinationProviders, this.layoutStack);
        }

        List<ValueProvider<string>> layoutNameProviders = new List<ValueProvider<string>>();
        List<ValueProvider<StackEntry>> destinationProviders = new List<ValueProvider<StackEntry>>();
        LayoutStack layoutStack;
    }

    class MenuLayout : ContainerLayout
    {
        public MenuLayout(List<ValueProvider<string>> buttonNameProviders, List<ValueProvider<StackEntry>> destinationProviders, LayoutStack layoutStack)
        {
            this.buttonNameProviders = buttonNameProviders;
            this.destinationProviders = destinationProviders;
            this.layoutStack = layoutStack;
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (this.SubLayout == null)
                this.SubLayout = this.build();
            for (int i = 0; i < this.buttonNameProviders.Count; i++)
            {
                this.buttons[i].Text = this.buttonNameProviders[i].Get();
            }
            return base.GetBestLayout(query);
        }
        private LayoutChoice_Set build()
        {
            Vertical_GridLayout_Builder builder = new Vertical_GridLayout_Builder().Uniform();
            this.buttons = new List<Button>();
            this.buttonDestinations = new Dictionary<Button, ValueProvider<StackEntry>>();
            for (int i = 0; i < this.buttonNameProviders.Count; i++)
            {
                Button button = new Button();
                button.Clicked += Button_Clicked;
                this.buttons.Add(button);
                builder.AddLayout(new ButtonLayout(button));
                ValueProvider<StackEntry> destinationProvider = destinationProviders[i];
                this.buttonDestinations[button] = destinationProvider;
            }
            return builder.BuildAnyLayout();
        }

        private void Button_Clicked(object sender, System.EventArgs e)
        {
            Button button = sender as Button;
            ValueProvider<StackEntry> destinationProvider = this.buttonDestinations[button];
            this.layoutStack.AddLayout(destinationProvider.Get());
        }

        List<ValueProvider<string>> buttonNameProviders;
        List<ValueProvider<StackEntry>> destinationProviders;
        List<Button> buttons;
        Dictionary<Button, ValueProvider<StackEntry>> buttonDestinations;
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
