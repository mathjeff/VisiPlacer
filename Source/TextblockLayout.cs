using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Data;

namespace VisiPlacement
{
    public class TextblockLayout : LayoutCache
    {
        public TextblockLayout(TextBlock textBlock)
        {
            this.Initialize(textBlock, -1);
        }
        public TextblockLayout(String text, double fontsize = -1)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.TextWrapping = TextWrapping.Wrap;
            this.Initialize(textBlock, fontsize);
        }
        private void Initialize(TextBlock textBlock, double fontsize)
        {
            textBlock.Margin = new Thickness(0);
            textBlock.Padding = new Thickness(0);
            this.textBlock = textBlock;

            List<LayoutChoice_Set> layouts = new List<LayoutChoice_Set>();
            if (fontsize > 0)
            {
                layouts.Add(new TextLayout(new TextBlock_Configurer(textBlock), fontsize));
            }
            else
            {
                layouts.Add(new TextLayout(new TextBlock_Configurer(textBlock), 10));
                layouts.Add(new TextLayout(new TextBlock_Configurer(textBlock), 16));
                layouts.Add(new TextLayout(new TextBlock_Configurer(textBlock), 30));
            }
                
            this.LayoutToManage = new LayoutUnion(layouts);

            //DependencyPropertyDescriptor textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            //textDescriptor.AddValueChanged(textBlock, new EventHandler(this.OnTextChange));

            //this.Setup_PropertyChange_Listener("Text", this.textBlock, this.OnTextChange);
        }





        private TextBlock textBlock;
    }
}
