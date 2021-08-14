using Xamarin.Forms;

namespace VisiPlacement
{
    // a layout that contains one sublayout within it
    public class MustBorderLayout : ContainerLayout
    {
        public MustBorderLayout()
        {
            this.Initialize();
        }
        public MustBorderLayout(ContentView view, LayoutChoice_Set subLayout, Thickness borderThickness)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
        }
        public MustBorderLayout(ContentView view, LayoutChoice_Set subLayout, Thickness borderThickness, bool fillAvailableSpace)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
            this.ChildFillsAvailableSpace = fillAvailableSpace;
        }

        private void Initialize()
        {
            this.ChildFillsAvailableSpace = false;
        }
        public void CopyFrom(MustBorderLayout original)
        {
            this.ChildFillsAvailableSpace = original.ChildFillsAvailableSpace;
            base.CopyFrom(original);
        }
        private View view;
        protected override View View
        {
            get
            {
                if (this.view == null)
                {
                    if (!this.BorderThickness.Equals(new Thickness(0)))
                        this.view = new ContainerView();
                }
                return this.view;
            }
            set
            {
                this.view = value;
            }
        }
        public Thickness BorderThickness { get; set; }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            Specific_ContainerLayout result;

            // Determine whether there's room for the border
            double borderWidth = this.BorderThickness.Left + this.BorderThickness.Right;
            double borderHeight = this.BorderThickness.Top + this.BorderThickness.Bottom;
            LayoutQuery subQuery = query.WithDimensions(query.MaxWidth - borderWidth, query.MaxHeight - borderHeight);
            if (subQuery.MaxWidth < 0 || subQuery.MaxHeight < 0)
            {
                return null;
            }

            // Query sublayout if it exists
            if (this.SubLayout != null)
            {
                SpecificLayout best_subLayout = this.SubLayout.GetBestLayout(subQuery);
                if (best_subLayout != null)
                {
                    result = this.makeSpecificLayout(this.view, new Size(best_subLayout.Width + borderWidth, best_subLayout.Height + borderHeight), LayoutScore.Zero, best_subLayout, this.BorderThickness);
                    result.ChildFillsAvailableSpace = this.ChildFillsAvailableSpace;
                    this.prepareLayoutForQuery(result, query);
                    return result;
                }
                return null;
            }
            // if there is no subLayout, for now we just return an empty size
            Specific_ContainerLayout empty = this.makeSpecificLayout(this.view, new Size(), LayoutScore.Zero, null, new Thickness());
            if (query.Accepts(empty))
                result = empty;
            else
                result = null;
            this.prepareLayoutForQuery(result, query);
            return result;
        }

    }

}
