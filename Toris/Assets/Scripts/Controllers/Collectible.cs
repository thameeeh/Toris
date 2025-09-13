using UnityEngine;

public class Collectible : MonoBehaviour
{
    public enum CollectibleType { Wood, Rock };
    public int amount = 1;
    public CollectibleType type;

    public float magnetDistance = 10f;
    public float magnetSpeed = 6f;

    bool isInArea = false;
    Vector3 SlideIntoPlayer;

    GameObject player;
    float distanceFromPlayer;
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            switch (type)
            {
                case CollectibleType.Wood:
                    Inventory.InventoryInstance.AddWood(amount);
                    break;
                case CollectibleType.Rock:
                    Inventory.InventoryInstance.AddRocks(amount);
                    break;
            }
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found in the scene. Please ensure there is a GameObject tagged 'Player'.");
        }
    }
    private void FixedUpdate()
    {
        distanceFromPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceFromPlayer < magnetDistance)
        {
            isInArea = true;
            SlideIntoPlayer = (player.transform.position - transform.position).normalized;
        }
        else
        {
            isInArea = false;
        }
    }
    private void Update()
    {
        if (isInArea)
        {
            transform.position += SlideIntoPlayer * Time.deltaTime * magnetSpeed;
        }
    }
}