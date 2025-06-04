using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using VisiPlacement;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VisiPlacement.iOS
{
    public class iOSButtonClicker : ButtonClicker
    {
        public static void Initialize()
        {
            ButtonClicker.Instance = new iOSButtonClicker();
        }

        public override void ClickButton(Button button)
        {
            button.BackgroundColor = Color.FromRgba(0, 0, 0, 0);
            ClickButton(Get_iOSButton(button));
        }
        public override void MakeButtonAppearPressed(Button button)
        {
            button.BackgroundColor = Colors.Green;
        }

        public void ClickButton(UIButton button)
        {
            button.SendActionForControlEvents(UIControlEvent.TouchUpInside);
        }
        public UIButton Get_iOSButton(Button button)
        {
            foreach (Effect e in button.Effects)
            {
                PlatformEffect<UIView, UIView> platformEffect = e as PlatformEffect<UIView, UIView>;
                if (platformEffect != null)
                    return platformEffect.Control as UIButton;
            }
            return null;
        }
    }
}