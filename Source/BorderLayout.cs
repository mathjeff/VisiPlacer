using Xamarin.Forms;

namespace VisiPlacement
{
    // a layout that contains one sublayout within it
    public class BorderLayout : ContainerLayout
    {
        public BorderLayout()
        {
            this.Initialize();
        }
        public BorderLayout(ContentView view, LayoutChoice_Set subLayout, Thickness borderThickness)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
        }
        public BorderLayout(ContentView view, LayoutChoice_Set subLayout, Thickness borderThickness, bool fillAvailableSpace)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
            this.ChildFillsAvailableSpace = fillAvailableSpace;
        }

        private void Initialize()
        {
            this.ChildFillsAvailableSpace = true;
        }
        public void CopyFrom(BorderLayout original)
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
            LayoutQuery subQuery = query.Clone();
            subQuery.MaxWidth = subQuery.MaxWidth - borderWidth;
            subQuery.MaxHeight = subQuery.MaxHeight - borderHeight;
            if (subQuery.MaxWidth < 0 || subQuery.MaxHeight < 0)
            {
                // If there is no room for the border, then even the border would be cropped
                result = this.makeSpecificLayout(this.view, new Size(0, 0), LayoutScore.Get_CutOff_LayoutScore(1), null, new Thickness(0));
                if (query.Accepts(result))
                    return this.prepareLayoutForQuery(result, query);
                return null;
            }


            // Query sublayout if it exists
            if (this.SubLayout != null)
            {
                SpecificLayout best_subLayout = this.SubLayout.GetBestLayout(subQuery);
                if (best_subLayout != null)
                {
                    result = this.makeSpecificLayout(this.view, new Size(best_subLayout.Width + borderWidth, best_subLayout.Height + borderHeight), best_subLayout.Score.Plus(this.BonusScore), best_subLayout, this.BorderThickness);
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
