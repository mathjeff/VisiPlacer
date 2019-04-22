using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class SingleSelect : Button
    {
        public SingleSelect(List<String> choices)
        {
            this.items = choices;
            this.updateText();
            this.Clicked += SingleSelect_Clicked;
        }


        public string SelectedItem
        {
            get
            {
                return this.items[this.selectedIndex];
            }
        }

        public int SelectedIndex
        {
            get
            {
                return this.selectedIndex;
            }
            set
            {
                this.selectedIndex = value;
                this.updateText();
            }
        }


        private void SingleSelect_Clicked(object sender, EventArgs e)
        {
            this.selectedIndex = (this.selectedIndex + 1) % this.items.Count;
            this.updateText();
        }
        private void updateText()
        {
            this.Text = this.SelectedItem;
        }

        List<String> items;
        int selectedIndex;
    }
}
