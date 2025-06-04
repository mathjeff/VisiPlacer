using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VisiPlacement
{
    public class EnableDebugging_Layout : ContainerLayout
    {
        public EnableDebugging_Layout(ViewManager viewManager)
        {
            Button button = new Button();
            button.Clicked += Button_Clicked;
            this.button = button;
            this.buttonLayout = new ButtonLayout(button);
            this.viewManager = viewManager;
            this.UpdateText();
            this.SubLayout = this.buttonLayout;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            this.viewManager.Debugging = !this.viewManager.Debugging;
            this.UpdateText();
        }

        private void UpdateText()
        {
            if (this.viewManager.Debugging)
                this.buttonLayout.setText("Disable Layout Debugging");
            else
                this.buttonLayout.setText("Enable Layout Debugging");
        }

        private ViewManager viewManager;
        private Button button;
        private ButtonLayout buttonLayout;
    }
}
