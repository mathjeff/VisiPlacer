using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// the MaxScore_LayoutQuery requests the Layout of minimum width satisfying the given constraints
namespace VisiPlacement
{
    public class MaxScore_LayoutQuery : LayoutQuery
    {
        public MaxScore_LayoutQuery(double maxWidth, double maxHeight, LayoutScore minScore)
        {
            this.setMaxWidth(maxWidth);
            this.setMaxHeight(maxHeight);
            this.setMinScore(minScore);
        }
        public MaxScore_LayoutQuery()
        {
        }
        public override LayoutQuery Clone()
        {
            return this.Clone((MaxScore_LayoutQuery)null);
        }
        public MaxScore_LayoutQuery Clone(MaxScore_LayoutQuery returnType)
        {
            MaxScore_LayoutQuery clone = new MaxScore_LayoutQuery();
            clone.CopyFrom(this);
            return clone;
        }
        public override LayoutQuery OptimizedUsingExample(SpecificLayout example)
        {
            MaxScore_LayoutQuery result = this;
            if (this.MinScore.CompareTo(example.Score) < 0)
            {
                result = this.Clone((MaxScore_LayoutQuery)null);
                result.setMinScore(example.Score);
            }
            return result;
        }
        public override LayoutQuery OptimizedPastDimensions(LayoutDimensions example)
        {
            MaxScore_LayoutQuery result = this;
            LayoutScore minScore = example.Score.Plus(LayoutScore.Tiny);
            if (this.MinScore.CompareTo(minScore) < 0)
            {
                result = this.Clone((MaxScore_LayoutQuery)null);
                result.setMinScore(minScore);
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
            if (choice1.Score.CompareTo(choice2.Score) >= 0)
                return choice1;
            return choice2;
        }
        public override bool MaximizesScore()
        {
            return true;
        }

        public override string ToString()
        {
            return "MaxScoreQuery(" + this.MaxWidth + "," + this.MaxHeight + "," + this.MinScore + ")";
        }
    }
}
