using UnityEngine;

public class LineFollower2D : MonoBehaviour
{
    public Transform startNode;
    public Transform endNode;
    private LineRenderer lr;

    void Start() => lr = GetComponent<LineRenderer>();

    void Update()
    {
        if (startNode && endNode)
        {
            lr.SetPosition(0, startNode.position);
            lr.SetPosition(1, endNode.position);
        }
    }
}