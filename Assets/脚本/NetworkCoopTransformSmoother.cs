using UnityEngine;

public class NetworkCoopTransformSmoother : MonoBehaviour
{
    public float positionSmoothTime = 0.08f;
    public float rotationLerpSpeed = 18f;
    public float snapDistance = 45f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasTarget;

    void Awake()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    public void ResetTo(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        hasTarget = true;
    }

    public void SetTarget(Vector3 position, Quaternion rotation)
    {
        if (!hasTarget)
        {
            ResetTo(position, rotation);
            return;
        }

        if ((position - transform.position).sqrMagnitude > snapDistance * snapDistance)
        {
            ResetTo(position, rotation);
            return;
        }

        targetPosition = position;
        targetRotation = rotation;
    }

    void Update()
    {
        if (!hasTarget)
            return;

        float positionT = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, positionSmoothTime));
        float rotationT = 1f - Mathf.Exp(-Mathf.Max(1f, rotationLerpSpeed) * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionT);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);
    }
}
