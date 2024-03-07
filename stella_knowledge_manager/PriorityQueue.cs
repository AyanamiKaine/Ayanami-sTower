using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{

    /// <summary>
    /// This abstract class defines the priority queue for abstract items.
    /// Its based on a float value of 0.0f to 100.0f, where 100.0f is the most prioritized item
    /// and 0.0f the least 
    /// </summary>
    public abstract class PriorityQueue<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey 
        : notnull

    {
        protected Dictionary<TKey, TValue> _items = new();

        public abstract void AddItem(TKey key, TValue value);
        public abstract void RemoveItem(TKey key);
        
        /// <summary>
        /// Get the highest priority item without removing it.
        /// </summary>
        /// <returns></returns>
        public abstract TKey Peek();
        public abstract int Count();
        public abstract TValue GetPriority(TKey key);
        public abstract void SetPriority(TKey key, TValue value);
        public abstract void Clear();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            // Implement your iteration logic here. You'll likely need to consider:

            // 1. Returning items based on priority order
            // 2. How to handle potential changes to the queue during iteration (consider a snapshot approach)

            // Simple placeholder: (This iterates without priority order)
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
