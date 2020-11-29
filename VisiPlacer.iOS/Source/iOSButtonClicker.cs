using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using VisiPlacement;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace VisiPlacement.iOS
{
    public class iOSButtonClicker : ButtonClicker
    {
        public static void Initialize()
        {
            ButtonClicker.Instance = new iOSButtonClicker();
        }

        public override void ClickButton(Xamarin.Forms.Button button)
        {
            button.BackgroundColor = Color.FromRgba(0, 0, 0, 0);
            ClickButton(Get_iOSButton(button));
        }
        public override void MakeButtonAppearPressed(Button button)
        {
            button.BackgroundColor = Color.Green;
        }

        public void ClickButton(UIButton button)
        {
            button.SendActionForControlEvents(UIControlEvent.TouchUpInside);
        }
        public UIButton Get_iOSButton(Xamarin.Forms.Button button)
        {
            foreach (Xamarin.Forms.Effect e in button.Effects)
            {
                PlatformEffect platformEffect = e as PlatformEffect;
                if (platformEffect != null)
                    return platformEffect.Control as UIButton;
            }
            return null;
        }
    }
}