using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerController player;
    public Image fillImage;

    // Update is called once per frame
    void Update()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null || fillImage == null) return;

        fillImage.fillAmount = player.HealthNormalized;
    }
}
