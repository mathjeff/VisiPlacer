using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Graphics.Display;

namespace VisiPlacement
{
    public class ViewManager : LayoutChoice_Set
    {
        public ViewManager(ContentControl parentView, LayoutChoice_Set layoutToManage)
        {
            this.visibleLayouts = new LinkedList<SpecificLayout>();
            this.layoutToManage = layoutToManage;
            layoutToManage.AddParent(this);

            this.parentView = new ManageableView(this);
            parentView.Content = this.parentView;
            this.displaySize = parentView.RenderSize;

            //this.DoLayout(new Size(456, 657));

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

        /*protected override void OnChildDesiredSizeChanged(FrameworkElement child)
        {
            this.InvalidateMeasure();
        }*/
        public void ClearLayout()
        {
            foreach (SpecificLayout layout in this.visibleLayouts)
            {
                layout.Remove_VisualDescendents();
            }
            this.visibleLayouts.Clear();
        }

        public void DoLayout(Size size)
        {
            this.displaySize = size;
            this.DoLayout();
        }

        public void DoLayout()
        {
            DateTime startTime = DateTime.Now;
            this.Reset_ChangeAnnouncement();
            this.ClearLayout();
            FrameworkElement newView = this.DoLayout(this.layoutToManage, this.displaySize);
            //newView.Width = this.parentView.Width = this.displaySize.Width;
            //newView.Height = this.parentView.Height = this.displaySize.Height;
            this.parentView.Content = newView;
            //this.parentView.InvalidateMeasure();
            //newView.InvalidateMeasure();
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime.Subtract(startTime);
            System.Diagnostics.Debug.WriteLine("ViewManager DoLayout finished in " + duration);
            System.Diagnostics.Debug.WriteLine("Text formatting time = " + TextLayout.TextTime + " for " + TextLayout.NumMeasures + " measures");
            TextLayout.NumMeasures = 0;
            TextLayout.TextTime = new TimeSpan();
            TextBlock block = new TextBlock();
            //block.Inlines.Add(;
        }

        public FrameworkElement DoLayout(LayoutChoice_Set layout, Size bounds)
        {
            LayoutQuery query = new MaxScore_LayoutQuery();
            query.MaxWidth = bounds.Width;
            query.MaxHeight = bounds.Height;
            SpecificLayout bestLayout = layout.GetBestLayout(query);

            // figure out where the subviews are placed
            IEnumerable<SubviewDimensions> locations = bestLayout.DoLayout(bounds);

            this.visibleLayouts.AddLast(bestLayout);

            // update each subview
            foreach (SubviewDimensions location in locations)
            {
                location.SubLayout.View.InvalidateMeasure();
                this.DoLayout(location.SubLayout, location.Size);
            }
            return bestLayout.View;
        }

        public override void On_ContentsChanged(bool mustRedraw)
        {
            if (mustRedraw)
                this.parentView.InvalidateMeasure();
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

        private ContentControl parentView;
        private LayoutChoice_Set layoutToManage;
        private LinkedList<SpecificLayout> visibleLayouts;
        private Size displaySize;
    }

    class ManageableView : ContentControl
    {
        public ManageableView(ViewManager viewManager)
        {
            this.Background = new SolidColorBrush(Colors.Green);
            this.ViewManager = viewManager;
            //Size size = new Size(456, 744);
            Size size = new Size(744, 456);
            this.makeContent(size);
            this.latestSize = size;

        }

        public ViewManager ViewManager { get; set; }

#if true
        protected override Size MeasureOverride(Size bounds)
        {
            /*if (!Size.Equals(this.latestSize, bounds))
            {
                if (timer == null)
                {
                    timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(0);
                    timer.Tick += timer_Tick;
                    timer.Start();
                }
                this.latestSize = bounds;
            }*/
            this.ViewManager.DoLayout(bounds);

            base.MeasureOverride(bounds);
            return bounds;
#if false
            bool matches = Size.Equals(bounds, this.latestSize);

            //if (!Size.Equals(this.latestSize, new Size()))
            //    bounds = new Size(this.latestSize.Height, this.latestSize.Width);
            this.latestSize = bounds;

            //if (bounds.Height < bounds.Width)
            //if (!matches && bounds.Height > bounds.Width)
            if (!matches)
            {
                /*if (timer == null)
                {
                    timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(2);
                    timer.Tick += timer_Tick;
                    timer.Start();
                }*/
                FrameworkElement content = (FrameworkElement)this.Content;
                //content.Width = bounds.Width;
                //content.Height = bounds.Height;
                content.InvalidateMeasure();
                content.InvalidateArrange();
                content.Measure(bounds);
                
                this.UpdateLayout();

                //((FrameworkElement)this.Content).Measure(bounds);
                //this.makeContent(bounds);

                base.Measure(bounds);

            }
            //this.ViewManager.DoLayout(bounds);

            this.latestSize = bounds;
            //return this.latestSize;
            return this.latestSize;
#endif
        }
#endif

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            timer = null;

            this.ViewManager.DoLayout(this.latestSize);
            //this.makeContent(this.latestSize);
            //base.Measure(this.latestSize);

        }
        
        protected override Size ArrangeOverride(Size finalSize)
        {
            FrameworkElement element = (FrameworkElement)this.Content;
            //element.Arrange(new Rect(new Point(), finalSize));
            Size tempSize = base.ArrangeOverride(finalSize);
            //System.Diagnostics.Debug.WriteLine(tempSize);
            return finalSize;
        }

        private void makeContent(Size bounds)
        {
            TextBlock block = new TextBlock();
            block.TextWrapping = TextWrapping.Wrap;
            block.Width = bounds.Width;
            block.Height = bounds.Height;
            block.Text = "Loading...\nPlease Wait";
            this.Content = block;
            this.Background = new SolidColorBrush(Colors.Blue);

        }

        void DisplayProperties_OrientationChanged(object sender)
        {
            System.Diagnostics.Debug.WriteLine("orientation changed!");
        }
        private Size latestSize = new Size();
        private DispatcherTimer timer;
        //private Size latestSize = new Size();
        //private int numMatches = 0;
        //private bool measured = true;
        //private bool measured = false;
    }
}
