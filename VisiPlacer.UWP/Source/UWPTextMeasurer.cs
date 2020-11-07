﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace VisiPlacement.UWP
{
    public class UWPTextMeasurer : TextMeasurer
    {
        public static void Initialize()
        {
            TextMeasurer.Instance = new UWPTextMeasurer();
        }

        public override global::Xamarin.Forms.Size Measure(string text, double fontSize, string fontName)
        {
            TextBlock textBlock;
            if (fontName != null && fontName != "")
            {
                textBlock = this.textBlockwithCustomFont;
                textBlock.FontFamily = this.getFontFamily(fontName);
            }
            else
            {
                textBlock = this.textBlockwithDefaultFont;
            }

            textBlock.Text = text;
            textBlock.FontSize = fontSize;
            textBlock.Measure(new Windows.Foundation.Size(Double.PositiveInfinity, Double.PositiveInfinity));

            return new Xamarin.Forms.Size(textBlock.DesiredSize.Width, textBlock.DesiredSize.Height);
        }

        private FontFamily getFontFamily(string name)
        {
            if (name == null)
                return null;
            if (!this.fonts.ContainsKey(name))
                this.fonts[name] = new FontFamily(name);
            return this.fonts[name];
        }

        TextBlock textBlockwithCustomFont = new TextBlock();
        TextBlock textBlockwithDefaultFont = new TextBlock();
        Dictionary<string, FontFamily> fonts = new Dictionary<string, FontFamily>();
    }
}
