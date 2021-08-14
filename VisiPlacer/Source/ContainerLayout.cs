using Xamarin.Forms;

namespace VisiPlacement
{
    // a layout that contains one sublayout within it
    public class ContainerLayout : LayoutChoice_Set
    {
        public ContainerLayout()
        {
            this.Initialize();
        }
        public ContainerLayout(ContentView view, LayoutChoice_Set subLayout)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
        }
        public ContainerLayout(ContentView view, LayoutChoice_Set subLayout, bool fillAvailableSpace)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
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
        protected virtual View View
        {
            get
            {
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

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            Specific_ContainerLayout result;

            SpecificLayout sublayoutResult;
            if (this.SubLayout != null)
            {
                // We have a sublayout and haven't been asked to wrap it in another view, so we can just forward the query on to it
                sublayoutResult = this.SubLayout.GetBestLayout(query);
                if (this.view == null && this.ChildFillsAvailableSpace)
                {
                    // If we haven't been asked to wrap the sublayout's result, we can just directly use it
                    return sublayoutResult;
                }
                else
                {
                    if (sublayoutResult == null)
                        return null;
                    result = this.makeSpecificLayout(this.View, sublayoutResult.Size, LayoutScore.Zero, sublayoutResult, new Thickness());
                    this.prepareLayoutForQuery(result, query);
                    return result;
                }
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

        protected virtual Specific_ContainerLayout makeSpecificLayout(View view, Size size, LayoutScore bonusScore, SpecificLayout subLayout, Thickness border)
        {
            Specific_ContainerLayout result = new Specific_ContainerLayout(view, size, bonusScore, subLayout, border);
            result.ChildFillsAvailableSpace = this.ChildFillsAvailableSpace;
            return result;
        }

        LayoutChoice_Set subLayout;
    }

}
