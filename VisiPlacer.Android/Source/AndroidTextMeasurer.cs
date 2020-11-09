using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using static Android.Views.View;

namespace VisiPlacement.Android
{
    public class AndroidTextMeasurer : TextMeasurer
    {
        public static void Initialize()
        {
            TextMeasurer.Instance = new AndroidTextMeasurer();
        }

        public override Xamarin.Forms.Size Measure(string text, double fontSize, string fontName)
        {
            TextView textView = new TextView(global::Android.App.Application.Context);
            textView.Typeface = this.getTypeface(fontName);
            textView.SetTextSize(ComplexUnitType.Px, (float)fontSize);
            textView.SetText(text, TextView.BufferType.Normal);

            int widthMeasureSpec = MeasureSpec.MakeMeasureSpec(int.MaxValue, MeasureSpecMode.AtMost);
            int heightMeasureSpec = MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);
            textView.Measure(widthMeasureSpec, heightMeasureSpec);

            // Android rounds sizes down, which we try to compensate for
            return new Xamarin.Forms.Size(textView.MeasuredWidth + 1, textView.MeasuredHeight + 1);
        }

        // Gets typeface, potentially using the cache
        Typeface getTypeface(string fontName)
        {
            if (!this.typeFaces.ContainsKey(fontName))
                this.typeFaces[fontName] = this.loadTypeface(fontName);

            return this.typeFaces[fontName];
        }

        // Gets typeface, not using the cache
        // Accepts font names like "monospace" and font files like "myfile.ttf"
        // Also provides limited support for font paths like "myfile.ttf#myfont", converting them to "myfile.ttf"
        Typeface loadTypeface(string fontName)
        {
            if (fontName == null)
                return Typeface.Default;

            bool isAsset = false;
            int poundIndex = fontName.IndexOf('#');
            if (poundIndex > 0)
            {
                isAsset = true;
                fontName = fontName.Substring(0, poundIndex);
            }
            if (fontName.Contains("."))
                isAsset = true;

            if (isAsset)
                return Typeface.CreateFromAsset(global::Android.App.Application.Context.Assets, fontName);
            else
                return Typeface.Create(fontName, TypefaceStyle.Normal);
        }

        Dictionary<string, Typeface> typeFaces = new Dictionary<string, Typeface>();
    }

}
