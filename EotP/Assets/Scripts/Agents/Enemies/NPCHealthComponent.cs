using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

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
        else
        {
            IEnumerator FlashRed()
            {
                SpriteRenderer _renderer = gameObject.GetComponent<SpriteRenderer>();
                if (_renderer == null) yield break;
                _renderer.color = Color.red;
                yield return new WaitForSeconds(.15f);
                _renderer.color = Color.gray;
            }

            IEnumerator co = FlashRed();
            StartCoroutine(co);
            
        }

        if (Health <= 0) OnDeath();

    }

    private void OnDeath()
    {
       if (DeathAction != null) DeathAction?.Invoke();
       else
       {
            Destroy(gameObject);
       }
    }
}
