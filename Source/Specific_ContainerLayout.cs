using System;
using System.Collections.Generic;
using Xamarin.Forms;

// a Specific_ContainerLayout just has a view, a size, a subLayout, and a score
namespace VisiPlacement
{
    public class Specific_ContainerLayout : SpecificLayout
    {
        public Specific_ContainerLayout()
        {
            this.Initialize();
        }
        public Specific_ContainerLayout(View view, Size size, LayoutScore bonusScore, SpecificLayout subLayout, Thickness borderThickness)
        {
            this.Initialize();
            this.view = view;
            this.Size = size;
            this.bonusScore = bonusScore;
            this.BorderThickness = borderThickness;
            this.subLayout = subLayout;
        }
        public Specific_ContainerLayout(View view, Size size, LayoutScore bonusScore, Thickness borderThickness)
            : this(view, size, bonusScore, null, borderThickness)
        {
        }
        private void Initialize()
        {
            this.ChildFillsAvailableSpace = true;
        }

        public bool ChildFillsAvailableSpace { get; set; }
        public override View DoLayout(Size displaySize, ViewDefaults layoutDefaults)
        {
            if (this.subLayout != null)
            {
                double outerWidth = displaySize.Width;
                if (this.Size.Width < outerWidth && !this.ChildFillsAvailableSpace)
                    outerWidth = this.Size.Width;
                double outerHeight = displaySize.Height;
                if (this.Size.Height < outerHeight && !this.ChildFillsAvailableSpace)
                    outerHeight = this.Size.Height;

                Size childSize = this.chooseSize(new Size(outerWidth, outerHeight));

                View childContent = this.subLayout.DoLayout(childSize, layoutDefaults);
                if (this.View != null)
                {
                    this.View.WidthRequest = displaySize.Width;
                    this.View.HeightRequest = displaySize.Height;
                    this.PutContentInView(this.View, childContent);
                    return this.View;
                }
                return childContent;
            }
            return this.View;
        }
        protected virtual Size chooseSize(Size availableSize)
        {
            double subviewWidth = availableSize.Width - this.BorderThickness.Left - this.BorderThickness.Right;
            double subviewHeight = availableSize.Height - this.BorderThickness.Top - this.BorderThickness.Bottom;
            return new Size(subviewWidth, subviewHeight);
        }

        // Makes one View be a direct child visual child of another one
        // TODO: refactor this into a helper class for each case, for easier customization
        protected void PutContentInView(View parent, View child)
        {
            ContentView parentAsContentControl = parent as ContentView;
            if (parentAsContentControl != null)
            {
                parentAsContentControl.Content = child;
                return;
            }
            Frame parentAsBorder = parent as Frame;
            if (parentAsBorder != null)
            {
                parentAsBorder.Content = child;
                return;
            }
            ScrollView parentAsScrollView = parent as ScrollView;
            if (parentAsScrollView != null)
            {
                parentAsScrollView.Content = child;
                return;
            }
            throw new ArgumentException("Unrecognized view type " + parent);
        }
        public override SpecificLayout Clone()
        {
            Specific_ContainerLayout clone = new Specific_ContainerLayout();
            clone.CopyFrom(this);
            return clone;
        }
        public void CopyFrom(Specific_ContainerLayout original)
        {
            base.CopyFrom(original);
            this.view = original.view;
            this.Size = original.Size;
            this.bonusScore = original.bonusScore;
            this.subLayout = original.subLayout;
            this.BorderThickness = original.BorderThickness;
            this.ChildFillsAvailableSpace = original.ChildFillsAvailableSpace;
        }
        public new Size Size { get; set; }
        public override double Width
        {
            get
            {
                return this.Size.Width;
            }
        }
        public override double Height
        {
            get 
            {
                return this.Size.Height;
            }
        }
        public override LayoutScore Score 
        {
            get
            {
                LayoutScore result = this.bonusScore;
                if (this.SubLayout != null)
                    result = result.Plus(this.subLayout.Score);
                return result;
            }
        }
        public override View View
        {
            get
            {
                if (this.view == null)
                {
                    if (!this.BorderThickness.Equals(new Thickness(0)))
                        this.view = new ContainerView();
                }
                return this.view;
            }
        }
        public override void Remove_VisualDescendents()
        {
            if (this.subLayout != null)
                this.subLayout.Remove_VisualDescendents();


            View view = this.view;
            ContentView parentAsContentControl = view as ContentView;
            if (parentAsContentControl != null)
            {
                parentAsContentControl.Content = null;
                return;
            }
            Frame parentAsBorder = view as Frame;
            if (parentAsBorder != null)
            {
                parentAsBorder.Content = null;
                return;
            }
            ScrollView parentAsScrollView = view as ScrollView;
            if (parentAsScrollView != null)
            {
                parentAsScrollView.Content = null;
                return;
            }
        }

        public override void Remove_VisualDescendent(View view)
        {
            if (this.view != null)
            {
                if (this.view == view)
                    this.Remove_VisualDescendents();
                return;
            }
            // If we just have a sublayout and no view, then ask our sublayout to remove this view
            if (this.subLayout != null)
                this.subLayout.Remove_VisualDescendent(view);
        }

        public override IEnumerable<SpecificLayout> GetParticipatingChildren()
        {
            List<SpecificLayout> children = new List<SpecificLayout>();
            if (this.subLayout != null)
            {
                children.Add(subLayout);
            }
            return children;
        }
        public SpecificLayout SubLayout
        {
            get
            {
                return this.subLayout;
            }
        }

        public Thickness BorderThickness { get; set; }
        private LayoutScore bonusScore;
        private View view;
        private SpecificLayout subLayout;
    }
}
