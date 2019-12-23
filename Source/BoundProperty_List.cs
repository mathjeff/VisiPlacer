using System;
using System.Collections.Generic;
using System.Linq;

namespace VisiPlacement
{
    public class BoundProperty_List
    {
        public static BoundProperty_List Uniform(int numProperties)
        {
            BoundProperty_List result = new BoundProperty_List(numProperties);
            int i;
            for (i = 1; i < numProperties; i++)
            {
                result.BindIndices(0, i);
            }
            return result;
        }
        public BoundProperty_List(int count)
        {
            this.values = new double[count];
            this.scales = new double[count];
            this.indicesByGroup = new List<List<int>>();
            int i;
            for (i = 0; i < count; i++)
            {
                this.scales[i] = 1;

                List<int> group = new List<int>();
                group.Add(i);
                indicesByGroup.Add(group);
            }
        }
        public BoundProperty_List(BoundProperty_List original)
        {
            this.values = new double[original.values.Length];
            original.values.CopyTo(this.values, 0);

            this.scales = new double[original.scales.Length];
            original.scales.CopyTo(this.scales, 0);

            this.indicesByGroup = new List<List<int>>(original.indicesByGroup);
        }
        // Sets the scale factor for the given property. 
        public void SetPropertyScale(int propertyIndex, double scale)
        {
            scales[propertyIndex] = scale;
        }
        public double GetPropertyScale(int propertyIndex)
        {
            return scales[propertyIndex];
        }
        // Binds two properties. The ratio between any properties that are bound will be equal to the ratio of their scales
        public void BindIndices(int propertyIndex1, int propertyIndex2)
        {
            int groupIndex1 = this.GetGroupIndexFromPropertyIndex(propertyIndex1);
            int groupIndex2 = this.GetGroupIndexFromPropertyIndex(propertyIndex2);
            // make sure they groups aren't already bound
            if (groupIndex1 == groupIndex2)
                return;
            List<int> newGroup = new List<int>(this.indicesByGroup[groupIndex1].Concat(this.indicesByGroup[groupIndex2]));
            // add the larger group
            this.indicesByGroup.Add(newGroup);
            // remove the smaller groups
            this.indicesByGroup.RemoveAt(Math.Max(groupIndex1, groupIndex2));
            this.indicesByGroup.RemoveAt(Math.Min(groupIndex1, groupIndex2));
        }
        // rescales group[groupIndex] to the lowest value possible such that each value is >= the corresponding minValue
        // returns true iff anything actually changed
        public bool SetMinValues(int groupIndex, List<double> minValues)
        {
            double groupRatio = 0;
            int i, propertyIndex;
            List<int> indices = this.GetGroupAtIndex(groupIndex);
            // figure out what the scale factor is
            for (i = 0; i < minValues.Count; i++)
            {
                propertyIndex = indices[i];
                if (this.scales[propertyIndex] != 0)
                {
                    double currentRatio = minValues[i] / this.scales[propertyIndex];
                    if (currentRatio > groupRatio)
                        groupRatio = currentRatio;
                }
            }
            bool changed = false;
            // now set each value accordingly
            for (i = 0; i < minValues.Count; i++)
            {
                propertyIndex = indices[i];
                double newValue = this.scales[propertyIndex] * groupRatio;
                if (this.values[propertyIndex] != newValue)
                    changed = true;
                this.values[propertyIndex] = newValue;
                if (newValue < minValues[i])
                {
                    ErrorReporter.ReportParadox("rounding error in BoundProperyList::SetMinValues");
                }
            }

            return changed;
        }
        public void SetValue(int propertyIndex, double value)
        {
            if (scales[propertyIndex] == 0)
                throw new ArgumentException("cannot set the value of a property having zero weight");
            if (double.IsNaN(value))
                throw new ArgumentException("illegal value: " + value + " to set for index " + propertyIndex);
            int groupIndex = this.GetGroupIndexFromPropertyIndex(propertyIndex);
            double groupRatio = value / this.scales[propertyIndex];
            if (double.IsNaN(groupRatio))
                throw new ArgumentException("Cannot set value " + value + " for property " + propertyIndex + " having scale of " + this.scales[propertyIndex]);
            // rescale the entire group so each value is its scale times the group ratio
            List<int> indices = this.indicesByGroup[groupIndex];
            foreach (int index in indices)
            {
                double oldValue = values[index];
                double newValue = scales[index] * groupRatio;
                double difference = newValue - oldValue;
                values[index] = newValue;
            }
            if (values[propertyIndex] != value)
            {
                ErrorReporter.ReportParadox("rounding error!");
            }
        }

        public void Set_GroupTotal(int groupIndex, double totalValue)
        {
            double totalWeight = this.GetGroupScale(groupIndex);
            if (totalWeight == 0)
                totalWeight = 1;
            List<int> indices = this.indicesByGroup[groupIndex];
            foreach (int index in indices)
            {
                double currentWeight = scales[index];
                double currentValue = totalValue * currentWeight / totalWeight;

                if (double.IsInfinity(totalValue))
                    currentValue = double.PositiveInfinity;
                this.values[index] = currentValue;

            }

            if (!double.IsInfinity(totalValue))
            {
                // fix any errors as evenly as possible
                double cumulativeWeight = 0;
                foreach (int index in indices)
                {
                    double error = totalValue - this.Get_GroupTotal(groupIndex);
                    if (error == 0)
                        break;
                    double currentWeight = this.scales[index];
                    double currentAdjustment = error * currentWeight / (totalWeight - cumulativeWeight);
                    this.values[index] += currentAdjustment;

                    cumulativeWeight += currentWeight;
                }


                // fix any remaining errors without requiring evenness
                foreach (int index in indices)
                {
                    double error = totalValue - this.Get_GroupTotal(groupIndex);
                    if (error == 0)
                        break;
                    double currentAdjustment = error / 2;
                    double newValue = this.values[index] + currentAdjustment;
                    this.values[index] = newValue;

                    error = totalValue - this.Get_GroupTotal(groupIndex);
                    if (error == 0)
                        break;
                    currentAdjustment = error;
                    newValue = this.values[index] + currentAdjustment;
                    this.values[index] = newValue;
                }

            }

            if (this.Get_GroupTotal(groupIndex) != totalValue)
            {
                ErrorReporter.ReportParadox("rounding error!");
            }
        }
        public double Get_GroupTotal(int groupIndex)
        {
            double total = 0;
            List<int> indices = this.indicesByGroup[groupIndex];
            foreach (int index in indices)
            {
                total += values[index];
            }
            return total;
        }
        public double GetValue(int propertyIndex)
        {
            return this.values[propertyIndex];
        }

        public int NumProperties
        {
            get
            {
                return this.values.Length;
            }
        }
        public int NumInfiniteProperties
        {
            get
            {
                int count = 0;
                for (int i = 0; i < this.values.Length; i++)
                {
                    if (double.IsInfinity(this.values[i]))
                        count++;
                }
                return count;
            }
        }

        // computes the weight of the property with the given index, divided by the weight of the group it is in
        public double GetFraction(int propertyIndex)
        {
            double weight = this.scales[propertyIndex];
            int groupIndex = this.GetGroupIndexFromPropertyIndex(propertyIndex);
            double groupWeight = this.GetGroupScale(groupIndex);
            return weight / groupWeight;
        }
        // computes the weight of the group at groupIndex
        public double GetGroupScale(int groupIndex)
        {
            double weight = 0;
            foreach (int index in this.indicesByGroup[groupIndex])
            {
                weight += this.scales[index];
            }
            return weight;
        }

        public double GetTotalValue()
        {
            double total = 0;
            int i;
            // The reason for adding these values in this order is to ensure that the rounding error is exactly zero when we update the total value of a group
            for (i = 0; i < this.NumGroups; i++)
            {
                total += this.Get_GroupTotal(i);
            }
            return total;
        }
        public double GetTotalWeight()
        {
            double total = 0;
            int i;
            // The reason for adding these values in this order is to ensure that the rounding error is exactly zero when we update the total value of a group
            for (i = 0; i < this.NumGroups; i++)
            {
                total += this.GetGroupScale(i);
            }
            return total;
        }

        public void TryToRescaleToTotalValue(double newTotal)
        {
            double currentTotal = this.GetTotalValue();
            if (currentTotal != 0 && !double.IsInfinity(currentTotal))
                this.RescaleToTotalValue(newTotal);
        }

        public void RescaleToTotalValue(double total)
        {
            double existingTotal = this.GetTotalValue();
            double ratio;
            if (double.IsPositiveInfinity(total))
                ratio = total;
            else
                ratio = total / this.GetTotalValue();
            for (int i = 0; i < this.NumGroups; i++)
            {
                this.Set_GroupTotal(i, this.Get_GroupTotal(i) * ratio);
            }
            // also go back and check for rounding error
            if (!double.IsInfinity(total))
            {
                // fix any remaining errors without requiring evenness
                for (int i = 0; i < this.NumGroups; i++)
                {
                    double error = total - this.GetTotalValue();
                    if (error == 0)
                        break;
                    double currentAdjustment = error;
                    double newValue = this.Get_GroupTotal(i) + currentAdjustment;
                    this.Set_GroupTotal(i, newValue);
                }
            }
        }

        public int NumGroups
        {
            get
            {
                return this.indicesByGroup.Count;
            }
        }
        public int GetGroupIndexFromPropertyIndex(int index)
        {
            // this could be optimized more if needed
            int i;
            for (i = 0; i < indicesByGroup.Count; i++)
            {
                if (this.indicesByGroup[i].Contains(index))
                    return i;
            }
            return -1;
        }
        public List<int> GetGroupAtIndex(int index)
        {
            return this.indicesByGroup[index];
        }
        public List<double> Values
        {
            get
            {
                return new List<double>(this.values);
            }
        }
        public override string ToString()
        {
            string result = "(";
            for (int i = 0; i < this.values.Length; i++)
            {
                result += values[i];
                if (i < this.values.Length - 1)
                    result += ",";
            }
            result += ")";
            return result;
        }
        double[] scales;
        double[] values;
        List<List<int>> indicesByGroup;
    }
}
