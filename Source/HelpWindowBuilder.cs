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
            this.messages.AddLast(message);
            return this;
        }

        private LayoutChoice_Set MakeSublayout(double fontSize)
        {
            if (messages.Count() > 1)
            {
                GridLayout gridLayout = GridLayout.New(new BoundProperty_List(this.messages.Count()), BoundProperty_List.Uniform(1), LayoutScore.Zero);
                foreach (string message in this.messages)
                {
                    string section = "    " + message;
                    gridLayout.AddLayout(new TextblockLayout(section, fontSize));
                }
                return gridLayout;
            }
            else
            {
                return new TextblockLayout(messages.First(), fontSize);
            }
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

        private LinkedList<string> messages = new LinkedList<string>();

    }
}
