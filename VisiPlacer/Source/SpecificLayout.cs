using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

// A SpecificLayout is the result of LayoutChoice_Set.GetBestLayout()
// A SpecificLayout tells the computed dimensions of a view, which makes it suitable to cache.
namespace VisiPlacement
{
    public abstract class SpecificLayout : LayoutChoice_Set
    {
        public SpecificLayout()
        {
            //this.ancestors = new List<LayoutChoice_Set>();
        }
        public abstract View View { get; }
        public abstract double Width { get; }
        public abstract double Height { get; }
        public Size Size
        {
            get
            {
                return new Size(this.Width, this.Height);
            }
        }
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
            if (!this.isScoreAtLeast(query))
                return null;
            return this;
        }
        protected virtual bool isScoreAtLeast(LayoutQuery query)
        {
            if (query.MinScore.Equals(LayoutScore.Minimum))
                return true;
            return this.Score.CompareTo(query.MinScore) >= 0;
        }
        public abstract View DoLayout(Size bounds, ViewDefaults layoutDefaults);
        public void CopyFrom(SpecificLayout original)
        {
            base.CopyFrom(original);
            this.SourceQuery = original.SourceQuery;
            //this.ancestors = new List<LayoutChoice_Set>(original.ancestors);
        }
        //public abstract SpecificLayout Clone();
        public LayoutQuery SourceQuery { get; set; }   // the query that generated this SpecificLayout

        public abstract void Remove_VisualDescendents();
        public virtual void Remove_VisualDescendent(View view)
        {
            this.Remove_VisualDescendents();
        }

        // Gets called after the view is part of the main view hieararchy

        public virtual void AfterLayoutAttached() { }
        // returns a list of general LayoutChoice_Sets where the first item created/found this item, and the second item found the first, etc
        // Note that this is different than the specific layout that contains this one as a visual parent
        /*public IEnumerable<LayoutChoice_Set> GetAncestors()
        {
            return this.ancestors;
        }*/
        public IEnumerable<SpecificLayout> GetDescendents()
        {
            List<SpecificLayout> layouts = new List<SpecificLayout>();
            layouts.Add(this);
            foreach (SpecificLayout child in this.GetParticipatingChildren())
            {
                layouts.AddRange(child.GetDescendents());
            }
            return layouts;
        }
        public abstract IEnumerable<SpecificLayout> GetParticipatingChildren();
        // Sets the general layout that created this specific layout
        /*public void Set_SourceParent(LayoutChoice_Set parent)
        {
            this.ancestors.Add(parent);
        }*/
        
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

        public override string ToString()
        {
            return "SpecificLayout: " + this.Dimensions;
        }

        //List<LayoutChoice_Set> ancestors;
    }

}
