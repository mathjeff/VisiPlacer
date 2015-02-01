using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisiPlacement
{
    public class LayoutDimensions
    {
        public LayoutDimensions()
        {
        }
        public LayoutDimensions(LayoutDimensions original)
        {
            this.CopyFrom(original);
        }
        protected virtual void CopyFrom(LayoutDimensions original)
        {
            this.Width = original.Width;
            this.Height = original.Height;
            this.Score = original.Score;
        }
        public LayoutDimensions Clone()
        {
            LayoutDimensions clone = new LayoutDimensions();
            clone.Width = this.Width;
            clone.Height = this.Height;
            clone.Score = this.Score;
            return clone;
        }

        public double Width { get; set; }
        public double Height { get; set; }
        public LayoutScore Score { get; set; }
    }
}
