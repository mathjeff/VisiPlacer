using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class CheckBox : SingleSelect
    {
        public CheckBox(string falseValue, string trueValue)
            : this(falseValue, Color.FromRgba(0, 0, 0, 0), trueValue, Color.FromRgba(0, 0, 0, 0))
        {
        }

        public CheckBox(string falseValue, Color falseBackgroundColor, string trueValue, Color trueBackgroundColor)
            : base(new List<SingleSelect_Choice>() { new SingleSelect_Choice(falseValue, falseBackgroundColor), new SingleSelect_Choice(trueValue, trueBackgroundColor) })
        {
        }


        public bool Checked
        {
            get
            {
                return this.SelectedIndex == 1;
            }
            set
            {
                if (value)
                    base.SelectedIndex = 1;
                else
                    base.SelectedIndex = 0;
            }
        }
    }
}
