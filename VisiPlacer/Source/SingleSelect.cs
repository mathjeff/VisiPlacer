using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class SingleSelect_Choice
    {
        public SingleSelect_Choice(String choice, Color backgroundColor)
        {
            this.Content = choice;
            this.BackgroundColor = backgroundColor;
        }
        public String Content;
        public Color BackgroundColor;
    }


    public class SingleSelect : Button
    {
        public SingleSelect(List<String> choices)
        {
            List<SingleSelect_Choice> buttonChoices = new List<SingleSelect_Choice>();
            foreach (String content in choices)
            {
                buttonChoices.Add(new SingleSelect_Choice(content, Color.FromRgba(0, 0, 0, 0)));
            }
            this.items = buttonChoices;
            this.initialize();
        }

        public SingleSelect(List<SingleSelect_Choice> choices)
        {
            this.items = choices;
            this.initialize();
        }
        private void initialize()
        {
            this.updateAppearance();
            this.Clicked += SingleSelect_Clicked;
        }


        public string SelectedItem
        {
            get
            {
                return this.items[this.selectedIndex].Content;
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
                this.updateAppearance();
            }
        }

        public void Advance()
        {
            this.selectedIndex = (this.selectedIndex + 1) % this.items.Count;
            this.updateAppearance();
        }


        private void SingleSelect_Clicked(object sender, EventArgs e)
        {
            this.Advance();
        }
        private void updateAppearance()
        {
            this.Text = this.SelectedItem;
            Color backgroundColor = this.items[this.selectedIndex].BackgroundColor;
            if (backgroundColor.A > 0)
                this.BackgroundColor = this.items[this.selectedIndex].BackgroundColor;
        }

        List<SingleSelect_Choice> items;
        int selectedIndex;
    }
}
