using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

// a LayoutCache acts like the layout provided to it, but faster by saving results
namespace VisiPlacement
{
    public class LayoutCache : LayoutChoice_Set, IEqualityComparer<LayoutQuery>, IEqualityComparer<Size>
    {
        public LayoutCache()
        {
            this.Initialize();
        }
        public LayoutCache(LayoutChoice_Set layoutToManage)
        {
            if (layoutToManage == null)
                System.Diagnostics.Debug.WriteLine("Warning: creating a LayoutCache with nothing in it");
            this.Initialize();
            if (layoutToManage is LayoutCache)
                System.Diagnostics.Debug.WriteLine("Warning: creating a LayoutCache that simply manages another LayoutCache");
            this.LayoutToManage = layoutToManage;
        }
        private void Initialize()
        {
            this.queryResults = new Dictionary<LayoutQuery, SpecificLayout>(this);
            this.sampleQueries = new Dictionary<LayoutQuery, LayoutQuery_And_Response>(this);
        }
        public LayoutChoice_Set LayoutToManage
        {
            set
            {
                if (this.layoutToManage != null)
                    this.layoutToManage.RemoveParent(this);
                this.layoutToManage = value;
                if (this.layoutToManage != null)
                    this.layoutToManage.AddParent(this);
                else
                    System.Diagnostics.Debug.WriteLine("Warning: creating a LayoutCache with nothing in it");

                this.AnnounceChange(true);
            }
        }
        public void Check_RoundingError(double value)
        {
            double shifted = value * 300;
            if (shifted - Math.Floor(shifted) != 0)
            {
                System.Diagnostics.Debug.WriteLine("rounding error");
            }
        }
        public SpecificLayout GetBestLayout_Quickly(LayoutQuery query)
        {
            // check whether we've previously saved the result
            SpecificLayout result = null;
            if (this.queryResults.TryGetValue(query, out result))
            {
                if (result != null)
                    return this.prepareLayoutForQuery(result.Clone(), query);
                return null;
            }
            // the result wasn't saved, so we need to delegate the layout query
            // However, we might first be able to make the query more strict

            LayoutQuery_And_Response broadened = this.Find_LargerQuery(query);
            if (broadened != null)
            {
                if (query.Debug)
                {
                    LayoutQuery debugQuery = broadened.Query.Clone();
                    debugQuery.Debug = true;
                    SpecificLayout correct_subLayout = this.Query_SubLayout(debugQuery);
                    if (broadened.Query.PreferredLayout(broadened.Response, correct_subLayout) != broadened.Response)
                        System.Diagnostics.Debug.WriteLine("Error; incorrect result for broadened query");
                }
                // if a less-restrictive query returned acceptable results, we can simply use those
                if (query.Accepts(broadened.Response))
                  return this.prepareLayoutForQuery(broadened.Response.Clone(), query);
            }
            LayoutQuery_And_Response shrunken = this.FindExample(query);
            if (query.Debug)
            {
                if (shrunken != null)
                {
                    LayoutQuery debugQuery = shrunken.Query.Clone();
                    debugQuery.Debug = true;
                    SpecificLayout correct_subLayout = this.Query_SubLayout(debugQuery);
                    if (shrunken.Query.PreferredLayout(shrunken.Response, correct_subLayout) != shrunken.Response)
                        System.Diagnostics.Debug.WriteLine("Error; incorrect result for shrunken query");
                }
            }

            if (broadened != null && shrunken != null)
            {
                // If the stricter query and broader query return equally-good results, then the results from the stricter query are optimal
                bool equal = false;
                if (query.MaximizesScore())
                {
                    if (shrunken.Response.Score.CompareTo(broadened.Response.Score) >= 0)
                        equal = true;
                }
                else
                {
                    if (query.MinimizesWidth())
                    {
                        if (shrunken.Response.Width <= broadened.Response.Width)
                            equal = true;
                    }
                    else
                    {
                        if (shrunken.Response.Height <= broadened.Response.Height)
                            equal = true;
                    }
                }
                if (equal && query.Accepts(shrunken.Response))
                    return this.prepareLayoutForQuery(shrunken.Response.Clone(), query);

            }
            // if we couldn't immediately return a result using the cache, we can still put a bound on the results we might get
            // They have to be at least as good as the sample we found
            if (shrunken != null && query.Accepts(shrunken.Response))
            {

                // First, see if we can improve past what we currently have
                LayoutQuery strictlyImprovedQuery = query.Clone();
                strictlyImprovedQuery.OptimizePastExample(shrunken.Response);
                result = this.Query_SubLayout(strictlyImprovedQuery);
                if (result == null)
                    result = shrunken.Response;      // couldn't improve past the previously found best layout
            }
            else
            {
                // Ask for the best layout satisfying this 
                result = this.Query_SubLayout(query.Clone());
            }

            if (result != null)
            {
                result = result.Clone();
                /*if (this.maxObservedWidth < result.Width)
                    this.maxObservedWidth = result.Width;
                if (this.maxObservedHeight < result.Height)
                    this.maxObservedHeight = result.Height;
                */
            }
            else
            {
                if (shrunken != null && query.Accepts(shrunken.Response))
                {
                    System.Diagnostics.Debug.WriteLine("Error: cache contains an acceptable value for the current query, but the layout claims there are none");
                    result = shrunken.Response;
                }
            }

            // record that this is the exact answer to this query
            if (this.queryResults.ContainsKey(query))
                System.Diagnostics.Debug.WriteLine("Error, layoutCache repeated a query that was already present ");
            this.queryResults[query.Clone()] = result;
            
            if (query.Debug)
            {
                if (result == null)
                    return null;
                return this.prepareLayoutForQuery(result.Clone(), query);
            }


            if (result != null)
            {
                // record that this layout is an option for larger queries, too
                //Size layoutSize = new Size(result.Width, result.Height);
                double blockWidth = 1;
                double maxBlockSize = Math.Max(query.MaxWidth, query.MaxHeight) * this.GetBlockRatio();
                while (blockWidth < maxBlockSize)
                {
                    LayoutQuery largerQuery = query.Clone();
                    largerQuery.MaxWidth = Math.Ceiling(query.MaxWidth / blockWidth) * blockWidth;
                    largerQuery.MaxHeight = Math.Ceiling(query.MaxHeight / blockWidth) * blockWidth;
                    LayoutQuery_And_Response previousData = null;
                    this.sampleQueries.TryGetValue(largerQuery, out previousData);
                    SpecificLayout previousResult = null;
                    LayoutQuery previousQuery = null;
                    if (previousData != null)
                    {
                        previousResult = previousData.Response;
                        previousQuery = previousData.Query;
                    }
                    LayoutQuery_And_Response newData = new LayoutQuery_And_Response();
                    // if the previous query was strictly larger, then leave it as the query to use
                    if (previousQuery != null && (previousQuery.MaxWidth >= query.MaxWidth && previousQuery.MaxHeight >= query.MaxHeight && previousQuery.MinScore.CompareTo(query.MinScore) <= 0))
                        newData.Query = previousQuery;
                    else
                        newData.Query = query.Clone();
                    // if the previous result was strictly better, then leave it as the result to use
                    double queryCount1 = numQueries;
                    if (previousResult != null && (previousResult.Width <= result.Width && previousResult.Height <= result.Height && previousResult.Score.CompareTo(result.Score) >= 0))
                        newData.Response = previousResult;
                    else
                        newData.Response = newData.Query.PreferredLayout(result.Clone(), previousResult);
                    double queryCount2 = numQueries;
                    if (newData.Query == previousQuery && newData.Response == previousResult)
                        break;

                    if (query.Debug)
                    {
                        LayoutQuery debugQuery = newData.Query.Clone();
                        debugQuery.Debug = true;
                        SpecificLayout correct_subLayout = this.Query_SubLayout(debugQuery);
                        if (newData.Query.PreferredLayout(newData.Response, correct_subLayout) != newData.Response)
                            System.Diagnostics.Debug.WriteLine("Error; LayoutCache attempting to insert an answer that is not good enough into sampleQueries");
                        if (newData.Query.PreferredLayout(correct_subLayout, newData.Response) != correct_subLayout)
                            System.Diagnostics.Debug.WriteLine("Error; LayoutCache attempting to insert an answer that is too good into sampleQueries");

                    }

                    this.sampleQueries[largerQuery] = newData;

                    // move to the next size (skipping any duplicates along the way)
                    if (largerQuery.MaxWidth == 0 || largerQuery.MaxHeight == 0)
                        break;
                    while (largerQuery.MaxWidth % blockWidth == 0 && largerQuery.MaxWidth % blockWidth == 0)
                    {
                        blockWidth *= this.GetBlockRatio();
                    }
                }
                return this.prepareLayoutForQuery(result.Clone(), query);
            }
            return null;
        }

        private SpecificLayout Query_SubLayout(LayoutQuery query)
        {
            numComputations++;
            return this.layoutToManage.GetBestLayout(query);
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            numQueries++;
            if (numQueries % 10000 == 0)
                System.Diagnostics.Debug.WriteLine("Overall LayoutCache miss rate: " + numComputations + " of " + numQueries + " = " + ((double)numComputations / (double)numQueries));
            /*
            LayoutQuery primingQuery = query.Clone();
            primingQuery.MinScore = LayoutScore.Minimum;
            this.GetBestLayout_Quickly(primingQuery);
            */
            SpecificLayout fastResult = this.GetBestLayout_Quickly(query.Clone());
            if (query.Debug)
            {
                SpecificLayout correctResult = this.Query_SubLayout(query.Clone());
                if (correctResult != null && !query.Accepts(correctResult))
                {
                    System.Diagnostics.Debug.WriteLine("Error: LayoutCache was given an incorrect response by its sublayout");
                }
                bool correct = true;
                if (query.PreferredLayout(correctResult, fastResult) != correctResult)
                {
                    System.Diagnostics.Debug.WriteLine("Error: layout cache returned incorrect (superior) result");
                    correct = false;
                }
                if (query.PreferredLayout(fastResult, correctResult) != fastResult)
                {
                    System.Diagnostics.Debug.WriteLine("Error: layout cache returned incorrect (inferior) result");
                    correct = false;
                }
                if (!correct)
                {
                    LayoutQuery subQuery = new MaxScore_LayoutQuery();
                    subQuery.Debug = true;
                    if (fastResult != null)
                    {
                        /*
                        subQuery.MaxWidth = fastResult.Dimensions.Width;
                        subQuery.MaxHeight = fastResult.Dimensions.Height;
                        this.Query_SubLayout(subQuery);
                        */
                        this.GetBestLayout_Quickly(query.Clone());
                    }
                }
                return this.prepareLayoutForQuery(correctResult, query);
            }
            return fastResult;
        }
        /*public override SpecificLayout GetBestLayout_Debugged(LayoutQuery query, SpecificLayout proposedSolution)
        {
            // check that the solution satisfies the query
            base.GetBestLayout_Debugged(query, proposedSolution);

            SpecificLayout fastResult = this.GetBestLayout(query.Clone());
            SpecificLayout correctResult = this.layoutToManage.GetBestLayout_Debugged(query.Clone(), proposedSolution);
            if (query.PreferredLayout(correctResult, fastResult) != correctResult)
                System.Diagnostics.Debug.WriteLine("Error: layout cache returned incorrect result");
            if (query.PreferredLayout(fastResult, correctResult) != fastResult)
                System.Diagnostics.Debug.WriteLine("Error: layout cache returned incorrect result");

            return this.prepareLayout(correctResult);
        }*/
        /*
        private SpecificLayout Get_MinWidth_Layout(MinWidth_LayoutQuery query)
        {
            SpecificLayout result = null;
            if (this.queryResults.TryGetValue(query, out result))
            {
                return result;
            } 
            LayoutQuery snapped = this.SnapToGridlines(query);
            result = this.GetBestLayout_Internal(snapped);
            if (query.Accepts(result))
                return result;
            return this.GetBestLayout_Internal(query);
        }
        private SpecificLayout Get_MinHeight_Layout(MinHeight_LayoutQuery query)
        {
            SpecificLayout result = null;
            if (this.queryResults.TryGetValue(query, out result))
            {
                return result;
            } 
            LayoutQuery snapped = this.SnapToGridlines(query);
            result = this.GetBestLayout_Internal(snapped);
            if (query.Accepts(result))
                return result;
            return this.GetBestLayout_Internal(query);
        }
        private SpecificLayout Get_MaxScore_Layout(MaxScore_LayoutQuery query)
        {
            //return this.GetBestLayout_Internal(query);
            SpecificLayout exactResult = null;
            if (this.queryResults.TryGetValue(query, out exactResult))
            {
                return exactResult;
            }
            SpecificLayout guessedResult = null;
            LayoutQuery snapped = this.SnapToGridlines(query);
            guessedResult = this.GetBestLayout_Internal(snapped);
            if (query.Accepts(guessedResult))
                return guessedResult;
            exactResult = this.GetBestLayout_Internal(query);
            if (guessedResult != null && exactResult != null)
            {
                if (exactResult.Dimensions.Score.CompareTo(guessedResult.Dimensions.Score) > 0)
                {
                    if (!query.Accepts(guessedResult))
                        System.Diagnostics.Debug.WriteLine("error evaluating augmented query");
                    if (!query.Accepts(exactResult))
                        System.Diagnostics.Debug.WriteLine("error evaluating basic query");
                    System.Diagnostics.Debug.WriteLine("error");
                }

                if (exactResult.Dimensions.Score.CompareTo(guessedResult.Dimensions.Score) >= 0)
                {
                    if (!snapped.Accepts(exactResult))
                    {
                        System.Diagnostics.Debug.WriteLine("error");
                    }
                    //this.savedResults[snapped] = snapped.PreferredLayout(exactResult, guessedResult).Clone();
                    // if the layout we found is just as good as the cached value, update the cache with the smaller layout
                    // This makes the cache faster to use in the future
                    this.queryResults[snapped] = exactResult.Clone();
                    return exactResult;
                }
            }
            return exactResult;
        }
        */

        // Attemps to find an already known SpecificLayout that satisfied this query
        /*private KeyValuePair<LayoutQuery, SpecificLayout> FindExample(LayoutQuery query)
        {
            if (query.MaxWidth <= 0 || query.MaxHeight < 0)
                return null;
            LayoutQuery constricted = query.Clone();
            double blockSize;
            double maxBlockSize = 1;
            // shrink the query a little bit if its bounds are beyond the data we have
            double width = Math.Min(this.maxObservedWidth, query.MaxWidth);
            double height = Math.Min(this.maxObservedHeight, query.MaxHeight);

            if (maxBlockSize < width)
                maxBlockSize = width;
            if (maxBlockSize < height)
                maxBlockSize = height;

            // search through the spots we would place them in
            //for (blockSize = maxBlockSize * this.GetBlockRatio(); blockSize >= 1; blockSize /= this.GetBlockRatio())
            for (blockSize = 1; blockSize < maxBlockSize * this.GetBlockRatio(); blockSize *= this.GetBlockRatio())
            {
                Size otherSize;
                SpecificLayout otherLayout = null;
                otherSize = new Size(Math.Ceiling(width / blockSize) * blockSize, Math.Ceiling(height / blockSize) * blockSize);
                this.sampleLayouts.TryGetValue(otherSize, out otherLayout);
                if (query.Accepts(otherLayout))
                {
                    // we found a layout that satisfies the requirements
                    return otherLayout;
                }
                otherSize = new Size(Math.Floor(width / blockSize) * blockSize, Math.Floor(height / blockSize) * blockSize);
                this.sampleLayouts.TryGetValue(otherSize, out otherLayout);
                if (query.Accepts(otherLayout))
                {
                    // we found a layout that satisfies the requirements
                    return otherLayout;
                }
            }
            // no examples were found
            return null;
        }
        */



        // Attempts to find a query having results that are accepted by this query
        private LayoutQuery_And_Response FindExample(LayoutQuery query)
        {
            double maxBoxWidth;
            LinkedList<LayoutQuery> queryTypes = new LinkedList<LayoutQuery>();
            queryTypes.AddLast(query.Clone());
            if (!query.MaximizesScore())
                queryTypes.AddLast(new MaxScore_LayoutQuery());
            if (!query.MinimizesHeight())
                queryTypes.AddLast(new MinHeight_LayoutQuery());
            if (!query.MinimizesWidth())
                queryTypes.AddLast(new MinWidth_LayoutQuery());

            //LayoutQuery query2 = query.Clone();
            // find the next power of this.GetBlockRatio() that is at least as big as the width and at least as big as the height

            double maxWidthAllowed = 1;
            if (!double.IsInfinity(query.MaxWidth) && maxWidthAllowed < query.MaxWidth)
                maxWidthAllowed = query.MaxWidth;
            if (!double.IsInfinity(query.MaxHeight) && maxWidthAllowed < query.MaxHeight)
                maxWidthAllowed = query.MaxHeight;

            for (maxBoxWidth = 1; maxBoxWidth < maxWidthAllowed; maxBoxWidth *= this.GetBlockRatio())
            {
            }
            double currentBoxWidth;
            for (currentBoxWidth = 1; currentBoxWidth <= maxBoxWidth; currentBoxWidth *= this.GetBlockRatio())
            {
                LayoutQuery_And_Response queryAndResponse;
                bool queryIsPresent;

                foreach (LayoutQuery query2 in queryTypes)
                {
                    // snap the width and height to a box of the given size
                    query2.MaxWidth = Math.Ceiling(query.MaxWidth / currentBoxWidth) * currentBoxWidth;
                    query2.MaxHeight = Math.Ceiling(query.MaxHeight / currentBoxWidth) * currentBoxWidth;

                    queryIsPresent = this.sampleQueries.TryGetValue(query2, out queryAndResponse);
                    if (queryIsPresent)
                    {
                        // if there are no layouts that satisfy the relaxed query, then there's no reason to keep relaxing the query more
                        if (queryAndResponse.Response == null)
                            return null;
                        LayoutQuery alternateQuery = queryAndResponse.Query;
                        // check whether the query we started with will accept the result of this query
                        if (query.Accepts(queryAndResponse.Response))
                            return queryAndResponse;
                    }
                }
            }
            // didn't find any acceptable queries to relax the query to
            return null;
        }

        // Attempts to find a strictly larger query so we can get an upper bound on the required size (and if possible, we want a query whose response is accepted by the original)
        private LayoutQuery_And_Response Find_LargerQuery(LayoutQuery query)
        {
            LayoutQuery query2 = query.Clone();
            LayoutQuery_And_Response bestResult = null;
            // find the next power of this.GetBlockRatio() that is at least as big as the width and at least as big as the height

            double maxInput = 1;
            double maxBoxWidth = 1;
            if (!double.IsInfinity(query.MaxWidth) && maxInput < query.MaxWidth)
                maxInput = query.MaxWidth;
            double maxBoxHeight = 1;
            if (!double.IsInfinity(query.MaxHeight) && maxInput < query.MaxHeight)
                maxInput = query.MaxHeight;
            maxBoxWidth = maxBoxHeight = maxInput * 4;

            double currentBoxWidth = 1;
            double currentBoxHeight = 1;
            // Search increasingly large boxes until either we find a relevant query or run out of time
            while (true)
            {
              //for (currentBoxWidth = 1; currentBoxWidth <= maxBoxWidth; currentBoxWidth *= this.GetBlockRatio())
              //{
                LayoutQuery_And_Response queryAndResponse;
                bool queryIsPresent;

                // snap the width and height to a box of the given size
                query2.MaxWidth = Math.Ceiling(query.MaxWidth / currentBoxWidth) * currentBoxWidth;
                query2.MaxHeight = Math.Ceiling(query.MaxHeight / currentBoxHeight) * currentBoxHeight;

                queryIsPresent = this.sampleQueries.TryGetValue(query2, out queryAndResponse);
                if (queryIsPresent)
                {
                    // if there are no layouts that satisfy the relaxed query, then there's no reason to keep relaxing the query more
                    if (queryAndResponse.Response == null)
                        return null;
                    LayoutQuery alternateQuery = queryAndResponse.Query;
                    // check whether the query we found will encompass strictly more things than the query we started with
                    if (alternateQuery.MaxWidth >= query.MaxWidth && alternateQuery.MaxHeight >= query.MaxHeight && alternateQuery.MinScore.CompareTo(query.MinScore) <= 0)
                    {
                        //return queryAndResponse;
                        if (query.Accepts(queryAndResponse.Response))
                        {
                            // We found a response that works, so just use it
                            return queryAndResponse;
                        }
                        // We found a query that's slightly bigger than the original and for which we know the response, so we'll save it in case it's the best we can find
                        if (bestResult == null || (query.PreferredLayout(queryAndResponse.Response, bestResult.Response) == queryAndResponse.Response))
                            bestResult = queryAndResponse;
                        
                        // Now put some bounds on the search if the response uses too much screen space
                        /*if (bestResult.Response.Width > query.MaxWidth)
                        {
                            // We've found a query for which the response is too wide, so stop increasing the width of our queries
                            //currentBoxWidth /= this.GetBlockRatio();
                            maxBoxWidth = currentBoxWidth;
                        }
                        if (bestResult.Response.Height > query.MaxHeight)
                        {
                            // We've found a query for which the response is too tall, so stop increasing the height of our queries
                            //currentBoxHeight /= this.GetBlockRatio();
                            maxBoxHeight = currentBoxHeight;
                        }*/
                    }
                }
                /*if (currentBoxHeight * maxBoxWidth > currentBoxWidth * maxBoxHeight)
                    currentBoxWidth *= this.GetBlockRatio();
                else
                    currentBoxHeight *= this.GetBlockRatio();*/
                if (currentBoxHeight < maxBoxHeight)
                    currentBoxHeight *= this.GetBlockRatio();
                if (currentBoxWidth < maxBoxWidth)
                    currentBoxWidth *= this.GetBlockRatio();
                if (currentBoxWidth >= maxBoxWidth && currentBoxHeight >= maxBoxHeight)
                {
                    // nothing left to search
                    break;
                }
            }
            // return the most useful query+response that we found
            return bestResult;
        }

        public override void On_ContentsChanged(bool mustRedraw)
        {
            this.Initialize();
            //this.maxObservedWidth = this.maxObservedHeight = 0;
        }

        private double GetBlockRatio()
        {
            return 2;
        }
        
#region Functions for for IEqualityComparer<LayoutQuery>
        public bool Equals(LayoutQuery query1, LayoutQuery query2)
        {
            if (query1.MaxHeight != query2.MaxHeight)
                return false;
            if (query1.MaxWidth != query2.MaxWidth)
                return false;
            if (query1.MinScore.CompareTo(query2.MinScore) != 0)
                return false;
            if (query1.MinimizesHeight() != query2.MinimizesHeight())
                return false;
            if (query1.MinimizesWidth() != query2.MinimizesWidth())
                return false;
            if (query1.MaximizesScore() != query2.MaximizesScore())
                return false;
            return true;
        }

        public int GetHashCode(LayoutQuery query)
        {
            double value = 1;
            if (!double.IsInfinity(query.MaxWidth))
                value *= Math.Floor(query.MaxWidth);
            value += 50000;
            if (!double.IsInfinity(query.MaxHeight))
                value *= Math.Floor(query.MaxHeight);
            value += 50000;
            double ratio = value / int.MaxValue;
            double fractionPart = ratio - Math.Floor(ratio);
            int remainder = (int)(fractionPart * int.MaxValue);
            return remainder;
        }
#endregion

#region Functions for for IEqualityComparer<Size>
        public bool Equals(Size size1, Size size2)
        {
            if (size1.Width != size2.Width)
                return false;
            if (size1.Height != size2.Height)
                return false;
            return true;
        }

        public int GetHashCode(Size size)
        {
            double value = 1;
            if (!double.IsInfinity(size.Width))
                value *= size.Width;
            value += 50000;
            if (!double.IsInfinity(size.Height))
                value *= size.Height;
            double ratio = value / int.MaxValue;
            double fractionPart = ratio - Math.Floor(ratio);
            int remainder = (int)(fractionPart * int.MaxValue);
            return remainder;
        }
#endregion

        Dictionary<LayoutQuery, SpecificLayout> queryResults;
        //Dictionary<Size, SpecificLayout> samplesByResult; // for a given size, returns a specificLayout of equal or smaller size
        Dictionary<LayoutQuery, LayoutQuery_And_Response> sampleQueries; // for a given layoutQuery, returns a query of equal or larger dimensions, and its result
        private LayoutChoice_Set layoutToManage;
        //public double maxObservedWidth;
        //public double maxObservedHeight;
        static int numComputations = 0;
        static int numQueries = 0;
    }
}
