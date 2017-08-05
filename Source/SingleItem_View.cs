using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VisiPlacement
{
    class SingleItem_View : ContentControl
    {
        public SingleItem_View()
        {
            this.Margin = new Thickness();
            this.Padding = new Thickness();
        }
        //protected override void OnChildDesiredSizeChanged(FrameworkElement child)
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            this.InvalidateMeasure();
        }
    }
}
