using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Error
{
    // Min heap
    public class BinaryHeap<T> where T : IComparable<T>
    {
        #region fields and properties
        const int DEFAULT_SIZE = 16;
        T[] _items;
        int _count;
        int _capacity;

        /// <summary>
        /// Gets the number of values in the heap. 
        /// </summary>
        public int Count
        {
            get { return _count; }
        }
        /// <summary>
        /// Gets or sets the capacity of the heap.
        /// </summary>
        public int Capacity
        {
            get { return _capacity; }
            set
            {
                int previousCapacity = _capacity;
                _capacity = System.Math.Max(value, _count);
                if (_capacity != previousCapacity)
                {
                    T[] temp = new T[_capacity];
                    Array.Copy(_items, temp, _count);
                    _items = temp;
                }
            }
        }
        #endregion

        #region constructors
        public BinaryHeap()
            : this(DEFAULT_SIZE)
        {
        }
        public BinaryHeap(int capacity)
        {
            _capacity = capacity;
            _items = new T[_capacity];
            _count = 0;
        }
        BinaryHeap(T[] data, int count)
        {
            Capacity = count;
            _count = count;
            Array.Copy(data, _items, count);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets the first value in the heap without removing it.
        /// </summary>
        /// <returns>The lowest value of type TValue.</returns>
        public T Peek()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Heap is empty.");
            }
            return _items[0];
        }
        /// <summary>
        /// Removes all items from the heap.
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _items = new T[_capacity]; // this isn't necessary for structs
        }
        /// <summary>
        /// Adds a key and value to the heap.
        /// </summary>
        /// <param name="item">The item to add to the heap.</param>
        public void Add(T item)
        {
            if (_count == _capacity)
            {
                Capacity *= 2;
            }
            _items[_count] = item;
            UpHeap(_count);
            _count++;
        }
        /// <summary>
        /// Removes and returns the first item in the heap.
        /// </summary>
        /// <returns>The next value in the heap.</returns>
        public T Remove()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Cannot remove item, heap is empty.");
            }
            T item = _items[0];
            _count--;
            _items[0] = _items[_count];
            _items[_count] = default(T); //Clears the Last item
            DownHeap(0);
            return item;
        }
        /// <summary>
        /// Creates a new instance of an identical binary heap.
        /// </summary>
        /// <returns>A BinaryHeap.</returns>
        public BinaryHeap<T> Copy()
        {
            return new BinaryHeap<T>(_items, _count);
        }
        /// <summary>
        /// Sets the capacity to the actual number of elements in the BinaryHeap
        /// </summary>
        public void TrimExcess()
        {
            Capacity = _count;
        }
        /// <summary>
        /// Gets an enumerator for the binary heap.
        /// </summary>
        /// <returns>An IEnumerator of type T.</returns>
        //public IEnumerator<T> GetEnumerator()
        //{
        //    for (int i = 0; i < _count; i++)
        //    {
        //        yield return _data[i];
        //    }
        //}
        //System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        //{
        //    return GetEnumerator();
        //}
        #endregion

        #region private methods and stuff
        public void UpHeap(int index)
        {
            T item = _items[index];
            while (index > 0) // While item hasn't bubbled to the top (index = 0)	
            {
                int parent = (index - 1) >> 1; // parent = (index - 1) / 2
                if (item.CompareTo(_items[parent]) < 0)
                {
                    _items[index] = _items[parent];
                    index = parent;
                }
                else break;
            }
            _items[index] = item;
        }
        void DownHeap(int index)
        {// Move item at index down the heap until heap condition is satisfied
            T item = _items[index];
            while (index < (_count >> 1)) // index * 2 > _count is not equal to this!!
            {
                // find smallest children
                int minChild = index * 2 + 1;
                if ((minChild + 1 < _count) && (_items[minChild].CompareTo(_items[minChild + 1])) > 0)
                    ++minChild;

                if (item.CompareTo(_items[minChild]) < 0) break;
                _items[index] = _items[minChild];
                index = minChild;
            }
            _items[index] = item;
        }

        /// <summary>
        /// Don't use this
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }
        #endregion
    }
}
