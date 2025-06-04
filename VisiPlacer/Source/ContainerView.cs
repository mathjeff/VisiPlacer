using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace VisiPlacement
{
    public class ContainerView : ContentView
    {
        public ContainerView()
        {
            this.Margin = new Thickness();
            this.Padding = new Thickness();
        }
        protected override void OnChildAdded(Element child)
        {
            this.InvalidateMeasure();
        }
        /*protected override void OnChildRemoved(Element child)
        {
            this.InvalidateMeasure();
        }*/
    }
}
