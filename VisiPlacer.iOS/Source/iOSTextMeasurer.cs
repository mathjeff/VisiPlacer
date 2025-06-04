using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using UIKit;

namespace VisiPlacement.iOS
{
    public class iOSTextMeasurer : TextMeasurer
    {

        public static void Initialize()
        {
            TextMeasurer.Instance = new iOSTextMeasurer();
        }

        public override Microsoft.Maui.Graphics.Size Measure(string text, double fontSize, string fontName)
        {
            UIFont font = this.getFont(fontName, fontSize);

            UIStringAttributes attributes = new UIStringAttributes();
            attributes.Font = font;
            
            SizeF boundSize = new SizeF(float.PositiveInfinity, float.PositiveInfinity);
            NSStringDrawingOptions options = NSStringDrawingOptions.UsesFontLeading |
                          NSStringDrawingOptions.UsesLineFragmentOrigin;

            NSString nsText = new NSString(text);
            CGSize resultSize = nsText.GetBoundingRect(
                boundSize,
                options,
                attributes,
                null).Size;

            nsText.Dispose();

            return new Microsoft.Maui.Graphics.Size(
                Math.Ceiling((double)resultSize.Width),
                Math.Ceiling((double)resultSize.Height));
        }
        
        private UIFont getFont(string name, double size)
        {
            if (name == null)
                name = "";
            if (!this.fonts.ContainsKey(name))
                this.fonts[name] = new Dictionary<double, UIFont>();

            Dictionary<double, UIFont> fontsBySize = this.fonts[name];
            if (!fontsBySize.ContainsKey(size))
            {
                fontsBySize[size] = this.loadFont(name, size);
            }

            return fontsBySize[size];
        }

        private UIFont loadFont(string name, double size)
        {
            if (name == "")
                return UIFont.SystemFontOfSize((nfloat)size);
            return UIFont.FromName(name, (nfloat)size);
        }

        Dictionary<string, Dictionary<double, UIFont>> fonts = new Dictionary<string, Dictionary<double, UIFont>>();
    }
}
