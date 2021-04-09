using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class TextMeasurement_Test_Layout : ContainerLayout
    {
        private static int colorIndex = 0;
        private static double scoreWeight = 1;

        public static LayoutChoice_Set New ()
        {
            return new TextMeasurement_Test_Layout();
        }
        public TextMeasurement_Test_Layout()
        {
            GridLayout_Builder gridBuilder = new Vertical_GridLayout_Builder().Uniform();

            Editor textBox = new Editor();
            textBox.TextChanged += TextBox_TextChanged;
            this.textBox = textBox;
            gridBuilder.AddLayout(new TextboxLayout(textBox, 16));

            for (double i = 0; i < 8; i += 1)
            {
                Label textBlock1 = new Label();
                textBlock1.BackgroundColor = Color.Red;
                TextblockLayout textBlockLayout = new TextblockLayout(textBlock1, i + 16, false, true);
                textBlockLayout.ScoreIfEmpty = false;
                gridBuilder.AddLayout(textBlockLayout);
                this.textBlockLayouts.Add(textBlockLayout);
            }

            this.SubLayout = ScrollLayout.New(gridBuilder.Build());
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (TextblockLayout textBlockLayout in this.textBlockLayouts)
            {
                textBlockLayout.setText(this.textBox.Text);
            }
        }

        /*
       private static LayoutChoice_Set NewRow(string text)
       {
           Horizontal_GridLayout_Builder builder = new Horizontal_GridLayout_Builder();

           builder.AddLayout(New_TextBoxLayout(text));

           builder.AddLayout(new ImageLayout(null, LayoutScore.Get_UsedSpace_LayoutScore(GetNextScoreWeight())));
           return builder.Build();

       }
*/

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            return base.GetBestLayout(query);
        }
        private static Color GetNextColor()
        {
            colorIndex++;
            return colorChoices[colorIndex % colorChoices.Count];
        }
        private static double GetNextScoreWeight()
        {
            scoreWeight *= 2;
            return scoreWeight;
        }
        private static List<Color> colorChoices = new List<Color>() { Color.Red, Color.Yellow, Color.Blue, Color.Orange, Color.Green, Color.Purple };

        private Editor textBox;
        private List<TextblockLayout> textBlockLayouts = new List<TextblockLayout>();
    }
}
