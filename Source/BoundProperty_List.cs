﻿using System;
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
                double currentRatio = minValues[i] / this.scales[propertyIndex];
                if (currentRatio > groupRatio)
                    groupRatio = currentRatio;
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
            int groupIndex = this.GetGroupIndexFromPropertyIndex(propertyIndex);
            double groupRatio = value / this.scales[propertyIndex];
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
            double totalWeight = this.GetGroupWeight(groupIndex);
            double groupRatio = totalValue / totalWeight;
            List<int> indices = this.indicesByGroup[groupIndex];
            double remainingValue = totalValue;
            double remainingWeight = totalWeight;
            // this is done in a strange way to better deal with rounding error

            /*
            if (indices.Count == 18 && Math.Abs(totalValue / 18 - 15.96) < 0.01)
            {
                ErrorReporter.ReportParadox("now");
                double oneRow = 15.96;
                double multipliedTotal = oneRow * 18;
                if (multipliedTotal < totalValue)
                {
                    ErrorReporter.ReportParadox("15.96 * 18 < totalValue");
                }
                else
                {
                    ErrorReporter.ReportParadox("15.96 * 18 >= totalValue");
                }
                double summedTotal = 0;
                for (int i = 0; i < 18; i++)
                {
                    summedTotal += oneRow;
                }
                if (summedTotal < totalValue)
                {
                    ErrorReporter.ReportParadox("15.96 * 18 < totalValue");
                }
                else
                {
                    ErrorReporter.ReportParadox("15.96 * 18 >= totalValue");
                }

            }
            */
            foreach (int index in indices)
            {
                double currentWeight = scales[index];
                /*double a = (totalValue - cumulativeValue);
                double b = a * currentWeight;
                double c = (totalWeight - cumulativeWeight);
                double currentValue = b / c;
                */

                //double currentValue = (totalValue - cumulativeValue) * currentWeight / (totalWeight - cumulativeWeight);
                double currentValue = totalValue * currentWeight / totalWeight;

                //Decimal d;

                if (double.IsInfinity(totalValue))
                    currentValue = double.PositiveInfinity;
                this.values[index] = currentValue;

                //cumulativeValue += currentValue;
                //cumulativeWeight += currentWeight;
                //remainingValue -= currentValue;
                //remainingWeight -= currentWeight;
                //values[index] = scales[index] * groupRatio;
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

        // computes the weight of the property with the given index, divided by the weight of the group it is in
        public double GetFraction(int propertyIndex)
        {
            double weight = this.scales[propertyIndex];
            int groupIndex = this.GetGroupIndexFromPropertyIndex(propertyIndex);
            double groupWeight = this.GetGroupWeight(groupIndex);
            return weight / groupWeight;
        }
        // computes the weight of the group at groupIndex
        public double GetGroupWeight(int groupIndex)
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
            /*for (i = 0; i < this.NumProperties; i++)
            {
                total += this.values[i];
            }
            */
            // The reason for adding these values in this order is to ensure that the rounding error is exactly zero when we update the total value of a group
            for (i = 0; i < this.NumGroups; i++)
            {
                total += this.Get_GroupTotal(i);
            }
            return total;
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
        double[] scales;
        double[] values;
        List<List<int>> indicesByGroup;
    }
}
