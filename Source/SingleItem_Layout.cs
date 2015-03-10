﻿using System;
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
            this.ChildFillsAvailableSpace = fillAvailableSpace;
        }
        private void Initialize()
        {
            this.ChildFillsAvailableSpace = true;
        }
        public void CopyFrom(SingleItem_Layout original)
        {
            this.ChildFillsAvailableSpace = original.ChildFillsAvailableSpace;
            base.CopyFrom(original);
        }
        private FrameworkElement view;
        public FrameworkElement View
        {
            get
            {
                if (this.view == null)
                {
                    if (!this.BorderThickness.Equals(new Thickness(0)))
                        this.view = new SingleItem_View();
                }
                return this.view;
            }
            set
            {
                this.view = value;
            }
        }
        public bool ChildFillsAvailableSpace { get; set; }
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

        // TODO: split SingleItem_Layout into more classes having different handling of BorderThickness: one that crops with a small penalty, and one that fails if cropped
        public Thickness BorderThickness { get; set; }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            if (this.SubLayout != null)
            {
                LayoutQuery subQuery = query.Clone();
                double borderWidth = this.BorderThickness.Left + this.BorderThickness.Right;
                double borderHeight = this.BorderThickness.Top + this.BorderThickness.Bottom;
                subQuery.MaxWidth = subQuery.MaxWidth - borderWidth;
                subQuery.MaxHeight = subQuery.MaxHeight - borderHeight;
                if (subQuery.MaxWidth < 0 || subQuery.MaxHeight < 0)
                {
                    // If there is no room for the border, then that violates a requirement and we return the worst possible score
                    SpecificLayout result = new Specific_SingleItem_Layout(this.view, new Size(0, 0), LayoutScore.Minimum, null, new Thickness(0));
                    if (query.Accepts(result))
                        return result;
                    return null;
                }
                subQuery.MinScore = subQuery.MinScore.Minus(this.BonusScore);
                SpecificLayout best_subLayout = this.SubLayout.GetBestLayout(subQuery);
                if (best_subLayout != null)
                {
                    Specific_SingleItem_Layout result = new Specific_SingleItem_Layout(this.View, new System.Windows.Size(best_subLayout.Width + borderWidth, best_subLayout.Height + borderHeight), best_subLayout.Score.Plus(this.BonusScore), best_subLayout, this.BorderThickness);
                    result.ChildFillsAvailableSpace = this.ChildFillsAvailableSpace;
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
