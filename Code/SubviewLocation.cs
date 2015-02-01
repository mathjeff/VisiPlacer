using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

// a subLayoutLocation tells where inside another view to place a child view
namespace VisiPlacement
{
    public class SubviewDimensions
    {
        public SubviewDimensions(SpecificLayout subLayout, Size size)
        {
            this.subLayout = subLayout;
            this.size = size;
        }
        public SpecificLayout SubLayout
        {
            get
            {
                return this.subLayout;
            }
        }
        public Size Size
        {
            get
            {
                return this.size;
            }
        }
        private SpecificLayout subLayout;
        private Size size;
    }
}
