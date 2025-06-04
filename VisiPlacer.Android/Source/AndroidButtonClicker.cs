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

namespace VisiPlacement.Android
{
    public class AndroidButtonClicker : ButtonClicker
    {
        public static void Initialize()
        {
            ButtonClicker.Instance = new AndroidButtonClicker();
        }


        public override void ClickButton(Microsoft.Maui.Controls.Button button)
        {
            button.Background = Microsoft.Maui.Graphics.Color.FromRgba(0, 0, 0, 0);
            ClickButton(Get_AndroidButton(button));
        }
        public override void MakeButtonAppearPressed(Microsoft.Maui.Controls.Button button)
        {
            button.BackgroundColor = Microsoft.Maui.Graphics.Colors.Green;
        }

        public void ClickButton(global::Android.Widget.Button button)
        {
            button.PerformClick();
        }
        public Button Get_AndroidButton(Microsoft.Maui.Controls.Button button)
        {
            foreach (Microsoft.Maui.Controls.Effect e in button.Effects)
            {
                Microsoft.Maui.Controls.PlatformEffect<Object, Object> platformEffect = e as Microsoft.Maui.Controls.PlatformEffect<Object, Object>;
                if (platformEffect != null)
                    return platformEffect.Control as Button;
            }
            return null;
        }
    }
}