using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    Vector3 playerPosition;
    GameObject player;

    int speed = 5;
    float distance;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerPosition = player.transform.position;
    }
    // Update is called once per frame
    void Update()
    {
        playerPosition = player.transform.position;
        Vector3 goTo = playerPosition - gameObject.transform.position;
        gameObject.transform.position += goTo.normalized * Time.deltaTime * speed;

        distance = Vector3.Distance(playerPosition, gameObject.transform.position);

        if (distance < 0.1) {
            speed = 0;
            Diactivate();
        } else
        {
            speed = 5;
        }
    }

    private void Diactivate()
    {
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
