﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// a LayoutUnion consists of a bunch of layouts to choose from and always selects the best sub-layout to use
// Layout options later in the list will win ties against layout options earlier in the list
namespace VisiPlacement
{
    public class LayoutUnion : LayoutChoice_Set
    {
        public static LayoutChoice_Set New(IEnumerable<LayoutChoice_Set> layoutOptions)
        {
            if (layoutOptions.Count() == 1)
                return layoutOptions.First();
            return new LayoutUnion(layoutOptions);
        }


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
            this.layoutOptions = new List<LayoutChoice_Set>();
            if (layoutOptions != null)
            {
                foreach (LayoutChoice_Set layout in layoutOptions)
                {
                    LayoutChoice_Set layoutToAdd = layout;
                    if (layoutOptions.Count() > 1)
                        layoutToAdd = LayoutCache.For(layout);
                    this.layoutOptions.Add(layoutToAdd);
                    layoutToAdd.AddParent(this);
                }
            }

            // announce having changed
            this.AnnounceChange(true);
        }
        // for convenience
        public LayoutUnion(LayoutChoice_Set layoutOption1, LayoutChoice_Set layoutOption2)
        {
            List<LayoutChoice_Set> options = new List<LayoutChoice_Set>();
            options.Add(layoutOption1);
            options.Add(layoutOption2);
            this.Set_LayoutChoices(options);
        }
        // for convenience
        public LayoutUnion(LayoutChoice_Set layoutOption1, LayoutChoice_Set layoutOption2, LayoutChoice_Set layoutOption3)
        {
            List<LayoutChoice_Set> options = new List<LayoutChoice_Set>();
            options.Add(layoutOption1);
            options.Add(layoutOption2);
            options.Add(layoutOption3);
            this.Set_LayoutChoices(options);
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            SpecificLayout best_specificLayout = null;
            SpecificLayout debugResult = query.ProposedSolution_ForDebugging;
            if (debugResult != null)
                debugResult = debugResult.Clone();
            List<LayoutChoice_Set> good_sourceLayouts = new List<LayoutChoice_Set>();
            LayoutQuery originalQuery = query;
            foreach (LayoutChoice_Set layoutSet in this.layoutOptions)
            {
                if (best_specificLayout != null)
                {
                    // make the query more strict, so we will only ever get dimensions that are at least as good as this
                    // TODO: figure out why it's not better to use OptimizedPastExample
                    query = query.OptimizedUsingExample(best_specificLayout);
                }
                SpecificLayout currentLayout;
                if (query.Debug)
                {
                    // if the proposed layout is an option, then be sure to consider it
                    if (debugResult != null && debugResult.GetAncestors().Contains(layoutSet))
                    {
                        query.ProposedSolution_ForDebugging = debugResult;
                        currentLayout = layoutSet.GetBestLayout(query);
                        query.ProposedSolution_ForDebugging = debugResult;
                        return this.prepareLayoutForQuery(currentLayout, query);
                    }
                }
                currentLayout = layoutSet.GetBestLayout(query);

                if (currentLayout != null && query.PreferredLayout(currentLayout, best_specificLayout) == currentLayout)
                {
                    // keep track of this query (which must be the best so far)
                    best_specificLayout = currentLayout;
                    good_sourceLayouts.Add(layoutSet);

                    if (query.Debug && query.ProposedSolution_ForDebugging != null)
                    {
                        if (query.PreferredLayout(query.ProposedSolution_ForDebugging, best_specificLayout) != query.ProposedSolution_ForDebugging)
                        {
                            ErrorReporter.ReportParadox("Error; query " + query + " prefers " + best_specificLayout + " over proposed debug solution " + query.ProposedSolution_ForDebugging);
                            LayoutQuery debugQuery = query.DebugClone();
                            layoutSet.GetBestLayout(debugQuery);
                        }
                    }
                }
            }
            originalQuery.ProposedSolution_ForDebugging = debugResult;
            return this.prepareLayoutForQuery(best_specificLayout, originalQuery);
        }

        private List<LayoutChoice_Set> layoutOptions;
    }
}
