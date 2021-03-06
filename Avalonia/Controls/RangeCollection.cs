﻿// -----------------------------------------------------------------------
// <copyright file="RangeCollection.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Avalonia.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class RangeCollection : ICloneable, ICollection<int>
    {
        private const int MinCapacity = 16;
        
        private Range[] ranges;
        
        private int rangeCount;
        
        private int indexCount;
        
        private int generation;

        public RangeCollection()
        {
            this.Clear();
        }

        public Range[] Ranges
        {
            get
            {
                Range[] copy = new Range[this.rangeCount];
                Array.Copy(this.ranges, copy, this.rangeCount);
                return copy;
            }
        }

        public int RangeCount
        {
            get { return this.rangeCount; }
        }

        public int Count
        {
            get { return this.indexCount; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int this[int index]
        {
            get
            {
                for (int i = 0, cumlCount = 0; i < this.rangeCount && index >= 0; i++)
                {
                    if (index < (cumlCount += this.ranges[i].Count))
                    {
                        return this.ranges[i].End - (cumlCount - index) + 1;
                    }
                }

                throw new IndexOutOfRangeException(index.ToString());
            }
        }

        public int FindRangeIndexForValue(int value)
        {
            int min = 0;
            int max = this.rangeCount - 1;

            while (min <= max)
            {
                int mid = min + ((max - min) / 2);
                Range range = this.ranges[mid];
                if (value >= range.Start && value <= range.End)
                {
                    return mid;    // In Range
                }
                else
                {
                    if (value < range.Start)
                    {
                        max = mid - 1; // Below Range
                    }
                    else
                    {
                        min = mid + 1; // Above Range
                    }
                }
            }

            return ~min;
        }

        public int IndexOf(int value)
        {
            int offset = 0;

            foreach (Range range in this.ranges)
            {
                if (value >= range.Start && value <= range.End)
                {
                    return offset + (value - range.Start);
                }

                offset += range.End - range.Start + 1;
            }

            return -1;
        }

        public bool Add(int value)
        {
            if (!this.Contains(value))
            {
                this.generation++;
                this.InsertRange(new Range(value, value));
                this.indexCount++;
                return true;
            }

            return false;
        }

        public bool Remove(int value)
        {
            this.generation++;
            return this.RemoveIndexFromRange(value);
        }

        public void Clear()
        {
            this.rangeCount = 0;
            this.indexCount = 0;
            this.generation++;
            this.ranges = new Range[MinCapacity];
        }

        public bool Contains(int value)
        {
            return this.FindRangeIndexForValue(value) >= 0;
        }

        public void CopyTo(int[] array, int index)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < this.rangeCount; i++)
            {
                for (int j = this.ranges[i].Start; j <= this.ranges[i].End; j++)
                {
                    yield return j;
                }
            }
        }

        void ICollection<int>.Add(int value)
        {
            this.Add(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static int CompareRanges(Range a, Range b)
        {
            return (a.Start + (a.End - a.Start)).CompareTo(b.Start + (b.End - b.Start));
        }

        private void Shift(int start, int delta)
        {
            if (delta < 0)
            {
                start -= delta;
            }

            if (start < this.rangeCount)
            {
                Array.Copy(this.ranges, start, this.ranges, start + delta, this.rangeCount - start);
            }

            this.rangeCount += delta;
        }

        private void EnsureCapacity(int growBy)
        {
            int newCapacity = this.ranges.Length == 0 ? 1 : this.ranges.Length;
            int minCapacity = this.ranges.Length == 0 ? MinCapacity : this.ranges.Length + growBy;

            while (newCapacity < minCapacity)
            {
                newCapacity <<= 1;
            }

            Array.Resize(ref this.ranges, newCapacity);
        }

        private void Insert(int position, Range range)
        {
            if (this.rangeCount == this.ranges.Length)
            {
                this.EnsureCapacity(1);
            }

            this.Shift(position, 1);
            this.ranges[position] = range;
        }

        private void RemoveAt(int position)
        {
            this.Shift(position, -1);
            Array.Clear(this.ranges, this.rangeCount, 1);
        }

        private bool RemoveIndexFromRange(int index)
        {
            int rangeIndex = this.FindRangeIndexForValue(index);
            
            if (rangeIndex < 0)
            {
                return false;
            }

            Range range = this.ranges[rangeIndex];
            if (range.Start == index && range.End == index)
            {
                this.RemoveAt(rangeIndex);
            }
            else
            {
                if (range.Start == index)
                {
                    this.ranges[rangeIndex].Start++;
                }
                else
                {
                    if (range.End == index)
                    {
                        this.ranges[rangeIndex].End--;
                    }
                    else
                    {
                        Range splitRange = new Range(index + 1, range.End);
                        this.ranges[rangeIndex].End = index - 1;
                        this.Insert(rangeIndex + 1, splitRange);
                    }
                }
            }

            this.indexCount--;
            return true;
        }

        private void InsertRange(Range range)
        {
            int position = this.FindInsertionPosition(range);
            bool mergedLeft = this.MergeLeft(range, position);
            bool mergedRight = this.MergeRight(range, position);

            if (!mergedLeft && !mergedRight)
            {
                this.Insert(position, range);
            }
            else
            {
                if (mergedLeft && mergedRight)
                {
                    this.ranges[position - 1].End = this.ranges[position].End;
                    this.RemoveAt(position);
                }
            }
        }

        private bool MergeLeft(Range range, int position)
        {
            int left = position - 1;
            if (left >= 0 && this.ranges[left].End + 1 == range.Start)
            {
                this.ranges[left].End = range.Start;
                return true;
            }

            return false;
        }

        private bool MergeRight(Range range, int position)
        {
            if (position < this.rangeCount && this.ranges[position].Start - 1 == range.End)
            {
                this.ranges[position].Start = range.End;
                return true;
            }

            return false;
        }

        private int FindInsertionPosition(Range range)
        {
            int min = 0;
            int max = this.rangeCount - 1;

            while (min <= max)
            {
                int mid = min + ((max - min) / 2);
                int cmp = CompareRanges(this.ranges[mid], range);

                if (cmp == 0)
                {
                    return mid;
                }
                else
                {
                    if (cmp > 0)
                    {
                        if (mid > 0 && CompareRanges(this.ranges[mid - 1], range) < 0)
                        {
                            return mid;
                        }

                        max = mid - 1;
                    }
                    else
                    {
                        min = mid + 1;
                    }
                }
            }

            return min;
        }

        public struct Range
        {
            private int start;
            private int end;

            /// <summary>
            /// Initializes a new instance of the <see cref="Range"/> struct.
            /// </summary>
            public Range(int start, int end)
            {
                this.start = start;
                this.end = end;
            }

            public int Start
            {
                get { return this.start; }
                set { this.start = value; }
            }

            public int End
            {
                get { return this.end; }
                set { this.end = value; }
            }

            public int Count
            {
                get { return this.End - this.Start + 1; }
            }

            public override string ToString()
            {
                return string.Format("{0}-{1} ({2})", this.Start, this.End, this.Count);
            }
        }
    }
}
