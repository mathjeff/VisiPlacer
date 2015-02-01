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
                this.Children.Add(view);
                Grid.SetColumn(view, columnNumber);
                Grid.SetRow(view, rowNumber);
                this.InvalidateMeasure();
            }
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

        public void Remove_VisualDescendents()
        {
            this.Children.Clear();
        }

        /*protected override void OnChildDesiredSizeChanged(UIElement child)
        {
            this.InvalidateMeasure();
        }*/
    }
}
