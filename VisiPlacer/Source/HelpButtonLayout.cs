using Microsoft.Maui.Controls;

namespace VisiPlacement
{
    public class HelpButtonLayout : LayoutCache
    {
        public HelpButtonLayout(LayoutChoice_Set detailLayout, LayoutStack layoutStack, double fontSize = -1)
        {
            this.initialize("Help", detailLayout, layoutStack, fontSize = -1);
        }

        public HelpButtonLayout(string message, LayoutChoice_Set detailLayout, LayoutStack layoutStack, double fontSize = -1)
        {
            this.initialize(message, detailLayout, layoutStack, fontSize);
        }

        private void initialize(string message, LayoutChoice_Set detailLayout, LayoutStack layoutStack, double fontSize)
        {
            this.message = message;
            Button button = new Button();
            ButtonLayout buttonLayout = new ButtonLayout(button, message, fontSize);

            this.detailLayout = detailLayout;
            this.layoutStack = layoutStack;


            button.Clicked += Button_Clicked;
            this.LayoutToManage = buttonLayout;
        }

        private void Button_Clicked(object sender, System.EventArgs e)
        {
            this.layoutStack.AddLayout(new StackEntry(this.detailLayout, this.message, null));
        }

        private LayoutStack layoutStack;
        private LayoutChoice_Set detailLayout;
        private string message;
    }
}
