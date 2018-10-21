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

        public LayoutChoice_Set Build()
        {
            List<LayoutChoice_Set> fontChoices = new List<LayoutChoice_Set>();
            int fontSize;
            for (fontSize = 20; fontSize >= 12; fontSize -= 8)
            {
                GridLayout gridLayout = GridLayout.New(new BoundProperty_List(this.messages.Count()), BoundProperty_List.Uniform(1), LayoutScore.Zero);
                foreach (string message in this.messages)
                {
                    string section = "    " + message;
                    gridLayout.AddLayout(new TextblockLayout(section, fontSize));
                }
                fontChoices.Add(gridLayout);
            }
            List<LayoutChoice_Set> allChoices = new List<LayoutChoice_Set>();
            foreach (LayoutChoice_Set fontChoice in fontChoices)
            {
                allChoices.Add(fontChoice);
                allChoices.Add(ScrollLayout.New(fontChoice));
            }
            return new LayoutUnion(allChoices);
        }

        private LinkedList<string> messages = new LinkedList<string>();

    }
}
