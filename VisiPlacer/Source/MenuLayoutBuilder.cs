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
            layout = LayoutCache.For(layout);
            return this.AddLayout(new StackEntry(layout, name, null));
        }
        public MenuLayoutBuilder AddLayout(StackEntry entry)
        {
            entry.Layout = LayoutCache.For(entry.Layout);
            return this.AddLayout(entry.Name, new ConstantValueProvider<StackEntry>(entry));
        }
        public MenuLayoutBuilder AddLayout(string name, StackEntry entry)
        {
            entry.Layout = LayoutCache.For(entry.Layout);
            return this.AddLayout(name, new ConstantValueProvider<StackEntry>(entry));
        }
        public MenuLayoutBuilder AddLayout(string name, ValueProvider<StackEntry> layoutProvider)
        {
            return this.AddLayout(new ConstantValueProvider<MenuItem>(new MenuItem(name, null)), layoutProvider);
        }
        public MenuLayoutBuilder AddLayout(ValueProvider<MenuItem> nameProvider, StackEntry entry)
        {
            entry.Layout = LayoutCache.For(entry.Layout);
            return this.AddLayout(nameProvider, new ConstantValueProvider<StackEntry>(entry));
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
            this.layoutStack.Navigated += LayoutStack_Navigated;
        }

        private void LayoutStack_Navigated()
        {
            // make sure that we get get the chance to update our button layouts if needed
            this.AnnounceChange(false);
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (this.SubLayout == null)
                this.SubLayout = this.build();
            for (int i = 0; i < this.buttonNameProviders.Count; i++)
            {
                MenuItem menuItem = this.buttonNameProviders[i].Get();
                this.buttons[i].SetText(menuItem.Name);
                this.buttons[i].SetEnabled(menuItem.Enabled);
                this.subtitles[i].setText(menuItem.Subtitle);
            }
            return base.GetBestLayout(query);
        }
        private LayoutChoice_Set build()
        {
            GridLayout_Builder verticalBuilder = new Vertical_GridLayout_Builder().Uniform();
            GridLayout_Builder horizontalBuilder = new Horizontal_GridLayout_Builder().Uniform();
            this.buttons = new List<DisablableButtonLayout>();
            this.subtitles = new List<TextblockLayout>();
            this.buttonDestinations = new Dictionary<DisablableButtonLayout, ValueProvider<StackEntry>>();
            for (int i = 0; i < this.buttonNameProviders.Count; i++)
            {
                DisablableButtonLayout button = new DisablableButtonLayout("");
                button.Clicked += Button_Clicked;
                this.buttons.Add(button);

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
                entryGrid.AddLayout(button);
                entryGrid.AddLayout(subtitleLayout);
                LayoutChoice_Set content = new LayoutUnion(entryGrid, button);
                LayoutChoice_Set fullContent = new LayoutUnion(entryGrid, button);

                // Also consider embedding the destination layout if possible
                // We can only add embed the destination layout if it didn't have an OnBack listener, because OnBack is ambiguous for an embedded layout
                StackEntry destinationEntry = this.destinationProviders[i].Get();
                if (destinationEntry.Listeners.Count < 1)
                {
                    LayoutChoice_Set destination = destinationEntry.Layout;
                    // We don't want to recurse arbitrarily far because that would be wasteful, so we only enable this check if the layout being considered is another MenuLayout
                    LayoutChoice_Set contained = destination;
                    LayoutCache cache = contained as LayoutCache;
                    if (cache != null)
                        contained = cache.LayoutToManage;
                    if (contained is MenuLayout)
                    {
                        // Allow dynamically embedding the layout if there's space.
                        // Most of the time there won't be space, but this should usually be fast to determine
                        LayoutChoice_Set inlinedContent = new Vertical_GridLayout_Builder().Uniform().AddLayout(content).AddLayout(destination).BuildAnyLayout();
                        fullContent = new LayoutUnion(content, inlinedContent);
                    }
                }

                horizontalBuilder.AddLayout(content);
                verticalBuilder.AddLayout(fullContent);

                ValueProvider<StackEntry> destinationProvider = destinationProviders[i];
                this.buttonDestinations[button] = destinationProvider;
            }
            LayoutChoice_Set verticalLayout = verticalBuilder.BuildAnyLayout();
            LayoutChoice_Set horizontalLayout = new ScoreShifted_Layout(horizontalBuilder.BuildAnyLayout(), LayoutScore.Get_UnCentered_LayoutScore(1));
            return LayoutCache.For(new LayoutUnion(verticalLayout, horizontalLayout));
        }

        private void Button_Clicked(object sender, System.EventArgs e)
        {
            DisablableButtonLayout button = sender as DisablableButtonLayout;
            ValueProvider<StackEntry> destinationProvider = this.buttonDestinations[button];
            this.layoutStack.AddLayout(destinationProvider.Get());
        }

        List<ValueProvider<MenuItem>> buttonNameProviders;
        List<ValueProvider<StackEntry>> destinationProviders;
        List<DisablableButtonLayout> buttons;
        List<TextblockLayout> subtitles;
        Dictionary<DisablableButtonLayout, ValueProvider<StackEntry>> buttonDestinations;
        LayoutStack layoutStack;
    }

    public class MenuItem
    {
        public MenuItem(string name, string subtitle, bool enabled = true)
        {
            this.Name = name;
            this.Subtitle = subtitle;
            this.Enabled = enabled;
        }
        public string Name;
        public string Subtitle;
        public bool Enabled;
    }

}
