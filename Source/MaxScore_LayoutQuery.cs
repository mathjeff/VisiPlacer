using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// the MaxScore_LayoutQuery requests the Layout of minimum width satisfying the given constraints
namespace VisiPlacement
{
    class MaxScore_LayoutQuery : LayoutQuery
    {
        public override LayoutQuery Clone()
        {
            MaxScore_LayoutQuery clone = new MaxScore_LayoutQuery();
            clone.CopyFrom(this);
            return clone;
        }
        public override void OptimizeUsingExample(SpecificLayout example)
        {
            if (this.MinScore.CompareTo(example.Score) < 0)
                this.MinScore = example.Score;
        }
        public override void OptimizePastDimensions(LayoutDimensions example)
        {
            LayoutScore minScore = example.Score.Plus(LayoutScore.Tiny);
            if (this.MinScore.CompareTo(example.Score) < 0)
                this.MinScore = minScore;
            if (!this.Accepts(this.ProposedSolution_ForDebugging))
                this.ProposedSolution_ForDebugging = null;
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
