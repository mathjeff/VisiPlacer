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
    // A ScrollLayout will allow its content to scroll if needed
    public class ScrollLayout : LayoutUnion
    {
        public static LayoutChoice_Set New(LayoutChoice_Set subLayout)
        {
            return new ScrollLayout(subLayout);
        }

        private ScrollLayout(LayoutChoice_Set subLayout)
        {
            List<LayoutChoice_Set> subLayouts = new List<LayoutChoice_Set>();
            subLayouts.Add(subLayout);
            double pixelSize = 1;
            subLayouts.Add(new PixelatedLayout(new MustScroll_Layout(subLayout, pixelSize), pixelSize));
            this.Set_LayoutChoices(subLayouts);
        }
    }

    // a MustScroll_Layout will always put its content into a ScrollView
    public class MustScroll_Layout : LayoutChoice_Set
    {
        public MustScroll_Layout(LayoutChoice_Set subLayout, double pixelSize)
        {
            this.pixelSize = pixelSize;
            if (subLayout is LayoutCache)
                this.subLayout = subLayout;
            else
                this.subLayout = new LayoutCache(subLayout);
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            SpecificLayout result = this.GetBestLayout2(query);
            return result;

        }
        public SpecificLayout GetBestLayout1(LayoutQuery query)
        {
            // ask the ImageLayout how much space to use
            // TODO: make this cleaner and also maybe use a different algorithm
            SpecificLayout window = this.imageLayout.GetBestLayout(query);
            if (window == null)
                return null;
            // ask the sublayout for the best layout it can do for the given bounds
            // First get the highest-scoring layout for these bounds
            MaxScore_LayoutQuery maxScoreQuery = new MaxScore_LayoutQuery();
            maxScoreQuery.MaxWidth = query.MaxWidth;
            maxScoreQuery.MaxHeight = double.PositiveInfinity;
            maxScoreQuery.MinScore = LayoutScore.Minimum;
            SpecificLayout maxScore_sublayout = this.subLayout.GetBestLayout(maxScoreQuery);
            // Next, get the smallest-height layout with having at least as good of a score
            MinHeight_LayoutQuery minHeightQuery = new MinHeight_LayoutQuery();
            minHeightQuery.MaxWidth = query.MaxWidth;
            minHeightQuery.MaxHeight = maxScore_sublayout.Height;
            minHeightQuery.MinScore = maxScore_sublayout.Score;
            SpecificLayout minHeight_sublayout = this.subLayout.GetBestLayout(minHeightQuery);

            // Finally, return the result
            Specific_ContainerLayout child = new Specific_ScrollLayout(this.view, window.Size, window.Score, minHeight_sublayout);

            return this.prepareLayoutForQuery(child, query);
        }
        public SpecificLayout GetBestLayout2(LayoutQuery query)
        {
            // Find the max-scoring sublayout

            // Now we get a couple of representative sublayouts and do linear interpolation among them
            // It would be nice to check all existent sublayouts but that would take a while

            // Find the min-width sublayout having nonzero score
            MinWidth_LayoutQuery minWidthPositiveQuery = new MinWidth_LayoutQuery();
            minWidthPositiveQuery.MinScore = LayoutScore.Tiny;
            SpecificLayout minWidthPositiveSublayout = this.subLayout.GetBestLayout(minWidthPositiveQuery);
            if (minWidthPositiveSublayout == null || minWidthPositiveSublayout.Width > query.MaxWidth)
            {
                // our result will entail a negative score, so don't bother using any space
                return this.zeroOrNull(query);
            }
            else
            {
                MinHeight_LayoutQuery minHeightPositiveQuery = new MinHeight_LayoutQuery();
                minHeightPositiveQuery.MaxWidth = minWidthPositiveSublayout.Width;
                minHeightPositiveQuery.MaxHeight = minWidthPositiveSublayout.Height;
                minHeightPositiveQuery.MinScore = minWidthPositiveSublayout.Score;
                minWidthPositiveSublayout = this.subLayout.GetBestLayout(minHeightPositiveQuery);
                if (minWidthPositiveSublayout == null)
                {
                    ErrorReporter.ReportParadox("MinHeight query " + minHeightPositiveQuery + " could not find the response from the MinWidth query " + minWidthPositiveQuery + " for " + this);
                }
                LayoutScore minScorePerHeight;
                if (minWidthPositiveSublayout.Height <= 0)
                    minScorePerHeight = LayoutScore.Zero;
                else
                    minScorePerHeight = minWidthPositiveSublayout.Score.Times(1.0 / minWidthPositiveSublayout.Height);

                // Next, check for the layout of minimum width having maximum score
                SpecificLayout maxScore_subLayout = this.subLayout.GetBestLayout(new MaxScore_LayoutQuery());
                MinWidth_LayoutQuery minWidthAwesomeQuery = new MinWidth_LayoutQuery();
                minWidthAwesomeQuery.MinScore = maxScore_subLayout.Score;
                SpecificLayout minWidthAwesomeSublayoutIntermediate = this.subLayout.GetBestLayout(minWidthAwesomeQuery);
                if (minWidthAwesomeSublayoutIntermediate == null)
                {
                    ErrorReporter.ReportParadox("MinWidth query " + minWidthAwesomeQuery + " could not find the response from the MaxScore query for " + this);
                }
                MinHeight_LayoutQuery minHeightAwesomeQuery = new MinHeight_LayoutQuery();
                minHeightAwesomeQuery.MaxWidth = minWidthAwesomeSublayoutIntermediate.Width;
                minHeightAwesomeQuery.MaxHeight = minWidthAwesomeSublayoutIntermediate.Height;
                minHeightAwesomeQuery.MinScore = minWidthAwesomeSublayoutIntermediate.Score;
                SpecificLayout minWidthAwesomeSublayout = this.subLayout.GetBestLayout(minHeightAwesomeQuery);
                if (minWidthAwesomeSublayout == null)
                {
                    ErrorReporter.ReportParadox("MinHeight query " + minHeightAwesomeQuery + " could not find the response from the MinWidth query " + minWidthAwesomeQuery + " for " + this);
                }
                LayoutScore middleScorePerHeight;
                if (minWidthAwesomeSublayout.Height <= 0)
                    middleScorePerHeight = LayoutScore.Zero;
                else
                    middleScorePerHeight = minWidthAwesomeSublayout.Score.Times(1.0 / minWidthAwesomeSublayout.Height);
                if (middleScorePerHeight.CompareTo(minScorePerHeight) < 0)
                {
                    // Clamp so we don't give our caller inconsistent results where a wider layout might have less score
                    middleScorePerHeight = minScorePerHeight;
                }
                if (minWidthAwesomeSublayout.Width > query.MaxWidth || (query.MinimizesWidth() && query.Accepts(minWidthAwesomeSublayout)))
                {
                    // Use some sublayout having width between the positive-scoring layout and the max-scoring layout
                    return this.interpolate(minWidthPositiveSublayout.Size, minScorePerHeight, minWidthAwesomeSublayout.Size, middleScorePerHeight, query);
                }
                // If we get here, our sublayout will give max score but we should check how much height it needs

                MinHeight_LayoutQuery minHeightQuery = new MinHeight_LayoutQuery();
                minHeightQuery.MaxWidth = query.MaxWidth;
                minHeightQuery.MinScore = maxScore_subLayout.Score;
                SpecificLayout maxWidth_subLayout = this.subLayout.GetBestLayout(minHeightQuery);
                LayoutScore maxScorePerHeight;
                if (maxWidth_subLayout.Height <= 0)
                    maxScorePerHeight = LayoutScore.Zero;
                else
                    maxScorePerHeight = maxWidth_subLayout.Score.Times(1.0 / maxWidth_subLayout.Height);
                if (maxScorePerHeight.CompareTo(middleScorePerHeight) < 0)
                {
                    // Clamp so we don't give our caller inconsistent results where a wider layout might have less score
                    maxScorePerHeight = middleScorePerHeight;
                }
                // Now interpolate from the middle coordinates to the max coordinates
                return this.interpolate(minWidthAwesomeSublayout.Size, middleScorePerHeight, maxWidth_subLayout.Size, maxScorePerHeight, query);
            }

        }

        // Given two layouts and a query, does linear interpolation to tell which part of the connecting line the query prefers
        private SpecificLayout interpolate(Size leftSize, LayoutScore leftScorePerHeight, Size rightSize, LayoutScore rightScorePerHeight, LayoutQuery query)
        {
            List<LayoutDimensions> options = new List<LayoutDimensions>();

            LayoutScore leftScore = leftScorePerHeight.Times(leftSize.Height);
            LayoutScore rightScore = rightScorePerHeight.Times(rightSize.Height);

            if (query.MinimizesHeight())
            {
                // for a MinHeight query, shrink the height of the bounds more
                if (leftScore.CompareTo(query.MinScore) > 0)
                {
                    double multiplier = query.MinScore.DividedBy(leftScore);
                    if (!double.IsInfinity(multiplier))
                    {
                        leftSize.Height *= multiplier;
                        leftScore = leftScorePerHeight.Times(leftSize.Height);
                        if (leftScore.CompareTo(query.MinScore) < 0)
                        {
                            // the score had some extra components that the division didn't catch
                            leftSize.Height = Math.Floor(leftSize.Height / this.pixelSize + 1.0) * this.pixelSize;
                            leftScore = leftScorePerHeight.Times(leftSize.Height);

                            if (leftScore.CompareTo(query.MinScore) < 0)
                            {
                                // the height must have already been so close to an integer that it hardly changed, so the score didn't change at all due to rounding error
                                leftSize.Height += this.pixelSize;
                                leftScore = leftScorePerHeight.Times(leftSize.Height);

                                if (leftScore.CompareTo(query.MinScore) < 0)
                                    ErrorReporter.ReportParadox("leftScore " + leftScore + " < query.MinScore " + query.MinScore);
                            }
                        }
                    }
                }
                if (rightScore.CompareTo(query.MinScore) > 0)
                {
                    double multiplier = query.MinScore.DividedBy(rightScore);
                    if (!double.IsInfinity(multiplier))
                    {
                        rightSize.Height *= multiplier;
                        rightScore = rightScorePerHeight.Times(rightSize.Height);
                        if (rightScore.CompareTo(query.MinScore) < 0)
                        {
                            // the score had some extra components that the division didn't catch
                            rightSize.Height = Math.Floor(rightSize.Height / this.pixelSize + 1.0) * this.pixelSize;
                            rightScore = rightScorePerHeight.Times(rightSize.Height);
                            if (rightScore.CompareTo(query.MinScore) < 0)
                            {
                                // the height must have already been so close to an integer that it hardly changed, so the score didn't change at all due to rounding error
                                rightSize.Height += this.pixelSize;
                                rightScore = rightScorePerHeight.Times(rightSize.Height);
                                if (rightScore.CompareTo(query.MinScore) < 0)
                                    ErrorReporter.ReportParadox("rightScore " + rightScore + " < query.MinScore " + query.MinScore);
                            }
                        }
                    }
                }
            }

            // clamp the heights to only allow getting points for sizes up to the maximum available height
            if (leftSize.Height > query.MaxHeight)
            {
                leftSize.Height = query.MaxHeight;
                leftScore = leftScorePerHeight.Times(leftSize.Height);
            }
            if (rightSize.Height > query.MaxHeight)
            {
                rightSize.Height = query.MaxHeight;
                rightScore = rightScorePerHeight.Times(rightSize.Height);
            }


            // check the two ends
            options.Add(new LayoutDimensions(leftSize.Width, leftSize.Height, leftScore));
            options.Add(new LayoutDimensions(rightSize.Width, rightSize.Height, rightScore));

            // check the possibility of the score being the limiting factor
            LayoutScore leftScoreDifference = query.MinScore.Minus(leftScore);
            LayoutScore rightScoreDifference = query.MinScore.Minus(rightScore);
            if (leftScoreDifference.CompareTo(LayoutScore.Zero) != rightScoreDifference.CompareTo(LayoutScore.Zero))
            {
                // The score difference crosses 0 between the two points
                LayoutScore totalWeight = leftScoreDifference.Minus(rightScoreDifference);
                double rightFraction = leftScoreDifference.DividedBy(totalWeight);
                double leftFraction = 1 - rightFraction;
                double x = leftSize.Width * leftFraction + rightSize.Width * rightFraction;
                double y = leftSize.Height * leftFraction + rightSize.Height * rightFraction;
                LayoutScore score = leftScore.Times(leftFraction).Plus(rightScore.Times(rightFraction));
                options.Add(new LayoutDimensions(x, y, score));
            }

            // check the possibility of the width being the limiting factor
            double leftXDifference = query.MaxWidth - leftSize.Width;
            double rightXDifference = query.MaxWidth - rightSize.Width;
            if (leftXDifference.CompareTo(0) != rightXDifference.CompareTo(0))
            {
                // the width difference crosses 0 between the two points
                double totalWeight = leftXDifference - rightXDifference;
                double rightFraction = leftXDifference / totalWeight;
                double leftFraction = 1 - rightFraction;
                double x = leftSize.Width * leftFraction + rightSize.Width * rightFraction;
                double y = leftSize.Height * leftFraction + rightSize.Height * rightFraction;
                LayoutScore score = leftScore.Times(leftFraction).Plus(rightScore.Times(rightFraction));
                options.Add(new LayoutDimensions(x, y, score));
            }

            // check the possibility of the height being the limiting factor
            double leftYDifference = query.MaxHeight - leftSize.Height;
            double rightYDifference = query.MaxHeight - rightSize.Height;
            if (leftYDifference.CompareTo(0) != rightYDifference.CompareTo(0))
            {
                // the height difference crosses 0 between the two points
                double totalWeight = leftYDifference - rightYDifference;
                double rightFraction = leftYDifference / totalWeight;
                double leftFraction = 1 - rightFraction;
                double x = leftSize.Width * leftFraction + rightSize.Width * rightFraction;
                double y = leftSize.Height * leftFraction + rightSize.Height * rightFraction;
                LayoutScore score = leftScore.Times(leftFraction).Plus(rightScore.Times(rightFraction));
                options.Add(new LayoutDimensions(x, y, score));
            }

            // choose best dimensions
            LayoutDimensions bestDimensions = null;
            foreach (LayoutDimensions option in options)
            {
                if (query.PreferredLayout(bestDimensions, option) == option)
                    bestDimensions = option;
            }
            if (bestDimensions == null)
                return null;
            return this.makeLayout(new Size(bestDimensions.Width, bestDimensions.Height), bestDimensions.Score);
        }

        // returns the empty layout if it's accepted
        private SpecificLayout zeroOrNull(LayoutQuery query)
        {
            SpecificLayout empty = this.makeLayout(new Size(), LayoutScore.Minimum);
            if (query.Accepts(empty))
                return this.prepareLayoutForQuery(empty, query);
            return null;
        }

        private SpecificLayout makeLayout(Size size, LayoutScore score)
        {
            // get max scoring sublayout
            MaxScore_LayoutQuery maxScoreQuery = new MaxScore_LayoutQuery();
            maxScoreQuery.MaxWidth = size.Width;
            SpecificLayout maxScoringChild = this.subLayout.GetBestLayout(maxScoreQuery);
            // get min height sublayout
            MinHeight_LayoutQuery minHeightQuery = new MinHeight_LayoutQuery();
            minHeightQuery.MaxWidth = size.Width;
            minHeightQuery.MinScore = maxScoringChild.Score;
            SpecificLayout minHeightChild = this.subLayout.GetBestLayout(minHeightQuery);
            // return results
            return new Specific_ScrollLayout(this.view, size, score, minHeightChild);
        }

        private LayoutChoice_Set subLayout;
        private ScrollView view = new ScrollView();
        private ImageLayout imageLayout = new ImageLayout(null, LayoutScore.Get_UsedSpace_LayoutScore(1));
        private double pixelSize;

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
