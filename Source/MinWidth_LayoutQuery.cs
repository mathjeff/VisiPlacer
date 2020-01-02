using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// the MinWidth_LayoutQuery requests the Layout of minimum width satisfying the given constraints
namespace VisiPlacement
{
    class MinWidth_LayoutQuery : LayoutQuery
    {
        public MinWidth_LayoutQuery(double maxWidth, double maxHeight, LayoutScore minScore)
        {
            this.setMaxWidth(maxWidth);
            this.setMaxHeight(maxHeight);
            this.setMinScore(minScore);
        }
        public MinWidth_LayoutQuery()
        {
        }

        public override LayoutQuery Clone()
        {
            return this.Clone((MinWidth_LayoutQuery)null);
        }
        public MinWidth_LayoutQuery Clone(MinWidth_LayoutQuery returnType)
        {
            MinWidth_LayoutQuery clone = new MinWidth_LayoutQuery();
            clone.CopyFrom(this);
            return clone;
        }
        public override LayoutQuery OptimizedUsingExample(SpecificLayout example)
        {
            MinWidth_LayoutQuery result = this;
            if (this.MaxWidth > example.Width)
            {
                result = this.Clone((MinWidth_LayoutQuery)null);
                result.setMaxWidth(example.Width);
            }
            return result;
        }
        public override LayoutQuery OptimizedPastDimensions(LayoutDimensions example)
        {
            MinWidth_LayoutQuery result = this;
            double newWidth = example.Width * 0.9999999999;
            if (this.MaxWidth > newWidth)
            {
                result = this.Clone((MinWidth_LayoutQuery)null);
                result.setMaxWidth(newWidth);
                if (!result.Accepts(result.ProposedSolution_ForDebugging))
                    result.ProposedSolution_ForDebugging = null;
            }
            return result;
        }

        public override LayoutDimensions PreferredLayout(LayoutDimensions choice1, LayoutDimensions choice2)
        {
            if (!this.Accepts(choice1))
            {
                if (this.Accepts(choice2))
                    return choice2;
                return null;
            }
            if (!this.Accepts(choice2))
                return choice1;
            if (choice1.Width <= choice2.Width)
                return choice1;
            return choice2;
        }
        public override bool MinimizesWidth()
        {
            return true;
        }
        public override string ToString()
        {
            return "MinWidthQuery(" + this.MaxWidth + "," + this.MaxHeight + "," + this.MinScore + ")";
        }
    }
}
