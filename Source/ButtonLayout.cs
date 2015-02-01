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
            LinkedList<LayoutChoice_Set> layoutChoices = new LinkedList<LayoutChoice_Set>();
            //layoutChoices.AddLast(new SingleItem_Layout(button, subLayout, new Thickness(3), LayoutScore.Zero, false)); // want to include the border if possible
            layoutChoices.AddLast(new SingleItem_Layout(button, subLayout, new Thickness(0), LayoutScore.Get_CutOff_LayoutScore(1), false)); // we can leave the border out but that's not desirable
            this.Set_LayoutChoices(layoutChoices);
        }
    }
}
