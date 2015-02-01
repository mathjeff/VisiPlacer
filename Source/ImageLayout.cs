using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

// Intended for displaying an image
namespace VisiPlacement
{
    public class ImageLayout : LayoutChoice_Set
    {
        public ImageLayout(FrameworkElement view, LayoutScore scorePerPixel)
        {
            this.view = view;
            this.pixelSize = 1;
            this.scorePerPixel = new LayoutScore(scorePerPixel);
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            LayoutScore score = this.ComputeScore(query.MaxWidth, query.MaxHeight);
            if (score.CompareTo(query.MinScore) < 0)
                return null;
            double ratio = query.MinScore.DividedBy(score);
            if (query.MinimizesWidth())
            {
                double width = Math.Ceiling(query.MaxWidth * ratio / this.pixelSize) * this.pixelSize;
                if (this.ComputeScore(width, query.MaxHeight).CompareTo(query.MinScore) < 0)
                {
                    // the score has some additional components that the division didn't catch, so we have to add another pixel
                    width += this.pixelSize;
                }
                return this.MakeLayout(width, query.MaxHeight, query);
            }
            if (query.MinimizesHeight())
            {
                double height = Math.Ceiling(query.MaxHeight * ratio / this.pixelSize) * this.pixelSize;
                if (this.ComputeScore(query.MaxWidth, height).CompareTo(query.MinScore) < 0)
                {
                    // the score has some additional components that the division didn't catch, so we have to add another pixel
                    height += this.pixelSize;
                }
                return this.MakeLayout(query.MaxWidth, height, query);
            }
            return MakeLayout(query.MaxWidth, query.MaxHeight, query);
        }
        private SpecificLayout MakeLayout(double width, double height, LayoutQuery layoutQuery)
        {
            return this.prepareLayoutForQuery(new Specific_SingleItem_Layout(this.view, new Size(width, height), this.ComputeScore(width, height), null, new Thickness()), layoutQuery);
        }
        private LayoutScore ComputeScore(double width, double height)
        {
            return this.scorePerPixel.Times(width * height);
        }

        FrameworkElement view;
        private LayoutScore scorePerPixel;
        private double pixelSize;
    }
}
