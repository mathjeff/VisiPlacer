using System;
using Xamarin.Forms;

// Intended for displaying an image
// Just computes score = width * height * constant and answers LayoutQueries accordingly
namespace VisiPlacement
{
    public class ImageLayout : LayoutChoice_Set
    {
        public ImageLayout(View view, LayoutScore scorePerPixel)
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
                if (width > query.MaxWidth)
                {
                    // We had to round up past the max height, so there is no solution
                    return null;
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
                if (height > query.MaxHeight)
                {
                    // We had to round up past the max height, so there is no solution
                    return null;
                }

                return this.MakeLayout(query.MaxWidth, height, query);
            }
            return MakeLayout(query.MaxWidth, query.MaxHeight, query);
        }
        private SpecificLayout MakeLayout(double width, double height, LayoutQuery layoutQuery)
        {
            SpecificLayout layout = this.prepareLayoutForQuery(new Specific_SingleItem_Layout(this.view, new Size(width, height), this.ComputeScore(width, height), null, new Thickness()), layoutQuery);
            if (!layoutQuery.Accepts(layout))
            {
                ErrorReporter.ReportParadox("Error; ImageLayout attempted to return an invalid layout result");
            }
            return layout;
        }
        private LayoutScore ComputeScore(double width, double height)
        {
            return this.scorePerPixel.Times(width * height);
        }

        View view;
        private LayoutScore scorePerPixel;
        private double pixelSize;
    }
}
