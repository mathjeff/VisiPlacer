using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// the MinHeight_LayoutQuery requests the Layout of minimum height satisfying the given constraints
namespace VisiPlacement
{
    class MinHeight_LayoutQuery : LayoutQuery
    {
        public MinHeight_LayoutQuery(double maxWidth, double maxHeight, LayoutScore minScore, LayoutDefaults layoutDefaults)
        {
            this.setMaxWidth(maxWidth);
            this.setMaxHeight(maxHeight);
            this.setMinScore(minScore);
            this.setLayoutDefaults(layoutDefaults);
        }
        private MinHeight_LayoutQuery()
        {
        }

        public override LayoutQuery Clone()
        {
            return this.Clone((MinHeight_LayoutQuery)null);
        }
        public MinHeight_LayoutQuery Clone(MinHeight_LayoutQuery returnType)
        {
            MinHeight_LayoutQuery clone = new MinHeight_LayoutQuery();
            clone.CopyFrom(this);
            return clone;
        }
        public override LayoutQuery OptimizedUsingExample(SpecificLayout example)
        {
            MinHeight_LayoutQuery result = this;
            if (this.MaxHeight > example.Height)
            {
                result = this.Clone((MinHeight_LayoutQuery)null);
                result.setMaxHeight(example.Height);
            }
            return result;
        }
        public override LayoutQuery OptimizedPastDimensions(LayoutDimensions example)
        {
            MinHeight_LayoutQuery result = this;
            double newHeight = example.Height * 0.9999999999;
            if (this.MaxHeight > newHeight)
            {
                result = this.Clone((MinHeight_LayoutQuery)null);
                result.setMaxHeight(newHeight);
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
