using System;
using System.Collections.Generic;
using System.Text;
using VisiPlacement;
using Microsoft.Maui.Controls;

namespace VisiPlacement
{
    public class ViewMemoryUsageLayout : ContainerLayout
    {
        public ViewMemoryUsageLayout()
        {
            this.SubLayout = this.textBlockLayout;
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            long allocated = GC.GetTotalMemory(false);
            // format a number like 1234567 into a string like 1,234,567
            string formatted = String.Format("{0:#,0}", allocated);
            this.textBlockLayout.setText("Memory usage: " + formatted + " bytes");
            return base.GetBestLayout(query);
        }

        private TextblockLayout textBlockLayout = new TextblockLayout();
    }
}
