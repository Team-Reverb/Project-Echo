using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Framing")]
    public float heightOffset = 2f;
    public float distance = -10f;

    [Header("Smoothing")]
    public float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = new Vector3(
            target.position.x,
            target.position.y + heightOffset,
            target.position.z + distance
        );

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
