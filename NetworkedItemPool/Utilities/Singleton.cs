using UnityEngine;

namespace AEB.Utilities
{
	public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		protected static T instance;
		public static T Instance
		{
			get
			{
				lock (_instanceLock)
				{
					if (instance == null && !_quitting)
					{
						instance = GameObject.FindObjectOfType<T>();

						if (instance == null)
						{
							GameObject go = new GameObject(typeof(T).ToString());
							instance = go.AddComponent<T>();					
						}

						if (instance is Singleton<T> singletonInstance && singletonInstance.ShouldPersist)
							DontDestroyOnLoad(instance.gameObject);
					}

					return instance;
				}
			}
		}

		#region Fields

		static readonly object _instanceLock = new object();
		static bool _quitting = false;

		#endregion

		#region Properties

		/// <summary>
		/// Determines whether this instance should persist across scene changes.
		/// Can be overridden by derived classes to control persistence behavior.
		/// </summary>
		protected virtual bool ShouldPersist => true;

		#endregion

		#region Unity

		protected virtual void Awake()
		{
			if (instance == null) Initialize();
			if (instance.GetInstanceID() != GetInstanceID()) Terminate();
		}

		protected virtual void OnApplicationQuit()
		{
			_quitting = true;
		}

        #endregion

        #region Protected

        protected virtual void Initialize()
		{
			instance = this as T;
			if (ShouldPersist)
				DontDestroyOnLoad(instance.gameObject);
		}

		protected virtual void Terminate()
		{
			Destroy(gameObject);
		}

        #endregion
    }
}