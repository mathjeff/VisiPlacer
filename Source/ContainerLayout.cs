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

            // Query sublayout if it exists
            if (this.SubLayout != null)
            {
                return this.SubLayout.GetBestLayout(query);
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
