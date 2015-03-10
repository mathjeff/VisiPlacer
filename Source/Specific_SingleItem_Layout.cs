using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

// a Specific_SingleItem_Layout just has a view, a size, a subLayout, and a score
namespace VisiPlacement
{
    class Specific_SingleItem_Layout : SpecificLayout
    {
        public Specific_SingleItem_Layout()
        {
            this.Initialize();
        }
        public Specific_SingleItem_Layout(FrameworkElement view, Size size, LayoutScore score, SpecificLayout subLayout, Thickness borderThickness)
        {
            this.Initialize();
            this.view = view;
            this.Size = size;
            this.score = score;
            this.BorderThickness = borderThickness;
            this.subLayout = subLayout;
        }
        private void Initialize()
        {
            this.ChildFillsAvailableSpace = true;
        }

        public bool ChildFillsAvailableSpace { get; set; }
        public override FrameworkElement DoLayout(Size displaySize)
        {
            LinkedList<SubviewDimensions> subLayouts = new LinkedList<SubviewDimensions>();
            //if (this.subLayout != null)
            {
#if false
                double subviewWidth = this.Size.Width - this.BorderThickness.Left - this.BorderThickness.Right;
                double subviewHeight = this.Size.Height - this.BorderThickness.Top - this.BorderThickness.Bottom;
                double subviewX = this.BorderThickness.Left;
                double subviewY = this.BorderThickness.Top;
                subLayouts.AddLast(new SubviewLocation(this.subLayout, new Rect(new Point(subviewX, subviewY), new Size(subviewWidth, subviewHeight))));
#else
                double outerWidth = displaySize.Width;
                if (this.Size.Width < outerWidth && !this.ChildFillsAvailableSpace)
                    outerWidth = this.Size.Width;
                double outerHeight = displaySize.Height;
                if (this.Size.Height < outerHeight && !this.ChildFillsAvailableSpace)
                    outerHeight = this.Size.Height;
                double subviewWidth = outerWidth - this.BorderThickness.Left - this.BorderThickness.Right;
                double subviewHeight = outerHeight - this.BorderThickness.Top - this.BorderThickness.Bottom;

                ContentControl contentView = this.View as ContentControl;
                SubviewDimensions dimensions = new SubviewDimensions(this.subLayout, new Size(subviewWidth, subviewHeight));
                FrameworkElement childContent = this.subLayout.DoLayout(new Size(subviewWidth, subviewHeight));
                if (contentView != null)
                {
                    contentView.Content = childContent;
                    contentView.Width = displaySize.Width;
                    contentView.Height = displaySize.Height;
                    return contentView;
                }
                return childContent;
#endif
            }
            //if (this.view != null)
            {
            }
            
        }
        public override SpecificLayout Clone()
        {
            Specific_SingleItem_Layout clone = new Specific_SingleItem_Layout();
            clone.CopyFrom(this);
            return clone;
        }
        public void CopyFrom(Specific_SingleItem_Layout original)
        {
            base.CopyFrom(original);
            this.view = original.view;
            this.Size = original.Size;
            this.score = original.score;
            this.subLayout = original.subLayout;
            this.BorderThickness = original.BorderThickness;
            this.ChildFillsAvailableSpace = original.ChildFillsAvailableSpace;
        }
        public Size Size { get; set; }
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
                return this.score;
            }
        }
        public override FrameworkElement View
        {
            get
            {
                if (this.view == null)
                {
                    if (!this.BorderThickness.Equals(new Thickness(0)))
                        this.view = new SingleItem_View();
                }
                return this.view;
            }
        }
        public override void Remove_VisualDescendents()
        {
            if (this.subLayout != null)
                this.subLayout.Remove_VisualDescendents();
            ContentControl content = this.view as ContentControl;
            if (content != null)
                content.Content = null;
        }

        public Thickness BorderThickness { get; set; }
        private LayoutScore score;
        private FrameworkElement view;
        private SpecificLayout subLayout;
    }
}
