using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VisiPlacement
{
    // A ReorderableList is a list that can be reordered
    public class ReorderableList<T> : ContainerLayout
    {
        public event ReorderedItems Reordered;
        public delegate void ReorderedItems(List<T> choices);

        public ReorderableList(IEnumerable<T> items, LayoutProvider<T> layoutProvider)
        {
            // get the items and layouts
            this.Items = new List<T>(items);
            this.itemLayouts = new List<LayoutChoice_Set>();

            foreach (T item in items)
            {
                LayoutChoice_Set itemLayout = layoutProvider.GetLayout(item);
                this.itemLayouts.Add(itemLayout);
            }

            this.makeArrows();
            BoundProperty_List columnWidths = new BoundProperty_List(2);
            columnWidths.BindIndices(0, 1);
            columnWidths.SetPropertyScale(0, 1);
            columnWidths.SetPropertyScale(1, 7);
            this.mainGrid = GridLayout.New(BoundProperty_List.Uniform(this.itemLayouts.Count), columnWidths, LayoutScore.Zero);
            this.SubLayout = this.mainGrid;
            this.putLayouts();
        }

        private void makeArrows()
        {
            this.arrowLayouts = new List<LayoutChoice_Set>();
            for (int i = 0; i < this.itemLayouts.Count; i++)
            {
                GridLayout_Builder arrowBuilder = new Vertical_GridLayout_Builder().Uniform();
                if (i != 0)
                    arrowBuilder.AddLayout(this.Make_PrevButton(i));
                if (i != this.itemLayouts.Count - 1)
                    arrowBuilder.AddLayout(this.Make_NextButton(i));
                this.arrowLayouts.Add(arrowBuilder.Build());
            }
        }

        private void putLayouts()
        {
            for (int i = 0; i < this.arrowLayouts.Count; i++)
            {
                this.mainGrid.PutLayout(this.arrowLayouts[i], 0, i);
            }
            for (int i = 0; i < this.itemLayouts.Count; i++)
            {
                this.mainGrid.PutLayout(null, 1, i);
            }
            for (int i = 0; i < this.itemLayouts.Count; i++)
            {
                this.mainGrid.PutLayout(this.itemLayouts[i], 1, i);
            }
        }


        private ButtonLayout Make_PrevButton(int index)
        {
            Button button = new Button();
            this.prevButtonIndices[button] = index;
            button.Clicked += PrevArrow_Clicked;
            this.prevButtonIndices[button] = index;

            ButtonLayout buttonLayout = new ButtonLayout(button, upArrow);
            return buttonLayout;
        }

        private void PrevArrow_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            int buttonIndex = this.prevButtonIndices[button];
            this.swapWithNext(buttonIndex - 1);
        }

        private ButtonLayout Make_NextButton(int index)
        {
            Button button = new Button();
            this.nextButtonIndices[button] = index;
            button.Clicked += NextArrow_Clicked;

            ButtonLayout buttonLayout = new ButtonLayout(button, downArrow);
            return buttonLayout;
        }

        private void NextArrow_Clicked(object sender, EventArgs e)
        {
            Button button = sender as Button;
            int buttonIndex = this.nextButtonIndices[button];
            this.swapWithNext(buttonIndex);
        }

        private void swapWithNext(int index)
        {
            // swap layouts
            LayoutChoice_Set layoutA = this.itemLayouts[index];
            LayoutChoice_Set layoutB = this.itemLayouts[index + 1];
            this.itemLayouts[index] = layoutB;
            this.itemLayouts[index + 1] = layoutA;

            // swap items
            T itemA = this.Items[index];
            T itemB = this.Items[index + 1];
            this.Items[index] = itemB;
            this.Items[index + 1] = itemA;

            // update positions in grid
            this.putLayouts();

            if (this.Reordered != null)
                this.Reordered.Invoke(this.Items);
        }

        public List<T> Items { get; set; }

        private GridLayout mainGrid;
        private String upArrow = char.ConvertFromUtf32(0x2191);
        private String downArrow = char.ConvertFromUtf32(0x2193);
        private List<LayoutChoice_Set> itemLayouts;
        private List<LayoutChoice_Set> arrowLayouts;
        private Dictionary<Button, int> prevButtonIndices = new Dictionary<Button, int>();
        private Dictionary<Button, int> nextButtonIndices = new Dictionary<Button, int>();
    }

    public interface LayoutProvider<T>
    {
        LayoutChoice_Set GetLayout(T item);
    }
}
