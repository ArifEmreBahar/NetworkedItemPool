
using Photon.Pun;
using AEB.Photon;
using System;
using UnityEngine;

namespace AEB.Utilities
{
    /// <summary>
    /// Manages a networked pool of items using Photon for instantiation.
    /// </summary>
    public class NetworkedItemPool : ItemPool
    {
        public PhotonView PhotonView { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            PhotonView = GetComponent<PhotonView>();
        }
        protected override void OnEnable()
        {
            base.OnEnable();

            PhotonNetwork.AddCallbackTarget(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            PhotonNetwork.RemoveCallbackTarget(this);
        }

        /// <summary>
        /// Creates a new item instance using Photon instantiation.
        /// </summary>
        /// <typeparam name="T">The type of item to create.</typeparam>
        /// <param name="item">The item prefab to instantiate.</param>
        /// <returns>A new networked item instance.</returns>
        protected override T CreateNewItem<T>(T item)
        {
            Item newItem = PhotonNetwork
                .Instantiate(item.name, InstantiationPosition, InstantiationRotation)
                .GetComponent<Item>();

            NetworkedObjectsManager.Instance.CacheMe(newItem.gameObject);

            return newItem as T;
        }

        // --------------------------------------------------------------------
        // GET ITEM
        // --------------------------------------------------------------------
        public override T GetItem<T>()
        {
            Item item = base.GetItem<T>();

            if (NetworkedObjectsManager.Instance.TryGetViewID(item.gameObject, out var viewID))
            { PhotonView.RPC(nameof(RPC_HandleItemPoolActions), RpcTarget.Others, 1, viewID);
            }

            return item as T;
        }

        // --------------------------------------------------------------------
        // RETURN ITEM
        // --------------------------------------------------------------------
        public override void ReturnItem(Item item)
        {
            if (item == null) return;

            if (NetworkedObjectsManager.Instance.TryGetViewID(item.gameObject, out int viewID))
                {
                CallRPC(RPC_HandleItemPoolActions, RpcTarget.AllViaServer, 2, viewID); }
        }

        // --------------------------------------------------------------------
        // ON TAKE FROM POOL
        // --------------------------------------------------------------------
        protected override void OnTakeFromPool(Item item)
        {
            base.OnTakeFromPool(item);
        }

        // --------------------------------------------------------------------
        // ON RETURN TO POOL
        // --------------------------------------------------------------------
        protected override void OnReturnToPool(Item item)
        {
            base.OnReturnToPool(item);
        }

        // --------------------------------------------------------------------
        // RPC CALLER
        // --------------------------------------------------------------------
        void CallRPC(Action<int, int> RPC, RpcTarget target, int action, int param1 = -1)
        {
            if (PhotonView != null && PhotonNetwork.InRoom)
            {
                if (PhotonView.IsMine)
                    PhotonView.RPC(RPC.Method.Name, target, action, param1);
            }
            else
            {
                RPC(action, param1);
            }
        }

        // --------------------------------------------------------------------
        // RPC RECEIVER
        // --------------------------------------------------------------------
        [PunRPC]
        private void RPC_HandleItemPoolActions(int actionId, int param1)
        {
            switch (actionId)
            {
                // -------------------------------------------------------------
                // CASE 0: CREATE ITEM
                // -------------------------------------------------------------
                case 0:
                    {
                        Item item = NetworkedObjectsManager.Instance.GetObject(param1).GetComponent<Item>();
                        CustomLinkedPool<Item> pool = itemPools.Get(item.GetType());
                        if (pool != null)
                            pool.Add(item);
                    }
                    break;

                // -------------------------------------------------------------
                // CASE 1: GET ITEM
                // -------------------------------------------------------------
                case 1:
                    {
                        GameObject go = NetworkedObjectsManager.Instance.GetObject(param1);
                        if (go != null)
                        {
                            Item itemToReturn = go.GetComponent<Item>();
                            if (itemToReturn != null)
                            {
                                int idx = itemPools.IndexOf(itemToReturn.GetType());
                                if (idx != -1)
                                  { var itemPool = itemPools.GetByIndex(idx).Value;
                                    var itemj = itemPool.Get();
                                }
                            }
                        }
                    }
                    break;

                // -------------------------------------------------------------
                // CASE 2: RETURN TO POOL
                // -------------------------------------------------------------
                case 2:
                    {
                        GameObject go = NetworkedObjectsManager.Instance.GetObject(param1);
                        if (go != null)
                        {
                            Item itemToReturn = go.GetComponent<Item>();
                            if (itemToReturn != null)
                            {
                                // Find the correct pool based on item type
                                int idx = itemPools.IndexOf(itemToReturn.GetType());
                                if (idx != -1)
                                {
                                    var itemPool = itemPools.GetByIndex(idx).Value;
                                    itemPool.Release(itemToReturn);
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}
