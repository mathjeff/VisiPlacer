using System;
using System.Collections.Generic;
using Xamarin.Forms;

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
                ErrorReporter.ReportParadox("Warning: creating a LayoutCache with nothing in it");
            this.Initialize();
            if (layoutToManage is LayoutCache)
                ErrorReporter.ReportParadox("Warning: creating a LayoutCache that simply manages another LayoutCache");
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
                    ErrorReporter.ReportParadox("Warning: creating a LayoutCache with nothing in it");

                this.AnnounceChange(true);
            }
        }
        public void Check_RoundingError(double value)
        {
            double shifted = value * 300;
            if (shifted - Math.Floor(shifted) != 0)
            {
                ErrorReporter.ReportParadox("rounding error");
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
                    {
                        ErrorReporter.ReportParadox("Error; incorrect result for broadened query");
                        this.Find_LargerQuery(query);
                    }
                }
                // if a less-restrictive query returned acceptable results, we can simply use those
                if (query.Accepts(broadened.Response))
                  return this.prepareLayoutForQuery(broadened.Response.Clone(), query);
                // if a less-restrictive query returned no results, then there will be no solution to our query either
                if (broadened.Response == null)
                {
                    if (broadened.Query.MaxWidth >= query.MaxWidth && broadened.Query.MaxHeight >= query.MaxHeight && broadened.Query.MinScore.CompareTo(query.MinScore) <= 0)
                        return null; // TODO should we cache the fact that we're returning null for this query (to avoid having to rebroaden in the next search)?
                }
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
                        ErrorReporter.ReportParadox("Error; incorrect result for shrunken query");
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
            }
            else
            {
                if (shrunken != null && query.Accepts(shrunken.Response))
                {
                    ErrorReporter.ReportParadox("Error: cache contains an acceptable value for the current query, but the layout claims there are none");
                    result = shrunken.Response;
                }
            }

            // record that this is the exact answer to this query
            if (this.queryResults.ContainsKey(query))
                ErrorReporter.ReportParadox("Error, layoutCache repeated a query that was already present ");
            this.queryResults[query.Clone()] = result;
            
            if (query.Debug)
            {
                if (result == null)
                    return null;
                return this.prepareLayoutForQuery(result.Clone(), query);
            }


            QueryBlurrer generator = new QueryBlurrer(query);
            // record that this layout is an option for larger queries, too
            while (true)
            {
                LayoutQuery largerQuery = generator.Next();
                if (largerQuery == null)
                    break;

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
                bool oldQueryIsBetter = false;
                bool oldResultsAreBetter = false;

                if (previousQuery != null && (previousQuery.MaxWidth >= query.MaxWidth && previousQuery.MaxHeight >= query.MaxHeight && previousQuery.MinScore.CompareTo(query.MinScore) <= 0))
                {
                    // the previous query was bigger so leave it
                    oldQueryIsBetter = true;
                    oldResultsAreBetter = true;

                    // if the new response is better, it can stay
                    if (result != null && previousResult != null)
                    {
                        if (result.Width <= previousResult.Width && result.Height <= previousResult.Height && result.Score.CompareTo(previousResult.Score) >= 0)
                        {
                            oldResultsAreBetter = false;
                        }
                    }

                }
                else
                {
                    // if the previous result was better, then leave it
                    if (result != null && previousResult != null)
                    {
                        if (previousResult.Width <= result.Width && previousResult.Height <= result.Height && previousResult.Score.CompareTo(result.Score) >= 0)
                            oldResultsAreBetter = true;
                    }
                }

                // if the previous query was strictly larger, then leave it as the query to use
                if (oldQueryIsBetter)
                    newData.Query = previousQuery;
                else
                    newData.Query = query.Clone();

                if (oldResultsAreBetter)
                {
                    newData.Response = previousResult;
                }
                else
                {
                    newData.Response = result;
                }

                double queryCount2 = numQueries;
                if (newData.Query == previousQuery && newData.Response == previousResult)
                    break;

                if (query.Debug || newData.Query.Debug)
                {
                    LayoutQuery debugQuery = newData.Query.Clone();
                    debugQuery.Debug = true;
                    SpecificLayout correct_subLayout = this.Query_SubLayout(debugQuery);
                    if (newData.Query.PreferredLayout(newData.Response, correct_subLayout) != newData.Response)
                        ErrorReporter.ReportParadox("Error; LayoutCache attempting to insert an answer that is not good enough into sampleQueries");
                    if (newData.Query.PreferredLayout(correct_subLayout, newData.Response) != correct_subLayout)
                        ErrorReporter.ReportParadox("Error; LayoutCache attempting to insert an answer that is too good into sampleQueries");

                    if (newData.Response != null && !newData.Query.Accepts(newData.Response))
                        ErrorReporter.ReportParadox("Error; inserting an incorrect answer into sampleQueries");
                }
                this.sampleQueries[largerQuery] = newData;

            }
            if (result != null)
                result = result.Clone();
            return this.prepareLayoutForQuery(result, query);
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
                    ErrorReporter.ReportParadox("Error: LayoutCache was given an incorrect response by its sublayout");
                }
                bool correct = true;
                if (query.PreferredLayout(correctResult, fastResult) != correctResult)
                {
                    ErrorReporter.ReportParadox("Error: layout cache returned incorrect (superior) result");
                    correct = false;
                }
                if (query.PreferredLayout(fastResult, correctResult) != fastResult)
                {
                    ErrorReporter.ReportParadox("Error: layout cache returned incorrect (inferior) result");
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

        // Attempts to find a query having results that are accepted by this query
        private LayoutQuery_And_Response FindExample(LayoutQuery query)
        {
            // make a bunch of query generators, with the type that we care about at the beginning of the list
            LinkedList<QueryBlurrer> generators = new LinkedList<QueryBlurrer>();
            generators.AddLast(new QueryBlurrer(query));
            // add the other types to the end of the list
            if (!query.MaximizesScore())
                generators.AddLast(new QueryBlurrer(new MaxScore_LayoutQuery().CopyFrom(query)));
            if (!query.MinimizesHeight())
                generators.AddLast(new QueryBlurrer(new MinHeight_LayoutQuery().CopyFrom(query)));
            if (!query.MinimizesWidth())
                generators.AddLast(new QueryBlurrer(new MinWidth_LayoutQuery().CopyFrom(query)));


            bool busy = true;
            LayoutQuery_And_Response queryAndResponse;
            while (busy)
            {
                busy = false;
                foreach (QueryBlurrer generator in generators)
                {
                    LayoutQuery query2 = generator.Next();
                    if (query2 == null)
                        continue;
                    busy = true;

                    bool queryIsPresent = this.sampleQueries.TryGetValue(query2, out queryAndResponse);
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
            QueryBlurrer generator = new QueryBlurrer(query);
            LayoutQuery_And_Response bestResult = null;
            while (true)
            {
                LayoutQuery nearbyQuery = generator.Next();
                if (nearbyQuery == null)
                    break;

                LayoutQuery_And_Response queryAndResponse;
                bool queryIsPresent;

                queryIsPresent = this.sampleQueries.TryGetValue(nearbyQuery, out queryAndResponse);
                if (queryIsPresent)
                {
                    LayoutQuery otherQuery = queryAndResponse.Query;
                    SpecificLayout otherResult = queryAndResponse.Response;
                    // check whether the query we found will encompass strictly more things than the query we started with
                    if (otherQuery.MaxWidth >= query.MaxWidth && otherQuery.MaxHeight >= query.MaxHeight && otherQuery.MinScore.CompareTo(query.MinScore) <= 0)
                    {
                        // if there are no layouts that satisfy the relaxed query, then there's no reason to keep relaxing the query more
                        if (otherResult == null)
                            return queryAndResponse;
                    }

                    // check whether the answer to the previous query must also be the answer to the current query
                    bool encompassedInputs = true;
                    // If we're looking for the min width or min height, then tightening the score means we can't necessarily use the other result as the best result
                    if (!query.MaximizesScore() && otherQuery.MinScore.CompareTo(query.MinScore) > 0)
                        encompassedInputs = false;
                    if (!query.MinimizesWidth() && otherQuery.MaxWidth < query.MaxWidth)
                        encompassedInputs = false;
                    if (!query.MinimizesHeight() && otherQuery.MaxHeight < query.MaxHeight)
                        encompassedInputs = false;
                    if (encompassedInputs && query.Accepts(otherResult))
                    {
                        // This more-relaxed query has an answer that satisfies our tighter query, so we can use it
                        return queryAndResponse;
                    }
                }

            }
            if (query.Debug)
            {
                if (bestResult != null && bestResult.Response != null && !bestResult.Query.Accepts(bestResult.Response))
                {
                    ErrorReporter.ReportParadox("Query does not accept its recorded answer");
                }
            }
            // return the most useful query+response that we found
            return bestResult;
        }

    

        public override void On_ContentsChanged(bool mustRedraw)
        {
            this.Initialize();
        }

        
#region Functions for for IEqualityComparer<LayoutQuery>
        public bool Equals(LayoutQuery query1, LayoutQuery query2)
        {
            return query1.Equals(query2);
        }

        public int GetHashCode(LayoutQuery query)
        {
            int value = 0;
            if (!double.IsInfinity(query.MaxWidth))
                value += (int)(query.MaxWidth * 100);
            value *= 500;
            if (!double.IsInfinity(query.MaxHeight))
                value += (int)(query.MaxHeight * 100);
            value *= 500;
            if (query.MinimizesHeight())
                value += 1;
            if (query.MinimizesWidth())
                value += 2;
            value += query.MinScore.GetHashCode();
            return value;
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

    // A QueryBlurrer lazily generates a List<LayoutQuery> in a manner such that nearby source LayoutQuery objects will eventually converge (for the purpose of doing a Dictionary lookup)
    public class QueryBlurrer
    {
        public QueryBlurrer(LayoutQuery original)
        {
            this.index = 0;

            double maxSize = 1;
            if (original.MaxWidth > maxSize && !double.IsInfinity(original.MaxWidth))
                maxSize = original.MaxWidth;
            if (original.MaxHeight > maxSize && !double.IsInfinity(original.MaxHeight))
                maxSize = original.MaxHeight;
            this.maxIndex = (int)Math.Log(maxSize, 2) + 2;

            this.original = original;
        }

        public LayoutQuery Next()
        {
            if (this.index >= maxIndex)
                return null;

            LayoutQuery result = this.original.Clone();
            double blockSize = Math.Pow(2, this.index);
            this.snap(result, blockSize);
#if false
            if (blockSize > 26)
                result.MinScore = LayoutScore.Minimum;
#else
            int numComponents = this.original.MinScore.NumComponents;
            int numComponentsToRemove = (int)Math.Round((double)numComponents * (double)this.index / (double)this.maxIndex);
            result.MinScore = result.MinScore.ComponentRange(numComponentsToRemove, numComponents);
#endif

            this.index++;

            // check for duplicates and skip them
            if (this.previousResult != null)
            {
                if (result.Equals(this.previousResult))
                    return this.Next();
            }
            this.previousResult = result;
            return result;
        }

        private double GetBlockRatio()
        {
            return 2;
        }

        private void snap(LayoutQuery layoutQuery, double blockSize)
        {
            layoutQuery.MaxWidth = this.round(layoutQuery.MaxWidth, blockSize);
            layoutQuery.MaxHeight = this.round(layoutQuery.MaxHeight, blockSize);
        }
        private double round(double input, double blockSize)
        {
            int offset = 1;
            return Math.Ceiling((input + offset) / blockSize) * blockSize - offset;
        }

        private int index;
        private int maxIndex;
        private LayoutQuery original;
        private LayoutQuery previousResult;
    }
}
