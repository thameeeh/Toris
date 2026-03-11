using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class HomeAnchor : MonoBehaviour
{
    [SerializeField] private Vector3 center;
    [SerializeField] private float radius = 8f;
    [SerializeField] private bool initializeFromTransformIfUnset = true;

    public Vector3 Center
    {
        get => center;
        set => center = value;
    }

    public float Radius
    {
        get => radius;
        set => radius = Mathf.Max(0.01f, value);
    }

    public void SetHome(Vector3 newCenter, float newRadius)
    {
        center = newCenter;
        radius = Mathf.Max(0.01f, newRadius);
    }

    private void Awake()
    {
        if (initializeFromTransformIfUnset && center == Vector3.zero)
        {
            center = transform.position;
        }
    }

    private void OnValidate()
    {
        radius = Mathf.Max(0.01f, radius);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(center, 0.15f);

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, center);
    }
#endif
}