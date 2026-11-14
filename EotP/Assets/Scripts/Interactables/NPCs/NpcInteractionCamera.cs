using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class NpcInteractionCamera : MonoBehaviour
{
    [Header("Focus")]
    [SerializeField] private Transform focusPoint; // Optional. Defaults to object's parent
    [SerializeField] private float focusHeightOffset = 0.75f;

    [Header("Zoom")]
    [SerializeField] private float zoomFactor = 0.7f;
    [SerializeField] private float blendTime = 0.5f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.enabled = false;
    }

    public IEnumerator BeginRoutine(Camera from)
    {
        transform.position = from.transform.position;
        cam.orthographicSize = from.orthographicSize;

        cam.enabled = true;
        from.enabled = false;

        Vector3 target = GetFocusPosition(from.transform.position.z);
        yield return BlendTo(target, from.orthographicSize * zoomFactor);
    }

    public IEnumerator EndRoutine(Camera to)
    {
        yield return BlendTo(to.transform.position, to.orthographicSize);

        to.enabled = true;
        cam.enabled = false;
    }

    Vector3 GetFocusPosition(float z)
    {
        Transform focus = focusPoint != null ? focusPoint : (transform.parent != null ? transform.parent : transform);
        return new Vector3(focus.position.x, focus.position.y + focusHeightOffset, z);
    }

    IEnumerator BlendTo(Vector3 targetPosition, float targetSize)
    {
        Vector3 startPosition = transform.position;
        float startSize = cam.orthographicSize;
        float elapsed = 0f;

        while (elapsed < blendTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / blendTime);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }

        transform.position = targetPosition;
        cam.orthographicSize = targetSize;
    }
}
