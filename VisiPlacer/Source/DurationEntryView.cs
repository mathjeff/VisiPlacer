using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

// Allows the user to enter a duration
namespace VisiPlacement
{
    public class DurationEntryView : ContainerLayout
    {
        public DurationEntryView()
        {
            this.textBox = new Editor();
            this.textBox.Keyboard = Keyboard.Numeric;
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
            this.textBox.BackgroundColor = Colors.LightGray;
        }
        private void appearInvalid()
        {
            this.textBox.BackgroundColor = Colors.Red;
        }

        private bool Parse(out TimeSpan result)
        {
            double numDays;
            bool valid = double.TryParse(this.getText(), out numDays);
            if (valid) {
                result = TimeSpan.FromDays(numDays);
            } else
            {
                result = new TimeSpan(0);
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

        private Editor textBox;
    }
}
