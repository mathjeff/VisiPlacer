using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace VisiPlacement
{
    public class ButtonLayout : LayoutUnion
    {
        public ButtonLayout(ContentControl button, LayoutChoice_Set subLayout)
        {
            this.Initialize(button, subLayout);
        }

        public ButtonLayout(ContentControl button, string content)
        {
            TextBlock block = new TextBlock();
            block.Text = content;
            this.Initialize(button, new TextblockLayout(block));
        }

        private void Initialize(ContentControl button, LayoutChoice_Set subLayout)
        {
            LinkedList<LayoutChoice_Set> layoutChoices = new LinkedList<LayoutChoice_Set>();
            // TODO: figure out how to get the button to stop automatically adding padding inside itself. The padding should be controlled only from here.
            layoutChoices.AddLast(new SingleItem_Layout(button, subLayout, new Thickness(10), LayoutScore.Zero, false)); // We want to include a border inside the button if possible
            layoutChoices.AddLast(new SingleItem_Layout(button, subLayout, new Thickness(0), LayoutScore.Get_CutOff_LayoutScore(1), false)); // If there isn't room for the border, then it's cropped
            this.Set_LayoutChoices(layoutChoices);
        }
    }
}
