using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VisiPlacement
{
    public abstract class TextMeasurer
    {
        public static TextMeasurer Instance;
        public abstract Size Measure(string text, double fontSize, string fontName);
    }
}
