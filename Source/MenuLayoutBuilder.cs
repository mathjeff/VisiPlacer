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
            return this.AddLayout(new ConstantValueProvider<MenuItem>(new MenuItem(name, null)), layoutProvider);
        }
        public MenuLayoutBuilder AddLayout(ValueProvider<MenuItem> nameProvider, StackEntry layout)
        {
            return this.AddLayout(nameProvider, new ConstantValueProvider<StackEntry>(layout));
        }
        public MenuLayoutBuilder AddLayout(ValueProvider<MenuItem> nameProvider, ValueProvider<StackEntry> layoutProvider)
        {
            this.layoutNameProviders.Add(nameProvider);
            this.destinationProviders.Add(layoutProvider);
            return this;
        }

        public LayoutChoice_Set Build()
        {
            return new MenuLayout(this.layoutNameProviders, this.destinationProviders, this.layoutStack);
        }

        List<ValueProvider<MenuItem>> layoutNameProviders = new List<ValueProvider<MenuItem>>();
        List<ValueProvider<StackEntry>> destinationProviders = new List<ValueProvider<StackEntry>>();
        LayoutStack layoutStack;
    }

    class MenuLayout : ContainerLayout
    {
        public MenuLayout(List<ValueProvider<MenuItem>> buttonNameProviders, List<ValueProvider<StackEntry>> destinationProviders, LayoutStack layoutStack)
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
                MenuItem menuItem = this.buttonNameProviders[i].Get();
                this.buttons[i].Text = menuItem.Name;
                this.subtitles[i].setText(menuItem.Subtitle);
            }
            return base.GetBestLayout(query);
        }
        private LayoutChoice_Set build()
        {
            Vertical_GridLayout_Builder mainBuilder = new Vertical_GridLayout_Builder().Uniform();
            this.buttons = new List<Button>();
            this.subtitles = new List<TextblockLayout>();
            this.buttonDestinations = new Dictionary<Button, ValueProvider<StackEntry>>();
            for (int i = 0; i < this.buttonNameProviders.Count; i++)
            {
                Button button = new Button();
                button.Clicked += Button_Clicked;
                this.buttons.Add(button);

                ButtonLayout buttonLayout = new ButtonLayout(button);
                TextblockLayout subtitleLayout = new TextblockLayout();
                subtitleLayout.AlignHorizontally(TextAlignment.Center);
                subtitleLayout.AlignVertically(TextAlignment.Center);
                subtitleLayout.ScoreIfEmpty = false;
                this.subtitles.Add(subtitleLayout);

                BoundProperty_List columnWidths = new BoundProperty_List(2);
                columnWidths.SetPropertyScale(0, 2);
                columnWidths.SetPropertyScale(1, 1);
                columnWidths.BindIndices(0, 1);
                GridLayout entryGrid = GridLayout.New(new BoundProperty_List(1), columnWidths, LayoutScore.Get_UnCentered_LayoutScore(1));
                entryGrid.AddLayout(buttonLayout);
                entryGrid.AddLayout(subtitleLayout);

                LayoutUnion content = new LayoutUnion(entryGrid, buttonLayout);
                mainBuilder.AddLayout(content);

                ValueProvider<StackEntry> destinationProvider = destinationProviders[i];
                this.buttonDestinations[button] = destinationProvider;
            }
            return mainBuilder.BuildAnyLayout();
        }

        private void Button_Clicked(object sender, System.EventArgs e)
        {
            Button button = sender as Button;
            ValueProvider<StackEntry> destinationProvider = this.buttonDestinations[button];
            this.layoutStack.AddLayout(destinationProvider.Get());
        }

        List<ValueProvider<MenuItem>> buttonNameProviders;
        List<ValueProvider<StackEntry>> destinationProviders;
        List<Button> buttons;
        List<TextblockLayout> subtitles;
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

    public class MenuItem
    {
        public MenuItem(string name, string subtitle)
        {
            this.Name = name;
            this.Subtitle = subtitle;
        }
        public string Name;
        public string Subtitle;
    }

}
