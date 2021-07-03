using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class DisablableButtonLayout : ContainerLayout
    {
        public event ClickedHandler Clicked;
        public delegate void ClickedHandler(object sender, EventArgs e);

        public DisablableButtonLayout(string text)
        {
            this.text = text;
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (this.enabled)
                this.SubLayout = this.getButtonLayout();
            else
                this.SubLayout = this.getTextblock();
            return base.GetBestLayout(query);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (this.Clicked != null)
                this.Clicked.Invoke(this, e);
        }

        public void SetEnabled(bool enabled)
        {
            if (this.enabled != enabled)
            {
                this.enabled = enabled;
                this.AnnounceChange(true);
            }
        }

        public void SetText(string text)
        {
            this.text = text;
            if (this.buttonLayout != null)
                this.buttonLayout.setText(text);
            if (this.textblockLayout != null)
                this.textblockLayout.setText(text);
        }

        private ButtonLayout getButtonLayout()
        {
            if (this.buttonLayout == null)
            {
                Button button = new Button();
                button.Clicked += Button_Clicked;
                this.buttonLayout = new ButtonLayout(button, this.text);
            }
            return this.buttonLayout;
        }

        private TextblockLayout getTextblock()
        {
            if (this.textblockLayout == null)
            {
                this.textblockLayout = new TextblockLayout(this.text).AlignHorizontally(TextAlignment.Center).AlignVertically(TextAlignment.Center);
            }
            return this.textblockLayout;
        }

        private ButtonLayout buttonLayout;
        private TextblockLayout textblockLayout;
        private string text;
        private bool enabled = true;
    }
}
