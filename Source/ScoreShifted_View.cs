using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

// a ScoreShifted_LayoutSet is a LayoutChoice_Set that adds a specified score to its layout
namespace VisiPlacement
{
    class ScoreShifted_LayoutSet : LayoutChoice_Set
    {
        public ScoreShifted_LayoutSet(LayoutChoice_Set layoutOptions, LayoutScore scoreToAdd)
        {
            this.layoutOptions = layoutOptions;
            this.scoreToAdd = scoreToAdd;
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            LayoutQuery subQuery = query.Clone();
            subQuery.MinScore = query.MinScore.Minus(this.scoreToAdd);
            SpecificLayout result = this.layoutOptions.GetBestLayout(subQuery);
            if (result != null)
                result = new Specific_ContainerLayout(null, result.Size, result.Score.Plus(this.scoreToAdd), result, new Thickness(0));
            return this.prepareLayoutForQuery(result, query);
        }

        private LayoutChoice_Set layoutOptions;
        private LayoutScore scoreToAdd;
    }
}
