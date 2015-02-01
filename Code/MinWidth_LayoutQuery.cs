using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// the MinWidth_LayoutQuery requests the Layout of minimum width satisfying the given constraints
namespace VisiPlacement
{
    class MinWidth_LayoutQuery : LayoutQuery
    {
        public override LayoutQuery Clone()
        {
            MinWidth_LayoutQuery clone = new MinWidth_LayoutQuery();
            clone.CopyFrom(this);
            return clone;
        }
        public override void OptimizeUsingExample(SpecificLayout example)
        {
            if (this.MaxWidth > example.Width)
                this.MaxWidth = example.Width;
        }
        public override void OptimizePastExample(SpecificLayout example)
        {
            double newWidth = example.Width * 0.9999999999;
            if (this.MaxWidth > newWidth)
                this.MaxWidth = newWidth;
        }

        public override LayoutDimensions PreferredLayout(LayoutDimensions choice1, LayoutDimensions choice2)
        {
            if (choice1 == null)
                return choice2;
            if (choice2 == null)
                return choice1;
            if (choice1.Width <= choice2.Width)
                return choice1;
            return choice2;
        }
        public override bool MinimizesWidth()
        {
            return true;
        }

    }
}
