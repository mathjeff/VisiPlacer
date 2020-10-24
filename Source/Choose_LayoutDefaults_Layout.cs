using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class Choose_LayoutDefaults_Layout : ContainerLayout
    {
        public event Chose_VisualDefaults_Handler Chose_VisualDefaults;
        public delegate void Chose_VisualDefaults_Handler(VisualDefaults defaults);
        public Choose_LayoutDefaults_Layout(IEnumerable<VisualDefaults> choices)
        {
            // title
            Vertical_GridLayout_Builder builder = new Vertical_GridLayout_Builder();

            builder.AddLayout(new TextblockLayout("Choose a theme!"));

            Editor sampleTextbox = new Editor();
            sampleTextbox.Text = "Sample editable text";
            builder.AddLayout(new TextboxLayout(sampleTextbox));

            // individual themes
            List<VisualDefaults> choiceList = new List<VisualDefaults>(choices);
            int numColumns = 2;
            int numRows = (choiceList.Count + 1) / 2;
            GridLayout grid = GridLayout.New(BoundProperty_List.Uniform(numRows), BoundProperty_List.Uniform(numColumns), LayoutScore.Zero);
            foreach (VisualDefaults choice in choiceList)
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

        private LayoutChoice_Set makeDemoLayout(VisualDefaults defaults)
        {
            Button okButton = new Button();
            okButton.Clicked += OkButton_Clicked;
            this.defaultsByButton[okButton] = defaults;

            return new ButtonLayout(okButton, defaults.DisplayName);
        }

        private void OkButton_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            VisualDefaults layoutDefaults = this.defaultsByButton[button];
            if (this.Chose_VisualDefaults != null)
                this.Chose_VisualDefaults.Invoke(layoutDefaults);
        }

        private Dictionary<Button, VisualDefaults> defaultsByButton = new Dictionary<Button, VisualDefaults>();
    }

    class OverrideLayoutDefaults_Layout : ContainerLayout
    {
        public OverrideLayoutDefaults_Layout(VisualDefaults defaultsOverride)
        {
            this.defaultsOverride = defaultsOverride;
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            SpecificLayout result = this.SubLayout.GetBestLayout(query);
            if (result != null)
                result = new OverrideLayoutDefaults_SpecificLayout(result, this.defaultsOverride.ViewDefaults);
            return this.prepareLayoutForQuery(result, query);
        }

        VisualDefaults defaultsOverride;
    }

    class OverrideLayoutDefaults_SpecificLayout : Specific_ContainerLayout
    {
        public OverrideLayoutDefaults_SpecificLayout(SpecificLayout sublayout, ViewDefaults defaultsOverride)
            : base(null, sublayout.Size, LayoutScore.Zero, sublayout, new Thickness(0))
        {
            this.defaultsOverride = defaultsOverride;
        }
        public override View DoLayout(Size displaySize, ViewDefaults layoutDefaults)
        {
            return this.SubLayout.DoLayout(displaySize, this.defaultsOverride);
        }
        public override SpecificLayout Clone()
        {
            return new OverrideLayoutDefaults_SpecificLayout(this.SubLayout, this.defaultsOverride);
        }

        ViewDefaults defaultsOverride;
    }
}
