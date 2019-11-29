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
        public LayoutStack()
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

        public void AddLayout(LayoutChoice_Set newLayout, String name)
        {
            this.AddLayout(new StackEntry(newLayout, name, null));
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
                this.backButton_layouts.RemoveAt(this.backButton_layouts.Count - 1);
                this.buttons.RemoveAt(this.buttons.Count - 1);
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
        }
        private void setSublayout(LayoutChoice_Set layout)
        {
            if (this.layoutEntries.Count > 1)
            {
                this.mainGrid.PutLayout(layout, 0, 0);
                this.mainGrid.PutLayout(this.backButtons(this.layoutEntries), 0, 2);
                this.SubLayout = this.mainGrid;
            }
            else
            {
                this.SubLayout = layout;
            }
        }
        private LayoutChoice_Set backButtons(List<StackEntry> layouts)
        {
            Horizontal_GridLayout_Builder fullBuilder = new Horizontal_GridLayout_Builder().Uniform();
            Horizontal_GridLayout_Builder abbreviatedBuilder = new Horizontal_GridLayout_Builder().Uniform();
            if (layouts.Count >= 3)
            {
                int oldestButton_index = layouts.Count - 3;
                abbreviatedBuilder.AddLayout(this.JumpBack_ButtonLayout(oldestButton_index, layouts[oldestButton_index].Name));
            }

            for (int i = 0; i < layouts.Count - 1; i++)
            {
                StackEntry entry = layouts[i];
                ButtonLayout nameLayout = this.JumpBack_ButtonLayout(i, entry.Name);
                fullBuilder.AddLayout(nameLayout);
                if (i == layouts.Count - 2)
                    abbreviatedBuilder.AddLayout(nameLayout);
            }
            return new LayoutUnion(abbreviatedBuilder.BuildAnyLayout(), fullBuilder.BuildAnyLayout());
        }
        private ButtonLayout JumpBack_ButtonLayout(int toIndex, string text)
        {
            if (toIndex >= this.backButton_layouts.Count)
            {
                Button button = new Button();
                button.Text = text;
                button.Clicked += Button_Clicked;
                this.buttons.Add(button);
                this.backButton_layouts.Add(new ButtonLayout(button));
            }
            return this.backButton_layouts[toIndex];
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
                return;
            int buttonIndex = this.buttons.IndexOf(button);
            if (buttonIndex < 0)
                return;
            this.goBackTo(buttonIndex);
        }

        private void goBackTo(int index)
        {
            while (this.layoutEntries.Count > index + 1)
                this.GoBack();
        }

        private List<StackEntry> layoutEntries = new List<StackEntry>();
        private List<Button> buttons = new List<Button>();
        private List<ButtonLayout> backButton_layouts = new List<ButtonLayout>();
        private GridLayout mainGrid;
    }

    public class StackEntry
    {
        public StackEntry(LayoutChoice_Set layout, string name, OnBack_Listener listener)
        {
            this.Layout = layout;
            if (listener != null)
                this.Listeners.AddLast(listener);
            this.Name = name;
        }

        public LayoutChoice_Set Layout;
        public LinkedList<OnBack_Listener> Listeners = new LinkedList<OnBack_Listener>();
        public string Name;
    }

    public interface OnBack_Listener
    {
        void OnBack(LayoutChoice_Set previousLayout);
    }
}
