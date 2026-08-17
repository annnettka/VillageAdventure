namespace JazzCreate.MultiPurposeUIPixel
{
    using System.Collections.Generic;
    using UnityEngine;

    public class BulletPoolScript : MonoBehaviour
    {
        public static BulletPoolScript Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private int pooledAmount = 10;
        [SerializeField] private bool willGrow = true;

        private readonly List<PooledBullet> pool = new();

        private void Awake()
        {
            // Singleton (but don't destroy silently without telling you)
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate BulletPoolScript found. Removing the duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (bulletPrefab == null)
            {
                Debug.LogError("BulletPoolScript: bulletPrefab is NOT assigned in the Inspector.", this);
                return;
            }

            // Prewarm in Awake so it's ready immediately
            for (int i = 0; i < pooledAmount; i++)
                pool.Add(CreateNew());
        }

        private PooledBullet CreateNew()
        {
            GameObject go = Instantiate(bulletPrefab, transform);
            go.SetActive(false);

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.GetComponentInChildren<Rigidbody2D>();

            if (rb == null)
                Debug.LogError("Bullet prefab needs a Rigidbody2D (on root or child).", bulletPrefab);

            return new PooledBullet(go, rb);
        }

        public bool TryGetBullet(out GameObject bullet, out Rigidbody2D rb)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].Go.activeInHierarchy)
                {
                    bullet = pool[i].Go;
                    rb = pool[i].Rb;
                    return true;
                }
            }

            if (willGrow)
            {
                var created = CreateNew();
                pool.Add(created);

                bullet = created.Go;
                rb = created.Rb;
                return true;
            }

            bullet = null;
            rb = null;
            return false;
        }

        private readonly struct PooledBullet
        {
            public readonly GameObject Go;
            public readonly Rigidbody2D Rb;

            public PooledBullet(GameObject go, Rigidbody2D rb)
            {
                Go = go;
                Rb = rb;
            }
        }
    }
}


