using System;
using System.Collections.Generic;
using System.Text;
using VisiPlacement;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class Change_ViewSize_Layout : ContainerLayout
    {
        public Change_ViewSize_Layout(ViewManager viewManager)
        {
            this.viewManager = viewManager;
            this.widthBox = new Editor();
            this.widthBox.Keyboard = Keyboard.Numeric;
            this.heightBox = new Editor();
            this.heightBox.Keyboard = Keyboard.Numeric;
            Button okButton = new Button();
            okButton.Clicked += OkButton_Clicked;

            this.SubLayout = new Vertical_GridLayout_Builder()
                .AddLayout(new TitledControl("Display Width", new TextboxLayout(this.widthBox)))
                .AddLayout(new TitledControl("Display Height", new TextboxLayout(this.heightBox)))
                .AddLayout(new ButtonLayout(okButton, "Update"))
                .Build();
        }

        private double parseNumber(string text)
        {
            double result;
            if (double.TryParse(text, out result))
                return result;
            return 0;
        }
        private Size getSize()
        {
            double width = this.parseNumber(this.widthBox.Text);
            double height = this.parseNumber(this.heightBox.Text);
            return new Size(width, height);
        }

        private void OkButton_Clicked(object sender, EventArgs e)
        {
            Size size = this.getSize();
            this.viewManager.forceSize(size);
        }

        private ViewManager viewManager;
        private Editor widthBox;
        private Editor heightBox;
    }
}
