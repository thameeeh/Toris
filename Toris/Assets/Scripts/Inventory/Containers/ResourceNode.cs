using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ResourceNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [SerializeField]
    private ResourceData ResourceToGive;

    [SerializeField]
    private int ResourceAmount = 1;

    GameObject player;
    SpriteRenderer spriteRenderer;
    private Color originalColor;
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(Vector3.Distance(transform.position, player.transform.position) < 3f)
        {
           spriteRenderer.color = Color.yellow;
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        // 5. On exit, set back to the original color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        Debug.Log("Mouse Exited!");
    }
}
