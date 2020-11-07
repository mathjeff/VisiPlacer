﻿using System.Collections.Generic;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class GridView : Grid
    {
        public GridView()
        {
            this.Margin = new Thickness();
            this.Padding = new Thickness();
            this.RowSpacing = 0;
            this.ColumnSpacing = 0;
        }
        public void PutView(View view, int columnNumber, int rowNumber)
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
        public void SetChildren(View[,] newChildren)
        {
            // Remove removed children, then add new children
            // This is done in two passes in case a child moved to another location
            int rowNumber, columnNumber;
            for (columnNumber = 0; columnNumber < newChildren.GetLength(0); columnNumber++)
            {
                for (rowNumber = 0; rowNumber < newChildren.GetLength(1); rowNumber++)
                {
                    View newChild = newChildren[columnNumber, rowNumber];
                    View oldChild;
                    if (this.children == null)
                        oldChild = null;
                    else
                        oldChild = this.children[columnNumber, rowNumber];
                    if (newChild != oldChild)
                    {
                        if (oldChild != null)
                            this.Children.Remove(oldChild);
                    }
                }
            }
            // add new children
            for (columnNumber = 0; columnNumber < newChildren.GetLength(0); columnNumber++)
            {
                for (rowNumber = 0; rowNumber < newChildren.GetLength(1); rowNumber++)
                {
                    View newChild = newChildren[columnNumber, rowNumber];
                    View oldChild;
                    if (this.children == null)
                        oldChild = null;
                    else
                        oldChild = this.children[columnNumber, rowNumber];
                    if (newChild != oldChild)
                    {
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
        public View GetChild(int x, int y)
        {
            return this.children[x, y];
        }
        View[,] children;

        public void Remove_VisualDescendents()
        {
            this.Children.Clear();
            if (this.children != null)
            {
                int rowNumber, columnNumber;
                for (columnNumber = 0; columnNumber < this.children.GetLength(0); columnNumber++)
                {
                    for (rowNumber = 0; rowNumber < this.children.GetLength(1); rowNumber++)
                    {
                        this.children[columnNumber, rowNumber] = null;
                    }
                }
            }
        }

        public void RemoveChild(int x, int y)
        {
            View child = this.GetChild(x, y);
            this.children[x, y] = null;
            this.Children.Remove(child);
        }
    }
}
