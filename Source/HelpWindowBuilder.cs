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
            GridLayout layout = GridLayout.New(new BoundProperty_List(this.messages.Count()), BoundProperty_List.Uniform(1), LayoutScore.Zero);
            foreach (string message in this.messages)
            {
                string section = "    " + message;
                layout.AddLayout(new LayoutUnion(new TextblockLayout(section, 20), new TextblockLayout(section, 10)));
            }
            return layout;
        }

        private LinkedList<string> messages = new LinkedList<string>();

    }
}
