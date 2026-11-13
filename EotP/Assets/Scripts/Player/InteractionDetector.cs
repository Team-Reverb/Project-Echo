using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class InteractionDetector : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logTargetChanges;

    public event Action<IInteractable> TargetChanged;
    public IInteractable Current { get; private set; }

    private bool isLocked;
    private readonly List<IInteractable> inRange = new List<IInteractable>();
    private PlayerController player;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    void OnDisable()
    {
        inRange.Clear();
        SetCurrent(null);
    }

    // Update is called once per frame
    void Update()
    {
        inRange.RemoveAll(i => !IsValid(i));
        SetCurrent(isLocked ? null : FindClosest());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null && !inRange.Contains(interactable))
        {
            inRange.Add(interactable);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            inRange.Remove(interactable);
        }
    }

    IInteractable FindClosest()
    {
        IInteractable best = null;
        float bestDistance = float.MaxValue;
        Vector2 origin = transform.position;

        foreach (IInteractable candidate in inRange)
        {
            if (!candidate.CanInteract(player)) continue;

            float distance = (candidate.InteractionPoint - origin).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    void SetCurrent(IInteractable next)
    {
        if (ReferenceEquals(next, Current)) return;

        if (IsValid(Current)) Current.SetHighlighted(false);
        Current = next;
        if (Current != null) Current.SetHighlighted(true);

        if (logTargetChanges)
        {
            Debug.Log("Interaction target: " + (Current != null ? Current.Prompt : "none"));
        }

        TargetChanged?.Invoke(Current);
    }

    public bool TryInteract()
    {
        if (!IsValid(Current) || !Current.CanInteract(player)) return false;

        Current.Interact(player);
        return true;
    }

    static bool IsValid(IInteractable interactable)
    {
        if (interactable == null) return false;
        if (interactable is Component component)
        {
            return component != null && component.gameObject.activeInHierarchy;
        }
        return true;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (locked)
        {
            SetCurrent(null);
        }
    }
}
