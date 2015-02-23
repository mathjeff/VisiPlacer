using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace VisiPlacement
{
    public class SingleItem_Layout : LayoutChoice_Set
    {
        public SingleItem_Layout()
        {
            this.Initialize();
            this.BonusScore = LayoutScore.Zero;
            //this.View = new SingleItem_View();
        }
        public SingleItem_Layout(FrameworkElement view, LayoutChoice_Set subLayout, Thickness borderThickness, LayoutScore bonusScore)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
            this.BonusScore = bonusScore;
        }
        public SingleItem_Layout(FrameworkElement view, LayoutChoice_Set subLayout, Thickness borderThickness, LayoutScore bonusScore, bool fillAvailableSpace)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BorderThickness = borderThickness;
            this.BonusScore = bonusScore;
            this.FillAvailableSpace = fillAvailableSpace;
        }
        private void Initialize()
        {
            this.FillAvailableSpace = true;
        }
        public void CopyFrom(SingleItem_Layout original)
        {
            this.FillAvailableSpace = original.FillAvailableSpace;
            base.CopyFrom(original);
        }
        private FrameworkElement view;
        public FrameworkElement View
        {
            get
            {
                if (this.view == null)
                    this.view = new SingleItem_View();
                return this.view;
            }
            set
            {
                this.view = value;
            }
        }
        public bool FillAvailableSpace { get; set; }
        public LayoutChoice_Set SubLayout 
        {
            get
            {
                return this.subLayout;
            }
            set
            {
                if (this.subLayout != null)
                    this.subLayout.RemoveParent(this);
                this.subLayout = value;
                if (this.subLayout != null)
                    this.subLayout.AddParent(this);
                this.AnnounceChange(true);
            }
        }
        public LayoutScore BonusScore { get; set; }

        public Thickness BorderThickness {get;set;}
        /*{
            getC:\Users\Jeff\Documents\Visual Studio 2012\Projects\Interesting\VisiPlacer\VisiPlacer\VisiPlacer\Specific_TextLayout.cs
            {
                return new Size(this.View.Padding.Left + this.View.Padding.Right + this.View.BorderThickness.Left + this.View.BorderThickness.Right, this.View.Padding.Top + this.View.Padding.Bottom + this.View.BorderThickness.Top + this.View.BorderThickness.Bottom);
            }
        }*/

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (this.SubLayout != null)
            {
                LayoutQuery subQuery = query.Clone();
                double borderWidth = this.BorderThickness.Left + this.BorderThickness.Right;
                double borderHeight = this.BorderThickness.Top + this.BorderThickness.Bottom;
                subQuery.MaxWidth = subQuery.MaxWidth - borderWidth;
                subQuery.MaxHeight = subQuery.MaxHeight - borderHeight;
                subQuery.MinScore = subQuery.MinScore.Minus(this.BonusScore);
                SpecificLayout best_subLayout = this.SubLayout.GetBestLayout(subQuery);
                if (best_subLayout != null)
                {
                    Specific_SingleItem_Layout result = new Specific_SingleItem_Layout(this.View, new System.Windows.Size(best_subLayout.Width + borderWidth, best_subLayout.Height + borderHeight), best_subLayout.Score.Plus(this.BonusScore), best_subLayout, this.BorderThickness);
                    result.FillAvailableSpace = this.FillAvailableSpace;
                    this.prepareLayoutForQuery(result, query);
                    return result;
                }
                return null;
            }
            // if there is no subLayout, for now we just return an empty size
            return null;
        }

        LayoutChoice_Set subLayout;
    }

}
