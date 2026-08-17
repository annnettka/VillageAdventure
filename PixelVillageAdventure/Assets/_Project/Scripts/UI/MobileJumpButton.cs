using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MobileJumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Image targetImage;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.34f);
    [SerializeField] private Color pressedColor = new Color(1f, 1f, 1f, 0.62f);
    [SerializeField] private float pressedScale = 0.94f;

    private int activePointerId = int.MinValue;
    private Vector3 baseScale;

    private void Awake()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        baseScale = transform.localScale;
        SetPressedVisual(false);
    }

    private void OnDisable()
    {
        activePointerId = int.MinValue;
        SetPressedVisual(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
        {
            return;
        }

        activePointerId = eventData.pointerId;
        if (player != null)
        {
            player.RequestMobileJump();
        }

        SetPressedVisual(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            activePointerId = int.MinValue;
            SetPressedVisual(false);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            activePointerId = int.MinValue;
            SetPressedVisual(false);
        }
    }

    private void SetPressedVisual(bool pressed)
    {
        if (targetImage != null)
        {
            targetImage.color = pressed ? pressedColor : normalColor;
        }

        transform.localScale = pressed ? baseScale * pressedScale : baseScale;
    }
}
