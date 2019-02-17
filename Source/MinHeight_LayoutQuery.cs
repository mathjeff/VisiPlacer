using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// the MinHeight_LayoutQuery requests the Layout of minimum height satisfying the given constraints
namespace VisiPlacement
{
    class MinHeight_LayoutQuery : LayoutQuery
    {
        public override LayoutQuery Clone()
        {
            MinHeight_LayoutQuery clone = new MinHeight_LayoutQuery();
            clone.CopyFrom(this);
            return clone;
        }
        public override void OptimizeUsingExample(SpecificLayout example)
        {
            if (this.MaxHeight > example.Height)
                this.MaxHeight = example.Height;
        }
        public override void OptimizePastDimensions(LayoutDimensions example)
        {
            double newHeight = example.Height * 0.9999999999;
            if (this.MaxHeight > newHeight)
                this.MaxHeight = newHeight;
            if (!this.Accepts(this.ProposedSolution_ForDebugging))
                this.ProposedSolution_ForDebugging = null;
        }
        public override LayoutDimensions PreferredLayout(LayoutDimensions choice1, LayoutDimensions choice2)
        {
            if (choice1 == null)
            {
                if (this.Accepts(choice2))
                    return choice2;
                return null;

            }
            if (choice2 == null)
            {
                if (this.Accepts(choice1))
                    return choice1;
                return null;
            }
            if (choice1.Height <= choice2.Height)
                return choice1;
            return choice2;
        }
        public override bool MinimizesHeight()
        {
            return true;
        }
        public override string ToString()
        {
            return "MinHeightQuery(" + this.MaxWidth + "," + this.MaxHeight + "," + this.MinScore + ")";
        }
    }
}
