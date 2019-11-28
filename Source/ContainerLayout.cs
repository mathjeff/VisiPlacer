using Xamarin.Forms;

namespace VisiPlacement
{
    // a layout that contains one sublayout within it
    public class ContainerLayout : LayoutChoice_Set
    {
        public ContainerLayout()
        {
            this.Initialize();
            this.BonusScore = LayoutScore.Zero;
        }
        public ContainerLayout(ContentView view, LayoutChoice_Set subLayout, Thickness borderThickness, LayoutScore bonusScore)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
            this.BonusScore = bonusScore;
        }
        public ContainerLayout(ContentView view, LayoutChoice_Set subLayout, Thickness borderThickness, LayoutScore bonusScore, bool fillAvailableSpace)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
            this.BonusScore = bonusScore;
            this.ChildFillsAvailableSpace = fillAvailableSpace;
        }
        // Returns a ContainerLayout that wraps a ScrollView whose content size will always match the ScrollView's size.
        // The reason someone might want this is if something may later temporarily show overtop of this view (like a keyboard) then
        // the ScrollView might scroll its content to still be visible.
        public static ContainerLayout SameSize_Scroller(ScrollView view, LayoutChoice_Set subLayout)
        {
            ContainerLayout containerLayout = new ContainerLayout();
            containerLayout.view = view;
            containerLayout.SubLayout = subLayout;
            return containerLayout;
        }

        private void Initialize()
        {
            this.ChildFillsAvailableSpace = true;
        }
        public void CopyFrom(ContainerLayout original)
        {
            this.ChildFillsAvailableSpace = original.ChildFillsAvailableSpace;
            base.CopyFrom(original);
        }
        private View view;
        private View View
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
        public bool ChildFillsAvailableSpace { get; set; }
        public LayoutChoice_Set SubLayout 
        {
            get
            {
                return this.subLayout;
            }
            set
            {
                if (value == this.subLayout)
                    return;
                if (this.subLayout != null)
                    this.subLayout.RemoveParent(this);
                this.subLayout = value;
                if (this.subLayout != null)
                    this.subLayout.AddParent(this);
                this.AnnounceChange(true);
            }
        }
        public LayoutScore BonusScore { get; set; }

        // TODO: split ContainerLayout into more classes having different handling of BorderThickness: one that crops with a small penalty, and one that fails if cropped
        public Thickness BorderThickness { get; set; }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            Specific_ContainerLayout result;

            // Determine whether there's room for the border
            LayoutQuery subQuery = query.Clone();
            double borderWidth = this.BorderThickness.Left + this.BorderThickness.Right;
            double borderHeight = this.BorderThickness.Top + this.BorderThickness.Bottom;
            subQuery.MaxWidth = subQuery.MaxWidth - borderWidth;
            subQuery.MaxHeight = subQuery.MaxHeight - borderHeight;
            if (subQuery.MaxWidth < 0 || subQuery.MaxHeight < 0)
            {
                // If there is no room for the border, then that violates a requirement and we return the worst possible score
                result = this.makeSpecificLayout(this.view, new Size(0, 0), LayoutScore.Minimum, null, new Thickness(0));
                if (query.Accepts(result))
                    return this.prepareLayoutForQuery(result, query);
                return null;
            }


            // Query sublayout if it exists
            if (this.SubLayout != null)
            {
                subQuery.MinScore = subQuery.MinScore.Minus(this.BonusScore);
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

        protected Specific_ContainerLayout makeSpecificLayout(View view, Size size, LayoutScore score, SpecificLayout subLayout, Thickness border)
        {
            return new Specific_ContainerLayout(view, size, score, subLayout, border);
        }

        LayoutChoice_Set subLayout;
    }

}
