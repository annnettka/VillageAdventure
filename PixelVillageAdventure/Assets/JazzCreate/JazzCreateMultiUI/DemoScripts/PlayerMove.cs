namespace JazzCreate.MultiPurposeUIPixel
{
    using UnityEngine;

    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float shipSpeed = 5f;

        [Header("Screen Bounds")]
        [SerializeField] private float padding = 1f;
        [SerializeField] private bool recalcBoundsEveryFixedUpdate = false;

        [Header("Vertical Limit (Optional)")]
        [SerializeField] private bool limitTopMovement = false;
        [Range(0.1f, 1f)]
        [SerializeField] private float maxScreenHeight = 0.85f;

        private Rigidbody2D rb2D;
        private Camera cam;

        private float minX, maxX, minY, maxY;

        private void Awake()
        {
            rb2D = GetComponent<Rigidbody2D>();
            cam = Camera.main;
        }

        private void Start()
        {
            RecalculateBounds();
        }

        private void FixedUpdate()
        {
            if (recalcBoundsEveryFixedUpdate)
                RecalculateBounds();

            float moveHorizontal = Input.GetAxisRaw("Horizontal");
            float moveVertical = Input.GetAxisRaw("Vertical");

            Vector2 input = new Vector2(moveHorizontal, moveVertical);

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            Vector2 delta = input * shipSpeed * Time.fixedDeltaTime;
            Vector2 targetPos = rb2D.position + delta;

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

            rb2D.MovePosition(targetPos);
        }

        private void RecalculateBounds()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }

            Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
            Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

            minX = bottomLeft.x + padding;
            minY = bottomLeft.y + padding;
            maxX = topRight.x - padding;
            maxY = topRight.y - padding;

            // Optional top limit
            if (limitTopMovement)
            {
                Vector3 limitedTop = cam.ViewportToWorldPoint(
                    new Vector3(0.5f, maxScreenHeight, 0f)
                );
                maxY = Mathf.Min(maxY, limitedTop.y - padding);
            }
        }
    }
}
