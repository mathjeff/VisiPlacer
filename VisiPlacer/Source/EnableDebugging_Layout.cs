﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class EnableDebugging_Layout : ContainerLayout
    {
        public EnableDebugging_Layout(ViewManager viewManager)
        {
            Button button = new Button();
            button.Clicked += Button_Clicked;
            this.button = button;
            this.viewManager = viewManager;
            this.UpdateText();
            this.SubLayout = new ButtonLayout(button);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            this.viewManager.Debugging = !this.viewManager.Debugging;
            this.UpdateText();
        }

        private void UpdateText()
        {
            if (this.viewManager.Debugging)
                this.button.Text = "Disable Layout Debugging";
            else
                this.button.Text = "Enable Layout Debugging";
        }

        private ViewManager viewManager;
        private Button button;
    }
}
