using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StatLists;

namespace VisiPlacement
{
    public class LayoutScore : IComparer<double>, ICombiner<double>
    {
        // the lowest score possible
        public static LayoutScore Minimum
        {
            get
            {
                LayoutScore minimum = new LayoutScore();
                minimum.components.Add(double.PositiveInfinity, double.NegativeInfinity);
                return minimum;
            }
        }
        public static LayoutScore Maximum
        {
            get
            {
                LayoutScore maximum = new LayoutScore();
                maximum.components.Add(double.PositiveInfinity, double.PositiveInfinity);
                return maximum;
            }
        }
        // adding or subtracting Zero will never change the score
        public static LayoutScore Zero
        {
            get
            {
                return new LayoutScore();
            }
        }
        // an extremely small score that is still larger than zero
        public static LayoutScore Tiny
        {
            get
            {
                return new LayoutScore(-10000, 1);
            }
        }
        public static LayoutScore Min(LayoutScore a, LayoutScore b)
        {
            if (a.CompareTo(b) < 0)
                return a;
            else
                return b;
        }
        public static LayoutScore Max(LayoutScore a, LayoutScore b)
        {
            if (a.CompareTo(b) > 0)
                return a;
            else
                return b;
        }

        // a score that is lower in priority than any other publicly available scores (except for Tiny which is an implementation detail)
        // which can be helpful for testing
        public static LayoutScore Get_MinPriorityScore_ForTesting(double numItems)
        {
            return new LayoutScore(-1, numItems);

        }
        // this score indicates that some items are not centered
        public static LayoutScore Get_UnCentered_LayoutScore(double numItems)
        {
            return new LayoutScore(0, -numItems);
        }
        // this score indicates that some space has been used
        public static LayoutScore Get_UsedSpace_LayoutScore(double numPixels)
        {
            double value = Math.Sqrt(numPixels);
            double multiplier = 256;
            double rounded = Math.Round(value * multiplier) / multiplier;
            return new LayoutScore(1, rounded);
        }
        // this score indicates that some items were laid out awkwardly
        /*public static LayoutScore Get_Disjointed_LayoutScore(double numItems)
        {
            return new LayoutScore(2, -numItems);
        }*/
        // this score indicates that some items were cut off
        public static LayoutScore Get_CutOff_LayoutScore(double numItems)
        {
            return new LayoutScore(2, -numItems);
        }

        public LayoutScore()
        {
            this.Initialize();
        }
        private LayoutScore(double priority, double weight)
        {
            this.Initialize();
            this.addComponent(priority, weight);
        }
        public LayoutScore(LayoutScore original)
        {
            this.Initialize();
            this.CopyFrom(original);
        }
        private void Initialize()
        {
            this.components = new StatList<double, double>(this, this);
        }
        public void CopyFrom(LayoutScore original)
        {
            this.components = new StatList<double, double>(original.components);
        }
        public LayoutScore Plus(LayoutScore other)
        {
            int ourIndex = 0;
            int theirIndex = 0;
            LayoutScore sum = new LayoutScore();
            while (true)
            {
                double priority;
                double weight = 0;
                if (ourIndex < this.components.NumItems)
                {
                    if (theirIndex < other.components.NumItems)
                    {
                        // both coordinates exist
                        ListItemStats<double, double> ourComponent = this.components.GetValueAtIndex(ourIndex);
                        ListItemStats<double, double> theirComponent = other.components.GetValueAtIndex(theirIndex);
                        priority = Math.Min(ourComponent.Key, theirComponent.Key);
                        if (ourComponent.Key == priority)
                        {
                            weight += ourComponent.Value;
                            ourIndex++;
                        }
                        if (theirComponent.Key == priority)
                        {
                            if (double.IsInfinity(weight) && double.IsInfinity(theirComponent.Value)
                                && double.IsPositiveInfinity(weight) != double.IsPositiveInfinity(theirComponent.Value))
                            {
                                // Treat negative infinity plus positive infinity as zero
                                weight = 0;
                            }
                            else
                            {
                                weight += theirComponent.Value;
                            }
                            theirIndex++;
                        }
                    }
                    else
                    {
                        // only our coordinate exists
                        ListItemStats<double, double> ourComponent = this.components.GetValueAtIndex(ourIndex);
                        priority = ourComponent.Key;
                        weight = ourComponent.Value;
                        ourIndex++;
                    }
                }
                else
                {
                    if (theirIndex < other.components.NumItems)
                    {
                        // only their coordinate exists
                        ListItemStats<double, double> theirComponent = other.components.GetValueAtIndex(theirIndex);
                        priority = theirComponent.Key;
                        weight = theirComponent.Value;
                        theirIndex++;
                    }
                    else
                    {
                        // no more components are left
                        // debug-check: make sure there are no duplicate coordinates
                        int i;
                        for (i = 1; i < sum.components.NumItems; i++)
                        {
                            if (sum.components.GetValueAtIndex(i - 1).Key == sum.components.GetValueAtIndex(i).Key)
                            {
                                System.Diagnostics.Debug.WriteLine("error: layout score has a duplicate key");
                            }
                        }
                        

                        return sum;
                    }
                }
                // only add the coordinate if it is nonzero, because zeros are always provided by default
                if (weight != 0)
                    sum.addComponent(priority, weight);
            }
        }
        public LayoutScore Times(double weightScale)
        {
            LayoutScore product = new LayoutScore();
            foreach (ListItemStats<double, double> item in this.components.AllItems)
            {
                product.addComponent(item.Key, item.Value * weightScale);
            }
            return product;
        }
        public LayoutScore Minus(LayoutScore other)
        {
            return this.Plus(other.Times(-1));
        }
        // Divides the two scores and truncates the result into a double
        public double DividedBy(LayoutScore other)
        {
            ListItemStats<double, double> ourFirstItem = this.components.GetLastValue();
            if (ourFirstItem == null)
                ourFirstItem = new ListItemStats<double, double>(double.NegativeInfinity, 0);
            ListItemStats<double, double> theirFirstItem = other.components.GetLastValue();
            if (theirFirstItem == null)
                theirFirstItem = new ListItemStats<double, double>(double.NegativeInfinity, 0);
            double ourValue = 0;
            double theirValue = 0;
            if (ourFirstItem.Key > theirFirstItem.Key)
                theirValue = 0;
            else
                theirValue = theirFirstItem.Value;
            if (ourFirstItem.Key < theirFirstItem.Key)
                ourValue = 0;
            else
                ourValue = ourFirstItem.Value;
            return ourValue / theirValue;
        }
        public int CompareTo(LayoutScore other)
        {
            int ourIndex = this.components.NumItems - 1;
            int theirIndex = other.components.NumItems - 1;
            // loop over the list of components and once a coordinate differs, use it for the comparison
            while (true)
            {
                if (ourIndex >= 0)
                {
                    if (theirIndex >= 0)
                    {
                        // both coordinates exist
                        ListItemStats<double, double> ourComponent = this.components.GetValueAtIndex(ourIndex);
                        ListItemStats<double, double> theirComponent = other.components.GetValueAtIndex(theirIndex);
                        // check for the possibility that one coordinate is more important than the other
                        if (ourComponent.Key > theirComponent.Key)
                        {
                            if (ourComponent.Value != 0)
                                return ourComponent.Value.CompareTo(0);
                        }
                        else
                        {
                            if (theirComponent.Key > ourComponent.Key)
                            {
                                if (theirComponent.Value != 0)
                                    return -theirComponent.Value.CompareTo(0);
                            }
                            else
                            {
                                // keys are equal
                                int comparison = ourComponent.Value.CompareTo(theirComponent.Value);
                                if (comparison != 0)
                                    return comparison;
                            }
                        }
                        if (ourComponent.Key >= theirComponent.Key)
                            ourIndex--;
                        if (ourComponent.Key <= theirComponent.Key)
                            theirIndex--;
                    }
                    else
                    {
                        // only our coordinate exists
                        ListItemStats<double, double> ourComponent = this.components.GetValueAtIndex(ourIndex);
                        if (ourComponent.Value != 0)
                            return ourComponent.Value.CompareTo(0);
                        ourIndex--;
                    }
                }
                else
                {
                    if (theirIndex >= 0)
                    {
                        // only their coordinate exists
                        ListItemStats<double, double> theirComponent = other.components.GetValueAtIndex(theirIndex);
                        if (theirComponent.Value != 0)
                            return -theirComponent.Value.CompareTo(0);
                        theirIndex--;
                    }
                    else
                    {
                        // neither score has any more coordinates
                        return 0;
                    }
                }
            }
        }

        public override bool Equals(object other)
        {
            LayoutScore otherScore = other as LayoutScore;
            if (otherScore != null)
                return (this.CompareTo(otherScore) == 0);
            return false;
        }

        public override int GetHashCode()
        {
            int value = 0;
            foreach (ListItemStats<double, double> component in this.components.AllItems)
            {
                value = value * 100;
                value = value + component.Key.GetHashCode();
                value = value * 100;
                value = value + component.Value.GetHashCode();
            }
            return value;
        }

        private void addComponent(double priority, double weight)
        {
            // workaround for rounding error
            weight = Math.Round(weight, 6);
            this.components.Add(priority, weight);
            ListItemStats<double, double> lastItem = this.components.GetLastValue();
            if (double.IsPositiveInfinity(lastItem.Key))
            {
                if (lastItem.Value > 0)
                    this.CopyFrom(LayoutScore.Maximum);
                else
                    this.CopyFrom(LayoutScore.Minimum);
            }
        }

        public string DebugSummary
        {
            get
            {
                return this.ToString();
            }
        }
        public override string ToString()
        {
            String result = "(";
            bool isFirst = true;
            foreach (ListItemStats<double, double> component in this.components.AllItems)
            {
                if (!isFirst)
                    result += ",";
                result += component.Key + ":" + component.Value;
                isFirst = false;
            }
            result += ")";
            return result;
        }

        public int NumComponents
        {
            get
            {
                return this.components.NumItems;
            }
        }

        public LayoutScore ComponentRange(int minIndexInclusive, int maxIndexExclusive)
        {
            LayoutScore range = new LayoutScore();
            List<ListItemStats<double, double>> subComponents = this.components.ItemsBetweenIndices(minIndexInclusive, maxIndexExclusive);
            foreach (ListItemStats<double, double> component in subComponents)
            {
                range.addComponent(component.Key, component.Value);
            }
            return range;
        }

        #region Required for IComparer<double>

        public int Compare(double priority1, double priority2)
        {
            return priority1.CompareTo(priority2);
        }

        #endregion

        #region Required for ICombiner<double>

        // this isn't actually used
        public double Combine(double a, double b)
        {
            return 0;
        }

        public double Default()
        {
            return 0;
        }

        #endregion

        private StatList<double, double> components;
    }
}
