using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private float fallDelay = 0.06f;

    private static readonly int MoveStateParam = Animator.StringToHash("MoveState");
    private static readonly int ComboIndexParam = Animator.StringToHash("ComboIndex");
    private static readonly int InActionParam = Animator.StringToHash("InAction");
    private static readonly int AttackParam = Animator.StringToHash("Attack");
    private static readonly int HurtParam = Animator.StringToHash("Hurt");

    private const int IdleId = 0;
    private const int RunId = 1;
    private const int JumpId = 2;
    private const int FallId = 3;

    private int shownId = IdleId;
    private float fallingFor;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (animator == null) animator = GetComponent<Animator>();

        if (player == null || animator == null)
        {
            Debug.LogWarning("PlayerAnimator: missing PlayerController or Animator reference.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (player == null) return;

        player.AttackStarted += OnAttackStarted;
        player.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        if (player == null) return;

        player.AttackStarted -= OnAttackStarted;
        player.Damaged -= OnDamaged;
    }


    // Update is called once per frame
    void Update()
    {
        UpdateMoveState();
        UpdateInAction();
    }

    void UpdateMoveState()
    {
        int id = ToMoveStateId(player.MovementState);

        fallingFor = id == FallId ? fallingFor + Time.deltaTime : 0f;
        if (id == FallId && fallingFor < fallDelay)
        {
            id = shownId;
        }

        shownId = id;
        animator.SetInteger(MoveStateParam, id);
    }

    void UpdateInAction()
    {
        CharacterState state = player.CurrentState;
        bool inAction = state == CharacterState.Attacking || state == CharacterState.Hurt;
        animator.SetBool(InActionParam, inAction);
    }

    int ToMoveStateId(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.Walking: return RunId;
            case CharacterState.Jumping: return JumpId;
            case CharacterState.Falling: return FallId;
            default: return IdleId;
        }
    }

    void OnAttackStarted(int comboIndex)
    {
        animator.SetInteger(ComboIndexParam, comboIndex);
        animator.SetTrigger(AttackParam);
    }

    void OnDamaged()
    {
        animator.ResetTrigger(AttackParam);
        animator.SetTrigger(HurtParam);
    }
}
