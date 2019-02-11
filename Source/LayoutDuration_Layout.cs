using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class LayoutDuration_Layout : ContainerLayout
    {
        public LayoutDuration_Layout(ViewManager viewManager)
        {
            this.textBlock = new Label();
            TextblockLayout textblockLayout = new TextblockLayout(this.textBlock, 10);
            textblockLayout.ScoreIfEmpty = true;
            this.SubLayout = textblockLayout;

            viewManager.LayoutCompleted += this.Update;
        }

        private void Update(ViewManager_LayoutStats stats)
        {
            this.textBlock.Text = "(Planned layout in " + Math.Round(stats.ViewManager_getBestLayout_Duration.TotalSeconds, 1)
                + "s. Completed layout in " + Math.Round(stats.ViewManager_LayoutDuration.TotalSeconds, 1) + "s)";
        }

        public override void AnnounceChange(bool mustRedraw)
        {
            // Never require a redraw when something about a LayoutDuration_Layout changes, because it always changes and it's only just a minor text change
            base.AnnounceChange(false);
        }

        private Label textBlock;
    }
}
