using System;
using System.Collections.Generic;
using System.Text;
using VisiPlacement;
using Xamarin.Forms;

namespace VisiPlacement
{
    // a PopoutTextbox is a text box that becomes fullscreen while the user edits it
    public class PopoutTextbox : TitledControl, OnBack_Listener
    {
        public PopoutTextbox(string title, LayoutStack layoutStack) : base(title)
        {
            this.TitleLayout.AlignVertically(TextAlignment.Center);
            this.title = title;
            this.layoutStack = layoutStack;

            Button button = new Button();
            button.Clicked += Button_Clicked;
            this.button = button;

            this.buttonLayout = new ButtonLayout(button, null, -1, false, true, true);

            this.textBox = new Editor();
            this.detailsLayout = new TitledControl(title, ScrollLayout.New(new TextboxLayout(this.textBox)));

            this.SetContent(buttonLayout);
        }

        public void Placeholder(string text)
        {
            this.placeholder = text;
            this.updateButtonText();
        }
        private void Button_Clicked(object sender, EventArgs e)
        {
            this.layoutStack.AddLayout(new StackEntry(this.detailsLayout, this.title, this));
        }

        public void OnBack(LayoutChoice_Set other)
        {
            this.updateButtonText();
        }

        public string Text
        {
            get
            {
                return this.textBox.Text;
            }
            set
            {
                this.textBox.Text = value;
                this.updateButtonText();
            }
        }

        private void updateButtonText()
        {
            string text = this.textBox.Text;
            if ((text == null || text == "") && this.placeholder != null)
            {
                this.buttonLayout.setText(this.placeholder);
            }
            else
            {
                // note that the button text may appear cropped if needed
                this.buttonLayout.setText(text);
            }
        }

        private Button button;
        private ButtonLayout buttonLayout;
        private Editor textBox;
        private LayoutChoice_Set detailsLayout;
        private LayoutStack layoutStack;
        private string title;
        private string placeholder;
    }
}
