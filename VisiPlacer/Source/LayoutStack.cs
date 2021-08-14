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

        public event OnNavigate Navigated;
        public delegate void OnNavigate();
        public LayoutStack(bool showBackButtons = true)
        {
            this.showBackButtons = showBackButtons;
            if (showBackButtons)
            {
                BoundProperty_List rowHeights = new BoundProperty_List(3);
                rowHeights.BindIndices(0, 1);
                rowHeights.BindIndices(0, 2);
                rowHeights.SetPropertyScale(0, 28);
                rowHeights.SetPropertyScale(1, 1); // extra unused space for visual separation
                rowHeights.SetPropertyScale(2, 3);
                this.mainGrid = GridLayout.New(rowHeights, new BoundProperty_List(1), LayoutScore.Zero);
                this.SubLayout = this.mainGrid;
            }
        }



        public void AddLayout(LayoutChoice_Set newLayout, String name, int backPriority = 1)
        {
            this.AddLayout(new StackEntry(newLayout, name, null, backPriority));
        }
        public void AddLayout(LayoutChoice_Set newLayout, String name, OnBack_Listener listener)
        {
            this.AddLayout(new StackEntry(newLayout, name, listener));
        }
        public void AddLayout(StackEntry entry)
        {
            this.layoutEntries.Add(entry);
            this.updateSubLayout();
        }
        public bool GoBack()
        {
            return this.RemoveLayout();
        }
        public bool RemoveLayout()
        {
            if (this.layoutEntries.Count > 1)
            {
                StackEntry entry = this.layoutEntries.Last();
                this.layoutEntries.RemoveAt(this.layoutEntries.Count - 1);
                foreach (OnBack_Listener listener in entry.Listeners)
                {
                    listener.OnBack(entry.Layout);
                }
                this.updateSubLayout();
                return true;
            }
            return false;
        }
        public bool Contains(LayoutChoice_Set layout)
        {
            foreach (StackEntry entry in this.layoutEntries)
            {
                if (entry.Layout == layout)
                    return true;
            }
            return false;
        }
        private void updateSubLayout()
        {
            if (this.layoutEntries.Count > 0)
            {
                this.setSublayout(this.layoutEntries.Last().Layout);
            }
            else
            {
                this.setSublayout(null);
            }
            if (this.Navigated != null)
                this.Navigated.Invoke();
        }
        private void setSublayout(LayoutChoice_Set layout)
        {
            if (this.showBackButtons && this.layoutEntries.Count > 1)
            {
                this.mainGrid.PutLayout(layout, 0, 0);
                this.mainGrid.PutLayout(this.makeBackButtons(this.layoutEntries), 0, 2);
                this.SubLayout = this.mainGrid;
            }
            else
            {
                this.SubLayout = layout;
            }
        }
        private LayoutChoice_Set makeBackButtons(List<StackEntry> layouts)
        {
            List<StackEntry> prevLayouts = layouts.GetRange(0, layouts.Count - 1);

            int maxPriority = -1000;
            foreach (StackEntry entry in prevLayouts)
            {
                maxPriority = Math.Max(maxPriority, entry.BackPriority);
            }
            List<StackEntry> interestingPrevLayouts = new List<StackEntry>();
            foreach (StackEntry entry in prevLayouts)
            {
                if (entry.BackPriority == maxPriority)
                    interestingPrevLayouts.Add(entry);
            }

            GridLayout_Builder fullBuilder = new Horizontal_GridLayout_Builder().Uniform();
            foreach (StackEntry entry in interestingPrevLayouts)
            {
                fullBuilder.AddLayout(this.JumpBack_ButtonLayout(entry));
            }
            if (interestingPrevLayouts.Count <= 2)
            {
                // If there are 2 or fewer previous layouts, then we show all of them
                return fullBuilder.BuildAnyLayout();
            }
            GridLayout_Builder abbreviatedBuilder = new Horizontal_GridLayout_Builder().Uniform();
            abbreviatedBuilder.AddLayout(this.JumpBack_ButtonLayout(interestingPrevLayouts[interestingPrevLayouts.Count - 2]));
            abbreviatedBuilder.AddLayout(this.JumpBack_ButtonLayout(interestingPrevLayouts[interestingPrevLayouts.Count - 1]));
            return new LayoutUnion(abbreviatedBuilder.BuildAnyLayout(), fullBuilder.BuildAnyLayout());
        }
        private LayoutChoice_Set JumpBack_ButtonLayout(StackEntry stackEntry)
        {
            int toIndex = -1;
            for (int i = 0; i < this.layoutEntries.Count; i++)
            {
                if (this.layoutEntries[i] == stackEntry)
                {
                    toIndex = i;
                    break;
                }
            }

            if (toIndex >= this.backButton_layouts.Count)
            {
                Button newButton = new Button();
                newButton.Clicked += Button_Clicked;
                this.backButtons.Add(newButton);
                this.backButton_layouts.Add(LayoutCache.For(new ButtonLayout(newButton)));
            }
            Button button = this.backButtons[toIndex];
            button.Text = "Back: " + stackEntry.Name;
            return this.backButton_layouts[toIndex];
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
                return;
            int buttonIndex = this.backButtons.IndexOf(button);
            if (buttonIndex < 0)
                return;
            this.goBackTo(buttonIndex);
        }

        private void goBackTo(int index)
        {
            while (this.layoutEntries.Count > index + 1)
                this.GoBack();
        }

        // list of entries in the stack, with the last entry in the list being the currently visible entry
        private List<StackEntry> layoutEntries = new List<StackEntry>();
        // List of buttons that can be pressed to go back. May contain more buttons than necessary, in case we want to repurpose them later
        private List<Button> backButtons = new List<Button>();
        // List of layouts that can be pressed to go back. May contain more buttons than necessary, in case we want to repurpose them later
        private List<LayoutChoice_Set> backButton_layouts = new List<LayoutChoice_Set>();
        private GridLayout mainGrid;
        private bool showBackButtons;
    }

    public class StackEntry
    {
        public StackEntry(LayoutChoice_Set layout, string name, OnBack_Listener listener, int backPriority = 1)
        {
            this.Layout = layout;
            if (listener != null)
                this.Listeners.Add(listener);
            this.Name = name;
            this.BackPriority = backPriority;
        }

        public LayoutChoice_Set Layout;
        public List<OnBack_Listener> Listeners = new List<OnBack_Listener>();
        public string Name;
        public int BackPriority;
    }

    public interface OnBack_Listener
    {
        void OnBack(LayoutChoice_Set previousLayout);
    }
}
