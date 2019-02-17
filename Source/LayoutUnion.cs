using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// a LayoutUnion consists of a bunch of layouts to choose from and always selects the best sub-layout to use
namespace VisiPlacement
{
    public class LayoutUnion : LayoutChoice_Set
    {
        public LayoutUnion()
        {
        }

        public LayoutUnion(IEnumerable<LayoutChoice_Set> layoutOptions)
        {
            this.Set_LayoutChoices(layoutOptions);
        }
        public void Set_LayoutChoices(IEnumerable<LayoutChoice_Set> layoutOptions)
        {
            // remove any existing options
            if (this.layoutOptions != null)
            {
                foreach (LayoutChoice_Set layout in this.layoutOptions)
                {
                    layout.RemoveParent(this);
                }
            }

            // put the new layout options
            this.layoutOptions = new LinkedList<LayoutChoice_Set>();
            if (layoutOptions != null)
            {
                foreach (LayoutChoice_Set layout in layoutOptions)
                {
                    LayoutChoice_Set layoutToAdd = layout;
                    if (layoutOptions.Count() > 1 && !(layout is LayoutCache))
                        layoutToAdd = new LayoutCache(layout);
                    this.layoutOptions.AddLast(layoutToAdd);
                    layoutToAdd.AddParent(this);
                }
            }

            // announce having changed
            this.AnnounceChange(true);
        }
        // for convenience
        public LayoutUnion(LayoutChoice_Set layoutOption1, LayoutChoice_Set layoutOption2)
        {
            LinkedList<LayoutChoice_Set> options = new LinkedList<LayoutChoice_Set>();
            options.AddLast(layoutOption1);
            options.AddLast(layoutOption2);
            this.Set_LayoutChoices(options);
        }
        // for convenience
        public LayoutUnion(LayoutChoice_Set layoutOption1, LayoutChoice_Set layoutOption2, LayoutChoice_Set layoutOption3)
        {
            LinkedList<LayoutChoice_Set> options = new LinkedList<LayoutChoice_Set>();
            options.AddLast(layoutOption1);
            options.AddLast(layoutOption2);
            options.AddLast(layoutOption3);
            this.Set_LayoutChoices(options);
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            SpecificLayout best_specificLayout = null;
            SpecificLayout debugResult = query.ProposedSolution_ForDebugging;
            if (debugResult != null)
                debugResult = debugResult.Clone();
            LinkedList<LayoutChoice_Set> good_sourceLayouts = new LinkedList<LayoutChoice_Set>();
            foreach (LayoutChoice_Set layoutSet in this.layoutOptions)
            {
                SpecificLayout currentLayout;
                if (query.Debug)
                {
                    // if the proposed layout is an option, then be sure to consider it
                    if (debugResult != null && debugResult.GetAncestors().Contains(layoutSet))
                    {
                        query.ProposedSolution_ForDebugging = debugResult;
                        currentLayout = layoutSet.GetBestLayout(query.Clone());
                        query.ProposedSolution_ForDebugging = debugResult;
                        return this.prepareLayoutForQuery(currentLayout, query);
                    }
                }
                currentLayout = layoutSet.GetBestLayout(query.Clone());

                if (currentLayout != null)
                {
                    currentLayout = currentLayout.Clone();
                    // keep track of this query (which must be the best so far)
                    best_specificLayout = currentLayout;
                    good_sourceLayouts.AddLast(layoutSet);

                    if (query.Debug && query.ProposedSolution_ForDebugging != null)
                    {
                        if (query.PreferredLayout(query.ProposedSolution_ForDebugging, best_specificLayout) != query.ProposedSolution_ForDebugging)
                        {
                            ErrorReporter.ReportParadox("Error; query " + query + " prefers " + best_specificLayout + " over proposed debug solution " + query.ProposedSolution_ForDebugging);
                            LayoutQuery debugQuery = query.Clone();
                            debugQuery.Debug = true;
                            layoutSet.GetBestLayout(query.Clone());
                        }
                    }

                    // make the query more strict, so we will only ever get dimensions that are at least as good as this
                    query.OptimizeUsingExample(best_specificLayout);
                }
            }
            query.ProposedSolution_ForDebugging = debugResult;
            return this.prepareLayoutForQuery(best_specificLayout, query);
        }

        private LinkedList<LayoutChoice_Set> layoutOptions;
    }
}
