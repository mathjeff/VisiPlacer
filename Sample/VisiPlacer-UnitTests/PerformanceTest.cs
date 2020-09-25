using System;
using Xamarin.Forms;
using VisiPlacement;
using Xunit;

namespace VisiPlacer_UnitTests
{
    public class PerformanceTest
    {
        public void Verify(LayoutChoice_Set layout, Size bounds, int expectedNumQueries)
        {
            ViewManager m = new ViewManager(null, null);
            m.SetLayout(layout);

            // get some basic information about which layouts will be going where
            LayoutQuery query = new MaxScore_LayoutQuery(bounds.Width, bounds.Height, LayoutScore.Minimum);
            // make sure that the layout has recursively solved for all children, too
            layout.GetBestLayout(query);
            int actualNumQueries = query.Cost;
            if (actualNumQueries != expectedNumQueries)
            {
                throw new ArgumentException("Test layout " + layout + " with bounds " + bounds + " required " + actualNumQueries + " queries, not " + expectedNumQueries);
            }
        }
    }
}
