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
        private TextMeasurement_Test_Layout()
        {
            GridLayout gridLayout = GridLayout.New(new BoundProperty_List(2), new BoundProperty_List(2), LayoutScore.Zero);
            Editor textBox = new Editor();
            textBox.TextChanged += TextBox_TextChanged;
            this.textBox = textBox;

            gridLayout.PutLayout(new TextboxLayout(textBox, 16), 0, 0);
            Label textBlock = new Label();
            this.textBlock = textBlock;
            //gridLayout.PutLayout(new TextblockLayout(textBlock, 16), 0, 1);
            gridLayout.PutLayout(new ImageLayout(null, LayoutScore.Get_UsedSpace_LayoutScore(1)), 1, 1);

            this.SubLayout = gridLayout;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.textBlock.Text = this.textBox.Text;
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
        private Label textBlock;
    }
}
