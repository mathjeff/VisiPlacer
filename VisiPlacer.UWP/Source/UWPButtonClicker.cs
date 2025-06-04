using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VisiPlacement.UWP
{
    public class UWPButtonClicker : ButtonClicker
    {
        public static void Initialize()
        {
            ButtonClicker.Instance = new UWPButtonClicker();
        }
        public override void ClickButton(Button button)
        {
            this.ClickButton(this.Get_UWPButton(button));
        }
        public Button Get_UWPButton(Button button)
        {
            foreach (Effect e in button.Effects)
            {
                PlatformEffect<Object, Button> platformEffect = e as PlatformEffect<Object, Button>;
                if (platformEffect != null)
                    return platformEffect.Control;
            }
            return null;
        }
        /*public override void ClickButton(Button button)
        {
            throw new NotImplementedException();
        }*/
        public override void MakeButtonAppearPressed(Button button)
        {
            button.BackgroundColor = Colors.Green;
        }
    }
}
