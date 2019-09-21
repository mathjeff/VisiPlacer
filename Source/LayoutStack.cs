using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisiPlacement;
using Xamarin.Forms;

// A LayoutStack is a stack of layouts that shows the top one at a time and supports going back
namespace VisiPlacement
{
    public class LayoutStack : ContainerLayout
    {
        public LayoutStack(bool addBackButton = false)
        {
            this.addBackButton = addBackButton;
            if (addBackButton)
            {
                Button backButton = new Button();
                this.backButtonLayout = new ButtonLayout(backButton, "<-");
                backButton.Pressed += BackButton_Pressed;
            }
        }

        private void BackButton_Pressed(object sender, EventArgs e)
        {
            this.GoBack();
        }

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            this.GoBack();
        }

        public void AddLayout(LayoutChoice_Set newLayout)
        {
            StackEntry entry = new StackEntry();
            entry.layout = newLayout;
            this.layouts.AddLast(entry);
            this.updateSubLayout();
        }
        public void AddLayout(LayoutChoice_Set newLayout, OnBack_Listener listener)
        {
            StackEntry entry = new StackEntry();
            entry.layout = newLayout;
            entry.listeners.AddLast(listener);
            this.AddEntry(entry);
        }
        public void AddEntry(StackEntry entry)
        {
            this.layouts.AddLast(entry);
            this.updateSubLayout();
        }
        public bool GoBack()
        {
            return this.RemoveLayout();
        }
        public bool RemoveLayout()
        {
            if (this.layouts.Count > 1)
            {
                StackEntry entry = this.layouts.Last();
                this.layouts.RemoveLast();
                foreach (OnBack_Listener listener in entry.listeners)
                {
                    listener.OnBack(entry.layout);
                }
                this.updateSubLayout();
                return true;
            }
            return false;
        }
        public bool Contains(LayoutChoice_Set layout)
        {
            foreach (StackEntry entry in this.layouts)
            {
                if (entry.layout == layout)
                    return true;
            }
            return false;
        }
        private void updateSubLayout()
        {
            if (this.layouts.Count > 0)
            {
                this.setSublayout(this.layouts.Last().layout);
            }
            else
            {
                this.setSublayout(null);
            }
        }
        private void setSublayout(LayoutChoice_Set layout)
        {
            if (this.addBackButton)
            {
                this.SubLayout = new Vertical_GridLayout_Builder().AddLayout(layout).AddLayout(this.backButtonLayout).BuildAnyLayout();
            }
            else
            {
                this.SubLayout = layout;
            }
        }
        private LinkedList<StackEntry> layouts = new LinkedList<StackEntry>();
        private bool addBackButton;
        private LayoutChoice_Set backButtonLayout;
    }

    public class StackEntry
    {
        public StackEntry()
        {
        }
        public StackEntry(LayoutChoice_Set layout)
        {
            this.layout = layout;
        }
        public StackEntry(LayoutChoice_Set layout, OnBack_Listener listener)
        {
            this.layout = layout;
            this.listeners.AddLast(listener);
        }

        public LayoutChoice_Set layout;
        public LinkedList<OnBack_Listener> listeners = new LinkedList<OnBack_Listener>();

    }

    public interface OnBack_Listener
    {
        void OnBack(LayoutChoice_Set previousLayout);
    }
}
