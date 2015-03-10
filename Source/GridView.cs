using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace VisiPlacement
{
    public class GridView : Grid
    {
        public GridView()
        {
            this.Margin = new Thickness();
        }
        public void PutView(FrameworkElement view, int columnNumber, int rowNumber)
        {
            if (view != null)
            {
                this.children[columnNumber, rowNumber] = view;
                this.Children.Add(view);
                Grid.SetColumn(view, columnNumber);
                Grid.SetRow(view, rowNumber);
                this.InvalidateMeasure();
            }
        }

        // provides new views to use
        public void SetChildren(FrameworkElement[,] newChildren)
        {
            int rowNumber, columnNumber;
            for (columnNumber = 0; columnNumber < newChildren.GetLength(0); columnNumber++)
            {
                for (rowNumber = 0; rowNumber < newChildren.GetLength(1); rowNumber++)
                {
                    FrameworkElement newChild = newChildren[columnNumber, rowNumber];
                    FrameworkElement oldChild;
                    if (this.children == null)
                        oldChild = null;
                    else
                        oldChild = this.children[columnNumber, rowNumber];
                    if (newChild != oldChild)
                    {
                        if (oldChild != null)
                            this.Children.Remove(oldChild);
                        if (newChild != null)
                        {
                            this.Children.Add(newChild);
                            Grid.SetColumn(newChild, columnNumber);
                            Grid.SetRow(newChild, rowNumber);
                        }
                    }
                }
            }
            this.InvalidateMeasure();
            this.children = newChildren;

        }

        public void SetDimensions(IEnumerable<double> columnWidths, IEnumerable<double> rowHeights)
        {

            this.RowDefinitions.Clear();

            foreach (double rowHeight in rowHeights)
            {
                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(rowHeight);
                this.RowDefinitions.Add(rowDefinition);
            }

            this.ColumnDefinitions.Clear();
            foreach (double columnWidth in columnWidths)
            {
                ColumnDefinition columnDefinition = new ColumnDefinition();
                columnDefinition.Width = new GridLength(columnWidth);
                this.ColumnDefinitions.Add(columnDefinition);
            }
        }
        FrameworkElement[,] children;

        public void Remove_VisualDescendents()
        {
            this.Children.Clear();
            int rowNumber, columnNumber;
            for (columnNumber = 0; columnNumber < this.children.GetLength(0); columnNumber++)
            {
                for (rowNumber = 0; rowNumber < this.children.GetLength(1); rowNumber++)
                {
                    this.children[columnNumber, rowNumber] = null;
                }
            }
        }

        /*protected override void OnChildDesiredSizeChanged(UIElement child)
        {
            this.InvalidateMeasure();
        }*/
    }
}
