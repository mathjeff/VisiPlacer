using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class Choose_LayoutDefaults_Layout : ContainerLayout
    {
        public event Chose_LayoutDefaultsHandler Chose_LayoutDefaults;
        public delegate void Chose_LayoutDefaultsHandler(LayoutDefaults defaults);
        public Choose_LayoutDefaults_Layout(IEnumerable<LayoutDefaults> choices)
        {
            // title
            Vertical_GridLayout_Builder builder = new Vertical_GridLayout_Builder();

            builder.AddLayout(new TextblockLayout("Choose a theme!"));

            Editor sampleTextbox = new Editor();
            sampleTextbox.Text = "Sample editable text";
            builder.AddLayout(new TextboxLayout(sampleTextbox));

            // individual themes
            List<LayoutDefaults> choiceList = new List<LayoutDefaults>(choices);
            int numColumns = 2;
            int numRows = (choiceList.Count + 1) / 2;
            GridLayout grid = GridLayout.New(BoundProperty_List.Uniform(numRows), BoundProperty_List.Uniform(numColumns), LayoutScore.Zero);
            foreach (LayoutDefaults choice in choiceList)
            {
                // add a separator so the user can see when it changes
                OverrideLayoutDefaults_Layout container = new OverrideLayoutDefaults_Layout(choice);
                container.SubLayout = this.makeDemoLayout(choice);
                grid.AddLayout(container);
            }
            builder.AddLayout(grid);

            // scrollable
            this.SubLayout = ScrollLayout.New(builder.Build());
        }

        private LayoutChoice_Set makeDemoLayout(LayoutDefaults defaults)
        {
            Button okButton = new Button();
            okButton.Clicked += OkButton_Clicked;
            this.defaultsByButton[okButton] = defaults;

            return new ButtonLayout(okButton, defaults.DisplayName);
        }

        private void OkButton_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            LayoutDefaults layoutDefaults = this.defaultsByButton[button];
            if (this.Chose_LayoutDefaults != null)
                this.Chose_LayoutDefaults.Invoke(layoutDefaults);
        }

        private Dictionary<Button, LayoutDefaults> defaultsByButton = new Dictionary<Button, LayoutDefaults>();
    }

    class OverrideLayoutDefaults_Layout : ContainerLayout
    {
        public OverrideLayoutDefaults_Layout(LayoutDefaults defaultsOverride)
        {
            this.defaultsOverride = defaultsOverride;
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            SpecificLayout result = this.SubLayout.GetBestLayout(query);
            if (result != null)
                result = new OverrideLayoutDefaults_SpecificLayout(result, this.defaultsOverride);
            return this.prepareLayoutForQuery(result, query);
        }

        LayoutDefaults defaultsOverride;
    }

    class OverrideLayoutDefaults_SpecificLayout : Specific_ContainerLayout
    {
        public OverrideLayoutDefaults_SpecificLayout(SpecificLayout sublayout, LayoutDefaults defaultsOverride)
            : base(null, sublayout.Size, LayoutScore.Zero, sublayout, new Thickness(0))
        {
            this.defaultsOverride = defaultsOverride;
        }
        public override View DoLayout(Size displaySize, LayoutDefaults layoutDefaults)
        {
            return this.SubLayout.DoLayout(displaySize, this.defaultsOverride);
        }
        public override SpecificLayout Clone()
        {
            return new OverrideLayoutDefaults_SpecificLayout(this.SubLayout, this.defaultsOverride);
        }

        LayoutDefaults defaultsOverride;
    }
}
