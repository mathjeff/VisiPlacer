using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class CheckBox : Button
    {
        public CheckBox(string falseValue, string trueValue)
        {
            this.falseValue = falseValue;
            this.trueValue = trueValue;
            this.updateText();
            this.Clicked += CheckBox_Clicked;
        }

        public string SelectedItem
        {
            get
            {
                if (this.selected)
                    return this.trueValue;
                else
                    return this.falseValue;
            }
        }
        public bool Checked
        {
            get
            {
                return this.selected;
            }
            set
            {
                this.selected = value;
                this.updateText();
            }
        }

        private void CheckBox_Clicked(object sender, EventArgs e)
        {
            this.selected = !this.selected;
            this.updateText();
        }
        private void updateText()
        {
            this.Text = this.SelectedItem;
        }

        string falseValue;
        string trueValue;
        bool selected;
    }
}
