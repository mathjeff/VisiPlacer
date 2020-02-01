using System.Collections.Generic;
using Xamarin.Forms;

// a LeafLayout is a LayoutChoiceSet that just wishes to be a certain size and has no children
namespace VisiPlacement
{
    public class LeafLayout : LayoutChoice_Set
    {
        public LeafLayout(View view, LayoutDimensions desiredDimensions)
        {
            this.view = view;
            List<LayoutDimensions> dimensionOptions = new List<LayoutDimensions>();
            dimensionOptions.Add(desiredDimensions);
            dimensionOptions.Add(new LayoutDimensions(0, 0, LayoutScore.Get_CutOff_LayoutScore(1)));
            this.dimensionOptions = dimensionOptions;
        }

        public LeafLayout(View view, IEnumerable<LayoutDimensions> dimensionOptions)
        {
            this.view = view;
            this.dimensionOptions = dimensionOptions;
        }

        public override SpecificLayout GetBestLayout(LayoutQuery layoutQuery)
        {
            IEnumerable<LayoutDimensions> options = this.getLayoutOptions(layoutQuery);

            LayoutDimensions dimensions = null;
            foreach (LayoutDimensions candidate in options)
            {
                dimensions = layoutQuery.PreferredLayout(candidate, dimensions);
            }

            Specific_LeafLayout result = null;
            if (dimensions != null)
                result = new Specific_LeafLayout(this.view, dimensions);
            return result;
        }

        private IEnumerable<LayoutDimensions> getLayoutOptions(LayoutQuery query)
        {
            return this.dimensionOptions;
        }

        private IEnumerable<LayoutDimensions> dimensionOptions;
        private View view;
    }

    public class Specific_LeafLayout : SpecificLayout
    {
        public Specific_LeafLayout(View view, LayoutDimensions dimensions)
        {
            this.view = view;
            this.dimensions = dimensions;
        }

        public override double Width
        {
            get
            {
                return this.dimensions.Width;
            }
        }
        public override double Height
        {
            get
            {
                return this.dimensions.Height;
            }
        }
        public override void Remove_VisualDescendents()
        {
        }
        public override IEnumerable<SpecificLayout> GetParticipatingChildren()
        {
            return new List<SpecificLayout>();
        }

        public override SpecificLayout Clone()
        {
            return new Specific_LeafLayout(this.view, this.dimensions);
        }
        public override View View
        {
            get
            {
                return this.view;
            }
        }
        public override LayoutScore Score
        {
            get
            {
                return this.dimensions.Score;
            }
        }
        public override View DoLayout(Size bounds)
        {
            if (this.view != null)
            {
                this.view.WidthRequest = bounds.Width;
                this.view.HeightRequest = bounds.Height;
            }
            return this.view;
        }
        private View view;
        private LayoutDimensions dimensions;
    }

}
