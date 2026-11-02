using UnityEngine;

public class PunchingBag : MonoBehaviour
{
    public void TakeDamage(float amount)
    {
        Debug.Log(amount + " damage taken by Punching Bag");
    }
}
