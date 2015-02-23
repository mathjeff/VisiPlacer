using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisiPlacement;

namespace VisiPlacement
{
    public class LayoutStack : SingleItem_Layout
    {
        public LayoutStack()
        {
        }
        public void AddLayout(LayoutChoice_Set newLayout)
        {
            this.layouts.AddLast(newLayout);
            this.updateSubLayout();
        }
        public bool GoBack()
        {
            if (this.layouts.Count > 1)
            {
                this.layouts.RemoveLast();
                this.updateSubLayout();
                return true;
            }
            return false;
        }
        public void RemoveLayout()
        {
            this.layouts.RemoveLast();
            this.updateSubLayout();
        }
        private void updateSubLayout()
        {
            if (this.layouts.Count > 0)
            {
                base.SubLayout = this.layouts.Last();
            }
            else
            {
                base.SubLayout = null;
            }
        }
        private LinkedList<LayoutChoice_Set> layouts = new LinkedList<LayoutChoice_Set>();
    }
}
