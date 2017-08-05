using System;
using Windows.UI.Xaml;
using Windows.Foundation;

// Intended for displaying an image
// Just computes score = width * height * constant and answers LayoutQueries accordingly
namespace VisiPlacement
{
    public class ImageLayout : LayoutChoice_Set
    {
        public ImageLayout(FrameworkElement view, LayoutScore scorePerPixel)
        {
            this.view = view;
            this.stepSize = 1;
            this.scorePerPixel = new LayoutScore(scorePerPixel);
            this.maxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
        }
        public ImageLayout(FrameworkElement view, LayoutScore scorePerPixel, Size maxSize)
        {
            this.view = view;
            this.stepSize = 1;
            this.scorePerPixel = new LayoutScore(scorePerPixel);
            this.maxSize = maxSize;
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            double maxWidth = Math.Min(this.maxSize.Width, query.MaxWidth);
            double maxHeight = Math.Min(this.maxSize.Height, query.MaxHeight);
            LayoutScore score = this.ComputeScore(maxWidth, maxHeight);
            if (score.CompareTo(query.MinScore) < 0)
                return null;
            double ratio = query.MinScore.DividedBy(score);
            if (double.IsNaN(ratio))
            {
                // if the best score we can get is zero, then don't bother trying to use any space
                ratio = 0;
            }
            if (query.MinimizesWidth())
            {
                double width = Math.Ceiling(maxWidth * ratio / this.stepSize) * this.stepSize;
                if (width < 0)
                {
                    width = 0;
                }
                if (this.ComputeScore(width, maxHeight).CompareTo(query.MinScore) < 0)
                {
                    // the score has some additional components that the division didn't catch, so we have to add another pixel
                    width += this.stepSize;
                }
                if (width > maxWidth)
                {
                    // We had to round up past the max height, so there is no solution
                    return null;
                }
                return this.MakeLayout(width, maxHeight, query);
            }
            if (query.MinimizesHeight())
            {
                double height = Math.Ceiling(maxHeight * ratio / this.stepSize) * this.stepSize;
                if (height < 0)
                {
                    height = 0;
                }
                if (this.ComputeScore(maxWidth, height).CompareTo(query.MinScore) < 0)
                {
                    // the score has some additional components that the division didn't catch, so we have to add another pixel
                    height += this.stepSize;
                }
                if (height > maxHeight)
                {
                    // We had to round up past the max height, so there is no solution
                    return null;
                }

                return this.MakeLayout(maxWidth, height, query);
            }
            return MakeLayout(maxWidth, maxHeight, query);
        }
        /*public double PixelSize
        {
            set
            {
                this.pixelSize = value;
                this.AnnounceChange(false);
            }
        }*/
        private SpecificLayout MakeLayout(double width, double height, LayoutQuery layoutQuery)
        {
            Specific_SingleItem_Layout specificLayout = new Specific_SingleItem_Layout(this.view, new Size(width, height), this.ComputeScore(width, height), null, new Thickness());
            specificLayout.ChildFillsAvailableSpace = false;
            SpecificLayout layout = this.prepareLayoutForQuery(specificLayout, layoutQuery);
            if (!layoutQuery.Accepts(layout))
            {
                System.Diagnostics.Debug.WriteLine("Error; ImageLayout attempted to return an invalid layout result");
            }
            return layout;
        }
        private LayoutScore ComputeScore(double width, double height)
        {
            return this.scorePerPixel.Times(Math.Min(this.maxSize.Width, width) * Math.Min(this.maxSize.Height, height));
        }

        FrameworkElement view;
        private LayoutScore scorePerPixel;
        private double stepSize;
        private Size maxSize;
    }
}
