using UnityEngine;

public sealed class PlayerAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    [SerializeField] private PlayerController player;
    [SerializeField] private Animator animator;

    private void Reset()
    {
        player = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void LateUpdate()
    {
        if (player == null || animator == null)
        {
            return;
        }

        animator.SetFloat(SpeedHash, player.HorizontalSpeed);
        animator.SetBool(IsGroundedHash, player.IsGrounded);
        animator.SetBool(DeadHash, player.IsDead);
    }

    public void SetDead(bool dead)
    {
        if (animator != null)
        {
            animator.SetBool(DeadHash, dead);
        }
    }

    public void ResetToIdle()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(DeadHash, false);
        animator.SetFloat(SpeedHash, 0f);
        animator.SetBool(IsGroundedHash, true);
        animator.Play("Idle", 0, 0f);
    }
}
