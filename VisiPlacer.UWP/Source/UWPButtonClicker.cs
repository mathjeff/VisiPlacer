using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Xamarin.Forms.Platform.UWP;

namespace VisiPlacement.UWP
{
    public class UWPButtonClicker : ButtonClicker
    {
        public static void Initialize()
        {
            ButtonClicker.Instance = new UWPButtonClicker();
        }
        public override void ClickButton(Xamarin.Forms.Button button)
        {
            this.ClickButton(this.Get_UWPButton(button));
        }
        public Windows.UI.Xaml.Controls.Button Get_UWPButton(Xamarin.Forms.Button button)
        {
            foreach (Xamarin.Forms.Effect e in button.Effects)
            {
                PlatformEffect platformEffect = e as PlatformEffect;
                if (platformEffect != null)
                    return platformEffect.Control as Windows.UI.Xaml.Controls.Button;
            }
            return null;
        }
        public void ClickButton(Windows.UI.Xaml.Controls.Button button)
        {
            throw new NotImplementedException();
        }
        public override void MakeButtonAppearPressed(Xamarin.Forms.Button button)
        {
            button.BackgroundColor = Color.Green;
        }
    }
}
