using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace AEB.Utilities
{
    /// <summary>
    /// Custom object pool implementation for efficient reuse of objects.
    /// </summary>
    /// <typeparam name="T">Type of objects to pool.</typeparam>
    [Serializable]
    [InlineProperty]
    public class CustomLinkedPool<T>
    {
        private readonly Stack<T> _pool;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly int _maxPoolSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomLinkedPool{T}"/> class.
        /// </summary>
        /// <param name="createFunc">Function to create a new instance when the pool is empty.</param>
        /// <param name="onGet">Action to perform when an item is taken from the pool.</param>
        /// <param name="onRelease">Action to perform when an item is returned to the pool.</param>
        /// <param name="initialCapacity">Optional initial number of items to pre-allocate.</param>
        /// <param name="maxPoolSize">Optional maximum number of objects the pool can hold.</param>
        public CustomLinkedPool(
            Func<T> createFunc,
            Action<T> onGet,
            Action<T> onRelease,
            int initialCapacity = 0,
            int maxPoolSize = int.MaxValue)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            _createFunc = createFunc;
            _onGet = onGet;
            _onRelease = onRelease;
            _maxPoolSize = maxPoolSize;

            _pool = new Stack<T>(initialCapacity);
            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(_createFunc());
            }
        }

        /// <summary>
        /// Retrieves an instance from the pool. Creates a new instance if the pool is empty,
        /// or if 'passive' is true (forcing creation regardless of pool contents).
        /// </summary>
        /// <param name="forceCreate">If true, skips using the pool and creates a fresh instance.</param>
        /// <returns>An instance of type T.</returns>
        public T Get(bool forceCreate = false)
        {
            T item;

            if (forceCreate || _pool.Count == 0)
            {
                item = _createFunc();
            }
            else
            {
                item = _pool.Pop();
            }

            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Returns an instance back to the pool.
        /// </summary>
        /// <param name="item">The item to return.</param>
        public void Release(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _onRelease?.Invoke(item);

            if (_pool.Count < _maxPoolSize)
            {
                _pool.Push(item);
            }
        }

        /// <summary>
        /// Adds an instance directly to the pool without invoking the onRelease action.
        /// Use this method to pre-populate the pool or add externally created objects.
        /// </summary>
        /// <param name="item">The item to add to the pool.</param>
        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_pool.Count < _maxPoolSize)
            {
                _pool.Push(item);
            }
        }

        /// <summary>
        /// Peeks at the item at the specified index without removing it from the pool.
        /// </summary>
        /// <param name="index">The zero-based index in the pool (0 = most recently added).</param>
        /// <returns>The item at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        public T Peek(int index)
        {
            if (index < 0 || index >= _pool.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            // Stack returns items in reverse order of insertion, most recent is first
            return new List<T>(_pool)[index];
        }

        /// <summary>
        /// Returns the next item that would be retrieved from the pool (top of the stack),
        /// without removing it.
        /// </summary>
        /// <returns>The top item in the pool, or default if the pool is empty.</returns>
        public T Peek()
        {
            return _pool.Count > 0 ? _pool.Peek() : default;
        }

        /// <summary>
        /// Returns all currently pooled (available) items without modifying the pool.
        /// </summary>
        public IEnumerable<T> PeekAll()
        {
            return _pool;
        }

        /// <summary>
        /// Gets the current number of objects in the pool.
        /// </summary>
        public int Count => _pool.Count;

        /// <summary>
        /// Gets a snapshot of the pool’s contents as a new list.  
        /// The most recently added items appear first, matching the stack order.
        /// </summary>
        public List<T> PoolContents
        {
            get { return new List<T>(_pool); }
        }
    }
}
