using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.96f;

    private Vector3 normalScale = Vector3.one;

    private void Awake()
    {
        normalScale = transform.localScale;
    }

    private void OnEnable()
    {
        normalScale = transform.localScale;
    }

    private void OnDisable()
    {
        transform.localScale = normalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = normalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = normalScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = normalScale;
    }
}
