using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

// Allows the user to enter a duration
namespace VisiPlacement
{
    public class DurationEntryView : SingleItem_Layout
    {
        public DurationEntryView()
        {
            this.textBox = new TextBox();
            this.textBox.TextChanged += this.TextBox_TextChanged;
            GridLayout grid = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(2), LayoutScore.Zero);
            grid.AddLayout(new TextboxLayout(this.textBox));
            grid.AddLayout(new TextblockLayout("days"));
            this.SubLayout = grid;
        }

        public bool IsDurationValid()
        {
            TimeSpan duration;
            return this.Parse(out duration);
        }

        public TimeSpan GetDuration()
        {
            TimeSpan duration;
            bool isValid = this.Parse(out duration);
            if (!isValid) {
                throw new FormatException("Invalid duration string " + this.getText());
            }
            return duration;
        }
        private void updateColor()
        {
            if (this.IsDurationValid())
                this.appearValid();
            else
                this.appearInvalid();
        }

        private void appearValid()
        {
            this.textBox.Background = new SolidColorBrush(Colors.White);
        }
        private void appearInvalid()
        {
            this.textBox.Background = new SolidColorBrush(Colors.Red);
        }

        private bool Parse(out TimeSpan result)
        {
            double numDays;
            bool valid = double.TryParse(this.getText(), out numDays);
            if (valid) {
                result = TimeSpan.FromDays(numDays);
            }
            return valid;
        }
        private string getText()
        {
            return this.textBox.Text;
        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.updateColor();
        }

        private TextBox textBox;
    }
}
