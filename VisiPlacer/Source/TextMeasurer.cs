using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace VisiPlacement
{
    public abstract class TextMeasurer
    {
        public static TextMeasurer Instance;
        public abstract Size Measure(string text, double fontSize, string fontName);
    }
}
