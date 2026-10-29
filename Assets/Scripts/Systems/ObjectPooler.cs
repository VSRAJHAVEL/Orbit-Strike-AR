using System.Collections.Generic;
using UnityEngine;

namespace OrbitStrike.Systems
{
    /// <summary>
    /// High-performance object pooling system for VFX, debris, crater decals, and score popups.
    /// Prevents GC spikes on mobile devices to sustain 60 FPS.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [System.Serializable]
        public class PoolItem
        {
            public string poolKey;
            public GameObject prefab;
            public int initialSize = 10;
        }

        [SerializeField] private List<PoolItem> pools = new List<PoolItem>();

        private readonly Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> _prefabMap = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var item in pools)
            {
                if (item.prefab == null) continue;

                _prefabMap[item.poolKey] = item.prefab;
                var queue = new Queue<GameObject>();

                for (int i = 0; i < item.initialSize; i++)
                {
                    GameObject obj = Instantiate(item.prefab, transform);
                    obj.SetActive(false);
                    queue.Enqueue(obj);
                }

                _poolDictionary[item.poolKey] = queue;
            }
        }

        public GameObject Spawn(string poolKey, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(poolKey))
            {
                if (_prefabMap.ContainsKey(poolKey))
                {
                    _poolDictionary[poolKey] = new Queue<GameObject>();
                }
                else
                {
                    Debug.LogWarning($"[ObjectPooler] No pool registered for key: {poolKey}");
                    return null;
                }
            }

            GameObject obj;
            if (_poolDictionary[poolKey].Count > 0)
            {
                obj = _poolDictionary[poolKey].Dequeue();
            }
            else
            {
                obj = Instantiate(_prefabMap[poolKey], transform);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public void Recycle(string poolKey, GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (!_poolDictionary.ContainsKey(poolKey))
            {
                _poolDictionary[poolKey] = new Queue<GameObject>();
            }

            _poolDictionary[poolKey].Enqueue(obj);
        }
    }
}

