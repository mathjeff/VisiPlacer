using System;
using System.Collections.Generic;
using Xamarin.Forms;

// the GridLayout will arrange the child elements in a grid pattern
namespace VisiPlacement
{
    public class GridLayout : LayoutChoice_Set
    {
        public static int NumQueries = 0;
        public static int NumComputations = 0;
        public static int ExpensiveThreshold = 10;

        public static GridLayout New(BoundProperty_List rowHeights, BoundProperty_List columnWidths, LayoutScore bonusScore)
        {
            if (Math.Min(rowHeights.NumGroups, columnWidths.NumGroups) <= 1)
            {
                if (Math.Max(rowHeights.NumGroups, columnWidths.NumGroups) > 2)
                {
                    // we should instead make a smaller grid with additional grids inside it, to better enable caching
                    if (rowHeights.NumGroups == rowHeights.NumProperties && columnWidths.NumGroups == columnWidths.NumProperties)
                    {
                        // we do support composition from smaller grids in this case
                        return new CompositeGridLayout(rowHeights.NumGroups, columnWidths.NumGroups, bonusScore);
                    }
                    // don't yet support automatically making a smaller grid in this case
                    // Probably should support this case though
                }
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
            this.wrappedChildren = new LayoutChoice_Set[numColumns, numRows];
            this.givenChildren = new LayoutChoice_Set[numColumns, numRows];
            this.rowHeights = rowHeights;
            this.columnWidths = columnWidths;
            this.bonusScore = bonusScore;
            this.pixelSize = 1;
            this.view = new GridView();
        }
        // puts this layout in the designated part of the grid
        public virtual void PutLayout(LayoutChoice_Set layout, int xIndex, int yIndex)
        {
            if (this.GetLayout(xIndex, yIndex) == layout)
                return;
            LayoutChoice_Set wrappedLayout = layout;
            LayoutChoice_Set previousLayout = this.wrappedChildren[xIndex, yIndex];
            if (previousLayout != null)
                previousLayout.RemoveParent(this);

            if (wrappedLayout != null)
            {
                wrappedLayout = LayoutCache.For(wrappedLayout);
                wrappedLayout = new PixelatedLayout(wrappedLayout, this.pixelSize);
                wrappedLayout.AddParent(this);
            }
            this.wrappedChildren[xIndex, yIndex] = wrappedLayout;
            this.givenChildren[xIndex, yIndex] = layout;
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
            return this.givenChildren[xIndex, yIndex];
        }
        public virtual int NumRows
        {
            get
            {
                return this.wrappedChildren.GetLength(1);
            }
        }
        public virtual int NumColumns
        {
            get
            {
                return this.wrappedChildren.GetLength(0);
            }
        }

        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            GridLayout.NumQueries++;
            /*LayoutChoice_Set uniqueChild = this.UniqueChild;
            if (uniqueChild != null)
                return uniqueChild.GetBestLayout(query);*/

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
            SemiFixed_GridLayout layout = this.GetBestLayout(query, new SemiFixed_GridLayout(this.wrappedChildren, this.rowHeights, this.columnWidths, this.bonusScore, setWidthBeforeHeight));
            if (layout != null && !query.Accepts(layout))
            {
                ErrorReporter.ReportParadox("error");
                LayoutQuery debugQuery = query.Clone();
                debugQuery.Debug = true;
                SemiFixed_GridLayout debugResult = this.GetBestLayout(debugQuery, new SemiFixed_GridLayout(this.wrappedChildren, this.rowHeights, this.columnWidths, this.bonusScore, setWidthBeforeHeight));
            }
            if (query.MinScore.Equals(LayoutScore.Minimum))
            {
                if (layout == null)
                {
                    ErrorReporter.ReportParadox("Layout " + this + " returned illegal null layout for query " + query);
                    return this.GetBestLayout(query);
                }
            }

            //if (query.MaximizesScore())
            {
                SemiFixed_GridLayout shrunken = null;
                if (layout != null)
                {
                    if (layout.Width > 0 && layout.Height > 0)
                    {
                        shrunken = this.ShrinkLayout(new SemiFixed_GridLayout(layout), query.Debug);
                        if (query.PreferredLayout(shrunken, layout) != shrunken)
                        {
                            ErrorReporter.ReportParadox("error");
                        }
                        layout = shrunken;
                    }
                }

            }

            if (layout != null)
                layout.GridView = this.view; // reuse the same view to decrease the amount of redrawing the caller has to do

            if (layout != null)
            {
                if (this.RoundWidthDown(layout.Width) != layout.Width || this.RoundHeightDown(layout.Height) != layout.Height)
                {
                    ErrorReporter.ReportParadox("Error; GridLayout didn't round size down");
                    return this.GetBestLayout(query);
                }
            }

            if (layout != null)
            {
                if (query.MinimizesHeight() && double.IsInfinity(layout.Height))
                {
                    ErrorReporter.ReportParadox("Infinite height requested by " + layout + " for " + query);
                    this.GetBestLayout(query);
                }
                if (query.MinimizesWidth() && double.IsInfinity(layout.Width))
                {
                    ErrorReporter.ReportParadox("Infinite width requested by " + layout + " for " + query);
                    this.GetBestLayout(query);
                }
            }
            if (layout != null)
            {
                if (layout.score == null)
                {
                    // Any layout we return to our caller is likely to need to know what its score is, for example for use by a LayoutCache
                    // So we make sure to compute the score before returning it
                    layout.SetScore(layout.Score);
                }
            }

            return this.prepareLayoutForQuery(layout, query);
        }

        // If this GridLayout has exactly one child, this property returns it
        // If this GridLayout has any other number of children, this property returns null
        private LayoutChoice_Set UniqueChild
        {
            get
            {
                LayoutChoice_Set result = null;
                for (int i = 0; i < this.rowHeights.NumProperties; i++)
                {
                    for (int j = 0; j < this.columnWidths.NumProperties; j++)
                    {
                        LayoutChoice_Set thisChild = this.wrappedChildren[j, i];
                        if (thisChild != null)
                        {
                            if (result != null)
                                return null; // no unique child
                            result = thisChild;
                        }
                    }
                }
                return result;
            }
        }

        // given some constraints and coordinates, returns a list of coordinates that are worth considering
        private List<SemiFixed_GridLayout> GetLayoutsToConsider(LayoutQuery query, SemiFixed_GridLayout semiFixedLayout)
        {
            SemiFixed_GridLayout debugAnswer = null;
            bool prevCoordinateShouldBeRight = false;
            if (query.Debug)
            {
                debugAnswer = query.ProposedSolution_ForDebugging as SemiFixed_GridLayout;
                if (debugAnswer != null)
                {
                    if (semiFixedLayout.Do_AllSetCoordinates_Match(debugAnswer))
                    {
                        prevCoordinateShouldBeRight = true;
                    }
                }
            }

            List<SemiFixed_GridLayout> results = new List<SemiFixed_GridLayout>();
            // round down because the layout is pixelized
            if (semiFixedLayout.HasUnforcedDimensions == false)
            {
                // we've finally pinned a value to each coordinate; now we return the layout if it satisfies the criteria

                if (query.Accepts(semiFixedLayout))
                    results.Add(semiFixedLayout);
                if (prevCoordinateShouldBeRight && results.Count < 1)
                {
                    ErrorReporter.ReportParadox("GridLayout did not find expected solution " + debugAnswer + " to query " + query);
                }
                return results;
            }
            // We haven't yet set a value for each dimension, so we should try a bunch of values for this next dimension
            double maxValue = semiFixedLayout.GetNextCoordinate_UpperBound_ForQuery(query);
            if (semiFixedLayout.NextCoordinateAffectsWidth)
                maxValue = this.RoundWidthDown(maxValue);
            else
                maxValue = this.RoundHeightDown(maxValue);
            double currentCoordinate = maxValue;
            if (currentCoordinate == 0)
            {
                // no space remaining for the new coordinate, so just zero it and continue
                semiFixedLayout.AddCoordinate(0);
                results = this.GetLayoutsToConsider(query, semiFixedLayout);
                if (prevCoordinateShouldBeRight && results.Count < 1)
                {
                    ErrorReporter.ReportParadox("GridLayout did not find expected solution " + debugAnswer + " to query " + query);
                }
                return results;
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
                        if (semiFixedLayout.Width != this.RoundWidthDown(query.MaxWidth))
                        {
                            ErrorReporter.ReportParadox("rounding error: semiFixedLayout.GetRequiredWidth() (" + semiFixedLayout.Width.ToString() + ") != query.MaxWidth (" + query.MaxWidth.ToString() + ")");
                            ErrorReporter.ReportParadox("rounding error is " + (semiFixedLayout.Width - query.MaxWidth));
                        }
                    }
                    else
                    {
                        if (semiFixedLayout.Height != this.RoundHeightDown(query.MaxHeight))
                        {
                            ErrorReporter.ReportParadox("rounding error: semiFixedLayout.GetRequiredHeight() (" + semiFixedLayout.Height.ToString() + ") != query.MaxHeight (" + query.MaxHeight.ToString() + ")");
                            ErrorReporter.ReportParadox("rounding error is " + (semiFixedLayout.Height - query.MaxHeight));
                        }
                    }
                    results = this.GetLayoutsToConsider(query, semiFixedLayout);
                    if (prevCoordinateShouldBeRight && results.Count < 1)
                    {
                        ErrorReporter.ReportParadox("GridLayout did not find expected solution " + debugAnswer + " to query " + query);
                    }
                    return results;
                }
            }

            SemiFixed_GridLayout bestSublayout = null;
            SemiFixed_GridLayout currentSublayout = null;
            bool increaseScore = true;
            int numIterations = 0;
            LayoutScore maxExistentScore = null; // becomes non-null if we discover what it is
            int index = semiFixedLayout.NumCoordinatesSetInCurrentDimension;
            while (currentCoordinate >= 0)
            {
                numIterations++;
                GridLayout.NumComputations++;
                if (increaseScore)
                {
                    // if we get here, we must increase the score of the layout without increasing the size too much

                    // If we want to eventually find the layout with max score, then start by checking for that
                    List<SemiFixed_GridLayout> newSublayouts = new List<SemiFixed_GridLayout>();
                    SemiFixed_GridLayout newSublayout = null;
                    if (query.MaximizesScore())
                    {
                        // maximize the score while keeping the size to the required value
                        SemiFixed_GridLayout currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                        currentLayout.AddCoordinate(currentCoordinate);

                        if (semiFixedLayout.NextCoordinateAffectsWidth != semiFixedLayout.SetWidthBeforeHeight)
                        {
                            // We've already finished setting the width or the height before this coordinate
                            // So, it's safe to autoshrink this coordinate if the sublayouts are saying they don't need all the space
                            if (semiFixedLayout.NextCoordinateAffectsWidth)
                            {
                                this.ShrinkWidth(currentLayout, index, query, LayoutScore.Zero, false);
                                currentCoordinate = this.RoundWidthDown(currentLayout.Get_GroupWidth(index));
                            }
                            else
                            {
                                this.ShrinkHeight(currentLayout, index, query, LayoutScore.Zero, false);
                                currentCoordinate = this.RoundHeightDown(currentLayout.Get_GroupHeight(index));
                            }

                            if (prevCoordinateShouldBeRight)
                            {
                                if (currentCoordinate < debugAnswer.GetCoordinate(semiFixedLayout.NextDimensionToSet))
                                {
                                    // We're passing the answer. Have we found it yet?
                                    if (query.PreferredLayout(bestSublayout, debugAnswer) == debugAnswer)
                                    {
                                        ErrorReporter.ReportParadox("Incorrectly passed the answer at dim " + semiFixedLayout.NextDimensionToSet + "; skipping to value " + currentCoordinate);
                                    }
                                }
                            }

                        }

                        // recursively query the next dimension
                        newSublayouts = this.GetLayoutsToConsider(query, currentLayout);
                        results.AddRange(newSublayouts);

                        newSublayout = GridLayout.PreferredLayout(query, newSublayouts);
                        if (newSublayout != null)
                        {
                            newSublayout = new SemiFixed_GridLayout(newSublayout);
                            currentSublayout = newSublayout;
                        }
                    }



                    if (newSublayout == null)
                    {
                        // If we didn't find any layout of this size that is valid, then relax the constraints but look for the thinnest one having enough score
                        // if we already have one solution and we are trying to maximize the score, then we only care about solutions that improve beyond that
                        LayoutScore minScore = query.MinScore;
                        if (query.MaximizesScore() && bestSublayout != null)
                            minScore = minScore.Plus(LayoutScore.Tiny);
                        double maxWidth, maxHeight;
                        if (semiFixedLayout.NextCoordinateAffectsWidth)
                        {
                            // bring the score to the required value and minimize the width
                            maxWidth = query.MaxWidth;
                            if (semiFixedLayout.NumUnsetCoordinatesInCurrentDimension > 1)
                                maxWidth += currentCoordinate;
                            maxHeight = query.MaxHeight;
                        }
                        else
                        {
                            // bring the score to the required value and minimize the height
                            maxWidth = query.MaxWidth;
                            maxHeight = query.MaxHeight;
                            if (semiFixedLayout.NumUnsetCoordinatesInCurrentDimension > 1)
                                maxHeight += currentCoordinate;
                        }

                        LayoutQuery subQuery;
                        if (semiFixedLayout.NextCoordinateAffectsWidth)
                            subQuery = new MinWidth_LayoutQuery(maxWidth, maxHeight, minScore);
                        else
                            subQuery = new MinHeight_LayoutQuery(maxWidth, maxHeight, minScore);
                        subQuery.Debug = query.Debug;


                        SemiFixed_GridLayout currentLayout = new SemiFixed_GridLayout(semiFixedLayout);
                        // start by setting the maximum coordinate, and decrease it until it gets to zero
                        currentLayout.AddCoordinate(currentCoordinate);

                        // recursively query the next dimension
                        newSublayouts = this.GetLayoutsToConsider(subQuery, currentLayout);
                        results.AddRange(newSublayouts);
                        

                        currentSublayout = GridLayout.PreferredLayout(subQuery, newSublayouts);
                        if (currentSublayout == null)
                        {
                            if (prevCoordinateShouldBeRight)
                            {
                                // We're passing the answer. Have we found it yet?
                                if (query.PreferredLayout(bestSublayout, debugAnswer) == debugAnswer)
                                {
                                    ErrorReporter.ReportParadox("GridLayout incorrectly giving up with at dim " + semiFixedLayout.NextDimensionToSet);
                                }
                            }
                            break;
                        }
                        currentSublayout = new SemiFixed_GridLayout(currentSublayout);

                        int numInfiniteWidths = currentSublayout.NumInfiniteWidths;
                        int numInfiniteHeights = currentSublayout.NumInfiniteHeights;
                        bool usesAllWidth = (currentSublayout.Width >= subQuery.MaxWidth) && (numInfiniteWidths == 0 || numInfiniteWidths == currentSublayout.GetNumWidthProperties());
                        bool usesAllHeight = (currentSublayout.Height >= subQuery.MaxHeight) && (numInfiniteHeights == 0 || numInfiniteHeights == currentSublayout.GetNumHeightProperties());
                        bool offeredExtraSpace = (subQuery.MaxWidth > query.MaxWidth || double.IsPositiveInfinity(subQuery.MaxWidth) || subQuery.MaxHeight > query.MaxHeight || double.IsPositiveInfinity(subQuery.MaxHeight));
                        if (usesAllWidth && usesAllHeight && offeredExtraSpace)
                        {
                            // If the layout that we found uses at least as much space as allowed, then it must also have at least as much score as possible
                            maxExistentScore = currentSublayout.Score;
                        }
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
                    double newCurrentCoordinate;
                    if (semiFixedLayout.NextCoordinateAffectsWidth)
                    {
                        // determine max width to satisfy query
                        double maxAcceptibleWidth;
                        if (!double.IsPositiveInfinity(query.MaxWidth))
                            maxAcceptibleWidth = semiFixedLayout.Get_GroupWidth(index) - (semiFixedLayout.Width - query.MaxWidth);
                        else
                            maxAcceptibleWidth = double.PositiveInfinity;
                        // determine max width required for us to be making progress
                        double maxInterestingWidth;
                        if (maxExistentScore != null && !query.MinimizesWidth() && maxExistentScore.Equals(currentSublayout.Score) && query.Accepts(currentSublayout))
                            maxInterestingWidth = currentCoordinate;
                        else
                            maxInterestingWidth = currentCoordinate - this.pixelSize;
                        if (semiFixedLayout.SetWidthBeforeHeight)
                        {
                            // if we compute width before height
                            // then we can increase the available height before shrinking the width, in case that lets us shrink the width more
                            currentSublayout.TryToRescaleToTotalHeight(this.RoundHeightDown(query.MaxHeight));
                        }
                        // now shrink the width
                        double maxWidth = Math.Min(maxInterestingWidth, maxAcceptibleWidth);
                        this.ShrinkWidth(currentSublayout, index, query, allowedScoreDecrease, true);
                        // update some variables
                        if (currentSublayout.Get_GroupWidth(index) == currentCoordinate)
                        {
                            // We couldn't decrease the size without decreasing the score
                            if (maxInterestingWidth == currentCoordinate)
                            {
                                // We don't need to decrease the score anymore so we're done

                                if (prevCoordinateShouldBeRight)
                                {
                                    // We're passing the answer. Have we found it yet?
                                    if (query.PreferredLayout(bestSublayout, debugAnswer) == debugAnswer)
                                    {
                                        ErrorReporter.ReportParadox("GridLayout incorrectly believes unable to decrease the width without decreasing the score");
                                    }
                                }
                                break;
                            }
                        }
                        if (currentSublayout.Get_GroupWidth(index) > maxWidth)
                        {
                            // Couldn't shrink this cordinate without dropping to too low of a score
                            if (double.IsPositiveInfinity(query.MaxWidth))
                            {
                                // If we already had infinite space, then don't try making more room for the next coordinate

                                if (prevCoordinateShouldBeRight)
                                {
                                    // We're passing the answer. Have we found it yet?
                                    if (query.PreferredLayout(bestSublayout, debugAnswer) == debugAnswer)
                                    {
                                        ErrorReporter.ReportParadox("GridLayout exiting too early with infinite width");
                                    }
                                }

                                break;
                            }
                            // Note that we have to step currentCoordinate on its own rather than running ShrinkWidth
                            // because ShrinkWidth assumes that no other coordinates change
                            currentSublayout.Set_GroupWidth(index, maxWidth);
                            // We don't want to run ShrinkWidth here because we probably introduced cropping
                            // It's probably fast to prove that this new layout isn't a solution, and probably slow to run ShrinkWidth
                            //if (!semiFixedLayout.SetWidthBeforeHeight)
                            //    this.ShrinkWidth(currentSublayout, index, query, LayoutScore.Zero, true);
                        }

                        newCurrentCoordinate = this.RoundWidthDown(currentSublayout.Get_GroupWidth(index));
                    }
                    else
                    {
                        // determine max height to satisfy query
                        double maxAcceptibleHeight;
                        if (!double.IsPositiveInfinity(query.MaxHeight))
                            maxAcceptibleHeight = semiFixedLayout.Get_GroupHeight(index) - (semiFixedLayout.Height - query.MaxHeight);
                        else
                            maxAcceptibleHeight = double.PositiveInfinity;
                        // determine max height required for us to be making progress
                        double maxInterestingHeight;
                        if (maxExistentScore != null && !query.MinimizesHeight() && maxExistentScore.Equals(currentSublayout.Score) && query.Accepts(currentSublayout))
                            maxInterestingHeight = currentCoordinate;
                        else
                            maxInterestingHeight = currentCoordinate - this.pixelSize;
                        if (!semiFixedLayout.SetWidthBeforeHeight)
                        {
                            // if we compute height before width,
                            // then we can increase the available width before shrinking the height, in case that lets us shrink the height more
                            currentSublayout.TryToRescaleToTotalWidth(this.RoundWidthDown(query.MaxWidth));
                        }
                        // now shrink the height
                        double maxHeight = Math.Min(maxInterestingHeight, maxAcceptibleHeight);
                        this.ShrinkHeight(currentSublayout, index, query, allowedScoreDecrease, true);
                        // update some variables
                        if (currentSublayout.Get_GroupHeight(index) == currentCoordinate)
                        {
                            // We couldn't decrease the size without decreasing the score
                            if (maxInterestingHeight == currentCoordinate)
                            {
                                // We don't need to decrease the score anymore so we're done

                                if (prevCoordinateShouldBeRight)
                                {
                                    // We're passing the answer. Have we found it yet?
                                    if (query.PreferredLayout(bestSublayout, debugAnswer) == debugAnswer)
                                    {
                                        ErrorReporter.ReportParadox("GridLayout incorrectly believes unable to decrease the height without decreasing the score");
                                    }
                                }

                                break;
                            }
                        }
                        if (currentSublayout.Get_GroupHeight(index) > maxHeight)
                        {
                            // Couldn't shrink this cordinate without dropping to too low of a score
                            if (double.IsPositiveInfinity(query.MaxHeight))
                            {
                                // If we already had infinite space, then don't try making more room for the next coordinate

                                if (prevCoordinateShouldBeRight)
                                {
                                    // We're passing the answer. Have we found it yet?
                                    if (query.PreferredLayout(bestSublayout, debugAnswer) == debugAnswer)
                                    {
                                        ErrorReporter.ReportParadox("GridLayout exiting too early with infinite height");
                                    }
                                }

                                break;
                            }
                            // Note that we have to step currentCoordinate on its own rather than running ShrinkHeight
                            // because ShrinkHeight assumes that no other coordinates change
                            currentSublayout.Set_GroupHeight(index, maxHeight);
                            // We don't want to run ShrinkHeight here because we probably introduced cropping
                            // It's probably fast to prove that this new layout isn't a solution, and probably slow to run ShrinkHeight
                            //if (semiFixedLayout.SetWidthBeforeHeight)
                            //    this.ShrinkHeight(currentSublayout, index, query, LayoutScore.Zero, true);
                        }

                        newCurrentCoordinate = this.RoundHeightDown(currentSublayout.Get_GroupHeight(index));
                    }
                    if (prevCoordinateShouldBeRight)
                    {
                        if (newCurrentCoordinate < debugAnswer.GetCoordinate(semiFixedLayout.NextDimensionToSet))
                        {
                            // We're passing the answer. Have we found it yet?
                            if (query.PreferredLayout(bestSublayout, debugAnswer) == debugAnswer)
                            {
                                ErrorReporter.ReportParadox("Incorrectly passed the answer at dim " + semiFixedLayout.NextDimensionToSet + "; skipping to value " + newCurrentCoordinate);
                                newCurrentCoordinate = currentCoordinate;
                            }
                        }
                    }
                    currentCoordinate = newCurrentCoordinate;

                }
                // keep track of the coordinates that we are considering
                //results.Add(new SemiFixed_GridLayout(currentSublayout));
                if (results.Count > ExpensiveThreshold)
                {
                    System.Diagnostics.Debug.WriteLine("Surprisingly slow query " + query + " in GridLayout " + this.DebugId + ": " + results.Count +
                        " results so far, currentCoordinate = " + currentCoordinate);
                    ExpensiveThreshold *= 2;
                }
                // keep track of the best layout so far
                if (GridLayout.PreferredLayout(query, currentSublayout, bestSublayout) == currentSublayout)
                {
                    bestSublayout = new SemiFixed_GridLayout(currentSublayout);
                    if (bestSublayout != null)
                    {
                        if (query.Accepts(bestSublayout))
                        {
                            // if we've found a layout that works, then any future layouts we find must be at least as good as this one
                            query = query.OptimizedUsingExample(bestSublayout);
                        }
                        results.Add(bestSublayout);
                        //System.Diagnostics.Debug.WriteLine("Gridlayout currentSublayout: " + currentSublayout + " with score " + currentSublayout.Score);
                    }
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
            if (prevCoordinateShouldBeRight && results.Count < 1)
            {
                ErrorReporter.ReportParadox("GridLayout did not find expected solution " + debugAnswer + " to query " + query);
            }

            //System.Diagnostics.Debug.WriteLine("num grid iterations = " + numIterations + " for size " + query.MaxWidth + "x" + query.MaxHeight);
            return results;
        }

        private static SemiFixed_GridLayout PreferredLayout(LayoutQuery query, IEnumerable<SemiFixed_GridLayout> choices)
        {
            SemiFixed_GridLayout result = null;
            foreach (SemiFixed_GridLayout choice in choices)
            {
                if (GridLayout.PreferredLayout(query, result, choice) == choice)
                    result = choice;
            }
            return result;
        }
        // Tells which SemiFixed_GridLayout is better and takes into account how many infinite coordinates there are
        private static SemiFixed_GridLayout PreferredLayout(LayoutQuery query, SemiFixed_GridLayout tieWinner, SemiFixed_GridLayout tieLoser)
        {
            if (!query.Accepts(tieWinner))
            {
                if (query.Accepts(tieLoser))
                    return tieLoser;
                else
                    return null;
            }
            if (!query.Accepts(tieLoser))
                return tieWinner;
            if (query.MinimizesWidth())
            {
                if (tieWinner.NumInfiniteWidths < tieLoser.NumInfiniteWidths)
                    return tieWinner;
                if (tieWinner.NumInfiniteWidths > tieLoser.NumInfiniteWidths)
                    return tieLoser;
            }
            if (query.MinimizesHeight())
            {
                if (tieWinner.NumInfiniteHeights < tieLoser.NumInfiniteHeights)
                    return tieWinner;
                if (tieWinner.NumInfiniteHeights > tieLoser.NumInfiniteHeights)
                    return tieLoser;
            }
            if (query.PreferredLayout(tieWinner, tieLoser) == tieWinner)
                return tieWinner;
            return tieLoser;
        }


        // I need to revamp the GetBestLayout function so that it processes constraints in different orders based on the query
        private SemiFixed_GridLayout GetBestLayout(LayoutQuery query, SemiFixed_GridLayout semiFixedLayout)
        {
            bool isMinScore = query.MinScore.Equals(LayoutScore.Minimum);
            SemiFixed_GridLayout bestLayout = null;
            IEnumerable<SemiFixed_GridLayout> layouts;
            /*if (this.rowHeights.NumProperties > 1 && this.columnWidths.NumProperties > 1)
            {
                System.Diagnostics.Debug.WriteLine("GridLayout working on difficult query");
            }*/

            layouts = this.GetLayoutsToConsider(query, semiFixedLayout);

            /*if (this.rowHeights.NumProperties > 1 && this.columnWidths.NumProperties > 1)
            {
                System.Diagnostics.Debug.WriteLine("GridLayout completed difficult query");
            }*/

            bestLayout = GridLayout.PreferredLayout(query, layouts);

            if (isMinScore)
            {
                if (bestLayout == null)
                {
                    ErrorReporter.ReportParadox("Layout " + this + " returned illegal null layout for query " + query);
                    List<SemiFixed_GridLayout> layoutList = new List<SemiFixed_GridLayout>(layouts);
                    System.Diagnostics.Debug.WriteLine("Identified these " + layoutList.Count + " layouts: " + layoutList);
                    foreach (SemiFixed_GridLayout layout in layouts)
                    {
                        System.Diagnostics.Debug.WriteLine("Accepts " + layout + "? " + query.Accepts(layout));
                    }
                    this.GetBestLayout(query);
                }
            }
            query.OnAnswered(this);
            return bestLayout;
        }

        // shrinks the specified width a lot without decreasing the layout's score by any more than allowedScoreDecrease
        // sourceQuery is the LayoutQuery that caused this call to ShrinkWidth
        private void ShrinkWidth(SemiFixed_GridLayout layout, int indexOf_propertyGroup_toShrink, LayoutQuery sourceQuery, LayoutScore totalAllowedScoreDecrease, bool runAllSubqueries)
        {
            if (totalAllowedScoreDecrease.CompareTo(LayoutScore.Zero) < 0)
            {
                ErrorReporter.ReportParadox("Error: cannot improve score by shrinking");
            }

            List<int> indices = layout.Get_WidthGroup_AtIndex(indexOf_propertyGroup_toShrink);
            List<double> minWidths = new List<double>();
            LayoutScore eachAllowedScoreDecrease = totalAllowedScoreDecrease.Times((double)1 / (double)(this.NumRows * indices.Count));
            LayoutScore actualScoreDecrease = LayoutScore.Zero;
            LayoutScore originalScore = null;
            //if (sourceQuery.Debug)
            //    originalScore = layout.Score;
            // the current setup is invalid, so shrinking the width down to a valid value is adequate
            foreach (int columnNumber in indices)
            {
                int rowNumber;
                double maxRequiredWidth = 0;
                for (rowNumber = 0; rowNumber < this.rowHeights.NumProperties; rowNumber++)
                {
                    double currentWidth = 0;
                    LayoutChoice_Set subLayout = this.wrappedChildren[columnNumber, rowNumber];
                    if (subLayout != null)
                    {
                        // ask the view for the highest-scoring size that fits within the specified dimensions
                        LayoutQuery query = new MaxScore_LayoutQuery(layout.GetWidthCoordinate(columnNumber), layout.GetHeightCoordinate(rowNumber), LayoutScore.Minimum);
                        query.Debug = sourceQuery.Debug;
                        if (query.Debug)
                        {
                            SemiFixed_GridLayout converted = sourceQuery.ProposedSolution_ForDebugging as SemiFixed_GridLayout;
                            if (converted != null)
                            {
                                SpecificLayout possibleSolution = converted.GetChildLayout(columnNumber, rowNumber);
                                // Because ShrinkWidth is used on intermediate grids that might not be the final return from the GridLayout,
                                // this SemiFixed_GridLayout might have some coordinates not equal to those of the original query
                                // So we can only use this debug solution if its source query has the same dimensions
                                if (possibleSolution.Width == query.MaxWidth && possibleSolution.Height == query.MaxHeight)
                                {
                                    query.ProposedSolution_ForDebugging = possibleSolution;
                                }
                            }

                        }
                        GridLayout.NumComputations++;
                        SpecificLayout bestLayout = subLayout.GetBestLayout(query);

                        SpecificLayout layout2;
                        if (runAllSubqueries)
                        {
                            // figure out how far the view can shrink while keeping the same score
                            LayoutQuery query2 = new MinWidth_LayoutQuery(query.MaxWidth, query.MaxHeight, bestLayout.Score.Minus(eachAllowedScoreDecrease));
                            query2.Debug = sourceQuery.Debug;

                            layout2 = subLayout.GetBestLayout(query2);

                            if (layout2 == null)
                            {
                                ErrorReporter.ReportParadox("Error: min-width query did not find result from max-score query");
                                LayoutQuery debugQuery1 = query2.Clone();
                                debugQuery1.Debug = true;
                                debugQuery1.ProposedSolution_ForDebugging = bestLayout;
                                SpecificLayout debugResult1 = subLayout.GetBestLayout(debugQuery1.Clone());

                                // note, also, that the layout cache seems to have an incorrect value for when minScore = -infinity
                                LayoutQuery debugQuery2 = query.Clone();
                                debugQuery2.Debug = true;
                                debugQuery2.ProposedSolution_ForDebugging = debugResult1;
                                subLayout.GetBestLayout(debugQuery2.Clone());

                                LayoutQuery debugQuery3 = debugQuery1.WithScore(LayoutScore.Minimum);
                                SpecificLayout layout3 = subLayout.GetBestLayout(debugQuery3);
                                ErrorReporter.ReportParadox("");
                            }
                            if (!query2.Accepts(layout2))
                                ErrorReporter.ReportParadox("Error: min-width query received an invalid response");
                            actualScoreDecrease = actualScoreDecrease.Plus(bestLayout.Score.Minus(layout2.Score));
                        }
                        else
                        {
                            layout2 = bestLayout;
                        }

                        currentWidth = layout2.Width;
                        if (currentWidth > maxRequiredWidth)
                        {
                            maxRequiredWidth = currentWidth;
                            if (maxRequiredWidth == query.MaxWidth)
                            {
                                break;
                            }
                        }
                    }
                }
                minWidths.Add(maxRequiredWidth);
            }

            layout.SetWidthMinValues(indexOf_propertyGroup_toShrink, minWidths);
            layout.Set_GroupWidth(indexOf_propertyGroup_toShrink, this.RoundWidthUp(layout.Get_GroupWidth(indexOf_propertyGroup_toShrink)));
            if (originalScore != null)
            {
                if (layout.Score.CompareTo(originalScore.Minus(totalAllowedScoreDecrease)) < 0)
                    ErrorReporter.ReportParadox("Error; ShrinkWidth decreased the score too much");
            }
        }

        // shrinks the specified height as much as possible without decreasing the layout's score
        private void ShrinkHeight(SemiFixed_GridLayout layout, int indexOf_propertyGroup_toShrink, LayoutQuery sourceQuery, LayoutScore totalAllowedScoreDecrease, bool runAllSubqueries)
        {
            if (totalAllowedScoreDecrease.CompareTo(LayoutScore.Zero) < 0)
            {
                ErrorReporter.ReportParadox("Error: cannot improve score by shrinking");
            }
            List<int> indices = layout.Get_HeightGroup_AtIndex(indexOf_propertyGroup_toShrink);
            List<double> minHeights = new List<double>();
            LayoutScore eachAllowedScoreDecrease = totalAllowedScoreDecrease.Times((double)1 / (double)(this.NumColumns * indices.Count));
            LayoutScore actualScoreDecrease = LayoutScore.Zero;
            LayoutScore originalScore = null;
            //if (sourceQuery.Debug)
            //    originalScore = layout.Score;
            foreach (int rowNumber in indices)
            {
                int columnNumber;
                double maxRequiredHeight = 0;
                for (columnNumber = 0; columnNumber < this.columnWidths.NumProperties; columnNumber++)
                {
                    double currentHeight = 0;
                    LayoutChoice_Set subLayout = this.wrappedChildren[columnNumber, rowNumber];
                    if (subLayout != null)
                    {
                        // ask the view for the highest-scoring size that fits within the specified dimensions
                        LayoutQuery query = new MaxScore_LayoutQuery(layout.GetWidthCoordinate(columnNumber), layout.GetHeightCoordinate(rowNumber), LayoutScore.Minimum);
                        query.Debug = sourceQuery.Debug;
                        if (query.Debug)
                        {
                            SemiFixed_GridLayout converted = sourceQuery.ProposedSolution_ForDebugging as SemiFixed_GridLayout;
                            if (converted != null)
                            {
                                SpecificLayout possibleSolution = converted.GetChildLayout(columnNumber, rowNumber);
                                // Because ShrinkHeight is used on intermediate grids that might not be the final return from the GridLayout,
                                // this SemiFixed_GridLayout might have some coordinates not equal to those of the original query
                                // So we can only use this debug solution if its source query has the same dimensions
                                if (possibleSolution.Width == query.MaxWidth && possibleSolution.Height == query.MaxHeight)
                                {
                                    query.ProposedSolution_ForDebugging = possibleSolution;
                                }
                            }
                        }
                        GridLayout.NumComputations++;
                        SpecificLayout bestLayout = subLayout.GetBestLayout(query);

                        SpecificLayout layout2;
                        if (runAllSubqueries)
                        {
                            // figure out how far the view can shrink while keeping the same score
                            LayoutQuery query2 = new MinHeight_LayoutQuery(query.MaxWidth, query.MaxHeight, bestLayout.Score.Minus(eachAllowedScoreDecrease));
                            query2.Debug = sourceQuery.Debug;
                            //query2.ProposedSolution_ForDebugging = sourceQuery.ProposedSolution_ForDebugging;

                            layout2 = subLayout.GetBestLayout(query2);
                            if (layout2 == null)
                            {
                                ErrorReporter.ReportParadox("Error: min-height query did not find result from max-score query");
                                // note, also, that the layout cache seems to have an incorrect value for when minScore = -infinity
                                LayoutQuery debugQuery1 = query2.Clone();
                                debugQuery1.Debug = true;
                                debugQuery1.ProposedSolution_ForDebugging = bestLayout;
                                SpecificLayout debugResult1 = subLayout.GetBestLayout(debugQuery1.Clone());

                                // note, also, that the layout cache seems to have an incorrect value for when minScore = -infinity
                                LayoutQuery debugQuery2 = query.Clone();
                                debugQuery2.Debug = true;
                                debugQuery2.ProposedSolution_ForDebugging = debugResult1;
                                subLayout.GetBestLayout(debugQuery2.Clone());

                                LayoutQuery debugQuery3 = debugQuery1.WithScore(LayoutScore.Minimum);
                                SpecificLayout layout3 = subLayout.GetBestLayout(debugQuery3);
                                ErrorReporter.ReportParadox("");
                            }

                            if (!query2.Accepts(layout2))
                            {
                                ErrorReporter.ReportParadox("Error: min-height query received an invalid response");
                            }
                            actualScoreDecrease = actualScoreDecrease.Plus(bestLayout.Score.Minus(layout2.Score));
                        }
                        else
                        {
                            layout2 = bestLayout;
                        }

                        currentHeight = layout2.Height;
                        if (currentHeight > maxRequiredHeight)
                        {
                            maxRequiredHeight = currentHeight;
                            if (maxRequiredHeight == query.MaxHeight)
                            {
                                break;
                            }
                        }

                    }
                }
                minHeights.Add(maxRequiredHeight);
            }
            layout.SetHeightMinValues(indexOf_propertyGroup_toShrink, minHeights);
            layout.Set_GroupHeight(indexOf_propertyGroup_toShrink, this.RoundHeightUp(layout.Get_GroupHeight(indexOf_propertyGroup_toShrink)));
            if (originalScore != null)
            {
                if (layout.Score.CompareTo(originalScore.Minus(totalAllowedScoreDecrease)) < 0)
                    ErrorReporter.ReportParadox("Error; ShrinkHeight decreased the score too much");
            }
        }


        private double GetUsableColumnWidth(int columnIndex, SemiFixed_GridLayout layout)
        {
            return this.RoundWidthDown(layout.GetWidthCoordinate(columnIndex));
        }
        private double GetUsableRowHeight(int rowIndex, SemiFixed_GridLayout layout)
        {
            return this.RoundHeightDown(layout.GetHeightCoordinate(rowIndex));
        }

        private double RoundWidthUp(double value)
        {
            return Math.Ceiling(value / this.pixelSize) * this.pixelSize;
        }
        private double RoundHeightUp(double value)
        {
            return this.RoundWidthUp(value);
        }

        public double RoundWidthDown(double value)
        {
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
            for (i = 0; i < layout.GetNumHeightGroups(); i++)
            {
                groupRowHeights.Add(0);
            }
            List<double> requiredRowHeights = new List<double>(layout.GetNumHeightProperties());
            for (i = 0; i < layout.GetNumHeightProperties(); i++)
            {
                requiredRowHeights.Add(0);
            }

            List<double> groupColumnWidths = new List<double>(layout.GetNumWidthGroups());
            for (i = 0; i < layout.GetNumWidthGroups(); i++)
            {
                groupColumnWidths.Add(0);
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
                    LayoutChoice_Set currentLayout = this.wrappedChildren[columnNumber, rowNumber];
                    if (currentLayout != null)
                    {
                        // ask the subview for the best layout that fits in these dimensions. Hopefully, it will generate a result that is no larger than needed
                        LayoutQuery query = new MaxScore_LayoutQuery(width, height, LayoutScore.Minimum);
                        query.Debug = enableDebugging;
                        SpecificLayout subLayout = currentLayout.GetBestLayout(query);
                        double requiredWidth = this.RoundWidthUp(subLayout.Width);
                        double requiredHeight = this.RoundHeightUp(subLayout.Height);
                        if (query.Debug)
                        {
                            query = new MaxScore_LayoutQuery(requiredWidth, requiredHeight, LayoutScore.Minimum);
                            SpecificLayout recheckedLayout = currentLayout.GetBestLayout(query);
                            if (query.PreferredLayout(subLayout, recheckedLayout) != subLayout)
                                ErrorReporter.ReportParadox("Error; sublayout cannot re-find its own response");

                        }
                        // update out how large the rows and columns need to be, using this new information
                        double requiredGroupWidth = this.RoundWidthUp(requiredWidth / layout.GetWidthFraction(columnNumber));
                        double requiredGroupHeight = this.RoundHeightUp(requiredHeight / layout.GetHeightFraction(rowNumber));

                        int groupRowIndex = layout.Get_HeightGroup_Index(rowNumber);
                        if (groupRowHeights[groupRowIndex] < requiredGroupHeight)
                        {
                            groupRowHeights[groupRowIndex] = requiredGroupHeight;
                        }
                        int groupColumnIndex = layout.Get_WidthGroup_Index(columnNumber);
                        if (groupColumnWidths[groupColumnIndex] < requiredGroupWidth)
                        {
                            groupColumnWidths[groupColumnIndex] = requiredGroupWidth;
                        }
                    }
                }
            }
            LayoutScore newScore;
            // update each row and column
            for (i = 0; i < groupRowHeights.Count; i++)
            {
                if (groupRowHeights[i] < layout.Get_GroupHeight(i))
                {
                    layout.Set_GroupHeight(i, groupRowHeights[i]);
                }
                if (enableDebugging)
                {
                    newScore = layout.Score;
                    if (newScore.CompareTo(previousScore) < 0)
                    {
                        ErrorReporter.ReportParadox("Rounding error appears to be taking place in a sublayout");
                        MaxScore_LayoutQuery query = new MaxScore_LayoutQuery(previousLayout.Width, previousLayout.Height, previousLayout.Score);
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
                    layout.Set_GroupWidth(i, groupColumnWidths[i]);
                }
                if (enableDebugging)
                {
                    newScore = layout.Score;
                    if (newScore.CompareTo(previousScore) < 0)
                    {
                        ErrorReporter.ReportParadox("Rounding error appears to be taking place in a sublayout");
                        if (!enableDebugging)
                            this.ShrinkLayout(new SemiFixed_GridLayout(previousLayout), true);
                        MaxScore_LayoutQuery query = new MaxScore_LayoutQuery(previousLayout.Width, previousLayout.Height, previousLayout.Score);
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
                    ErrorReporter.ReportParadox("Rounding error");
            }
            for (i = 0; i < layout.GetNumWidthProperties(); i++)
            {
                if (layout.GetWidthCoordinate(i) < requiredColumnWidths[i])
                    ErrorReporter.ReportParadox("Rounding error");
            }
            newScore = layout.Score;
            if (newScore.CompareTo(previousScore) < 0)
            {
                ErrorReporter.ReportParadox("Rounding error appears to be taking place in a sublayout");
                if (!enableDebugging)
                    this.ShrinkLayout(new SemiFixed_GridLayout(previousLayout), true);
                MaxScore_LayoutQuery query = new MaxScore_LayoutQuery(previousLayout.Width, previousLayout.Height, previousLayout.Score);
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



        // the children that we given to us by the caller
        private LayoutChoice_Set[,] givenChildren;
        // We add some extra wrapping around elements for caching and for snapping to gridlines. These are the wrapped layouts
        private LayoutChoice_Set[,] wrappedChildren;
        private BoundProperty_List rowHeights;
        private BoundProperty_List columnWidths;
        private LayoutScore bonusScore;
        double pixelSize;   // for rounding error, we snap all values to a multiple of pixelSize
        int nextOpenRow;
        int nextOpenColumn;
        GridView view;

    }

    public class Vertical_GridLayout_Builder
    {
        public Vertical_GridLayout_Builder AddLayout(LayoutChoice_Set subLayout)
        {
            this.subLayouts.AddLast(subLayout);
            return this;
        }
        public Vertical_GridLayout_Builder AddLayouts(IEnumerable<LayoutChoice_Set> subLayouts)
        {
            foreach (LayoutChoice_Set subLayout in subLayouts)
            {
                this.AddLayout(subLayout);
            }
            return this;
        }
        public Vertical_GridLayout_Builder Uniform()
        {
            this.uniform = true;
            return this;
        }
        // builds a GridLayout
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
        public LayoutChoice_Set BuildAnyLayout()
        {
            if (this.subLayouts.Count == 1)
                return this.subLayouts.First.Value;
            return this.Build();
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
        public Horizontal_GridLayout_Builder AddLayouts(IEnumerable<LayoutChoice_Set> subLayouts)
        {
            foreach (LayoutChoice_Set subLayout in subLayouts)
            {
                this.AddLayout(subLayout);
            }
            return this;
        }
        public Horizontal_GridLayout_Builder Uniform()
        {
            this.uniform = true;
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

        public LayoutChoice_Set BuildAnyLayout()
        {
            if (this.subLayouts.Count == 1)
                return this.subLayouts.First.Value;
            return this.Build();
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
            this.rowHeights = new BoundProperty_List(original.rowHeights);
            this.columnWidths = new BoundProperty_List(original.columnWidths);
            this.elements = original.elements;
            this.nextDimensionToSet = original.nextDimensionToSet;
            this.bonusScore = original.bonusScore;
            this.setWidthBeforeHeight = original.setWidthBeforeHeight;
            this.SetScore(original.score);
            this.view = original.view;
            this.sub_specificLayouts = original.sub_specificLayouts;
        }
        private void Initialize()
        {
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
            if (double.IsNaN(value))
                throw new ArgumentException("Illegal dimension: " + value);
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
        public bool Do_AllSetCoordinates_Match(SemiFixed_GridLayout other)
        {
            for (int i = 0; i < this.nextDimensionToSet; i++)
            {
                if (this.GetCoordinate(i) != other.GetCoordinate(i))
                    return false;
            }
            return true;
        }
        public double LatestCoordinateValue
        {
            get
            {
                return this.GetCoordinate(this.nextDimensionToSet - 1);
            }
        }
        public int NumInfiniteWidths
        {
            get
            {
                return this.columnWidths.NumInfiniteProperties;
            }
        }
        public int NumInfiniteHeights
        {
            get
            {
                return this.rowHeights.NumInfiniteProperties;
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
            if (double.IsPositiveInfinity(total))
            {
                return double.PositiveInfinity;
            }
            double error = (subtraction + remainder) - total;
            if (error != 0)
            {
                // rounding error
                if (error > 0)
                    remainder -= error;
                error = (subtraction + remainder) - total;
                if (error > 0)
                    ErrorReporter.ReportParadox("Rounding Error!");
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

        public override String ToString()
        {
            string score = "?";
            if (this.score != null)
               score = this.score.ToString();
            return "SemiFixed_GridLayout: (" + String.Join(",", this.columnWidths.Values) + ") x (" + String.Join(",", this.rowHeights.Values) + "): (" + score + ")";
        }
        public void InvalidateScore()
        {
            this.SetScore(null);
        }

        public override LayoutScore Score
        {
            get
            {
                if (this.score == null)
                    this.ComputeScore();
                return this.score;
            }
        }

        public void SetScore(LayoutScore newScore)
        {
            this.score = newScore;
        }

        // TODO this should probably be deduplicated with isScoreAtLeast()
        private LayoutScore ComputeScore()
        {
            if (this.sub_specificLayouts == null)
                this.sub_specificLayouts = new SpecificLayout[this.elements.GetLength(0), this.elements.GetLength(1)];
            LayoutScore totalScore = this.bonusScore;
            int rowNumber, columnNumber;
            double width, height;
            for (rowNumber = 0; rowNumber < this.rowHeights.NumProperties; rowNumber++)
            {
                height = this.rowHeights.GetValue(rowNumber);
                for (columnNumber = 0; columnNumber < this.columnWidths.NumProperties; columnNumber++)
                {
                    width = this.columnWidths.GetValue(columnNumber);
                    LayoutChoice_Set subLayout = this.elements[columnNumber, rowNumber];
                    if (subLayout != null)
                    {
                        SpecificLayout previousLayout = this.sub_specificLayouts[columnNumber, rowNumber];
                        SpecificLayout layout;
                        if (previousLayout == null || previousLayout.Width != width || previousLayout.Height != height)
                        {
                            MaxScore_LayoutQuery query = new MaxScore_LayoutQuery(width, height, LayoutScore.Minimum);
                            layout = subLayout.GetBestLayout(query);
                            this.sub_specificLayouts[columnNumber, rowNumber] = layout;
                        }
                        else
                        {
                            layout = previousLayout;
                        }
                        totalScore = totalScore.Plus(layout.Score);
                    }
                }
            }
            this.SetScore(totalScore);
            return totalScore;
        }

        // tells whether this.Score is at least as large as the given score, without necessarily computing this.Score exactly
        // TODO this should probably save this.Score in cases where it computes the score - we just have to worry about setting this.subLayouts correctly too
        protected override bool isScoreAtLeast(LayoutQuery query)
        {
            if (query.MinScore.Equals(LayoutScore.Minimum))
            {
                // It is required to always be true that our score is at least the minimum possible score
                // Also, the minimum possible score uses negative infinity as a component,
                // so if several of our sublayouts report the minimum score as their own score, then it would introduce some errors when we try to add and subtract them
                return true;
            }
            if ((this.Width <= 0 || this.Height <= 0) && this.score == null)
            {
                // If we don't have any space then our children won't have any space either, and it should be fast to directly compute our score
                this.ComputeScore();
            }
            LayoutScore targetScore = query.MinScore;
            if (this.score != null)
                return (this.score.CompareTo(targetScore) >= 0);
            // Make a list of how much space we're giving to each sublayout
            List<LayoutAndSize> unscoredLayouts = new List<LayoutAndSize>();
            int rowNumber, columnNumber;
            double width, height;
            for (rowNumber = 0; rowNumber < this.rowHeights.NumProperties; rowNumber++)
            {
                height = this.rowHeights.GetValue(rowNumber);
                for (columnNumber = 0; columnNumber < this.columnWidths.NumProperties; columnNumber++)
                {
                    width = this.columnWidths.GetValue(columnNumber);
                    LayoutChoice_Set layout = this.elements[columnNumber, rowNumber];
                    if (layout != null)
                        unscoredLayouts.Add(new LayoutAndSize(layout, new Size(width, height)));
                }
            }
            // look for any sublayouts that can increase the score by at least the required average amount
            LayoutScore currentScore = this.bonusScore;
            bool seemsSufficient = true;
            while (unscoredLayouts.Count > 0)
            {
                LayoutScore requiredExtraScore = targetScore.Minus(currentScore);

                LayoutScore averageRequiredExtraScore = requiredExtraScore.Times(1.0 / unscoredLayouts.Count);
                List<LayoutAndSize> nextUnscoredLayouts = new List<LayoutAndSize>();

                for (int i = 0; i < unscoredLayouts.Count; i++)
                {
                    LayoutAndSize layoutAndSize = unscoredLayouts[i];
                    if (nextUnscoredLayouts.Count == 0 && i == unscoredLayouts.Count - 1 && i > 0)
                    {
                        // If all other layouts were able to reach the target successfully, then we can recompute a new target score before running the last query
                        nextUnscoredLayouts.Add(layoutAndSize);
                        break;
                    }

                    // loosen the score requirements slightly in case of rounding error
                    MaxScore_LayoutQuery subQuery = new MaxScore_LayoutQuery(layoutAndSize.Size.Width, layoutAndSize.Size.Height, LayoutScore.Min(averageRequiredExtraScore.Times(0.9999), averageRequiredExtraScore.Times(1.0001)));
                    SpecificLayout result = layoutAndSize.Layout.GetBestLayout(subQuery);
                    if (result != null)
                        currentScore = currentScore.Plus(result.Score);
                    else
                        nextUnscoredLayouts.Add(layoutAndSize);
                }

                if (nextUnscoredLayouts.Count >= unscoredLayouts.Count)
                {
                    // no sublayout could make at least an average increase, therefore we can't increase the total up to the target
                    seemsSufficient = false;
                    break;
                }
                unscoredLayouts = nextUnscoredLayouts;
            }
            // If we have a score with multiple components, then when we adjust the target for rounding error, it's possible that we will compute a score for each child but still not have enough
            // The first component should be satisfied but other components might not be
            if (currentScore.CompareTo(targetScore) < 0)
            {
                seemsSufficient = false;
            }
            if (query.Debug)
            {
                bool rightAnswer = (this.Score.CompareTo(targetScore) >= 0);
                if (rightAnswer != seemsSufficient)
                    ErrorReporter.ReportParadox("incorrect answer from SemiFixed_GridLayout.isScoreAtLeast");
                return rightAnswer;
            }
            if (!seemsSufficient)
                return false;
            // If our above calculations indicated that our score is sufficient, then we've computed a score for each child layout, which is essentially all of the work required to compute our score
            // Nowever, we may have added the results together in a non-canonical order and may still encounter rounding errors
            // Now we recalculate the score in the standard order and compare that to our target
            return this.Score.CompareTo(targetScore) >= 0;
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
        public void TryToRescaleToTotalWidth(double width)
        {
            this.columnWidths.TryToRescaleToTotalValue(width);
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
        public void TryToRescaleToTotalHeight(double height)
        {
            this.rowHeights.TryToRescaleToTotalValue(height);
            this.InvalidateScore();
        }


        public int NextDimensionToSet
        {
            get
            {
                return this.nextDimensionToSet;
            }
        }
        public override View DoLayout(Size bounds)
        {
            return this.DoLayout_Impl(bounds, false);
        }
        private View DoLayout_Impl(Size bounds, bool dryRun)
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
            View[,] subviews = new View[this.columnWidths.NumProperties, this.rowHeights.NumProperties];
            LayoutScore totalScore = this.bonusScore;
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
                    View subview = null;
                    if (subLayout != null)
                    {
                        MaxScore_LayoutQuery query = new MaxScore_LayoutQuery(unscaledWidth, unscaledHeight, LayoutScore.Minimum);
                        SpecificLayout bestLayout = subLayout.GetBestLayout(query);
                        totalScore = totalScore.Plus(bestLayout.Score);
                        subview = bestLayout.DoLayout(new Size(width, height));
                    }
                    subviews[columnNumber, rowNumber] = subview;

                    unscaledX = nextUnscaledX;
                    x = nextX;
                }
                unscaledY = nextUnscaledY;
                y = nextY;
            }
            if (this.score != null)
            {
                if (this.score.CompareTo(totalScore) != 0)
                {
                    ErrorReporter.ReportParadox("Score discrepancy in " + this + "; previously computed " + this.score + "; recomputed " + totalScore);
                    //LayoutScore recomputed = this.ComputeScore();
                    //ErrorReporter.ReportParadox("Recomputed score = " + recomputed);
                }
            }
            if (!dryRun)
            {
                // Now that the computation is done, update the GridView
                this.GridView.SetDimensions(columnWidths, rowHeights);
                this.GridView.SetChildren(subviews);
            }
            return this.View;
        }

        public override View View
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
                    ErrorReporter.ReportParadox("Reassigned GridView in a GridLayout - did the debugger access this property previously?");
            }
        }

        public SpecificLayout GetChildLayout(int columnNumber, int rowNumber)
        {
            if (this.sub_specificLayouts == null)
                this.ComputeScore();
            return this.sub_specificLayouts[columnNumber, rowNumber];
        }

        public override void Remove_VisualDescendents()
        {
            this.GridView.Remove_VisualDescendents();
            foreach (SpecificLayout layout in this.GetParticipatingChildren())
            {
                layout.Remove_VisualDescendents();
            }
        }

        public override IEnumerable<SpecificLayout> GetParticipatingChildren()
        {
            if (this.sub_specificLayouts == null)
            {
                this.DoLayout_Impl(new Size(this.columnWidths.GetTotalValue(), this.rowHeights.GetTotalValue()), true);
            }
            List<SpecificLayout> results = new List<SpecificLayout>();
            for (int i = 0; i < this.columnWidths.NumProperties; i++)
            {
                for (int j = 0; j < this.rowHeights.NumProperties; j++)
                {
                    SpecificLayout candidate = this.sub_specificLayouts[i, j];
                    if (candidate != null)
                        results.Add(candidate);
                }
            }
            return results;
        }

        LayoutChoice_Set[,] elements;
        int nextDimensionToSet;
        BoundProperty_List rowHeights;
        BoundProperty_List columnWidths;
        LayoutScore bonusScore;
        bool setWidthBeforeHeight;
        GridView view;
        public LayoutScore score;
        SpecificLayout[,] sub_specificLayouts;
    }

    // A CompositeGridLayout just has a few GridLayouts inside it, which results in better (more-granular) caching than just one grid with everything
    class CompositeGridLayout : GridLayout
    {
        public CompositeGridLayout(int numRows, int numColumns, LayoutScore bonusScore)
        {
            this.numRows = numRows;
            this.numTopRows = Math.Max(this.numRows / 4, 1);
            this.numColumns = numColumns;
            this.numLeftColumns = Math.Max(this.numColumns / 4, 1);
            int actualNumRows = Math.Min(numRows, 2);
            int actualNumColumns = Math.Min(numColumns, 2);
            this.Initialize(new BoundProperty_List(actualNumRows), new BoundProperty_List(actualNumColumns), bonusScore);
            this.gridLayouts = new GridLayout[actualNumColumns, actualNumRows];
            int numBottomRows = numRows - numTopRows;
            int numRightColumns = numColumns - numLeftColumns;


            if (numTopRows > 1 || numLeftColumns > 1)
                base.PutLayout(this.gridLayouts[0, 0] = GridLayout.New(new BoundProperty_List(numTopRows), new BoundProperty_List(numLeftColumns), LayoutScore.Zero), 0, 0);
            if (numRightColumns > 1)
                base.PutLayout(this.gridLayouts[1, 0] = GridLayout.New(new BoundProperty_List(numTopRows), new BoundProperty_List(numColumns - numLeftColumns), LayoutScore.Zero), 1, 0);
            if (numBottomRows > 1)
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

            GridLayout subGrid = this.gridLayouts[primaryXIndex, primaryYIndex];
            if (subGrid != null)
                subGrid.PutLayout(layout, xIndex, yIndex);
            else
                base.PutLayout(layout, primaryXIndex, primaryYIndex);

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

            GridLayout subGrid = this.gridLayouts[primaryXIndex, primaryYIndex];
            if (subGrid != null)
                return subGrid.GetLayout(xIndex, yIndex);
            else
                return base.GetLayout(primaryXIndex, primaryYIndex);
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

        private GridLayout[,] gridLayouts;
        private int numRows;
        private int numColumns;
        private int numTopRows;
        private int numLeftColumns;
    }

    class LayoutAndSize
    {
        public LayoutAndSize(LayoutChoice_Set layout, Size size)
        {
            this.Layout = layout;
            this.Size = size;
        }
        public LayoutChoice_Set Layout;
        public Size Size;
    }
}
