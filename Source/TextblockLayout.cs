﻿using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VisiPlacement
{
    public class TextblockLayout : LayoutCache
    {
        public TextblockLayout(TextBlock textBlock, double fontSize = -1)
        {
            this.Initialize(textBlock, fontSize);
        }
        public TextblockLayout(String text, double fontsize = -1)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.TextWrapping = TextWrapping.Wrap;
            this.Initialize(textBlock, fontsize);
        }
        public TextblockLayout(string text, TextAlignment textAlignment)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.TextAlignment = textAlignment;
            this.Initialize(textBlock, -1);
        }
        private void Initialize(TextBlock textBlock, double fontsize)
        {
            textBlock.Margin = new Thickness(0);
            textBlock.Padding = new Thickness(0);
            this.textBlock = textBlock;

            List<LayoutChoice_Set> layouts = new List<LayoutChoice_Set>();
            if (fontsize > 0)
            {
                layouts.Add(this.makeLayout(fontsize));
            }
            else
            {
                layouts.Add(this.makeLayout(30));
                layouts.Add(this.makeLayout(16));
                layouts.Add(this.makeLayout(10));
            }
                
            this.LayoutToManage = new LayoutUnion(layouts);
        }

        private LayoutChoice_Set makeLayout(double fontsize)
        {
            TextBlock_Configurer configurer = new TextBlock_Configurer(this.textBlock);
            TextLayout layout = new TextLayout(configurer, fontsize);
            layout.ScoreIfEmpty = false; // no points for an empty text box
            return layout;
        }



        private TextBlock textBlock;
    }
}
