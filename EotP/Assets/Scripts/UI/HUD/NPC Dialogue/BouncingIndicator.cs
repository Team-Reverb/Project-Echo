using UnityEngine;

public class BouncingIndicator : MonoBehaviour
{
    [SerializeField] private float height = 5f;
    [SerializeField] private float speed = 5f;

    private RectTransform rect;
    private Vector2 basePosition;
    private float startTime;

    void Awake()
    {
        rect = (RectTransform)transform;
        basePosition = rect.anchoredPosition;
    }

    void OnEnable()
    {
        startTime = Time.unscaledTime;
    }

    // Update is called once per frame
    void Update()
    {
        float t = (Time.unscaledTime - startTime) * speed;
        float offset = (1f - Mathf.Cos(t)) * 0.5f * height;
        rect.anchoredPosition = basePosition + Vector2.up * offset;
    }
    void OnDisable()
    {
        rect.anchoredPosition = basePosition;
    }
}
