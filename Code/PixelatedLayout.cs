using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace VisiPlacement
{
    class PixelatedLayout : LayoutChoice_Set
    {
        public PixelatedLayout(LayoutChoice_Set layoutToManage, double pixelSize)
        {
            this.layoutToManage = layoutToManage;
            pixelWidth = pixelHeight = pixelSize;
            layoutToManage.AddParent(this);
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (this.pixelWidth > 0)
                query.MaxWidth = Math.Floor(query.MaxWidth / this.pixelWidth) * this.pixelWidth;
            if (this.pixelHeight > 0)
                query.MaxHeight = Math.Floor(query.MaxHeight / this.pixelHeight) * this.pixelHeight;
            SpecificLayout internalLayout = this.layoutToManage.GetBestLayout(query.Clone());
            if (internalLayout != null) {
                Size size = new Size(Math.Ceiling(internalLayout.Width / this.pixelWidth) * this.pixelWidth, Math.Ceiling(internalLayout.Height / this.pixelHeight) * this.pixelHeight);
                Specific_SingleItem_Layout result = new Specific_SingleItem_Layout(null, size, internalLayout.Score, internalLayout, new System.Windows.Thickness(0));
                return this.prepareLayoutForQuery(result, query);
            }
            return null;
        }

        LayoutChoice_Set layoutToManage;
        double pixelWidth;
        double pixelHeight;
    }
}
