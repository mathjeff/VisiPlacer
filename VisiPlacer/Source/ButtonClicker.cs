using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls;

namespace VisiPlacement
{
    public abstract class ButtonClicker
    {
        public static ButtonClicker Instance;
        public abstract void ClickButton(Button button);
        public abstract void MakeButtonAppearPressed(Button button);
    }
}
