using UnityEngine;

namespace OutlandHaven.Inventory
{

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
            // Used sqrMagnitude instead of Distance for distance comparison to avoid expensive square root calculations
            if((transform.position - player.transform.position).sqrMagnitude < 9f)
            {
                float step = 5 * Time.deltaTime; // adjust speed as necessary
                transform.position = Vector3.MoveTowards(transform.position, player.transform.position, step);
            }
        }
    }

}