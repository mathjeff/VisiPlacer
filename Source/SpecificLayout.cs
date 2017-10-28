using System.Collections.Generic;
using Xamarin.Forms;

// Unlike a general LayoutChoice_Set, A SpecificLayout can render reasonably quickly because
// it is the result of having queried any child layouts about what scores they give for certain sizes
namespace VisiPlacement
{
    public abstract class SpecificLayout : LayoutChoice_Set
    {
        public SpecificLayout()
        {
            this.ancestors = new LinkedList<LayoutChoice_Set>();
        }
        public abstract View View { get; }
        public abstract double Width { get; }
        public abstract double Height { get; }
        public abstract LayoutScore Score { get; }
        public LayoutDimensions Dimensions
        {
            get
            {
                LayoutDimensions dimensions = new LayoutDimensions();
                dimensions.Width = this.Width;
                dimensions.Height = this.Height;
                dimensions.Score = this.Score;
                return dimensions;
            }

        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (query.MaxWidth < this.Width)
                return null;
            if (query.MaxHeight < this.Height)
                return null;
            if (query.MinScore.CompareTo(this.Score) > 0)
                return null;
            return this;
        }
        public abstract View DoLayout(Size bounds);
        public void CopyFrom(SpecificLayout original)
        {
            base.CopyFrom(original);
            this.SourceQuery_ForDebugging = original.SourceQuery_ForDebugging;
            this.ancestors = new LinkedList<LayoutChoice_Set>(original.ancestors);
        }
        public abstract SpecificLayout Clone();
        public LayoutQuery SourceQuery_ForDebugging { get; set; }   // the query that generated this SpecificLayout

        // for directly modifying what will show onscreen
        //public abstract void Set_SubviewLocations(IEnumerable<SubviewDimensions> locations);
        public abstract void Remove_VisualDescendents();

        // returns a list of general LayoutChoice_Sets where the first item created/found this item, and the second item found the first, etc
        // Note that this is different than the specific layout that contains this one as a visual parent
        public IEnumerable<LayoutChoice_Set> GetAncestors()
        {
            return this.ancestors;
        }
        public IEnumerable<SpecificLayout> GetDescendents()
        {
            List<SpecificLayout> layouts = new List<SpecificLayout>();
            layouts.Add(this);
            foreach (SpecificLayout child in this.GetChildren())
            {
                layouts.AddRange(child.GetDescendents());
            }
            return layouts;
        }
        public abstract IEnumerable<SpecificLayout> GetChildren();
        // Sets the general layout that created this specific layout
        public void Set_SourceParent(LayoutChoice_Set parent)
        {
            this.ancestors.AddLast(parent);
        }
        
        // the layout that actually contains this particular layout
        //public SpecificLayout VisualParent { get; set; }

        public virtual ViewManager Get_ViewManager()
        {
            View view = this.View;
            while (true)
            {
                ManageableView managedView = view as ManageableView;
                if (managedView != null)
                {
                    return managedView.ViewManager;
                }
                view = view.Parent as ContentView;
                if (view == null)
                    return null;
            }
        }

        LinkedList<LayoutChoice_Set> ancestors;
    }

}
