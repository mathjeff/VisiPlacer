using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
#if true
            throw new NotImplementedException();
#else

            query.MinScore = query.MinScore.Minus(this.scoreToAdd);
            SpecificLayout result = this.layoutOptions.GetBestLayout(query);
            if (result != null)
                result.Score = result.Score.Plus(this.scoreToAdd);
            return result;
#endif
        }
        private LayoutChoice_Set layoutOptions;
        private LayoutScore scoreToAdd;
    }
}
