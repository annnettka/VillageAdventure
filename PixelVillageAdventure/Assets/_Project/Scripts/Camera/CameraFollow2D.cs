using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private bool followVertical = true;

    private Vector3 velocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        desired.z = offset.z;

        if (!followVertical)
        {
            desired.y = transform.position.y;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        desired.z = offset.z;
        transform.position = desired;
        velocity = Vector3.zero;
    }
}
