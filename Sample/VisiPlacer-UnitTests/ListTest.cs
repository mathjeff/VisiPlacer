using System;
using System.Collections.Generic;
using System.Text;
using VisiPlacement;
using Xunit;

namespace VisiPlacer_UnitTests
{
    public class ListTest : PerformanceTest
    {
        [Fact]
        public void testList()
        {
            Vertical_GridLayout_Builder builder = new Vertical_GridLayout_Builder();
            for (int i = 0; i < 20; i++)
            {
                builder.AddLayout(new TextblockLayout("Sample text " + i, 16));
            }
            this.Verify(builder.Build(), new Xamarin.Forms.Size(400, 1000), 1784);
        }

    }
}
