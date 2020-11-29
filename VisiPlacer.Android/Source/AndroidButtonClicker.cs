using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms.Platform.Android;

namespace VisiPlacement.Android
{
    public class AndroidButtonClicker : ButtonClicker
    {
        public static void Initialize()
        {
            ButtonClicker.Instance = new AndroidButtonClicker();
        }


        public override void ClickButton(Xamarin.Forms.Button button)
        {
            button.BackgroundColor = Xamarin.Forms.Color.FromRgba(0, 0, 0, 0);
            ClickButton(Get_AndroidButton(button));
        }
        public override void MakeButtonAppearPressed(Xamarin.Forms.Button button)
        {
            button.BackgroundColor = Color.Green;
        }

        public void ClickButton(global::Android.Widget.Button button)
        {
            button.PerformClick();
        }
        public Button Get_AndroidButton(Xamarin.Forms.Button button)
        {
            foreach (Xamarin.Forms.Effect e in button.Effects)
            {
                PlatformEffect platformEffect = e as PlatformEffect;
                if (platformEffect != null)
                    return platformEffect.Control as Button;
            }
            return null;
        }
    }
}