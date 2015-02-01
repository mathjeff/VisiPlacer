using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

// A LayoutChoice_Set describes different layouts for an IView's child IViews
// The LayoutChoice_Set can be asked to choose the best layout (such as the highest-scoring layout) fitting inside a certain size
namespace VisiPlacement
{
    public abstract class LayoutChoice_Set
    {
        public LayoutChoice_Set()
        {
            this.changedSinceLatestQuery = true;
            this.changedSinceLastRender = true;
            this.parents = new HashSet<LayoutChoice_Set>();
        }
        public virtual void CopyFrom(LayoutChoice_Set original)
        {
            this.changedSinceLatestQuery = original.changedSinceLatestQuery;
            this.changedSinceLastRender = original.changedSinceLastRender;
            this.parents = new HashSet<LayoutChoice_Set>(original.parents);
        }

        // asks for the best dimensions that this set allows
        public abstract SpecificLayout GetBestLayout(LayoutQuery query);


        // declares that the given size is what has been chosen (note that the choice is computed based on space constraints including other outside views)
        public void SetSize(Rect size)
        {
        }


        // Given a SpecificLayout, sets any necessary properties to make it suitable to return to the caller of GetBestLayout(LayoutQuery)
        protected SpecificLayout prepareLayoutForQuery(SpecificLayout layout, LayoutQuery query)
        {
            this.changedSinceLatestQuery = false;
            this.changedSinceLastRender = false;

            int numMatches;
            if (query.Debug)
            {
                if (query.ProposedSolution_ForDebugging != null)
                {
                    if (!query.Accepts(query.ProposedSolution_ForDebugging))
                    {
                        System.Diagnostics.Debug.WriteLine("Error: the proposed solution was not valid");
                    }
                }
            }

            if (layout != null)
            {
                layout.SetParent(this);

                numMatches = 0;
                foreach (LayoutChoice_Set ancestor in layout.GetAncestors())
                {
                    if (ancestor == this)
                        numMatches++;
                }
                if (numMatches == 0)
                    System.Diagnostics.Debug.WriteLine("Error: the returned layout did not come from this layout");
                if (numMatches > 1)
                    System.Diagnostics.Debug.WriteLine("Error: the returned layout contained multiple ancestors matching this one");

                layout.SourceQuery_ForDebugging = query.Clone();
            }

            return layout;
        }

        public void AddParent(LayoutChoice_Set parent)
        {
            this.parents.Add(parent);
        }
        public void RemoveParent(LayoutChoice_Set parent)
        {
            this.parents.Remove(parent);
        }

        // Call this when the layout changes and can no longer be cached
        public void AnnounceChange(bool mustRedraw)
        {
            if (this.changedSinceLatestQuery && !mustRedraw)
                return;
            if (this.changedSinceLastRender)
                return;
            this.changedSinceLatestQuery = true;
            this.changedSinceLastRender = mustRedraw;
            this.On_ContentsChanged(mustRedraw);
            foreach (LayoutChoice_Set parent in this.parents)
            {
                parent.AnnounceChange(mustRedraw);
            }
        }

        public void Reset_ChangeAnnouncement()
        {
            this.changedSinceLatestQuery = false;
            this.changedSinceLastRender = false;
        }

        // override this in any layout that caches anything
        public virtual void On_ContentsChanged(bool mustRedraw)
        {
        }

        protected bool Get_ChangedSinceLastRender()
        {
            return this.changedSinceLastRender;
        }

        private bool changedSinceLatestQuery; // for caching
        private bool changedSinceLastRender; // for caching

        //public LayoutChoice_Set Parent { get; set; }
        //private List<LayoutChoice_Set> ancestors;
        private HashSet<LayoutChoice_Set> parents;
    }
}
