namespace JazzCreate.MultiPurposeUIPixel
{
    using UnityEngine;

    public class PlayerShoot : MonoBehaviour
    {
        [SerializeField] private Transform gunTip;
        [SerializeField] private float bulletSpeed = 5f;

        private void Update()
        {
            // Fire1 (mouse/touch/ctrl depending on Input Manager) OR Space
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
            {
                FireBullet();
            }
        }

        private void FireBullet()
        {
            if (gunTip == null)
            {
                Debug.LogError("PlayerShoot: gunTip not assigned in Inspector.", this);
                return;
            }

            if (BulletPoolScript.Instance == null)
            {
                Debug.LogError("PlayerShoot: BulletPoolScript.Instance is NULL. Make sure BulletPoolScript exists in the scene.", this);
                return;
            }

            if (!BulletPoolScript.Instance.TryGetBullet(out GameObject obj, out Rigidbody2D rb))
                return;

            obj.transform.position = gunTip.position;
            obj.transform.rotation = Quaternion.identity; // optional: keep bullets unrotated
            obj.SetActive(true);

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                // Always shoot straight up the screen (world +Y)
                rb.linearVelocity = Vector2.up * bulletSpeed;
            }
        }
    }
}


