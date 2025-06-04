using System;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VisiPlacement
{
    class PixelatedLayout : ContainerLayout
    {
        public PixelatedLayout(LayoutChoice_Set layoutToManage, double pixelSize)
        {
            this.SubLayout = layoutToManage;
            pixelWidth = pixelHeight = pixelSize;
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
            SpecificLayout internalLayout = this.SubLayout.GetBestLayout(query);
            if (internalLayout != null) {
                Size size = new Size(Math.Ceiling(internalLayout.Width / this.pixelWidth) * this.pixelWidth, Math.Ceiling(internalLayout.Height / this.pixelHeight) * this.pixelHeight);
                Specific_ContainerLayout result = new Specific_ContainerLayout(null, size, new LayoutScore(), internalLayout, new Thickness(0));
                return this.prepareLayoutForQuery(result, query);
            }
            return null;
        }


        double pixelWidth;
        double pixelHeight;
    }
}
