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
            subLayouts.Add(new MustScroll_Layout(subLayout));
            this.Set_LayoutChoices(subLayouts);
        }
    }

    // a MustScroll_Layout will always put its content into a ScrollView
    public class MustScroll_Layout : LayoutChoice_Set
    {
        public MustScroll_Layout(LayoutChoice_Set subLayout)
        {
            if (subLayout is LayoutCache)
                this.subLayout = subLayout;
            else
                this.subLayout = new LayoutCache(subLayout);
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
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
        private LayoutChoice_Set subLayout;
        private ScrollView view = new ScrollView();
        private ImageLayout imageLayout = new ImageLayout(null, LayoutScore.Get_UsedSpace_LayoutScore(1));

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
