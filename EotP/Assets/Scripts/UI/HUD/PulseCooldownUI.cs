using UnityEngine;
using UnityEngine.UI;

public class PulseCooldownUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Image cooldownFillImage;
    public Image outlineImage;
    public CanvasGroup canvasGroup;

    [Header("Combat Outline Flash")]
    public float flashSpeed = 4f;
    public float minOutlineAlpha = 0f;
    public float maxOutlineAlpha = 0.8f;

    [Header("Unavailable Transparency")]
    public float unavailableAlpha = 0.4f;

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        UpdateCooldownBar();
        UpdateCombatOutline();
        UpdateTransparency();
    }

    void UpdateCooldownBar()
    {
        if (cooldownFillImage == null) return;

        cooldownFillImage.fillAmount = player.PulseCooldownNormalized;
    }

    void UpdateCombatOutline()
    {
        if (outlineImage == null) return;

        if (player.IsInCombat)
        {
            outlineImage.enabled = true;

            float t = (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f;
            float alpha = Mathf.Lerp(minOutlineAlpha, maxOutlineAlpha, t);

            Color c = outlineImage.color;
            c.a = alpha;
            outlineImage.color = c;
        }
        else
        {
            outlineImage.enabled = false;
        }
    }

    void UpdateTransparency()
    {
        if (canvasGroup == null) return;

        if (player.IsPulseReady == true)
        {
            canvasGroup.alpha = 1f;
        }
        else
        {
            canvasGroup.alpha = unavailableAlpha;
        }
    }
}
