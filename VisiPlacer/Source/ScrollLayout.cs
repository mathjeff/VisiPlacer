using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

// A ScrollLayout contains another layout inside it and enables scrolling.

// The score of a Must_ScrollLayout is defined as a constant times the visible fraction of its sublayout
// A ScrollLayout is just a Must_ScrollLayout unioned with the original sublayout

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
                new PixelatedLayout(
                    new MustScroll_Layout(sublayoutCache, scrollView, pixelSize, treatNegativeScoresAsZero),
                    pixelSize
                )
            );

            // We also allow showing the content without scrolling (and usually prefer that when there's space).
            // However, we still include the ScrollView in that case so that if the content becomes longer,
            // we don't have to change the view structure and introduce new views and potentially reset information like where the cursor is in a contained textbox
            subLayouts.Add(ContainerLayout.SameSize_Scroller(scrollView, subLayout));
            this.Set_LayoutChoices(subLayouts);
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            return base.GetBestLayout(query);
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
            this.requiredChildScore = LayoutScore.Zero;
            this.resultingScore = LayoutScore.Get_UsedSpace_LayoutScore(1);
            this.subLayout = subLayout;
            this.subLayout.AddParent(this);
            this.view = view;
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            // The score of a Specific_ScrollLayout is defined in returnLayout:
            // If the child layout has negative score, the Specific_ScrollLayout refuses to do a layout
            // If the child layout has nonnegative score, the Specific_ScrollLayout's score equals
            //   (this.resultingScore * (what fraction of the child layout is visible))
            if (query.MinScore.CompareTo(this.resultingScore) > 0)
            {
                // Demands too high of a score: no solution
                return null;
            }

            if (query.MaxHeight <= 0)
            {
                return null;
            }

            // what fraction of the score of the sublayout will appear onscreen at once
            double scoreFraction = Math.Max(query.MinScore.DividedBy(this.resultingScore), 0);
            // what fraction of the child's height we need to include in the size of the ScrollView
            double requiredHeightFraction = scoreFraction;
            // the child's height divided by the ScrollView's height
            double requiredHeightMultiplier;
            if (requiredHeightFraction != 0)
                requiredHeightMultiplier = 1 / requiredHeightFraction;
            else
                requiredHeightMultiplier = double.PositiveInfinity;
            // the maximum child height
            double maxChildHeight = query.MaxHeight * requiredHeightMultiplier;

            if (query.MinimizesWidth())
            {
                // For a min-width query, first shrink the width as much as possible before continuing
                SpecificLayout minWidth_childLayout = this.subLayout.GetBestLayout(query.New_MinWidth_LayoutQuery(query.MaxWidth, maxChildHeight, this.requiredChildScore));
                if (minWidth_childLayout == null)
                    return null;
                query = query.WithDimensions(minWidth_childLayout.Width, minWidth_childLayout.Height);
            }

            SpecificLayout childLayout = this.subLayout.GetBestLayout(query.New_MinHeight_LayoutQuery(query.MaxWidth, maxChildHeight, this.requiredChildScore));
            if (childLayout == null)
                return null;

            if (!query.MinimizesHeight())
            {
                // For a max-score (or min-width) query, use as much height as was allowed
                Size size = new Size(childLayout.Width, Math.Min(query.MaxHeight, childLayout.Height));
                SpecificLayout result = this.makeLayout(size, childLayout);
                if (query.Accepts(result))
                    return this.prepareLayoutForQuery(result, query);
                return null;
            }
            else
            {
                // For a min-height query, use only as much size as is needed
                double requiredScrollviewHeight = childLayout.Height * requiredHeightFraction;
                Size size = new Size(childLayout.Width, requiredScrollviewHeight);

                SpecificLayout result = this.makeLayout(size, childLayout);
                if (!query.Accepts(result))
                {
                    // Check for possible rounding error
                    SpecificLayout larger = this.makeLayout(new Size(size.Width, size.Height + this.pixelSize), childLayout);
                    if (query.Accepts(larger))
                        return this.prepareLayoutForQuery(larger, query);
                    return null;
                }
                return this.prepareLayoutForQuery(result, query);
            }
        }


        private SpecificLayout makeLayout(Size size, SpecificLayout childLayout)
        {
            double childHeight = childLayout.Height;
            if (childHeight == 0)
                childHeight = 1;
            LayoutScore score = this.resultingScore.Times(size.Height / childHeight);
            LayoutScore scoreDifference = score.Minus(childLayout.Score);
            SpecificLayout result = new Specific_ScrollLayout(this.view, size, scoreDifference, childLayout);
            return result;
        }

        private LayoutChoice_Set subLayout;
        private ScrollView view;

        private LayoutScore requiredChildScore;
        private LayoutScore resultingScore;
        private double pixelSize;
    }


    public class Specific_ScrollLayout : Specific_ContainerLayout
    {
        public Specific_ScrollLayout(ScrollView view, Size size, LayoutScore score, SpecificLayout sublayout)
            : base(view, size, score, sublayout, new Thickness())
        {
            this.View = view;
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

        public override void Remove_VisualDescendents()
        {
            ScrollView view = this.View;

            if (view != null)
            {
                this.scrollX = view.ScrollX;
                this.scrollY = view.ScrollY;
                view.Content = null;
            }
        }

        public new ScrollView View { get; set; }

        public override void AfterLayoutAttached()
        {
            this.View.ScrollToAsync(this.scrollX, this.scrollY, false);
            //this.View.SetScrolledPosition(this.scrollX, this.scrollY);
        }

        // The scroll coordinates are attached to the Specific_ScrollLayout because if we switch to a different Specific_ScrollLayout (which might use a different font size for its content),
        // then we probably want to reset the scroll position
        public double scrollX = 0;
        public double scrollY = 0;
    }

}
