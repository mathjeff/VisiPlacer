﻿using Xamarin.Forms;

namespace VisiPlacement
{
    // a layout that contains one sublayout within it
    public class ContainerLayout : LayoutChoice_Set
    {
        public ContainerLayout()
        {
            this.Initialize();
            this.BonusScore = LayoutScore.Zero;
        }
        public ContainerLayout(ContentView view, LayoutChoice_Set subLayout, LayoutScore bonusScore)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BonusScore = bonusScore;
        }
        public ContainerLayout(ContentView view, LayoutChoice_Set subLayout, LayoutScore bonusScore, bool fillAvailableSpace)
        {
            this.Initialize();
            this.View = view;
            this.SubLayout = subLayout;
            this.BonusScore = bonusScore;
            this.ChildFillsAvailableSpace = fillAvailableSpace;
        }
        // Returns a ContainerLayout that wraps a ScrollView whose content size will always match the ScrollView's size.
        // The reason someone might want this is if something may later temporarily show overtop of this view (like a keyboard) then
        // the ScrollView might scroll its content to still be visible.
        public static ContainerLayout SameSize_Scroller(ScrollView view, LayoutChoice_Set subLayout)
        {
            ContainerLayout containerLayout = new ContainerLayout();
            containerLayout.view = view;
            containerLayout.SubLayout = subLayout;
            return containerLayout;
        }

        private void Initialize()
        {
            this.ChildFillsAvailableSpace = true;
        }
        public void CopyFrom(ContainerLayout original)
        {
            this.ChildFillsAvailableSpace = original.ChildFillsAvailableSpace;
            base.CopyFrom(original);
        }
        private View view;
        protected virtual View View
        {
            get
            {
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
                if (value == this.subLayout)
                    return;
                if (this.subLayout != null)
                    this.subLayout.RemoveParent(this);
                this.subLayout = value;
                if (this.subLayout != null)
                    this.subLayout.AddParent(this);
                this.AnnounceChange(true);
            }
        }
        public LayoutScore BonusScore { get; set; }


        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            Specific_ContainerLayout result;

            LayoutQuery subQuery = query.Clone();
            if (subQuery.MaxWidth < 0 || subQuery.MaxHeight < 0)
            {
                // If there is no room for the border, then even the border would be cropped
                result = this.makeSpecificLayout(this.view, new Size(0, 0), LayoutScore.Get_CutOff_LayoutScore(1), null, new Thickness(0));
                if (query.Accepts(result))
                    return this.prepareLayoutForQuery(result, query);
                return null;
            }

            // Query sublayout if it exists
            if (this.SubLayout != null)
            {
                subQuery.MinScore = subQuery.MinScore.Minus(this.BonusScore);
                SpecificLayout best_subLayout = this.SubLayout.GetBestLayout(subQuery);
                if (best_subLayout != null)
                {
                    result = this.makeSpecificLayout(this.view, new Size(best_subLayout.Width, best_subLayout.Height), best_subLayout.Score.Plus(this.BonusScore), best_subLayout, new Size());
                    result.ChildFillsAvailableSpace = this.ChildFillsAvailableSpace;
                    this.prepareLayoutForQuery(result, query);
                    return result;
                }
                return null;
            }
            // if there is no subLayout, for now we just return an empty size
            Specific_ContainerLayout empty = this.makeSpecificLayout(this.view, new Size(), LayoutScore.Zero, null, new Thickness());
            if (query.Accepts(empty))
                result = empty;
            else
                result = null;
            this.prepareLayoutForQuery(result, query);
            return result;
        }

        protected Specific_ContainerLayout makeSpecificLayout(View view, Size size, LayoutScore score, SpecificLayout subLayout, Thickness border)
        {
            return new Specific_ContainerLayout(view, size, score, subLayout, border);
        }

        LayoutChoice_Set subLayout;
    }

}
