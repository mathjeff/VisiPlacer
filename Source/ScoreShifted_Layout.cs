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
    }
}
