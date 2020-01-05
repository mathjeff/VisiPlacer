using System;
using System.Collections.Generic;
using Xamarin.Forms;

// a LayoutCache acts like the layout provided to it, but faster by saving results
namespace VisiPlacement
{
    public class LayoutCache : LayoutChoice_Set, IEqualityComparer<LayoutQuery>, IEqualityComparer<Size>
    {
        public static LayoutCache For(LayoutChoice_Set layout)
        {
            LayoutCache result = layout as LayoutCache;
            if (result != null)
                return result;
            return new LayoutCache(layout);
        }
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
            this.true_queryResults = new Dictionary<LayoutQuery, SpecificLayout>(this);
            this.inferred_queryResults = new Dictionary<LayoutQuery, SpecificLayout>(this);
            this.orderedResponses = new List<LayoutQuery_And_Response>();
            this.sizeZeroQuery = null;
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
        private SpecificLayout GetBestLayout_Quickly(LayoutQuery query)
        {
            SpecificLayout result = null;
            // A layout of size 0 in one dimension doesn't get any points for being nonzero in the other dimension
            if ((query.MaxHeight == 0) != (query.MaxWidth == 0))
            {
                result = this.GetBestLayout_Quickly(this.SizeZeroQuery);
                if (query.Accepts(result))
                    return result;
                return null;
            }

            // check whether we've previously saved the result
            if (!query.Debug)
            {
                if (this.true_queryResults.TryGetValue(query, out result) || this.inferred_queryResults.TryGetValue(query, out result))
                {
                    if (result != null)
                        return result;
                    return null;
                }
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
                        ErrorReporter.ReportParadox("Error; incorrect result for broadened query: broadened query " + broadened.Query + " returned " + broadened.Response +
                            " whereas the response from the sublayout for debug query " + debugQuery + " is " + correct_subLayout);
                        LayoutQuery debugQuery2 = broadened.Query.DebugClone();
                        debugQuery2.ProposedSolution_ForDebugging = correct_subLayout;
                        this.GetBestLayout(debugQuery2);
                    }
                }
                return this.inferredLayout(query, broadened.Response);
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
                    {
                        ErrorReporter.ReportParadox("Error; incorrect result for shrunken query");
                    }
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
            /* TODO: Figure out why this block doesn't improve performance
            // if we couldn't immediately return a result using the cache, we can still put a bound on the results we might get
            // They have to be at least as good as the sample we found
            if (shrunken != null)
            {
                // First, see if we can improve past what we currently have
                LayoutQuery strictlyImprovedQuery = query.OptimizedPastExample(shrunken.Response);
                bool allowCache = !strictlyImprovedQuery.Accepts(shrunken.Response);

                // Ask the sublayout for this result (or use the cache if we've already asked)
                if (allowCache)
                    result = this.GetBestLayout_Quickly(strictlyImprovedQuery);
                else
                    result = this.Query_SubLayout(strictlyImprovedQuery);
                if (result == null)
                    result = shrunken.Response;
                return this.inferredLayout(query, result);
            }
            else*/
            {
                result = this.Query_SubLayout(query);
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
            LayoutQuery_And_Response queryAndResponse = new LayoutQuery_And_Response(query, result);
            this.orderedResponses.Add(queryAndResponse);


            //this.debugCheck(queryAndResponse);

            if (result != null)
                result = result.Clone();
            return result;
        }

        private SpecificLayout Query_SubLayout(LayoutQuery query)
        {
            numComputations++;
            if (this.true_queryResults.Count == 30)
            {
                System.Diagnostics.Debug.WriteLine("Lots of queries being sent to " + this.layoutToManage);
            }
            if (!query.Debug)
            {
                if (this.true_queryResults.ContainsKey(query))
                    ErrorReporter.ReportParadox("Error, layoutCache repeated a query that was already present");
            }
            SpecificLayout result = this.layoutToManage.GetBestLayout(query);
            if (!query.Debug)
            {
                if (this.true_queryResults.ContainsKey(query))
                    ErrorReporter.ReportParadox("Error, layoutCache query results were saved before it completed?");
                query.OnAnswered(this.layoutToManage);
                this.true_queryResults[query] = result;
            }
            return result;
        }
        private LayoutQuery SizeZeroQuery
        {
            get
            {
                if (sizeZeroQuery == null)
                    sizeZeroQuery = new MaxScore_LayoutQuery(0, 0, LayoutScore.Minimum);
                return sizeZeroQuery;
            }
        }
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            numQueries++;
            if (numQueries % 10000 == 0)
            {
                double rate = (double)numComputations / (double)numQueries;
                System.Diagnostics.Debug.WriteLine("Overall LayoutCache miss rate: " + numComputations + " of " + numQueries + " = " + rate);
                if (rate < 0.02)
                {
                    System.Diagnostics.Debug.WriteLine("Surprisingly high layoutcache hit rate");
                }
            }

            SpecificLayout fastResult = this.GetBestLayout_Quickly(query);
            if (query.Debug)
            {
                SpecificLayout correctResult = this.Query_SubLayout(query);
                if (correctResult != null && !query.Accepts(correctResult))
                {
                    ErrorReporter.ReportParadox("Error: LayoutCache was given an incorrect response by its sublayout");
                }
                bool correct = true;
                if (query.PreferredLayout(correctResult, fastResult) != correctResult)
                {
                    ErrorReporter.ReportParadox("Error: layout cache returned incorrect (superior) result");
                    query.ProposedSolution_ForDebugging = fastResult;
                    correct = false;
                }
                if (query.PreferredLayout(fastResult, correctResult) != fastResult)
                {
                    ErrorReporter.ReportParadox("Error: layout cache returned incorrect (inferior) result");
                    query.ProposedSolution_ForDebugging = correctResult;
                    correct = false;
                }
                if (!correct)
                {
                    this.GetBestLayout_Quickly(query);
                    this.Query_SubLayout(query);
                }
                this.debugCheck(new LayoutQuery_And_Response(query, fastResult));
                return this.prepareLayoutForQuery(correctResult, query);
            }
            //this.debugCheck(new LayoutQuery_And_Response(query, fastResult));
            if (fastResult != null)
                fastResult = fastResult.Clone();
            return this.prepareLayoutForQuery(fastResult, query);
        }

        // Attempts to find a query having results that are accepted by this query
        private LayoutQuery_And_Response FindExample(LayoutQuery query)
        {
            // look for the best response we've seen so far for this query
            LayoutQuery_And_Response best = null;

            foreach (LayoutQuery_And_Response candidate in this.orderedResponses)
            {
                if (best == null)
                {
                    if (query.Accepts(candidate.Response))
                        best = candidate;
                }
                else
                {
                    if (query.PreferredLayout(best.Response, candidate.Response) == candidate.Response)
                        best = candidate;
                }
            }
            return best;
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
                bool otherEncompassesInputs = true;
                bool otherEncompassesOutputs = true;
                bool inputAccepted = true;
                bool thisEncompassesInputs = true;
                // If we're looking for the min width or min height, then tightening the score means we can't necessarily use the other result as the best result
                if (query.MaximizesScore())
                {
                    if (otherQuery.MaxWidth < query.MaxWidth)
                        otherEncompassesInputs = false;
                    if (otherQuery.MaxWidth > query.MaxWidth)
                        thisEncompassesInputs = false;
                    
                    if (otherQuery.MaxHeight < query.MaxHeight)
                        otherEncompassesInputs = false;
                    if (otherQuery.MaxHeight > query.MaxHeight)
                        thisEncompassesInputs = false;

                    if (otherQuery.MinScore.CompareTo(query.MinScore) > 0)
                        otherEncompassesOutputs = false;

                    if (otherResult == null || otherResult.Width > query.MaxWidth)
                        inputAccepted = false;
                    if (otherResult == null || otherResult.Height > query.MaxHeight)
                        inputAccepted = false;
                }
                if (query.MinimizesWidth())
                {
                    if (otherQuery.MaxWidth < query.MaxWidth)
                        otherEncompassesOutputs = false;

                    if (otherQuery.MaxHeight < query.MaxHeight)
                        otherEncompassesInputs = false;
                    if (otherQuery.MaxHeight > query.MaxHeight)
                        thisEncompassesInputs = false;

                    if (otherQuery.MinScore.CompareTo(query.MinScore) > 0)
                        otherEncompassesInputs = false;
                    if (otherQuery.MinScore.CompareTo(query.MinScore) < 0)
                        thisEncompassesInputs = false;

                    if (otherResult == null || otherResult.Score.CompareTo(query.MinScore) < 0)
                        inputAccepted = false;
                    if (otherResult == null || otherResult.Height > query.MaxHeight)
                        inputAccepted = false;
                }
                if (query.MinimizesHeight())
                {
                    if (otherQuery.MaxWidth < query.MaxWidth)
                        otherEncompassesInputs = false;
                    if (otherQuery.MaxWidth > query.MaxWidth)
                        thisEncompassesInputs = false;

                    if (otherQuery.MaxHeight < query.MaxHeight)
                        otherEncompassesOutputs = false;

                    if (otherQuery.MinScore.CompareTo(query.MinScore) > 0)
                        otherEncompassesInputs = false;
                    if (otherQuery.MinScore.CompareTo(query.MinScore) < 0)
                        thisEncompassesInputs = false;

                    if (otherResult == null || otherResult.Width > query.MaxWidth)
                        inputAccepted = false;
                    if (otherResult == null || otherResult.Score.CompareTo(query.MinScore) < 0)
                        inputAccepted = false;
                }
                if (otherEncompassesInputs)
                {
                    if (query.SameType(otherQuery) && query.Accepts(otherResult))
                    {
                        // The previous query had looser inputs but its result was still in our range, so we know its result will be right for us too
                        return queryAndResponse;
                    }
                    if (query.SameType(otherQuery) && inputAccepted && otherEncompassesOutputs)
                    {
                        if (!query.Accepts(otherResult))
                        {
                            // The previous query had looser inputs, and demonstrated that the best output is worse than our threshold
                            // So, our current query has no solution
                            return new LayoutQuery_And_Response(otherQuery, null);
                        }
                    }
                    if (otherEncompassesOutputs && otherResult == null)
                    {
                        // The previous query had looser inputs and looser outputs but no solution, so the current query will also have no solution
                        return queryAndResponse;
                    }
                    if (query.SameType(otherQuery) && thisEncompassesInputs)
                    {
                        if (otherResult != null)
                        {
                            // If we already asked the same question then we already know what the best answer is
                            // We just have to double-check whether the best answer is good enough
                            if (query.Accepts(otherResult))
                            {
                                return queryAndResponse;
                            }
                            else
                            {
                                return new LayoutQuery_And_Response(otherQuery, null);
                            }
                        }
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


        private SpecificLayout inferredLayout(LayoutQuery query, SpecificLayout response)
        {
            this.inferred_queryResults[query] = response;
            LayoutQuery_And_Response pair = new LayoutQuery_And_Response(query, response);
            this.orderedResponses.Add(pair);
            //this.debugCheck(pair);
            return response;
        }

        private void debugCheck(LayoutQuery_And_Response queryAndResponse)
        {
            // make sure that each query likes its response at least as much as all others
            SpecificLayout result = queryAndResponse.Response;
            LayoutQuery query = queryAndResponse.Query;
            for (int i = 0; i < this.orderedResponses.Count; i++) 
            {
                LayoutQuery_And_Response other = this.orderedResponses[i];
                if (other.Query.PreferredLayout(other.Response, result) != other.Response)
                {
                    ErrorReporter.ReportParadox("New response is a better solution to previous query than preexisting response.\n" +
                        "Layout      : " + this.layoutToManage + "\n" +
                        "Old query   : " + other.Query + "\n" +
                        "Old response: " + other.Response + "\n" +
                        "New query   : " + query + "\n" +
                        "New response: " + result);
                    LayoutQuery query2 = other.Query.Clone();
                    query2.Debug = true;
                    query2.ProposedSolution_ForDebugging = result;
                    SpecificLayout oldQueryNewDebugResult = this.GetBestLayout(query2.Clone());
                    LayoutQuery query1 = query.Clone();
                    query1.Debug = true;
                    SpecificLayout newQueryDebugResult = this.GetBestLayout(query1.Clone());
                    System.Diagnostics.Debug.WriteLine("Results from LayoutCache discrepancy: Old query new result = " + oldQueryNewDebugResult + ", New query new result = " + newQueryDebugResult);
                    this.layoutToManage.GetBestLayout(query2.Clone());
                    this.layoutToManage.GetBestLayout(query1.Clone());
                }
                if (query.PreferredLayout(result, other.Response) != result)
                {
                    ErrorReporter.ReportParadox("New response is a worse solution to new query than previous response.\n" +
                        "Layout      : " + this.layoutToManage + "\n" +
                        "Old query   : " + other.Query + "\n" +
                        "Old response: " + other.Response + "\n" +
                        "New query   : " + query + "\n" +
                        "New response: " + result);

                    LayoutQuery query1 = query.Clone();
                    query1.Debug = true;
                    query1.ProposedSolution_ForDebugging = other.Response;
                    SpecificLayout newQueryDebugResult = this.GetBestLayout(query1);
                    LayoutQuery query2 = other.Query.Clone();
                    query2.Debug = true;
                    SpecificLayout oldQueryNewDebugResult = this.GetBestLayout(query2);
                    System.Diagnostics.Debug.WriteLine("Results from LayoutCache discrepancy: Old query new result = " + oldQueryNewDebugResult + ", New query new result = " + newQueryDebugResult);
                    this.GetBestLayout(query1.Clone());
                    this.GetBestLayout(query2.Clone());
                }
            }

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

        Dictionary<LayoutQuery, SpecificLayout> true_queryResults; // for a query, gives the sublayout's response
        Dictionary<LayoutQuery, SpecificLayout> inferred_queryResults; // for a query, gives what we infer must be the sublayout's response
        private LayoutChoice_Set layoutToManage;
        List<LayoutQuery_And_Response> orderedResponses; // all responses that were returned by this LayoutCache that required querying the sublayout as part of their computation
        private LayoutQuery sizeZeroQuery;

        static int numComputations = 0;
        static int numQueries = 0;
    }
}
