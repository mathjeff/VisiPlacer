using System;
using System.Collections.Generic;
using System.Linq;

namespace VisiPlacement
{
    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    public abstract class LayoutQuery
    {
        static int nextID;
        public LayoutQuery()
        {
            this.minScore = LayoutScore.Minimum;
            this.maxWidth = double.PositiveInfinity;
            this.maxHeight = double.PositiveInfinity;
            this.debugID = nextID;
            if (this.debugID >= 4 && this.debugID <= 58)
            {
                System.Diagnostics.Debug.WriteLine("Hi");
            }
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
                return tieWinner;
            LayoutDimensions dimensions1 = new LayoutDimensions();
            dimensions1.Width = tieWinner.Width;
            dimensions1.Height = tieWinner.Height;
            dimensions1.Score = tieWinner.Score;
            LayoutDimensions dimensions2 = new LayoutDimensions();
            dimensions2.Width = tieLoser.Width;
            dimensions2.Height = tieLoser.Height;
            dimensions2.Score = tieLoser.Score;
            // TODO: when asking a MaxScore_LayoutQuery which layout it likes better, consider calling isScoreAtLeast instead of computing all the scores
            if (this.PreferredLayout(dimensions1, dimensions2) == dimensions1)
                return tieWinner;
            else
                return tieLoser;
        }
        public abstract LayoutQuery Clone();
        public LayoutQuery DebugClone()
        {
            LayoutQuery clone = this.Clone();
            clone.Debug = true;
            return clone;
        }
        public LayoutQuery WithDimensions(double width, double height)
        {
            LayoutQuery result = this.Clone();
            result.setMaxWidth(width);
            result.setMaxHeight(height);
            return result;
        }
        public LayoutQuery WithDimensions(double width, double height, LayoutScore score)
        {
            LayoutQuery result = this.Clone();
            result.setMaxWidth(width);
            result.setMaxHeight(height);
            result.setMinScore(score);
            return result;
        }
        public LayoutQuery WithScore(LayoutScore score)
        {
            LayoutQuery result = this.Clone();
            result.setMinScore(score);
            return result;
        }
        public LayoutQuery WithDefaults(LayoutDefaults defaults)
        {
            LayoutQuery result = this.Clone();
            result.setLayoutDefaults(defaults);
            return result;
        }
        // returns a stricter query given that this example is one of the options
        public abstract LayoutQuery OptimizedUsingExample(SpecificLayout example);
        // returns a stricter query that won't even be satisfied by this example
        public LayoutQuery OptimizedPastExample(SpecificLayout example)
        {
            return this.OptimizedPastDimensions(example.Dimensions);
        }
        // returns a stricter query that won't even be satisfied by this example
        public abstract LayoutQuery OptimizedPastDimensions(LayoutDimensions dimensions);
        protected LayoutQuery CopyFrom(LayoutQuery original)
        {
            this.setMaxWidth(original.maxWidth);
            this.setMaxHeight(original.maxHeight);
            this.setMinScore(original.minScore);
            this.setLayoutDefaults(original.layoutDefaults);
            if (original.Debug)
                this.Debug = true;
            this.Cost = original.Cost;
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
            if (dimensions == null)
                return false;
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
        }
        protected void setMaxWidth(double value)
        {
            if (double.IsNaN(value))
                throw new ArgumentException("Illegal width: " + value);
            this.maxWidth = value;
            if (!this.Accepts(this.proposedSolution_forDebugging))
                this.proposedSolution_forDebugging = null;
        }
        public double MaxHeight
        {
            get
            {
                return this.maxHeight;
            }
        }
        protected void setMaxHeight(double value)
        {
            if (double.IsNaN(value))
                throw new ArgumentException("Illegal height: " + value);
            this.maxHeight = value;
            if (!this.Accepts(this.proposedSolution_forDebugging))
                this.proposedSolution_forDebugging = null;
        }
        public LayoutScore MinScore
        {
            get
            {
                return this.minScore;
            }
        }
        public LayoutDefaults LayoutDefaults
        {
            get
            {
                return this.layoutDefaults;
            }
        }

        public LayoutQuery New_MaxScore_LayoutQuery(double width, double height, LayoutScore layoutScore)
        {
            return new MaxScore_LayoutQuery(width, height, layoutScore, this.layoutDefaults);
        }
        public LayoutQuery New_MinWidth_LayoutQuery(double width, double height, LayoutScore layoutScore)
        {
            return new MinWidth_LayoutQuery(width, height, layoutScore, this.layoutDefaults);
        }
        public LayoutQuery New_MinHeight_LayoutQuery(double width, double height, LayoutScore layoutScore)
        {
            return new MinHeight_LayoutQuery(width, height, layoutScore, this.layoutDefaults);
        }
        protected void setMinScore(LayoutScore value)
        {
            this.minScore = value;
            if (!this.Accepts(this.proposedSolution_forDebugging))
                this.proposedSolution_forDebugging = null;
            if (this.Debug)
            {
                if (value.CompareTo(LayoutScore.Get_CutOff_LayoutScore(1)) <= 0 && this.MaxWidth > 0 && this.MaxHeight > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Cropping query score for query " + this);
                }
            }
        }
        protected void setLayoutDefaults(LayoutDefaults defaults)
        {
            this.layoutDefaults = defaults;
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
                    /* // go back and run the original query again
                    LayoutQuery debugQuery = proposedSolution.SourceQuery;
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
                    }*/
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
            if (query1.layoutDefaults != query2.layoutDefaults)
                return false;
            return true;
        }
        public static int ExpensiveThreshold = 50;
        public void OnAnswered(LayoutChoice_Set layout)
        {
            this.Cost = LayoutQuery.nextID - this.debugID;
            if (this.Cost >= ExpensiveThreshold)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debug.WriteLine("Expensive layout query: " + this + " in " + layout + " required " + this.Cost + " queries");
                }
                ExpensiveThreshold *= 2;
            }
        }

        public override string ToString()
        {
            return this.GetType().Name + ": (" + this.MaxWidth + ", " + this.MaxHeight + ", " + this.minScore + ")";
        }
        private double maxWidth, maxHeight;
        private LayoutScore minScore;
        private LayoutDefaults layoutDefaults;
        private SpecificLayout proposedSolution_forDebugging;
        public int Cost { get; set; }
        public int debugID;
    }
}
