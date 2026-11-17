using UnityEngine;
using System;

public class NPCHealthComponent : MonoBehaviour
{

    [Header("Values")]
    public float MaxHealth;
    public float Health;

    [Header("References")]
    public GameObject HealthBar;
    public Action DamageAction;
    public Action DeathAction;

    void Start()
    {
        Health = MaxHealth;
    }


    void Update()
    {
        
    }

    public void TakeDamage(float amount)
    {

        Health -= amount;
        OnDamage(amount);
    }

    private void OnDamage(float amount)
    {
        Debug.Log(amount + " damage taken by " + gameObject.name);
        if (DamageAction != null) DamageAction?.Invoke();

        if (Health <= 0) OnDeath();

    }

    private void OnDeath()
    {
        DeathAction?.Invoke();
    }
}
