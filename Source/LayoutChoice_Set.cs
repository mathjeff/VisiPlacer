using System;
using System.Collections.Generic;

// A LayoutChoice_Set describes different layouts for a view's child views.
// The LayoutChoice_Set can be asked to choose the best layout (such as the highest-scoring layout) fitting inside a certain size.
// The base LayoutChoice_Set is the generic version that potentially must do intensive queries to child layouts before answering.
// The SpecificLayout is the precomputed version that is the result of these calucations and knows where to put the child views.
namespace VisiPlacement
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    public abstract class LayoutChoice_Set
    {
        static int nextDebugId = 0;

        public LayoutChoice_Set()
        {
            this.changedSinceLatestQuery = true;
            this.changedSinceLastRender = true;
            this.parents = new HashSet<LayoutChoice_Set>();
            this.children = new HashSet<LayoutChoice_Set>();

            this.DebugId = nextDebugId;
            nextDebugId++;
        }
        public virtual void CopyFrom(LayoutChoice_Set original)
        {
            this.changedSinceLatestQuery = original.changedSinceLatestQuery;
            this.changedSinceLastRender = original.changedSinceLastRender;
            this.parents = new HashSet<LayoutChoice_Set>();
            foreach (LayoutChoice_Set parent in original.parents)
            {
                this.AddParent(parent);
            }
            this.children = new HashSet<LayoutChoice_Set>();
            foreach (LayoutChoice_Set child in original.children)
            {
                child.AddParent(this);
            }
        }

        // asks for the best dimensions that this set allows
        public abstract SpecificLayout GetBestLayout(LayoutQuery query);


        // Given a SpecificLayout, sets any necessary properties to make it suitable to return to the caller of GetBestLayout(LayoutQuery)
        protected SpecificLayout prepareLayoutForQuery(SpecificLayout layout, LayoutQuery query)
        {
            if (layout != null)
            {
                if (layout.Width < 0 || layout.Height < 0)
                {
                    ErrorReporter.ReportParadox("Illegal layout size: " + layout.Size);
                    this.GetBestLayout(query);
                }
            }
            int numMatches;
            //if (query.Debug)
            {
                if (query.ProposedSolution_ForDebugging != null)
                {
                    if (!query.Accepts(query.ProposedSolution_ForDebugging))
                    {
                        ErrorReporter.ReportParadox("Error: the proposed solution was not valid");
                    }
                }
                if (layout != null && !query.Accepts(layout))
                {
                    ErrorReporter.ReportParadox("Error: the returned layout was not valid");
                    LayoutQuery query2 = query.DebugClone();
                    this.GetBestLayout(query2);
                }
            }

            if (layout != null)
            {
                layout.Set_SourceParent(this);

                numMatches = 0;
                foreach (LayoutChoice_Set ancestor in layout.GetAncestors())
                {
                    if (ancestor == this)
                        numMatches++;
                }
                if (numMatches == 0)
                    ErrorReporter.ReportParadox("Error: the returned layout did not come from this layout");
                if (numMatches > 1)
                    ErrorReporter.ReportParadox("Error: the returned layout contained multiple ancestors matching this one");

                layout.SourceQuery_ForDebugging = query;
            }

            if (this.parents.Count < 1 && !(this is ViewManager))
            {
                throw new InvalidOperationException("No parents assigned to " + this);
            }

            query.OnAnswered(this);

            return layout;
        }

        public void AddParent(LayoutChoice_Set parent)
        {
            this.parents.Add(parent);
            parent.children.Add(this);
        }
        public void RemoveParent(LayoutChoice_Set parent)
        {
            this.parents.Remove(parent);
            parent.children.Remove(this);
        }

        // Call this when the layout changes and can no longer be cached
        public virtual void AnnounceChange(bool mustRedraw)
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
            if (this.changedSinceLatestQuery || this.changedSinceLatestQuery)
            {
                this.changedSinceLatestQuery = false;
                this.changedSinceLastRender = false;
                foreach (LayoutChoice_Set child in this.children)
                {
                    child.Reset_ChangeAnnouncement();
                }
            }
        }

        // override this in any layout that caches anything
        public virtual void On_ContentsChanged(bool mustRedraw)
        {
        }

        public int DebugId;

        protected bool Get_ChangedSinceLastRender()
        {
            return this.changedSinceLastRender;
        }

        private bool changedSinceLatestQuery; // for caching
        private bool changedSinceLastRender; // for caching

        private HashSet<LayoutChoice_Set> parents;
        private HashSet<LayoutChoice_Set> children;
    }
}
