using UnityEngine;

public class ItemPickMagnet : MonoBehaviour
{
    GameObject player;
    void Start()
    {
        player =  GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;
        if(Vector3.Distance(transform.position, player.transform.position) < 3f)
        {
            float step = 5 * Time.deltaTime; // adjust speed as necessary
            transform.position = Vector3.MoveTowards(transform.position, player.transform.position, step);
        }
    }
}
