using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class ViewManager : LayoutChoice_Set
    {
        public event LayoutCompletedHandler LayoutCompleted;
        public delegate void LayoutCompletedHandler(ViewManager_LayoutStats layoutStats);

        // A ViewManager queries its child LayoutChoice_Set and puts the result into its parent View as needed.
        // That is, the ViewManager is what triggers the querying of layouts in the first place.
        public ViewManager(ContentView parentView, LayoutChoice_Set childLayout)
        {
            this.callerHolder.AddParent(this);
            this.SetLayout(childLayout);

            this.mainView = new ManageableView(this);
            this.SetParent(parentView);
        }
        public void SetParent(ContentView parentView)
        {
            if (parentView != null)
            {
                parentView.Content = this.mainView;
                this.displaySize = new Size(parentView.Width, parentView.Height);
            }
            else
            {
                this.displaySize = new Size();
            }
            this.forceRelayout();
        }
        public void SetLayout(LayoutChoice_Set childLayout)
        {
            this.callerHolder.SubLayout = childLayout;
        }

        public bool Debugging
        {
            get
            {
                return this.debugLayout != null;
            }
            set
            {
                bool willDebug = value;
                if (willDebug != this.Debugging)
                {
                    if (willDebug)
                    {
                        this.debugLayout = GridLayout.New(new BoundProperty_List(2), new BoundProperty_List(1), LayoutScore.Zero);
                        this.debugLayout.AddLayout(new LayoutDuration_Layout(this));
                        this.debugLayout.AddLayout(this.callerHolder);
                        this.debugLayout.AddParent(this);
                    }
                    else
                    {
                        this.debugLayout = null;
                    }
                    this.forceRelayout();
                }
            }
        }

        // The layout that we actually use as the root layout
        // If debugging is enabled then we display some extra information
        private LayoutChoice_Set GetSublayout()
        {
            if (this.debugLayout != null)
                return this.debugLayout;
            return this.callerHolder;
        }

        // does a layout of size <size> if anything has changed since the last layout
        public void DoLayoutIfOutOfDate(Size size)
        {
            // if the user asked us to use a different size, use that instead
            if (this.forcedSize.Width > 0)
                size.Width = this.forcedSize.Width;
            if (this.forcedSize.Height > 0)
                size.Height = this.forcedSize.Height;

            // update size, do layout
            if (size.Width != this.displaySize.Width || size.Height != this.displaySize.Height)
                this.needsRelayout = true;
            if (this.needsRelayout)
                this.DoLayout(size);
        }

        // does a layout of size <size>
        public void DoLayout(Size size)
        {
            if (Double.IsInfinity(size.Width) || Double.IsInfinity(size.Height))
                throw new ArgumentException();
            this.displaySize = size;
            this.DoLayout();
        }

        public void forceSize(Size size)
        {
            this.forcedSize = size;
            this.forceRelayout();
        }

        // redoes the layout
        private void DoLayout()
        {
            this.needsRelayout = false;

            // determine which views are currently focused so we can re-focus them after redoing the layout
            List<View> focusedViews = new List<View>();
            if (this.specificLayout != null)
            {
                foreach (SpecificLayout layout in this.specificLayout.GetDescendents())
                {
                    if (layout.View != null && layout.View.IsFocused && layout.GetParticipatingChildren().Count() < 1)
                        focusedViews.Add(layout.View);
                }
            }

            // check some data in preparation for computing stats
            int num_grid_preComputations = GridLayout.NumComputations;
            DateTime startTime = DateTime.Now;

            // record the parent of each view before the relayout, to help us know which parents to disconnect
            Dictionary<View, SpecificLayout> preParents = this.findAncestors(this.specificLayout);

            // recompute the new desired layout
            // generally we expect the overall score to be positive, so we start by hypothesizing
            // that there exists a layout with positive score, and only checking negative-scoring layouts if no positive-scoring layout is found
            LayoutQuery query = new MaxScore_LayoutQuery(this.displaySize.Width, this.displaySize.Height, LayoutScore.Zero);
            DateTime getBestLayout_startDate = DateTime.Now;
            this.specificLayout = this.GetSublayout().GetBestLayout(query);
            if (this.specificLayout == null)
            {
                query = query.WithScore(LayoutScore.Minimum);
                this.specificLayout = this.GetSublayout().GetBestLayout(query);
            }

            DateTime getBestLayout_endDate = DateTime.Now;

            // find the parent of each view after the relayout, to help us know which parents to disconnect
            Dictionary<View, SpecificLayout> postParents = this.findAncestors(this.specificLayout);

            // disconnect any parents that are no longer the same
            foreach (View view in preParents.Keys)
            {
                SpecificLayout preLayout = preParents[view];
                if (preLayout != null)
                {
                    SpecificLayout postLayout = this.DictionaryGet(postParents, view);
                    if (postLayout == null || preLayout.View != postLayout.View)
                    {
                        // The parent of <view> has changed.
                        // Disconnect it from the previous parent.
                        preLayout.Remove_VisualDescendent(view);
                    }
                }
            }

            // record that our layout is up-to-date (so any future updates will trigger a relayout)
            this.Reset_ChangeAnnouncement();

            // update our actual view
            this.mainView.Content = this.specificLayout.DoLayout(displaySize);

            // Inform each layout whose view was reattached, in case they need to restore any state that can only be restored after being reattached (most likely because the view system would overwrite it)
            foreach (SpecificLayout layout in postParents.Values)
            {
                layout.AfterLayoutAttached();
            }

            // display stats
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            System.Diagnostics.Debug.WriteLine("ViewManager DoLayout finished in " + duration + " (" + query.Cost + ") queries");
            System.Diagnostics.Debug.WriteLine("Text formatting time = " + TextLayout.TextTime + " for " + TextLayout.NumMeasures + " measures");
            int num_grid_postComputations = GridLayout.NumComputations;
            System.Diagnostics.Debug.WriteLine("Num grid computations = " + (num_grid_postComputations - num_grid_preComputations));
            TextLayout.NumMeasures = 0;
            TextLayout.TextTime = new TimeSpan();

            // refocus the previously focused views
            foreach (View view in focusedViews)
            {
                if (postParents.ContainsKey(view))
                    view.Focus();
            }

            System.Diagnostics.Debug.WriteLine("ViewManager completed layout at " + DateTime.Now);

            if (this.LayoutCompleted != null)
            {
                ViewManager_LayoutStats stats = new ViewManager_LayoutStats();
                stats.ViewManager_LayoutDuration = duration;
                stats.ViewManager_getBestLayout_Duration = getBestLayout_endDate.Subtract(getBestLayout_startDate);
                this.LayoutCompleted.Invoke(stats);
            }
        }

        private SpecificLayout DictionaryGet(Dictionary<View, SpecificLayout> dictionary, View value)
        {
            if (dictionary.ContainsKey(value))
                return dictionary[value];
            return null;
        }

        // returns a Dictionary that maps each view in the tree to its closest containing ancestor layout
        private Dictionary<View, SpecificLayout> findAncestors(SpecificLayout layout)
        {
            Dictionary<View, SpecificLayout> parents = new Dictionary<View, SpecificLayout>();
            if (layout != null)
                this.addAllParents(layout.GetParticipatingChildren(), layout, parents);
            return parents;
        }
        private void addAllParents(IEnumerable<SpecificLayout> candidates, SpecificLayout parentView_layout, Dictionary<View, SpecificLayout> accumulator)
        {
            foreach (SpecificLayout childLayout in candidates)
            {
                if (childLayout.View != null)
                {
                    accumulator[childLayout.View] = parentView_layout;
                    this.addAllParents(childLayout.GetParticipatingChildren(), childLayout, accumulator);
                }
                else
                {
                    this.addAllParents(childLayout.GetParticipatingChildren(), parentView_layout, accumulator);
                }
            }
        }

        public override void On_ContentsChanged(bool mustRedraw)
        {
            if (mustRedraw)
            {
                this.forceRelayout();
            }
        }

        private void forceRelayout()
        {
            // tell the framework to reinvoke the layout
            // for some reason, this.parentView.ForceLayout doesn't work
            this.even = !this.even;
            if (this.even)
            {
                this.mainView.VerticalOptions = LayoutOptions.CenterAndExpand;
            }
            else
            {
                this.mainView.VerticalOptions = LayoutOptions.Center;
            }

            // update our own note that layout is needed
            this.needsRelayout = true;

        }

        // tests that the layout satisfies all of the queries consistently
        public void DebugCheck(LayoutChoice_Set layout)
        {
            int i, j;
            int maxWidth, maxHeight;
            maxWidth = 127;
            maxHeight = 127;
            LayoutDimensions[,] maxScore_dimensions = new LayoutDimensions[maxWidth, maxHeight];
            LayoutDimensions[,] minWidth_dimensions = new LayoutDimensions[maxWidth, maxHeight];
            LayoutDimensions[,] minHeight_dimensions = new LayoutDimensions[maxWidth, maxHeight];
            for (i = 0; i < maxWidth; i++)
            {
                System.Diagnostics.Debug.WriteLine(i.ToString() + " of " + maxWidth.ToString());
                for (j = 0; j < maxHeight; j++)
                {
                    int width = i + 29;
                    int height = j;

                    // find the maximum score of all layouts that fit in these dimensions
                    LayoutQuery maxScoreQuery = new MaxScore_LayoutQuery(width, height, LayoutScore.Minimum);
                    SpecificLayout maxScore_layout = layout.GetBestLayout(maxScoreQuery);
                    maxScore_dimensions[i, j] = maxScore_layout.Dimensions;

                    
                    // find the layout of minimum width having at least this score
                    LayoutQuery minWidthQuery = new MinWidth_LayoutQuery();
                    minWidthQuery = new MinWidth_LayoutQuery(width, height, maxScore_layout.Score);
                    SpecificLayout minWidth_layout = layout.GetBestLayout(minWidthQuery);
                    if (minWidth_layout != null)
                        minWidth_dimensions[i, j] = minWidth_layout.Dimensions;

                    // find the layout of minimum height having at least this score
                    LayoutQuery minHeightQuery = new MinHeight_LayoutQuery(width, height, maxScore_layout.Score);
                    SpecificLayout minHeight_layout = layout.GetBestLayout(minHeightQuery);
                    if (minHeight_layout != null)
                        minHeight_dimensions[i, j] = minHeight_layout.Dimensions;
                    if (i > 0)
                    {
                        if (maxScore_dimensions[i, j].Score.CompareTo(maxScore_dimensions[i - 1, j].Score) < 0)
                            System.Diagnostics.Debug.WriteLine("Error: inconsistency between (" + i.ToString() + ", " + j.ToString() + ") and (" + (i - 1).ToString() + ", " + j.ToString() +")");
                    }
                    if (j > 0)
                    {
                        if (maxScore_dimensions[i, j].Score.CompareTo(maxScore_dimensions[i, j - 1].Score) < 0)
                            System.Diagnostics.Debug.WriteLine("Error: inconsistency between (" + i.ToString() + ", " + j.ToString() + ") and (" + i.ToString() + ", " + (j - 1).ToString() + ")");
                    }
                    if ((width == 0 || height == 0) && maxScore_dimensions[i, j].Score.CompareTo(LayoutScore.Zero) > 0)
                        System.Diagnostics.Debug.WriteLine("Error: clipping not noticed at (" + i.ToString() + ", " + j.ToString() + ")");
                    if (minWidth_dimensions[i, j] == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Error: minWidth query for (" + i.ToString() + ", " + j.ToString() + ") returned null");
                        minWidthQuery.Debug = true;
                        layout.GetBestLayout(minWidthQuery.Clone());
                    }
                    if (minHeight_dimensions[i, j] == null)
                        System.Diagnostics.Debug.WriteLine("Error: minHeight query for (" + i.ToString() + ", " + j.ToString() + ") returned null");
                    if (i > 0 && minWidth_dimensions[i, j] != null && minWidth_dimensions[i, j] != null)
                    {
                        if (minWidth_dimensions[i, j].Score.CompareTo(minWidth_dimensions[i - 1, j].Score) == 0)
                        {
                            if (minWidth_dimensions[i, j].Width != minWidth_dimensions[i - 1, j].Width)
                                System.Diagnostics.Debug.WriteLine("Error: width is wrong in minWidth query between (" + i.ToString() + ", " + j.ToString() + ") and (" + (i - 1).ToString() + ", " + j.ToString() + ")");
                        }
                    }
                    if (j > 0 && minHeight_dimensions[i, j] != null && minHeight_dimensions[i, j] != null)
                    {
                        if (minHeight_dimensions[i, j].Score.CompareTo(minHeight_dimensions[i, j - 1].Score) == 0)
                        {
                            if (minHeight_dimensions[i, j].Height != minHeight_dimensions[i, j - 1].Height)
                            {
                                System.Diagnostics.Debug.WriteLine("Error: height is wrong in minHeight query between (" + i.ToString() + ", " + j.ToString() + ") and (" + i.ToString() + ", " + (j - 1).ToString() + ")");
                                minHeightQuery.Debug = true;
                                layout.GetBestLayout(minHeightQuery.Clone());
                            }
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("done with debugCheck");
            /*
            System.Diagnostics.Debug.WriteLine("checking minWidth queries");
            for (i = 1; i < maxWidth; i++)
            {
                for (j = 1; j < maxHeight; j++)
                {
                    if (maxScore_dimensions[i, j].Score.CompareTo(maxScore_dimensions[i - 1, j] < 0))
                        System.Diagnostics.Debug.WriteLine("Error: inconsistency between (" + i.ToString() + ", " + j.ToString() + ") and (" + (i - 1).ToString() + ", " + j.ToString());
                    if (maxScore_dimensions[i, j].Score.CompareTo(maxScore_dimensions[i, j - 1] < 0))
                        System.Diagnostics.Debug.WriteLine("Error: inconsistency between (" + i.ToString() + ", " + j.ToString() + ") and (" + i.ToString() + ", " + (j - 1).ToString());
                }
            }*/
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            return null;    // not relevant
        }

        private ContentView mainView;
        // the layout that we put the caller's layout into
        private ContainerLayout callerHolder = new ContainerLayout();
        private Size displaySize;
        private Size forcedSize;
        private SpecificLayout specificLayout;
        private bool even;
        private bool needsRelayout = true;
        private GridLayout debugLayout;
    }

    // The ManageableView listens for changes in its dimensions and informs its ViewManager
    public class ManageableView : ContentView
    {
        public ManageableView(ViewManager viewManager)
        {
            this.ViewManager = viewManager;
        }

        public ViewManager ViewManager { get; set; }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width > 0 && height > 0)
            {
                Size bounds = new Size(width, height);
                this.ViewManager.DoLayoutIfOutOfDate(bounds);
            }
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            Size bounds = new Size(widthConstraint, heightConstraint);
            this.ViewManager.DoLayoutIfOutOfDate(bounds);

            return new SizeRequest(bounds);
        }
    }

    public class ViewManager_LayoutStats
    {
        // the time spent in ViewManager.DoLayout
        public TimeSpan ViewManager_LayoutDuration;
        // the time spent in ViewManager.specificLayout.getBestLayout
        public TimeSpan ViewManager_getBestLayout_Duration;
    }
}
