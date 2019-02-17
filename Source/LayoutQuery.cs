using System;
using System.Linq;

namespace VisiPlacement
{
    public abstract class LayoutQuery
    {
        static int nextID;
        public LayoutQuery()
        {
            this.MinScore = LayoutScore.Minimum;
            this.maxWidth = double.PositiveInfinity;
            this.maxHeight = double.PositiveInfinity;
            this.debugID = nextID;
            nextID++;

        }
        // returns whichever layout it likes better
        public abstract LayoutDimensions PreferredLayout(LayoutDimensions tieWinner, LayoutDimensions tieLoser);
        // returns whichever layout it likes better
        public SpecificLayout PreferredLayout(SpecificLayout tieWinner, SpecificLayout tieLoser)
        {
            if (!this.Accepts(tieWinner))
            {
                if (this.Accepts(tieLoser))
                    return tieLoser;
                else
                    return null;
            }
            if (!this.Accepts(tieLoser))
            {
                if (this.Accepts(tieWinner))
                    return tieWinner;
                else
                    return null;
            }
            LayoutDimensions dimensions1 = new LayoutDimensions();
            dimensions1.Width = tieWinner.Width;
            dimensions1.Height = tieWinner.Height;
            dimensions1.Score = tieWinner.Score;
            LayoutDimensions dimensions2 = new LayoutDimensions();
            dimensions2.Width = tieLoser.Width;
            dimensions2.Height = tieLoser.Height;
            dimensions2.Score = tieLoser.Score;
            if (this.PreferredLayout(dimensions1, dimensions2) == dimensions1)
                return tieWinner;
            else
                return tieLoser;
        }
        public abstract LayoutQuery Clone();
        // returns a stricter query given that this example is one of the options
        public abstract void OptimizeUsingExample(SpecificLayout example);
        // returns a stricter query that won't even be satisfied by this example
        public void OptimizePastExample(SpecificLayout example)
        {
            this.OptimizePastDimensions(example.Dimensions);
        }
        // returns a stricter query that won't even be satisfied by this example
        public abstract void OptimizePastDimensions(LayoutDimensions dimensions);
        public LayoutQuery CopyFrom(LayoutQuery original)
        {
            this.MaxWidth = original.MaxWidth;
            this.MaxHeight = original.MaxHeight;
            this.MinScore = original.MinScore;
            if (original.Debug)
                this.Debug = true;
            this.ProposedSolution_ForDebugging = original.ProposedSolution_ForDebugging;
            return this;
        }
        public bool Accepts(SpecificLayout layout)
        {
            if (layout == null)
                return false;
            if (layout.GetBestLayout(this) != null)
                return true;
            return false;
        }
        public bool Accepts(LayoutDimensions dimensions)
        {
            if (dimensions.Width > this.MaxWidth)
                return false;
            if (dimensions.Height > this.MaxHeight)
                return false;
            if (dimensions.Score.CompareTo(this.MinScore) < 0)
                return false;
            return true;
        }
        
        // whether this query tries to minimize Height
        public virtual bool MinimizesHeight()
        {
            return false;
        }
        public virtual bool MinimizesWidth()
        {
            return false;
        }
        public virtual bool MaximizesScore()
        {
            return false;
        }

        public bool SameType(LayoutQuery other)
        {
            if (this.MinimizesWidth() != other.MinimizesWidth())
                return false;
            if (this.MinimizesHeight() != other.MinimizesHeight())
                return false;
            if (this.MaximizesScore() != other.MaximizesScore())
                return false;
            return true;
        }
        
        public double MaxWidth 
        {
            get
            {
                return this.maxWidth;
            }
            set
            {
                if (double.IsNaN(value))
                    throw new ArgumentException("Illegal width: " + value);
                this.maxWidth = value;
                if (!this.Accepts(this.proposedSolution_forDebugging))
                    this.proposedSolution_forDebugging = null;
            }
        }
        public double MaxHeight
        {
            get
            {
                return this.maxHeight;
            }
            set
            {
                if (double.IsNaN(value))
                    throw new ArgumentException("Illegal height: " + value);
                this.maxHeight = value;
                if (!this.Accepts(this.proposedSolution_forDebugging))
                    this.proposedSolution_forDebugging = null;
            }
        }
        public LayoutScore MinScore
        {
            get
            {
                return this.minScore;
            }
            set
            {
                this.minScore = value;
                if (!this.Accepts(this.proposedSolution_forDebugging))
                    this.proposedSolution_forDebugging = null;
            }
        }
        public bool Debug { get; set; } // whether we want to do extra work for this query to ensure the results are correct
        public SpecificLayout ProposedSolution_ForDebugging 
        {
            get
            {
                return this.proposedSolution_forDebugging;
            }
            set
            {
                SpecificLayout proposedSolution = value;
                if ((proposedSolution != null) && !this.Accepts(proposedSolution))
                {
                    ErrorReporter.ReportParadox("Error: attempted to provide an invalid debugging solution");
                    // go back and run the original query again
                    LayoutQuery debugQuery = proposedSolution.SourceQuery_ForDebugging;
                    if (debugQuery != null)
                    {
                        debugQuery = debugQuery.Clone();
                        debugQuery.Debug = true;
                        if (proposedSolution.GetAncestors().Count() > 0)
                        {
                            LayoutChoice_Set parent = proposedSolution.GetAncestors().First();
                            SpecificLayout result = parent.GetBestLayout(debugQuery);
                            ErrorReporter.ReportParadox("result = " + result);
                        }
                    }
                }
                this.proposedSolution_forDebugging = proposedSolution;
            }
        }

        public bool Equals(LayoutQuery other)
        {
            LayoutQuery query1 = this;
            LayoutQuery query2 = other;
            if (query1.MaxHeight != query2.MaxHeight)
                return false;
            if (query1.MaxWidth != query2.MaxWidth)
                return false;
            if (query1.MinScore.CompareTo(query2.MinScore) != 0)
                return false;
            if (query1.MinimizesHeight() != query2.MinimizesHeight())
                return false;
            if (query1.MinimizesWidth() != query2.MinimizesWidth())
                return false;
            if (query1.MaximizesScore() != query2.MaximizesScore())
                return false;
            return true;
        }
        private double maxWidth, maxHeight;
        private LayoutScore minScore;
        private SpecificLayout proposedSolution_forDebugging;
        public int debugID;
    }
}
