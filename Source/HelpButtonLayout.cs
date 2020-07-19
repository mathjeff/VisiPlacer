using Xamarin.Forms;

namespace VisiPlacement
{
    public class HelpButtonLayout : LayoutCache
    {
        public HelpButtonLayout(LayoutChoice_Set detailLayout, LayoutStack layoutStack)
        {
            this.initialize("Help", detailLayout, layoutStack);
        }

        public HelpButtonLayout(string message, LayoutChoice_Set detailLayout, LayoutStack layoutStack)
        {
            this.initialize(message, detailLayout, layoutStack);
        }

        private void initialize(string message, LayoutChoice_Set detailLayout, LayoutStack layoutStack)
        {
            Button button = new Button();
            ButtonLayout buttonLayout = new ButtonLayout(button, message);

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
