using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisiPlacement
{
    public class HelpWindowBuilder
    {
        public HelpWindowBuilder()
        {
        }

        public HelpWindowBuilder AddMessage(string message)
        {
            this.components.Add(new HelpParagraph(message));
            return this;
        }

        public HelpWindowBuilder AddLayout(LayoutChoice_Set layout)
        {
            this.components.Add(new HelpLayout(layout));
            return this;
        }

        private LayoutChoice_Set MakeSublayout(double fontSize)
        {
            Vertical_GridLayout_Builder builder = new Vertical_GridLayout_Builder();
            GridLayout gridLayout = GridLayout.New(new BoundProperty_List(this.components.Count()), BoundProperty_List.Uniform(1), LayoutScore.Zero);
            foreach (HelpBlock block in this.components)
            {
                builder.AddLayout(block.Get(fontSize, this.components.Count()));
            }
            return builder.Build();
        }
        public LayoutChoice_Set Build()
        {
            List<LayoutChoice_Set> fontChoices = new List<LayoutChoice_Set>();
            LayoutChoice_Set large = this.MakeSublayout(20);
            fontChoices.Add(ScrollLayout.New(large));
            LayoutChoice_Set small = this.MakeSublayout(16);
            fontChoices.Add(small);
            return new LayoutUnion(fontChoices);
        }

        private List<HelpBlock> components = new List<HelpBlock>();
    }

    // a block (generally a paragraph) that goes inside a help window
    interface HelpBlock
    {
        LayoutChoice_Set Get(double fontsize, int numComponents);
    }

    // a paragraph inside a help window
    class HelpParagraph : HelpBlock
    {
        public HelpParagraph(string text)
        {
            this.text = text;
        }
        public LayoutChoice_Set Get(double fontsize, int numComponents)
        {
            if (numComponents > 1)
                return new TextblockLayout("    " + this.text, fontsize);
            return new TextblockLayout(this.text, fontsize);
        }
        private string text;
    }

    // a layout inside a help window
    class HelpLayout : HelpBlock
    {
        public HelpLayout(LayoutChoice_Set layout)
        {
            this.layout = layout;
        }
        public LayoutChoice_Set Get(double fontsize, int numComponents)
        {
            return this.layout;
        }
        private LayoutChoice_Set layout;
    }
}
