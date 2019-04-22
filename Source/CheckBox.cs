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
            : base(new List<String>() { falseValue, trueValue})
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
