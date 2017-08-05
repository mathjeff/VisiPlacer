using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
            Thickness buttonPadding = new Thickness(1);
            this.BorderThickness = buttonPadding;

            // The color of the inside of the bevel
            Thickness innerBevelThickness = new Thickness(2);
            Border border = new Border();
            border.BorderThickness = innerBevelThickness;
            border.BorderBrush = new SolidColorBrush(Colors.Gray);
            border.Padding = new Thickness();
            border.Margin = new Thickness();

            Thickness outerBevelThickness = new Thickness(2);
            button.Margin = new Thickness();
            button.Padding = new Thickness();
            button.BorderThickness = outerBevelThickness;

            // Put the desired content directly inside the bevel without any extra margin
            SingleItem_Layout contentLayout = new SingleItem_Layout(border, subLayout, outerBevelThickness, LayoutScore.Zero, false);
            // Put the inner bevel color directly inside the outer bevel color without any blank space
            this.SubLayout = new SingleItem_Layout(button, contentLayout, innerBevelThickness, LayoutScore.Zero, true);

        }
    }
}
