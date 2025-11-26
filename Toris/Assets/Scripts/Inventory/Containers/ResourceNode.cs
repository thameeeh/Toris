using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ResourceNode : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private ResourceData ResourceToGive;

    [SerializeField]
    private int ResourceAmount = 1;

    GameObject player;
    SpriteRenderer spriteRenderer;
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, player.transform.position) < .5f)
        {
            Inventory.InventoryInstance.AddResource(ResourceToGive, ResourceAmount);
            Destroy(gameObject);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Inventory.InventoryInstance.AddResource(ResourceToGive, ResourceAmount);
        Destroy(gameObject);
    }
}
