using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

// A ScrollLayout contains another layout inside it and enables scrolling.

// Understanding how the the score of a ScrollLayout works is a bit weird. Theoretically a caller could make a complicated layout containing several ScrollLayouts at various places.
// Most likely the way a user would interact with that kind of layout would be to scroll whichever layout contained the content most interesting to the user, and ignore the other layouts.
// However, if the user's screen is small, then it would be better for each ScrollLayout to be replaced with a ButtonLayout that when clicked, shows a ScrollLayout that fills the entire
// screen until the user dismisses the ScrollLayout. This would enable the user to use more of their screen while scrolling.

// For VisiPlacer to compare the relative values of a ButtonLayout and a ScrollLayout, there are these options:
// 1. Analyze the destination behind each ButtonLayout and ScrollLayout and compare their values
//    This would be quite difficult because the destination of a ButtonLayout could be dynamically-generated and could be based on a TextboxLayout elsewhere in the tree
//    This would also require VisiPlacer to understand the code being executed when the user clicks the ButtonLayout
// 2. Require the caller to explicitly dictate the value for the various layouts
//    It should be reasonable to implement this once it's needed.
// 3. Compute the value of each layout essentially based only on the pixels that it encompasses currently
//    This should be a good default to have, especially because callers are expected to only ask for one ScrollLayout onscreen at a time anyway

namespace VisiPlacement
{
    public class ScrollLayout : LayoutChoice_Set
    {
        public static LayoutChoice_Set New(LayoutChoice_Set subLayout)
        {
            return new PixelatedLayout(new LayoutCache(new ScrollLayout(subLayout)), 1);
        }

        private ScrollLayout(LayoutChoice_Set subLayout)
        {
            this.subLayout = subLayout;
            this.view = new ScrollView();
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            SpecificLayout result;
            if (query.MaxHeight < this.minHeight)
            {
                result = this.subLayout.GetBestLayout(query);
            }
            else
            {
                result = this.doGetBestLayout(query);
            }
            return this.prepareLayoutForQuery(result, query);
        }

        // suports MaxScore_LayoutQuery and MinWidth_LayoutQuery
        private Specific_ScrollLayout doGetBestLayout(LayoutQuery query)
        {
            // find the max-scoring layout, to put a bounds on the search space
            LayoutQuery subQuery = new MaxScore_LayoutQuery();
            subQuery.MaxWidth = query.MaxWidth;
            subQuery.MaxHeight = double.PositiveInfinity;
            subQuery.MinScore = LayoutScore.Minimum;

            SpecificLayout bestSublayout = this.subLayout.GetBestLayout(subQuery);
            if (bestSublayout == null)
                return null;

            // compute the score that we assign to the child's favorite layout
            Size ourSize = new Size(query.MaxWidth, query.MaxHeight);

            // We want to check a bunch of layout sizes for two reasons:
            // 1. This lets us find the max-average-visible-score-at-once layout
            // 2. This means that slightly increasing the width cannot cause a huge increase in height and therefore a large decrease in max-average-visible-score-at-once
            // So, we need some sort of step size to use to avoid executing too slowly, and ourSize.Height is a convenient step size to use
            // Also note that stepping is more convenient when we consider the final partial page to exist but have no value in its lower portion.
            // That is, if ourSize.Height = 10 and bestSublayout.Height = 52, then the remaining 8 units of height (52 % 10 = 2, 10 - 2 = 8) are considered to take up space but
            // to not provide value. This enables us to adjust bestSublayout.Height by ourSize.Height at each iteration and still be sure of correctness.
            double bestNumWindows = Math.Ceiling(bestSublayout.Height / ourSize.Height);
            LayoutScore bestScorePerWindow = bestSublayout.Score.Times(1.0 / bestNumWindows);
            if (bestScorePerWindow.CompareTo(query.MinScore) < 0)
                bestSublayout = null;

            LayoutQuery queryTemplate;
            if (query.MinimizesHeight() || query.MaximizesScore())
            {
                queryTemplate = new MaxScore_LayoutQuery();
            }
            else
            {
                queryTemplate = new MinWidth_LayoutQuery();
            }
            queryTemplate.MaxWidth = query.MaxWidth;
            for (int numWindows = (int)bestNumWindows - 1; numWindows > 0; numWindows--)
            {
                if (bestScorePerWindow.CompareTo(LayoutScore.Zero) < 0)
                {
                    if (query.MaximizesScore())
                    {
                        // The only change in the sublayout's score that can happen as we shrink its size is that its score can decrease.
                        // If the sublayout's score stays approximately constant, then its score per window can get further from zero
                        // So, if the score is already negative, then score will just continue to get more negative, and we should give up
                        break;
                    }
                }

                // shrink the size by one window and look for a better layout
                subQuery = queryTemplate.Clone();
                subQuery.MaxHeight = numWindows * ourSize.Height;
                subQuery.MinScore = query.MinScore.Times(numWindows);
                if (bestSublayout != null)
                    subQuery.OptimizePastDimensions(new LayoutDimensions(bestSublayout.Width, bestSublayout.Height, bestScorePerWindow.Times(numWindows)));
                SpecificLayout childResult = this.subLayout.GetBestLayout(subQuery);
                if (childResult != null)
                {
                    // compute our score for the child's layout and update bestLayout
                    bestNumWindows = Math.Ceiling(childResult.Height / ourSize.Height);
                    bestScorePerWindow = childResult.Score.Times(1.0 / bestNumWindows);
                    bestSublayout = childResult;
                    // jump to the given window
                    numWindows = (int)(Math.Min(numWindows, bestNumWindows));
                }
            }




            if (bestSublayout == null)
                return null;

            Size size = new Size(bestSublayout.Width, Math.Min(bestSublayout.Height, query.MaxHeight));

            Specific_ScrollLayout result = new Specific_ScrollLayout(this.view, size, bestScorePerWindow, bestSublayout);
            if (!query.Accepts(result))
            {
                ErrorReporter.ReportParadox("ScrollLayout.getMaxScoreOrMinWidthLayout returning illegal response");
                LayoutQuery debugQuery = query.Clone();
                debugQuery.Debug = true;
                this.doGetBestLayout(debugQuery);
            }
            return result;
        }

        private LayoutChoice_Set subLayout;
        private ScrollView view;
        private double minHeight = 1;
    }


    public class Specific_ScrollLayout : Specific_ContainerLayout
    {
        public Specific_ScrollLayout(View view, Size size, LayoutScore score, SpecificLayout sublayout)
            : base(view, size, score, sublayout, new Thickness())
        {
        }

        protected override Size chooseSize(Size availableSize)
        {
            return new Size(Math.Max(availableSize.Width, this.SubLayout.Width), this.SubLayout.Height);
        }

        public override SpecificLayout Clone()
        {
            return new Specific_ScrollLayout(this.View, this.Size, this.Score, this.SubLayout);
        }

    }

}
