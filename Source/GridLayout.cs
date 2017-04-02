using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.Foundation;


// the GridLayout will arrange the child elements in a grid pattern
namespace VisiPlacement
{
    public class GridLayout : LayoutChoice_Set
    {
        public static int NumQueries = 0;
        public static int NumComputations = 0;

        public static GridLayout New(BoundProperty_List rowHeights, BoundProperty_List columnWidths, LayoutScore bonusScore)
        {
            if (Math.Min(rowHeights.NumGroups, columnWidths.NumGroups) == 1 && (Math.Max(rowHeights.NumGroups, columnWidths.NumGroups) > 2))
            {
                // we should instead make a smaller grid with additional grids inside it, to better enable caching
                if (rowHeights.NumGroups == rowHeights.NumProperties && columnWidths.NumGroups == columnWidths.NumProperties)
                {
                    // we do support composition from smaller grids in this case
                    return new CompositeGridLayout(rowHeights.NumGroups, columnWidths.NumGroups, bonusScore);
                }
                // not yet implemented
            }
            // can't compose from smaller grids
            return new GridLayout(rowHeights, columnWidths, bonusScore);
        }

        public GridLayout()
        {
        }

        private GridLayout(BoundProperty_List rowHeights, BoundProperty_List columnWidths, LayoutScore bonusScore)
        {
            this.Initialize(rowHeights, columnWidths, bonusScore);
        }
        protected void Initialize(BoundProperty_List rowHeights, BoundProperty_List columnWidths, LayoutScore bonusScore)
        {
            int numColumns = columnWidths.NumProperties;
            int numRows = rowHeights.NumProperties;
            this.elements = new LayoutChoice_Set[numColumns, numRows];
            this.rowHeights = rowHeights;
            this.columnWidths = columnWidths;
            this.bonusScore = bonusScore;
            //this.pixelSize = (double)1/(double)128;
            this.pixelSize = 1;
            this.view = new GridView();
        }
        // puts this layout in the designated part of the grid
        public virtual void PutLayout(LayoutChoice_Set layout, int xIndex, int yIndex)
        {
            LayoutChoice_Set previousLayout = this.elements[xIndex, yIndex];
            if (previousLayout != null)
                previousLayout.RemoveParent(this);

            if (layout != null)
            {
                if (!(layout is LayoutCache))
                    layout = new LayoutCache(layout);
                layout = new PixelatedLayout(layout, this.pixelSize);
                layout.AddParent(this);
            }
            this.elements[xIndex, yIndex] = layout;
            this.AnnounceChange(true);
        }

        // put a view into the next available spot in the grid
        public void AddLayout(LayoutChoice_Set newLayout)
        {
            this.FindNextOpenLocation();
            this.PutLayout(newLayout, this.nextOpenColumn, this.nextOpenRow);
        }

        // sets this.nextOpenRow and this.nextOpenColumn
        private void FindNextOpenLocation()
        {
            int row = this.nextOpenRow;
            int column = this.nextOpenColumn;
            for (row = this.nextOpenRow; row < this.NumRows; row++)
            {
                for (; column < this.NumColumns; column++)
                {
                    if (this.GetLayout(column, row) == null)
                    {
                        this.nextOpenRow = row;
                        this.nextOpenColumn = column;
                        return;
                    }
                }
                column = 0;
            }
        }
        public virtual LayoutChoice_Set GetLayout(int xIndex, int yIndex)
        {
            return this.elements[xIndex, yIndex];
        }
        public virtual int NumRows
        {
            get
            {
                return this.elements.GetLength(1);
            }
        }
        public virtual int NumColumns
        {
            get
            {
                return this.elements.GetLength(0);
            }
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            GridLayout.NumQueries++;
            bool setWidthBeforeHeight;
            if (query.MinimizesWidth())
            {
                if (this.rowHeights.NumGroups <= 1)
                    setWidthBeforeHeight = false;
                else
                    setWidthBeforeHeight = true;
            }
            else
            {
                if (query.MinimizesHeight())
                {
                    if (this.columnWidths.NumGroups <= 1)
                        setWidthBeforeHeight = true;
                    else
                        setWidthBeforeHeight = false;
                }
                else
                {
                    // query maximizes score
                    if (this.rowHeights.NumGroups > this.columnWidths.NumGroups)
                        setWidthBeforeHeight = true;
                    else
                        setWidthBeforeHeight = false;
                }
            }
            SemiFixed_GridLayout layout = this.GetBestLayout(query, new SemiFixed_GridLayout(this.elements, this.rowHeights, this.columnWidths, this.bonusScore, setWidthBeforeHeight));
            if (layout != null && !query.Accepts(layout))
            {
                System.Diagnostics.Debug.WriteLine("error");
            }
            if (query.MaximizesScore())
            {
                SemiFixed_GridLayout shrunken = null;
                if (layout != null)
                {
                    shrunken = this.ShrinkLayout(new SemiFixed_GridLayout(layout), query.Debug);
                }
                if (query.PreferredLayout(shrunken, layout) != shrunken)
                {
                    System.Diagnostics.Debug.WriteLine("error");
                }

                layout = shrunken;
            }

            if (layout != null)
                layout.GridView = this.view; // reuse the same view to decrease the amount of redrawing the caller has to do

            return this.prepareLayoutForQuery(layout, query);
        }

#if false
        // This function is broken. The problem is that when increasing the width of some columns, it's possible that the best set of heights of the rows will change
        // This is much harder to detect here than when shrinking the widths
        private SemiFixed_GridLayout Get_MinWidth_Layout(LayoutQuery query, SemiFixed_GridLayout semiFixedLayout)
        {
            throw new NotSupportedException("this function has a bug in it");
            // round down because the layout is pixelized
            //query.MaxWidth = Math.Floor(query.MaxWidth);
            //query.MaxHeight = Math.Floor(query.MaxHeight);

            if (semiFixedLayout.HasUnforcedDimensions == false)
            {
                // we've finally pinned a value to each coordinate; now we return the layout if it satisfies the criteria
                if (query.Accepts(semiFixedLayout))
                    return semiFixedLayout;
                else
                    return null;
            }
            // if the current coordinate does not affect the width, then delegate to the max-score case
            if (semiFixedLayout.NextCoordinateAffectsWidth == false)
            {
                LayoutQuery scoreQuery = new MaxScore_LayoutQuery();
                scoreQuery.MinScore = query.MinScore;
                scoreQuery.MaxWidth = query.MaxWidth;
                scoreQuery.MaxHeight = query.MaxHeight;
                return this.GetBestLayout(scoreQuery, semiFixedLayout);
            }
            // We haven't yet set a value for each dimension, so we should try a bunch of values for this next dimension
            //double maxValue = semiFixedLayout.GetNextCoordinate_UpperBound_ForQuery(query);
            double currentCoordinate = 0;

            // set current width to zero
            // set other widths to max
            // while not done:
            //   increase current width to bring score high enough
            //   decrease other widths as much as allowed

            SemiFixed_GridLayout bestSublayout = null;
            SemiFixed_GridLayout currentSublayout = null;
            bool decreaseWidth = true;
            while (currentCoordinate <= query.MaxWidth - semiFixedLayout.ColumnWidths.GetTotalValue())
            {
                if (decreaseWidth)
                {
                    // if we get here, we must find the minimum width that can be assigned for the next dimension that will raise the score enough
                    LayoutQuery nextQuery;
                    nextQuery = new MinWidth_LayoutQuery();
                    nextQuery.MinScore = query.MinScore;
                    // bring the score to the required value and minimize the width
                    nextQuery.MaxWidth = query.MaxWidth;
                    nextQuery.MaxHeight = query.MaxHeight;


                    SemiFixed_GridLayout currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                    // start by setting the maximum coordinate, and decrease it until it gets to zero
                    currentLayout.AddCoordinate(currentCoordinate);

                    // recursively query the next dimension
                    currentSublayout = this.GetBestLayout(nextQuery, currentLayout);

                    // if we can't raise the score high enough using the next dimension, then we must increase the current dimension next
                    // However, to know by how much to increase the current dimension, we will need to have the best possible score for the next dimension
                    if (currentSublayout == null)
                    {
                        LayoutQuery nextQuery2 = new MaxScore_LayoutQuery();
                        nextQuery2.MaxWidth = query.MaxWidth;
                        nextQuery2.MaxHeight = query.MaxHeight;

                        currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                        currentLayout.AddCoordinate(currentCoordinate);

                        currentSublayout = this.GetBestLayout(nextQuery2, currentLayout);

                        if (currentSublayout == null)
                        {
                            System.Diagnostics.Debug.WriteLine("error");
                        }
                    }
                }
                else
                {
                    // if we get here, we wish to increase the score of the layout by increasing the current coordinate
                    int index = semiFixedLayout.NumCoordinatesSetInCurrentDimension;
                    LayoutScore minScore = query.MinScore;
                    // make sure that the score actually increases
                    if (query.MinScore.CompareTo(currentSublayout.Dimensions.Score) <= 0)
                        minScore = currentSublayout.Dimensions.Score.Plus(LayoutScore.Tiny);
                    SemiFixed_GridLayout newLayout = this.IncreaseWidth(minScore, currentSublayout, index, query.MaxWidth);
                    // if we can't increase the current coordinate any more, then we're done
                    if (newLayout == null)
                        break;
                    currentSublayout = newLayout;
                    currentCoordinate = currentSublayout.ColumnWidths.Get_GroupTotal(index);
                }
                // choose the best layout so far
                if (query.PreferredLayout(currentSublayout, bestSublayout) == currentSublayout)
                    bestSublayout = new SemiFixed_GridLayout(currentSublayout);
                if (bestSublayout != null && query.Accepts(bestSublayout))
                {
                    // if we've found a layout that works, then any future layouts we find must be at least as good as this one
                    query.OptimizeUsingExample(bestSublayout.Dimensions);
                }
                decreaseWidth = !decreaseWidth;
            }
            return bestSublayout;
        }





        // This function is broken. The problem is that when increasing the height of some rows, it's possible that the best set of widths of the columns will change
        // This is much harder to detect here than when shrinking the height
        private SemiFixed_GridLayout Get_MinHeight_Layout(LayoutQuery query, SemiFixed_GridLayout semiFixedLayout)
        {
            throw new NotSupportedException("this function has a bug in it");
            if (semiFixedLayout.HasUnforcedDimensions == false)
            {
                // we've finally pinned a value to each coordinate; now we return the layout if it satisfies the criteria
                if (query.Accepts(semiFixedLayout))
                    return semiFixedLayout;
                else
                    return null;
            }
            // if the current coordinate does not affect the height, then delegate to the max-score case
            if (semiFixedLayout.NextCoordinateAffectsWidth)
            {
                LayoutQuery scoreQuery = new MaxScore_LayoutQuery();
                scoreQuery.MinScore = query.MinScore;
                scoreQuery.MaxWidth = query.MaxWidth;
                scoreQuery.MaxHeight = query.MaxHeight;
                return this.GetBestLayout(scoreQuery, semiFixedLayout);
            }
            // We haven't yet set a value for each dimension, so we should try a bunch of values for this next dimension
            double currentCoordinate = 0;


            SemiFixed_GridLayout bestSublayout = null;
            SemiFixed_GridLayout currentSublayout = null;
            bool decreaseHeight = true;
            while (currentCoordinate <= query.MaxHeight - semiFixedLayout.RowHeights.GetTotalValue())
            {
                if (decreaseHeight)
                {
                    // if we get here, we must find the minimum height that can be assigned for the next dimension that will raise the score enough
                    LayoutQuery nextQuery;
                    nextQuery = new MinHeight_LayoutQuery();
                    nextQuery.MinScore = query.MinScore;
                    // bring the score to the required value and minimize the height
                    nextQuery.MaxWidth = query.MaxWidth;
                    nextQuery.MaxHeight = query.MaxHeight;


                    SemiFixed_GridLayout currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                    currentLayout.AddCoordinate(currentCoordinate);

                    // recursively query the next dimension
                    currentSublayout = this.GetBestLayout(nextQuery, currentLayout);

                    // if we can't raise the score high enough using the next dimension, then we must increase the current dimension next
                    // However, to know by how much to increase the current dimension, we will need to have the best possible score for the next dimension
                    if (currentSublayout == null)
                    {
                        LayoutQuery nextQuery2 = new MaxScore_LayoutQuery();
                        nextQuery2.MaxWidth = query.MaxWidth;
                        nextQuery2.MaxHeight = query.MaxHeight;

                        currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                        // start by setting the maximum coordinate, and decrease it until it gets to zero
                        currentLayout.AddCoordinate(currentCoordinate);

                        currentSublayout = this.GetBestLayout(nextQuery2, currentLayout);

                        if (currentSublayout == null)
                        {
                            System.Diagnostics.Debug.WriteLine("error");
                        }
                    }
                }
                else
                {
                    // if we get here, we wish to increase the score of the layout by increasing the current coordinate
                    int index = semiFixedLayout.NumCoordinatesSetInCurrentDimension;
                    LayoutScore minScore = query.MinScore;
                    // make sure that the score actually increases
                    if (query.MinScore.CompareTo(currentSublayout.Dimensions.Score) <= 0)
                        minScore = currentSublayout.Dimensions.Score.Plus(LayoutScore.Tiny);
                    SemiFixed_GridLayout newLayout = this.IncreaseHeight(minScore, currentSublayout, index, query.MaxHeight);
                    // if we can't increase the current coordinate any more, then we're done
                    if (newLayout == null)
                        break;
                    currentSublayout = newLayout;
                    currentCoordinate = currentSublayout.RowHeights.Get_GroupTotal(index);
                }
                // choose the best layout so far
                if (query.PreferredLayout(currentSublayout, bestSublayout) == currentSublayout)
                    bestSublayout = new SemiFixed_GridLayout(currentSublayout);
                if (bestSublayout != null && query.Accepts(bestSublayout))
                {
                    // if we've found a layout that works, then any future layouts we find must be at least as good as this one
                    query.OptimizeUsingExample(bestSublayout.Dimensions);
                }
                decreaseHeight = !decreaseHeight;
            }
            return bestSublayout;


        }


#endif

#if false

        // It might be nice to revamp the GetBestLayout function so that it processes constraints in different orders based on the query
        private SemiFixed_GridLayout Get_MaxScore_Layout(LayoutQuery query, SemiFixed_GridLayout semiFixedLayout)
        {
            // round down because the layout is pixelized
            if (semiFixedLayout.HasUnforcedDimensions == false)
            {
                // we've finally pinned a value to each coordinate; now we return the layout if it satisfies the criteria

                if (query.Accepts(semiFixedLayout))
                    return semiFixedLayout;
                else
                    return null;
            }
            // We haven't yet set a value for each dimension, so we should try a bunch of values for this next dimension
            double maxValue = semiFixedLayout.GetNextCoordinate_UpperBound_ForQuery(query);
            double currentCoordinate = maxValue;

            // If this dimension does not affect this query, then just set it to maximum and move on
            if (semiFixedLayout.NumUnsetCoordinatesInCurrentDimension == 1)
            {
                semiFixedLayout.AddCoordinate(currentCoordinate);
                return this.GetBestLayout(query, semiFixedLayout);
            }

            
            SemiFixed_GridLayout bestSublayout = null;
            SemiFixed_GridLayout currentSublayout = null;
            bool increaseScore = true;
            while (currentCoordinate >= 0)
            {
                LayoutQuery nextQuery = null;
                if (increaseScore)
                {
                    // if we get here, we must increase the score of the layout without increasing the size too much
                    LayoutQuery subQuery;
                    if (semiFixedLayout.NextCoordinateAffectsWidth)
                        subQuery = new MinWidth_LayoutQuery();
                    else
                        subQuery = new MinHeight_LayoutQuery();

                    // if we already have one solution and we are trying to maximize the score, then we only care about solutions that improve beyond that
                    subQuery.MinScore = query.MinScore;
                    if (bestSublayout != null)
                        subQuery.MinScore = subQuery.MinScore.Plus(LayoutScore.Tiny);
                    if (semiFixedLayout.NextCoordinateAffectsWidth)
                    {
                        // bring the score to the required value and minimize the width
                        subQuery.MaxWidth = currentCoordinate + query.MaxWidth;
                        subQuery.MaxHeight = query.MaxHeight;
                        nextQuery = subQuery;
                    }
                    else
                    {
                        // bring the score to the required value and minimize the height
                        subQuery.MaxWidth = query.MaxWidth;
                        subQuery.MaxHeight = currentCoordinate + query.MaxHeight;
                        nextQuery = subQuery;
                    }


                    SemiFixed_GridLayout currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                    // start by setting the maximum coordinate, and decrease it until it gets to zero
                    currentLayout.AddCoordinate(currentCoordinate);

                    // recursively query the next dimension
                    currentSublayout = this.GetBestLayout(nextQuery, currentLayout);

                    if (currentSublayout == null)
                        break;



                    if (query.Accepts(currentSublayout))
                    {
                        // maximize the score while keeping the size to the required value
                        subQuery = new MaxScore_LayoutQuery();
                        subQuery.MaxWidth = query.MaxWidth;
                        subQuery.MaxHeight = query.MaxHeight;
                        subQuery.MinScore = query.MinScore;
                        nextQuery = subQuery;



                        currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                        currentLayout.AddCoordinate(currentCoordinate);

                        SemiFixed_GridLayout oldLayout = currentSublayout;

                        // recursively query the next dimension
                        currentSublayout = this.GetBestLayout(nextQuery, currentLayout);
                        if (currentSublayout == null)
                        {
                            // This case only happens when there is rounding error, in which case the previous result is fine
                            currentSublayout = oldLayout;
                        }
                    }
                }
                else
                {
                    if (currentCoordinate < this.pixelSize)
                        break;

                    // if we get here, we wish to decrease the size of the layout
                    if (semiFixedLayout.NextCoordinateAffectsWidth)
                    {
                        int index = semiFixedLayout.NumCoordinatesSetInCurrentDimension;
                        // shrink the width without decreasing the score any more than needed
                        double maxWidth = Math.Min(currentCoordinate - this.pixelSize, currentSublayout.Get_GroupWidth(index) - (currentSublayout.Width - query.MaxWidth));
                        this.ShrinkWidth(currentSublayout, index, query);
                        if (currentSublayout.ColumnWidths.Get_GroupTotal(index) > maxWidth)
                            currentSublayout.ColumnWidths.Set_GroupTotal(index, maxWidth);
                        currentCoordinate = currentSublayout.ColumnWidths.Get_GroupTotal(index);
                        
                        currentSublayout.ColumnWidths.Set_GroupTotal(index, currentCoordinate);
                    }
                    else
                    {
                        int index = semiFixedLayout.NumCoordinatesSetInCurrentDimension;
                        // shrink the height without decreasing the score any more than needed
                        double maxHeight = Math.Min(currentCoordinate - this.pixelSize, currentSublayout.Get_GroupHeight(index) - (currentSublayout.Height - query.MaxHeight));
                        this.ShrinkHeight(currentSublayout, index, query);
                        if (currentSublayout.RowHeights.Get_GroupTotal(index) > maxHeight)
                            currentSublayout.RowHeights.Set_GroupTotal(index, maxHeight);

                        currentCoordinate = currentSublayout.RowHeights.Get_GroupTotal(index);

                        currentSublayout.RowHeights.Set_GroupTotal(index, currentCoordinate);
                    }
                }
                // choose the best layout so far
                if (query.PreferredLayout(currentSublayout, bestSublayout) == currentSublayout)
                    bestSublayout = new SemiFixed_GridLayout(currentSublayout);
                if (bestSublayout != null && query.Accepts(bestSublayout))
                {
                    // if we've found a layout that works, then any future layouts we find must be at least as good as this one
                    query.OptimizeUsingExample(bestSublayout);
                }
                increaseScore = !increaseScore;
            }
            return bestSublayout;
        }

#endif
        // given some constraints and coordinates, returns a list of coordinates that are worth considering
        private IEnumerable<SemiFixed_GridLayout> GetLayoutsToConsider(LayoutQuery query, SemiFixed_GridLayout semiFixedLayout)
        {
            LinkedList<SemiFixed_GridLayout> results = new LinkedList<SemiFixed_GridLayout>();
            // round down because the layout is pixelized
            if (semiFixedLayout.HasUnforcedDimensions == false)
            {
                // we've finally pinned a value to each coordinate; now we return the layout if it satisfies the criteria

                if (query.Accepts(semiFixedLayout))
                    results.AddLast(semiFixedLayout);
                return results;
            }
            // We haven't yet set a value for each dimension, so we should try a bunch of values for this next dimension
            double maxValue = semiFixedLayout.GetNextCoordinate_UpperBound_ForQuery(query);
            double currentCoordinate = maxValue;
            if (currentCoordinate == 0)
            {
                // no space remaining for the new coordinate, so just zero it and continue
                semiFixedLayout.AddCoordinate(0);
                return this.GetLayoutsToConsider(query, semiFixedLayout);
            }
            // If this dimension does not affect this query, then just set it to maximum and move on
            if (semiFixedLayout.NumUnsetCoordinatesInCurrentDimension == 1)
            {
                if ((semiFixedLayout.NextCoordinateAffectsWidth && !query.MinimizesWidth()) || (!semiFixedLayout.NextCoordinateAffectsWidth && !query.MinimizesHeight()))
                {
                    bool widthNext = semiFixedLayout.NextCoordinateAffectsWidth;
                    semiFixedLayout.AddCoordinate(currentCoordinate);
                    if (widthNext)
                    {
                        if (semiFixedLayout.Width != query.MaxWidth)
                        {
                            System.Diagnostics.Debug.WriteLine("rounding error: semiFixedLayout.GetRequiredWidth() (" + semiFixedLayout.Width.ToString() + ") != query.MaxWidth (" + query.MaxWidth.ToString() + ")");
                            System.Diagnostics.Debug.WriteLine("rounding error is " + (semiFixedLayout.Width - query.MaxWidth));
                        }
                    }
                    else
                    {
                        if (semiFixedLayout.Height != query.MaxHeight)
                        {
                            System.Diagnostics.Debug.WriteLine("rounding error: semiFixedLayout.GetRequiredHeight() (" + semiFixedLayout.Height.ToString() + ") != query.MaxHeight (" + query.MaxHeight.ToString() + ")");
                            System.Diagnostics.Debug.WriteLine("rounding error is " + (semiFixedLayout.Height - query.MaxHeight));
                        }
                    }
                    return this.GetLayoutsToConsider(query, semiFixedLayout);
                }
            }
            if (!query.MaximizesScore())
            {
                // if we can use 0's for all of the remaining coordinates, then do that
                SemiFixed_GridLayout candidateWithZeros = new SemiFixed_GridLayout(semiFixedLayout);
                while (candidateWithZeros.NumUnsetCoordinatesInCurrentDimension > 0)
                {
                    candidateWithZeros.AddCoordinate(0);
                }
                if (query.Accepts(candidateWithZeros))
                {
                    results.AddLast(candidateWithZeros);
                    return results;
                }
            }

            SemiFixed_GridLayout bestSublayout = null;
            SemiFixed_GridLayout currentSublayout = null;
            bool increaseScore = true;
            while (currentCoordinate >= 0)
            {
                GridLayout.NumComputations++;
                if (increaseScore)
                {
                    // if we get here, we must increase the score of the layout without increasing the size too much

                    // If we want to eventually find the layout with max score, then start by checking for that
                    LayoutQuery subQuery;
                    SemiFixed_GridLayout newSublayout = null;
                    if (query.MaximizesScore())
                    {
                        // maximize the score while keeping the size to the required value
                        subQuery = new MaxScore_LayoutQuery();
                        subQuery.MaxWidth = query.MaxWidth;
                        subQuery.MaxHeight = query.MaxHeight;
                        subQuery.MinScore = query.MinScore;
                        subQuery.Debug = query.Debug;
                        subQuery.ProposedSolution_ForDebugging = query.ProposedSolution_ForDebugging;
                        


                        SemiFixed_GridLayout currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                        currentLayout.AddCoordinate(currentCoordinate);

                        SemiFixed_GridLayout oldLayout = currentSublayout;

                        // recursively query the next dimension
                        newSublayout = this.GetBestLayout(subQuery, currentLayout);
                        if (newSublayout != null)
                        {
                            currentSublayout = newSublayout;
                        }
                    }



                    if (newSublayout == null)
                    {
                        // If we didn't find any layout of this size that is valid, then relax the constraints but look for the thinnest one having enough score
                        if (semiFixedLayout.NextCoordinateAffectsWidth)
                            subQuery = new MinWidth_LayoutQuery();
                        else
                            subQuery = new MinHeight_LayoutQuery();
                        subQuery.Debug = query.Debug;

                        // if we already have one solution and we are trying to maximize the score, then we only care about solutions that improve beyond that
                        subQuery.MinScore = query.MinScore;
                        if (query.MaximizesScore() && bestSublayout != null)
                            subQuery.MinScore = subQuery.MinScore.Plus(LayoutScore.Tiny);
                        if (semiFixedLayout.NextCoordinateAffectsWidth)
                        {
                            // bring the score to the required value and minimize the width
                            subQuery.MaxWidth = currentCoordinate + query.MaxWidth;
                            subQuery.MaxHeight = query.MaxHeight;
                        }
                        else
                        {
                            // bring the score to the required value and minimize the height
                            subQuery.MaxWidth = query.MaxWidth;
                            subQuery.MaxHeight = currentCoordinate + query.MaxHeight;
                        }


                        SemiFixed_GridLayout currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                        // start by setting the maximum coordinate, and decrease it until it gets to zero
                        currentLayout.AddCoordinate(currentCoordinate);

                        // recursively query the next dimension
                        currentSublayout = this.GetBestLayout(subQuery, currentLayout);

                        if (currentSublayout == null)
                            break;
                    }





                }
                else
                {
                    if (currentCoordinate < this.pixelSize)
                        break;
                    LayoutScore allowedScoreDecrease = LayoutScore.Zero;
                    if (!query.MaximizesScore() && semiFixedLayout.NextCoordinateAffectsWidth != semiFixedLayout.SetWidthBeforeHeight)
                    {
                        // If we're shrinking a coordinate in the second dimension, and we don't care to maximize the score,
                        // then we can compute a worse score that we may still shrink to
                        LayoutScore decrease = currentSublayout.Score.Minus(query.MinScore);
                        if (decrease.CompareTo(LayoutScore.Zero) > 0)
                            allowedScoreDecrease = decrease;
                    }
                    // if we get here, we wish to decrease the size of the layout
                    if (semiFixedLayout.NextCoordinateAffectsWidth)
                    {
                        int index = semiFixedLayout.NumCoordinatesSetInCurrentDimension;
                        // shrink the width without decreasing the score any more than needed
                        double maxWidth = Math.Min(currentCoordinate - this.pixelSize, currentSublayout.Get_GroupWidth(index) - (currentSublayout.Width - query.MaxWidth));
                        this.ShrinkWidth(currentSublayout, index, query, allowedScoreDecrease);
                        if (currentSublayout.Get_GroupWidth(index) > maxWidth)
                            currentSublayout.Set_GroupWidth(index, maxWidth);
                        currentCoordinate = currentSublayout.Get_GroupWidth(index);

                        //currentSublayout.Set_GroupWidth(index, currentCoordinate);
                    }
                    else
                    {
                        int index = semiFixedLayout.NumCoordinatesSetInCurrentDimension;
                        // shrink the height without decreasing the score any more than needed
                        double maxHeight = Math.Min(currentCoordinate - this.pixelSize, currentSublayout.Get_GroupHeight(index) - (currentSublayout.Height - query.MaxHeight));
                        this.ShrinkHeight(currentSublayout, index, query, allowedScoreDecrease);
                        if (currentSublayout.Get_GroupHeight(index) > maxHeight)
                            currentSublayout.Set_GroupHeight(index, maxHeight);

                        currentCoordinate = currentSublayout.Get_GroupHeight(index);

                        //currentSublayout.Set_GroupHeight(index, currentCoordinate);
                    }
                }
                // keep track of the coordinates that we are considering
                results.AddLast(new SemiFixed_GridLayout(currentSublayout));
                // keep track of the best layout so far
                if (query.PreferredLayout(currentSublayout, bestSublayout) == currentSublayout)
                    bestSublayout = new SemiFixed_GridLayout(currentSublayout);
                if (bestSublayout != null && query.Accepts(bestSublayout))
                {
                    // if we've found a layout that works, then any future layouts we find must be at least as good as this one
                    query.OptimizeUsingExample(bestSublayout);
                }
                if (!query.MaximizesScore() && query.Accepts(currentSublayout))
                {
                    // if the score is enough for now, then keep shrinking the width some more
                    increaseScore = false;
                }
                else
                {
                    increaseScore = !increaseScore;
                }
            }
            return results;
        }

        // I need to revamp the GetBestLayout function so that it processes constraints in different orders based on the query
        private SemiFixed_GridLayout GetBestLayout(LayoutQuery query, SemiFixed_GridLayout semiFixedLayout)
        {
            SemiFixed_GridLayout bestLayout = null;
            IEnumerable<SemiFixed_GridLayout> layouts;
#if false
            if (query.MinimizesWidth())
            {
                // for min-width queries, try a slightly smaller query first
                LayoutQuery subQuery = query.Clone();
                subQuery.MaxWidth /= 2;

                layouts = this.GetLayoutsToConsider(subQuery, semiFixedLayout);
                if (layouts.Count() != 0)
                {
                    foreach (SemiFixed_GridLayout layout in layouts)
                    {
                        if (query.PreferredLayout(layout, bestLayout) == layout)
                        {
                            bestLayout = layout;
                        }
                    }
                    return bestLayout;
                }
            }
            if (query.MinimizesHeight())
            {
                // for min-height queries, try a slightly smaller query first
                LayoutQuery subQuery = query.Clone();
                subQuery.MaxHeight /= 2;

                layouts = this.GetLayoutsToConsider(subQuery, semiFixedLayout);
                if (layouts.Count() != 0)
                {
                    foreach (SemiFixed_GridLayout layout in layouts)
                    {
                        if (query.PreferredLayout(layout, bestLayout) == layout)
                        {
                            bestLayout = layout;
                        }
                    }
                    return bestLayout;
                }
            }
#endif
            layouts = this.GetLayoutsToConsider(query, semiFixedLayout);
            foreach (SemiFixed_GridLayout layout in layouts)
            {
                if (query.PreferredLayout(layout, bestLayout) == layout)
                {
                    bestLayout = layout;
                }
            }
            return bestLayout;
        }

        /*

        // answers the given LayoutQuery and explicitly checks proposedSolution
        public override SpecificLayout GetBestLayout_Debugged(LayoutQuery query, SpecificLayout proposedSolution)
        {
            if (!(proposedSolution is SemiFixed_GridLayout))
                throw new ArgumentException("proposedSolution must be of type SemiFixed_GridLayout");
            SemiFixed_GridLayout convertedSolution = proposedSolution as SemiFixed_GridLayout;


            SemiFixed_GridLayout fastResult = (SemiFixed_GridLayout) this.GetBestLayout(query);
            if (query.PreferredLayout(fastResult, proposedSolution) == fastResult)
                return base.GetBestLayout(query);  // everything worked fine

            System.Diagnostics.Debug.WriteLine("error");
            // now try to figure out what went wrong

            SemiFixed_GridLayout semiFixedLayout = new SemiFixed_GridLayout(this.elements, this.rowHeights, this.columnWidths, this.bonusScore, query.MinimizesWidth());
            IEnumerable<SemiFixed_GridLayout> layoutOptions = this.GetLayoutsToConsider(query, semiFixedLayout);

            SemiFixed_GridLayout previousLayout = null;
            foreach (SemiFixed_GridLayout currentLayout in layoutOptions)
            {
                if (!this.CouldTransform(currentLayout, convertedSolution))
                {
                    System.Diagnostics.Debug.WriteLine("error: went from " + previousLayout + " to " + currentLayout + " without considering " + proposedSolution);
                }
                previousLayout = null;
            }

            return base.GetBestLayout_Debugged(query, proposedSolution);
        }

        */
        // shrinks the specified width a lot without decreasing the layout's score by any more than allowedScoreDecrease
        // sourceQuery is the LayoutQuery that caused this call to ShrinkWidth
        private void ShrinkWidth(SemiFixed_GridLayout layout, int indexOf_propertyGroup_toShrink, LayoutQuery sourceQuery, LayoutScore totalAllowedScoreDecrease)
        {
            if (totalAllowedScoreDecrease.CompareTo(LayoutScore.Zero) < 0)
            {
                System.Diagnostics.Debug.WriteLine("Error: cannot improve score by shrinking");
            }

            //layout = new SemiFixed_GridLayout(layout);
            //double maxWidth = layout.Get_GroupWidth(indexOf_propertyGroup_toShrink);
            List<int> indices = layout.Get_WidthGroup_AtIndex(indexOf_propertyGroup_toShrink);
            List<double> minWidths = new List<double>();
            LayoutScore eachAllowedScoreDecrease = totalAllowedScoreDecrease.Times((double)1 / (double)(this.NumRows * indices.Count));
            // the current setup is invalid, so shrinking the width down to a valid value is adequate
            foreach (int columnNumber in indices)
            {
                int rowNumber;
                double maxRequiredWidth = 0;
                for (rowNumber = 0; rowNumber < this.rowHeights.NumProperties; rowNumber++)
                {
                    double currentWidth = 0;
                    LayoutChoice_Set subLayout = this.elements[columnNumber, rowNumber];
                    if (subLayout != null)
                    {
                        double columnWeight = layout.GetWidthFraction(columnNumber);

                        // ask the view for the highest-scoring size that fits within the specified dimensions
                        LayoutQuery query = new MaxScore_LayoutQuery();
                        query.Debug = sourceQuery.Debug;
                        //query.MaxWidth = this.RoundWidthUp(maxWidth * columnWeight);
                        //query.MaxWidth = this.RoundWidthDown(maxWidth * columnWeight);
                        query.MaxWidth = this.RoundWidthDown(layout.GetWidthCoordinate(columnNumber));
                        query.MaxHeight = layout.GetHeightCoordinate(rowNumber);
                        //SpecificLayout repeatResult = null;
                        if (query.Debug)
                        {
                            SemiFixed_GridLayout converted = sourceQuery.ProposedSolution_ForDebugging as SemiFixed_GridLayout;
                            if (converted != null)
                            {
                                SpecificLayout possibleSolution = converted.GetChildLayout(columnNumber, rowNumber);
                                if (query.Accepts(possibleSolution))
                                    query.ProposedSolution_ForDebugging = possibleSolution;
                            }
                        }
                        GridLayout.NumComputations++;
                        SpecificLayout bestLayout = subLayout.GetBestLayout(query.Clone());
                        //LayoutDimensions dimensions = bestLayout.Dimensions;


                        // figure out how far the view can shrink while keeping the same score
                        LayoutQuery query2 = new MinWidth_LayoutQuery();
                        query2.Debug = sourceQuery.Debug;
                        //query2.ProposedSolution_ForDebugging = query.ProposedSolution_ForDebugging;
                        //query2.MaxWidth = (maxWidth - 1) * columnWeight;    // 1 unit less than the current value, to ensure that the width actually decreases
                        query2.MaxWidth = query.MaxWidth;    // 1 unit less than the current value, to ensure that the width actually decreases


                        query2.MaxHeight = query.MaxHeight;
                        query2.MinScore = bestLayout.Score.Minus(eachAllowedScoreDecrease);

                        //LayoutQuery copy = query2.Clone();


                        SpecificLayout layout2 = subLayout.GetBestLayout(query2.Clone());


                        // problem: if
                        if (layout2 == null)
                        {
                            System.Diagnostics.Debug.WriteLine("Error: min-width query did not find result from max-score query");
                            // note, also, that the layout cache seems to have an incorrect value for when minScore = -infinity
                            LayoutQuery debugQuery1 = query.Clone();
                            debugQuery1.Debug = true;
                            debugQuery1.ProposedSolution_ForDebugging = bestLayout;
                            SpecificLayout debugResult1 = subLayout.GetBestLayout(debugQuery1.Clone());
                            LayoutQuery debugQuery2 = query2.Clone();
                            debugQuery2.Debug = true;
                            debugQuery2.ProposedSolution_ForDebugging = debugResult1;
                            subLayout.GetBestLayout(debugQuery2.Clone());
                            debugQuery2.MinScore = LayoutScore.Minimum;
                            SpecificLayout layout3 = subLayout.GetBestLayout(debugQuery2.Clone());
                            System.Diagnostics.Debug.WriteLine("");
                        }
                        if (!query2.Accepts(layout2))
                        {
                            System.Diagnostics.Debug.WriteLine("Error: min-width query received an invalid response");
                        }

                        currentWidth = this.RoundWidthUp(layout2.Width);
                        if (currentWidth > maxRequiredWidth)
                        {
                            maxRequiredWidth = currentWidth;
                            if (maxRequiredWidth == query.MaxWidth)
                                break;
                        }
                    }
                }
                minWidths.Add(maxRequiredWidth);
            }

            LayoutScore originalScore = layout.Score;
            layout.SetWidthMinValues(indexOf_propertyGroup_toShrink, minWidths);
            if (LayoutScore.Zero.Equals(totalAllowedScoreDecrease))
            {
                if (sourceQuery.Debug)
                {
                    if (layout.Score.CompareTo(originalScore) != 0)
                        System.Diagnostics.Debug.WriteLine("Error; ShrinkWidth caused a change in score");
                }
                else
                {
                    // restore the score since we know it hasn't changed
                    layout.SetScore(originalScore);
                }
            }
            else
            {
                if (layout.Score.CompareTo(originalScore.Minus(totalAllowedScoreDecrease)) < 0)
                    System.Diagnostics.Debug.WriteLine("Error; ShrinkWidth decreased the score too much");
            }
        }

        // shrinks the specified height as much as possible without decreasing the layout's score
        private void ShrinkHeight(SemiFixed_GridLayout layout, int indexOf_propertyGroup_toShrink, LayoutQuery sourceQuery, LayoutScore totalAllowedScoreDecrease)
        {
            if (totalAllowedScoreDecrease.CompareTo(LayoutScore.Zero) < 0)
            {
                System.Diagnostics.Debug.WriteLine("Error: cannot improve score by shrinking");
            }
            //layout = new SemiFixed_GridLayout(layout);
            //double maxHeight = layout.Get_GroupHeight(indexOf_propertyGroup_toShrink);
            List<int> indices = layout.Get_HeightGroup_AtIndex(indexOf_propertyGroup_toShrink);
            List<double> minHeights = new List<double>();
            LayoutScore eachAllowedScoreDecrease = totalAllowedScoreDecrease.Times((double)1 / (double)(this.columnWidths.NumProperties * indices.Count));
            foreach (int rowNumber in indices)
            {
                int columnNumber;
                double maxRequiredHeight = 0;
                for (columnNumber = 0; columnNumber < this.columnWidths.NumProperties; columnNumber++)
                {
                    double currentHeight = 0;
                    LayoutChoice_Set subLayout = this.elements[columnNumber, rowNumber];
                    if (subLayout != null)
                    {
                        double rowWeight = layout.GetHeightFraction(rowNumber);
                        // ask the view for the highest-scoring size that fits within the specified dimensions
                        LayoutQuery query = new MaxScore_LayoutQuery();
                        query.Debug = sourceQuery.Debug;
                        query.ProposedSolution_ForDebugging = sourceQuery.ProposedSolution_ForDebugging;
                        query.MaxWidth = layout.GetWidthCoordinate(columnNumber);
                        query.MaxHeight = this.RoundHeightDown(layout.GetHeightCoordinate(rowNumber));
                        //query.MaxHeight = this.RoundHeightUp(maxHeight * rowWeight);
                        //query.MaxHeight = this.RoundHeightDown(maxHeight * rowWeight);
                        if (query.Debug)
                        {
                            SemiFixed_GridLayout converted = sourceQuery.ProposedSolution_ForDebugging as SemiFixed_GridLayout;
                            if (converted != null)
                            {
                                SpecificLayout possibleSolution = converted.GetChildLayout(columnNumber, rowNumber);
                                if (query.Accepts(possibleSolution))
                                    query.ProposedSolution_ForDebugging = possibleSolution;
                            }
                        }
                        GridLayout.NumComputations++;
                        SpecificLayout bestLayout = subLayout.GetBestLayout(query.Clone());
                        //LayoutDimensions dimensions = bestLayout.Dimensions;


                        // figure out how far the view can shrink while keeping the same score
                        LayoutQuery query2 = new MinHeight_LayoutQuery();
                        query2.MaxWidth = query.MaxWidth;
                        query2.MaxHeight = query.MaxHeight;
                        query2.MinScore = bestLayout.Score.Minus(eachAllowedScoreDecrease);
                        query2.Debug = sourceQuery.Debug;
                        query2.ProposedSolution_ForDebugging = sourceQuery.ProposedSolution_ForDebugging;

                        SpecificLayout layout2 = subLayout.GetBestLayout(query2.Clone());

                        // problem: if
                        if (layout2 == null)
                        {
                            System.Diagnostics.Debug.WriteLine("Error: min-height query did not find result from max-score query");
                            // note, also, that the layout cache seems to have an incorrect value for when minScore = -infinity
                            LayoutQuery debugQuery1 = query.Clone();
                            debugQuery1.Debug = true;
                            debugQuery1.ProposedSolution_ForDebugging = bestLayout;
                            subLayout.GetBestLayout(debugQuery1.Clone());
                            LayoutQuery debugQuery2 = query2.Clone();
                            debugQuery2.Debug = true;
                            debugQuery2.ProposedSolution_ForDebugging = bestLayout;
                            subLayout.GetBestLayout(debugQuery2.Clone());
                            debugQuery2.MinScore = LayoutScore.Minimum;
                            SpecificLayout layout3 = subLayout.GetBestLayout(debugQuery2.Clone());
                            System.Diagnostics.Debug.WriteLine("");
                        }

                        if (!query2.Accepts(layout2))
                        {
                            System.Diagnostics.Debug.WriteLine("Error: min-height query received an invalid response");
                        }

                        currentHeight = this.RoundHeightUp(layout2.Height);
                        if (currentHeight > maxRequiredHeight)
                        {
                            maxRequiredHeight = currentHeight;
                            if (maxRequiredHeight == query.MaxHeight)
                                break;
                        }
                    }
                }
                minHeights.Add(maxRequiredHeight);
            }
            LayoutScore originalScore = layout.Score;
            layout.SetHeightMinValues(indexOf_propertyGroup_toShrink, minHeights);
            if (LayoutScore.Zero.Equals(totalAllowedScoreDecrease))
            {
                if (sourceQuery.Debug)
                {
                    if (layout.Score.CompareTo(originalScore) != 0)
                        System.Diagnostics.Debug.WriteLine("Error; ShrinkHeight caused a change in score");
                }
                else
                {
                    // restore the score since we know it hasn't changed
                    layout.SetScore(originalScore);
                }
            }
            else
            {
                if (layout.Score.CompareTo(originalScore.Minus(totalAllowedScoreDecrease)) < 0)
                    System.Diagnostics.Debug.WriteLine("Error; ShrinkHeight decreased the score too much");
            }
        }


        /*
        // attempts to increase specified width until the total score of the entire layout is at least minScore
        private SemiFixed_GridLayout IncreaseWidth(LayoutScore minScore, SemiFixed_GridLayout layout, int indexOf_propertyGroup_toShrink, double maxWidth)
        {
            layout = new SemiFixed_GridLayout(layout);
            List<int> indices = layout.ColumnWidths.GetGroupAtIndex(indexOf_propertyGroup_toShrink);

            // count the number of views being resized, for the purpose of computing how much some scores must increase
            double numViewsBeingResized = 0;
            foreach (int columnNumber in indices)
            {
                int rowNumber;
                for (rowNumber = 0; rowNumber < this.NumRows; rowNumber++)
                {
                    IView view = this.elements[columnNumber, rowNumber];
                    if (view != null)
                        numViewsBeingResized++;
                }
            }

            while (true)
            {
                double bestWidth = double.PositiveInfinity;
                // Given N views that must contribute to an increase S in score, we know that at least one of them must contribute at least S/N
                // So, we ask each of them to increase by at least S/N, look for the smallest increase, and repeat
                LayoutScore scoreGap = minScore.Minus(layout.Score);
                LayoutScore minImprovement = scoreGap.Times(1 / numViewsBeingResized);

                // iterate over each relevant subview
                foreach (int columnNumber in indices)
                {
                    int rowNumber;
                    for (rowNumber = 0; rowNumber < this.NumRows; rowNumber++)
                    {
                        IView view = this.elements[columnNumber, rowNumber];
                        if (view != null)
                        {
                            double columnWeight = layout.ColumnWidths.GetFraction(columnNumber);

                            // ask the view for the highest-scoring size for its current bounds
                            LayoutQuery query = new MaxScore_LayoutQuery();
                            query.MaxWidth = this.GetUsableColumnWidth(columnNumber, layout);
                            query.MaxHeight = this.GetUsableRowHeight(rowNumber, layout);
                            SpecificLayout bestLayout = view.Layout.GetBestLayout(query.Clone());
                            //LayoutDimensions dimensions = bestLayout.Dimensions;


                            // figure out how much bigger the view must get in order to increase its score appropriately
                            LayoutQuery query2 = new MinWidth_LayoutQuery();
                            query2.MaxHeight = query.MaxHeight;
                            query2.MinScore = bestLayout.Score.Plus(minImprovement);

                            // ask the view for the the answer
                            SpecificLayout layout2 = view.Layout.GetBestLayout(query2.Clone());

                            if (layout2 != null)
                            {
                                // compute coordinates
                                double columnWidth = this.RoundWidthUp(layout2.Width);
                                double currentWidth = columnWidth / columnWeight;
                                //currentWidth = Math.Ceiling(currentWidth / pixelSize) * pixelSize;

                                // keep track of the largest amount we might have to rescale the values by
                                if (currentWidth < bestWidth)
                                {
                                    bestWidth = currentWidth;
                                }
                            }
                        }
                    }
                }
                // if no view could increase enough, then there are no solutions
                if (double.IsPositiveInfinity(bestWidth))
                    return null;
                // if no view could increase enough, then there are no solutions
                if (bestWidth > maxWidth)
                    return null;
                // If at least one view could increase its score enough, increase the width of this column and repeat
                layout.ColumnWidths.Set_GroupTotal(indexOf_propertyGroup_toShrink, bestWidth);
                // if we've finally achieved our goal, then return that information
                if (layout.Score.CompareTo(minScore) >= 0)
                    return layout;
            }
        }

        // attempts to increase specified width until the total score of the entire layout is at least minScore
        private SemiFixed_GridLayout IncreaseHeight(LayoutScore minScore, SemiFixed_GridLayout layout, int indexOf_propertyGroup_toShrink, double maxHeight)
        {
            layout = new SemiFixed_GridLayout(layout);
            List<int> indices = layout.RowHeights.GetGroupAtIndex(indexOf_propertyGroup_toShrink);

            // count the number of views being resized, for the purpose of computing how much some scores must increase
            double numViewsBeingResized = 0;
            foreach (int rowNumber in indices)
            {
                int columnNumber;
                for (columnNumber = 0; columnNumber < this.NumColumns; columnNumber++)
                {
                    IView view = this.elements[columnNumber, rowNumber];
                    if (view != null)
                        numViewsBeingResized++;
                }
            }

            while (true)
            {
                double bestHeight = double.PositiveInfinity;
                // Given N views that must contribute to an increase S in score, we know that at least one of them must contribute at least S/N
                // So, we ask each of them to increase by at least S/N, look for the smallest increase, and repeat
                LayoutScore scoreGap = minScore.Minus(layout.Score);
                LayoutScore minImprovement = scoreGap.Times(1 / numViewsBeingResized);

                // iterate over each relevant subview
                foreach (int rowNumber in indices)
                {
                    int columnNumber;
                    for (columnNumber = 0; columnNumber < this.NumColumns; columnNumber++)
                    {
                        IView view = this.elements[columnNumber, rowNumber];
                        if (view != null)
                        {
                            double rowWeight = layout.RowHeights.GetFraction(rowNumber);

                            // ask the view for the highest-scoring size for its current bounds
                            LayoutQuery query = new MaxScore_LayoutQuery();
                            query.MaxWidth = this.GetUsableColumnWidth(columnNumber, layout);
                            query.MaxHeight = this.GetUsableRowHeight(rowNumber, layout);
                            SpecificLayout bestLayout = view.Layout.GetBestLayout(query.Clone());
                            //LayoutDimensions dimensions = bestLayout.Dimensions;


                            // figure out how much bigger the view must get in order to increase its score appropriately
                            LayoutQuery query2 = new MinHeight_LayoutQuery();
                            query2.MaxWidth = query.MaxWidth;
                            query2.MinScore = bestLayout.Score.Plus(minImprovement);

                            // ask the view for the the answer
                            SpecificLayout layout2 = view.Layout.GetBestLayout(query2.Clone());

                            if (layout2 != null)
                            {
                                // compute coordinates
                                double rowHeight = this.RoundHeightUp(layout2.Height);
                                double currentHeight = rowHeight / rowWeight;
                                //currentHeight = Math.Ceiling(currentHeight / pixelSize) * pixelSize;

                                // keep track of the largest amount we might have to rescale the values by
                                if (currentHeight < bestHeight)
                                {
                                    bestHeight = currentHeight;
                                }
                            }
                        }
                    }
                }
                // if no view could increase enough, then there are no solutions
                if (double.IsPositiveInfinity(bestHeight))
                    return null;
                // if no view could increase enough, then there are no solutions
                if (bestHeight > maxHeight)
                    return null;
                // If at least one view could increase its score enough, the we increase the width of this column and repeat
                layout.RowHeights.Set_GroupTotal(indexOf_propertyGroup_toShrink, bestHeight);
                // if we've finally achieved our goal, then return that information
                if (layout.Score.CompareTo(minScore) >= 0)
                    return layout;
            }
        }


        */
        private double GetUsableColumnWidth(int columnIndex, SemiFixed_GridLayout layout)
        {
            //return layout.ColumnWidths.GetValue(columnIndex);
            return this.RoundWidthDown(layout.GetWidthCoordinate(columnIndex));
            //return Math.Floor(layout.ColumnWidths.GetValue(columnIndex) / this.pixelSize) * this.pixelSize;
        }
        private double GetUsableRowHeight(int rowIndex, SemiFixed_GridLayout layout)
        {
            //return layout.RowHeights.GetValue(rowIndex);
            return this.RoundHeightDown(layout.GetHeightCoordinate(rowIndex));
            //return Math.Floor(layout.RowHeights.GetValue(rowIndex) / this.pixelSize) * this.pixelSize;
        }

        private double RoundWidthUp(double value)
        {
            //return value;
            return Math.Ceiling(value / this.pixelSize) * this.pixelSize;
        }
        private double RoundHeightUp(double value)
        {
            return this.RoundWidthUp(value);
        }

        public double RoundWidthDown(double value)
        {
            //return value;
            return Math.Floor(value / this.pixelSize) * this.pixelSize;
        }
        public double RoundHeightDown(double value)
        {
            return this.RoundWidthDown(value);
        }


        // given a layout, this function attempts to shrink it without decreasing the score
        // It allows the cache managing this layout to run faster, because it makes our results more likely to be valid for other queries
        private SemiFixed_GridLayout ShrinkLayout(SemiFixed_GridLayout layout, bool enableDebugging)
        {
            SemiFixed_GridLayout previousLayout = new SemiFixed_GridLayout(layout);
            LayoutScore previousScore = layout.Score;
            if (layout == null)
                return layout;
            int i;
            List<double> groupRowHeights = new List<double>(layout.GetNumHeightGroups());
            List<int> group_limitingRowNumbers = new List<int>(layout.GetNumHeightGroups());
            List<double> group_limitingRowHeights = new List<double>(layout.GetNumHeightGroups());
            for (i = 0; i < layout.GetNumHeightGroups(); i++)
            {
                groupRowHeights.Add(0);
                group_limitingRowNumbers.Add(layout.Get_HeightGroup_AtIndex(i)[0]);
                group_limitingRowHeights.Add(0);
            }
            List<double> requiredRowHeights = new List<double>(layout.GetNumHeightProperties());
            for (i = 0; i < layout.GetNumHeightProperties(); i++)
            {
                requiredRowHeights.Add(0);
            }

            List<double> groupColumnWidths = new List<double>(layout.GetNumWidthGroups());
            List<int> group_limitingColumnNumbers = new List<int>(layout.GetNumWidthGroups());
            List<double> group_limitingColumnWidths = new List<double>(layout.GetNumWidthGroups());
            for (i = 0; i < layout.GetNumWidthGroups(); i++)
            {
                groupColumnWidths.Add(0);
                group_limitingColumnNumbers.Add(layout.Get_WidthGroup_AtIndex(i)[0]);
                group_limitingColumnWidths.Add(0);
            }
            List<double> requiredColumnWidths = new List<double>(layout.GetNumWidthProperties());
            for (i = 0; i < layout.GetNumWidthProperties(); i++)
            {
                requiredColumnWidths.Add(0);
            }

            int rowNumber, columnNumber;

            // iterate over each subview
            for (rowNumber = layout.GetNumHeightProperties() - 1; rowNumber >= 0; rowNumber--)
            {
                double height = this.GetUsableRowHeight(rowNumber, layout);
                for (columnNumber = layout.GetNumWidthProperties() - 1; columnNumber >= 0; columnNumber--)
                {
                    double width = this.GetUsableColumnWidth(columnNumber, layout);
                    LayoutChoice_Set currentLayout = this.elements[columnNumber, rowNumber];
                    if (currentLayout != null)
                    {
                        // ask the subview for the best layout that fits in these dimensions. Hopefully, it will generate a result that is no larger than needed
                        LayoutQuery query = new MaxScore_LayoutQuery();
                        query.MaxWidth = width;
                        query.MaxHeight = height;
                        query.Debug = enableDebugging;
                        SpecificLayout subLayout = currentLayout.GetBestLayout(query);
                        double requiredWidth = this.RoundWidthUp(subLayout.Width);
                        double requiredHeight = this.RoundHeightUp(subLayout.Height);
                        if (query.Debug)
                        {
                            query.MaxWidth = requiredWidth;
                            query.MaxHeight = requiredHeight;
                            SpecificLayout recheckedLayout = currentLayout.GetBestLayout(query);
                            if (query.PreferredLayout(subLayout, recheckedLayout) != subLayout)
                                System.Diagnostics.Debug.WriteLine("Error; sublayout cannot re-find its own response");

                        }
                        // update out how large the rows and columns need to be, using this new information
                        double requiredGroupWidth = requiredWidth / layout.GetWidthFraction(columnNumber);
                        double requiredGroupHeight = requiredHeight / layout.GetHeightFraction(rowNumber);

                        int groupRowIndex = layout.Get_HeightGroup_Index(rowNumber);
                        if (groupRowHeights[groupRowIndex] < requiredGroupHeight)
                        {
                            groupRowHeights[groupRowIndex] = requiredGroupHeight;
                            group_limitingRowNumbers[groupRowIndex] = rowNumber;
                            group_limitingRowHeights[groupRowIndex] = requiredHeight;
                        }
                        int groupColumnIndex = layout.Get_WidthGroup_Index(columnNumber);
                        if (groupColumnWidths[groupColumnIndex] < requiredGroupWidth)
                        {
                            groupColumnWidths[groupColumnIndex] = requiredGroupWidth;
                            group_limitingColumnNumbers[groupColumnIndex] = columnNumber;
                            group_limitingColumnWidths[groupColumnIndex] = requiredWidth;
                        }

                        if (requiredColumnWidths[columnNumber] < requiredWidth)
                            requiredColumnWidths[columnNumber] = requiredWidth;
                        if (requiredRowHeights[rowNumber] < requiredHeight)
                            requiredRowHeights[rowNumber] = requiredHeight;
                    }
                }
            }
            LayoutScore newScore;
            // update each row and column
            for (i = 0; i < groupRowHeights.Count; i++)
            {
                if (groupRowHeights[i] < layout.Get_GroupHeight(i))
                {
                    //layout.RowHeights.Set_GroupTotal(i, groupRowHeights[i]);
                    layout.SetHeightValue(group_limitingRowNumbers[i], group_limitingRowHeights[i]);
                }
                if (enableDebugging)
                {
                    newScore = layout.Score;
                    if (newScore.CompareTo(previousScore) < 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Rounding error appears to be taking place in a sublayout");
                        if (!enableDebugging)
                            this.ShrinkLayout(new SemiFixed_GridLayout(previousLayout), true);
                        MaxScore_LayoutQuery query = new MaxScore_LayoutQuery();
                        query.MaxWidth = previousLayout.Width;
                        query.MaxHeight = previousLayout.Height;
                        query.MinScore = previousLayout.Score;
                        query.Debug = true;
                        query.ProposedSolution_ForDebugging = previousLayout;
                        this.GetBestLayout(query);
                    }
                }
                else
                {
                    // we made sure not to change the score
                    layout.SetScore(previousScore);
                }
            }
            for (i = 0; i < groupColumnWidths.Count; i++)
            {
                if (groupColumnWidths[i] < layout.Get_GroupWidth(i))
                {
                    //layout.ColumnWidths.Set_GroupTotal(i, groupColumnWidths[i]);
                    layout.SetWidthValue(group_limitingColumnNumbers[i], group_limitingColumnWidths[i]);
                }
                if (enableDebugging)
                {
                    newScore = layout.Score;
                    if (newScore.CompareTo(previousScore) < 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Rounding error appears to be taking place in a sublayout");
                        if (!enableDebugging)
                            this.ShrinkLayout(new SemiFixed_GridLayout(previousLayout), true);
                        MaxScore_LayoutQuery query = new MaxScore_LayoutQuery();
                        query.MaxWidth = previousLayout.Width;
                        query.MaxHeight = previousLayout.Height;
                        query.MinScore = previousLayout.Score;
                        query.Debug = true;
                        query.ProposedSolution_ForDebugging = previousLayout;
                        this.GetBestLayout(query);
                    }
                }
                else
                {
                    // we made sure not to change the score
                    layout.SetScore(previousScore);
                }
            }
            for (i = 0; i < layout.GetNumHeightProperties(); i++)
            {
                if (layout.GetHeightCoordinate(i) < requiredRowHeights[i])
                    System.Diagnostics.Debug.WriteLine("Rounding error");
            }
            for (i = 0; i < layout.GetNumWidthProperties(); i++)
            {
                if (layout.GetWidthCoordinate(i) < requiredColumnWidths[i])
                    System.Diagnostics.Debug.WriteLine("Rounding error");
            }
            newScore = layout.Score;
            if (newScore.CompareTo(previousScore) < 0)
            {
                System.Diagnostics.Debug.WriteLine("Rounding error appears to be taking place in a sublayout");
                if (!enableDebugging)
                    this.ShrinkLayout(new SemiFixed_GridLayout(previousLayout), true);
                MaxScore_LayoutQuery query = new MaxScore_LayoutQuery();
                query.MaxWidth = previousLayout.Width;
                query.MaxHeight = previousLayout.Height;
                query.MinScore = previousLayout.Score;
                query.Debug = true;
                query.ProposedSolution_ForDebugging = previousLayout;
                this.GetBestLayout(query);
            }

            return layout;
        }

        // tells whether it's possible that if we consider earlierLayout then we may later consider laterLayout
        private bool CouldTransform(SemiFixed_GridLayout earlierLayout, SemiFixed_GridLayout laterLayout)
        {
            if (laterLayout.NextDimensionToSet < earlierLayout.NextDimensionToSet)
                return false;
            if (earlierLayout.SetWidthBeforeHeight != laterLayout.SetWidthBeforeHeight)
                return false;
            int i;
            for (i = 0; i < earlierLayout.NextDimensionToSet; i++)
            {
                int comparison = earlierLayout.GetCoordinate(i).CompareTo(laterLayout.GetCoordinate(i));
                if (comparison < 0)
                    return false;
                if (comparison > 0)
                    return true;
            }
            return true;   // all coordinates in earlierLayout match the coordinates in laterLayout
        }



        private LayoutChoice_Set[,] elements;
        private BoundProperty_List rowHeights;
        private BoundProperty_List columnWidths;
        private LayoutScore bonusScore;
        double pixelSize;   // for rounding error, we snap all values to a multiple of pixelSize
        int nextOpenRow;
        int nextOpenColumn;
        //double numComputations;
        GridView view;


    }

    public class Vertical_GridLayout_Builder
    {
        public Vertical_GridLayout_Builder AddLayout(LayoutChoice_Set subLayout)
        {
            this.subLayouts.AddLast(subLayout);
            return this;
        }
        public Vertical_GridLayout_Builder Uniform()
        {
            this.uniform = true;
            return this;
        }
        public GridLayout Build()
        {
            BoundProperty_List rowHeights = new BoundProperty_List(this.subLayouts.Count);
            if (this.uniform)
            {
                for (int i = 1; i < rowHeights.NumProperties; i++)
                {
                    rowHeights.BindIndices(0, i);
                }
            }
            GridLayout grid = GridLayout.New(rowHeights, new BoundProperty_List(1), LayoutScore.Zero);
            foreach (LayoutChoice_Set sublayout in this.subLayouts)
            {
                grid.AddLayout(sublayout);
            }
            return grid;
        }

        private LinkedList<LayoutChoice_Set> subLayouts = new LinkedList<LayoutChoice_Set>();
        private bool uniform = false;
    }

    public class Horizontal_GridLayout_Builder
    {
        public Horizontal_GridLayout_Builder AddLayout(LayoutChoice_Set subLayout)
        {
            this.subLayouts.AddLast(subLayout);
            return this;
        }
        public GridLayout Build()
        {
            BoundProperty_List columnWidths = new BoundProperty_List(this.subLayouts.Count);
            if (this.uniform)
            {
                for (int i = 1; i < columnWidths.NumProperties; i++)
                {
                    columnWidths.BindIndices(0, i);
                }
            }

            GridLayout grid = GridLayout.New(new BoundProperty_List(1), columnWidths, LayoutScore.Zero);
            foreach (LayoutChoice_Set sublayout in this.subLayouts)
            {
                grid.AddLayout(sublayout);
            }
            return grid;
        }

        private LinkedList<LayoutChoice_Set> subLayouts = new LinkedList<LayoutChoice_Set>();
        private bool uniform = false;
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // tells the precise locations of some gridlines in a GridLayout
    class SemiFixed_GridLayout : SpecificLayout
    {
        public SemiFixed_GridLayout()
        {
            this.Initialize();
        }
        public SemiFixed_GridLayout(bool setWidthBeforeHeight)
        {
            this.Initialize();
            this.setWidthBeforeHeight = setWidthBeforeHeight;
        }
        public SemiFixed_GridLayout(SemiFixed_GridLayout original)
        {
            this.Initialize();
            this.CopyFrom(original);
        }
        public SemiFixed_GridLayout(LayoutChoice_Set[,] elements, BoundProperty_List rowHeights, BoundProperty_List columnWidths, LayoutScore bonusScore, bool setWidthBeforeHeight)
        {
            this.Initialize();
            this.elements = elements;
            int numRows = elements.GetLength(1);
            int numColumns = elements.GetLength(0);
            this.rowHeights = new BoundProperty_List(rowHeights);
            this.columnWidths = new BoundProperty_List(columnWidths);
            this.bonusScore = bonusScore;
            this.setWidthBeforeHeight = setWidthBeforeHeight;
            //this.subLayouts = new SpecificLayout[numColumns, numRows];
        }
        public override SpecificLayout Clone()
        {
            SemiFixed_GridLayout clone = new SemiFixed_GridLayout();
            clone.CopyFrom(this);
            return clone;
        }
        public void CopyFrom(SemiFixed_GridLayout original)
        {
            base.CopyFrom(original);
            //this.subLayouts = new SpecificLayout[,](original.subLayouts);
            this.rowHeights = new BoundProperty_List(original.rowHeights);
            this.columnWidths = new BoundProperty_List(original.columnWidths);
            this.elements = original.elements;
            this.nextDimensionToSet = original.nextDimensionToSet;
            this.bonusScore = original.bonusScore;
            this.setWidthBeforeHeight = original.setWidthBeforeHeight;
            this.score = original.score;
            this.view = original.view;
        }
        private void Initialize()
        {
            //this.view = new GridView();
        }
        public int NumSubLayouts
        {
            get
            {
                int count = 0;
                foreach (LayoutChoice_Set layout in this.elements)
                {
                    if (layout != null)
                        count++;
                }
                return count;
            }
        }
        public bool SetWidthBeforeHeight
        {
            get
            {
                return this.setWidthBeforeHeight;
            }
        }
        // tells whether the next coordinate to be applied will alter the width of the layout (rather than the height)
        public bool NextCoordinateAffectsWidth
        {
            get
            {
                if (this.setWidthBeforeHeight)
                {
                    if (this.nextDimensionToSet < this.columnWidths.NumGroups)
                        return true;
                    else
                        return false;
                }
                else
                {
                    if (this.nextDimensionToSet < this.rowHeights.NumGroups)
                        return false;
                    else
                        return true;
                }
            }
        }

        public int NumUnsetCoordinatesInCurrentDimension
        {
            get
            {
                if (this.setWidthBeforeHeight)
                {
                    if (this.NextCoordinateAffectsWidth)
                        return this.columnWidths.NumGroups - this.nextDimensionToSet;
                    else
                        return this.rowHeights.NumGroups + this.columnWidths.NumGroups - this.nextDimensionToSet;
                }
                else
                {
                    if (this.NextCoordinateAffectsWidth)
                        return this.rowHeights.NumGroups + this.columnWidths.NumGroups - this.nextDimensionToSet;
                    else
                        return this.rowHeights.NumGroups - this.nextDimensionToSet;
                }
            }
        }
        public int NumCoordinatesSetInCurrentDimension
        {
            get
            {
                if (this.setWidthBeforeHeight)
                {
                    if (this.NextCoordinateAffectsWidth)
                        return this.nextDimensionToSet;
                    else
                        return this.nextDimensionToSet - this.columnWidths.NumGroups;
                }
                else
                {
                    if (this.NextCoordinateAffectsWidth)
                        return this.nextDimensionToSet - this.rowHeights.NumGroups;
                    else
                        return this.nextDimensionToSet;
                }
            }
        }
        public void AddCoordinate(double value)
        {
            if (Double.IsNaN(value))
                throw new ArgumentException();
            if (this.NextCoordinateAffectsWidth)
            {
                this.columnWidths.Set_GroupTotal(this.NumCoordinatesSetInCurrentDimension, value);
            }
            else
            {
                this.rowHeights.Set_GroupTotal(this.NumCoordinatesSetInCurrentDimension, value);
            }
            this.nextDimensionToSet++;
            this.InvalidateScore();
        }
        public double LatestCoordinateValue
        {
            get
            {
                return this.GetCoordinate(this.nextDimensionToSet - 1);
            }
        }
        public double GetCoordinate(int index)
        {
            if (this.setWidthBeforeHeight)
            {
                if (index < this.columnWidths.NumGroups)
                    return this.columnWidths.Get_GroupTotal(index);
                else
                    return this.rowHeights.Get_GroupTotal(index - this.columnWidths.NumGroups);
            }
            else
            {
                if (index < this.rowHeights.NumGroups)
                    return this.rowHeights.Get_GroupTotal(index);
                else
                    return this.columnWidths.Get_GroupTotal(index - this.rowHeights.NumGroups);
            }
        }
        public double GetNextCoordinate_UpperBound_ForQuery(LayoutQuery query)
        {
            // it's annoying that these subtractions can cause rounding errors
            double total, subtraction, remainder;
            if (this.NextCoordinateAffectsWidth)
            {
                total = query.MaxWidth;
                subtraction = this.columnWidths.GetTotalValue();
            }
            else
            {
                total = query.MaxHeight;
                subtraction = this.rowHeights.GetTotalValue();
            }
            remainder = total - subtraction;
            double error = (subtraction + remainder) - total;
            if (error != 0)
            {
                // rounding error
                if (error > 0)
                    remainder -= error;
                error = (subtraction + remainder) - total;
                if (error > 0)
                    System.Diagnostics.Debug.WriteLine("Rounding Error!");
            }
            if (Double.IsNaN(remainder))
                throw new InvalidOperationException();
            return remainder;
        }
        public override double Width
        {
            get
            {
                return this.columnWidths.GetTotalValue();
            }
        }
        public override double Height
        {
            get
            {
                return this.rowHeights.GetTotalValue();
            }
        }
        /*
        public override LayoutDimensions Dimensions
        {
            get
            {
                LayoutDimensions dimensions = new LayoutDimensions();
                dimensions.Width = this.GetRequiredWidth();
                dimensions.Height = this.GetRequiredHeight();
                dimensions.Score = this.ComputeScore();
                return dimensions;
            }
        }
        */
        public void InvalidateScore()
        {
            this.score = null;
        }

        public override LayoutScore Score
        {
            get
            {
                if (this.score == null)
                    this.score = this.ComputeScore();
                return this.score;
            }
        }

        public void SetScore(LayoutScore newScore)
        {
            this.score = newScore;
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
#if true
            return base.GetBestLayout(query);
#else
            // alternate implementation of GetBestLayout(LayoutQuery), which can be faster when the result is null, but is slower when the result is not null, and in practice is slower
            if (this.score != null)
            {
                // if we've already computed a score then just use that
                return base.GetBestLayout(query);
            }
            this.subLayouts = new SpecificLayout[this.elements.GetLength(0), this.elements.GetLength(1)];
            LayoutScore targetScore = new LayoutScore(query.MinScore);
            targetScore = LayoutScore.Minimum;
            LayoutScore totalScore = LayoutScore.Zero;
            int rowNumber, columnNumber;
            double width, height;
            int numRemainingLayouts = this.NumSubLayouts;

            bool changed = false;

            for (int i = 0; i < 1; i++)
            {
                changed = false;
                for (rowNumber = 0; rowNumber < this.rowHeights.NumProperties; rowNumber++)
                {
                    height = this.rowHeights.GetValue(rowNumber);
                    for (columnNumber = 0; columnNumber < this.columnWidths.NumProperties; columnNumber++)
                    {
                        width = this.columnWidths.GetValue(columnNumber);
                        LayoutChoice_Set subLayout = this.elements[columnNumber, rowNumber];
                        // make sure there is a layout at that location, and that we haven't already computed its score
                        if (subLayout != null && this.subLayouts[columnNumber, rowNumber] == null)
                        {
                            MaxScore_LayoutQuery subQuery = new MaxScore_LayoutQuery();
                            subQuery.MaxWidth = width;
                            subQuery.MaxHeight = height;
                            if (numRemainingLayouts == 1)
                                subQuery.MinScore = targetScore.Minus(totalScore).Times((double)1 / (double)numRemainingLayouts);
                            else
                                subQuery.MinScore = LayoutScore.Minimum;
                            SpecificLayout layout = subLayout.GetBestLayout(subQuery);
                            if (layout != null)
                            {
                                this.subLayouts[columnNumber, rowNumber] = this.prepareLayoutForQuery(layout, subQuery);
                                totalScore = totalScore.Plus(layout.Score);
                                numRemainingLayouts--;
                                changed = true;
                            }
                        }
                    }
                }
                if (numRemainingLayouts == 0 || !changed)
                    break;
            }
            if (numRemainingLayouts == 0)
            {
                // the total score for each sublayout was enough to satisfy the query
                this.score = totalScore;
                return base.GetBestLayout(query);
            }
            // the total score for each sublayout wasn't enough to satisfy the query, so we didn't even bother computing the total score
            return null;
#endif
        }

        private LayoutScore ComputeScore()
        {
            this.subLayouts = new SpecificLayout[this.elements.GetLength(0), this.elements.GetLength(1)];
            LayoutScore totalScore = this.bonusScore;
            int rowNumber, columnNumber;
            double width, height;
            for (rowNumber = 0; rowNumber < this.rowHeights.NumProperties; rowNumber++)
            {
                //height = this.RoundHeightDown(this.rowHeights.GetValue(rowNumber));
                height = this.rowHeights.GetValue(rowNumber);
                for (columnNumber = 0; columnNumber < this.columnWidths.NumProperties; columnNumber++)
                {
                    //width = this.RoundWidthDown(this.columnWidths.GetValue(columnNumber));
                    width = this.columnWidths.GetValue(columnNumber);
                    LayoutChoice_Set subLayout = this.elements[columnNumber, rowNumber];
                    if (subLayout != null)
                    {
                        MaxScore_LayoutQuery query = new MaxScore_LayoutQuery();
                        query.MaxWidth = Math.Max(width, 0);
                        query.MaxHeight = Math.Max(height, 0);
                        SpecificLayout layout = subLayout.GetBestLayout(query);
                        totalScore = totalScore.Plus(layout.Score);
                        this.subLayouts[columnNumber, rowNumber] = this.prepareLayoutForQuery(layout, query);
                        //this.childLayouts.Add(layout);
                    }
                }
            }
            return totalScore;
        }
        public int NumForcedDimensions
        {
            get
            {
                return this.nextDimensionToSet;
            }
        }
        public bool HasUnforcedDimensions
        {
            get
            {
                if (this.nextDimensionToSet < this.rowHeights.NumGroups + this.columnWidths.NumGroups)
                    return true;
                return false;
            }
        }
        /*public BoundProperty_List RowHeights
        {
            get
            {
                return this.rowHeights;
            }
        }
        public BoundProperty_List ColumnWidths
        {
            get
            {
                return this.columnWidths;
            }
        }*/
        public double Get_GroupWidth(int groupIndex)
        {
            return this.columnWidths.Get_GroupTotal(groupIndex);
        }
        public List<int> Get_WidthGroup_AtIndex(int groupIndex)
        {
            return this.columnWidths.GetGroupAtIndex(groupIndex);
        }
        public double GetWidthFraction(int propertyIndex)
        {
            return this.columnWidths.GetFraction(propertyIndex);
        }
        public double GetWidthCoordinate(int propertyIndex)
        {
            return this.columnWidths.GetValue(propertyIndex);
        }
        public int GetNumWidthGroups()
        {
            return this.columnWidths.NumGroups;
        }
        public int GetNumWidthProperties()
        {
            return this.columnWidths.NumProperties;
        }
        public int Get_WidthGroup_Index(int propertyIndex)
        {
            return this.columnWidths.GetGroupIndexFromPropertyIndex(propertyIndex);
        }
        public void Set_GroupWidth(int groupIndex, double newValue)
        {
            this.columnWidths.Set_GroupTotal(groupIndex, newValue);
            this.InvalidateScore();
        }
        public void SetWidthMinValues(int groupIndex, List<double> minValues)
        {
            if (this.columnWidths.SetMinValues(groupIndex, minValues))
                this.InvalidateScore();
        }
        public void SetWidthValue(int propertyIndex, double newValue)
        {
            this.columnWidths.SetValue(propertyIndex, newValue);
            this.InvalidateScore();
        }

        public List<int> Get_HeightGroup_AtIndex(int groupIndex)
        {
            return this.rowHeights.GetGroupAtIndex(groupIndex);
        }
        public double GetHeightFraction(int propertyIndex)
        {
            return this.rowHeights.GetFraction(propertyIndex);
        }
        public double GetHeightCoordinate(int propertyIndex)
        {
            return this.rowHeights.GetValue(propertyIndex);
        }
        public double Get_GroupHeight(int groupIndex)
        {
            return this.rowHeights.Get_GroupTotal(groupIndex);
        }
        public int GetNumHeightGroups()
        {
            return this.rowHeights.NumGroups;
        }
        public int GetNumHeightProperties()
        {
            return this.rowHeights.NumProperties;
        }
        public int Get_HeightGroup_Index(int propertyIndex)
        {
            return this.rowHeights.GetGroupIndexFromPropertyIndex(propertyIndex);
        }
        public void Set_GroupHeight(int groupIndex, double newValue)
        {
            this.rowHeights.Set_GroupTotal(groupIndex, newValue);
            this.InvalidateScore();
        }
        public void SetHeightMinValues(int groupIndex, List<double> minValues)
        {
            if (this.rowHeights.SetMinValues(groupIndex, minValues))
                this.InvalidateScore();
        }
        public void SetHeightValue(int propertyIndex, double newValue)
        {
            this.rowHeights.SetValue(propertyIndex, newValue);
            this.InvalidateScore();
        }

        public int NextDimensionToSet
        {
            get
            {
                return this.nextDimensionToSet;
            }
        }
        public override FrameworkElement DoLayout(Size bounds)
        {
            int rowNumber, columnNumber;
            double unscaledX, unscaledY;    // paying a lot of attention to rounding;
            double nextUnscaledX, nextUnscaledY;
            double unscaledHeight, unscaledWidth;
            double x, y, nextX, nextY;

            // the actual provided size might be slightly more than we asked for, so rescale accordingly
            double horizontalScale, desiredWidth, verticalScale, desiredHeight;
            desiredWidth = this.columnWidths.GetTotalValue();
            if (desiredWidth <= 0)
                horizontalScale = 0;
            else
                horizontalScale = bounds.Width / desiredWidth;
            desiredHeight = this.rowHeights.GetTotalValue();
            if (desiredHeight <= 0)
                verticalScale = 0;
            else
                verticalScale = bounds.Height / desiredHeight;
            y = unscaledY = 0;
            List<double> columnWidths = new List<double>();
            List<double> rowHeights = new List<double>();
            FrameworkElement[,] subviews = new FrameworkElement[this.columnWidths.NumProperties, this.rowHeights.NumProperties];
            for (rowNumber = 0; rowNumber < this.rowHeights.NumProperties; rowNumber++)
            {
                // compute the coordinates in the unscaled coordinate system
                unscaledHeight = this.rowHeights.GetValue(rowNumber);
                nextUnscaledY = unscaledY + unscaledHeight;
                // stretch the coordinates to this coordinate system
                nextY = nextUnscaledY * verticalScale;
                double height = nextY - y;
                rowHeights.Add(height);

                x = unscaledX = 0;
                columnWidths.Clear();
                for (columnNumber = 0; columnNumber < this.columnWidths.NumProperties; columnNumber++)
                {
                    // compute the coordinates in the unscaled coordinate system
                    unscaledWidth = this.columnWidths.GetValue(columnNumber);
                    nextUnscaledX = unscaledX + unscaledWidth;
                    // stretch the coordinates to this coordinate system
                    nextX = nextUnscaledX * horizontalScale;
                    double width = nextX - x;
                    columnWidths.Add(width);


                    LayoutChoice_Set subLayout = this.elements[columnNumber, rowNumber];
                    FrameworkElement subview = null;
                    if (subLayout != null)
                    {
                        MaxScore_LayoutQuery query = new MaxScore_LayoutQuery();
                        query.MaxWidth = width;
                        query.MaxHeight = height;
                        SpecificLayout bestLayout = subLayout.GetBestLayout(query);
                        this.specificSublayouts.AddLast(bestLayout);
                        subview = bestLayout.DoLayout(new Size((int)width, (int)height));
                    }
                    subviews[columnNumber, rowNumber] = subview;

                    unscaledX = nextUnscaledX;
                    x = nextX;
                }
                unscaledY = nextUnscaledY;
                y = nextY;
            }
            // Now that the computation is done, update the GridView
            this.GridView.SetDimensions(columnWidths, rowHeights);
            this.GridView.SetChildren(subviews);
            return this.View;
        }

        public override FrameworkElement View
        {
            get { return this.GridView; }
        }
        public GridView GridView
        {
            get
            {
                if (this.view == null)
                    this.view = new GridView();
                return this.view;
            }
            set
            {
                if (this.view == null)
                    this.view = value;
                else
                    throw new InvalidOperationException();
            }
        }

        public SpecificLayout GetChildLayout(int columnNumber, int rowNumber)
        {
            return this.subLayouts[columnNumber, rowNumber];
        }

        public override void Remove_VisualDescendents()
        {
            this.GridView.Remove_VisualDescendents();
            foreach (SpecificLayout layout in this.specificSublayouts)
            {
                layout.Remove_VisualDescendents();
            }
            this.specificSublayouts.Clear();
        }

        LayoutChoice_Set[,] elements;
        int nextDimensionToSet;
        BoundProperty_List rowHeights;
        BoundProperty_List columnWidths;
        SpecificLayout[,] subLayouts;
        LayoutScore bonusScore;
        bool setWidthBeforeHeight;
        GridView view;
        LayoutScore score;
        LinkedList<SpecificLayout> specificSublayouts = new LinkedList<SpecificLayout>();
        //private List<SpecificLayout> childLayouts;
        //LayoutDimensions cachedDimensions;
    }

    // A CompositeGridLayout just has a few GridLayouts inside it, which results in better (more-granular) caching than just one grid with everything
    class CompositeGridLayout : GridLayout
    {
        public CompositeGridLayout(int numRows, int numColumns, LayoutScore bonusScore)
        {
            this.numRows = numRows;
            this.numTopRows = (this.numRows +  1) / 2;
            this.numColumns = numColumns;
            this.numLeftColumns = (this.numColumns + 1) / 2;
            int actualNumRows = Math.Min(numRows, 2);
            int actualNumColumns = Math.Min(numColumns, 2);
            this.Initialize(new BoundProperty_List(actualNumRows), new BoundProperty_List(actualNumColumns), bonusScore);
            this.gridLayouts = new GridLayout[actualNumColumns, actualNumRows];
            base.PutLayout(this.gridLayouts[0, 0] = GridLayout.New(new BoundProperty_List(numTopRows), new BoundProperty_List(numLeftColumns), LayoutScore.Zero), 0, 0);
            if (numColumns > 1)
                base.PutLayout(this.gridLayouts[1, 0] = GridLayout.New(new BoundProperty_List(numTopRows), new BoundProperty_List(numColumns - numLeftColumns), LayoutScore.Zero), 1, 0);
            if (numRows > 1)
                base.PutLayout(this.gridLayouts[0, 1] = GridLayout.New(new BoundProperty_List(numRows - numTopRows), new BoundProperty_List(numColumns), LayoutScore.Zero), 0, 1);
        }
        public override void PutLayout(LayoutChoice_Set layout, int xIndex, int yIndex)
        {
            int primaryXIndex = 0;
            int primaryYIndex = 0;
            if (xIndex >= this.numLeftColumns)
            {
                xIndex -= this.numLeftColumns;
                primaryXIndex++;
            }
            if (yIndex >= this.numTopRows)
            {
                yIndex -= this.numTopRows;
                primaryYIndex++;
            }

            this.gridLayouts[primaryXIndex, primaryYIndex].PutLayout(layout, xIndex, yIndex);
        }
        public override int NumRows
        {
            get
            {
                return this.numRows;
            }
        }
        public override int NumColumns
        {
            get
            {
                return this.numColumns;
            }
        }
        public override LayoutChoice_Set GetLayout(int xIndex, int yIndex)
        {
            int primaryXIndex = 0;
            int primaryYIndex = 0;
            if (xIndex >= this.numLeftColumns)
            {
                xIndex -= this.numLeftColumns;
                primaryXIndex++;
            }
            if (yIndex >= this.numTopRows)
            {
                yIndex -= this.numTopRows;
                primaryYIndex++;
            }
            return this.gridLayouts[primaryXIndex, primaryYIndex].GetLayout(xIndex, yIndex);
        }
        private GridLayout[,] gridLayouts;
        private int numRows;
        private int numColumns;
        private int numTopRows;
        private int numLeftColumns;
    }
}
