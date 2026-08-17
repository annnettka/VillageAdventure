using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class GameBackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 0.1f)] private float parallaxStrength;
    [SerializeField] private float coverPadding = 1.08f;
    [SerializeField] private float cameraZOffset = 10f;

    private Vector3 originCameraPosition;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetCamera = Camera.main;
        CaptureOrigins();
        FitNow();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureOrigins();
        FitNow();
    }

    private void LateUpdate()
    {
        FitNow();
    }

    public void FitNow()
    {
        ResolveReferences();
        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.transform.position;
        Vector3 desiredPosition = cameraPosition;
        if (parallaxStrength > 0f)
        {
            desiredPosition += (originCameraPosition - cameraPosition) * parallaxStrength;
        }

        desiredPosition.z = cameraPosition.z + cameraZOffset;
        transform.position = desiredPosition;

        if (!targetCamera.orthographic)
        {
            return;
        }

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y) * Mathf.Max(1f, coverPadding);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void CaptureOrigins()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        originCameraPosition = targetCamera != null ? targetCamera.transform.position : Vector3.zero;
    }
}
