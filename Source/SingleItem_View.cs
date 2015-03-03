using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
