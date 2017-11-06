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
            this.orderedResponses = new List<LayoutQuery_And_Response>();
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
                    LayoutQuery debugQuery = query.Clone();
                    debugQuery.Debug = true;
                    SpecificLayout correct_subLayout = this.Query_SubLayout(debugQuery);
                    if (broadened.Query.PreferredLayout(broadened.Response, correct_subLayout) != broadened.Response)
                    {
                        ErrorReporter.ReportParadox("Error; incorrect result for broadened query");
                        this.Find_LargerQuery(query);
                    }
                }
                return broadened.Response;
                /*
                // if a less-restrictive query returned acceptable results, we can simply use those
                if (query.Accepts(broadened.Response))
                  return this.prepareLayoutForQuery(broadened.Response.Clone(), query);
                // if a less-restrictive query returned no results, then there will be no solution to our query either
                if (broadened.Response == null)
                {
                    if (broadened.Query.MaxWidth >= query.MaxWidth && broadened.Query.MaxHeight >= query.MaxHeight && broadened.Query.MinScore.CompareTo(query.MinScore) <= 0)
                        return null; // TODO should we cache the fact that we're returning null for this query (to avoid having to rebroaden in the next search)?
                }
                */
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

            if (shrunken != null)
            {
                // If the existing example is already at the extreme, then use it
                if (query.MaximizesScore())
                {
                    if (shrunken.Response.Width >= query.MaxWidth && shrunken.Response.Height >= query.MaxHeight)
                        return shrunken.Response;
                }
                if (query.MinimizesWidth())
                {
                    if (shrunken.Response.Width <= 0)
                        return shrunken.Response;
                }
                if (query.MinimizesHeight())
                {
                    if (shrunken.Response.Height <= 0)
                        return shrunken.Response;
                }
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
                /*if (this.queryResults.Count > 80)
                {
                    System.Diagnostics.Debug.WriteLine("many (" + this.queryResults.Count + ") queries to cache " + this);
                    return this.GetBestLayout_Quickly(query);
                }*/
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
            this.orderedResponses.Add(new LayoutQuery_And_Response(query, result));
            
            if (query.Debug)
            {
                if (result == null)
                    return null;
                return this.prepareLayoutForQuery(result.Clone(), query);
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
            //query.Debug = true;
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
            // check each previous query and response to see if we can use one of them

            // make a bunch of query generators, with the type that we care about at the beginning of the list
            foreach (LayoutQuery_And_Response queryAndResponse in this.orderedResponses)
            {
                LayoutQuery alternateQuery = queryAndResponse.Query;
                // check whether the query we started with will accept the result of this query
                if (query.Accepts(queryAndResponse.Response))
                    return queryAndResponse;
            }
            // didn't find any acceptable queries to relax the query to
            return null;
        }

        // Attempts to find a strictly larger query so we can get an upper bound on the required size (and if possible, we want a query whose response is accepted by the original)
        private LayoutQuery_And_Response Find_LargerQuery(LayoutQuery query)
        {
            LayoutQuery_And_Response bestResult = null;

            // check each previous query and response to see if we can use one of them
            // TODO do something faster and more complicated that doesn't entail checking every query, using something like an R-tree
            foreach (LayoutQuery_And_Response queryAndResponse in this.orderedResponses)
            {
                LayoutQuery otherQuery = queryAndResponse.Query;
                SpecificLayout otherResult = queryAndResponse.Response;

                // check whether the answer to the previous query must also be the answer to the current query
                bool encompassedInputs = true;
                bool encompassedOutputs = true;
                bool inputAccepted = true;
                // If we're looking for the min width or min height, then tightening the score means we can't necessarily use the other result as the best result
                if (query.MaximizesScore())
                {
                    if (otherQuery.MaxWidth < query.MaxWidth)
                        encompassedInputs = false;
                    if (otherQuery.MaxHeight < query.MaxHeight)
                        encompassedInputs = false;
                    if (otherQuery.MinScore.CompareTo(query.MinScore) > 0)
                        encompassedOutputs = false;

                    if (otherResult == null || otherResult.Width > query.MaxWidth)
                        inputAccepted = false;
                    if (otherResult == null || otherResult.Height > query.MaxHeight)
                        inputAccepted = false;
                }
                if (query.MinimizesWidth())
                {
                    if (otherQuery.MaxWidth < query.MaxWidth)
                        encompassedOutputs = false;
                    if (otherQuery.MaxHeight < query.MaxHeight)
                        encompassedInputs = false;
                    if (otherQuery.MinScore.CompareTo(query.MinScore) > 0)
                        encompassedInputs = false;

                    if (otherResult == null || otherResult.Score.CompareTo(query.MinScore) < 0)
                        inputAccepted = false;
                    if (otherResult == null || otherResult.Height > query.MaxHeight)
                        inputAccepted = false;
                }
                if (query.MinimizesHeight())
                {
                    if (otherQuery.MaxWidth < query.MaxWidth)
                        encompassedInputs = false;
                    if (otherQuery.MaxHeight < query.MaxHeight)
                        encompassedOutputs = false;
                    if (otherQuery.MinScore.CompareTo(query.MinScore) > 0)
                        encompassedInputs = false;

                    if (otherResult == null || otherResult.Width > query.MaxWidth)
                        inputAccepted = false;
                    if (otherResult == null || otherResult.Score.CompareTo(query.MinScore) < 0)
                        inputAccepted = false;
                }
                if (encompassedInputs)
                {
                    if (query.SameType(otherQuery) && query.Accepts(otherResult))
                    {
                        // The previous query had looser inputs but its result was still in our range, so we know its result will be right for us too
                        return queryAndResponse;
                    }
                    if (query.SameType(otherQuery) && inputAccepted && encompassedOutputs)
                    {
                        if (!query.Accepts(otherResult))
                        {
                            // The previous query had looser inputs, and demonstrated that the best output is worse than our threshold
                            // So, our current query has no solution
                            return new LayoutQuery_And_Response(otherQuery, null);
                        }
                    }
                    if (encompassedOutputs && otherResult == null)
                    {
                        // The previous query had looser inputs and looser outputs but no solution, so the current query will also have no solution
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
        //Dictionary<LayoutQuery, LayoutQuery_And_Response> sampleQueries; // for a given layoutQuery, returns a query of equal or larger dimensions, and its result
        private LayoutChoice_Set layoutToManage;
        List<LayoutQuery_And_Response> orderedResponses;
        static int numComputations = 0;
        static int numQueries = 0;
    }
}
