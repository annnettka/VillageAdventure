namespace JazzCreate.MultiPurposeUIPixel
{
    using System.Collections.Generic;
    using UnityEngine;

    public class EnemyPoolScript : MonoBehaviour
    {
        public static EnemyPoolScript Instance { get; private set; }

        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int pooledAmount = 10;
        [SerializeField] private bool willGrow = true;

        private readonly List<GameObject> pool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate EnemyPoolScript found. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (enemyPrefab == null)
                Debug.LogError("EnemyPoolScript: enemyPrefab not assigned!", this);
        }

        private void Start()
        {
            for (int i = 0; i < pooledAmount; i++)
                pool.Add(CreateNew());
        }

        private GameObject CreateNew()
        {
            var obj = Instantiate(enemyPrefab, transform);
            obj.SetActive(false);
            return obj;
        }

        public GameObject GetEnemyObject()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].activeInHierarchy)
                    return pool[i];
            }

            if (willGrow)
            {
                var obj = CreateNew();
                pool.Add(obj);
                return obj;
            }

            return null;
        }
    }
}

