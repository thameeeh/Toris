using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class HomeAnchor : MonoBehaviour
{
    [SerializeField] private Vector3 center;
    [SerializeField] private float radius = 8f;
    [SerializeField] private bool initializeFromTransformIfUnset = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmo = true;
    [SerializeField] private bool drawOnlyWhenSelected = true;
    [SerializeField] private float centerMarkerRadius = 0.15f;
    [SerializeField] private Color centerColor = Color.red;
    [SerializeField] private Color radiusColor = Color.yellow;
    [SerializeField] private Color linkColor = Color.cyan;

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
        if (!showDebugGizmo || drawOnlyWhenSelected)
            return;

        DrawDebugGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmo || !drawOnlyWhenSelected)
            return;

        DrawDebugGizmo();
    }

    private void DrawDebugGizmo()
    {
        Gizmos.color = centerColor;
        Gizmos.DrawSphere(center, centerMarkerRadius);

        Handles.color = radiusColor;
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        Gizmos.color = linkColor;
        Gizmos.DrawLine(transform.position, center);
    }
#endif
}