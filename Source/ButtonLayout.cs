using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace VisiPlacement
{
    public class ButtonLayout : SingleItem_Layout
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
            // add a small border, so that it's easy to see where the buttons end
            this.BorderThickness = new Thickness(1);
            // we don't need any additional border inside of the button itself
            this.SubLayout = new SingleItem_Layout(button, subLayout, new Thickness(0), LayoutScore.Zero, false);
        }
    }
}
