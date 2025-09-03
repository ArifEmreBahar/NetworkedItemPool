using AEB.Photon;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AEB.Utilities
{
    /// <summary>
    /// Manages a pool of Item objects using a custom ObjectPool for efficient reuse.
    /// </summary>
    public class ItemPool : Singleton<ItemPool>
    {
        #region Fields

        /// <summary>
        /// Prefabs for the Item objects to be pooled.
        /// </summary>
        [SerializeField]
        protected List<Item> itemPrefabs;

        /// <summary>
        /// Object pools for managing Item instances.
        /// </summary>
        protected ListDictionary<Type, CustomLinkedPool<Item>> itemPools
            = new ListDictionary<Type, CustomLinkedPool<Item>>();

        /// <summary>
        /// Tracks all instantiated Item objects, including those currently in use.
        /// </summary>
        public List<Item> AllItems { get; protected set; } = new List<Item>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the position where items will be instantiated.
        /// </summary>
        public Vector3 InstantiationPosition { get; set; } = Vector3.up;

        /// <summary>
        /// Gets or sets the rotation for newly instantiated items.
        /// </summary>
        public Quaternion InstantiationRotation { get; set; } = Quaternion.identity;

        #endregion

        #region Unity

        protected override void Awake()
        {
            base.Awake();

            foreach (var item in itemPrefabs)
            {
                itemPools.Add(
                    item.GetType(),
                    new CustomLinkedPool<Item>(
                        () => CreateNewItem(item),   
                        OnTakeFromPool,             
                        OnReturnToPool          
                    )
                );
            }
        }

        protected virtual void OnEnable() 
        {
            NetworkedObjectsManager.Instance.OnObjectInstantiation += HandleOnObjectInstantiation;
            SceneManager.sceneUnloaded += HandleSceneChange;
        }

        protected virtual void OnDisable()
        {
            NetworkedObjectsManager.Instance.OnObjectInstantiation -= HandleOnObjectInstantiation;
            SceneManager.sceneUnloaded -= HandleSceneChange;
        }

        #endregion

        #region Public

        /// <summary>
        /// Retrieves an Item of type T from the pool.
        /// </summary>
        /// <returns>An Item object ready for use.</returns>
        public virtual T GetItem<T>() where T : Item
        {
            if (!itemPools.TryGet(typeof(T), out var itemPool))
                return null;

            return itemPool.Get() as T;
        }

        /// <summary>
        /// Retrieves a random Device from the pool.
        /// </summary>
        /// <returns>A random Device object ready for use.</returns>
        public virtual Device GetRandomDevice(int seed)
        {
            var devicePrefabs = itemPrefabs.Where(i => i is Device).ToList();
            if (devicePrefabs.Count == 0)
                return null;

            System.Random rng = new System.Random(seed);
            var randomIndex = rng.Next(devicePrefabs.Count);
            var randomType = devicePrefabs[randomIndex].GetType();

            if (itemPools.TryGet(randomType, out var itemPool) && typeof(Device).IsAssignableFrom(randomType))
            {
                return itemPool.Get() as Device;
            }

            return null;
        }

        /// <summary>
        /// Returns an Item to the pool for future reuse.
        /// </summary>
        /// <param name="item">The Item object to return.</param>
        public virtual void ReturnItem(Item item)
        {
            if (!itemPools.TryGet(item.GetType(), out var itemPool))
                return;

            itemPool.Release(item);
        }

        /// <summary>
        /// Returns all currently available (inactive) items of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of Item to peek.</typeparam>
        /// <returns>A list of available items of the specified type.</returns>
        public virtual List<T> PeekAll<T>() where T : Item
        {
            if (!itemPools.TryGet(typeof(T), out var itemPool))
                return new List<T>();

            return (itemPool as CustomLinkedPool<Item>)
                   ?.PeekAll()
                   .OfType<T>()
                   .ToList()
                   ?? new List<T>();
        }

        /// <summary>
        /// Returns all tracked items to their respective pools.
        /// </summary>
        public virtual void ReturnAllItems()
        {
            foreach (Item item in AllItems)
                ReturnItem(item);
        }

        /// <summary>
        /// Resets all tracked items to their default state.
        /// </summary>
        public virtual void ResetAllItems()
        {
            foreach (Item item in AllItems)
                item.Reset();
        }

        #endregion

        #region Protected

        /// <summary>
        /// Creates a new Item instance.
        /// </summary>
        /// <returns>A new Item object.</returns>
        protected virtual T CreateNewItem<T>(T item) where T : Item
        {
            if (item == null)
                return null;

            T newItem = Instantiate(item, InstantiationPosition, InstantiationRotation, transform);
            return newItem;
        }

        /// <summary>
        /// Called when an Item is taken from the pool.
        /// </summary>
        /// <param name="item">The Item object.</param>
        protected virtual void OnTakeFromPool(Item item)
        {
            if (item == null) return;
            item.Enable(true);
        }

        /// <summary>
        /// Called when an Item is returned to the pool.
        /// </summary>
        /// <param name="item">The Item object.</param>
        protected virtual void OnReturnToPool(Item item)
        {
            if (item == null) return;
            item.Enable(false);
        }

        #endregion

        #region Private

        void HandleOnObjectInstantiation(GameObject gameObject)
        {
            Item item = gameObject.GetComponent<Item>();
            if (item == null) return;

            AllItems.Add(item);
        }

        void HandleSceneChange(Scene scene)
        {
            if (!string.Equals(scene.name, "IAIRoulette")) return;
            Destroy(this.gameObject);
        }

        #endregion
    }
}
