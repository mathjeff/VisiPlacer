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
            bool isFirst = (this.components.Count == 0);
            this.components.Add(new HelpParagraph(message, isFirst));
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
            foreach (HelpBlock block in this.components)
            {
                builder.AddLayout(block.Get(fontSize, this.components.Count()));
            }
            return builder.Build();
        }
        public LayoutChoice_Set Build()
        {
            List<LayoutChoice_Set> fontChoices = new List<LayoutChoice_Set>();
            for (int i = 28; i >= 20; i -= 2)
            {
                LayoutChoice_Set large = this.MakeSublayout(i);
                fontChoices.Add(large);
            }
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
        public HelpParagraph(string text, bool isFirstBlock)
        {
            this.text = text;
            this.isFirstBlock = isFirstBlock;
        }
        public LayoutChoice_Set Get(double fontsize, int numComponents)
        {
            TextblockLayout result;
            if (numComponents > 1)
            {
                // indent paragraphs unless there's only one
                string prefix = "    ";
                if (!isFirstBlock)
                {
                    // blank lines between paragraphs
                    prefix = "\n" + prefix;
                }
                result = new TextblockLayout(prefix + this.text, fontsize);
            }
            else
            {
                result = new TextblockLayout(this.text, fontsize);
            }
            result.AlignVertically(Xamarin.Forms.TextAlignment.Center);
            return result;
        }
        private string text;
        private bool isFirstBlock;
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
