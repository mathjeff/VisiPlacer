﻿using System;
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
            this.true_queryResults = new Dictionary<LayoutQuery, SpecificLayout>(this);
            this.inferred_queryResults = new Dictionary<LayoutQuery, SpecificLayout>(this);
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
            if (this.true_queryResults.TryGetValue(query, out result) || this.inferred_queryResults.TryGetValue(query, out result))
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
            // if we couldn't immediately return a result using the cache, we can still put a bound on the results we might get
            // They have to be at least as good as the sample we found
            if (shrunken != null)
            {
                // First, see if we can improve past what we currently have
                LayoutQuery strictlyImprovedQuery = query.Clone();
                strictlyImprovedQuery.OptimizePastExample(shrunken.Response);
                result = this.Query_SubLayout(strictlyImprovedQuery);
                if (result == null)
                    result = shrunken.Response;      // couldn't improve past the previously found best layout
                return this.inferredLayout(query, result);
            }
            else
            {
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
            if (this.true_queryResults.ContainsKey(query))
                ErrorReporter.ReportParadox("Error, layoutCache repeated a query that was already present ");
            this.true_queryResults[query.Clone()] = result;
            LayoutQuery_And_Response queryAndResponse = new LayoutQuery_And_Response(query, result);
            this.orderedResponses.Add(queryAndResponse);


            this.debugCheck(queryAndResponse);

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


        private SpecificLayout inferredLayout(LayoutQuery query, SpecificLayout response)
        {
            this.inferred_queryResults[query] = response;
            return response;
        }

        private void debugCheck(LayoutQuery_And_Response queryAndResponse)
        {
            // make sure that each query likes its response at least as much as all others
            SpecificLayout result = queryAndResponse.Response;
            foreach (LayoutQuery_And_Response other in this.orderedResponses)
            {
                if (other.Query.PreferredLayout(other.Response, result) != other.Response)
                {
                    ErrorReporter.ReportParadox("New response " + result + " from " + this.layoutToManage + " is a better solution to preexisting query " + other.Query + " than preexisting response " + other.Response);
                    LayoutQuery query1 = queryAndResponse.Query.Clone();
                    query1.Debug = true;
                    this.layoutToManage.GetBestLayout(query1);
                    LayoutQuery query2 = other.Query.Clone();
                    query2.Debug = true;
                    this.layoutToManage.GetBestLayout(query2);
                    System.Diagnostics.Debug.WriteLine("done debugging LayoutCache discrepancy");
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
        List<LayoutQuery_And_Response> orderedResponses;
        static int numComputations = 0;
        static int numQueries = 0;
    }
}
