using System;
using System.Collections.Generic;
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
            this.childLayout = childLayout;
            childLayout.AddParent(this);

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
            if (this.childLayout != null)
                this.childLayout.RemoveParent(this);
            this.childLayout = childLayout;
            if (this.childLayout != null)
            {
                childLayout.AddParent(this);
                this.DoLayout();
            }
        }

        public void Remove_VisualDescendents()
        {
            if (this.specificLayout != null)
                this.specificLayout.Remove_VisualDescendents();
        }

        // does a layout of size <size> if anything has changed since the last layout
        public void DoLayoutIfOutOfDate(Size size)
        {
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
                    if (layout.View != null && layout.View.IsFocused)
                        focusedViews.Add(layout.View);
                }
            }

            // check some data in preparation for computing stats
            int num_grid_preComputations = GridLayout.NumComputations;
            DateTime startTime = DateTime.Now;

            // record the parent of each view before the relayout, to help us know which parents to disconnect
            Dictionary<View, SpecificLayout> preParents = this.findAncestors(this.specificLayout);

            // recompute the new desired layout
            LayoutQuery query = new MaxScore_LayoutQuery();
            query.MaxWidth = this.displaySize.Width;
            query.MaxHeight = this.displaySize.Height;
            DateTime getBestLayout_startDate = DateTime.Now;
            this.specificLayout = this.childLayout.GetBestLayout(query);
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
                        // Disconnect the previous parent's children.
                        // TODO: only disconnect this specific child (<view>).
                        preLayout.Remove_VisualDescendents();
                    }
                }
            }

            // record that our layout is up-to-date (so any future updates will trigger a relayout)
            this.Reset_ChangeAnnouncement();

            // update our actual view
            this.mainView.Content = this.specificLayout.DoLayout(displaySize);

            // display stats
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            System.Diagnostics.Debug.WriteLine("ViewManager DoLayout finished in " + duration);
            System.Diagnostics.Debug.WriteLine("Text formatting time = " + TextLayout.TextTime + " for " + TextLayout.NumMeasures + " measures");
            int num_grid_postComputations = GridLayout.NumComputations;
            System.Diagnostics.Debug.WriteLine("Num grid computations = " + (num_grid_postComputations - num_grid_preComputations));
            TextLayout.NumMeasures = 0;
            TextLayout.TextTime = new TimeSpan();

            // refocus the previously focused views
            foreach (View view in focusedViews)
                view.Focus();

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

        // returns a Dictionary that maps each view in the tree to its closest containing ancestor
        private Dictionary<View, SpecificLayout> findAncestors(SpecificLayout layout)
        {
            Dictionary<View, SpecificLayout> parents = new Dictionary<View, SpecificLayout>();
            if (layout != null)
                this.addAllParents(layout.GetChildren(), layout, parents);
            return parents;
        }
        private void addAllParents(IEnumerable<SpecificLayout> candidates, SpecificLayout parentView_layout, Dictionary<View, SpecificLayout> accumulator)
        {
            foreach (SpecificLayout childLayout in candidates)
            {
                if (childLayout.View != null)
                {
                    accumulator[childLayout.View] = parentView_layout;
                    this.addAllParents(childLayout.GetChildren(), childLayout, accumulator);
                }
                else
                {
                    this.addAllParents(childLayout.GetChildren(), parentView_layout, accumulator);
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
                    LayoutQuery maxScoreQuery = new MaxScore_LayoutQuery();
                    maxScoreQuery.MaxWidth = width;
                    maxScoreQuery.MaxHeight = height;
                    SpecificLayout maxScore_layout = layout.GetBestLayout(maxScoreQuery);
                    maxScore_dimensions[i, j] = maxScore_layout.Dimensions;

                    
                    // find the layout of minimum width having at least this score
                    LayoutQuery minWidthQuery = new MinWidth_LayoutQuery();
                    minWidthQuery = new MinWidth_LayoutQuery();
                    minWidthQuery.MaxWidth = width;
                    minWidthQuery.MaxHeight = height;
                    minWidthQuery.MinScore = maxScore_layout.Score;
                    SpecificLayout minWidth_layout = layout.GetBestLayout(minWidthQuery);
                    if (minWidth_layout != null)
                        minWidth_dimensions[i, j] = minWidth_layout.Dimensions;

                    // find the layout of minimum height having at least this score
                    LayoutQuery minHeightQuery = new MinHeight_LayoutQuery();
                    minHeightQuery.MaxWidth = width;
                    minHeightQuery.MaxHeight = height;
                    minHeightQuery.MinScore = maxScore_layout.Score;
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
        private LayoutChoice_Set childLayout;
        private Size displaySize;
        private SpecificLayout specificLayout;
        private bool even;
        private bool needsRelayout = true;
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
