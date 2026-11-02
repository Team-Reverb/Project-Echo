using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalSpeedParam = Animator.StringToHash("VerticalSpeed");
    private static readonly int AttackParam = Animator.StringToHash("Attack");
    private static readonly int HurtParam = Animator.StringToHash("Hurt");

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (body == null) body = GetComponentInParent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        player.AttackRequested += OnAttackRequested;
        player.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        player.AttackRequested -= OnAttackRequested;
        player.Damaged -= OnDamaged;
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat(SpeedParam, Mathf.Abs(body.linearVelocity.x));
        animator.SetBool(IsGroundedParam, player.IsGrounded);
        animator.SetFloat(VerticalSpeedParam, body.linearVelocity.y);

    }

    void OnAttackRequested()
    {
        animator.SetTrigger(AttackParam);
    }

    void OnDamaged()
    {
        animator.ResetTrigger(AttackParam);
        animator.SetTrigger(HurtParam);
    }

    // Animation events

    public void SwordHit()
    {
        player.SwordHit();
    }

    public void ComboEnd()
    {
        animator.ResetTrigger(AttackParam);
        player.StartAttackCooldown();
    }
}
