using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class ViewManager : LayoutChoice_Set
    {
        public ViewManager(ContentView parentView, LayoutChoice_Set layoutToManage)
        {
            this.layoutToManage = layoutToManage;
            layoutToManage.AddParent(this);

            this.parentView = new ManageableView(this);
            parentView.Content = this.parentView;
            this.displaySize = new Size(parentView.Width, parentView.Height);

            //this.DoLayout(new Size(100, 100));
        }
        public void SetLayout(LayoutChoice_Set layoutToManage)
        {
            if (this.layoutToManage != null)
                this.layoutToManage.RemoveParent(this);
            this.layoutToManage = layoutToManage;
            if (this.layoutToManage != null)
            {
                layoutToManage.AddParent(this);
                this.DoLayout();
            }
        }

        public void Remove_VisualDescendents()
        {
            if (this.specificLayout != null)
                this.specificLayout.Remove_VisualDescendents();
        }

        public void UpdateSize(Size size)
        {
            if (size.Width != this.displaySize.Width || size.Height != this.displaySize.Height)
            {
                this.DoLayout(size);
            }
        }
        public void DoLayout(Size size)
        {
            if (Double.IsInfinity(size.Width) || Double.IsInfinity(size.Height))
                throw new ArgumentException();
            this.displaySize = size;
            this.DoLayout();
        }

        public void DoLayout()
        {
            IEnumerable<SpecificLayout> previousLayouts;
            if (this.specificLayout != null)
            {
                previousLayouts = this.specificLayout.GetDescendents();
            }
            else
            {
                previousLayouts = new LinkedList<SpecificLayout>();
            }
            List<View> focusedLayouts = new List<View>();
            foreach (SpecificLayout layout in previousLayouts)
            {
                if (layout.View != null && layout.View.IsFocused)
                {
                    focusedLayouts.Add(layout.View);
                }
            }

            //object focusedElement = FocusManager.GetFocusedElement();
            int num_grid_preComputations = GridLayout.NumComputations;

            DateTime startTime = DateTime.Now;
            this.Remove_VisualDescendents();
            View newView = this.DoLayout(this.layoutToManage, this.displaySize);
            this.Reset_ChangeAnnouncement();
            //newView.Width = this.parentView.Width = this.displaySize.Width;
            //newView.Height = this.parentView.Height = this.displaySize.Height;
            this.parentView.Content = newView;
            //this.parentView.InvalidateMeasure();
            //newView.InvalidateMeasure();
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            System.Diagnostics.Debug.WriteLine("ViewManager DoLayout finished in " + duration);
            System.Diagnostics.Debug.WriteLine("Text formatting time = " + TextLayout.TextTime + " for " + TextLayout.NumMeasures + " measures");
            int num_grid_postComputations = GridLayout.NumComputations;
            System.Diagnostics.Debug.WriteLine("Num grid computations = " + (num_grid_postComputations - num_grid_preComputations));
            TextLayout.NumMeasures = 0;
            TextLayout.TextTime = new TimeSpan();

            foreach (View view in focusedLayouts)
            {
                view.Focus();
            }
            //View control = focusedElement as View;
            //if (control != null)
            //    control.Focus();

        }

        public View DoLayout(LayoutChoice_Set layout, Size bounds)
        {
            LayoutQuery query = new MaxScore_LayoutQuery();
            query.MaxWidth = bounds.Width;
            query.MaxHeight = bounds.Height;
            this.specificLayout = layout.GetBestLayout(query);

            // figure out where the subviews are placed
            return this.specificLayout.DoLayout(bounds);

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
            // for some reason, this.parentView.ForceLayout doesn't work
            this.even = !this.even;
            if (this.even)
            {
                this.parentView.VerticalOptions = LayoutOptions.CenterAndExpand;
            }
            else
            {
                this.parentView.VerticalOptions = LayoutOptions.Center;
            }

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

        private ContentView parentView;
        private LayoutChoice_Set layoutToManage;
        //private LinkedList<SpecificLayout> visibleLayouts;
        private Size displaySize;
        private SpecificLayout specificLayout;
        private bool even;
    }

    public class ManageableView : ContentView
    {
        public ManageableView(ViewManager viewManager)
        {
            //this.BackgroundColor = Color.Green;
            this.ViewManager = viewManager;
        }

        public ViewManager ViewManager { get; set; }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width > 0 && height > 0)
            {
                Size bounds = new Size(width, height);
                this.ViewManager.UpdateSize(bounds);
            }
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            Size bounds = new Size(widthConstraint, heightConstraint);
            this.ViewManager.DoLayout(bounds);

            return new SizeRequest(bounds);
        }
        
    }
}
