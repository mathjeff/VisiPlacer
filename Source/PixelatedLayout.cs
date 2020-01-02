using System;
using Xamarin.Forms;

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
            double width = query.MaxWidth;
            if (this.pixelWidth > 0)
                width = Math.Floor(query.MaxWidth / this.pixelWidth) * this.pixelWidth;
            double height = query.MaxHeight;
            if (this.pixelHeight > 0)
                height = Math.Floor(query.MaxHeight / this.pixelHeight) * this.pixelHeight;
            if (width != query.MaxWidth || height != query.MaxHeight)
                query = query.WithDimensions(width, height);
            SpecificLayout internalLayout = this.layoutToManage.GetBestLayout(query);
            if (internalLayout != null) {
                Size size = new Size(Math.Ceiling(internalLayout.Width / this.pixelWidth) * this.pixelWidth, Math.Ceiling(internalLayout.Height / this.pixelHeight) * this.pixelHeight);
                Specific_ContainerLayout result = new Specific_ContainerLayout(null, size, new LayoutScore(), internalLayout, new Thickness(0));
                return this.prepareLayoutForQuery(result, query);
            }
            return null;
        }

        public LayoutChoice_Set LayoutToManage
        {
            get
            {
                return this.layoutToManage;
            }
        }

        LayoutChoice_Set layoutToManage;
        double pixelWidth;
        double pixelHeight;
    }
}
