using UnityEngine;

/// <summary>Keeps the imported character animation in sync with the existing player controller.</summary>
public sealed class PlayerCharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator[] animators;
    [SerializeField] private float transitionDuration = 0.12f;
    [SerializeField] private float actionDuration = 0.55f;

    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
    private static readonly int AirState = Animator.StringToHash("Base Layer.Air");
    private static readonly int ClimbState = Animator.StringToHash("Base Layer.Climb");
    private static readonly int ActionState = Animator.StringToHash("Base Layer.Action");
    private static readonly int CarryState = Animator.StringToHash("Carry Layer.Carry");

    private int currentState;
    private float actionUntil;

    private void Awake()
    {
        if (animators == null || animators.Length == 0)
        {
            animators = GetComponentsInChildren<Animator>(true);
        }

        foreach (Animator animator in animators)
        {
            if (animator == null) continue;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
    }

    public void UpdateMotion(float speed, bool grounded, bool climbing, bool holding)
    {
        UpdateCarryLayer(holding && Time.time >= actionUntil);
        if (Time.time < actionUntil) return;

        int nextState = climbing && speed > 0.05f
            ? ClimbState
            : !grounded
                ? AirState
                : speed > 0.05f ? WalkState : IdleState;

        CrossFade(nextState);
    }

    public void PlayAction()
    {
        actionUntil = Time.time + actionDuration;
        UpdateCarryLayer(false);
        CrossFade(ActionState);
    }

    private void UpdateCarryLayer(bool active)
    {
        foreach (Animator animator in animators)
        {
            if (animator == null || animator.runtimeAnimatorController == null || animator.layerCount < 2) continue;
            if (active && animator.GetLayerWeight(1) < 0.01f) animator.CrossFade(CarryState, transitionDuration, 1);
            float target = active ? 1f : 0f;
            animator.SetLayerWeight(1, Mathf.MoveTowards(animator.GetLayerWeight(1), target, Time.deltaTime * 8f));
        }
    }

    private void CrossFade(int state)
    {
        if (currentState == state) return;
        currentState = state;
        foreach (Animator animator in animators)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.CrossFade(state, transitionDuration);
            }
        }
    }
}
