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
            Vertical_GridLayout_Builder grid1Builder = new Vertical_GridLayout_Builder().Uniform();

            GridLayout grid2 = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(2), LayoutScore.Zero, 0.01);
            Editor textBox = new Editor();
            textBox.TextChanged += TextBox_TextChanged;
            this.textBox = textBox;

            grid1Builder.AddLayout(new TextboxLayout(textBox, 16));

            Label textBlock = new Label();
            textBlock.BackgroundColor = Color.Red;
            this.textBlockLayout = new TextblockLayout(textBlock, 16, false, false);
            this.textBlockLayout.ScoreIfEmpty = false;
            this.textBlockLayout.LoggingEnabled = true;
            grid2.PutLayout(this.textBlockLayout, 0, 0);
            grid2.PutLayout(new ImageLayout(null, LayoutScore.Get_MinPriorityScore_ForTesting(1)), 1, 0);

            grid1Builder.AddLayout(grid2);
            this.SubLayout = grid1Builder.Build();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.textBlockLayout.setText(this.textBox.Text);
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
        private Label textBlock;
        private TextblockLayout textBlockLayout;
    }
}
