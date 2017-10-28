using Xamarin.Forms;

namespace VisiPlacement
{
    public class SingleItem_View : ContentView
    {
        public SingleItem_View()
        {
            this.Margin = new Thickness();
            this.Padding = new Thickness();
        }
        protected override void OnChildAdded(Element child)
        {
            this.InvalidateMeasure();
        }
        protected override void OnChildRemoved(Element child)
        {
            this.InvalidateMeasure();
        }
    }
}
