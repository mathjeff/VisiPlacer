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
            return New(subLayout, new ScrollView());
        }
        public static LayoutChoice_Set New(LayoutChoice_Set subLayout, bool treatNegativeScoresAsZero)
        {
            return new ScrollLayout(subLayout, new ScrollView(), treatNegativeScoresAsZero);
        }

        public static LayoutChoice_Set New(LayoutChoice_Set subLayout, ScrollView scrollView, bool treatNegativeScoresAsZero = false)
        {
            return new ScrollLayout(subLayout, scrollView, treatNegativeScoresAsZero);
        }

        private ScrollLayout(LayoutChoice_Set subLayout, ScrollView scrollView, bool treatNegativeScoresAsZero)
        {
            List<LayoutChoice_Set> subLayouts = new List<LayoutChoice_Set>();
            LayoutCache sublayoutCache = LayoutCache.For(subLayout);
            double pixelSize = 1;
            subLayouts.Add(
                new ScoreShifted_Layout(
                    new PixelatedLayout(
                        new MustScroll_Layout(sublayoutCache, scrollView, pixelSize, treatNegativeScoresAsZero),
                        pixelSize
                    )
                ,
                    LayoutScore.Get_UnCentered_LayoutScore(1)
                )
            );
            subLayouts.Add(sublayoutCache);
            this.Set_LayoutChoices(subLayouts);
        }
    }

    // a MustScroll_Layout will always put its content into a ScrollView
    public class MustScroll_Layout : LayoutChoice_Set
    {
        public MustScroll_Layout(LayoutChoice_Set subLayout, double pixelSize, bool treatNegativeScoresAsZero) : this(subLayout, new ScrollView(), pixelSize, treatNegativeScoresAsZero)
        {
        }
        public MustScroll_Layout(LayoutChoice_Set subLayout, ScrollView view, double pixelSize, bool treatNegativeScoresAsZero)
        {
            this.pixelSize = pixelSize;
            this.treatNegativeScoresAsZero = treatNegativeScoresAsZero;
            this.subLayout = subLayout;
            this.subLayout.AddParent(this);
            this.view = view;
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (query.MaxHeight == 0 && query.MaxWidth >= 0)
            {
                // No height means no score
                return this.zeroOrNull(query);
            }

            // We get a couple of representative sublayouts and do linear interpolation among them
            // It would be nice to check all existent sublayouts but that would take a while

            // First, find the layout of min height among those having max score
            LayoutScore minInterestingScore = LayoutScore.Tiny;
            // The ScrollLayout's score won't be higher than that of its sublayout, so only check for high-scoring sublayouts
            if (minInterestingScore.CompareTo(query.MinScore) < 0)
                minInterestingScore = query.MinScore;
            MaxScore_LayoutQuery maxScore_layoutQuery = new MaxScore_LayoutQuery();
            maxScore_layoutQuery.MinScore = minInterestingScore;
            SpecificLayout maxScore_subLayout = this.subLayout.GetBestLayout(maxScore_layoutQuery);
            if (maxScore_subLayout == null)
            {
                return null;
            }
            MinHeight_LayoutQuery minHeightAwesomeQuery = new MinHeight_LayoutQuery();
            minHeightAwesomeQuery.MaxWidth = maxScore_subLayout.Width;
            minHeightAwesomeQuery.MaxHeight = maxScore_subLayout.Height;
            minHeightAwesomeQuery.MinScore = maxScore_subLayout.Score;
            SpecificLayout minHeightAwesomeSublayout = this.subLayout.GetBestLayout(minHeightAwesomeQuery);
            LayoutScore maxScore = maxScore_subLayout.Score;
            if (!query.MinimizesWidth() && minHeightAwesomeSublayout.Width <= query.MaxWidth)
            {
                // This layout has as much score per height as possible, so we don't have to check any other layouts
                return this.prepareLayoutForQuery(this.interpolate(minHeightAwesomeSublayout.Size, maxScore, minHeightAwesomeSublayout.Size, maxScore, query), query);
            }

            // Next, find the layout of min height among those having min width among those having max score
            // Next, check for the layout of minimum width having maximum score
            MinWidth_LayoutQuery minWidthAwesomeQuery = new MinWidth_LayoutQuery();
            minWidthAwesomeQuery.MinScore = maxScore_subLayout.Score;
            SpecificLayout minWidthAwesomeSublayoutIntermediate = this.subLayout.GetBestLayout(minWidthAwesomeQuery);
            if (minWidthAwesomeSublayoutIntermediate == null)
            {
                ErrorReporter.ReportParadox("MinWidth query " + minWidthAwesomeQuery + " could not find the response from the MaxScore query for " + this);
            }
            MinHeight_LayoutQuery minWidthMinHeightAwesomeQuery = new MinHeight_LayoutQuery();
            minWidthMinHeightAwesomeQuery.MaxWidth = minWidthAwesomeSublayoutIntermediate.Width;
            minWidthMinHeightAwesomeQuery.MaxHeight = minWidthAwesomeSublayoutIntermediate.Height;
            minWidthMinHeightAwesomeQuery.MinScore = minWidthAwesomeSublayoutIntermediate.Score;
            SpecificLayout minWidthAwesomeSublayout = this.subLayout.GetBestLayout(minWidthMinHeightAwesomeQuery);
            if (minWidthAwesomeSublayout == null)
            {
                ErrorReporter.ReportParadox("MinHeight query " + minHeightAwesomeQuery + " could not find the response from the MinWidth query " + minWidthAwesomeQuery + " for " + this);
            }

            LayoutScore middleScore = minWidthAwesomeSublayout.Score;
            if (middleScore.CompareTo(maxScore) > 0)
                middleScore = maxScore;
            if (minWidthAwesomeSublayout.Width <= query.MaxWidth)
            {
                SpecificLayout result = this.interpolate(minWidthAwesomeSublayout.Size, middleScore, minHeightAwesomeSublayout.Size, maxScore, query);
                if (!query.MinimizesWidth())
                {
                    // The layouts having less width won't have more score per height (we enforce this below)
                    // So, no need to keep looking
                    return this.prepareLayoutForQuery(result, query);
                }
                if (result == null)
                {
                    // Not enough score per height to satisfy the query
                    return this.prepareLayoutForQuery(result, query);
                }
                if (result.Width > query.MaxWidth)
                {
                    // If the MinWidth query didn't want to shrink the width any more, then no need to check other layouts having less width
                    return this.prepareLayoutForQuery(result, query);
                }
            }

            // Now find the min-height sublayout among min-width sublayouts having positive score
            MinWidth_LayoutQuery minWidthPositiveQuery = new MinWidth_LayoutQuery();
            minWidthPositiveQuery.MinScore = minInterestingScore;
            SpecificLayout minWidthPositiveSublayout = this.subLayout.GetBestLayout(minWidthPositiveQuery);
            MinHeight_LayoutQuery minHeightPositiveQuery = new MinHeight_LayoutQuery();
            minHeightPositiveQuery.MaxWidth = minWidthPositiveSublayout.Width;
            minHeightPositiveQuery.MinScore = minInterestingScore;
            SpecificLayout minHeightPositiveSublayout = this.subLayout.GetBestLayout(minHeightPositiveQuery);
            
            LayoutScore minScore = minHeightPositiveSublayout.Score;
            if (minScore.CompareTo(middleScore) > 0)
                minScore = middleScore;

            // Any sublayout having width less than minHeightPositiveSublayout.Width will have score <= 0
            // Normally, because the score of a ScrollLayout is defined as (sublayout.Score / layout.Height),
            //   that means that if the sublayout's score is negative then we should create a ScrollView having infinite height
            // Rather than creating a ScrollView whose contents are of infinite height, normally we skip showing any content having negative score
            // However, we can be configured to treat this score as 0
            if (minHeightPositiveSublayout.Width > query.MaxWidth)
            {
                if (this.treatNegativeScoresAsZero)
                    return this.prepareLayoutForQuery(this.interpolate(new Size(), LayoutScore.Zero, minHeightPositiveSublayout.Size, minScore, query), query);
                return this.zeroOrNull(query);
            }

            return this.prepareLayoutForQuery(this.interpolate(minHeightPositiveSublayout.Size, minScore, minWidthAwesomeSublayout.Size, middleScore, query), query);
        }


        // Given two layouts and a query, does linear interpolation to tell which part of the connecting line the query prefers
        private SpecificLayout interpolate(Size leftSize, LayoutScore leftScore, Size rightSize, LayoutScore rightScore, LayoutQuery query)
        {
            List<Size> options = new List<Size>();

            // Add the two ends
            options.Add(leftSize);
            options.Add(rightSize);

            // Compute the location where X and Y are the limiting factors
            double leftXDifference = query.MaxWidth - leftSize.Width;
            double rightXDifference = query.MaxWidth - rightSize.Width;
            if (leftXDifference.CompareTo(0) != rightXDifference.CompareTo(0))
            {
                options.Add(new Size(query.MaxWidth, query.MaxHeight));
            }

            // Compute the location where X and Score are the limiting factors
            if (query.MaxHeight > 0 && !double.IsInfinity(query.MaxWidth))
            {
                double totalWeight = leftXDifference - rightXDifference;
                double rightFraction;
                if (totalWeight != 0)
                    rightFraction = leftXDifference / totalWeight;
                else
                    rightFraction = 1;
                double leftFraction = 1 - rightFraction;
                double x = query.MaxWidth;
                double interpolatedY = leftSize.Height * leftFraction + rightSize.Height * rightFraction;
                if (interpolatedY != 0)
                {
                    LayoutScore interpolatedScore = leftScore.Times(leftFraction).Plus(rightScore.Times(rightFraction));
                    double y = query.MinScore.DividedBy(interpolatedScore.Times(1.0 / interpolatedY));
                    if (!double.IsInfinity(y) && !double.IsNaN(y))
                    {
                        LayoutScore score = interpolatedScore.Times(y / interpolatedY);
                        if (score.CompareTo(query.MinScore) < 0)
                        {
                            // the score had some components that the division didn't catch
                            y = Math.Floor(y / this.pixelSize + 1.0) * this.pixelSize;
                            score = interpolatedScore.Times(y / interpolatedY);

                            if (score.CompareTo(query.MinScore) < 0)
                            {
                                // the height must have already been so close to an integer that it hardly changed, so the score didn't change at all due to rounding error
                                y += this.pixelSize;
                                score = interpolatedScore.Times(y / interpolatedY);

                                if (score.CompareTo(query.MinScore) < 0)
                                    ErrorReporter.ReportParadox("ScrollLayout rounding error: score " + leftScore + " < query.MinScore " + query.MinScore);
                            }
                        }
                        options.Add(new Size(x, y));
                    }
                }
            }

            // Compute the location where Y and Score are the limiting factors
            if (!double.IsInfinity(query.MaxHeight))
            {
                double y = query.MaxHeight;
                LayoutScore leftRescaledScore = leftScore.Times(y / leftSize.Height);
                LayoutScore rightRescaledScore = rightScore.Times(y / rightSize.Height);
                LayoutScore leftScoreDifference = query.MinScore.Minus(leftRescaledScore);
                LayoutScore rightScoreDifference = query.MinScore.Minus(rightRescaledScore);
                LayoutScore totalWeight = leftScoreDifference.Minus(rightScoreDifference);
                double rightFraction = leftScoreDifference.DividedBy(totalWeight);
                if (double.IsInfinity(rightFraction) || double.IsNaN(rightFraction))
                    rightFraction = 0;
                double leftFraction = 1 - rightFraction;
                LayoutScore score = leftRescaledScore.Times(leftFraction).Plus(rightRescaledScore.Times(rightFraction));
                double x = leftSize.Width * leftFraction + rightSize.Height * rightFraction;
                options.Add(new Size(x, y));
            }


            // choose best dimensions
            LayoutDimensions bestDimensions = null;
            foreach (Size size in options)
            {
                double x = Math.Ceiling(size.Width / this.pixelSize) * this.pixelSize;
                if (x < 0)
                    x = 0;
                double leftDifference = x - leftSize.Width;
                double rightDifference = x - rightSize.Width;
                double totalWeight = leftDifference - rightDifference;
                double rightFraction;
                if (totalWeight != 0)
                    rightFraction = leftDifference / totalWeight;
                else
                    rightFraction = 1;
                double leftFraction = 1 - rightFraction;

                LayoutScore interpolatedScore = leftScore.Times(leftFraction).Plus(rightScore.Times(rightFraction));
                double interpolatedY = leftSize.Height * leftFraction + rightSize.Height * rightFraction;

                double y = Math.Ceiling(size.Height / this.pixelSize) * this.pixelSize;
                if (y > interpolatedY)
                    y = interpolatedY;
                if (y < 0)
                    y = 0;

                LayoutScore score = interpolatedScore.Times(y / interpolatedY);

                LayoutDimensions option = new LayoutDimensions(x, y, score);
                if (query.PreferredLayout(bestDimensions, option) == option)
                    bestDimensions = option;
            }
            if (bestDimensions == null)
                return null;
            SpecificLayout result = this.makeLayout(new Size(bestDimensions.Width, bestDimensions.Height), bestDimensions.Score);
            return result;
        }

        // returns the empty layout if it's accepted
        private SpecificLayout zeroOrNull(LayoutQuery query)
        {
            SpecificLayout empty = this.makeLayout(new Size(), LayoutScore.Zero);
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
        private ScrollView view;
        private ImageLayout imageLayout = new ImageLayout(null, LayoutScore.Get_UsedSpace_LayoutScore(1));
        private double pixelSize;
        private bool treatNegativeScoresAsZero;

    }


    public class Specific_ScrollLayout : Specific_ContainerLayout
    {
        public Specific_ScrollLayout(View view, Size size, LayoutScore score, SpecificLayout sublayout)
            : base(view, size, score, sublayout, new Thickness())
        {
            if (double.IsInfinity(this.SubLayout.Height))
            {
                ErrorReporter.ReportParadox("Infinite Specific_ScrollLayout height: " + this);
            }
        }

        protected override Size chooseSize(Size availableSize)
        {
            Size result = new Size(Math.Max(availableSize.Width, this.SubLayout.Width), this.SubLayout.Height);
            return result;
        }

        public override SpecificLayout Clone()
        {
            return new Specific_ScrollLayout(this.View, this.Size, this.Score, this.SubLayout);
        }

    }

}
