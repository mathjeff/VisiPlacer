using Xamarin.Forms;

namespace VisiPlacement
{
    public class TextDiagnosticLayout : ContainerLayout
    {
        public static LayoutChoice_Set New()
        {
            return new TextDiagnosticLayout();
        }
        public TextDiagnosticLayout() {
            //GridLayout grid0 = GridLayout.New(BoundProperty_List.Uniform(2), BoundProperty_List.Uniform(1), new LayoutScore());
            Editor editor = new Editor();
            editor.TextChanged += Editor_TextChanged;
            //grid0.AddLayout(new TextboxLayout(editor));
            BoundProperty_List heights = new BoundProperty_List(3);
            heights.BindIndices(0, 1);
            heights.BindIndices(0, 2);
            heights.SetPropertyScale(0, 1);
            heights.SetPropertyScale(1, 1);
            heights.SetPropertyScale(2, 12);

            GridLayout grid1 = GridLayout.New(heights, new BoundProperty_List(1), LayoutScore.Zero);
            View bottomView = new ContentView();
            bottomView.BackgroundColor = Color.DarkGray;

            GridLayout grid2 = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(2), LayoutScore.Zero);

            this.editorToUpdate = new Editor();
            grid2.AddLayout(new TextboxLayout(this.editorToUpdate));
            View rightView = new ContentView();
            rightView.BackgroundColor = Color.Green;
            grid2.AddLayout(new ImageLayout(rightView, LayoutScore.Get_UsedSpace_LayoutScore(1)));

            grid1.AddLayout(grid2);
            grid1.AddLayout(new TextboxLayout(editor));
            grid1.AddLayout(new ImageLayout(bottomView, LayoutScore.Get_UsedSpace_LayoutScore(1000)));

            this.SubLayout = grid1;
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.editorToUpdate.Text = e.NewTextValue;
        }

        Editor editorToUpdate;
    }
}
