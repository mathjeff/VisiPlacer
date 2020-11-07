using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

// a ScoreShifted_LayoutSet is a LayoutChoice_Set that adds a specified score to its layout
namespace VisiPlacement
{
    public class ScoreShifted_Layout : ContainerLayout
    {
        public ScoreShifted_Layout(LayoutChoice_Set layoutOptions, LayoutScore scoreToAdd)
        {
            this.SubLayout = layoutOptions;
            this.BonusScore = scoreToAdd;
        }

        public LayoutScore BonusScore { get; set; }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            LayoutQuery parentQuery = query.WithScore(query.MinScore.Minus(this.BonusScore));

            SpecificLayout parentResult = base.GetBestLayout(parentQuery);
            if (parentResult == null)
                return null;
            SpecificLayout result = this.makeSpecificLayout(this.View, parentResult.Size, this.BonusScore, parentResult, new Thickness());
            this.prepareLayoutForQuery(result, query);
            return result;

        }
    }
}
