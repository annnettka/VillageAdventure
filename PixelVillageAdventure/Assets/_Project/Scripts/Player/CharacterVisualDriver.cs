using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterVisualDriver : MonoBehaviour
{
    private readonly Dictionary<string, AnimatorControllerParameterType> animatorParameters = new Dictionary<string, AnimatorControllerParameterType>();

    private PlayerController player;
    private SpriteRenderer primaryRenderer;
    private Sprite fallbackSprite;
    private Animator animator;
    private bool deathTriggered;

    public void Initialize(PlayerController targetPlayer, Sprite previewFallback, int sortingOrder)
    {
        player = targetPlayer;
        animator = GetComponentInChildren<Animator>(true);
        primaryRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = sortingOrder;
            if (fallbackSprite == null && renderer.sprite != null)
            {
                fallbackSprite = renderer.sprite;
            }
        }

        if (fallbackSprite == null)
        {
            fallbackSprite = previewFallback;
        }

        CacheAnimatorParameters();
        RestoreMissingSprites();
    }

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
            CacheAnimatorParameters();
        }

        if (primaryRenderer == null)
        {
            primaryRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        UpdateFacing();
        UpdateAnimatorParameters();
        RestoreMissingSprites();
    }

    private void UpdateFacing()
    {
        if (primaryRenderer == null)
        {
            return;
        }

        float xVelocity = player.Velocity.x;
        if (Mathf.Abs(xVelocity) > 0.05f)
        {
            primaryRenderer.flipX = xVelocity < 0f;
        }
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            return;
        }

        float speed = player.HorizontalSpeed;
        bool moving = speed > 0.05f;
        bool grounded = player.IsGrounded;
        bool dead = player.IsDead;

        SetBoolIfExists("IsMoving", moving);
        SetBoolIfExists("Moving", moving);
        SetBoolIfExists("Run", moving);
        SetBoolIfExists("Running", moving);
        SetBoolIfExists("IsRunning", moving);
        SetBoolIfExists("Grounded", grounded);
        SetBoolIfExists("IsGrounded", grounded);
        SetBoolIfExists("Air", !grounded);
        SetBoolIfExists("InAir", !grounded);
        SetBoolIfExists("Jump", !grounded);
        SetBoolIfExists("IsJumping", !grounded);
        SetBoolIfExists("Dead", dead);
        SetBoolIfExists("IsDead", dead);
        SetFloatIfExists("Speed", speed);
        SetFloatIfExists("HorizontalSpeed", speed);
        SetFloatIfExists("VerticalSpeed", player.Velocity.y);

        if (dead && !deathTriggered)
        {
            SetTriggerIfExists("Death");
            SetTriggerIfExists("Die");
            deathTriggered = true;
        }
        else if (!dead)
        {
            deathTriggered = false;
        }
    }

    private void RestoreMissingSprites()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sprite == null && fallbackSprite != null)
            {
                renderer.sprite = fallbackSprite;
            }
        }
    }

    private void CacheAnimatorParameters()
    {
        animatorParameters.Clear();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            animatorParameters[parameter.name] = parameter.type;
        }
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (animatorParameters.TryGetValue(parameterName, out AnimatorControllerParameterType type) && type == AnimatorControllerParameterType.Bool)
        {
            animator.SetBool(parameterName, value);
        }
    }

    private void SetFloatIfExists(string parameterName, float value)
    {
        if (animatorParameters.TryGetValue(parameterName, out AnimatorControllerParameterType type))
        {
            if (type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
            }
            else if (type == AnimatorControllerParameterType.Int)
            {
                animator.SetInteger(parameterName, Mathf.RoundToInt(value));
            }
        }
    }

    private void SetTriggerIfExists(string parameterName)
    {
        if (animatorParameters.TryGetValue(parameterName, out AnimatorControllerParameterType type) && type == AnimatorControllerParameterType.Trigger)
        {
            animator.SetTrigger(parameterName);
        }
    }
}
