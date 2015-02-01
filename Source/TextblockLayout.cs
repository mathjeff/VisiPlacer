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
            this.Initialize(textBlock);
        }
        public TextblockLayout(String text)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.TextWrapping = TextWrapping.Wrap;
            this.Initialize(textBlock);
        }
        private void Initialize(TextBlock textBlock)
        {
            textBlock.Margin = new Thickness(0);
            textBlock.Padding = new Thickness(0);
            this.textBlock = textBlock;

            List<LayoutChoice_Set> layouts = new List<LayoutChoice_Set>();
            layouts.Add(new TextLayout(new TextBlock_Configurer(textBlock), 12));
                
            this.LayoutToManage = new LayoutUnion(layouts);

            //DependencyPropertyDescriptor textDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            //textDescriptor.AddValueChanged(textBlock, new EventHandler(this.OnTextChange));

            //this.Setup_PropertyChange_Listener("Text", this.textBlock, this.OnTextChange);
        }





        private TextBlock textBlock;
    }
}
